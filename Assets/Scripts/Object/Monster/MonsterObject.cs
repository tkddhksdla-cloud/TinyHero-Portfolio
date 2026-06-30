using System.Collections;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Maps;
using TinyHero.Player;
using TinyHero.Quest;
using TinyHero.Skill;
using TinyHero.UI;
using UnityEngine;

///<summary>
/// 몬스터 정보와 상태 기반 행동 제어 컴포넌트
///</summary>
[RequireComponent( typeof( Rigidbody2D ) )]
public sealed class MonsterObject : MonoBehaviour
{
    private const string WorldItemDropPrefabResourcePath = "Prefabs/Item/WorldItemDropObject";
    private enum eMonsterBehaviorSelectionContext
    {
        NONE,
        ALWAYS,
        ALWAYS_ATTACK,
        PLAYER_DISTANCE,
        PLAYER_DISTANCE_ATTACK
    }

    private enum eMonsterState
    {
        IDLE,
        MOVE,
        ATTACK,
        HIT,
        DIE
    }

    private enum eMonsterCrowdControlType
    {
        NONE,
        STUN,
        KNOCKBACK,
        AIRBORNE
    }

    private const string ContactHitboxObjectName = "ContactHitbox";
    private const string MonsterStatTableResourcePath = "Data/Monster/MonsterStatTableData";
    private const string IdleAnimationStateName = "Idle";
    private const string WalkAnimationStateName = "Walk";
    private const string HitAnimationStateName = "Hit";
    private const string DeadAnimationStateName = "Dead";
    private const float DefaultFacingScaleX = 1.0f;
    private const float DefaultMoveSpeed = 1.0f;
    private const float MoveSpeedStatToUnitsMultiplier = 0.01f;
    private const float WanderMoveSpeedMultiplier = 0.6f;
    private const float TraceMoveSpeedMultiplier = 0.78f;
    private const float TeleportHorizontalOffset = 1.25f;
    private const float PlayerDetectionRetentionSeconds = 2.0f;
    private const float DefaultAttackStateDuration = 0.35f;
    private const float DefaultHitStateDuration = 0.5f;
    private const float DefaultDeathFallbackDuration = 5.0f;
    private const float DefaultDeathReleaseNormalizedTime = 0.7f;
    private const float MinimumCrowdControlDuration = 0.01f;
    private const int RecentSingleHitSkillExecutionCacheSize = 32;

    [SerializeField] private string monsterId = string.Empty;
    [SerializeField] private string monsterName = string.Empty;
    [SerializeField] private long level = 1;
    [SerializeField] private long maxHp = 1;
    [SerializeField] private long currentHp = 1;
    [SerializeField] private long atk;
    [SerializeField] private long def;
    [SerializeField] private long ats;
    [SerializeField] private long mvs;
    [SerializeField] private long expReward;
    [SerializeField] private bool useItemDrop;
    [SerializeField] private List<CMonsterItemDropEntry> itemDropEntryList = new List<CMonsterItemDropEntry>();
    [SerializeField] private bool atAvailable = true;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private BoxCollider2D contactHitboxCollider;
    [SerializeField] private CMonsterBehaviorPatternData behaviorPatternData;
    [SerializeField] private bool isDefaultFacingRight;
    [SerializeField] private bool isBehaviorEnabled = true;

    private static CMonsterStatTableData cachedMonsterStatTableData;
    private static GameObject cachedDefaultWorldItemDropPrefab;
    private static bool isMonsterCollisionIgnored;
    private Transform cachedPlayerTransform;
    private CMonsterBehaviorActionEntry currentActionEntry;
    private eMonsterBehaviorSelectionContext currentSelectionContext = eMonsterBehaviorSelectionContext.NONE;
    private eMonsterState currentState = eMonsterState.IDLE;
    private float currentActionElapsedTime;
    private float currentActionCooldownRemaining;
    private float currentStateElapsedTime;
    private float currentWanderDirection = 1.0f;
    private float defaultScaleX = DefaultFacingScaleX;
    private float desiredHorizontalVelocity;
    private float defReductionDebuffRemaining;
    private float defReductionPercent;
    private float hitStateDuration = DefaultHitStateDuration;
    private float atkReductionDebuffRemaining;
    private float crowdControlRemaining;
    private float crowdControlTotalDuration;
    private float crowdControlAirborneHeight;
    private string mapRuntimePoolKey = string.Empty;
    private string pendingAnimationStateName = string.Empty;
    private Vector3 mapRuntimeSpawnPosition;
    private Vector3 mapRuntimeSpawnRotation;
    private Vector3 mapRuntimeSpawnScale = Vector3.one;
    private Vector3 crowdControlStartPosition;
    private Vector3 crowdControlTargetPosition;
    private float playerDetectionRetentionRemaining;
    private bool hasTeleportedInCurrentAction;
    private bool isConfigured;
    private bool isDeathSequenceStarted;
    private bool hasRewardGranted;
    private Coroutine deathSequenceRoutine;
    private long atkReductionAmount;
    private eMonsterCrowdControlType activeCrowdControlType = eMonsterCrowdControlType.NONE;
    private CSkillPooledVfxHandle crowdControlVfxHandle;
    private readonly int[] recentSingleHitSkillExecutionIdArray = new int[ RecentSingleHitSkillExecutionCacheSize ];
    private int recentSingleHitSkillExecutionWriteIndex;

    ///<summary>
    /// 컴포넌트 초기화
    ///</summary>
    private void Awake()
    {
        CacheDefaultScale();
        CacheAnimator();
        EnsureMonsterLayer();
        EnsureMonsterCollisionIgnored();
        EnsureBodyCollider();
        EnsureContactHitbox();

        if ( targetRigidbody == null )
        {
            Rigidbody2D resolvedRigidbody = GetComponent<Rigidbody2D>();
            targetRigidbody = resolvedRigidbody;
        }

        if ( targetRigidbody != null )
        {
            targetRigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }
    }

    ///<summary>
    /// 초기 데이터 구성
    ///</summary>
    private void Start()
    {
        bool hasMonsterId = string.IsNullOrWhiteSpace( monsterId ) == false;

        if ( hasMonsterId )
        {
            ApplyMonsterStatData( false, currentHp );
            isConfigured = true;
            ChangeState( eMonsterState.IDLE );
            RegisterMonsterInfo();
            return;
        }

        string resolvedMonsterName = gameObject.name;
        ConfigureMonster( resolvedMonsterName, resolvedMonsterName );
    }

    ///<summary>
    /// 활성화 시 UI 등록
    ///</summary>
    private void OnEnable()
    {
        isDeathSequenceStarted = false;
        hasRewardGranted = false;
        ClearSkillDebuffState();
        ClearCrowdControlState();
        SetBodyColliderEnabled( true );
        SetContactHitboxEnabled( true );
        TryPlayPendingAnimationState();
        RegisterMonsterInfo();
    }

    ///<summary>
    /// 비활성화 시 UI 반환
    ///</summary>
    private void OnDisable()
    {
        StopDeathSequence();
        StopHorizontalMovement();
        ClearSkillDebuffState();
        ClearCrowdControlState();
        UnregisterMonsterInfo();
    }

    ///<summary>
    /// 프레임 상태 처리
    ///</summary>
    private void Update()
    {
        if ( isConfigured == false )
        {
            return;
        }

        TickSkillDebuffState();
        TickCrowdControlState();

        if ( currentHp <= 0 && currentState != eMonsterState.DIE )
        {
            ChangeState( eMonsterState.DIE );
        }

        UpdateStateMachine();
    }

    ///<summary>
    /// 물리 이동 속도 적용
    ///</summary>
    private void FixedUpdate()
    {
        if ( IsCrowdControlActive() )
        {
            ApplyZeroVelocity();
            return;
        }

        ApplyDesiredHorizontalVelocity();
    }

    ///<summary>
    /// 몬스터 스탯 데이터 적용
    ///</summary>
    public void ConfigureMonster( string _monsterId, string _monsterName )
    {
        string trimmedMonsterId = string.IsNullOrWhiteSpace( _monsterId ) ? string.Empty : _monsterId.Trim();
        string trimmedMonsterName = string.IsNullOrWhiteSpace( _monsterName ) ? trimmedMonsterId : _monsterName.Trim();
        bool shouldPreserveCurrentHp = isConfigured
            && string.Equals( monsterId, trimmedMonsterId, System.StringComparison.Ordinal )
            && currentHp > 0;
        long previousCurrentHp = currentHp;
        monsterId = trimmedMonsterId;
        monsterName = trimmedMonsterName;
        ApplyMonsterStatData( shouldPreserveCurrentHp, previousCurrentHp );
        ClearBehaviorState();
        isConfigured = true;
        ChangeState( eMonsterState.IDLE );
        RegisterMonsterInfo();
    }

    ///<summary>
    /// 런타임 맵 풀 키 설정
    ///</summary>
    public void SetMapRuntimePoolKey( string _poolKey )
    {
        string resolvedPoolKey = string.IsNullOrWhiteSpace( _poolKey ) ? string.Empty : _poolKey.Trim();
        mapRuntimePoolKey = resolvedPoolKey;
    }

    ///<summary>
    /// 런타임 맵 풀 키 반환
    ///</summary>
    public string GetMapRuntimePoolKey()
    {
        string result = mapRuntimePoolKey;
        return result;
    }

    ///<summary>
    /// 런타임 맵 풀 키 초기화
    ///</summary>
    public void ClearMapRuntimePoolKey()
    {
        mapRuntimePoolKey = string.Empty;
    }

    ///<summary>
    /// 맵 런타임 리스폰 기준점 설정
    ///</summary>
    public void SetMapRuntimeSpawnTransform( Vector3 _spawnPosition, Vector3 _spawnRotation, Vector3 _spawnScale )
    {
        mapRuntimeSpawnPosition = _spawnPosition;
        mapRuntimeSpawnRotation = _spawnRotation;
        mapRuntimeSpawnScale = _spawnScale;
    }

    ///<summary>
    /// 맵 런타임 리스폰 위치 반환
    ///</summary>
    public Vector3 GetMapRuntimeSpawnPosition()
    {
        Vector3 result = mapRuntimeSpawnPosition;
        return result;
    }

    ///<summary>
    /// 맵 런타임 리스폰 회전 반환
    ///</summary>
    public Vector3 GetMapRuntimeSpawnRotation()
    {
        Vector3 result = mapRuntimeSpawnRotation;
        return result;
    }

    ///<summary>
    /// 맵 런타임 리스폰 스케일 반환
    ///</summary>
    public Vector3 GetMapRuntimeSpawnScale()
    {
        Vector3 result = mapRuntimeSpawnScale;
        return result;
    }

    ///<summary>
    /// 몬스터 리스폰 대기 시간 반환
    ///</summary>
    public float GetRespawnDelaySeconds()
    {
        if ( behaviorPatternData == null )
        {
            return 0.0f;
        }

        float result = behaviorPatternData.GetRespawnDelaySeconds();
        return result;
    }

    ///<summary>
    /// 리스폰용 런타임 상태 초기화
    ///</summary>
    public void ResetRuntimeStateForRespawn()
    {
        StopDeathSequence();
        isDeathSequenceStarted = false;
        hasRewardGranted = false;
        ClearSkillDebuffState();
        SetBodyColliderEnabled( true );
        SetContactHitboxEnabled( true );
        StopHorizontalMovement();

        if ( targetRigidbody != null )
        {
            targetRigidbody.linearVelocity = Vector2.zero;
            targetRigidbody.angularVelocity = 0.0f;
        }

        ResetAnimatorRuntimeState();
    }

    ///<summary>
    /// 몬스터 아이디 반환
    ///</summary>
    public string GetMonsterId()
    {
        string result = monsterId;
        return result;
    }

    ///<summary>
    /// 몬스터 이름 반환
    ///</summary>
    public string GetMonsterName()
    {
        string resolvedMonsterName = string.IsNullOrWhiteSpace( monsterName ) ? monsterId : monsterName;
        string result = resolvedMonsterName;
        return result;
    }

    ///<summary>
    /// 레벨 반환
    ///</summary>
    public long GetLevel()
    {
        long result = level;
        return result;
    }

    ///<summary>
    /// 최대 체력 반환
    ///</summary>
    public long GetMaxHp()
    {
        long result = maxHp;
        return result;
    }

    ///<summary>
    /// 현재 체력 반환
    ///</summary>
    public long GetCurrentHp()
    {
        long result = currentHp;
        return result;
    }

    ///<summary>
    /// 공격력 반환
    ///</summary>
    public long GetAtk()
    {
        long reducedAtk = atk;

        if ( atkReductionDebuffRemaining > 0.0f )
        {
            reducedAtk = System.Math.Max( 0L, atk - atkReductionAmount );
        }

        long result = reducedAtk;
        return result;
    }

    ///<summary>
    /// 방어력 반환
    ///</summary>
    public long GetDef()
    {
        long reducedDef = def;

        if ( defReductionDebuffRemaining > 0.0f )
        {
            float debuffedDef = def * ( 1.0f - Mathf.Clamp01( defReductionPercent ) );
            long roundedDefValue = Mathf.RoundToInt( debuffedDef );
            reducedDef = System.Math.Max( 0L, roundedDefValue );
        }

        long result = reducedDef;
        return result;
    }

    ///<summary>
    /// 공격 속도 반환
    ///</summary>
    public long GetAts()
    {
        long result = ats;
        return result;
    }

    ///<summary>
    /// 이동 속도 반환
    ///</summary>
    public long GetMvs()
    {
        long result = mvs;
        return result;
    }

    ///<summary>
    /// 경험치 보상 반환
    ///</summary>
    public long GetExpReward()
    {
        long result = expReward;
        return result;
    }

    ///<summary>
    /// 아이템 드랍 사용 여부 반환
    ///</summary>
    public bool GetUseItemDrop()
    {
        bool result = useItemDrop;
        return result;
    }

    ///<summary>
    /// 아이템 드랍 목록 반환
    ///</summary>
    public List<CMonsterItemDropEntry> GetItemDropEntryList()
    {
        List<CMonsterItemDropEntry> result = itemDropEntryList;
        return result;
    }

    ///<summary>
    /// 몬스터 보상 지급 시도
    ///</summary>
    public bool TryGrantReward( PlayerController _playerController )
    {
        if ( _playerController == null || currentHp > 0 || hasRewardGranted )
        {
            return false;
        }

        CPlayerStatManager playerStatManager = _playerController.GetPlayerStatManager();

        hasRewardGranted = true;

        if ( playerStatManager != null && expReward > 0 )
        {
            float expRewardMultiplier = playerStatManager.GetExpGainMultiplier();
            float scaledExpReward = expReward * expRewardMultiplier;
            playerStatManager.AddExp( scaledExpReward );
        }

        CQuestManager questManager = _playerController.GetQuestManager();

        if ( questManager != null )
        {
            string resolvedMonsterId = GetMonsterId();
            questManager.NotifyMonsterKilled( resolvedMonsterId );
        }

        SpawnDropItems();
        return true;
    }

    ///<summary>
    /// 직접 공격 가능 여부 반환
    ///</summary>
    public bool IsAttackAvailable()
    {
        bool result = atAvailable;
        return result;
    }

    ///<summary>
    /// 몬스터 행동 패턴 에셋 반환
    ///</summary>
    public CMonsterBehaviorPatternData GetBehaviorPatternData()
    {
        CMonsterBehaviorPatternData result = behaviorPatternData;
        return result;
    }

    ///<summary>
    /// 행동 패턴 활성 여부 설정
    ///</summary>
    public void SetBehaviorEnabled( bool _isBehaviorEnabled )
    {
        isBehaviorEnabled = _isBehaviorEnabled;

        if ( isBehaviorEnabled == false )
        {
            ClearBehaviorState();
            ChangeState( eMonsterState.IDLE );
            return;
        }

        ChangeState( eMonsterState.IDLE );
    }

    ///<summary>
    /// 몬스터 본체 콜라이더 반환
    ///</summary>
    public Collider2D GetBodyCollider()
    {
        Collider2D result = bodyCollider;
        return result;
    }

    ///<summary>
    /// 몬스터 정보 표시 월드 위치 반환
    ///</summary>
    public Vector3 GetMonsterInfoWorldPosition()
    {
        if ( bodyCollider == null )
        {
            Vector3 fallbackPosition = transform.position;
            return fallbackPosition;
        }

        Bounds colliderBounds = bodyCollider.bounds;
        Vector3 result = new Vector3( colliderBounds.center.x, colliderBounds.max.y, colliderBounds.center.z );
        return result;
    }

    ///<summary>
    /// 현재 체력 갱신
    ///</summary>
    public void SetCurrentHp( long _newCurrentHp )
    {
        long previousCurrentHp = currentHp;
        long clampedCurrentHp = _newCurrentHp;

        if ( clampedCurrentHp < 0 )
        {
            clampedCurrentHp = 0;
        }

        if ( clampedCurrentHp > maxHp )
        {
            clampedCurrentHp = maxHp;
        }

        currentHp = clampedCurrentHp;

        if ( currentHp < previousCurrentHp )
        {
            MarkPlayerDetected();
        }

        if ( currentHp <= 0 )
        {
            ChangeState( eMonsterState.DIE );
        }

        RefreshMonsterInfo();
    }

    ///<summary>
    /// 몬스터 피해 적용
    ///</summary>
    public void TakeDamage( long _damage, bool _isCritical = false )
    {
        if ( currentState == eMonsterState.DIE )
        {
            return;
        }

        long appliedDamage = _damage;

        if ( appliedDamage < 0 )
        {
            appliedDamage = 0;
        }

        EnsurePlayerTransform();
        ApplyFacingDirectionTowardPlayer();

        long previousCurrentHp = currentHp;
        long nextHp = currentHp - appliedDamage;
        SetCurrentHp( nextHp );

        if ( appliedDamage > 0 && CDamageFontManager.TryGetInstance( out CDamageFontManager damageFontManager ) )
        {
            damageFontManager.ShowMonsterDamage( this, appliedDamage, _isCritical );
        }

        if ( currentHp <= 0 )
        {
            return;
        }

        if ( currentState == eMonsterState.HIT )
        {
            RestartHitState();
            return;
        }

        ChangeState( eMonsterState.HIT );
    }

    ///<summary>
    /// 기절 군중제어 적용
    ///</summary>
    public void ApplyStunCrowdControl( float _durationSeconds, GameObject _vfxPrefab, Vector3 _vfxOffset )
    {
        float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );

        if ( resolvedDurationSeconds <= 0.0f || currentState == eMonsterState.DIE )
        {
            return;
        }

        if ( activeCrowdControlType == eMonsterCrowdControlType.STUN && crowdControlRemaining >= resolvedDurationSeconds )
        {
            return;
        }

        BeginCrowdControl( eMonsterCrowdControlType.STUN, resolvedDurationSeconds, _vfxPrefab, _vfxOffset );
    }

    ///<summary>
    /// 넉백 군중제어 적용
    ///</summary>
    public void ApplyKnockbackCrowdControl( float _durationSeconds, float _distance, Vector3 _sourceWorldPosition, GameObject _vfxPrefab, Vector3 _vfxOffset )
    {
        float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );
        float resolvedDistance = Mathf.Max( 0.0f, _distance );

        if ( resolvedDurationSeconds <= 0.0f || resolvedDistance <= 0.0f || currentState == eMonsterState.DIE )
        {
            return;
        }

        BeginCrowdControl( eMonsterCrowdControlType.KNOCKBACK, resolvedDurationSeconds, _vfxPrefab, _vfxOffset );
        float knockbackDirection = ResolveCrowdControlKnockbackDirection( _sourceWorldPosition );
        crowdControlTargetPosition = crowdControlStartPosition + new Vector3( knockbackDirection * resolvedDistance, 0.0f, 0.0f );
    }

    ///<summary>
    /// 에어본 군중제어 적용
    ///</summary>
    public void ApplyAirborneCrowdControl( float _durationSeconds, float _height, GameObject _vfxPrefab, Vector3 _vfxOffset )
    {
        float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );
        float resolvedHeight = Mathf.Max( 0.0f, _height );

        if ( resolvedDurationSeconds <= 0.0f || resolvedHeight <= 0.0f || currentState == eMonsterState.DIE )
        {
            return;
        }

        if ( activeCrowdControlType == eMonsterCrowdControlType.AIRBORNE && IsCrowdControlActive() )
        {
            return;
        }

        BeginCrowdControl( eMonsterCrowdControlType.AIRBORNE, resolvedDurationSeconds, _vfxPrefab, _vfxOffset );
        crowdControlAirborneHeight = resolvedHeight;
    }

    ///<summary>
    /// 단발 스킬 피격 등록 시도
    ///</summary>
    public bool TryRegisterSingleHitSkillExecution( int _executionId )
    {
        if ( _executionId <= 0 )
        {
            return true;
        }

        for ( int index = 0; index < recentSingleHitSkillExecutionIdArray.Length; index++ )
        {
            int cachedExecutionId = recentSingleHitSkillExecutionIdArray[ index ];

            if ( cachedExecutionId != _executionId )
            {
                continue;
            }

            return false;
        }

        recentSingleHitSkillExecutionIdArray[ recentSingleHitSkillExecutionWriteIndex ] = _executionId;
        recentSingleHitSkillExecutionWriteIndex++;

        if ( recentSingleHitSkillExecutionWriteIndex >= recentSingleHitSkillExecutionIdArray.Length )
        {
            recentSingleHitSkillExecutionWriteIndex = 0;
        }

        return true;
    }

    ///<summary>
    /// 몬스터 정보 UI 새로고침 요청
    ///</summary>
    ///<summary>
    /// 방어력 감소 디버프 적용
    ///</summary>
    public void ApplyDefReductionDebuff( float _reductionPercent, float _durationSeconds )
    {
        float resolvedReductionPercent = Mathf.Clamp01( _reductionPercent );
        float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );

        if ( resolvedReductionPercent <= 0.0f || resolvedDurationSeconds <= 0.0f )
        {
            return;
        }

        defReductionPercent = Mathf.Max( defReductionPercent, resolvedReductionPercent );
        defReductionDebuffRemaining = Mathf.Max( defReductionDebuffRemaining, resolvedDurationSeconds );
    }

    ///<summary>
    /// 공격력 감소 디버프 적용
    ///</summary>
    public void ApplyAtkReductionDebuff( long _reductionAmount, float _durationSeconds )
    {
        long resolvedReductionAmount = _reductionAmount > 0 ? _reductionAmount : 0;
        float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );

        if ( resolvedReductionAmount <= 0 || resolvedDurationSeconds <= 0.0f )
        {
            return;
        }

        atkReductionAmount = System.Math.Max( atkReductionAmount, resolvedReductionAmount );
        atkReductionDebuffRemaining = Mathf.Max( atkReductionDebuffRemaining, resolvedDurationSeconds );
    }

    ///<summary>
    /// 몬스터 정보 UI 새로고침 요청
    ///</summary>
    public void RefreshMonsterInfo()
    {
        bool hasManager = CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager );

        if ( hasManager == false )
        {
            return;
        }

        monsterInfoManager.RefreshMonsterInfo( this );
    }

    ///<summary>
    /// 루트 스케일 기준값 캐시
    ///</summary>
    private void CacheDefaultScale()
    {
        float localScaleX = transform.localScale.x;
        float resolvedScaleX = Mathf.Abs( localScaleX );

        if ( resolvedScaleX <= 0.0f )
        {
            resolvedScaleX = DefaultFacingScaleX;
        }

        if ( Mathf.Approximately( localScaleX, 0.0f ) == false )
        {
            isDefaultFacingRight = localScaleX < 0.0f;
        }

        defaultScaleX = resolvedScaleX;
    }

    ///<summary>
    /// 애니메이터 참조 캐시
    ///</summary>
    private void CacheAnimator()
    {
        if ( targetAnimator != null )
        {
            return;
        }

        Animator resolvedAnimator = GetComponentInChildren<Animator>( true );
        targetAnimator = resolvedAnimator;
    }

    ///<summary>
    /// 상태 머신 갱신
    ///</summary>
    ///<summary>
    /// 스킬 디버프 지속시간 갱신
    ///</summary>
    private void TickSkillDebuffState()
    {
        float deltaTime = Time.deltaTime;

        if ( defReductionDebuffRemaining > 0.0f )
        {
            defReductionDebuffRemaining = Mathf.Max( 0.0f, defReductionDebuffRemaining - deltaTime );

            if ( defReductionDebuffRemaining <= 0.0f )
            {
                defReductionPercent = 0.0f;
            }
        }

        if ( atkReductionDebuffRemaining > 0.0f )
        {
            atkReductionDebuffRemaining = Mathf.Max( 0.0f, atkReductionDebuffRemaining - deltaTime );

            if ( atkReductionDebuffRemaining <= 0.0f )
            {
                atkReductionAmount = 0;
            }
        }
    }

    ///<summary>
    /// 스킬 디버프 상태 초기화
    ///</summary>
    private void ClearSkillDebuffState()
    {
        defReductionDebuffRemaining = 0.0f;
        defReductionPercent = 0.0f;
        atkReductionDebuffRemaining = 0.0f;
        atkReductionAmount = 0;
    }

    ///<summary>
    /// 군중제어 상태 갱신
    ///</summary>
    private void TickCrowdControlState()
    {
        if ( IsCrowdControlActive() == false )
        {
            return;
        }

        if ( currentState == eMonsterState.DIE )
        {
            ClearCrowdControlState();
            return;
        }

        crowdControlRemaining -= Time.deltaTime;

        if ( crowdControlRemaining < 0.0f )
        {
            crowdControlRemaining = 0.0f;
        }

        UpdateCrowdControlMotion();

        if ( crowdControlRemaining > 0.0f )
        {
            return;
        }

        CompleteCrowdControl();
    }

    ///<summary>
    /// 군중제어 상태 시작
    ///</summary>
    private void BeginCrowdControl( eMonsterCrowdControlType _crowdControlType, float _durationSeconds, GameObject _vfxPrefab, Vector3 _vfxOffset )
    {
        float resolvedDurationSeconds = Mathf.Max( MinimumCrowdControlDuration, _durationSeconds );
        activeCrowdControlType = _crowdControlType;
        crowdControlRemaining = resolvedDurationSeconds;
        crowdControlTotalDuration = resolvedDurationSeconds;
        crowdControlAirborneHeight = 0.0f;
        crowdControlStartPosition = transform.position;
        crowdControlTargetPosition = crowdControlStartPosition;
        InterruptBehaviorForCrowdControl();
        EnterCrowdControlHitState();
        ApplyZeroVelocity();
        RefreshCrowdControlVfx( _vfxPrefab, _vfxOffset, resolvedDurationSeconds );
    }

    ///<summary>
    /// 군중제어 동작 종료
    ///</summary>
    private void CompleteCrowdControl()
    {
        ClearCrowdControlState();

        if ( currentHp <= 0 || currentState != eMonsterState.HIT )
        {
            return;
        }

        ChangeState( eMonsterState.IDLE );
    }

    ///<summary>
    /// 군중제어 상태 초기화
    ///</summary>
    private void ClearCrowdControlState()
    {
        activeCrowdControlType = eMonsterCrowdControlType.NONE;
        crowdControlRemaining = 0.0f;
        crowdControlTotalDuration = 0.0f;
        crowdControlAirborneHeight = 0.0f;
        crowdControlStartPosition = Vector3.zero;
        crowdControlTargetPosition = Vector3.zero;
        ReturnCrowdControlVfx();
    }

    ///<summary>
    /// 군중제어 활성 여부 반환
    ///</summary>
    private bool IsCrowdControlActive()
    {
        bool result = activeCrowdControlType != eMonsterCrowdControlType.NONE && crowdControlRemaining > 0.0f;
        return result;
    }

    ///<summary>
    /// 군중제어 동작 갱신
    ///</summary>
    private void UpdateCrowdControlMotion()
    {
        StopHorizontalMovement();
        ApplyZeroVelocity();

        switch ( activeCrowdControlType )
        {
            case eMonsterCrowdControlType.STUN:
                ApplyCrowdControlPosition( crowdControlStartPosition );
                break;

            case eMonsterCrowdControlType.KNOCKBACK:
                ApplyKnockbackCrowdControlMotion();
                break;

            case eMonsterCrowdControlType.AIRBORNE:
                ApplyAirborneCrowdControlMotion();
                break;
        }
    }

    ///<summary>
    /// 넉백 군중제어 위치 갱신
    ///</summary>
    private void ApplyKnockbackCrowdControlMotion()
    {
        float progress = ResolveCrowdControlProgress();
        Vector3 nextPosition = Vector3.Lerp( crowdControlStartPosition, crowdControlTargetPosition, progress );
        ApplyCrowdControlPosition( nextPosition );
    }

    ///<summary>
    /// 에어본 군중제어 위치 갱신
    ///</summary>
    private void ApplyAirborneCrowdControlMotion()
    {
        float progress = ResolveCrowdControlProgress();
        float verticalOffset = Mathf.Sin( progress * Mathf.PI ) * crowdControlAirborneHeight;
        Vector3 nextPosition = crowdControlStartPosition + new Vector3( 0.0f, verticalOffset, 0.0f );
        ApplyCrowdControlPosition( nextPosition );
    }

    ///<summary>
    /// 군중제어 진행률 계산
    ///</summary>
    private float ResolveCrowdControlProgress()
    {
        if ( crowdControlTotalDuration <= 0.0f )
        {
            return 1.0f;
        }

        float progress = 1.0f - ( crowdControlRemaining / crowdControlTotalDuration );
        float result = Mathf.Clamp01( progress );
        return result;
    }

    ///<summary>
    /// 군중제어 위치 반영
    ///</summary>
    private void ApplyCrowdControlPosition( Vector3 _worldPosition )
    {
        if ( targetRigidbody != null )
        {
            targetRigidbody.position = _worldPosition;
        }

        transform.position = _worldPosition;
    }

    ///<summary>
    /// 군중제어 넉백 방향 계산
    ///</summary>
    private float ResolveCrowdControlKnockbackDirection( Vector3 _sourceWorldPosition )
    {
        float direction = transform.position.x - _sourceWorldPosition.x;

        if ( Mathf.Approximately( direction, 0.0f ) )
        {
            bool isFacingRight = ResolveFacingRight();
            float fallbackDirection = isFacingRight ? 1.0f : -1.0f;
            return fallbackDirection;
        }

        float result = direction > 0.0f ? 1.0f : -1.0f;
        return result;
    }

    ///<summary>
    /// 군중제어 피격 상태 진입
    ///</summary>
    private void EnterCrowdControlHitState()
    {
        if ( currentState == eMonsterState.DIE )
        {
            return;
        }

        if ( currentState == eMonsterState.HIT )
        {
            RestartHitState();
            return;
        }

        ChangeState( eMonsterState.HIT );
    }

    ///<summary>
    /// 군중제어용 행동 중단
    ///</summary>
    private void InterruptBehaviorForCrowdControl()
    {
        FinishCurrentAction();
        StopHorizontalMovement();
    }

    ///<summary>
    /// 군중제어 이펙트 갱신
    ///</summary>
    private void RefreshCrowdControlVfx( GameObject _vfxPrefab, Vector3 _vfxOffset, float _durationSeconds )
    {
        ReturnCrowdControlVfx();

        if ( _vfxPrefab == null )
        {
            return;
        }

        Vector3 spawnWorldPosition = transform.position + _vfxOffset;
        crowdControlVfxHandle = CSkillVfxPoolManager.Spawn( _vfxPrefab, spawnWorldPosition, transform, _durationSeconds );
    }

    ///<summary>
    /// 군중제어 이펙트 반환
    ///</summary>
    private void ReturnCrowdControlVfx()
    {
        if ( crowdControlVfxHandle == null )
        {
            return;
        }

        crowdControlVfxHandle.ForceReturn();
        crowdControlVfxHandle = null;
    }

    ///<summary>
    /// 강제 정지 속도 반영
    ///</summary>
    private void ApplyZeroVelocity()
    {
        if ( targetRigidbody == null )
        {
            return;
        }

        targetRigidbody.linearVelocity = Vector2.zero;
        targetRigidbody.angularVelocity = 0.0f;
    }

    ///<summary>
    /// 상태 머신 갱신
    ///</summary>
    private void UpdateStateMachine()
    {
        currentStateElapsedTime += Time.deltaTime;

        switch ( currentState )
        {
            case eMonsterState.IDLE:
                UpdateIdleState();
                break;

            case eMonsterState.MOVE:
                UpdateMoveState();
                break;

            case eMonsterState.ATTACK:
                UpdateAttackState();
                break;

            case eMonsterState.HIT:
                UpdateHitState();
                break;

            case eMonsterState.DIE:
                UpdateDieState();
                break;
        }
    }

    ///<summary>
    /// 상태 전환 처리
    ///</summary>
    private void ChangeState( eMonsterState _nextState )
    {
        if ( currentState == _nextState )
        {
            return;
        }

        currentState = _nextState;
        currentStateElapsedTime = 0.0f;

        switch ( currentState )
        {
            case eMonsterState.IDLE:
                EnterIdleState();
                break;

            case eMonsterState.MOVE:
                EnterMoveState();
                break;

            case eMonsterState.ATTACK:
                EnterAttackState();
                break;

            case eMonsterState.HIT:
                EnterHitState();
                break;

            case eMonsterState.DIE:
                EnterDieState();
                break;
        }
    }

    ///<summary>
    /// 대기 상태 진입 처리
    ///</summary>
    private void EnterIdleState()
    {
        StopHorizontalMovement();
        PlayAnimationState( IdleAnimationStateName );
    }

    ///<summary>
    /// 이동 상태 진입 처리
    ///</summary>
    private void EnterMoveState()
    {
        PlayAnimationState( WalkAnimationStateName );
    }

    ///<summary>
    /// 공격 상태 진입 처리
    ///</summary>
    private void EnterAttackState()
    {
        StopHorizontalMovement();
        PlayAnimationState( IdleAnimationStateName );
    }

    ///<summary>
    /// 피격 상태 진입 처리
    ///</summary>
    private void EnterHitState()
    {
        StopHorizontalMovement();
        ApplyFacingDirectionTowardPlayer();
        PlayAnimationStateFromStart( HitAnimationStateName );
    }

    ///<summary>
    /// 피격 상태 재시작 처리
    ///</summary>
    private void RestartHitState()
    {
        currentStateElapsedTime = 0.0f;
        StopHorizontalMovement();
        ApplyFacingDirectionTowardPlayer();
        PlayAnimationStateFromStart( HitAnimationStateName );
    }

    ///<summary>
    /// 사망 상태 진입 처리
    ///</summary>
    private void EnterDieState()
    {
        ClearCrowdControlState();
        StopHorizontalMovement();
        SetContactHitboxEnabled( false );

        if ( targetRigidbody != null )
        {
            targetRigidbody.linearVelocity = Vector2.zero;
            targetRigidbody.angularVelocity = 0.0f;
        }

        PlayAnimationState( DeadAnimationStateName );
        StartDeathSequence();
    }

    ///<summary>
    /// 대기 상태 갱신
    ///</summary>
    private void UpdateIdleState()
    {
        UpdateBehaviorPlan();

        if ( currentState != eMonsterState.IDLE )
        {
            return;
        }

        StopHorizontalMovement();
    }

    ///<summary>
    /// 이동 상태 갱신
    ///</summary>
    private void UpdateMoveState()
    {
        UpdateBehaviorPlan();
    }

    ///<summary>
    /// 공격 상태 갱신
    ///</summary>
    private void UpdateAttackState()
    {
        UpdateBehaviorPlan();

        if ( currentState != eMonsterState.ATTACK )
        {
            return;
        }

        float attackStateDuration = ResolveAttackStateDuration();

        if ( currentStateElapsedTime < attackStateDuration )
        {
            return;
        }

        FinishCurrentAction();
        ChangeState( eMonsterState.IDLE );
    }

    ///<summary>
    /// 피격 상태 갱신
    ///</summary>
    private void UpdateHitState()
    {
        if ( IsCrowdControlActive() )
        {
            StopHorizontalMovement();
            return;
        }

        if ( currentStateElapsedTime < hitStateDuration )
        {
            return;
        }

        ChangeState( eMonsterState.IDLE );
    }

    ///<summary>
    /// 사망 상태 갱신
    ///</summary>
    private void UpdateDieState()
    {
        StopHorizontalMovement();
    }

    ///<summary>
    /// 행동 패턴 계획 갱신
    ///</summary>
    private void UpdateBehaviorPlan()
    {
        if ( isBehaviorEnabled == false )
        {
            if ( currentState != eMonsterState.IDLE && currentState != eMonsterState.DIE )
            {
                ChangeState( eMonsterState.IDLE );
            }

            StopHorizontalMovement();
            return;
        }

        if ( behaviorPatternData == null )
        {
            if ( currentState != eMonsterState.DIE )
            {
                ChangeState( eMonsterState.IDLE );
            }

            return;
        }

        EnsurePlayerTransform();
        UpdatePlayerDetectionRetention();
        bool hasActionSet = ResolveActiveActionSet( out List<CMonsterBehaviorActionEntry> actionEntryList, out eMonsterBehaviorSelectionContext nextSelectionContext );

        if ( hasActionSet == false )
        {
            if ( currentState != eMonsterState.DIE )
            {
                ChangeState( eMonsterState.IDLE );
            }

            return;
        }

        if ( currentSelectionContext != nextSelectionContext )
        {
            ResetCurrentAction( nextSelectionContext );
        }

        if ( currentActionEntry == null )
        {
            UpdateActionCooldown();

            if ( currentActionCooldownRemaining > 0.0f )
            {
                if ( currentState != eMonsterState.IDLE )
                {
                    ChangeState( eMonsterState.IDLE );
                }

                return;
            }

            bool didBeginAction = TryBeginNextAction( actionEntryList, nextSelectionContext );

            if ( didBeginAction == false )
            {
                ChangeState( eMonsterState.IDLE );
                return;
            }
        }

        currentActionElapsedTime += Time.deltaTime;
        eMonsterState nextState = ResolveStateFromCurrentAction();

        if ( currentState != nextState )
        {
            ChangeState( nextState );
        }

        ExecuteCurrentAction();

        if ( currentState == eMonsterState.ATTACK )
        {
            return;
        }

        if ( currentActionElapsedTime >= currentActionEntry.GetDurationSeconds() )
        {
            FinishCurrentAction();
            ChangeState( eMonsterState.IDLE );
        }
    }

    ///<summary>
    /// 현재 행동 기준 상태 결정
    ///</summary>
    private eMonsterState ResolveStateFromCurrentAction()
    {
        if ( currentActionEntry == null )
        {
            return eMonsterState.IDLE;
        }

        eMonsterBehaviorAction actionType = currentActionEntry.GetActionType();

        switch ( actionType )
        {
            case eMonsterBehaviorAction.IDLE:
            case eMonsterBehaviorAction.LOOK_PLAYER:
            case eMonsterBehaviorAction.TELEPORT_TO_PLAYER:
                return eMonsterState.IDLE;

            case eMonsterBehaviorAction.WANDER:
            case eMonsterBehaviorAction.TRACE_PLAYER:
                return eMonsterState.MOVE;

            case eMonsterBehaviorAction.ATTACK:
            case eMonsterBehaviorAction.SKILL:
                return eMonsterState.ATTACK;
        }

        return eMonsterState.IDLE;
    }

    ///<summary>
    /// 플레이어 참조 확보
    ///</summary>
    private void EnsurePlayerTransform()
    {
        if ( cachedPlayerTransform != null && cachedPlayerTransform.gameObject.activeInHierarchy )
        {
            return;
        }

        PlayerController playerController = FindAnyObjectByType<PlayerController>();

        if ( playerController == null )
        {
            cachedPlayerTransform = null;
            return;
        }

        cachedPlayerTransform = playerController.transform;
    }

    ///<summary>
    /// 현재 적용 행동 세트 결정
    ///</summary>
    private bool ResolveActiveActionSet( out List<CMonsterBehaviorActionEntry> _actionEntryList, out eMonsterBehaviorSelectionContext _selectionContext )
    {
        CMonsterPlayerDistancePatternData playerDistancePatternData = behaviorPatternData.GetPlayerDistancePatternData();
        float distanceToPlayer = ResolveDistanceToPlayer();
        bool isPlayerNear = cachedPlayerTransform != null && distanceToPlayer <= playerDistancePatternData.GetPlayerDistance();
        bool isFacingPlayer = IsFacingPlayer();
        bool isPlayerDetected = IsPlayerDetectionRetained() || ( isFacingPlayer && isPlayerNear );

        if ( isPlayerDetected )
        {
            CMonsterAttackPatternData playerDistanceAttackPatternData = playerDistancePatternData.GetAttackPatternData();
            bool canUsePlayerDistanceAttack = CanUseAttackPattern( playerDistanceAttackPatternData, distanceToPlayer );

            if ( canUsePlayerDistanceAttack )
            {
                _actionEntryList = playerDistanceAttackPatternData.GetActionEntryList();
                _selectionContext = eMonsterBehaviorSelectionContext.PLAYER_DISTANCE_ATTACK;
                return true;
            }

            _actionEntryList = playerDistancePatternData.GetActionEntryList();
            _selectionContext = eMonsterBehaviorSelectionContext.PLAYER_DISTANCE;
            return _actionEntryList != null && _actionEntryList.Count > 0;
        }

        CMonsterAlwaysPatternData alwaysPatternData = behaviorPatternData.GetAlwaysPatternData();
        CMonsterAttackPatternData alwaysAttackPatternData = alwaysPatternData.GetAttackPatternData();
        bool canUseAlwaysAttack = CanUseAttackPattern( alwaysAttackPatternData, distanceToPlayer );

        if ( canUseAlwaysAttack )
        {
            _actionEntryList = alwaysAttackPatternData.GetActionEntryList();
            _selectionContext = eMonsterBehaviorSelectionContext.ALWAYS_ATTACK;
            return true;
        }

        _actionEntryList = alwaysPatternData.GetActionEntryList();
        _selectionContext = eMonsterBehaviorSelectionContext.ALWAYS;
        return _actionEntryList != null && _actionEntryList.Count > 0;
    }

    ///<summary>
    /// 플레이어 감지 유지 시간 갱신
    ///</summary>
    private void UpdatePlayerDetectionRetention()
    {
        if ( playerDetectionRetentionRemaining <= 0.0f )
        {
            playerDetectionRetentionRemaining = 0.0f;
            return;
        }

        playerDetectionRetentionRemaining -= Time.deltaTime;

        if ( playerDetectionRetentionRemaining < 0.0f )
        {
            playerDetectionRetentionRemaining = 0.0f;
        }
    }

    ///<summary>
    /// 플레이어 감지 유지 상태 반환
    ///</summary>
    private bool IsPlayerDetectionRetained()
    {
        bool result = playerDetectionRetentionRemaining > 0.0f;
        return result;
    }

    ///<summary>
    /// 플레이어 감지 유지 시작
    ///</summary>
    private void MarkPlayerDetected()
    {
        playerDetectionRetentionRemaining = PlayerDetectionRetentionSeconds;
    }

    ///<summary>
    /// 공격 패턴 사용 가능 여부 반환
    ///</summary>
    private bool CanUseAttackPattern( CMonsterAttackPatternData _attackPatternData, float _distanceToPlayer )
    {
        if ( atAvailable == false )
        {
            return false;
        }

        if ( cachedPlayerTransform == null || _attackPatternData == null )
        {
            return false;
        }

        if ( _attackPatternData.GetUseAttackPattern() == false )
        {
            return false;
        }

        if ( _attackPatternData.GetActionEntryList() == null || _attackPatternData.GetActionEntryList().Count == 0 )
        {
            return false;
        }

        bool isWithinAttackDistance = _distanceToPlayer <= _attackPatternData.GetAttackDistance();
        return isWithinAttackDistance;
    }

    ///<summary>
    /// 플레이어 거리 계산
    ///</summary>
    private float ResolveDistanceToPlayer()
    {
        if ( cachedPlayerTransform == null )
        {
            return float.MaxValue;
        }

        Vector3 playerPosition = cachedPlayerTransform.position;
        Vector3 currentPosition = transform.position;
        float result = Vector3.Distance( currentPosition, playerPosition );
        return result;
    }

    ///<summary>
    /// 플레이어 정면 바라보기 상태 반환
    ///</summary>
    private bool IsFacingPlayer()
    {
        if ( cachedPlayerTransform == null )
        {
            return false;
        }

        float horizontalDelta = cachedPlayerTransform.position.x - transform.position.x;

        if ( Mathf.Approximately( horizontalDelta, 0.0f ) )
        {
            return false;
        }

        bool isPlayerOnRight = horizontalDelta > 0.0f;
        bool isFacingRight = ResolveFacingRight();
        bool result = isPlayerOnRight == isFacingRight;
        return result;
    }

    ///<summary>
    /// 행동 선택 쿨타임 갱신
    ///</summary>
    private void UpdateActionCooldown()
    {
        if ( currentActionCooldownRemaining <= 0.0f )
        {
            currentActionCooldownRemaining = 0.0f;
            return;
        }

        currentActionCooldownRemaining -= Time.deltaTime;

        if ( currentActionCooldownRemaining < 0.0f )
        {
            currentActionCooldownRemaining = 0.0f;
        }
    }

    ///<summary>
    /// 다음 행동 시작 시도
    ///</summary>
    private bool TryBeginNextAction( List<CMonsterBehaviorActionEntry> _actionEntryList, eMonsterBehaviorSelectionContext _selectionContext )
    {
        CMonsterBehaviorActionEntry nextActionEntry = SelectWeightedActionEntry( _actionEntryList );

        if ( nextActionEntry == null )
        {
            return false;
        }

        BeginAction( nextActionEntry, _selectionContext );
        return true;
    }

    ///<summary>
    /// 가중치 기반 행동 엔트리 선택
    ///</summary>
    private CMonsterBehaviorActionEntry SelectWeightedActionEntry( List<CMonsterBehaviorActionEntry> _actionEntryList )
    {
        if ( _actionEntryList == null || _actionEntryList.Count == 0 )
        {
            return null;
        }

        float totalWeight = 0.0f;

        for ( int index = 0; index < _actionEntryList.Count; index++ )
        {
            CMonsterBehaviorActionEntry entryData = _actionEntryList[ index ];

            if ( entryData == null )
            {
                continue;
            }

            totalWeight += Mathf.Max( 0.0f, entryData.GetWeight() );
        }

        if ( totalWeight <= 0.0f )
        {
            return null;
        }

        float randomWeight = Random.Range( 0.0f, totalWeight );
        float currentWeight = 0.0f;

        for ( int index = 0; index < _actionEntryList.Count; index++ )
        {
            CMonsterBehaviorActionEntry entryData = _actionEntryList[ index ];

            if ( entryData == null )
            {
                continue;
            }

            currentWeight += Mathf.Max( 0.0f, entryData.GetWeight() );

            if ( randomWeight <= currentWeight )
            {
                return entryData;
            }
        }

        int lastIndex = _actionEntryList.Count - 1;
        CMonsterBehaviorActionEntry fallbackEntry = _actionEntryList[ lastIndex ];
        return fallbackEntry;
    }

    ///<summary>
    /// 행동 시작 처리
    ///</summary>
    private void BeginAction( CMonsterBehaviorActionEntry _entryData, eMonsterBehaviorSelectionContext _selectionContext )
    {
        currentActionEntry = _entryData;
        currentSelectionContext = _selectionContext;
        currentActionElapsedTime = 0.0f;
        hasTeleportedInCurrentAction = false;

        if ( currentActionEntry.GetActionType() == eMonsterBehaviorAction.WANDER )
        {
            currentWanderDirection = GetRandomHorizontalDirection();
        }
    }

    ///<summary>
    /// 현재 행동 수행 처리
    ///</summary>
    private void ExecuteCurrentAction()
    {
        if ( currentActionEntry == null )
        {
            StopHorizontalMovement();
            return;
        }

        eMonsterBehaviorAction actionType = currentActionEntry.GetActionType();

        switch ( actionType )
        {
            case eMonsterBehaviorAction.IDLE:
                ExecuteIdleAction();
                break;

            case eMonsterBehaviorAction.WANDER:
                ExecuteWanderAction();
                break;

            case eMonsterBehaviorAction.TELEPORT_TO_PLAYER:
                ExecuteTeleportToPlayerAction();
                break;

            case eMonsterBehaviorAction.TRACE_PLAYER:
                ExecuteTracePlayerAction();
                break;

            case eMonsterBehaviorAction.LOOK_PLAYER:
                ExecuteLookPlayerAction();
                break;

            case eMonsterBehaviorAction.ATTACK:
                ExecuteAttackAction();
                break;

            case eMonsterBehaviorAction.SKILL:
                ExecuteSkillAction();
                break;
        }
    }

    ///<summary>
    /// 대기 행동 처리
    ///</summary>
    private void ExecuteIdleAction()
    {
        StopHorizontalMovement();
    }

    ///<summary>
    /// 배회 행동 처리
    ///</summary>
    private void ExecuteWanderAction()
    {
        float moveSpeed = ResolveMoveSpeed() * WanderMoveSpeedMultiplier;
        SetHorizontalMovementDirection( currentWanderDirection, moveSpeed );
    }

    ///<summary>
    /// 플레이어 순간이동 행동 처리
    ///</summary>
    private void ExecuteTeleportToPlayerAction()
    {
        StopHorizontalMovement();

        if ( hasTeleportedInCurrentAction )
        {
            return;
        }

        hasTeleportedInCurrentAction = true;
        TryTeleportNearPlayer();
    }

    ///<summary>
    /// 플레이어 추적 행동 처리
    ///</summary>
    private void ExecuteTracePlayerAction()
    {
        if ( cachedPlayerTransform == null )
        {
            StopHorizontalMovement();
            return;
        }

        float horizontalDelta = cachedPlayerTransform.position.x - transform.position.x;

        if ( Mathf.Approximately( horizontalDelta, 0.0f ) )
        {
            StopHorizontalMovement();
            return;
        }

        float moveSpeed = ResolveMoveSpeed() * TraceMoveSpeedMultiplier;
        float horizontalDirection = Mathf.Sign( horizontalDelta );
        SetHorizontalMovementDirection( horizontalDirection, moveSpeed );
    }

    ///<summary>
    /// 플레이어 바라보기 행동 처리
    ///</summary>
    private void ExecuteLookPlayerAction()
    {
        StopHorizontalMovement();
        ApplyFacingDirectionTowardPlayer();
    }

    ///<summary>
    /// 공격 행동 처리
    ///</summary>
    private void ExecuteAttackAction()
    {
        StopHorizontalMovement();
        ApplyFacingDirectionTowardPlayer();
    }

    ///<summary>
    /// 스킬 행동 처리
    ///</summary>
    private void ExecuteSkillAction()
    {
        StopHorizontalMovement();
        ApplyFacingDirectionTowardPlayer();
    }

    ///<summary>
    /// 현재 행동 종료 처리
    ///</summary>
    private void FinishCurrentAction()
    {
        if ( currentActionEntry != null )
        {
            currentActionCooldownRemaining = Mathf.Max( 0.0f, currentActionEntry.GetCooldownSeconds() );
        }

        currentActionEntry = null;
        currentActionElapsedTime = 0.0f;
        hasTeleportedInCurrentAction = false;
    }

    ///<summary>
    /// 행동 컨텍스트 전환 처리
    ///</summary>
    private void ResetCurrentAction( eMonsterBehaviorSelectionContext _nextSelectionContext )
    {
        currentActionEntry = null;
        currentActionElapsedTime = 0.0f;
        currentActionCooldownRemaining = 0.0f;
        currentSelectionContext = _nextSelectionContext;
        hasTeleportedInCurrentAction = false;
        StopHorizontalMovement();
    }

    ///<summary>
    /// 행동 상태 전체 초기화
    ///</summary>
    private void ClearBehaviorState()
    {
        currentActionEntry = null;
        currentSelectionContext = eMonsterBehaviorSelectionContext.NONE;
        currentActionElapsedTime = 0.0f;
        currentActionCooldownRemaining = 0.0f;
        currentStateElapsedTime = 0.0f;
        currentWanderDirection = 1.0f;
        playerDetectionRetentionRemaining = 0.0f;
        hasTeleportedInCurrentAction = false;
        StopHorizontalMovement();
    }

    ///<summary>
    /// 목표 수평 속도 적용
    ///</summary>
    private void ApplyDesiredHorizontalVelocity()
    {
        if ( targetRigidbody == null )
        {
            return;
        }

        Vector2 currentVelocity = targetRigidbody.linearVelocity;
        currentVelocity.x = desiredHorizontalVelocity;
        targetRigidbody.linearVelocity = currentVelocity;
    }

    ///<summary>
    /// 수평 이동 중지
    ///</summary>
    private void StopHorizontalMovement()
    {
        desiredHorizontalVelocity = 0.0f;
    }

    ///<summary>
    /// 수평 이동 방향 반영
    ///</summary>
    private void SetHorizontalMovementDirection( float _horizontalDirection, float _moveSpeed )
    {
        if ( Mathf.Approximately( _horizontalDirection, 0.0f ) )
        {
            StopHorizontalMovement();
            return;
        }

        float normalizedDirection = Mathf.Sign( _horizontalDirection );
        desiredHorizontalVelocity = normalizedDirection * Mathf.Max( 0.0f, _moveSpeed );
        ApplyFacingDirectionFromDirection( normalizedDirection );
    }

    ///<summary>
    /// 플레이어 근처 순간이동 처리
    ///</summary>
    private void TryTeleportNearPlayer()
    {
        if ( cachedPlayerTransform == null )
        {
            return;
        }

        float horizontalDirection = transform.position.x <= cachedPlayerTransform.position.x ? -1.0f : 1.0f;
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition;
        nextPosition.x = cachedPlayerTransform.position.x + horizontalDirection * TeleportHorizontalOffset;
        transform.position = nextPosition;
        ApplyFacingDirectionTowardPlayer();
    }

    ///<summary>
    /// 이동 속도 결정
    ///</summary>
    private float ResolveMoveSpeed()
    {
        float moveSpeedFromStat = mvs > 0 ? mvs * MoveSpeedStatToUnitsMultiplier : 0.0f;
        float resolvedMoveSpeed = moveSpeedFromStat > 0.0f ? moveSpeedFromStat : DefaultMoveSpeed;
        return resolvedMoveSpeed;
    }

    ///<summary>
    /// 공격 상태 유지 시간 반환
    ///</summary>
    private float ResolveAttackStateDuration()
    {
        if ( currentActionEntry == null )
        {
            return DefaultAttackStateDuration;
        }

        float durationSeconds = currentActionEntry.GetDurationSeconds();
        float result = durationSeconds > 0.0f ? durationSeconds : DefaultAttackStateDuration;
        return result;
    }

    ///<summary>
    /// 랜덤 수평 방향 반환
    ///</summary>
    private float GetRandomHorizontalDirection()
    {
        float randomValue = Random.value;
        float result = randomValue >= 0.5f ? 1.0f : -1.0f;
        return result;
    }

    ///<summary>
    /// 방향값 기준 바라보기 적용
    ///</summary>
    private void ApplyFacingDirectionFromDirection( float _horizontalDirection )
    {
        if ( Mathf.Approximately( _horizontalDirection, 0.0f ) )
        {
            return;
        }

        Vector3 localScale = transform.localScale;
        bool isFacingRight = _horizontalDirection > 0.0f;
        bool usePositiveScale = isFacingRight == false ? isDefaultFacingRight == false : isDefaultFacingRight;
        localScale.x = usePositiveScale ? defaultScaleX : -defaultScaleX;
        transform.localScale = localScale;
    }

    ///<summary>
    /// 현재 바라보기 방향 반환
    ///</summary>
    private bool ResolveFacingRight()
    {
        float currentScaleX = transform.localScale.x;
        bool isUsingPositiveScale = currentScaleX >= 0.0f;
        bool result = isUsingPositiveScale ? isDefaultFacingRight : isDefaultFacingRight == false;
        return result;
    }

    ///<summary>
    /// 플레이어 방향 바라보기 적용
    ///</summary>
    private void ApplyFacingDirectionTowardPlayer()
    {
        if ( cachedPlayerTransform == null )
        {
            return;
        }

        float horizontalDelta = cachedPlayerTransform.position.x - transform.position.x;

        if ( Mathf.Approximately( horizontalDelta, 0.0f ) )
        {
            return;
        }

        ApplyFacingDirectionFromDirection( horizontalDelta );
    }

    ///<summary>
    /// 애니메이션 상태 재생
    ///</summary>
    private void PlayAnimationState( string _animationStateName )
    {
        if ( targetAnimator == null || string.IsNullOrWhiteSpace( _animationStateName ) )
        {
            return;
        }

        if ( targetAnimator.gameObject.activeInHierarchy == false || targetAnimator.isActiveAndEnabled == false )
        {
            pendingAnimationStateName = _animationStateName;
            return;
        }

        pendingAnimationStateName = string.Empty;
        targetAnimator.Play( _animationStateName );
    }

    ///<summary>
    /// 애니메이션 상태 처음부터 재생
    ///</summary>
    private void PlayAnimationStateFromStart( string _animationStateName )
    {
        if ( targetAnimator == null || string.IsNullOrWhiteSpace( _animationStateName ) )
        {
            return;
        }

        if ( targetAnimator.gameObject.activeInHierarchy == false || targetAnimator.isActiveAndEnabled == false )
        {
            pendingAnimationStateName = _animationStateName;
            return;
        }

        pendingAnimationStateName = string.Empty;
        targetAnimator.Play( _animationStateName, 0, 0.0f );
    }

    ///<summary>
    /// 보류된 애니메이션 상태 재생 시도
    ///</summary>
    private void TryPlayPendingAnimationState()
    {
        if ( targetAnimator == null || string.IsNullOrWhiteSpace( pendingAnimationStateName ) )
        {
            return;
        }

        if ( targetAnimator.gameObject.activeInHierarchy == false || targetAnimator.isActiveAndEnabled == false )
        {
            return;
        }

        string animationStateName = pendingAnimationStateName;
        pendingAnimationStateName = string.Empty;
        targetAnimator.Play( animationStateName );
    }

    ///<summary>
    /// 애니메이터 런타임 상태 초기화
    ///</summary>
    private void ResetAnimatorRuntimeState()
    {
        if ( targetAnimator == null )
        {
            return;
        }

        targetAnimator.Rebind();
        targetAnimator.Update( 0.0f );
    }

    ///<summary>
    /// 전투 콜라이더 활성 상태 설정
    ///</summary>
    private void SetBodyColliderEnabled( bool _isEnabled )
    {
        if ( bodyCollider != null )
        {
            bodyCollider.enabled = _isEnabled;
        }
    }

    ///<summary>
    /// 플레이어 상호작용 충돌체 활성 상태 설정
    ///</summary>
    private void SetContactHitboxEnabled( bool _isEnabled )
    {
        if ( contactHitboxCollider != null )
        {
            contactHitboxCollider.enabled = _isEnabled;
        }
    }

    ///<summary>
    /// 사망 시퀀스 시작
    ///</summary>
    private void StartDeathSequence()
    {
        if ( isDeathSequenceStarted )
        {
            return;
        }

        isDeathSequenceStarted = true;
        StopDeathSequence();
        deathSequenceRoutine = StartCoroutine( IE_HandleDeathSequence() );
    }

    ///<summary>
    /// 사망 시퀀스 중단
    ///</summary>
    private void StopDeathSequence()
    {
        if ( deathSequenceRoutine == null )
        {
            return;
        }

        StopCoroutine( deathSequenceRoutine );
        deathSequenceRoutine = null;
    }

    ///<summary>
    /// 사망 애니메이션 종료 대기
    ///</summary>
    private IEnumerator IE_HandleDeathSequence()
    {
        float fallbackElapsedTime = 0.0f;

        while ( HasDeathAnimationFinished() == false )
        {
            fallbackElapsedTime += Time.deltaTime;

            if ( fallbackElapsedTime >= DefaultDeathFallbackDuration )
            {
                break;
            }

            yield return null;
        }

        ReleaseMonsterObject();
    }

    ///<summary>
    /// 사망 애니메이션 종료 여부 반환
    ///</summary>
    private bool HasDeathAnimationFinished()
    {
        if ( targetAnimator == null )
        {
            return true;
        }

        AnimatorStateInfo animatorStateInfo = targetAnimator.GetCurrentAnimatorStateInfo( 0 );

        if ( animatorStateInfo.IsName( DeadAnimationStateName ) == false )
        {
            return false;
        }

        bool hasFinished = animatorStateInfo.normalizedTime >= DefaultDeathReleaseNormalizedTime;
        return hasFinished;
    }

    ///<summary>
    /// 몬스터 비활성 처리
    ///</summary>
    private void ReleaseMonsterObject()
    {
        deathSequenceRoutine = null;
        RestoreIdleStateBeforeRelease();

        bool wasReleasedToPool = TryReleaseToMapRuntimePool();

        if ( wasReleasedToPool )
        {
            return;
        }

        gameObject.SetActive( false );
    }

    ///<summary>
    /// 반환 직전 유휴 상태 복원
    ///</summary>
    private void RestoreIdleStateBeforeRelease()
    {
        isDeathSequenceStarted = false;
        hasRewardGranted = false;
        ClearSkillDebuffState();
        currentActionEntry = null;
        currentSelectionContext = eMonsterBehaviorSelectionContext.NONE;
        currentActionElapsedTime = 0.0f;
        currentActionCooldownRemaining = 0.0f;
        hasTeleportedInCurrentAction = false;
        ChangeState( eMonsterState.IDLE );

        if ( targetAnimator == null )
        {
            return;
        }

        if ( targetAnimator.gameObject.activeInHierarchy == false || targetAnimator.isActiveAndEnabled == false )
        {
            return;
        }

        targetAnimator.Update( 0.0f );
    }

    ///<summary>
    /// 런타임 맵 풀 반환 시도
    ///</summary>
    private bool TryReleaseToMapRuntimePool()
    {
        if ( string.IsNullOrWhiteSpace( mapRuntimePoolKey ) )
        {
            return false;
        }

        bool hasMapManager = CMapManager.TryGetInstance( out CMapManager mapManager );

        if ( hasMapManager == false || mapManager == null )
        {
            return false;
        }

        bool wasReleased = mapManager.ReleasePooledMonster( this, mapRuntimePoolKey );
        return wasReleased;
    }

    ///<summary>
    /// 몬스터 레이어 상호 충돌 비활성화
    ///</summary>
    private void EnsureMonsterCollisionIgnored()
    {
        if ( isMonsterCollisionIgnored )
        {
            return;
        }

        int monsterLayer = LayerMask.NameToLayer( "Monster" );

        if ( monsterLayer < 0 )
        {
            return;
        }

        Physics2D.IgnoreLayerCollision( monsterLayer, monsterLayer, true );
        isMonsterCollisionIgnored = true;
    }

    ///<summary>
    /// 몬스터 레이어 적용
    ///</summary>
    private void EnsureMonsterLayer()
    {
        int monsterLayer = LayerMask.NameToLayer( "Monster" );

        if ( monsterLayer < 0 )
        {
            return;
        }

        gameObject.layer = monsterLayer;
    }

    ///<summary>
    /// 본체 콜라이더 구성
    ///</summary>
    private void EnsureBodyCollider()
    {
        if ( bodyCollider == null )
        {
            Collider2D resolvedCollider = GetComponent<Collider2D>();
            bodyCollider = resolvedCollider;
        }

        if ( bodyCollider == null )
        {
            return;
        }

        bodyCollider.isTrigger = false;
        bodyCollider.excludeLayers = LayerMask.GetMask( "Player" );
    }

    ///<summary>
    /// 접촉 히트박스 구성
    ///</summary>
    private void EnsureContactHitbox()
    {
        Transform hitboxTransform = transform.Find( ContactHitboxObjectName );

        if ( hitboxTransform == null )
        {
            GameObject createdHitboxObject = new GameObject( ContactHitboxObjectName );
            Transform createdTransform = createdHitboxObject.transform;
            createdTransform.SetParent( transform, false );
            hitboxTransform = createdTransform;
        }

        int monsterLayer = LayerMask.NameToLayer( "Monster" );

        if ( monsterLayer >= 0 )
        {
            hitboxTransform.gameObject.layer = monsterLayer;
        }

        if ( contactHitboxCollider == null )
        {
            BoxCollider2D resolvedHitboxCollider = hitboxTransform.GetComponent<BoxCollider2D>();

            if ( resolvedHitboxCollider == null )
            {
                resolvedHitboxCollider = hitboxTransform.gameObject.AddComponent<BoxCollider2D>();
            }

            contactHitboxCollider = resolvedHitboxCollider;
        }

        contactHitboxCollider.isTrigger = true;

        if ( bodyCollider is BoxCollider2D sourceBoxCollider )
        {
            contactHitboxCollider.offset = sourceBoxCollider.offset;
            contactHitboxCollider.size = sourceBoxCollider.size;
        }

        MonsterContactHitbox contactHitbox = hitboxTransform.GetComponent<MonsterContactHitbox>();

        if ( contactHitbox == null )
        {
            hitboxTransform.gameObject.AddComponent<MonsterContactHitbox>();
        }
    }

    ///<summary>
    /// 몬스터 스탯 데이터 적용
    ///</summary>
    private void ApplyMonsterStatData( bool _preserveCurrentHp, long _previousCurrentHp )
    {
        if ( string.IsNullOrWhiteSpace( monsterId ) )
        {
            return;
        }

        CMonsterStatTableData monsterStatTableData = ResolveMonsterStatTableData();

        if ( monsterStatTableData == null )
        {
            return;
        }

        bool isFound = monsterStatTableData.TryGetRow( monsterId, out CMonsterStatRow rowData );

        if ( isFound == false || rowData == null )
        {
            Debug.LogWarning( $"Monster stat row was not found for id '{monsterId}'. Fallback name '{monsterName}' will be used.", this );
            return;
        }

        string rowName = rowData.GetName();
        level = rowData.GetLv();
        maxHp = rowData.GetHp();
        currentHp = ResolveConfiguredCurrentHp( _preserveCurrentHp, _previousCurrentHp, maxHp );
        atk = rowData.GetAtk();
        def = rowData.GetDef();
        ats = rowData.GetAts();
        mvs = rowData.GetMvs();
        expReward = rowData.GetExp();
        atAvailable = rowData.GetAtAvailable();
        hasRewardGranted = false;
        ClearSkillDebuffState();

        if ( string.IsNullOrWhiteSpace( rowName ) == false )
        {
            monsterName = rowName;
        }
    }

    ///<summary>
    /// 재구성 시 사용할 현재 체력 결정
    ///</summary>
    private long ResolveConfiguredCurrentHp( bool _preserveCurrentHp, long _previousCurrentHp, long _resolvedMaxHp )
    {
        if ( _preserveCurrentHp == false )
        {
            return _resolvedMaxHp;
        }

        long clampedCurrentHp = _previousCurrentHp;

        if ( clampedCurrentHp < 0 )
        {
            clampedCurrentHp = 0;
        }

        if ( clampedCurrentHp > _resolvedMaxHp )
        {
            clampedCurrentHp = _resolvedMaxHp;
        }

        return clampedCurrentHp;
    }

    ///<summary>
    /// 몬스터 스탯 테이블 결정
    ///</summary>
    private CMonsterStatTableData ResolveMonsterStatTableData()
    {
        if ( cachedMonsterStatTableData != null )
        {
            return cachedMonsterStatTableData;
        }

        CMonsterStatTableData loadedTableData = Resources.Load<CMonsterStatTableData>( MonsterStatTableResourcePath );
        cachedMonsterStatTableData = loadedTableData;

        if ( cachedMonsterStatTableData == null )
        {
            Debug.LogWarning( $"Monster stat table was not found at Resources/{MonsterStatTableResourcePath}.", this );
        }

        return cachedMonsterStatTableData;
    }

    ///<summary>
    /// 몬스터 정보 UI 등록
    ///</summary>
    private void RegisterMonsterInfo()
    {
        bool canRegister = isActiveAndEnabled && isConfigured;

        if ( canRegister == false )
        {
            return;
        }

        bool hasManager = CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager );

        if ( hasManager == false )
        {
            return;
        }

        monsterInfoManager.RegisterMonster( this );
    }

    ///<summary>
    /// 몬스터 정보 UI 해제
    ///</summary>
    private void UnregisterMonsterInfo()
    {
        bool hasManager = CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager );

        if ( hasManager == false )
        {
            return;
        }

        monsterInfoManager.UnregisterMonster( this );
    }

    ///<summary>
    /// 드랍 아이템 지급 처리
    ///</summary>
    private void SpawnDropItems()
    {
        if ( useItemDrop == false || itemDropEntryList == null || itemDropEntryList.Count == 0 )
        {
            return;
        }

        for ( int index = 0; index < itemDropEntryList.Count; index++ )
        {
            CMonsterItemDropEntry dropEntry = itemDropEntryList[ index ];

            if ( dropEntry == null )
            {
                continue;
            }

            CItemDefinition itemDefinition = dropEntry.GetItemDefinition();

            if ( itemDefinition == null )
            {
                continue;
            }

            float dropChance = dropEntry.GetDropChance();
            float randomValue = Random.value;

            if ( randomValue > dropChance )
            {
                continue;
            }

            long dropCount = ResolveDropCount( dropEntry );

            if ( dropCount <= 0 )
            {
                continue;
            }

            CreateWorldDropObject( itemDefinition, dropCount, index );
        }
    }

    ///<summary>
    /// 드랍 수량 결정
    ///</summary>
    private long ResolveDropCount( CMonsterItemDropEntry _dropEntry )
    {
        if ( _dropEntry == null )
        {
            return 0L;
        }

        long minDropCount = _dropEntry.GetMinDropCount();
        long maxDropCount = _dropEntry.GetMaxDropCount();

        if ( maxDropCount <= minDropCount )
        {
            return minDropCount;
        }

        double randomValue = UnityEngine.Random.value;
        double randomCount = minDropCount + ( ( maxDropCount - minDropCount + 1L ) * randomValue );
        long result = System.Math.Min( maxDropCount, ( long )randomCount );
        return result;
    }

    ///<summary>
    /// 월드 드랍 오브젝트 생성
    ///</summary>
    private void CreateWorldDropObject( CItemDefinition _itemDefinition, long _dropCount, int _dropIndex )
    {
        if ( _itemDefinition == null || _dropCount <= 0 )
        {
            return;
        }

        GameObject worldItemDropPrefab = ResolveWorldItemDropPrefab( _itemDefinition );

        if ( worldItemDropPrefab == null )
        {
            return;
        }

        float randomOffsetX = Random.Range( -0.45f, 0.45f );
        float randomOffsetY = Random.Range( 0.05f, 0.18f );
        Vector3 basePosition = transform.position;
        Vector3 dropPosition = new Vector3( basePosition.x + randomOffsetX, basePosition.y + randomOffsetY + ( _dropIndex * 0.05f ), basePosition.z );
        bool hasMapManager = CMapManager.TryGetInstance( out CMapManager mapManager );

        if ( hasMapManager && mapManager != null )
        {
            bool wasSpawnedFromPool = mapManager.TrySpawnWorldItemDrop( worldItemDropPrefab, _itemDefinition, _dropCount, dropPosition );

            if ( wasSpawnedFromPool )
            {
                return;
            }
        }

        GameObject createdDropObject = Instantiate( worldItemDropPrefab, dropPosition, Quaternion.identity );
        CWorldItemDropObject worldItemDropObject = createdDropObject.GetComponent<CWorldItemDropObject>();

        if ( worldItemDropObject == null )
        {
            return;
        }

        worldItemDropObject.SetMapRuntimePoolKey( string.Empty );
        worldItemDropObject.ConfigureDrop( _itemDefinition, _dropCount );
    }

    ///<summary>
    /// 월드 드랍 프리팹 결정
    ///</summary>
    private GameObject ResolveWorldItemDropPrefab( CItemDefinition _itemDefinition )
    {
        if ( _itemDefinition != null )
        {
            GameObject itemSpecificDropPrefab = _itemDefinition.GetWorldDropPrefab();

            if ( itemSpecificDropPrefab != null )
            {
                return itemSpecificDropPrefab;
            }
        }

        GameObject result = ResolveDefaultWorldItemDropPrefab();
        return result;
    }

    ///<summary>
    /// 공용 월드 드랍 프리팹 결정
    ///</summary>
    private GameObject ResolveDefaultWorldItemDropPrefab()
    {
        if ( cachedDefaultWorldItemDropPrefab != null )
        {
            return cachedDefaultWorldItemDropPrefab;
        }

        GameObject loadedWorldItemDropPrefab = Resources.Load<GameObject>( WorldItemDropPrefabResourcePath );
        cachedDefaultWorldItemDropPrefab = loadedWorldItemDropPrefab;
        return cachedDefaultWorldItemDropPrefab;
    }
}
