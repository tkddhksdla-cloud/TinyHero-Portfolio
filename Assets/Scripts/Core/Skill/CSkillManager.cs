using System;
using System.Collections;
using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 플레이어 스킬 상태 관리 컴포넌트
    ///</summary>
    [RequireComponent( typeof( CPlayerStatManager ) )]
    public sealed class CSkillManager : MonoBehaviour
    {
        [Header( "References" )]
        [SerializeField] private PlayerController targetPlayerController;
        [SerializeField] private CPlayerStatManager targetStatManager;
        [SerializeField] private CQuestStateProvider targetQuestStateProvider;

        [Header( "Skill Catalog" )]
        [SerializeField] private List<CSkillDefinition> skillDefinitionList = new List<CSkillDefinition>();

        [Header( "Runtime" )]
        [SerializeField] private List<CSkillRuntimeData> skillRuntimeDataList = new List<CSkillRuntimeData>();
        [SerializeField] private bool useDefaultSampleSkills = true;

        private readonly Dictionary<string, CSkillRuntimeData> skillRuntimeDataById = new Dictionary<string, CSkillRuntimeData>();
        private readonly CPlayerStatRuntimeData aggregatedPassiveStatBonus = new CPlayerStatRuntimeData();
        private readonly List<ScriptableObject> runtimeGeneratedScriptableObjectList = new List<ScriptableObject>();

        public event Action<CSkillDefinition> OnSkillUnlocked;
        public event Action<CSkillDefinition> OnSkillUsed;
        public event Action<CSkillDefinition> OnSkillExecuted;
        public event Action OnSkillStateChanged;

        ///<summary>
        /// 스킬 매니저 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultSampleSkills();
            RebuildRuntimeData();
            RefreshUnlockState();
        }

        ///<summary>
        /// 해금 조건 이벤트 구독
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            SubscribeRuntimeEvents();
            RefreshUnlockState();
        }

        ///<summary>
        /// 해금 조건 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeRuntimeEvents();
        }

        ///<summary>
        /// 런타임 생성 오브젝트 정리
        ///</summary>
        private void OnDestroy()
        {
            ReleaseRuntimeGeneratedObjects();
        }

        ///<summary>
        /// 스킬 개수 반환
        ///</summary>
        public int GetSkillCount()
        {
            int result = skillRuntimeDataList.Count;
            return result;
        }

        ///<summary>
        /// 인덱스 기반 스킬 런타임 데이터 반환
        ///</summary>
        public CSkillRuntimeData GetSkillRuntimeData( int _index )
        {
            if ( _index < 0 || _index >= skillRuntimeDataList.Count )
            {
                return null;
            }

            CSkillRuntimeData result = skillRuntimeDataList[ _index ];
            return result;
        }

        ///<summary>
        /// 식별자 기반 스킬 런타임 데이터 반환
        ///</summary>
        public CSkillRuntimeData GetSkillRuntimeData( string _skillId )
        {
            if ( string.IsNullOrWhiteSpace( _skillId ) )
            {
                return null;
            }

            bool hasRuntimeData = skillRuntimeDataById.TryGetValue( _skillId, out CSkillRuntimeData skillRuntimeData );

            if ( hasRuntimeData == false )
            {
                return null;
            }

            return skillRuntimeData;
        }

        ///<summary>
        /// 식별자 기반 스킬 정의 반환
        ///</summary>
        public CSkillDefinition GetSkillDefinition( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return null;
            }

            CSkillDefinition result = runtimeData.GetSkillDefinition();
            return result;
        }

        ///<summary>
        /// 퀵슬롯 인덱스 기반 스킬 정의 반환
        ///</summary>
        public CSkillDefinition GetSkillDefinitionByQuickSlotIndex( int _quickSlotIndex )
        {
            for ( int index = 0; index < skillRuntimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = skillRuntimeDataList[ index ];

                if ( runtimeData == null )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

                if ( skillDefinition == null )
                {
                    continue;
                }

                if ( skillDefinition.GetQuickSlotIndex() != _quickSlotIndex )
                {
                    continue;
                }

                return skillDefinition;
            }

            return null;
        }

        ///<summary>
        /// 퀵슬롯 인덱스 기반 스킬 사용 시도
        ///</summary>
        public eSkillUseResult TryUseSkillByQuickSlotIndex( int _quickSlotIndex )
        {
            CSkillDefinition skillDefinition = GetSkillDefinitionByQuickSlotIndex( _quickSlotIndex );

            if ( skillDefinition == null )
            {
                return eSkillUseResult.INVALID_SKILL;
            }

            string skillId = skillDefinition.GetSkillId();
            eSkillUseResult result = TryUseSkill( skillId );
            return result;
        }

        ///<summary>
        /// 스킬 해금 상태 반환
        ///</summary>
        public bool IsSkillUnlocked( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return false;
            }

            bool result = runtimeData.IsUnlocked();
            return result;
        }

        ///<summary>
        /// 스킬 남은 쿨타임 반환
        ///</summary>
        public float GetSkillCooldownRemaining( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return 0.0f;
            }

            float currentTime = Time.time;
            float result = runtimeData.GetRemainingCooldown( currentTime );
            return result;
        }

        ///<summary>
        /// 스킬 사용 가능 판정 결과 반환
        ///</summary>
        public eSkillUseResult GetSkillUseResult( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            eSkillUseResult result = EvaluateSkillUseResult( runtimeData );
            return result;
        }

        ///<summary>
        /// 인덱스 기반 스킬 사용 시도
        ///</summary>
        public eSkillUseResult TryUseSkill( int _index )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _index );
            eSkillUseResult result = TryUseSkillInternal( runtimeData );
            return result;
        }

        ///<summary>
        /// 식별자 기반 스킬 사용 시도
        ///</summary>
        public eSkillUseResult TryUseSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            eSkillUseResult result = TryUseSkillInternal( runtimeData );
            return result;
        }

        ///<summary>
        /// 스킬 정의 목록 교체
        ///</summary>
        public void SetSkillDefinitions( List<CSkillDefinition> _skillDefinitionList )
        {
            skillDefinitionList = _skillDefinitionList != null ? _skillDefinitionList : new List<CSkillDefinition>();
            RebuildRuntimeData();
            RefreshUnlockState();

            if ( OnSkillStateChanged != null )
            {
                OnSkillStateChanged();
            }
        }

        ///<summary>
        /// 참조 컴포넌트 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetStatManager == null )
            {
                CPlayerStatManager resolvedStatManager = GetComponent<CPlayerStatManager>();
                targetStatManager = resolvedStatManager;
            }

            if ( targetPlayerController == null )
            {
                PlayerController resolvedPlayerController = GetComponent<PlayerController>();
                targetPlayerController = resolvedPlayerController;
            }

            if ( targetQuestStateProvider == null )
            {
                CQuestStateProvider resolvedQuestStateProvider = GetComponent<CQuestStateProvider>();

                if ( resolvedQuestStateProvider == null )
                {
                    resolvedQuestStateProvider = FindFirstObjectByType<CQuestStateProvider>();
                }

                targetQuestStateProvider = resolvedQuestStateProvider;
            }
        }

        ///<summary>
        /// 기본 샘플 스킬 구성 보장
        ///</summary>
        private void EnsureDefaultSampleSkills()
        {
            if ( useDefaultSampleSkills == false )
            {
                return;
            }

            if ( skillDefinitionList != null && skillDefinitionList.Count > 0 )
            {
                return;
            }

            List<CSkillDefinition> defaultSkillDefinitionList = new List<CSkillDefinition>();
            CSkillDefinition flameSlashDefinition = CreateDefaultInstantSampleSkillDefinition();
            CSkillDefinition frostFieldDefinition = CreateDefaultPlaceSampleSkillDefinition();
            CSkillDefinition arcBoltDefinition = CreateDefaultProjectileSampleSkillDefinition();
            CSkillDefinition echoCloneDefinition = CreateDefaultCloneSampleSkillDefinition();
            defaultSkillDefinitionList.Add( flameSlashDefinition );
            defaultSkillDefinitionList.Add( frostFieldDefinition );
            defaultSkillDefinitionList.Add( arcBoltDefinition );
            defaultSkillDefinitionList.Add( echoCloneDefinition );
            skillDefinitionList = defaultSkillDefinitionList;
        }

        ///<summary>
        /// 기본 인스턴트 샘플 스킬 정의 생성
        ///</summary>
        private CSkillDefinition CreateDefaultInstantSampleSkillDefinition()
        {
            string skillId = "sample_flame_slash";
            string skillName = "Flame Slash";
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.name = skillName;
            runtimeGeneratedScriptableObjectList.Add( skillDefinition );

            CInstantActiveSkillEffect instantActiveSkillEffect = ScriptableObject.CreateInstance<CInstantActiveSkillEffect>();
            instantActiveSkillEffect.name = $"{skillName}_Effect";
            instantActiveSkillEffect.Configure( new Vector2( 1.25f, 0.0f ), 1.35f, 1.8f, 4, 16 );

            List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
            CDefReductionDebuffEffect defReductionDebuffEffect = ScriptableObject.CreateInstance<CDefReductionDebuffEffect>();
            defReductionDebuffEffect.name = $"{skillName}_DefDebuff";
            defReductionDebuffEffect.Configure( 4.0f, 0.2f, 0.35f );
            debuffEffectList.Add( defReductionDebuffEffect );
            instantActiveSkillEffect.SetDebuffEffects( debuffEffectList );
            runtimeGeneratedScriptableObjectList.Add( instantActiveSkillEffect );
            runtimeGeneratedScriptableObjectList.Add( defReductionDebuffEffect );

            Sprite skillIcon = CreateSolidColorSkillIcon( new Color32( 234, 107, 56, 255 ) );
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 0, 1, 2.0f, 10.0f, "전방 단발 공격 및 방어력 감소 부여", instantActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 1 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 기본 설치형 샘플 스킬 정의 생성
        ///</summary>
        private CSkillDefinition CreateDefaultPlaceSampleSkillDefinition()
        {
            string skillId = "sample_frost_field";
            string skillName = "Frost Field";
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.name = skillName;
            runtimeGeneratedScriptableObjectList.Add( skillDefinition );

            CPlaceActiveSkillEffect placeActiveSkillEffect = ScriptableObject.CreateInstance<CPlaceActiveSkillEffect>();
            placeActiveSkillEffect.name = $"{skillName}_Effect";
            placeActiveSkillEffect.Configure( new Vector2( 1.0f, 0.0f ), 2.0f, 5.0f, 1.0f, 1.25f, 2, 16 );

            List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
            CAtkReductionDebuffEffect atkReductionDebuffEffect = ScriptableObject.CreateInstance<CAtkReductionDebuffEffect>();
            atkReductionDebuffEffect.name = $"{skillName}_AtkDebuff";
            atkReductionDebuffEffect.Configure( 3.0f, 2, 0.4f );
            debuffEffectList.Add( atkReductionDebuffEffect );
            placeActiveSkillEffect.SetDebuffEffects( debuffEffectList );
            runtimeGeneratedScriptableObjectList.Add( placeActiveSkillEffect );
            runtimeGeneratedScriptableObjectList.Add( atkReductionDebuffEffect );

            Sprite skillIcon = CreateSolidColorSkillIcon( new Color32( 79, 176, 255, 255 ) );
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 1, 2, 5.0f, 18.0f, "설치 후 주기 피해 및 공격력 감소 부여", placeActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 2 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 기본 발사체 샘플 스킬 정의 생성
        ///</summary>
        private CSkillDefinition CreateDefaultProjectileSampleSkillDefinition()
        {
            string skillId = "sample_arc_bolt";
            string skillName = "Arc Bolt";
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.name = skillName;
            runtimeGeneratedScriptableObjectList.Add( skillDefinition );

            CProjectileActiveSkillEffect projectileActiveSkillEffect = ScriptableObject.CreateInstance<CProjectileActiveSkillEffect>();
            projectileActiveSkillEffect.name = $"{skillName}_Effect";
            projectileActiveSkillEffect.Configure( new Vector2( 0.7f, 0.2f ), 0.45f, 6.0f, 10.5f, 1.35f, 3, 1 );
            runtimeGeneratedScriptableObjectList.Add( projectileActiveSkillEffect );

            Sprite skillIcon = CreateSolidColorSkillIcon( new Color32( 104, 255, 181, 255 ) );
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 2, 3, 3.0f, 12.0f, "Projectile skill that travels forward and damages the first target hit.", projectileActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 3 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 기본 분신 샘플 스킬 정의 생성
        ///</summary>
        private CSkillDefinition CreateDefaultCloneSampleSkillDefinition()
        {
            string skillId = "sample_echo_clone";
            string skillName = "Echo Clone";
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.name = skillName;
            runtimeGeneratedScriptableObjectList.Add( skillDefinition );

            CCloneReplayActiveSkillEffect cloneReplayActiveSkillEffect = ScriptableObject.CreateInstance<CCloneReplayActiveSkillEffect>();
            cloneReplayActiveSkillEffect.name = $"{skillName}_Effect";
            cloneReplayActiveSkillEffect.Configure( 6.0f, 0.45f, 0.65f, new Vector3( -0.35f, 0.0f, 0.0f ), 0.85f );
            runtimeGeneratedScriptableObjectList.Add( cloneReplayActiveSkillEffect );

            Sprite skillIcon = CreateSolidColorSkillIcon( new Color32( 173, 139, 255, 255 ) );
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 3, 4, 12.0f, 24.0f, "Summons a delayed replay clone that mimics movement, attacks, and skills.", cloneReplayActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 4 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 단색 스킬 아이콘 생성
        ///</summary>
        private Sprite CreateSolidColorSkillIcon( Color32 _iconColor )
        {
            Texture2D texture = new Texture2D( 32, 32, TextureFormat.RGBA32, false );
            texture.name = "RuntimeSkillIcon";
            texture.filterMode = FilterMode.Point;
            Color32[] pixelArray = new Color32[ 32 * 32 ];

            for ( int index = 0; index < pixelArray.Length; index++ )
            {
                pixelArray[ index ] = _iconColor;
            }

            texture.SetPixels32( pixelArray );
            texture.Apply();

            Rect spriteRect = new Rect( 0.0f, 0.0f, texture.width, texture.height );
            Vector2 spritePivot = new Vector2( 0.5f, 0.5f );
            Sprite skillIcon = Sprite.Create( texture, spriteRect, spritePivot, 32.0f );
            return skillIcon;
        }

        ///<summary>
        /// 런타임 이벤트 구독
        ///</summary>
        private void SubscribeRuntimeEvents()
        {
            if ( targetStatManager != null )
            {
                targetStatManager.OnLevelExpChanged -= HandleLevelExpChanged;
                targetStatManager.OnLevelExpChanged += HandleLevelExpChanged;
            }

            if ( targetQuestStateProvider == null )
            {
                return;
            }

            targetQuestStateProvider.OnQuestStateChanged -= HandleQuestStateChanged;
            targetQuestStateProvider.OnQuestStateChanged += HandleQuestStateChanged;
        }

        ///<summary>
        /// 런타임 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeRuntimeEvents()
        {
            if ( targetStatManager != null )
            {
                targetStatManager.OnLevelExpChanged -= HandleLevelExpChanged;
            }

            if ( targetQuestStateProvider == null )
            {
                return;
            }

            targetQuestStateProvider.OnQuestStateChanged -= HandleQuestStateChanged;
        }

        ///<summary>
        /// 레벨 변경 반영 처리
        ///</summary>
        private void HandleLevelExpChanged( int _level, float _currentExp, float _maxExp )
        {
            RefreshUnlockState();
        }

        ///<summary>
        /// 퀘스트 상태 변경 반영 처리
        ///</summary>
        private void HandleQuestStateChanged()
        {
            RefreshUnlockState();
        }

        ///<summary>
        /// 런타임 스킬 캐시 재구성
        ///</summary>
        private void RebuildRuntimeData()
        {
            skillRuntimeDataById.Clear();

            if ( skillRuntimeDataList == null )
            {
                skillRuntimeDataList = new List<CSkillRuntimeData>();
            }
            else
            {
                skillRuntimeDataList.Clear();
            }

            if ( skillDefinitionList == null )
            {
                return;
            }

            for ( int index = 0; index < skillDefinitionList.Count; index++ )
            {
                CSkillDefinition skillDefinition = skillDefinitionList[ index ];

                if ( skillDefinition == null )
                {
                    continue;
                }

                string skillId = skillDefinition.GetSkillId();

                if ( string.IsNullOrWhiteSpace( skillId ) )
                {
                    continue;
                }

                if ( skillRuntimeDataById.ContainsKey( skillId ) )
                {
                    Debug.LogWarning( $"Duplicated skill id was ignored: {skillId}", this );
                    continue;
                }

                CSkillRuntimeData runtimeData = new CSkillRuntimeData();
                runtimeData.SetSkillDefinition( skillDefinition );
                runtimeData.SetSkillLevel( 1 );
                skillRuntimeDataList.Add( runtimeData );
                skillRuntimeDataById.Add( skillId, runtimeData );
            }
        }

        ///<summary>
        /// 해금 상태 갱신
        ///</summary>
        private void RefreshUnlockState()
        {
            int currentLevel = targetStatManager != null ? targetStatManager.GetCurrentLevel() : 1;
            bool didUnlockAnySkill = false;
            bool hasPassiveStateChanged = false;

            for ( int index = 0; index < skillRuntimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = skillRuntimeDataList[ index ];

                if ( runtimeData == null )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

                if ( skillDefinition == null )
                {
                    continue;
                }

                bool canUnlock = skillDefinition.AreUnlockConditionsSatisfied( this, currentLevel, targetQuestStateProvider );
                bool isAlreadyUnlocked = runtimeData.IsUnlocked();

                if ( canUnlock == false || isAlreadyUnlocked )
                {
                    continue;
                }

                runtimeData.SetUnlocked( true );
                didUnlockAnySkill = true;

                if ( skillDefinition.GetSkillType() == eSkillType.PASSIVE )
                {
                    hasPassiveStateChanged = true;
                }

                if ( OnSkillUnlocked != null )
                {
                    OnSkillUnlocked( skillDefinition );
                }
            }

            if ( hasPassiveStateChanged )
            {
                RebuildPassiveStatBonus();
            }

            if ( didUnlockAnySkill && OnSkillStateChanged != null )
            {
                OnSkillStateChanged();
            }
        }

        ///<summary>
        /// 패시브 스탯 보너스 재계산
        ///</summary>
        private void RebuildPassiveStatBonus()
        {
            aggregatedPassiveStatBonus.Clear();

            for ( int index = 0; index < skillRuntimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = skillRuntimeDataList[ index ];

                if ( runtimeData == null || runtimeData.IsUnlocked() == false )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

                if ( skillDefinition == null || skillDefinition.GetSkillType() != eSkillType.PASSIVE )
                {
                    continue;
                }

                CPlayerStatRuntimeData passiveStatBonus = skillDefinition.GetPassiveStatBonus();
                AddPassiveStatBonus( passiveStatBonus );
                AddPassiveSkillEffects( skillDefinition.GetPassiveSkillEffectList() );
            }

            if ( targetStatManager == null )
            {
                return;
            }

            targetStatManager.ApplySkillStatBonus( aggregatedPassiveStatBonus );
        }

        ///<summary>
        /// 레거시 패시브 스탯 보너스 누적
        ///</summary>
        private void AddPassiveStatBonus( CPlayerStatRuntimeData _passiveStatBonus )
        {
            if ( _passiveStatBonus == null )
            {
                return;
            }

            Array statTypeArray = Enum.GetValues( typeof( ePlayerStatType ) );

            for ( int index = 0; index < statTypeArray.Length; index++ )
            {
                ePlayerStatType statType = ( ePlayerStatType ) statTypeArray.GetValue( index );
                float statValue = _passiveStatBonus.GetStatValue( statType );

                if ( Mathf.Approximately( statValue, 0.0f ) )
                {
                    continue;
                }

                aggregatedPassiveStatBonus.AddStatValue( statType, statValue );
            }
        }

        ///<summary>
        /// 패시브 효과 목록 일괄 반영
        ///</summary>
        private void AddPassiveSkillEffects( List<CPassiveSkillEffectBase> _passiveSkillEffectList )
        {
            if ( _passiveSkillEffectList == null )
            {
                return;
            }

            for ( int index = 0; index < _passiveSkillEffectList.Count; index++ )
            {
                CPassiveSkillEffectBase passiveSkillEffect = _passiveSkillEffectList[ index ];

                if ( passiveSkillEffect == null )
                {
                    continue;
                }

                passiveSkillEffect.ApplyPassiveEffect( aggregatedPassiveStatBonus );
            }
        }

        ///<summary>
        /// 스킬 사용 처리
        ///</summary>
        private eSkillUseResult TryUseSkillInternal( CSkillRuntimeData _runtimeData )
        {
            eSkillUseResult precheckResult = EvaluateSkillUseResult( _runtimeData );

            if ( precheckResult != eSkillUseResult.SUCCESS )
            {
                return precheckResult;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();
            CActiveSkillEffectBase activeSkillEffect = skillDefinition.GetActiveSkillEffect();
            CSkillActionBase activeAction = skillDefinition.GetActiveAction();
            CSkillContext skillContext = CreateSkillContext( skillDefinition, _runtimeData );
            float mpCost = skillDefinition.GetMpCost();
            bool didConsumeMp = targetStatManager.TryConsumeMp( mpCost );

            if ( didConsumeMp == false )
            {
                return eSkillUseResult.NOT_ENOUGH_MP;
            }

            float castLockDurationSeconds = skillDefinition.GetCastLockDurationSeconds();
            bool shouldUseCastFlow = targetPlayerController != null && castLockDurationSeconds > 0.0f;

            if ( shouldUseCastFlow )
            {
                bool didBeginCast = TryBeginSkillCast( skillDefinition );

                if ( didBeginCast == false )
                {
                    targetStatManager.RecoverMp( mpCost );
                    return eSkillUseResult.BLOCKED;
                }

                float acceptedTime = Time.time;
                _runtimeData.MarkUsed( acceptedTime );
                NotifySkillUse( skillDefinition );
                StartCoroutine( IE_ExecuteSkillAfterCastDelay( activeSkillEffect, activeAction, skillContext, castLockDurationSeconds ) );
                return eSkillUseResult.SUCCESS;
            }

            bool didExecute = ExecuteSkillContents( activeSkillEffect, activeAction, skillContext );

            if ( didExecute == false )
            {
                targetStatManager.RecoverMp( mpCost );
                return eSkillUseResult.BLOCKED;
            }

            float currentTime = Time.time;
            _runtimeData.MarkUsed( currentTime );
            NotifySkillUse( skillDefinition );
            return eSkillUseResult.SUCCESS;
        }

        ///<summary>
        /// 스킬 사용 가능 판정
        ///</summary>
        private eSkillUseResult EvaluateSkillUseResult( CSkillRuntimeData _runtimeData )
        {
            if ( _runtimeData == null )
            {
                return eSkillUseResult.INVALID_SKILL;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return eSkillUseResult.INVALID_SKILL;
            }

            if ( _runtimeData.IsUnlocked() == false )
            {
                return eSkillUseResult.LOCKED;
            }

            if ( skillDefinition.GetSkillType() == eSkillType.PASSIVE )
            {
                return eSkillUseResult.PASSIVE_SKILL;
            }

            CActiveSkillEffectBase activeSkillEffect = skillDefinition.GetActiveSkillEffect();
            CSkillActionBase activeAction = skillDefinition.GetActiveAction();

            if ( activeSkillEffect == null && activeAction == null )
            {
                return eSkillUseResult.MISSING_ACTION;
            }

            float currentTime = Time.time;

            if ( _runtimeData.IsOnCooldown( currentTime ) )
            {
                return eSkillUseResult.COOLDOWN;
            }

            if ( targetStatManager == null )
            {
                return eSkillUseResult.BLOCKED;
            }

            if ( targetPlayerController != null && targetPlayerController.CurrentState == PlayerController.ePlayerState.Skill )
            {
                return eSkillUseResult.BLOCKED;
            }

            float currentMp = targetStatManager.GetCurrentMp();
            float mpCost = skillDefinition.GetMpCost();

            if ( currentMp < mpCost )
            {
                return eSkillUseResult.NOT_ENOUGH_MP;
            }

            CSkillContext skillContext = CreateSkillContext( skillDefinition, _runtimeData );
            bool canExecute = activeSkillEffect != null ? activeSkillEffect.CanExecute( skillContext ) : activeAction.CanExecute( skillContext );
            eSkillUseResult result = canExecute ? eSkillUseResult.SUCCESS : eSkillUseResult.BLOCKED;
            return result;
        }

        ///<summary>
        /// 스킬 실행 문맥 생성
        ///</summary>
        ///<summary>
        /// 스킬 캐스팅 시작 처리
        ///</summary>
        private bool TryBeginSkillCast( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null || targetPlayerController == null )
            {
                return false;
            }

            string castAnimationName = _skillDefinition.GetResolvedCastAnimationName();
            float castAnimationSpeed = _skillDefinition.GetCastAnimationSpeed();
            float castLockDurationSeconds = _skillDefinition.GetCastLockDurationSeconds();
            bool result = targetPlayerController.TryBeginToolSkillCast( castAnimationName, castAnimationSpeed, castLockDurationSeconds );
            return result;
        }

        ///<summary>
        /// 캐스팅 종료 후 스킬 실행 코루틴
        ///</summary>
        private IEnumerator IE_ExecuteSkillAfterCastDelay( CActiveSkillEffectBase _activeSkillEffect, CSkillActionBase _activeAction, CSkillContext _skillContext, float _castLockDurationSeconds )
        {
            if ( _castLockDurationSeconds > 0.0f )
            {
                yield return new WaitForSeconds( _castLockDurationSeconds );
            }

            ExecuteSkillContents( _activeSkillEffect, _activeAction, _skillContext );
        }

        ///<summary>
        /// 스킬 본체 실행 처리
        ///</summary>
        private bool ExecuteSkillContents( CActiveSkillEffectBase _activeSkillEffect, CSkillActionBase _activeAction, CSkillContext _skillContext )
        {
            if ( _activeSkillEffect != null )
            {
                bool didExecuteEffect = _activeSkillEffect.Execute( _skillContext );

                if ( didExecuteEffect )
                {
                    NotifySkillExecuted( _skillContext );
                }

                return didExecuteEffect;
            }

            if ( _activeAction != null )
            {
                bool didExecuteAction = _activeAction.Execute( _skillContext );

                if ( didExecuteAction )
                {
                    NotifySkillExecuted( _skillContext );
                }

                return didExecuteAction;
            }

            return false;
        }

        ///<summary>
        /// 스킬 사용 이벤트 통지
        ///</summary>
        private void NotifySkillUse( CSkillDefinition _skillDefinition )
        {
            if ( OnSkillUsed != null )
            {
                OnSkillUsed( _skillDefinition );
            }

            if ( OnSkillStateChanged != null )
            {
                OnSkillStateChanged();
            }
        }

        ///<summary>
        /// 스킬 실제 실행 이벤트 전파
        ///</summary>
        private void NotifySkillExecuted( CSkillContext _skillContext )
        {
            if ( _skillContext == null || OnSkillExecuted == null )
            {
                return;
            }

            CSkillDefinition skillDefinition = _skillContext.GetSkillDefinition();
            OnSkillExecuted( skillDefinition );
        }

        ///<summary>
        /// 스킬 실행 문맥 생성
        ///</summary>
        private CSkillContext CreateSkillContext( CSkillDefinition _skillDefinition, CSkillRuntimeData _runtimeData )
        {
            Transform ownerTransform = transform;
            CSkillContext skillContext = new CSkillContext( this, targetPlayerController, targetStatManager, _skillDefinition, _runtimeData, ownerTransform );
            return skillContext;
        }

        ///<summary>
        /// 레벨 해금 조건 목록 생성
        ///</summary>
        private List<CSkillUnlockConditionBase> CreateLevelUnlockConditionList( int _requiredLevel )
        {
            List<CSkillUnlockConditionBase> unlockConditionList = new List<CSkillUnlockConditionBase>();
            CLevelUnlockCondition levelUnlockCondition = ScriptableObject.CreateInstance<CLevelUnlockCondition>();
            levelUnlockCondition.name = $"LevelUnlock_{_requiredLevel}";
            levelUnlockCondition.Configure( _requiredLevel );
            unlockConditionList.Add( levelUnlockCondition );
            runtimeGeneratedScriptableObjectList.Add( levelUnlockCondition );
            return unlockConditionList;
        }

        ///<summary>
        /// 런타임 생성 오브젝트 해제
        ///</summary>
        private void ReleaseRuntimeGeneratedObjects()
        {
            for ( int index = 0; index < runtimeGeneratedScriptableObjectList.Count; index++ )
            {
                ScriptableObject runtimeGeneratedObject = runtimeGeneratedScriptableObjectList[ index ];

                if ( runtimeGeneratedObject == null )
                {
                    continue;
                }

                Destroy( runtimeGeneratedObject );
            }

            runtimeGeneratedScriptableObjectList.Clear();
        }
    }
}
