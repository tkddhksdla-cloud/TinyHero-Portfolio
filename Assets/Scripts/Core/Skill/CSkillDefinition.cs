using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 정적 데이터 정의
    ///</summary>
    [CreateAssetMenu( fileName = "SkillDefinition", menuName = "TinyHero/Skill/Skill Definition" )]
    public sealed class CSkillDefinition : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string skillName;
        [SerializeField] private Sprite skillIcon;
        [SerializeField] private eSkillType skillType = eSkillType.ACTIVE;
        [SerializeField] private eActiveSkillType activeSkillType = eActiveSkillType.NONE;
        [SerializeField] private int quickSlotIndex;
        [SerializeField] private float cooldownSeconds = 1.0f;
        [SerializeField] private float mpCost;
        [SerializeField] private float mpCostReductionPerLevel;
        [SerializeField] private int learnSpCost = 1;
        [SerializeField] private int levelUpSpCost = 1;
        [SerializeField] private int maxSkillLevel = 5;
        [SerializeField] private bool assignableToQuickSlot = true;
        [SerializeField] private float cooldownReductionPerLevel;
        [SerializeField] private float damageMultiplierBonusPerLevel = 0.1f;
        [SerializeField] private int flatDamageBonusPerLevel;
        [SerializeField] private float castLockDurationSeconds = 0.15f;
        [SerializeField] private ePlayerSkillCastAnimation castAnimation = ePlayerSkillCastAnimation.ATTACK;
        [SerializeField] private string castAnimationName = "Attack";
        [SerializeField] private float castAnimationSpeed = 1.0f;
        [SerializeField] [TextArea( 2, 4 )] private string description;
        [SerializeField] private GameObject castVfxPrefab;
        [SerializeField] private Vector3 castVfxOffset;
        [SerializeField] private float castVfxReturnDelay = 1.0f;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private Vector3 hitVfxOffset;
        [SerializeField] private float hitVfxReturnDelay = 1.0f;
        [SerializeField] private string castSfxClipName = string.Empty;
        [SerializeField] private string hitSfxClipName = string.Empty;
        [SerializeField] private string loopSfxClipName = string.Empty;
        [SerializeField] private GameObject projectileVfxPrefab;
        [SerializeField] private Vector3 projectileVfxOffset;
        [SerializeField] private float projectileVfxReturnDelay = 1.0f;
        [SerializeField] private GameObject loopVfxPrefab;
        [SerializeField] private Vector3 loopVfxOffset;
        [SerializeField] private float loopVfxReturnDelay = 1.0f;
        [SerializeField] private CPlayerStatRuntimeData passiveStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private CSkillActionBase activeAction;
        [SerializeField] private CActiveSkillEffectBase activeSkillEffect;
        [SerializeField] private List<CPassiveSkillEffectBase> passiveSkillEffectList = new List<CPassiveSkillEffectBase>();
        [SerializeField] private List<CSkillUnlockConditionBase> unlockConditionList = new List<CSkillUnlockConditionBase>();

        ///<summary>
        /// 스킬 식별자 반환
        ///</summary>
        public string GetSkillId()
        {
            string result = string.IsNullOrWhiteSpace( skillId ) ? name : skillId;
            return result;
        }

        ///<summary>
        /// 스킬 이름 반환
        ///</summary>
        public string GetSkillName()
        {
            string resolvedSkillName = string.IsNullOrWhiteSpace( skillName ) ? name : skillName;
            string result = CDataManager.GetText( resolvedSkillName );
            return result;
        }

        ///<summary>
        /// 스킬 아이콘 반환
        ///</summary>
        public Sprite GetSkillIcon()
        {
            Sprite result = skillIcon;
            return result;
        }

        ///<summary>
        /// 스킬 분류 반환
        ///</summary>
        public eSkillType GetSkillType()
        {
            eSkillType result = skillType;
            return result;
        }

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = ResolveActiveSkillType();
            return result;
        }

        ///<summary>
        /// 퀵슬롯 인덱스 반환
        ///</summary>
        public int GetQuickSlotIndex()
        {
            int result = quickSlotIndex;
            return result;
        }

        ///<summary>
        /// 필요 레벨 반환
        ///</summary>
        public int GetRequiredLevel()
        {
            int result = ResolveRequiredLevelFromUnlockConditions();
            return result;
        }

        ///<summary>
        /// 쿨타임 반환
        ///</summary>
        public float GetCooldownSeconds()
        {
            float result = Mathf.Max( 0.0f, cooldownSeconds );
            return result;
        }

        ///<summary>
        /// 스킬 레벨 기반 쿨타임 반환
        ///</summary>
        public float GetCooldownSeconds( int _skillLevel )
        {
            int normalizedSkillLevel = Mathf.Max( 1, _skillLevel );
            float reducedCooldown = cooldownSeconds - cooldownReductionPerLevel * ( normalizedSkillLevel - 1 );
            float result = Mathf.Max( 0.0f, reducedCooldown );
            return result;
        }

        ///<summary>
        /// MP 소모량 반환
        ///</summary>
        public float GetMpCost()
        {
            float result = Mathf.Max( 0.0f, mpCost );
            return result;
        }

        ///<summary>
        /// 스킬 레벨 기반 MP 소모량 반환
        ///</summary>
        public float GetMpCost( int _skillLevel )
        {
            int normalizedSkillLevel = Mathf.Max( 1, _skillLevel );
            float reducedMpCost = mpCost - mpCostReductionPerLevel * ( normalizedSkillLevel - 1 );
            float result = Mathf.Max( 0.0f, reducedMpCost );
            return result;
        }

        ///<summary>
        /// 스킬 학습 SP 비용 반환
        ///</summary>
        public int GetLearnSpCost()
        {
            int result = Mathf.Max( 0, learnSpCost );
            return result;
        }

        ///<summary>
        /// 스킬 레벨업 SP 비용 반환
        ///</summary>
        public int GetLevelUpSpCost()
        {
            int result = Mathf.Max( 0, levelUpSpCost );
            return result;
        }

        ///<summary>
        /// 스킬 최대 레벨 반환
        ///</summary>
        public int GetMaxSkillLevel()
        {
            int result = Mathf.Max( 1, maxSkillLevel );
            return result;
        }

        ///<summary>
        /// 퀵슬롯 배정 가능 여부 반환
        ///</summary>
        public bool IsAssignableToQuickSlot()
        {
            bool result = assignableToQuickSlot;
            return result;
        }

        ///<summary>
        /// 스킬 레벨 기반 데미지 배율 반환
        ///</summary>
        public float ResolveDamageMultiplier( float _baseDamageMultiplier, int _skillLevel )
        {
            int normalizedSkillLevel = Mathf.Max( 1, _skillLevel );
            float bonusDamageMultiplier = damageMultiplierBonusPerLevel * ( normalizedSkillLevel - 1 );
            float resolvedDamageMultiplier = _baseDamageMultiplier + bonusDamageMultiplier;
            float result = Mathf.Max( 0.0f, resolvedDamageMultiplier );
            return result;
        }

        ///<summary>
        /// 스킬 레벨 기반 고정 데미지 반환
        ///</summary>
        public int ResolveFlatDamageBonus( int _baseFlatDamageBonus, int _skillLevel )
        {
            int normalizedSkillLevel = Mathf.Max( 1, _skillLevel );
            int resolvedFlatDamageBonus = _baseFlatDamageBonus + flatDamageBonusPerLevel * ( normalizedSkillLevel - 1 );
            int result = resolvedFlatDamageBonus;
            return result;
        }

        ///<summary>
        /// 시전 잠금 시간 반환
        ///</summary>
        public float GetCastLockDurationSeconds()
        {
            float result = Mathf.Max( 0.0f, castLockDurationSeconds );
            return result;
        }

        ///<summary>
        /// 시전 애니메이션 종류 반환
        ///</summary>
        public ePlayerSkillCastAnimation GetCastAnimation()
        {
            ePlayerSkillCastAnimation result = castAnimation;
            return result;
        }

        ///<summary>
        /// 시전 애니메이션 이름 반환
        ///</summary>
        public string GetCastAnimationName()
        {
            string result = string.IsNullOrWhiteSpace( castAnimationName ) ? "Attack" : castAnimationName.Trim();
            return result;
        }

        ///<summary>
        /// 시전 애니메이션 실사용 이름 반환
        ///</summary>
        public string GetResolvedCastAnimationName()
        {
            string result = ResolveCastAnimationName( castAnimation, castAnimationName );
            return result;
        }

        ///<summary>
        /// 시전 애니메이션 속도 반환
        ///</summary>
        public float GetCastAnimationSpeed()
        {
            float result = Mathf.Max( 0.01f, castAnimationSpeed );
            return result;
        }

        ///<summary>
        /// 스킬 설명 반환
        ///</summary>
        public string GetDescription()
        {
            string result = CDataManager.GetText( description );
            return result;
        }

        ///<summary>
        /// 스킬 레벨 기반 설명 문자열 반환
        ///</summary>
        public string GetFormattedDescription( int _skillLevel )
        {
            string result = CSkillDescriptionFormatter.Format( this, _skillLevel );
            return result;
        }

        ///<summary>
        /// 시전 이펙트 프리팹 반환
        ///</summary>
        public GameObject GetCastVfxPrefab()
        {
            GameObject result = castVfxPrefab;
            return result;
        }

        ///<summary>
        /// 시전 이펙트 오프셋 반환
        ///</summary>
        public Vector3 GetCastVfxOffset()
        {
            Vector3 result = castVfxOffset;
            return result;
        }

        ///<summary>
        /// 시전 이펙트 반환 시간 반환
        ///</summary>
        public float GetCastVfxReturnDelay()
        {
            float result = Mathf.Max( 0.0f, castVfxReturnDelay );
            return result;
        }

        ///<summary>
        /// 타격 이펙트 프리팹 반환
        ///</summary>
        public GameObject GetHitVfxPrefab()
        {
            GameObject result = hitVfxPrefab;
            return result;
        }

        ///<summary>
        /// 타격 이펙트 오프셋 반환
        ///</summary>
        public Vector3 GetHitVfxOffset()
        {
            Vector3 result = hitVfxOffset;
            return result;
        }

        ///<summary>
        /// 타격 이펙트 반환 시간 반환
        ///</summary>
        public float GetHitVfxReturnDelay()
        {
            float result = Mathf.Max( 0.0f, hitVfxReturnDelay );
            return result;
        }

        ///<summary>
        /// 타격 이펙트 설정 구성
        ///</summary>
        public void ConfigureHitVfx( GameObject _hitVfxPrefab, Vector3 _hitVfxOffset, float _hitVfxReturnDelay )
        {
            hitVfxPrefab = _hitVfxPrefab;
            hitVfxOffset = _hitVfxOffset;
            hitVfxReturnDelay = Mathf.Max( 0.0f, _hitVfxReturnDelay );
        }

        ///<summary>
        /// 시전 효과음 클립 이름 반환
        ///</summary>
        public string GetCastSfxClipName()
        {
            string result = NormalizeAudioClipName( castSfxClipName );
            return result;
        }

        ///<summary>
        /// 타격 효과음 클립 이름 반환
        ///</summary>
        public string GetHitSfxClipName()
        {
            string result = NormalizeAudioClipName( hitSfxClipName );
            return result;
        }

        ///<summary>
        /// 지속 루프 효과음 클립 이름 반환
        ///</summary>
        public string GetLoopSfxClipName()
        {
            string result = NormalizeAudioClipName( loopSfxClipName );
            return result;
        }

        ///<summary>
        /// 발사체 이펙트 프리팹 반환
        ///</summary>
        public GameObject GetProjectileVfxPrefab()
        {
            GameObject result = projectileVfxPrefab;
            return result;
        }

        ///<summary>
        /// 발사체 이펙트 오프셋 반환
        ///</summary>
        public Vector3 GetProjectileVfxOffset()
        {
            Vector3 result = projectileVfxOffset;
            return result;
        }

        ///<summary>
        /// 발사체 이펙트 반환 시간 반환
        ///</summary>
        public float GetProjectileVfxReturnDelay()
        {
            float result = Mathf.Max( 0.0f, projectileVfxReturnDelay );
            return result;
        }

        ///<summary>
        /// 지속 이펙트 프리팹 반환
        ///</summary>
        public GameObject GetLoopVfxPrefab()
        {
            GameObject result = loopVfxPrefab;
            return result;
        }

        ///<summary>
        /// 지속 이펙트 오프셋 반환
        ///</summary>
        public Vector3 GetLoopVfxOffset()
        {
            Vector3 result = loopVfxOffset;
            return result;
        }

        ///<summary>
        /// 지속 이펙트 반환 시간 반환
        ///</summary>
        public float GetLoopVfxReturnDelay()
        {
            float result = Mathf.Max( 0.0f, loopVfxReturnDelay );
            return result;
        }

        ///<summary>
        /// 레거시 패시브 스탯 보너스 반환
        ///</summary>
        public CPlayerStatRuntimeData GetPassiveStatBonus()
        {
            CPlayerStatRuntimeData result = passiveStatBonus;
            return result;
        }

        ///<summary>
        /// 레거시 액티브 실행 정의 반환
        ///</summary>
        public CSkillActionBase GetActiveAction()
        {
            CSkillActionBase result = activeAction;
            return result;
        }

        ///<summary>
        /// 액티브 스킬 효과 정의 반환
        ///</summary>
        public CActiveSkillEffectBase GetActiveSkillEffect()
        {
            CActiveSkillEffectBase result = activeSkillEffect;
            return result;
        }

        ///<summary>
        /// 패시브 스킬 효과 목록 반환
        ///</summary>
        public List<CPassiveSkillEffectBase> GetPassiveSkillEffectList()
        {
            List<CPassiveSkillEffectBase> result = passiveSkillEffectList;
            return result;
        }

        ///<summary>
        /// 해금 조건 목록 반환
        ///</summary>
        public List<CSkillUnlockConditionBase> GetUnlockConditionList()
        {
            List<CSkillUnlockConditionBase> result = unlockConditionList;
            return result;
        }

        ///<summary>
        /// 레거시 레벨 조건 기반 해금 여부 반환
        ///</summary>
        public bool CanUnlock( int _level )
        {
            bool result = _level >= GetRequiredLevel();
            return result;
        }

        ///<summary>
        /// 해금 조건 충족 여부 반환
        ///</summary>
        public bool AreUnlockConditionsSatisfied( CSkillManager _skillManager, int _playerLevel, CQuestStateProvider _questStateProvider )
        {
            if ( unlockConditionList == null || unlockConditionList.Count == 0 )
            {
                bool fallbackResult = CanUnlock( _playerLevel );
                return fallbackResult;
            }

            for ( int index = 0; index < unlockConditionList.Count; index++ )
            {
                CSkillUnlockConditionBase unlockCondition = unlockConditionList[ index ];

                if ( unlockCondition == null )
                {
                    continue;
                }

                bool isSatisfied = unlockCondition.IsSatisfied( _skillManager, _playerLevel, _questStateProvider );

                if ( isSatisfied == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 액티브 스킬 데이터 구성
        ///</summary>
        public void ConfigureActiveSkill( string _skillId, string _skillName, Sprite _skillIcon, int _quickSlotIndex, int _requiredLevel, float _cooldownSeconds, float _mpCost, string _description, CActiveSkillEffectBase _activeSkillEffect )
        {
            skillId = _skillId;
            skillName = _skillName;
            skillIcon = _skillIcon;
            skillType = eSkillType.ACTIVE;
            activeSkillType = _activeSkillEffect != null ? _activeSkillEffect.GetActiveSkillType() : eActiveSkillType.NONE;
            quickSlotIndex = Mathf.Max( 0, _quickSlotIndex );
            cooldownSeconds = Mathf.Max( 0.0f, _cooldownSeconds );
            mpCost = Mathf.Max( 0.0f, _mpCost );
            learnSpCost = Mathf.Max( 0, learnSpCost );
            levelUpSpCost = Mathf.Max( 0, levelUpSpCost );
            maxSkillLevel = Mathf.Max( 1, maxSkillLevel );
            assignableToQuickSlot = true;
            castLockDurationSeconds = 0.15f;
            castAnimation = ePlayerSkillCastAnimation.ATTACK;
            castAnimationName = "Attack";
            castAnimationSpeed = 1.0f;
            description = _description;
            activeAction = null;
            activeSkillEffect = _activeSkillEffect;
            passiveStatBonus.Clear();
            passiveSkillEffectList.Clear();
        }

        ///<summary>
        /// 패시브 스킬 데이터 구성
        ///</summary>
        public void ConfigurePassiveSkill( string _skillId, string _skillName, Sprite _skillIcon, string _description, List<CPassiveSkillEffectBase> _passiveSkillEffectList )
        {
            skillId = _skillId;
            skillName = _skillName;
            skillIcon = _skillIcon;
            skillType = eSkillType.PASSIVE;
            activeSkillType = eActiveSkillType.NONE;
            quickSlotIndex = -1;
            cooldownSeconds = 0.0f;
            mpCost = 0.0f;
            learnSpCost = Mathf.Max( 0, learnSpCost );
            levelUpSpCost = Mathf.Max( 0, levelUpSpCost );
            maxSkillLevel = Mathf.Max( 1, maxSkillLevel );
            assignableToQuickSlot = false;
            castLockDurationSeconds = 0.0f;
            castAnimation = ePlayerSkillCastAnimation.ATTACK;
            castAnimationName = "Attack";
            castAnimationSpeed = 1.0f;
            description = _description;
            activeAction = null;
            activeSkillEffect = null;
            passiveStatBonus.Clear();
            passiveSkillEffectList = _passiveSkillEffectList != null ? _passiveSkillEffectList : new List<CPassiveSkillEffectBase>();
        }

        ///<summary>
        /// 시전 설정 구성
        ///</summary>
        public void ConfigureCastSetting( float _castLockDurationSeconds, ePlayerSkillCastAnimation _castAnimation, string _castAnimationName, float _castAnimationSpeed )
        {
            castLockDurationSeconds = Mathf.Max( 0.0f, _castLockDurationSeconds );
            castAnimation = _castAnimation;
            castAnimationName = string.IsNullOrWhiteSpace( _castAnimationName ) ? "Attack" : _castAnimationName.Trim();
            castAnimationSpeed = Mathf.Max( 0.01f, _castAnimationSpeed );
        }

        ///<summary>
        /// 스킬 오디오 설정 구성
        ///</summary>
        public void ConfigureAudioSetting( string _castSfxClipName, string _hitSfxClipName, string _loopSfxClipName )
        {
            castSfxClipName = NormalizeAudioClipName( _castSfxClipName );
            hitSfxClipName = NormalizeAudioClipName( _hitSfxClipName );
            loopSfxClipName = NormalizeAudioClipName( _loopSfxClipName );
        }

        ///<summary>
        /// MP 소모 강화 수치 설정
        ///</summary>
        public void ConfigureMpScaling( float _mpCostReductionPerLevel )
        {
            mpCostReductionPerLevel = Mathf.Max( 0.0f, _mpCostReductionPerLevel );
        }

        ///<summary>
        /// 퀵슬롯 배정 가능 여부 설정
        ///</summary>
        public void SetAssignableToQuickSlot( bool _isAssignable )
        {
            assignableToQuickSlot = _isAssignable;
        }

        ///<summary>
        /// 시전 애니메이션 이름 결정
        ///</summary>
        private string ResolveCastAnimationName( ePlayerSkillCastAnimation _castAnimation, string _castAnimationName )
        {
            switch ( _castAnimation )
            {
                case ePlayerSkillCastAnimation.ATTACK:
                    return "Attack";

                case ePlayerSkillCastAnimation.IDLE:
                    return "Idle";

                case ePlayerSkillCastAnimation.MOVE:
                    return "Move";

                case ePlayerSkillCastAnimation.HIT:
                    return "Hit";

                case ePlayerSkillCastAnimation.DIE:
                    return "Die";

                case ePlayerSkillCastAnimation.CUSTOM:
                default:
                    return string.IsNullOrWhiteSpace( _castAnimationName ) ? "Attack" : _castAnimationName.Trim();
            }
        }

        ///<summary>
        /// 오디오 클립 이름 정규화
        ///</summary>
        private string NormalizeAudioClipName( string _clipName )
        {
            string result = string.IsNullOrWhiteSpace( _clipName ) ? string.Empty : _clipName.Trim();
            return result;
        }

        ///<summary>
        /// 현재 설정 기준 액티브 스킬 타입 결정
        ///</summary>
        private eActiveSkillType ResolveActiveSkillType()
        {
            if ( skillType != eSkillType.ACTIVE )
            {
                return eActiveSkillType.NONE;
            }

            if ( activeSkillEffect != null )
            {
                return activeSkillEffect.GetActiveSkillType();
            }

            eActiveSkillType result = activeSkillType;
            return result;
        }

        ///<summary>
        /// 해금 조건 기반 요구 레벨 계산
        ///</summary>
        private int ResolveRequiredLevelFromUnlockConditions()
        {
            if ( unlockConditionList == null || unlockConditionList.Count == 0 )
            {
                return 1;
            }

            int requiredLevel = 1;

            for ( int index = 0; index < unlockConditionList.Count; index++ )
            {
                CSkillUnlockConditionBase unlockCondition = unlockConditionList[ index ];
                CLevelUnlockCondition levelUnlockCondition = unlockCondition as CLevelUnlockCondition;

                if ( levelUnlockCondition == null )
                {
                    continue;
                }

                int conditionRequiredLevel = levelUnlockCondition.GetRequiredLevel();
                requiredLevel = Mathf.Max( requiredLevel, conditionRequiredLevel );
            }

            return requiredLevel;
        }

        ///<summary>
        /// 해금 조건 목록 설정
        ///</summary>
        public void SetUnlockConditions( List<CSkillUnlockConditionBase> _unlockConditionList )
        {
            unlockConditionList = _unlockConditionList != null ? _unlockConditionList : new List<CSkillUnlockConditionBase>();
        }

        ///<summary>
        /// 패시브 효과 목록 설정
        ///</summary>
        public void SetPassiveSkillEffects( List<CPassiveSkillEffectBase> _passiveSkillEffectList )
        {
            passiveSkillEffectList = _passiveSkillEffectList != null ? _passiveSkillEffectList : new List<CPassiveSkillEffectBase>();
        }
    }
}
