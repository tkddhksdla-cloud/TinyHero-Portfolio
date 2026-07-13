using TinyHero.Core;
using System.Collections;
using TinyHero.Quest;
using TinyHero.Skill;
using System.Collections.Generic;
using LayerLab.ArtMakerUnity;
using UnityEngine;
using TinyHero.UI;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 제어 컴포넌트
    ///</summary>
    [RequireComponent( typeof( Rigidbody2D ) )]
    public sealed class PlayerController : MonoBehaviour
    {
        ///<summary>
        /// 플레이어 상태 정의
        ///</summary>
        public enum ePlayerState
        {
            Idle,
            Move,
            Attack,
            Skill,
            Jump,
            Hit,
            Die
        }

        private const float DefaultGravityScale = 1.0f;
        private const float GroundDetachVelocityThreshold = 0.01f;
        private const string DefaultAttackSlashFxResourcePath = "Prefabs/FX/FX_DefaultAttack_Slash";
        private const string DefaultAttackSwingSfxClipName = "SFX_PLAYER_ATTACK_SWING_NORMAL";
        private const string JumpSfxClipName = "SFX_PLAYER_JUMP";
        private const string IdleAnimationStateName = "Idle";
        private const string MoveAnimationStateName = "Move";
        private const string AttackAnimationStateName = "Attack";
        private const string HitAnimationStateName = "Hit";
        private const string DieAnimationStateName = "Die";
        private const float DefaultAttackSlashFxLifetime = 0.5f;
        private const int InvalidSkillSlotIndex = -1;
        private const int MaxSkillSlotCount = 8;
        private const string DoubleJumpSkillId = "skill_double_jump";
        private const string AttackSlashFxPoolKeyPrefix = "Player.AttackSlashFx";

        [Header( "References" )]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private AnimationEventReceiver animationEventReceiver;
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private CPlayerStatManager targetStatManager;
        [SerializeField] private CPlayerEquipmentManager targetEquipmentManager;
        [SerializeField] private CPlayerInventoryManager targetInventoryManager;
        [SerializeField] private CSkillManager targetSkillManager;
        [SerializeField] private CQuestManager targetQuestManager;
        [SerializeField] private BoxCollider2D bodyCollider;
        [SerializeField] private Collider2D attackHitCollider;
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Collider2D[] targetColliders;

        [Header( "Movement" )]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float jumpPower = 8.5f;
        [SerializeField] private float doubleJumpForwardPower = 8.5f;
        [SerializeField] private float doubleJumpUpwardPower = 8.5f;
        [SerializeField] private float verticalDoubleJumpUpwardPower = 10.5f;
        [SerializeField] private float airHorizontalForce = 0.8f;
        [SerializeField] private float airHorizontalMaxSpeed = 3.0f;
        [SerializeField] private float risingGravityScale = 2.2f;
        [SerializeField] private float fallingGravityScale = 3.8f;
        [SerializeField] private int maxJumpCount = 2;

        [Header( "Ground Check" )]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private float groundCheckRadius = 0.12f;

        [Header( "Combat" )]
        [SerializeField] private float attackDuration = 0.2f;
        [SerializeField] private float attackAnimationSpeedMultiplier = 3.0f;
        [SerializeField] private float attackSlashFxLifetime = DefaultAttackSlashFxLifetime;
        [SerializeField] private bool isMonsterContactHitEnabled = true;
        [SerializeField] private float hitStateDuration = 0.25f;
        [SerializeField] private float hitKnockbackDistance = 0.45f;
        [SerializeField] private float invincibilityDuration = 1.25f;
        [SerializeField] private float invincibilityBlinkInterval = 0.1f;
        [SerializeField] private Color invincibilityTintColor = new Color( 0.35f, 0.35f, 0.35f, 1.0f );
        [SerializeField] private Color skillInvincibilityTintColor = new Color( 1.0f, 0.88f, 0.3f, 1.0f );
        [SerializeField] private SpriteRenderer[] targetSpriteRenderers;

        private const float DefaultAnimatorSpeed = 1.0f;
        private ePlayerState currentState = ePlayerState.Idle;
        private float horizontalInput;
        private float attackElapsedTime;
        private float defaultScaleX;
        private float defaultAnimatorSpeed = DefaultAnimatorSpeed;
        private float hitElapsedTime;
        private float hitReactionDirection = -1.0f;
        private float nextAttackAvailableTime;
        private float skillCastElapsedTime;
        private float skillCastDuration;
        private int superArmorCount;
        private float skillCastAnimationSpeedMultiplier = DefaultAnimatorSpeed;
        private float skillFinalAttackPercentBonus;
        private float skillFinalAttackBuffRemaining;
        private float skillInvincibilityRemaining;
        private float attackHitColliderBaseCircleRadius;
        private int currentJumpCount;
        private Color[] defaultSpriteColors;
        private SpriteRenderer[] defaultSpriteColorRendererArray;
        private GameObject attackSlashFxPrefab;
        private string attackSlashFxPoolKey = string.Empty;
        private Coroutine hitReactionRoutine;
        private Coroutine invincibilityRoutine;
        private string skillAnimationStateName = AttackAnimationStateName;
        private Vector2 attackHitColliderBaseBoxOffset;
        private Vector2 attackHitColliderBaseBoxSize;
        private Vector2 attackHitColliderBaseCapsuleOffset;
        private Vector2 attackHitColliderBaseCapsuleSize;
        private Vector2 attackHitColliderBaseCircleOffset;
        private Vector3 attackHitColliderBaseLocalPosition;
        private readonly Collider2D[] overlapResultBuffer = new Collider2D[ 16 ];
        private readonly Collider2D[] attackHitResultBuffer = new Collider2D[ 16 ];
        private readonly List<GameObject> activeAttackSlashFxObjectList = new List<GameObject>();
        private bool hasCachedAttackHitColliderBaseline;
        private bool isGrounded;
        private bool isHitReactionActive;
        private bool isInvincible;
        private bool isJumpHeld;
        private bool isInteractionHeld;
        private bool isPhaseStrikeActive;
        private bool isPendingJump;
        private bool isPendingAttack;
        private bool wasGrounded;
        private int pendingSkillSlotIndex = InvalidSkillSlotIndex;
        private CPlayerRuntimeContext playerRuntimeContext;

        public event System.Action OnAttackHitTriggered;

        ///<summary>
        /// 현재 상태 정보
        ///</summary>
        public ePlayerState CurrentState
        {
            get
            {
                ePlayerState result = currentState;
                return result;
            }
        }

        ///<summary>
        /// 플레이어 스킬 매니저 반환
        ///</summary>
        public CSkillManager GetSkillManager()
        {
            CSkillManager result = targetSkillManager;
            return result;
        }

        ///<summary>
        /// 플레이어 스탯 매니저 반환
        ///</summary>
        public CPlayerStatManager GetPlayerStatManager()
        {
            CPlayerStatManager result = targetStatManager;
            return result;
        }

        ///<summary>
        /// 플레이어 인벤토리 매니저 반환
        ///</summary>
        public CPlayerInventoryManager GetInventoryManager()
        {
            CPlayerInventoryManager result = targetInventoryManager;
            return result;
        }

        ///<summary>
        /// 플레이어 장비 매니저 반환
        ///</summary>
        public CPlayerEquipmentManager GetEquipmentManager()
        {
            CPlayerEquipmentManager result = targetEquipmentManager;
            return result;
        }

        ///<summary>
        /// 플레이어 퀘스트 매니저 반환
        ///</summary>
        public CQuestManager GetQuestManager()
        {
            CQuestManager result = targetQuestManager;
            return result;
        }

        ///<summary>
        /// 플레이어 런타임 컨텍스트 연결
        ///</summary>
        public void BindRuntimeContext( CPlayerRuntimeContext _playerRuntimeContext )
        {
            if ( _playerRuntimeContext == null )
            {
                return;
            }

            playerRuntimeContext = _playerRuntimeContext;
            targetStatManager = playerRuntimeContext.GetStatManager();
            targetInventoryManager = playerRuntimeContext.GetInventoryManager();
            targetEquipmentManager = playerRuntimeContext.GetEquipmentManager();
            targetQuestManager = playerRuntimeContext.GetQuestManager();
            targetSkillManager = playerRuntimeContext.GetSkillManager();
            CPlayerEquipmentPartsSync equipmentPartsSync = GetComponent<CPlayerEquipmentPartsSync>();

            if ( equipmentPartsSync != null )
            {
                equipmentPartsSync.BindEquipmentManager( targetEquipmentManager );
            }
        }

        ///<summary>
        /// 플레이어 애니메이터 반환
        ///</summary>
        public Animator GetTargetAnimator()
        {
            Animator result = targetAnimator;
            return result;
        }

        ///<summary>
        /// 현재 애니메이션 상태 이름 반환
        ///</summary>
        public string GetCurrentAnimationStateName()
        {
            string result = ResolveAnimationStateName();
            return result;
        }

        ///<summary>
        /// 플레이어 이름표 월드 위치 반환
        ///</summary>
        public Vector3 GetNameTagWorldPosition()
        {
            if ( bodyCollider == null )
            {
                Vector3 fallbackPosition = transform.position + new Vector3( 0.0f, 1.4f, 0.0f );
                return fallbackPosition;
            }

            Bounds colliderBounds = bodyCollider.bounds;
            Vector3 result = new Vector3( colliderBounds.center.x, colliderBounds.max.y, colliderBounds.center.z );
            return result;
        }

        ///<summary>
        /// 현재 애니메이터 속도 반환
        ///</summary>
        public float GetCurrentAnimatorSpeed()
        {
            float result = targetAnimator != null ? targetAnimator.speed : DefaultAnimatorSpeed;
            return result;
        }

        ///<summary>
        /// 몬스터 접촉 피격 활성 상태 설정
        ///</summary>
        public void SetMonsterContactHitEnabled( bool _isEnabled )
        {
            isMonsterContactHitEnabled = _isEnabled;
        }

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            attackSlashFxPoolKey = AttackSlashFxPoolKeyPrefix + "." + GetInstanceID();
            defaultScaleX = Mathf.Abs( transform.localScale.x );

            if ( defaultScaleX <= 0.0f )
            {
                defaultScaleX = 1.0f;
            }

            if ( targetAnimator == null )
            {
                Animator resolvedAnimator = GetComponent<Animator>();
                targetAnimator = resolvedAnimator;
            }

            if ( targetAnimator != null )
            {
                defaultAnimatorSpeed = targetAnimator.speed;
            }

            if ( animationEventReceiver == null )
            {
                AnimationEventReceiver resolvedAnimationEventReceiver = GetComponentInChildren<AnimationEventReceiver>( true );
                animationEventReceiver = resolvedAnimationEventReceiver;
            }

            if ( targetRigidbody == null )
            {
                Rigidbody2D resolvedRigidbody = GetComponent<Rigidbody2D>();
                targetRigidbody = resolvedRigidbody;
            }

            if ( targetRigidbody != null )
            {
                targetRigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }

            ResolveStatManager();
            ResolveEquipmentManager();
            ResolveInventoryManager();
            ResolveSkillManager();
            ResolveQuestManager();

            if ( bodyCollider == null )
            {
                BoxCollider2D resolvedBodyCollider = ResolveBodyCollider();
                bodyCollider = resolvedBodyCollider;
            }

            if ( attackHitCollider == null )
            {
                Collider2D resolvedAttackHitCollider = ResolveAttackHitCollider();
                attackHitCollider = resolvedAttackHitCollider;
            }

            ConfigureAttackHitCollider();
            CacheAttackHitColliderBaseline();
            ApplyAttackHitColliderRange();
            SetAttackHitColliderActive( false );
            SubscribeAnimationEventReceiver();
            EnsureAttackSlashFxPoolInitialized();

            CacheTargetColliders();
            CacheSpriteRenderers();
            CacheDefaultSpriteColors();
        }

        ///<summary>
        /// 초기 상태 설정
        ///</summary>
        private void Start()
        {
            PreloadCombatSfx();
            currentState = ePlayerState.Die;
            ChangeState( ePlayerState.Idle );
        }

        ///<summary>
        /// 활성화 시 이벤트 재구독
        ///</summary>
        private void OnEnable()
        {
            if ( animationEventReceiver == null )
            {
                AnimationEventReceiver resolvedAnimationEventReceiver = GetComponentInChildren<AnimationEventReceiver>( true );
                animationEventReceiver = resolvedAnimationEventReceiver;
            }

            if ( attackHitCollider == null )
            {
                Collider2D resolvedAttackHitCollider = ResolveAttackHitCollider();
                attackHitCollider = resolvedAttackHitCollider;
            }

            if ( bodyCollider == null )
            {
                BoxCollider2D resolvedBodyCollider = ResolveBodyCollider();
                bodyCollider = resolvedBodyCollider;
            }

            ConfigureAttackHitCollider();
            CacheAttackHitColliderBaseline();
            ApplyAttackHitColliderRange();
            SetAttackHitColliderActive( false );
            CacheTargetColliders();
            SubscribeAnimationEventReceiver();
            EnsureAttackSlashFxPoolInitialized();

            if ( CPlayerNameTagManager.TryGetInstance( out CPlayerNameTagManager playerNameTagManager ) && playerNameTagManager != null )
            {
                playerNameTagManager.RegisterPlayer( this );
            }
        }

        ///<summary>
        /// 비활성화 시 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeAnimationEventReceiver();
            SetAttackHitColliderActive( false );
            ClearSkillBuffState();
            isPhaseStrikeActive = false;
            SetSpriteRendererVisible( true );
            StopInvincibilityVisual();

            if ( CPlayerNameTagManager.TryGetExistingInstance( out CPlayerNameTagManager playerNameTagManager ) && playerNameTagManager != null )
            {
                playerNameTagManager.UnregisterPlayer( this );
            }
        }

        ///<summary>
        /// 프레임 상태 처리
        ///</summary>
        private void Update()
        {
            if ( targetRigidbody == null )
            {
                return;
            }

            TickSkillBuffState();

            if ( isPhaseStrikeActive )
            {
                ApplyPhaseStrikeControlLock();
                ClearOneShotInputs();
                return;
            }

            CaptureInput();
            UpdateGroundState();
            ApplyNpcInteractionControlLock();
            ProcessStateTransitions();
            UpdateCurrentState();
            EvaluateMonsterContactOverlap();
            ClearOneShotInputs();
        }

        ///<summary>
        /// 물리 상태 처리
        ///</summary>
        private void FixedUpdate()
        {
            if ( targetRigidbody == null )
            {
                return;
            }

            if ( isPhaseStrikeActive )
            {
                ApplyPhaseStrikeControlLock();
                return;
            }

            ApplyFacingDirection();
            ApplyHorizontalMovement();
            ApplyJumpGravity();
        }

        ///<summary>
        /// 종료 시각 효과 복원
        ///</summary>
        private void OnDestroy()
        {
            StopHitReaction();
            StopInvincibilityVisual();
            RestoreAnimatorSpeed();
            SetAttackHitColliderActive( false );
            ReleaseAllPooledEffects();
            CObjectPoolManager.TryClearPool( attackSlashFxPoolKey );

            SetSpriteRendererVisible( true );

            if ( playerRuntimeContext != null )
            {
                playerRuntimeContext.UnbindPlayerController( this );
                playerRuntimeContext = null;
            }
        }

        ///<summary>
        /// 활성 풀링 이펙트 일괄 반환
        ///</summary>
        public void ReleaseAllPooledEffects()
        {
            List<GameObject> activeFxObjectList = new List<GameObject>( activeAttackSlashFxObjectList );

            for ( int index = 0; index < activeFxObjectList.Count; index++ )
            {
                GameObject fxObject = activeFxObjectList[ index ];

                if ( fxObject == null )
                {
                    continue;
                }

                CObjectPoolManager.TryRelease( attackSlashFxPoolKey, fxObject );
            }

            activeAttackSlashFxObjectList.Clear();
        }

        ///<summary>
        /// 사망 상태 전환
        ///</summary>
        public void Die()
        {
            ChangeState( ePlayerState.Die );
        }

        ///<summary>
        /// 피격 상태 전환
        ///</summary>
        public void Hit()
        {
            if ( IsSuperArmorActive() )
            {
                return;
            }

            ChangeState( ePlayerState.Hit );
        }

        public void BeginSuperArmor()
        {
            superArmorCount++;
        }

        public void EndSuperArmor()
        {
            superArmorCount = Mathf.Max( 0, superArmorCount - 1 );
        }

        public bool IsSuperArmorActive()
        {
            return superArmorCount > 0;
        }

        ///<summary>
        /// 공격 타격 이벤트 수신
        ///</summary>
        public void OnAttackHit()
        {
            HandleAttackHitEvent();
        }

        ///<summary>
        /// 툴 전용 스킬 시전 상태 시작
        ///</summary>
        public bool TryBeginToolSkillCast( string _animationStateName, float _animationSpeedMultiplier, float _lockDurationSeconds )
        {
            if ( currentState == ePlayerState.Die || currentState == ePlayerState.Hit )
            {
                return false;
            }

            skillCastElapsedTime = 0.0f;
            skillCastDuration = Mathf.Max( 0.0f, _lockDurationSeconds );
            skillCastAnimationSpeedMultiplier = Mathf.Max( 0.01f, _animationSpeedMultiplier );
            skillAnimationStateName = ResolveSkillAnimationStateName( _animationStateName );

            if ( currentState == ePlayerState.Skill )
            {
                EnterSkillState();
                ApplyAnimationState();
                return true;
            }

            ChangeState( ePlayerState.Skill );
            return true;
        }

        public bool IsGrounded()
        {
            return isGrounded;
        }

        ///<summary>
        /// 페이즈 스트라이크 상태 시작
        ///</summary>
        public void BeginPhaseStrikeState()
        {
            isPhaseStrikeActive = true;
            CacheSpriteRenderers();
            CacheDefaultSpriteColors();
            RestoreSpriteColors();
            SetSpriteRendererVisible( false );
            SetPlayerNameTagVisible( false );
            ApplyPhaseStrikeControlLock();
        }

        ///<summary>
        /// 페이즈 스트라이크 상태 종료
        ///</summary>
        public void EndPhaseStrikeState( Vector3 _returnWorldPosition )
        {
            transform.position = _returnWorldPosition;
            isPhaseStrikeActive = false;
            RestoreSpriteColors();
            SetSpriteRendererVisible( true );
            SetPlayerNameTagVisible( true );
            ApplyPhaseStrikeControlLock();
            EvaluateMonsterContactOverlap();
        }

        ///<summary>
        /// 접촉 피격 처리
        ///</summary>
        public bool TryReceiveContactHit()
        {
            bool didReceiveHit = TryReceiveContactHit( null );
            return didReceiveHit;
        }

        ///<summary>
        /// 접촉 피격 처리
        ///</summary>
        public bool TryReceiveContactHit( MonsterObject _monsterObject )
        {
            if ( isMonsterContactHitEnabled == false )
            {
                return false;
            }

            if ( currentState == ePlayerState.Die || IsAnyInvincibleStateActive() )
            {
                return false;
            }

            ApplyMonsterContactDamage( _monsterObject );
            hitReactionDirection = -ResolveFacingDirection();
            Hit();
            BeginInvincibility();
            return true;
        }

        ///<summary>
        /// 스킬 최종 공격력 증가 버프 적용
        ///</summary>
        public void ApplyFinalAttackPercentBuff( float _increasePercent, float _durationSeconds )
        {
            float resolvedIncreasePercent = Mathf.Max( 0.0f, _increasePercent );
            float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );

            if ( resolvedIncreasePercent <= 0.0f || resolvedDurationSeconds <= 0.0f )
            {
                return;
            }

            skillFinalAttackPercentBonus = resolvedIncreasePercent;
            skillFinalAttackBuffRemaining = resolvedDurationSeconds;
        }

        ///<summary>
        /// 스킬 무적 버프 적용
        ///</summary>
        public void ApplySkillInvincibility( float _durationSeconds )
        {
            float resolvedDurationSeconds = Mathf.Max( 0.0f, _durationSeconds );

            if ( resolvedDurationSeconds <= 0.0f )
            {
                return;
            }

            skillInvincibilityRemaining = resolvedDurationSeconds;
        }

        ///<summary>
        /// 스킬 기반 공격력 배수 반환
        ///</summary>
        public float GetSkillAttackPowerMultiplier()
        {
            float equipmentFinalAttackPercentBonus = targetStatManager != null ? targetStatManager.GetEquipmentFinalAttackPercentBonus() : 0.0f;
            float result = 1.0f + Mathf.Max( 0.0f, skillFinalAttackPercentBonus ) + Mathf.Max( 0.0f, equipmentFinalAttackPercentBonus );
            return result;
        }

        ///<summary>
        /// 플레이어 스탯 매니저 결정
        ///</summary>
        private void ResolveStatManager()
        {
            if ( targetStatManager != null )
            {
                return;
            }

            CPlayerStatManager resolvedStatManager = GetComponent<CPlayerStatManager>();
            targetStatManager = resolvedStatManager;
        }

        ///<summary>
        /// 플레이어 스킬 매니저 결정
        ///</summary>
        private void ResolveSkillManager()
        {
            if ( targetSkillManager != null )
            {
                return;
            }

            CSkillManager resolvedSkillManager = GetComponent<CSkillManager>();
            targetSkillManager = resolvedSkillManager;
        }

        ///<summary>
        /// 플레이어 장비 매니저 결정
        ///</summary>
        private void ResolveEquipmentManager()
        {
            if ( targetEquipmentManager != null )
            {
                return;
            }

            CPlayerEquipmentManager resolvedEquipmentManager = GetComponent<CPlayerEquipmentManager>();
            targetEquipmentManager = resolvedEquipmentManager;
        }

        ///<summary>
        /// 플레이어 인벤토리 매니저 결정
        ///</summary>
        private void ResolveInventoryManager()
        {
            if ( targetInventoryManager != null )
            {
                return;
            }

            CPlayerInventoryManager resolvedInventoryManager = GetComponent<CPlayerInventoryManager>();
            targetInventoryManager = resolvedInventoryManager;
        }

        ///<summary>
        /// 플레이어 퀘스트 매니저 결정
        ///</summary>
        private void ResolveQuestManager()
        {
            if ( targetQuestManager != null )
            {
                return;
            }

            CQuestManager resolvedQuestManager = GetComponent<CQuestManager>();
            targetQuestManager = resolvedQuestManager;
        }

        ///<summary>
        /// 입력 수집
        ///</summary>
        private void CaptureInput()
        {
            if ( isHitReactionActive )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                pendingSkillSlotIndex = InvalidSkillSlotIndex;
                return;
            }

            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager == null )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                pendingSkillSlotIndex = InvalidSkillSlotIndex;
                return;
            }

            if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager ) && interactionManager != null && interactionManager.IsInteractionInProgress() )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                pendingSkillSlotIndex = InvalidSkillSlotIndex;
                interactionManager.TryProcessInteractionInput( inputManager.GetInteractionDown() );
                return;
            }

            float capturedHorizontalInput = inputManager.GetHorizontalInput();
            bool capturedJumpHeld = inputManager.GetJumpHeld();
            bool capturedInteractionHeld = inputManager.GetInteractionHeld();
            bool capturedJumpDown = inputManager.GetJumpDown();
            bool capturedAttackDown = inputManager.GetAttackDown();
            bool capturedInteractionDown = inputManager.GetInteractionDown();

            if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager availableInteractionManager ) && availableInteractionManager != null )
            {
                availableInteractionManager.TryProcessInteractionInput( capturedInteractionDown );
            }

            if ( currentState == ePlayerState.Attack )
            {
                horizontalInput = 0.0f;
                isJumpHeld = capturedJumpHeld;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                pendingSkillSlotIndex = InvalidSkillSlotIndex;
                return;
            }

            if ( currentState == ePlayerState.Skill )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                pendingSkillSlotIndex = InvalidSkillSlotIndex;
                return;
            }

            horizontalInput = capturedHorizontalInput;
            isJumpHeld = capturedJumpHeld;
            isInteractionHeld = capturedInteractionHeld;
            isPendingJump = capturedJumpDown;
            isPendingAttack = capturedAttackDown;
            pendingSkillSlotIndex = ResolvePendingSkillSlotIndex( inputManager );
        }

        ///<summary>
        /// 지상 상태 갱신
        ///</summary>
        private void UpdateGroundState()
        {
            Vector2 groundCheckPosition = GetGroundCheckPosition();
            Collider2D hitCollider = Physics2D.OverlapCircle( groundCheckPosition, groundCheckRadius, groundLayerMask );
            bool currentGroundedState = hitCollider != null;
            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > GroundDetachVelocityThreshold )
            {
                currentGroundedState = false;
            }

            wasGrounded = isGrounded;
            isGrounded = currentGroundedState;

            if ( isGrounded && wasGrounded == false )
            {
                currentJumpCount = 0;
            }

            if ( isGrounded == false && wasGrounded && targetRigidbody.linearVelocity.y <= 0.0f )
            {
                currentJumpCount = 1;
            }
        }

        ///<summary>
        /// 상태 전환 조건 처리
        ///</summary>
        private void ProcessStateTransitions()
        {
            if ( currentState == ePlayerState.Die )
            {
                return;
            }

            if ( currentState == ePlayerState.Hit )
            {
                return;
            }

            if ( currentState == ePlayerState.Attack )
            {
                return;
            }

            if ( currentState == ePlayerState.Skill )
            {
                return;
            }

            if ( IsNpcInteractionBlockingControls() )
            {
                return;
            }

            if ( TryProcessPendingSkillInput() )
            {
                return;
            }

            if ( isPendingAttack )
            {
                if ( CanStartAttack() == false )
                {
                    return;
                }

                ChangeState( ePlayerState.Attack );
                return;
            }

            if ( isPendingJump && CanJump() )
            {
                bool didJump = TryExecuteJump();

                if ( didJump )
                {
                    ChangeState( ePlayerState.Jump );
                }

                return;
            }

            if ( currentState == ePlayerState.Jump && isGrounded )
            {
                ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
                ChangeState( nextGroundedState );
                return;
            }

        }

        ///<summary>
        /// 현재 상태 갱신
        ///</summary>
        private void UpdateCurrentState()
        {
            switch ( currentState )
            {
                case ePlayerState.Idle:
                    UpdateIdleState();
                    break;

                case ePlayerState.Move:
                    UpdateMoveState();
                    break;

                case ePlayerState.Attack:
                    UpdateAttackState();
                    break;

                case ePlayerState.Skill:
                    UpdateSkillState();
                    break;

                case ePlayerState.Jump:
                    UpdateJumpState();
                    break;

                case ePlayerState.Hit:
                    UpdateHitState();
                    break;

                case ePlayerState.Die:
                    UpdateDieState();
                    break;
            }
        }

        ///<summary>
        /// 상태 변경
        ///</summary>
        private void ChangeState( ePlayerState _nextState )
        {
            if ( currentState == _nextState )
            {
                ApplyAnimationState();
                return;
            }

            currentState = _nextState;

            switch ( currentState )
            {
                case ePlayerState.Idle:
                    EnterIdleState();
                    break;

                case ePlayerState.Move:
                    EnterMoveState();
                    break;

                case ePlayerState.Attack:
                    EnterAttackState();
                    break;

                case ePlayerState.Skill:
                    EnterSkillState();
                    break;

                case ePlayerState.Jump:
                    EnterJumpState();
                    break;

                case ePlayerState.Hit:
                    EnterHitState();
                    break;

                case ePlayerState.Die:
                    EnterDieState();
                    break;
            }

            ApplyAnimationState();
        }

        ///<summary>
        /// 대기 상태 진입 처리
        ///</summary>
        private void EnterIdleState()
        {
            attackElapsedTime = 0.0f;
            RestoreAnimatorSpeed();
        }

        ///<summary>
        /// 이동 상태 진입 처리
        ///</summary>
        private void EnterMoveState()
        {
            attackElapsedTime = 0.0f;
            RestoreAnimatorSpeed();
        }

        ///<summary>
        /// 공격 상태 진입 처리
        ///</summary>
        private void EnterAttackState()
        {
            attackElapsedTime = 0.0f;
            ApplyAttackAnimationSpeed();
            ApplyAttackHitColliderRange();
            SetAttackHorizontalVelocity( 0.0f );
            SetAttackHitColliderActive( false );
            PlayDefaultAttackSwingSfx();
            nextAttackAvailableTime = Time.time + ResolveAttackIntervalSeconds();
        }

        ///<summary>
        /// 점프 상태 진입 처리
        ///</summary>
        private void EnterJumpState()
        {
            RestoreAnimatorSpeed();
            SetAttackHitColliderActive( false );
        }

        ///<summary>
        /// 스킬 상태 진입 처리
        ///</summary>
        private void EnterSkillState()
        {
            attackElapsedTime = 0.0f;
            skillCastElapsedTime = 0.0f;
            ApplySkillAnimationSpeed();
            SetAttackHorizontalVelocity( 0.0f );
            SetAttackHitColliderActive( false );
        }

        ///<summary>
        /// 피격 상태 진입 처리
        ///</summary>
        private void EnterHitState()
        {
            attackElapsedTime = 0.0f;
            hitElapsedTime = 0.0f;
            RestoreAnimatorSpeed();
            SetAttackHitColliderActive( false );

            if ( targetRigidbody != null )
            {
                targetRigidbody.linearVelocity = Vector2.zero;
            }

            StartHitReaction();
        }

        ///<summary>
        /// 사망 상태 진입 처리
        ///</summary>
        private void EnterDieState()
        {
            RestoreAnimatorSpeed();
            SetAttackHitColliderActive( false );
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.x = 0.0f;
            targetRigidbody.linearVelocity = currentVelocity;
            targetRigidbody.gravityScale = DefaultGravityScale;
        }

        ///<summary>
        /// 대기 상태 갱신
        ///</summary>
        private void UpdateIdleState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.Jump );
                return;
            }

            if ( HasHorizontalInput() )
            {
                ChangeState( ePlayerState.Move );
            }
        }

        ///<summary>
        /// 이동 상태 갱신
        ///</summary>
        private void UpdateMoveState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.Jump );
                return;
            }

            if ( HasHorizontalInput() == false )
            {
                ChangeState( ePlayerState.Idle );
            }
        }

        ///<summary>
        /// 공격 상태 갱신
        ///</summary>
        private void UpdateAttackState()
        {
            attackElapsedTime += Time.deltaTime;

            if ( HasAttackAnimationFinished() == false )
            {
                return;
            }

            ePlayerState nextState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.Jump;
            ChangeState( nextState );
        }

        ///<summary>
        /// 스킬 상태 갱신
        ///</summary>
        private void UpdateSkillState()
        {
            skillCastElapsedTime += Time.deltaTime;

            if ( skillCastElapsedTime < skillCastDuration )
            {
                return;
            }

            ePlayerState nextState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.Jump;
            ChangeState( nextState );
        }

        ///<summary>
        /// 점프 상태 갱신
        ///</summary>
        private void UpdateJumpState()
        {
            ApplyAnimationState();

            if ( isGrounded == false )
            {
                return;
            }

            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > 0.01f )
            {
                return;
            }

            ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
            ChangeState( nextGroundedState );
        }

        ///<summary>
        /// 피격 상태 갱신
        ///</summary>
        private void UpdateHitState()
        {
            hitElapsedTime += Time.deltaTime;

            if ( hitElapsedTime < hitStateDuration || isHitReactionActive )
            {
                return;
            }

            ePlayerState nextGroundedState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.Jump;
            ChangeState( nextGroundedState );
        }

        ///<summary>
        /// 사망 상태 갱신
        ///</summary>
        private void UpdateDieState()
        {
        }

        ///<summary>
        /// 수평 이동 적용
        ///</summary>
        private void ApplyHorizontalMovement()
        {
            if ( IsNpcInteractionBlockingControls() )
            {
                return;
            }

            if ( currentState == ePlayerState.Die || currentState == ePlayerState.Hit || currentState == ePlayerState.Attack || currentState == ePlayerState.Skill )
            {
                return;
            }

            if ( isGrounded == false )
            {
                ApplyAirHorizontalForce();
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            float resolvedMoveSpeed = ResolveMoveSpeed();
            float horizontalVelocity = horizontalInput * resolvedMoveSpeed;
            currentVelocity.x = horizontalVelocity;
            targetRigidbody.linearVelocity = currentVelocity;
        }

        ///<summary>
        /// 바라보기 방향 적용
        ///</summary>
        private void ApplyFacingDirection()
        {
            if ( IsNpcInteractionBlockingControls() )
            {
                return;
            }

            if ( currentState == ePlayerState.Hit || currentState == ePlayerState.Attack || currentState == ePlayerState.Skill )
            {
                return;
            }

            if ( Mathf.Approximately( horizontalInput, 0.0f ) )
            {
                return;
            }

            Vector3 localScale = transform.localScale;
            float facingScaleX = horizontalInput > 0.0f ? defaultScaleX : -defaultScaleX;
            localScale.x = facingScaleX;
            transform.localScale = localScale;
        }

        ///<summary>
        /// 점프 중력 적용
        ///</summary>
        private void ApplyJumpGravity()
        {
            if ( currentState == ePlayerState.Die )
            {
                return;
            }

            if ( isGrounded )
            {
                targetRigidbody.gravityScale = DefaultGravityScale;
                return;
            }

            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > 0.0f && isJumpHeld )
            {
                targetRigidbody.gravityScale = risingGravityScale;
                return;
            }

            targetRigidbody.gravityScale = fallingGravityScale;
        }

        ///<summary>
        /// 점프 가능 여부
        ///</summary>
        private bool CanJump()
        {
            if ( currentJumpCount == 1 && CanUseDoubleJumpSkill() == false )
            {
                return false;
            }

            bool canJump = currentJumpCount < maxJumpCount;
            return canJump;
        }

        ///<summary>
        /// 점프 실행 시도
        ///</summary>
        private bool TryExecuteJump()
        {
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            bool isDoubleJump = currentJumpCount > 0;

            if ( isDoubleJump )
            {
                bool didConsumeDoubleJumpMp = TryConsumeDoubleJumpMp();

                if ( didConsumeDoubleJumpMp == false )
                {
                    return false;
                }

                PlayDoubleJumpCastFeedback();

                currentVelocity.y = 0.0f;
                targetRigidbody.linearVelocity = currentVelocity;

                Vector2 doubleJumpForce = ResolveDoubleJumpForce();
                targetRigidbody.AddForce( doubleJumpForce, ForceMode2D.Impulse );
            }
            else
            {
                currentVelocity.x = ResolveGroundJumpHorizontalVelocity();
                currentVelocity.y = jumpPower;
                targetRigidbody.linearVelocity = currentVelocity;
            }

            currentJumpCount++;
            isGrounded = false;

            if ( isDoubleJump == false )
            {
                PlayJumpSfx();
            }

            return true;
        }

        ///<summary>
        /// 지상 점프 수평 속도 반환
        ///</summary>
        private float ResolveGroundJumpHorizontalVelocity()
        {
            if ( HasHorizontalInput() == false )
            {
                return 0.0f;
            }

            float resolvedMoveSpeed = ResolveMoveSpeed();
            float result = horizontalInput * resolvedMoveSpeed;
            return result;
        }

        ///<summary>
        /// 더블 점프 스킬 사용 가능 여부 반환
        ///</summary>
        private bool CanUseDoubleJumpSkill()
        {
            if ( targetSkillManager == null || targetStatManager == null )
            {
                return false;
            }

            bool isUnlocked = targetSkillManager.IsSkillUnlocked( DoubleJumpSkillId );

            if ( isUnlocked == false )
            {
                return false;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( DoubleJumpSkillId );

            if ( skillDefinition == null )
            {
                return false;
            }

            int skillLevel = Mathf.Max( 1, targetSkillManager.GetSkillLevel( DoubleJumpSkillId ) );
            float mpCost = skillDefinition.GetMpCost( skillLevel );
            float currentMp = targetStatManager.GetCurrentMp();
            bool result = currentMp >= mpCost;
            return result;
        }

        ///<summary>
        /// 더블 점프 MP 소모 처리
        ///</summary>
        private bool TryConsumeDoubleJumpMp()
        {
            if ( targetSkillManager == null || targetStatManager == null )
            {
                return false;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( DoubleJumpSkillId );

            if ( skillDefinition == null )
            {
                return false;
            }

            int skillLevel = Mathf.Max( 1, targetSkillManager.GetSkillLevel( DoubleJumpSkillId ) );
            float mpCost = skillDefinition.GetMpCost( skillLevel );
            bool result = targetStatManager.TryConsumeMp( mpCost );
            return result;
        }

        ///<summary>
        /// 더블 점프 캐스트 피드백 재생
        ///</summary>
        private void PlayDoubleJumpCastFeedback()
        {
            if ( targetSkillManager == null || targetStatManager == null )
            {
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( DoubleJumpSkillId );

            if ( skillDefinition == null )
            {
                return;
            }

            CSkillRuntimeData skillRuntimeData = targetSkillManager.GetSkillRuntimeData( DoubleJumpSkillId );
            CSkillContext skillContext = new CSkillContext( targetSkillManager, this, targetStatManager, skillDefinition, skillRuntimeData, transform );
            CSkillVfxUtility.PlayCastVfx( skillContext );
            CSkillAudioUtility.PlayCastSfx( skillContext );
        }

        ///<summary>
        /// 지상 체크 위치 반환
        ///</summary>
        private Vector2 GetGroundCheckPosition()
        {
            if ( groundCheckPoint != null )
            {
                Vector2 pointPosition = groundCheckPoint.position;
                return pointPosition;
            }

            Vector2 fallbackPosition = transform.position;
            return fallbackPosition;
        }

        ///<summary>
        /// 수평 입력 여부
        ///</summary>
        private bool HasHorizontalInput()
        {
            bool hasHorizontalInput = Mathf.Approximately( horizontalInput, 0.0f ) == false;
            return hasHorizontalInput;
        }

        ///<summary>
        /// 이동 속도 결정
        ///</summary>
        private float ResolveMoveSpeed()
        {
            if ( targetStatManager == null )
            {
                float fallbackMoveSpeed = moveSpeed;
                return fallbackMoveSpeed;
            }

            float moveSpeedMultiplier = targetStatManager.GetMoveSpeedMultiplier();
            float result = moveSpeed * moveSpeedMultiplier;
            return result;
        }

        ///<summary>
        /// 공중 수평 힘 적용
        ///</summary>
        private void ApplyAirHorizontalForce()
        {
            if ( currentState == ePlayerState.Attack )
            {
                return;
            }

            if ( HasHorizontalInput() == false )
            {
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            float inputDirection = Mathf.Sign( horizontalInput );
            float directionalVelocity = currentVelocity.x * inputDirection;

            if ( directionalVelocity >= airHorizontalMaxSpeed )
            {
                return;
            }

            Vector2 airForce = new Vector2( inputDirection * airHorizontalForce, 0.0f );
            targetRigidbody.AddForce( airForce, ForceMode2D.Force );
        }

        ///<summary>
        /// 더블 점프 힘 결정
        ///</summary>
        private Vector2 ResolveDoubleJumpForce()
        {
            if ( HasHorizontalInput() )
            {
                Vector2 inputForwardDoubleJumpForce = GetInputForwardDoubleJumpForce();
                return inputForwardDoubleJumpForce;
            }

            if ( isInteractionHeld )
            {
                Vector2 upwardDoubleJumpForce = GetUpwardDoubleJumpForce();
                return upwardDoubleJumpForce;
            }

            Vector2 facingForwardDoubleJumpForce = GetFacingForwardDoubleJumpForce();
            return facingForwardDoubleJumpForce;
        }

        ///<summary>
        /// 입력 전방 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetInputForwardDoubleJumpForce()
        {
            float inputDirection = Mathf.Sign( horizontalInput );
            float horizontalForce = inputDirection * doubleJumpForwardPower;
            Vector2 result = new Vector2( horizontalForce, doubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 바라보기 전방 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetFacingForwardDoubleJumpForce()
        {
            float facingDirection = ResolveFacingDirection();
            float horizontalForce = facingDirection * doubleJumpForwardPower;
            Vector2 result = new Vector2( horizontalForce, doubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 상향 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetUpwardDoubleJumpForce()
        {
            Vector2 result = new Vector2( 0.0f, verticalDoubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 바라보기 방향 결정
        ///</summary>
        private float ResolveFacingDirection()
        {
            if ( Mathf.Approximately( horizontalInput, 0.0f ) == false )
            {
                float inputDirection = Mathf.Sign( horizontalInput );
                return inputDirection;
            }

            float facingDirection = Mathf.Sign( transform.localScale.x );

            if ( Mathf.Approximately( facingDirection, 0.0f ) )
            {
                facingDirection = 1.0f;
            }

            float result = facingDirection;
            return result;
        }

        ///<summary>
        /// 지상 이동 상태 결정
        ///</summary>
        private ePlayerState ResolveGroundedLocomotionState()
        {
            ePlayerState result = HasHorizontalInput() ? ePlayerState.Move : ePlayerState.Idle;
            return result;
        }

        ///<summary>
        /// 원샷 입력 정리
        ///</summary>
        private void ClearOneShotInputs()
        {
            isPendingJump = false;
            isPendingAttack = false;
            pendingSkillSlotIndex = InvalidSkillSlotIndex;
        }

        ///<summary>
        /// NPC 상호작용 입력 차단 여부 반환
        ///</summary>
        private bool IsNpcInteractionBlockingControls()
        {
            if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager ) == false )
            {
                return false;
            }

            if ( interactionManager == null )
            {
                return false;
            }

            bool isBlocking = interactionManager.IsInteractionInProgress();
            return isBlocking;
        }

        ///<summary>
        /// NPC 상호작용 중 플레이어 상태 잠금
        ///</summary>
        private void ApplyNpcInteractionControlLock()
        {
            if ( IsNpcInteractionBlockingControls() == false )
            {
                return;
            }

            if ( currentState == ePlayerState.Die || currentState == ePlayerState.Hit )
            {
                return;
            }

            SetAttackHitColliderActive( false );

            if ( targetRigidbody != null )
            {
                Vector2 currentVelocity = targetRigidbody.linearVelocity;
                currentVelocity.x = 0.0f;
                targetRigidbody.linearVelocity = currentVelocity;
            }

            if ( currentState == ePlayerState.Jump && isGrounded == false )
            {
                return;
            }

            if ( currentState != ePlayerState.Idle )
            {
                ChangeState( ePlayerState.Idle );
            }
        }

        ///<summary>
        /// 대기 중인 스킬 슬롯 인덱스 결정
        ///</summary>
        private int ResolvePendingSkillSlotIndex( CInputManager _inputManager )
        {
            if ( _inputManager == null )
            {
                return InvalidSkillSlotIndex;
            }

            for ( int slotIndex = 0; slotIndex < MaxSkillSlotCount; slotIndex++ )
            {
                bool isSkillSlotDown = _inputManager.GetSkillSlotDown( slotIndex );

                if ( isSkillSlotDown == false )
                {
                    continue;
                }

                return slotIndex;
            }

            return InvalidSkillSlotIndex;
        }

        ///<summary>
        /// 대기 중인 스킬 입력 처리
        ///</summary>
        private bool TryProcessPendingSkillInput()
        {
            if ( pendingSkillSlotIndex == InvalidSkillSlotIndex || targetSkillManager == null )
            {
                return false;
            }

            eSkillUseResult useResult = targetSkillManager.TryUseSkillByQuickSlotIndex( pendingSkillSlotIndex );
            bool didUseSkill = useResult == eSkillUseResult.SUCCESS;
            return didUseSkill;
        }

        ///<summary>
        /// 애니메이션 상태 적용
        ///</summary>
        private void ApplyAnimationState()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            string animationStateName = ResolveAnimationStateName();
            targetAnimator.Play( animationStateName );
        }

        ///<summary>
        /// 애니메이션 상태 이름 결정
        ///</summary>
        private string ResolveAnimationStateName()
        {
            string animationStateName = IdleAnimationStateName;

            switch ( currentState )
            {
                case ePlayerState.Idle:
                    animationStateName = IdleAnimationStateName;
                    break;

                case ePlayerState.Move:
                    animationStateName = MoveAnimationStateName;
                    break;

                case ePlayerState.Attack:
                    animationStateName = AttackAnimationStateName;
                    break;

                case ePlayerState.Skill:
                    animationStateName = skillAnimationStateName;
                    break;

                case ePlayerState.Jump:
                    animationStateName = HasHorizontalInput() ? MoveAnimationStateName : IdleAnimationStateName;
                    break;

                case ePlayerState.Hit:
                    animationStateName = HitAnimationStateName;
                    break;

                case ePlayerState.Die:
                    animationStateName = DieAnimationStateName;
                    break;
            }

            return animationStateName;
        }

        ///<summary>
        /// 무적 상태 시작
        ///</summary>
        private void BeginInvincibility()
        {
            if ( invincibilityRoutine != null )
            {
                StopCoroutine( invincibilityRoutine );
                invincibilityRoutine = null;
            }

            isInvincible = true;

            if ( invincibilityDuration <= 0.0f )
            {
                isInvincible = false;
                RestoreSpriteColors();
                return;
            }

            invincibilityRoutine = StartCoroutine( IE_HandleInvincibilityVisual() );
        }

        ///<summary>
        /// 무적 시각 효과 중단 및 색상 복원
        ///</summary>
        private void StopInvincibilityVisual()
        {
            if ( invincibilityRoutine != null )
            {
                StopCoroutine( invincibilityRoutine );
                invincibilityRoutine = null;
            }

            isInvincible = false;
            RestoreSpriteColors();
        }

        ///<summary>
        /// 스킬 버프 지속시간 갱신
        ///</summary>
        private void TickSkillBuffState()
        {
            float deltaTime = Time.deltaTime;

            if ( skillFinalAttackBuffRemaining > 0.0f )
            {
                skillFinalAttackBuffRemaining = Mathf.Max( 0.0f, skillFinalAttackBuffRemaining - deltaTime );

                if ( skillFinalAttackBuffRemaining <= 0.0f )
                {
                    skillFinalAttackPercentBonus = 0.0f;
                }
            }

            if ( skillInvincibilityRemaining > 0.0f )
            {
                skillInvincibilityRemaining = Mathf.Max( 0.0f, skillInvincibilityRemaining - deltaTime );

                if ( isPhaseStrikeActive == false )
                {
                    ApplySkillInvincibilityTint();
                }

                if ( skillInvincibilityRemaining <= 0.0f && isInvincible == false && isPhaseStrikeActive == false )
                {
                    RestoreSpriteColors();
                }
            }
        }

        ///<summary>
        /// 스킬 버프 상태 초기화
        ///</summary>
        private void ClearSkillBuffState()
        {
            skillFinalAttackPercentBonus = 0.0f;
            skillFinalAttackBuffRemaining = 0.0f;
            skillInvincibilityRemaining = 0.0f;
        }

        ///<summary>
        /// 현재 무적 상태 여부 반환
        ///</summary>
        private bool IsAnyInvincibleStateActive()
        {
            bool result = isInvincible || skillInvincibilityRemaining > 0.0f || isPhaseStrikeActive;
            return result;
        }

        private void SetPlayerNameTagVisible( bool _isVisible )
        {
            if ( CPlayerNameTagManager.TryGetExistingInstance( out CPlayerNameTagManager playerNameTagManager ) == false || playerNameTagManager == null )
            {
                return;
            }

            playerNameTagManager.SetPlayerNameTagVisible( _isVisible );
        }

        private void ApplySkillInvincibilityTint()
        {
            if ( targetSpriteRenderers == null || defaultSpriteColors == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer != null )
                {
                    Color defaultColor = defaultSpriteColors[ index ];
                    spriteRenderer.color = Color.Lerp( defaultColor, skillInvincibilityTintColor, 0.5f );
                }
            }
        }

        ///<summary>
        /// 페이즈 스트라이크 제어 잠금 적용
        ///</summary>
        private void ApplyPhaseStrikeControlLock()
        {
            horizontalInput = 0.0f;
            isJumpHeld = false;
            isInteractionHeld = false;
            isPendingJump = false;
            isPendingAttack = false;
            pendingSkillSlotIndex = InvalidSkillSlotIndex;
            SetAttackHitColliderActive( false );

            if ( targetRigidbody != null )
            {
                targetRigidbody.linearVelocity = Vector2.zero;
            }
        }

        ///<summary>
        /// 피격 리액션 시작
        ///</summary>
        private void StartHitReaction()
        {
            StopHitReaction();
            isHitReactionActive = true;
            hitReactionRoutine = StartCoroutine( IE_PlayHitReaction() );
        }

        ///<summary>
        /// 피격 리액션 중단
        ///</summary>
        private void StopHitReaction()
        {
            if ( hitReactionRoutine != null )
            {
                StopCoroutine( hitReactionRoutine );
                hitReactionRoutine = null;
            }

            isHitReactionActive = false;
        }

        ///<summary>
        /// 피격 튕김 연출
        ///</summary>
        private IEnumerator IE_PlayHitReaction()
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + new Vector3( hitReactionDirection * hitKnockbackDistance, 0.0f, 0.0f );
            float elapsedTime = 0.0f;

            if ( hitStateDuration <= 0.0f )
            {
                transform.position = endPosition;
                isHitReactionActive = false;
                hitReactionRoutine = null;
                yield break;
            }

            while ( elapsedTime < hitStateDuration )
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01( elapsedTime / hitStateDuration );
                float easedTime = 1.0f - Mathf.Pow( 1.0f - normalizedTime, 2.0f );
                transform.position = Vector3.Lerp( startPosition, endPosition, easedTime );
                yield return null;
            }

            transform.position = endPosition;
            isHitReactionActive = false;
            hitReactionRoutine = null;
        }

        ///<summary>
        /// 무적 시각 효과 처리
        ///</summary>
        private IEnumerator IE_HandleInvincibilityVisual()
        {
            float elapsedTime = 0.0f;
            float blinkInterval = Mathf.Max( 0.01f, invincibilityBlinkInterval );

            while ( elapsedTime < invincibilityDuration )
            {
                bool useTint = Mathf.Repeat( elapsedTime, blinkInterval * 2.0f ) < blinkInterval;
                ApplyInvincibilityTint( useTint );
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            isInvincible = false;
            invincibilityRoutine = null;
            RestoreSpriteColors();
            EvaluateMonsterContactOverlap();
        }

        ///<summary>
        /// 플레이어 충돌체 캐시
        ///</summary>
        private void CacheTargetColliders()
        {
            if ( bodyCollider == null )
            {
                BoxCollider2D resolvedBodyCollider = ResolveBodyCollider();
                bodyCollider = resolvedBodyCollider;
            }

            if ( bodyCollider == null )
            {
                targetColliders = new Collider2D[ 0 ];
                return;
            }

            targetColliders = new Collider2D[] { bodyCollider };
        }

        ///<summary>
        /// 몬스터 접촉 중첩 검사
        ///</summary>
        private void EvaluateMonsterContactOverlap()
        {
            if ( isMonsterContactHitEnabled == false )
            {
                return;
            }

            if ( currentState == ePlayerState.Die || IsAnyInvincibleStateActive() )
            {
                return;
            }

            if ( targetColliders == null || targetColliders.Length == 0 )
            {
                return;
            }

            ContactFilter2D contactFilter = BuildMonsterContactFilter();

            for ( int index = 0; index < targetColliders.Length; index++ )
            {
                Collider2D playerCollider = targetColliders[ index ];

                if ( playerCollider == null || playerCollider.enabled == false )
                {
                    continue;
                }

                int overlapCount = playerCollider.Overlap( contactFilter, overlapResultBuffer );

                if ( overlapCount <= 0 )
                {
                    continue;
                }

                bool didReceiveHit = TryHandleMonsterContactOverlap( overlapCount );

                if ( didReceiveHit )
                {
                    return;
                }
            }
        }

        ///<summary>
        /// 몬스터 접촉 필터 구성
        ///</summary>
        private ContactFilter2D BuildMonsterContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = false;
            contactFilter.useTriggers = true;
            return contactFilter;
        }

        ///<summary>
        /// 플레이어 몸통 충돌체 결정
        ///</summary>
        private BoxCollider2D ResolveBodyCollider()
        {
            BoxCollider2D resolvedBodyCollider = GetComponent<BoxCollider2D>();
            return resolvedBodyCollider;
        }

        ///<summary>
        /// 몬스터 접촉 중첩 처리
        ///</summary>
        private bool TryHandleMonsterContactOverlap( int _overlapCount )
        {
            for ( int overlapIndex = 0; overlapIndex < _overlapCount; overlapIndex++ )
            {
                Collider2D overlapCollider = overlapResultBuffer[ overlapIndex ];

                if ( overlapCollider == null )
                {
                    continue;
                }

                MonsterContactHitbox monsterContactHitbox = overlapCollider.GetComponent<MonsterContactHitbox>();

                if ( monsterContactHitbox == null )
                {
                    monsterContactHitbox = overlapCollider.GetComponentInParent<MonsterContactHitbox>();
                }

                if ( monsterContactHitbox == null )
                {
                    continue;
                }

                MonsterObject monsterObject = overlapCollider.GetComponentInParent<MonsterObject>();
                bool didReceiveHit = TryReceiveContactHit( monsterObject );
                return didReceiveHit;
            }

            return false;
        }

        ///<summary>
        /// 스프라이트 렌더러 캐시
        ///</summary>
        private void CacheSpriteRenderers()
        {
            if ( targetSpriteRenderers != null && targetSpriteRenderers.Length > 0 )
            {
                return;
            }

            SpriteRenderer[] resolvedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>( true );
            targetSpriteRenderers = resolvedSpriteRenderers;
        }

        ///<summary>
        /// 기본 스프라이트 색상 캐시
        ///</summary>
        private void CacheDefaultSpriteColors()
        {
            if ( targetSpriteRenderers == null || targetSpriteRenderers.Length == 0 )
            {
                defaultSpriteColors = new Color[ 0 ];
                defaultSpriteColorRendererArray = new SpriteRenderer[ 0 ];
                return;
            }

            if ( IsDefaultSpriteColorCacheValid() )
            {
                return;
            }

            defaultSpriteColors = new Color[ targetSpriteRenderers.Length ];
            defaultSpriteColorRendererArray = new SpriteRenderer[ targetSpriteRenderers.Length ];

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];
                defaultSpriteColorRendererArray[ index ] = spriteRenderer;
                defaultSpriteColors[ index ] = spriteRenderer != null ? spriteRenderer.color : Color.white;
            }
        }

        ///<summary>
        /// 기본 스프라이트 색상 캐시 유효 여부 반환
        ///</summary>
        private bool IsDefaultSpriteColorCacheValid()
        {
            if ( defaultSpriteColors == null || defaultSpriteColorRendererArray == null )
            {
                return false;
            }

            if ( defaultSpriteColors.Length != targetSpriteRenderers.Length || defaultSpriteColorRendererArray.Length != targetSpriteRenderers.Length )
            {
                return false;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer cachedSpriteRenderer = defaultSpriteColorRendererArray[ index ];
                SpriteRenderer currentSpriteRenderer = targetSpriteRenderers[ index ];

                if ( cachedSpriteRenderer != currentSpriteRenderer )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 무적 색상 적용
        ///</summary>
        private void ApplyInvincibilityTint( bool _useTint )
        {
            if ( targetSpriteRenderers == null || defaultSpriteColors == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                if ( index >= defaultSpriteColors.Length )
                {
                    continue;
                }

                Color defaultColor = defaultSpriteColors[ index ];
                Color tintColor = Color.Lerp( defaultColor, invincibilityTintColor, 0.65f );
                Color appliedColor = _useTint ? tintColor : defaultColor;
                spriteRenderer.color = appliedColor;
            }
        }

        ///<summary>
        /// 스프라이트 색상 복원
        ///</summary>
        private void RestoreSpriteColors()
        {
            if ( targetSpriteRenderers == null || defaultSpriteColors == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                if ( index >= defaultSpriteColors.Length )
                {
                    continue;
                }

                spriteRenderer.color = defaultSpriteColors[ index ];
            }
        }

        ///<summary>
        /// 스프라이트 렌더러 표시 상태 설정
        ///</summary>
        private void SetSpriteRendererVisible( bool _isVisible )
        {
            if ( targetSpriteRenderers == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                spriteRenderer.enabled = _isVisible;
            }
        }

        ///<summary>
        /// 애니메이션 이벤트 수신기 구독
        ///</summary>
        private void SubscribeAnimationEventReceiver()
        {
            if ( animationEventReceiver == null )
            {
                return;
            }

            animationEventReceiver.OnAttackHitEvent -= HandleAttackHitEvent;
            animationEventReceiver.OnAttackHitEvent += HandleAttackHitEvent;
        }

        ///<summary>
        /// 애니메이션 이벤트 수신기 구독 해제
        ///</summary>
        private void UnsubscribeAnimationEventReceiver()
        {
            if ( animationEventReceiver == null )
            {
                return;
            }

            animationEventReceiver.OnAttackHitEvent -= HandleAttackHitEvent;
        }

        ///<summary>
        /// 공격 이벤트 처리
        ///</summary>
        private void HandleAttackHitEvent()
        {
            if ( currentState != ePlayerState.Attack )
            {
                return;
            }

            if ( OnAttackHitTriggered != null )
            {
                OnAttackHitTriggered();
            }

            PlayAttackSlashFx();

            MonsterObject attackTarget = ResolveHighestPriorityAttackTarget();

            if ( attackTarget == null )
            {
                return;
            }

            bool wasAliveBeforeHit = attackTarget.GetCurrentHp() > 0;
            long attackDamage = ResolveAttackDamage( attackTarget, out bool isCritical );
            attackTarget.TakeDamage( attackDamage, isCritical );

            if ( wasAliveBeforeHit && attackTarget.GetCurrentHp() <= 0 )
            {
                GrantMonsterReward( attackTarget );
            }
        }

        ///<summary>
        /// 공격 대상 결정
        ///</summary>
        private MonsterObject ResolveHighestPriorityAttackTarget()
        {
            if ( attackHitCollider == null )
            {
                return null;
            }

            SetAttackHitColliderActive( true );

            ContactFilter2D contactFilter = new ContactFilter2D();
            int monsterLayer = LayerMask.NameToLayer( "Monster" );
            contactFilter.useLayerMask = monsterLayer >= 0;
            contactFilter.useTriggers = true;
            contactFilter.layerMask = monsterLayer >= 0 ? LayerMask.GetMask( "Monster" ) : Physics2D.AllLayers;
            int overlapCount = attackHitCollider.Overlap( contactFilter, attackHitResultBuffer );
            MonsterObject highestPriorityMonster = null;
            int highestPriorityScore = int.MinValue;

            for ( int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++ )
            {
                Collider2D overlapCollider = attackHitResultBuffer[ overlapIndex ];
                MonsterObject monsterObject = ResolveMonsterObjectFromCollider( overlapCollider );

                if ( monsterObject == null )
                {
                    continue;
                }

                int priorityScore = ResolveMonsterPriorityScore( monsterObject );

                if ( priorityScore <= highestPriorityScore )
                {
                    continue;
                }

                highestPriorityScore = priorityScore;
                highestPriorityMonster = monsterObject;
            }

            SetAttackHitColliderActive( false );
            return highestPriorityMonster;
        }

        ///<summary>
        /// 공격 범위 콜라이더 결정
        ///</summary>
        private Collider2D ResolveAttackHitCollider()
        {
            Collider2D[] colliderArray = GetComponentsInChildren<Collider2D>( true );

            for ( int index = 0; index < colliderArray.Length; index++ )
            {
                Collider2D childCollider = colliderArray[ index ];

                if ( childCollider == null )
                {
                    continue;
                }

                if ( string.Equals( childCollider.gameObject.name, "AttackHitCollider", System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                return childCollider;
            }

            return null;
        }

        ///<summary>
        /// 공격 범위 활성 상태 설정
        ///</summary>
        private void ConfigureAttackHitCollider()
        {
            if ( attackHitCollider == null )
            {
                return;
            }

            attackHitCollider.isTrigger = true;
            int monsterLayer = LayerMask.NameToLayer( "Monster" );

            if ( monsterLayer >= 0 )
            {
                attackHitCollider.includeLayers = LayerMask.GetMask( "Monster" );
                attackHitCollider.excludeLayers = ~LayerMask.GetMask( "Monster" );
            }
        }

        ///<summary>
        /// 공격 콜라이더 기준값 캐시
        ///</summary>
        private void CacheAttackHitColliderBaseline()
        {
            if ( attackHitCollider == null || hasCachedAttackHitColliderBaseline )
            {
                return;
            }

            Transform attackColliderTransform = attackHitCollider.transform;

            if ( attackColliderTransform != null )
            {
                attackHitColliderBaseLocalPosition = attackColliderTransform.localPosition;
            }

            BoxCollider2D boxCollider = attackHitCollider as BoxCollider2D;

            if ( boxCollider != null )
            {
                attackHitColliderBaseBoxOffset = boxCollider.offset;
                attackHitColliderBaseBoxSize = boxCollider.size;
            }

            CircleCollider2D circleCollider = attackHitCollider as CircleCollider2D;

            if ( circleCollider != null )
            {
                attackHitColliderBaseCircleOffset = circleCollider.offset;
                attackHitColliderBaseCircleRadius = circleCollider.radius;
            }

            CapsuleCollider2D capsuleCollider = attackHitCollider as CapsuleCollider2D;

            if ( capsuleCollider != null )
            {
                attackHitColliderBaseCapsuleOffset = capsuleCollider.offset;
                attackHitColliderBaseCapsuleSize = capsuleCollider.size;
            }

            hasCachedAttackHitColliderBaseline = true;
        }

        ///<summary>
        /// 공격 콜라이더 범위 배율 적용
        ///</summary>
        private void ApplyAttackHitColliderRange()
        {
            if ( attackHitCollider == null )
            {
                return;
            }

            CacheAttackHitColliderBaseline();
            float rangeMultiplier = targetStatManager != null ? targetStatManager.GetRangeMultiplier() : 1.0f;
            rangeMultiplier = Mathf.Max( 0.1f, rangeMultiplier );
            Transform attackColliderTransform = attackHitCollider.transform;

            if ( attackColliderTransform != null )
            {
                Vector3 adjustedLocalPosition = attackHitColliderBaseLocalPosition;
                adjustedLocalPosition.x *= rangeMultiplier;
                attackColliderTransform.localPosition = adjustedLocalPosition;
            }

            BoxCollider2D boxCollider = attackHitCollider as BoxCollider2D;

            if ( boxCollider != null )
            {
                Vector2 adjustedOffset = attackHitColliderBaseBoxOffset;
                adjustedOffset.x *= rangeMultiplier;
                Vector2 adjustedSize = attackHitColliderBaseBoxSize;
                adjustedSize.x *= rangeMultiplier;
                boxCollider.offset = adjustedOffset;
                boxCollider.size = adjustedSize;
            }

            CircleCollider2D circleCollider = attackHitCollider as CircleCollider2D;

            if ( circleCollider != null )
            {
                Vector2 adjustedOffset = attackHitColliderBaseCircleOffset;
                adjustedOffset.x *= rangeMultiplier;
                circleCollider.offset = adjustedOffset;
                circleCollider.radius = attackHitColliderBaseCircleRadius * rangeMultiplier;
            }

            CapsuleCollider2D capsuleCollider = attackHitCollider as CapsuleCollider2D;

            if ( capsuleCollider != null )
            {
                Vector2 adjustedOffset = attackHitColliderBaseCapsuleOffset;
                adjustedOffset.x *= rangeMultiplier;
                Vector2 adjustedSize = attackHitColliderBaseCapsuleSize;

                if ( capsuleCollider.direction == CapsuleDirection2D.Horizontal )
                {
                    adjustedSize.x *= rangeMultiplier;
                }
                else
                {
                    adjustedSize.y *= rangeMultiplier;
                }

                capsuleCollider.offset = adjustedOffset;
                capsuleCollider.size = adjustedSize;
            }
        }

        ///<summary>
        /// 공격 범위 활성 상태 설정
        ///</summary>
        private void SetAttackHitColliderActive( bool _isActive )
        {
            if ( attackHitCollider == null )
            {
                return;
            }

            GameObject attackHitObject = attackHitCollider.gameObject;

            if ( attackHitObject.activeSelf == _isActive )
            {
                attackHitCollider.enabled = _isActive;
                return;
            }

            attackHitObject.SetActive( _isActive );
            attackHitCollider.enabled = _isActive;
        }

        ///<summary>
        /// 콜라이더 기반 몬스터 결정
        ///</summary>
        private MonsterObject ResolveMonsterObjectFromCollider( Collider2D _overlapCollider )
        {
            if ( _overlapCollider == null )
            {
                return null;
            }

            MonsterObject resolvedMonsterObject = _overlapCollider.GetComponent<MonsterObject>();

            if ( resolvedMonsterObject != null )
            {
                return resolvedMonsterObject;
            }

            MonsterObject resolvedParentMonsterObject = _overlapCollider.GetComponentInParent<MonsterObject>();
            return resolvedParentMonsterObject;
        }

        ///<summary>
        /// 몬스터 표시 우선순위 점수 계산
        ///</summary>
        private int ResolveMonsterPriorityScore( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return int.MinValue;
            }

            SpriteRenderer[] spriteRendererArray = _monsterObject.GetComponentsInChildren<SpriteRenderer>( true );
            int highestScore = int.MinValue;

            for ( int index = 0; index < spriteRendererArray.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRendererArray[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                int sortingLayerValue = SortingLayer.GetLayerValueFromID( spriteRenderer.sortingLayerID );
                int currentScore = sortingLayerValue * 10000 + spriteRenderer.sortingOrder;

                if ( currentScore > highestScore )
                {
                    highestScore = currentScore;
                }
            }

            return highestScore;
        }

        ///<summary>
        /// 플레이어 공격 피해량 계산
        ///</summary>
        private long ResolveAttackDamage( MonsterObject _monsterObject, out bool _isCritical )
        {
            _isCritical = false;

            if ( _monsterObject == null )
            {
                return 0L;
            }

            float playerAtk = targetStatManager != null ? targetStatManager.GetFinalStatValue( ePlayerStatType.ATK ) : 0.0f;
            float skillAttackPowerMultiplier = GetSkillAttackPowerMultiplier();
            float monsterDef = _monsterObject.GetDef();
            float rawDamage = playerAtk * skillAttackPowerMultiplier - monsterDef;
            float resolvedDamage = CPlayerCombatStatUtility.ResolveCombatDamage( targetStatManager, rawDamage, out bool isCritical );
            _isCritical = isCritical;
            long resolvedDamageValue = System.Math.Max( 0L, ( long )System.Math.Round( resolvedDamage ) );
            CSecureLong secureDamage = new CSecureLong( resolvedDamageValue );
            long result = secureDamage.Value;
            return result;
        }

        ///<summary>
        /// 공격 시작 가능 여부 반환
        ///</summary>
        private bool CanStartAttack()
        {
            bool result = Time.time >= nextAttackAvailableTime;
            return result;
        }

        ///<summary>
        /// 공격 주기 시간 결정
        ///</summary>
        private float ResolveAttackIntervalSeconds()
        {
            if ( targetStatManager == null )
            {
                return 1.0f;
            }

            float result = targetStatManager.GetAttackIntervalSeconds();
            return result;
        }

        ///<summary>
        /// 전투 효과음 선로딩
        ///</summary>
        private void PreloadCombatSfx()
        {
            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            audioManager.PreloadSfx( DefaultAttackSwingSfxClipName );
            audioManager.PreloadSfx( JumpSfxClipName );
        }

        ///<summary>
        /// 기본 공격 휘두름 효과음 재생
        ///</summary>
        private void PlayDefaultAttackSwingSfx()
        {
            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            audioManager.PlaySfx( DefaultAttackSwingSfxClipName );
        }

        ///<summary>
        /// 점프 효과음 재생
        ///</summary>
        private void PlayJumpSfx()
        {
            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            audioManager.PlaySfx( JumpSfxClipName );
        }

        ///<summary>
        /// 몬스터 처치 경험치 지급
        ///</summary>
        private void GrantMonsterReward( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            _monsterObject.TryGrantReward( this );
        }

        ///<summary>
        /// 접촉 피해량 적용
        ///</summary>
        private void ApplyMonsterContactDamage( MonsterObject _monsterObject )
        {
            if ( targetStatManager == null || _monsterObject == null )
            {
                return;
            }

            float playerDef = targetStatManager.GetFinalStatValue( ePlayerStatType.DEF );
            float rawDamage = _monsterObject.GetAtk() - playerDef;
            long resolvedDamageValue = System.Math.Max( 0L, ( long )System.Math.Round( Mathf.Max( 0.0f, rawDamage ) ) );
            CSecureLong secureDamage = new CSecureLong( resolvedDamageValue );
            long damage = secureDamage.Value;
            targetStatManager.ConsumeHp( damage );

            if ( damage > 0L && CDamageFontManager.TryGetInstance( out CDamageFontManager damageFontManager ) )
            {
                damageFontManager.ShowPlayerDamage( transform, damage );
            }
        }

        ///<summary>
        /// 공격 중 수평 속도 설정
        ///</summary>
        private void SetAttackHorizontalVelocity( float _horizontalVelocity )
        {
            if ( targetRigidbody == null )
            {
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.x = _horizontalVelocity;
            targetRigidbody.linearVelocity = currentVelocity;
        }

        ///<summary>
        /// 공격 애니메이션 속도 적용
        ///</summary>
        private void ApplyAttackAnimationSpeed()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            float statAttackAnimationSpeedMultiplier = targetStatManager != null ? targetStatManager.GetAttackAnimationSpeedMultiplier() : 1.0f;
            float resolvedSpeed = Mathf.Max( 0.01f, attackAnimationSpeedMultiplier * statAttackAnimationSpeedMultiplier );
            targetAnimator.speed = defaultAnimatorSpeed * resolvedSpeed;
        }

        ///<summary>
        /// 공격 애니메이션 종료 여부 반환
        ///</summary>
        private bool HasAttackAnimationFinished()
        {
            if ( targetAnimator == null )
            {
                return attackElapsedTime >= attackDuration;
            }

            AnimatorStateInfo animatorStateInfo = targetAnimator.GetCurrentAnimatorStateInfo( 0 );

            if ( animatorStateInfo.IsName( AttackAnimationStateName ) == false )
            {
                return false;
            }

            bool hasFinished = animatorStateInfo.normalizedTime >= 1.0f;
            return hasFinished;
        }

        ///<summary>
        /// 기본 애니메이션 속도 복원
        ///</summary>
        private void RestoreAnimatorSpeed()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            targetAnimator.speed = defaultAnimatorSpeed;
        }

        ///<summary>
        /// 스킬 애니메이션 속도 적용
        ///</summary>
        private void ApplySkillAnimationSpeed()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            float resolvedSpeed = Mathf.Max( 0.01f, skillCastAnimationSpeedMultiplier );
            targetAnimator.speed = defaultAnimatorSpeed * resolvedSpeed;
        }

        ///<summary>
        /// 스킬 애니메이션 이름 결정
        ///</summary>
        private string ResolveSkillAnimationStateName( string _animationStateName )
        {
            string result = string.IsNullOrWhiteSpace( _animationStateName ) ? AttackAnimationStateName : _animationStateName.Trim();
            return result;
        }

        ///<summary>
        /// 공격 이펙트 풀 초기화
        ///</summary>
        private void EnsureAttackSlashFxPoolInitialized()
        {
            if ( string.IsNullOrWhiteSpace( attackSlashFxPoolKey ) )
            {
                return;
            }

            GameObject loadedFxPrefab = Resources.Load<GameObject>( DefaultAttackSlashFxResourcePath );
            attackSlashFxPrefab = loadedFxPrefab;

            if ( attackSlashFxPrefab == null )
            {
                Debug.LogWarning( $"Attack slash FX prefab was not found at Resources/{DefaultAttackSlashFxResourcePath}.", this );
                return;
            }

            CObjectPoolManager.TryEnsurePoolRegistered<GameObject>( attackSlashFxPoolKey, CreateAttackSlashFxInstance, OnGetAttackSlashFxInstance, OnReleaseAttackSlashFxInstance );
        }

        ///<summary>
        /// 공격 이펙트 인스턴스 생성
        ///</summary>
        private GameObject CreateAttackSlashFxInstance()
        {
            if ( attackSlashFxPrefab == null )
            {
                return null;
            }

            GameObject createdFxObject = Instantiate( attackSlashFxPrefab );
            createdFxObject.name = attackSlashFxPrefab.name;
            createdFxObject.SetActive( false );
            return createdFxObject;
        }

        ///<summary>
        /// 공격 이펙트 대여 처리
        ///</summary>
        private void OnGetAttackSlashFxInstance( GameObject _fxObject )
        {
            if ( _fxObject == null )
            {
                return;
            }

            _fxObject.transform.SetParent( null, true );
            _fxObject.SetActive( true );

            if ( activeAttackSlashFxObjectList.Contains( _fxObject ) == false )
            {
                activeAttackSlashFxObjectList.Add( _fxObject );
            }
        }

        ///<summary>
        /// 공격 이펙트 반환 처리
        ///</summary>
        private void OnReleaseAttackSlashFxInstance( GameObject _fxObject )
        {
            if ( _fxObject == null )
            {
                return;
            }

            activeAttackSlashFxObjectList.Remove( _fxObject );
            _fxObject.transform.SetParent( null, true );
            _fxObject.SetActive( false );
        }

        ///<summary>
        /// 공격 이펙트 재생
        ///</summary>
        private void PlayAttackSlashFx()
        {
            if ( attackHitCollider == null )
            {
                return;
            }

            EnsureAttackSlashFxPoolInitialized();

            if ( CObjectPoolManager.TryGet( attackSlashFxPoolKey, out GameObject attackSlashFxObject ) == false || attackSlashFxObject == null )
            {
                return;
            }

            Transform fxTransform = attackSlashFxObject.transform;
            Transform attackColliderTransform = attackHitCollider.transform;
            fxTransform.position = attackColliderTransform.position;
            Quaternion fxRotation = attackSlashFxPrefab != null ? attackSlashFxPrefab.transform.rotation : Quaternion.identity;
            fxTransform.rotation = fxRotation;

            Vector3 fxScale = attackSlashFxPrefab != null ? attackSlashFxPrefab.transform.localScale : Vector3.one;
            float facingDirection = ResolveFacingDirection();
            float rangeMultiplier = targetStatManager != null ? targetStatManager.GetRangeMultiplier() : 1.0f;
            rangeMultiplier = Mathf.Max( 0.1f, rangeMultiplier );
            fxScale.x *= rangeMultiplier;
            fxScale.y *= rangeMultiplier;
            fxScale.z *= rangeMultiplier;
            fxScale.x = Mathf.Abs( fxScale.x ) * facingDirection;
            fxTransform.localScale = fxScale;

            float fxLifetime = Mathf.Max( 0.01f, attackSlashFxLifetime );
            StartCoroutine( IE_ReturnAttackSlashFxAfterDelay( attackSlashFxObject, fxLifetime ) );
        }

        ///<summary>
        /// 공격 이펙트 지연 반환
        ///</summary>
        private IEnumerator IE_ReturnAttackSlashFxAfterDelay( GameObject _fxObject, float _delay )
        {
            if ( _fxObject == null )
            {
                yield break;
            }

            yield return new WaitForSeconds( _delay );

            if ( string.IsNullOrWhiteSpace( attackSlashFxPoolKey ) )
            {
                yield break;
            }

            if ( activeAttackSlashFxObjectList.Contains( _fxObject ) == false )
            {
                yield break;
            }

            CObjectPoolManager.TryRelease( attackSlashFxPoolKey, _fxObject );
        }

        ///<summary>
        /// 선택 기즈모 표시
        ///</summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 groundCheckPosition = GetGroundCheckPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere( groundCheckPosition, groundCheckRadius );
        }
    }
}



