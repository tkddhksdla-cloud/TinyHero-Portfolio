using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 장착 무기를 플레이어 주변에 소환해 자동 공격시키는 액티브 스킬 효과
    ///</summary>
    [CreateAssetMenu( fileName = "FloatingWeaponSummonActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Floating Weapon Summon" )]
    public sealed class CFloatingWeaponSummonActiveSkillEffect : CActiveSkillEffectBase
    {
        private const int MinimumCompanionCount = 1;
        private const float MinimumPositiveValue = 0.01f;

        [Header( "소환 설정" )]
        [SerializeField] private int companionCount = 3;
        [SerializeField] private float durationSeconds = 12.0f;
        [SerializeField] private Vector2 formationBaseOffset = new Vector2( -0.9f, 0.9f );
        [SerializeField] private float formationVerticalSpacing = 0.65f;
        [SerializeField] private float hoverAmplitude = 0.12f;
        [SerializeField] private float hoverFrequency = 2.4f;

        [Header( "자동 공격" )]
        [SerializeField] private float targetSearchRadius = 6.0f;
        [SerializeField] private float attackIntervalSeconds = 1.2f;
        [SerializeField] private float flightSpeed = 12.0f;
        [SerializeField] private float hitRadius = 0.35f;
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private int flatDamageBonus;

        [Header( "외형" )]
        [SerializeField] private GameObject floatingWeaponPrefab;
        [SerializeField] private GameObject floatingWeaponTrailPrefab;
        [SerializeField] private float weaponVisualScale = 0.6f;
        [SerializeField] private float attackRotationSpeed = 720.0f;

        ///<summary>
        /// 부유 무기 소환 효과 설정
        ///</summary>
        public void Configure( int _companionCount, float _durationSeconds, Vector2 _formationBaseOffset, float _formationVerticalSpacing, float _hoverAmplitude, float _hoverFrequency, float _targetSearchRadius, float _attackIntervalSeconds, float _flightSpeed, float _hitRadius, float _damageMultiplier, int _flatDamageBonus, GameObject _floatingWeaponPrefab, GameObject _floatingWeaponTrailPrefab, float _weaponVisualScale, float _attackRotationSpeed )
        {
            companionCount = Mathf.Max( MinimumCompanionCount, _companionCount );
            durationSeconds = Mathf.Max( MinimumPositiveValue, _durationSeconds );
            formationBaseOffset = _formationBaseOffset;
            formationVerticalSpacing = Mathf.Max( 0.0f, _formationVerticalSpacing );
            hoverAmplitude = Mathf.Max( 0.0f, _hoverAmplitude );
            hoverFrequency = Mathf.Max( 0.0f, _hoverFrequency );
            targetSearchRadius = Mathf.Max( MinimumPositiveValue, _targetSearchRadius );
            attackIntervalSeconds = Mathf.Max( MinimumPositiveValue, _attackIntervalSeconds );
            flightSpeed = Mathf.Max( MinimumPositiveValue, _flightSpeed );
            hitRadius = Mathf.Max( MinimumPositiveValue, _hitRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            floatingWeaponPrefab = _floatingWeaponPrefab;
            floatingWeaponTrailPrefab = _floatingWeaponTrailPrefab;
            weaponVisualScale = Mathf.Max( MinimumPositiveValue, _weaponVisualScale );
            attackRotationSpeed = Mathf.Max( 0.0f, _attackRotationSpeed );
        }

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.FLOATING_WEAPON_SUMMON;
            return result;
        }

        ///<summary>
        /// 장착 또는 기본 표시 무기가 있을 때 실행 가능
        ///</summary>
        public override bool CanExecute( CSkillContext _skillContext )
        {
            if ( base.CanExecute( _skillContext ) == false )
            {
                return false;
            }

            bool hasWeaponVisual = CFloatingWeaponVisualUtility.TryResolveWeaponSprite( _skillContext, out Sprite weaponSprite );
            bool result = hasWeaponVisual && weaponSprite != null && floatingWeaponPrefab != null && floatingWeaponTrailPrefab != null;
            return result;
        }

        ///<summary>
        /// 부유 무기 소환 실행
        ///</summary>
        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            Transform ownerTransform = _skillContext.GetOwnerTransform();
            GameObject runtimeObject = new GameObject( "FloatingWeaponSummonRuntime" );
            runtimeObject.transform.position = ownerTransform.position;
            CFloatingWeaponSummonRuntime summonRuntime = runtimeObject.AddComponent<CFloatingWeaponSummonRuntime>();
            bool didInitialize = summonRuntime.Initialize(
                _skillContext,
                companionCount,
                durationSeconds,
                formationBaseOffset,
                formationVerticalSpacing,
                hoverAmplitude,
                hoverFrequency,
                targetSearchRadius,
                attackIntervalSeconds,
                flightSpeed,
                hitRadius,
                damageMultiplier,
                flatDamageBonus,
                floatingWeaponPrefab,
                floatingWeaponTrailPrefab,
                weaponVisualScale,
                attackRotationSpeed
            );

            if ( didInitialize == false )
            {
                Destroy( runtimeObject );
                return false;
            }

            CSkillVfxUtility.PlayCastVfx( _skillContext );
            CSkillAudioUtility.PlayCastSfx( _skillContext );
            return true;
        }
    }
}
