using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 설치형 액티브 스킬 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "PlaceActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Place" )]
    public sealed class CPlaceActiveSkillEffect : CActiveSkillEffectBase, ISerializationCallbackReceiver
    {
        [SerializeField] private Vector2 placementOffset;
        [SerializeField] private float areaRadius = 1.75f;
        [SerializeField] private float durationSeconds = 5.0f;
        [SerializeField] private float tickIntervalSeconds = 1.0f;
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] [HideInInspector] private int maxTargetCount = DefaultSimultaneousTargetCount;
        [SerializeField] private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();

        ///<summary>
        /// 설치형 스킬 효과 데이터 구성
        ///</summary>
        public void Configure( Vector2 _placementOffset, float _areaRadius, float _durationSeconds, float _tickIntervalSeconds, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount )
        {
            placementOffset = _placementOffset;
            areaRadius = Mathf.Max( 0.1f, _areaRadius );
            durationSeconds = Mathf.Max( 0.01f, _durationSeconds );
            tickIntervalSeconds = Mathf.Max( 0.01f, _tickIntervalSeconds );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            SetSimultaneousTargetCount( _maxTargetCount );
            maxTargetCount = GetSimultaneousTargetCount();
        }

        ///<summary>
        /// 디버프 효과 목록 설정
        ///</summary>
        public void SetDebuffEffects( List<CEnemyDebuffEffectBase> _debuffEffectList )
        {
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
        }

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.PLACE;
            return result;
        }

        ///<summary>
        /// 툴 미리보기 범위 데이터 반환
        ///</summary>
        public override bool TryGetToolRangePreviewData( Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _ownerTransform == null )
            {
                return false;
            }

            float facingDirection = _ownerTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            Vector2 resolvedOffset = placementOffset;
            resolvedOffset.x *= facingDirection;
            Vector3 centerPosition = _ownerTransform.position + ( Vector3 ) resolvedOffset;
            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = centerPosition;
            _previewData.radius = areaRadius;
            return true;
        }

        ///<summary>
        /// 툴 미리보기 표시 시간 반환
        ///</summary>
        public override float GetToolPreviewDurationSeconds()
        {
            float previewDurationSeconds = Mathf.Max( durationSeconds, base.GetToolPreviewDurationSeconds() );
            return previewDurationSeconds;
        }

        ///<summary>
        /// 스킬 실행 처리
        ///</summary>
        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            Transform ownerTransform = _skillContext.GetOwnerTransform();

            if ( ownerTransform == null )
            {
                return false;
            }

            float facingDirection = ownerTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            Vector2 resolvedOffset = placementOffset;
            resolvedOffset.x *= facingDirection;
            Vector3 spawnPosition = ownerTransform.position + ( Vector3 ) resolvedOffset;
            CSkillVfxUtility.PlayCastVfx( _skillContext );
            GameObject placedSkillObject = new GameObject( "PlacedSkillAreaRuntime" );
            placedSkillObject.transform.position = spawnPosition;
            CPlacedSkillAreaRuntime placedSkillAreaRuntime = placedSkillObject.AddComponent<CPlacedSkillAreaRuntime>();
            placedSkillAreaRuntime.Initialize( _skillContext, durationSeconds, GetDamageStartDelaySeconds(), tickIntervalSeconds, areaRadius, damageMultiplier, flatDamageBonus, GetSimultaneousTargetCount(), debuffEffectList );
            return true;
        }

        ///<summary>
        /// 직렬화 이전 대상 수 동기화
        ///</summary>
        public void OnBeforeSerialize()
        {
            maxTargetCount = GetSimultaneousTargetCount();
        }

        ///<summary>
        /// 역직렬화 이후 대상 수 마이그레이션
        ///</summary>
        public void OnAfterDeserialize()
        {
            SyncLegacyTargetCount();
        }

        ///<summary>
        /// 레거시 대상 수 데이터 동기화
        ///</summary>
        private void SyncLegacyTargetCount()
        {
            int configuredTargetCount = GetConfiguredSimultaneousTargetCount();

            if ( configuredTargetCount == DefaultSimultaneousTargetCount && maxTargetCount != DefaultSimultaneousTargetCount )
            {
                SetSimultaneousTargetCount( maxTargetCount );
                return;
            }

            maxTargetCount = GetSimultaneousTargetCount();
        }
    }
}
