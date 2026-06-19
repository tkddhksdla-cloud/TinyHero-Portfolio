using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 발사체 활성 스킬 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "ProjectileActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Projectile" )]
    public sealed class CProjectileActiveSkillEffect : CActiveSkillEffectBase, ISerializationCallbackReceiver
    {
        [SerializeField] private Vector2 spawnOffset = new Vector2( 0.65f, 0.0f );
        [SerializeField] private float collisionRadius = 0.45f;
        [SerializeField] private float travelDistance = 6.0f;
        [SerializeField] private float travelSpeed = 10.0f;
        [SerializeField] private float damageMultiplier = 1.3f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] private bool destroyOnFirstHit = true;
        [SerializeField] [HideInInspector] private int maxTargetCount = DefaultSimultaneousTargetCount;
        [SerializeField] private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();

        ///<summary>
        /// 발사체 스킬 효과 데이터 구성
        ///</summary>
        public void Configure( Vector2 _spawnOffset, float _collisionRadius, float _travelDistance, float _travelSpeed, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount )
        {
            spawnOffset = _spawnOffset;
            collisionRadius = Mathf.Max( 0.05f, _collisionRadius );
            travelDistance = Mathf.Max( collisionRadius, _travelDistance );
            travelSpeed = Mathf.Max( 0.01f, _travelSpeed );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            SetSimultaneousTargetCount( _maxTargetCount );
            maxTargetCount = GetSimultaneousTargetCount();
        }

        ///<summary>
        /// 발사체 적 디버프 목록 설정
        ///</summary>
        public void SetDebuffEffects( List<CEnemyDebuffEffectBase> _debuffEffectList )
        {
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
        }

        ///<summary>
        /// 발사체 최초 충돌 시 파괴 여부 설정
        ///</summary>
        public void SetDestroyOnFirstHit( bool _destroyOnFirstHit )
        {
            destroyOnFirstHit = _destroyOnFirstHit;
        }

        ///<summary>
        /// 활성 스킬 세부 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.PROJECTILE;
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

            Vector2 projectileEndPosition = ResolveProjectileEndPosition( _ownerTransform );
            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = new Vector3( projectileEndPosition.x, projectileEndPosition.y, _ownerTransform.position.z );
            _previewData.radius = collisionRadius;
            return true;
        }

        ///<summary>
        /// 발사체 미리보기 표시 시간 반환
        ///</summary>
        public override float GetToolPreviewDurationSeconds()
        {
            float travelDurationSeconds = travelSpeed > 0.0f ? travelDistance / travelSpeed : 0.0f;
            float previewDurationSeconds = Mathf.Max( base.GetToolPreviewDurationSeconds(), travelDurationSeconds );
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

            float facingDirection = ResolveFacingDirection( ownerTransform );
            Vector2 resolvedOffset = ResolveSpawnOffset( facingDirection );
            Vector3 spawnPosition = ownerTransform.position + ( Vector3 ) resolvedOffset;
            Vector2 moveDirection = new Vector2( facingDirection, 0.0f );
            CSkillVfxUtility.PlayCastVfx( _skillContext );
            GameObject projectileObject = new GameObject( "ProjectileSkillRuntime" );
            projectileObject.transform.position = spawnPosition;
            CProjectileSkillRuntime projectileSkillRuntime = projectileObject.AddComponent<CProjectileSkillRuntime>();
            projectileSkillRuntime.Initialize(
                _skillContext,
                moveDirection,
                collisionRadius,
                travelDistance,
                travelSpeed,
                damageMultiplier,
                flatDamageBonus,
                GetSimultaneousTargetCount(),
                destroyOnFirstHit,
                debuffEffectList
            );
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
        /// 발사체 끝 지점 계산
        ///</summary>
        private Vector2 ResolveProjectileEndPosition( Transform _ownerTransform )
        {
            float facingDirection = ResolveFacingDirection( _ownerTransform );
            Vector2 resolvedOffset = ResolveSpawnOffset( facingDirection );
            Vector2 ownerPosition = _ownerTransform.position;
            Vector2 projectileEndPosition = ownerPosition + resolvedOffset + new Vector2( travelDistance * facingDirection, 0.0f );
            return projectileEndPosition;
        }

        ///<summary>
        /// 발사체 생성 오프셋 계산
        ///</summary>
        private Vector2 ResolveSpawnOffset( float _facingDirection )
        {
            Vector2 resolvedOffset = spawnOffset;
            resolvedOffset.x *= _facingDirection;
            return resolvedOffset;
        }

        ///<summary>
        /// 바라보기 방향 계산
        ///</summary>
        private float ResolveFacingDirection( Transform _ownerTransform )
        {
            if ( _ownerTransform == null )
            {
                return 1.0f;
            }

            float scaleX = _ownerTransform.localScale.x;
            float result = scaleX < 0.0f ? -1.0f : 1.0f;
            return result;
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
