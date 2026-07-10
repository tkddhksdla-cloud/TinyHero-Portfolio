using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    [CreateAssetMenu( fileName = "SkyfallStrikeActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Skyfall Strike" )]
    public sealed class CSkyfallStrikeActiveSkillEffect : CActiveSkillEffectBase
    {
        [SerializeField] private float launchDurationSeconds = 0.42f;
        [SerializeField] private float plungeDurationSeconds = 0.08f;
        [SerializeField] private float launchDistance = 2.4f;
        [SerializeField] private float launchHeight = 2.2f;
        [SerializeField] private float landingDistance = 3.1f;
        [SerializeField] private float areaRadius = 2.0f;
        [SerializeField] private float damageMultiplier = 2.4f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] [Range( 0.0f, 1.0f )] private float airborneRangeIncreasePercent = 0.3f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float airborneDamageIncreasePercent = 0.25f;
        [SerializeField] private GameObject landingVfxPrefab;
        [SerializeField] private Vector3 landingVfxOffset;
        [SerializeField] private float landingVfxReturnDelay = 2.0f;
        [SerializeField] private List<CEnemyCrowdControlEffectBase> crowdControlEffectList = new List<CEnemyCrowdControlEffectBase>();

        public void Configure( float _launchDurationSeconds, float _plungeDurationSeconds, float _launchDistance, float _launchHeight, float _landingDistance, float _areaRadius, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount )
        {
            launchDurationSeconds = Mathf.Max( 0.01f, _launchDurationSeconds );
            plungeDurationSeconds = Mathf.Max( 0.01f, _plungeDurationSeconds );
            launchDistance = Mathf.Max( 0.0f, _launchDistance );
            launchHeight = Mathf.Max( 0.0f, _launchHeight );
            landingDistance = Mathf.Max( launchDistance, _landingDistance );
            areaRadius = Mathf.Max( 0.1f, _areaRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            SetSimultaneousTargetCount( _maxTargetCount );
        }

        public void SetCrowdControlEffects( List<CEnemyCrowdControlEffectBase> _crowdControlEffectList )
        {
            crowdControlEffectList = _crowdControlEffectList != null ? _crowdControlEffectList : new List<CEnemyCrowdControlEffectBase>();
        }

        public GameObject GetLandingVfxPrefab()
        {
            return landingVfxPrefab;
        }

        public Vector3 GetLandingVfxOffset()
        {
            return landingVfxOffset;
        }

        public float GetLandingVfxReturnDelay()
        {
            return Mathf.Max( 0.0f, landingVfxReturnDelay );
        }

        public float GetAirborneRangeIncreasePercent()
        {
            return Mathf.Clamp01( airborneRangeIncreasePercent );
        }

        public float GetAirborneDamageIncreasePercent()
        {
            return Mathf.Clamp01( airborneDamageIncreasePercent );
        }

        public override eActiveSkillType GetActiveSkillType()
        {
            return eActiveSkillType.SKYFALL_STRIKE;
        }

        public override bool CanExecute( CSkillContext _skillContext )
        {
            if ( base.CanExecute( _skillContext ) == false )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();
            return playerController != null;
        }

        public override bool ShouldExecuteDuringCast()
        {
            return true;
        }

        public override bool TryGetToolRangePreviewData( Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _ownerTransform == null )
            {
                return false;
            }

            float facingDirection = _ownerTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            Vector3 centerPosition = _ownerTransform.position + Vector3.right * facingDirection * landingDistance;
            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = centerPosition;
            _previewData.radius = areaRadius;
            return true;
        }

        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            GameObject runtimeObject = new GameObject( "SkyfallStrikeSkillRuntime" );
            CSkyfallStrikeSkillRuntime runtime = runtimeObject.AddComponent<CSkyfallStrikeSkillRuntime>();
            runtime.Initialize( _skillContext, launchDurationSeconds, plungeDurationSeconds, launchDistance, launchHeight, landingDistance, areaRadius, damageMultiplier, flatDamageBonus, GetSimultaneousTargetCount(), crowdControlEffectList );
            CSkillVfxUtility.PlayCastVfx( _skillContext );
            CSkillAudioUtility.PlayCastSfx( _skillContext );
            return true;
        }
    }
}
