using System;
using System.Collections;
using System.Collections.Generic;
using TinyHero.Core;
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

        [Header( "Skill Progression" )]
        [SerializeField] private int currentSkillPoint;
        [SerializeField] private int initialSkillPoint;
        [SerializeField] private int skillPointPerLevelUp = 1;
        [SerializeField] private int lastGrantedPlayerLevel = 1;

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
            InitializeSkillProgression();
            RebuildPassiveStatBonus();
            RaiseSkillStateChanged();
        }

        ///<summary>
        /// 해금 조건 이벤트 구독
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            EnsureRuntimeDataIntegrity();
            SubscribeRuntimeEvents();
            RebuildPassiveStatBonus();
            RaiseSkillStateChanged();
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
            EnsureRuntimeDataIntegrity();
            int result = skillRuntimeDataList.Count;
            return result;
        }

        ///<summary>
        /// 인덱스 기반 스킬 런타임 데이터 반환
        ///</summary>
        public CSkillRuntimeData GetSkillRuntimeData( int _index )
        {
            EnsureRuntimeDataIntegrity();

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
            EnsureRuntimeDataIntegrity();

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

                if ( runtimeData.GetAssignedQuickSlotIndex() != _quickSlotIndex )
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
        /// 현재 스킬 포인트 반환
        ///</summary>
        public int GetCurrentSkillPoint()
        {
            int result = Mathf.Max( 0, currentSkillPoint );
            return result;
        }

        ///<summary>
        /// 스킬 레벨 반환
        ///</summary>
        public int GetSkillLevel( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return 0;
            }

            int result = runtimeData.GetSkillLevel();
            return result;
        }

        ///<summary>
        /// 스킬 배정 퀵슬롯 인덱스 반환
        ///</summary>
        public int GetAssignedQuickSlotIndex( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return -1;
            }

            int result = runtimeData.GetAssignedQuickSlotIndex();
            return result;
        }

        ///<summary>
        /// 스킬 동적 설명 문자열 반환
        ///</summary>
        public string GetFormattedSkillDescription( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                return string.Empty;
            }

            CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return string.Empty;
            }

            int skillLevel = runtimeData.IsUnlocked() ? Mathf.Max( 1, runtimeData.GetSkillLevel() ) : 1;
            string result = skillDefinition.GetFormattedDescription( skillLevel );
            return result;
        }

        ///<summary>
        /// 스킬 학습 가능 여부 반환
        ///</summary>
        public bool CanLearnSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            bool result = CanLearnSkill( runtimeData );
            return result;
        }

        ///<summary>
        /// 스킬 레벨업 가능 여부 반환
        ///</summary>
        public bool CanLevelUpSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            bool result = CanLevelUpSkill( runtimeData );
            return result;
        }

        ///<summary>
        /// 스킬 학습 처리
        ///</summary>
        public bool TryLearnSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            bool result = TryLearnSkill( runtimeData, false );
            return result;
        }

        ///<summary>
        /// 스킬 강제 학습 처리
        ///</summary>
        public bool TryForceLearnSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            bool result = TryLearnSkill( runtimeData, true );
            return result;
        }

        ///<summary>
        /// 스킬 레벨업 처리
        ///</summary>
        public bool TryLevelUpSkill( string _skillId )
        {
            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );
            bool result = TryLevelUpSkill( runtimeData );
            return result;
        }

        ///<summary>
        /// 스킬 퀵슬롯 배정 가능 여부 반환
        ///</summary>
        public bool CanAssignSkillToQuickSlot( string _skillId, int _quickSlotIndex )
        {
            if ( _quickSlotIndex < 0 )
            {
                return false;
            }

            CSkillRuntimeData runtimeData = GetSkillRuntimeData( _skillId );

            if ( runtimeData == null || runtimeData.IsUnlocked() == false )
            {
                return false;
            }

            CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

            if ( skillDefinition == null || skillDefinition.GetSkillType() != eSkillType.ACTIVE || skillDefinition.IsAssignableToQuickSlot() == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 스킬 퀵슬롯 배정 처리
        ///</summary>
        public bool TryAssignSkillToQuickSlot( string _skillId, int _quickSlotIndex )
        {
            if ( CanAssignSkillToQuickSlot( _skillId, _quickSlotIndex ) == false )
            {
                return false;
            }

            CSkillRuntimeData sourceRuntimeData = GetSkillRuntimeData( _skillId );

            if ( sourceRuntimeData == null )
            {
                return false;
            }

            CSkillRuntimeData occupiedRuntimeData = GetSkillRuntimeDataByQuickSlotIndex( _quickSlotIndex );

            if ( occupiedRuntimeData == sourceRuntimeData )
            {
                return true;
            }

            int previousQuickSlotIndex = sourceRuntimeData.GetAssignedQuickSlotIndex();

            if ( occupiedRuntimeData != null )
            {
                occupiedRuntimeData.SetAssignedQuickSlotIndex( previousQuickSlotIndex );
            }

            sourceRuntimeData.SetAssignedQuickSlotIndex( _quickSlotIndex );
            RaiseSkillStateChanged();
            return true;
        }

        ///<summary>
        /// 퀵슬롯 간 스킬 배치 교체 처리
        ///</summary>
        public bool TrySwapQuickSlotAssignments( int _fromQuickSlotIndex, int _toQuickSlotIndex )
        {
            if ( _fromQuickSlotIndex < 0 || _toQuickSlotIndex < 0 || _fromQuickSlotIndex == _toQuickSlotIndex )
            {
                return false;
            }

            CSkillRuntimeData fromRuntimeData = GetSkillRuntimeDataByQuickSlotIndex( _fromQuickSlotIndex );
            CSkillRuntimeData toRuntimeData = GetSkillRuntimeDataByQuickSlotIndex( _toQuickSlotIndex );

            if ( fromRuntimeData == null && toRuntimeData == null )
            {
                return false;
            }

            if ( fromRuntimeData != null )
            {
                fromRuntimeData.SetAssignedQuickSlotIndex( _toQuickSlotIndex );
            }

            if ( toRuntimeData != null )
            {
                toRuntimeData.SetAssignedQuickSlotIndex( _fromQuickSlotIndex );
            }

            RaiseSkillStateChanged();
            return true;
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
            RebuildPassiveStatBonus();
            RaiseSkillStateChanged();
        }

        ///<summary>
        /// 플레이어 스킬 저장 데이터 생성
        ///</summary>
        public CSkillSnapshotData CreateSnapshotData()
        {
            EnsureRuntimeDataIntegrity();
            CSkillSnapshotData snapshotData = new CSkillSnapshotData();
            snapshotData.currentSkillPoint = currentSkillPoint;
            snapshotData.lastGrantedPlayerLevel = lastGrantedPlayerLevel;
            int runtimeDataCount = skillRuntimeDataList.Count;

            for ( int index = 0; index < runtimeDataCount; index++ )
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

                CSkillRuntimeSnapshotEntryData snapshotEntryData = new CSkillRuntimeSnapshotEntryData();
                snapshotEntryData.skillId = skillDefinition.GetSkillId();
                snapshotEntryData.isUnlocked = runtimeData.IsUnlocked();
                snapshotEntryData.skillLevel = runtimeData.GetSkillLevel();
                snapshotEntryData.assignedQuickSlotIndex = runtimeData.GetAssignedQuickSlotIndex();
                snapshotData.skillRuntimeEntryList.Add( snapshotEntryData );
            }

            return snapshotData;
        }

        ///<summary>
        /// 플레이어 스킬 저장 데이터 로드
        ///</summary>
        public void LoadSnapshotData( CSkillSnapshotData _snapshotData )
        {
            EnsureRuntimeDataIntegrity();
            currentSkillPoint = _snapshotData != null ? Mathf.Max( 0, _snapshotData.currentSkillPoint ) : 0;
            lastGrantedPlayerLevel = _snapshotData != null ? Mathf.Max( 1, _snapshotData.lastGrantedPlayerLevel ) : 1;
            Dictionary<string, CSkillRuntimeSnapshotEntryData> snapshotEntryBySkillId = new Dictionary<string, CSkillRuntimeSnapshotEntryData>();

            if ( _snapshotData != null && _snapshotData.skillRuntimeEntryList != null )
            {
                int snapshotEntryCount = _snapshotData.skillRuntimeEntryList.Count;

                for ( int index = 0; index < snapshotEntryCount; index++ )
                {
                    CSkillRuntimeSnapshotEntryData snapshotEntryData = _snapshotData.skillRuntimeEntryList[ index ];

                    if ( snapshotEntryData == null || string.IsNullOrWhiteSpace( snapshotEntryData.skillId ) )
                    {
                        continue;
                    }

                    snapshotEntryBySkillId[ snapshotEntryData.skillId.Trim() ] = snapshotEntryData;
                }
            }

            int runtimeDataCount = skillRuntimeDataList.Count;

            for ( int index = 0; index < runtimeDataCount; index++ )
            {
                CSkillRuntimeData runtimeData = skillRuntimeDataList[ index ];

                if ( runtimeData == null )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();
                string skillId = skillDefinition != null ? skillDefinition.GetSkillId() : string.Empty;
                runtimeData.SetUnlocked( false );
                runtimeData.SetSkillLevel( 0 );
                runtimeData.SetAssignedQuickSlotIndex( ResolveInitialQuickSlotIndex( skillDefinition ) );

                if ( string.IsNullOrWhiteSpace( skillId ) )
                {
                    continue;
                }

                bool hasSnapshotEntry = snapshotEntryBySkillId.TryGetValue( skillId, out CSkillRuntimeSnapshotEntryData snapshotEntryData );

                if ( hasSnapshotEntry == false || snapshotEntryData == null )
                {
                    continue;
                }

                runtimeData.SetUnlocked( snapshotEntryData.isUnlocked );
                runtimeData.SetSkillLevel( Mathf.Max( 0, snapshotEntryData.skillLevel ) );
                runtimeData.SetAssignedQuickSlotIndex( snapshotEntryData.assignedQuickSlotIndex );
            }

            RebuildPassiveStatBonus();
            RaiseSkillStateChanged();
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
            CSkillDefinition phaseStrikeDefinition = CreateDefaultPhaseStrikeSampleSkillDefinition();
            CSkillDefinition echoCloneDefinition = CreateDefaultCloneSampleSkillDefinition();
            defaultSkillDefinitionList.Add( flameSlashDefinition );
            defaultSkillDefinitionList.Add( frostFieldDefinition );
            defaultSkillDefinitionList.Add( arcBoltDefinition );
            defaultSkillDefinitionList.Add( phaseStrikeDefinition );
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
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 0, 1, 2.0f, 10.0f, "전방을 베어 {damage}%의 피해를 주고, 방어력을 {defReduction}% 감소시킨다. 디버프는 {debuffDuration}초 동안 유지된다.", instantActiveSkillEffect );
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
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 1, 2, 5.0f, 18.0f, "냉기 지대를 생성해 {duration}초 동안 유지한다. {tickInterval}초마다 {damage}%의 피해를 주고, 공격력을 {atkReduction} 감소시킨다. 디버프는 {debuffDuration}초 동안 유지된다.", placeActiveSkillEffect );
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
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 2, 3, 3.0f, 12.0f, "전방으로 번개 구체를 발사해 처음 맞은 적에게 {damage}%의 피해를 준다.", projectileActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 3 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 기본 분신 샘플 스킬 정의 생성
        ///</summary>
        ///<summary>
        /// 기본 페이즈 스트라이크 샘플 스킬 정의 생성
        ///</summary>
private CSkillDefinition CreateDefaultPhaseStrikeSampleSkillDefinition()
        {
            string skillId = "sample_phase_strike";
            string skillName = "Phase Strike";
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.name = skillName;
            runtimeGeneratedScriptableObjectList.Add( skillDefinition );

            CPhaseStrikeActiveSkillEffect phaseStrikeActiveSkillEffect = ScriptableObject.CreateInstance<CPhaseStrikeActiveSkillEffect>();
            phaseStrikeActiveSkillEffect.name = $"{skillName}_Effect";
            phaseStrikeActiveSkillEffect.Configure( 10, 0.15f, 1.15f, 2 );
            runtimeGeneratedScriptableObjectList.Add( phaseStrikeActiveSkillEffect );

            Sprite skillIcon = CreateSolidColorSkillIcon( new Color32( 255, 92, 156, 255 ) );
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 4, 1, 14.0f, 22.0f, "시전 즉시 모습을 감추고 무적 상태가 된다. 화면 안의 적을 {hitInterval}초 간격으로 최대 {hitCount}회 베며, 매 타격마다 {damage}%의 피해를 준다. 총 지속 시간은 {duration}초이며 종료 후 원래 위치로 돌아온다.", phaseStrikeActiveSkillEffect );
            skillDefinition.SetUnlockConditions( CreateLevelUnlockConditionList( 1 ) );
            return skillDefinition;
        }

        ///<summary>
        /// 湲곕낯 遺꾩떊 ?섑뵆 ?ㅽ궗 ?뺤쓽 ?앹꽦
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
            skillDefinition.ConfigureActiveSkill( skillId, skillName, skillIcon, 3, 4, 12.0f, 24.0f, "잔상을 소환해 {duration}초 동안 유지한다. 잔상은 플레이어의 이동과 공격을 재현하며, 공격 시 {damage}%의 피해를 준다.", cloneReplayActiveSkillEffect );
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
            GrantSkillPointsForLevelProgress( _level );
            RaiseSkillStateChanged();
        }

        ///<summary>
        /// 퀘스트 상태 변경 반영 처리
        ///</summary>
        private void HandleQuestStateChanged()
        {
            RaiseSkillStateChanged();
        }

        ///<summary>
        /// 런타임 스킬 캐시 재구성
        ///</summary>
        private void RebuildRuntimeData()
        {
            skillRuntimeDataList = BuildRuntimeDataListPreserveState( null );
            RebuildRuntimeDataDictionary();
        }

        ///<summary>
        /// 런타임 스킬 데이터 무결성 보정
        ///</summary>
        private void EnsureRuntimeDataIntegrity()
        {
            int skillDefinitionCount = skillDefinitionList != null ? skillDefinitionList.Count : 0;
            int runtimeDataCount = skillRuntimeDataList != null ? skillRuntimeDataList.Count : 0;
            bool needsRebuild = skillDefinitionCount != runtimeDataCount || skillRuntimeDataById.Count != runtimeDataCount;

            if ( needsRebuild == false )
            {
                return;
            }

            List<CSkillRuntimeData> previousRuntimeDataList = skillRuntimeDataList;
            skillRuntimeDataList = BuildRuntimeDataListPreserveState( previousRuntimeDataList );
            RebuildRuntimeDataDictionary();
        }

        ///<summary>
        /// 스킬 정의 기반 런타임 데이터 목록 구성
        ///</summary>
        private List<CSkillRuntimeData> BuildRuntimeDataListPreserveState( List<CSkillRuntimeData> _previousRuntimeDataList )
        {
            Dictionary<string, CSkillRuntimeData> previousRuntimeDataById = BuildRuntimeDataDictionary( _previousRuntimeDataList );
            List<CSkillRuntimeData> rebuiltRuntimeDataList = new List<CSkillRuntimeData>();

            if ( skillDefinitionList == null )
            {
                return rebuiltRuntimeDataList;
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

                if ( ContainsRuntimeDataForSkill( rebuiltRuntimeDataList, skillId ) )
                {
                    Debug.LogWarning( $"Duplicated skill id was ignored: {skillId}", this );
                    continue;
                }

                bool hasPreviousRuntimeData = previousRuntimeDataById.TryGetValue( skillId, out CSkillRuntimeData runtimeData );

                if ( hasPreviousRuntimeData == false || runtimeData == null )
                {
                    runtimeData = CreateSkillRuntimeData( skillDefinition );
                }
                else
                {
                    runtimeData.SetSkillDefinition( skillDefinition );
                }

                rebuiltRuntimeDataList.Add( runtimeData );
            }

            return rebuiltRuntimeDataList;
        }

        ///<summary>
        /// 런타임 스킬 데이터 사전 재구성
        ///</summary>
        private void RebuildRuntimeDataDictionary()
        {
            Dictionary<string, CSkillRuntimeData> rebuiltRuntimeDataById = BuildRuntimeDataDictionary( skillRuntimeDataList );
            skillRuntimeDataById.Clear();

            foreach ( KeyValuePair<string, CSkillRuntimeData> pair in rebuiltRuntimeDataById )
            {
                skillRuntimeDataById.Add( pair.Key, pair.Value );
            }
        }

        ///<summary>
        /// 런타임 스킬 데이터 사전 구성
        ///</summary>
        private Dictionary<string, CSkillRuntimeData> BuildRuntimeDataDictionary( List<CSkillRuntimeData> _runtimeDataList )
        {
            Dictionary<string, CSkillRuntimeData> runtimeDataById = new Dictionary<string, CSkillRuntimeData>();

            if ( _runtimeDataList == null )
            {
                return runtimeDataById;
            }

            for ( int index = 0; index < _runtimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = _runtimeDataList[ index ];

                if ( runtimeData == null )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

                if ( skillDefinition == null )
                {
                    continue;
                }

                string skillId = skillDefinition.GetSkillId();

                if ( string.IsNullOrWhiteSpace( skillId ) || runtimeDataById.ContainsKey( skillId ) )
                {
                    continue;
                }

                runtimeDataById.Add( skillId, runtimeData );
            }

            return runtimeDataById;
        }

        ///<summary>
        /// 신규 런타임 스킬 데이터 생성
        ///</summary>
        private CSkillRuntimeData CreateSkillRuntimeData( CSkillDefinition _skillDefinition )
        {
            CSkillRuntimeData runtimeData = new CSkillRuntimeData();
            runtimeData.SetSkillDefinition( _skillDefinition );
            runtimeData.SetUnlocked( false );
            runtimeData.SetSkillLevel( 0 );
            runtimeData.SetAssignedQuickSlotIndex( ResolveInitialQuickSlotIndex( _skillDefinition ) );
            return runtimeData;
        }

        ///<summary>
        /// 목록 내 스킬 아이디 중복 여부 확인
        ///</summary>
        private bool ContainsRuntimeDataForSkill( List<CSkillRuntimeData> _runtimeDataList, string _skillId )
        {
            if ( _runtimeDataList == null || string.IsNullOrWhiteSpace( _skillId ) )
            {
                return false;
            }

            for ( int index = 0; index < _runtimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = _runtimeDataList[ index ];

                if ( runtimeData == null || runtimeData.GetSkillDefinition() == null )
                {
                    continue;
                }

                string currentSkillId = runtimeData.GetSkillDefinition().GetSkillId();

                if ( string.Equals( currentSkillId, _skillId, StringComparison.Ordinal ) )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 해금 상태 갱신
        ///</summary>
        private void InitializeSkillProgression()
        {
            currentSkillPoint = Mathf.Max( currentSkillPoint, initialSkillPoint );
            int currentLevel = targetStatManager != null ? targetStatManager.GetCurrentLevel() : 1;

            if ( lastGrantedPlayerLevel <= 0 )
            {
                lastGrantedPlayerLevel = 1;
            }

            GrantSkillPointsForLevelProgress( currentLevel );
        }

        ///<summary>
        /// 레벨 증가 기반 스킬 포인트 지급
        ///</summary>
        private void GrantSkillPointsForLevelProgress( int _currentLevel )
        {
            int normalizedLevel = Mathf.Max( 1, _currentLevel );
            int normalizedGrantedLevel = Mathf.Max( 1, lastGrantedPlayerLevel );

            if ( normalizedLevel <= normalizedGrantedLevel )
            {
                lastGrantedPlayerLevel = normalizedGrantedLevel;
                return;
            }

            int grantedLevelCount = normalizedLevel - normalizedGrantedLevel;
            int grantedSkillPoint = grantedLevelCount * Mathf.Max( 0, skillPointPerLevelUp );
            currentSkillPoint = Mathf.Max( 0, currentSkillPoint + grantedSkillPoint );
            lastGrantedPlayerLevel = normalizedLevel;
        }

        ///<summary>
        /// 스킬 학습 가능 상태 판정
        ///</summary>
        private bool CanLearnSkill( CSkillRuntimeData _runtimeData )
        {
            if ( _runtimeData == null || _runtimeData.IsUnlocked() )
            {
                return false;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return false;
            }

            int currentLevel = targetStatManager != null ? targetStatManager.GetCurrentLevel() : 1;
            bool isConditionSatisfied = skillDefinition.AreUnlockConditionsSatisfied( this, currentLevel, targetQuestStateProvider );

            if ( isConditionSatisfied == false )
            {
                return false;
            }

            bool result = currentSkillPoint >= skillDefinition.GetLearnSpCost();
            return result;
        }

        ///<summary>
        /// 스킬 레벨업 가능 상태 판정
        ///</summary>
        private bool CanLevelUpSkill( CSkillRuntimeData _runtimeData )
        {
            if ( _runtimeData == null || _runtimeData.IsUnlocked() == false )
            {
                return false;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return false;
            }

            int currentSkillLevel = _runtimeData.GetSkillLevel();

            if ( currentSkillLevel >= skillDefinition.GetMaxSkillLevel() )
            {
                return false;
            }

            bool result = currentSkillPoint >= skillDefinition.GetLevelUpSpCost();
            return result;
        }

        ///<summary>
        /// 스킬 학습 처리
        ///</summary>
        private bool TryLearnSkill( CSkillRuntimeData _runtimeData, bool _ignoreConditionAndCost )
        {
            if ( _runtimeData == null || _runtimeData.IsUnlocked() )
            {
                return false;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return false;
            }

            if ( _ignoreConditionAndCost == false && CanLearnSkill( _runtimeData ) == false )
            {
                return false;
            }

            if ( _ignoreConditionAndCost == false )
            {
                int nextSkillPoint = currentSkillPoint - skillDefinition.GetLearnSpCost();
                currentSkillPoint = Mathf.Max( 0, nextSkillPoint );
            }

            _runtimeData.SetUnlocked( true );
            _runtimeData.SetSkillLevel( 1 );

            if ( skillDefinition.GetSkillType() == eSkillType.PASSIVE )
            {
                RebuildPassiveStatBonus();
            }

            if ( OnSkillUnlocked != null )
            {
                OnSkillUnlocked( skillDefinition );
            }

            RaiseSkillStateChanged();
            return true;
        }

        ///<summary>
        /// 스킬 레벨업 처리
        ///</summary>
        private bool TryLevelUpSkill( CSkillRuntimeData _runtimeData )
        {
            if ( CanLevelUpSkill( _runtimeData ) == false )
            {
                return false;
            }

            CSkillDefinition skillDefinition = _runtimeData.GetSkillDefinition();
            int nextSkillPoint = currentSkillPoint - skillDefinition.GetLevelUpSpCost();
            int nextSkillLevel = _runtimeData.GetSkillLevel() + 1;
            currentSkillPoint = Mathf.Max( 0, nextSkillPoint );
            _runtimeData.SetSkillLevel( nextSkillLevel );

            if ( skillDefinition.GetSkillType() == eSkillType.PASSIVE )
            {
                RebuildPassiveStatBonus();
            }

            RaiseSkillStateChanged();
            return true;
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

                int skillLevel = Mathf.Max( 1, runtimeData.GetSkillLevel() );
                CPlayerStatRuntimeData passiveStatBonus = skillDefinition.GetPassiveStatBonus();
                AddPassiveStatBonus( passiveStatBonus, skillLevel );
                AddPassiveSkillEffects( skillDefinition.GetPassiveSkillEffectList(), skillLevel );
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
        private void AddPassiveStatBonus( CPlayerStatRuntimeData _passiveStatBonus, int _skillLevel )
        {
            if ( _passiveStatBonus == null )
            {
                return;
            }

            Array statTypeArray = Enum.GetValues( typeof( ePlayerStatType ) );

            for ( int index = 0; index < statTypeArray.Length; index++ )
            {
                ePlayerStatType statType = ( ePlayerStatType ) statTypeArray.GetValue( index );
                float statValue = _passiveStatBonus.GetStatValue( statType ) * Mathf.Max( 1, _skillLevel );

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
        private void AddPassiveSkillEffects( List<CPassiveSkillEffectBase> _passiveSkillEffectList, int _skillLevel )
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

                if ( passiveSkillEffect is CPassiveStatSkillEffect passiveStatSkillEffect )
                {
                    ePlayerStatType targetStatType = passiveStatSkillEffect.GetTargetStatType();
                    float scaledBonusValue = passiveStatSkillEffect.GetBonusValue() * Mathf.Max( 1, _skillLevel );
                    aggregatedPassiveStatBonus.AddStatValue( targetStatType, scaledBonusValue );
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
            int skillLevel = Mathf.Max( 1, _runtimeData.GetSkillLevel() );
            float mpCost = skillDefinition.GetMpCost( skillLevel );
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
            int skillLevel = Mathf.Max( 1, _runtimeData.GetSkillLevel() );
            float mpCost = skillDefinition.GetMpCost( skillLevel );

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
        /// 퀵슬롯 인덱스 기반 런타임 데이터 반환
        ///</summary>
        private CSkillRuntimeData GetSkillRuntimeDataByQuickSlotIndex( int _quickSlotIndex )
        {
            for ( int index = 0; index < skillRuntimeDataList.Count; index++ )
            {
                CSkillRuntimeData runtimeData = skillRuntimeDataList[ index ];

                if ( runtimeData == null )
                {
                    continue;
                }

                if ( runtimeData.GetAssignedQuickSlotIndex() != _quickSlotIndex )
                {
                    continue;
                }

                return runtimeData;
            }

            return null;
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

            RaiseSkillStateChanged();
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
        /// 스킬 상태 변경 이벤트 전파
        ///</summary>
        private void RaiseSkillStateChanged()
        {
            if ( OnSkillStateChanged != null )
            {
                OnSkillStateChanged();
            }
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
        /// 초기 퀵슬롯 인덱스 결정
        ///</summary>
        private int ResolveInitialQuickSlotIndex( CSkillDefinition _skillDefinition )
        {
            return -1;
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
