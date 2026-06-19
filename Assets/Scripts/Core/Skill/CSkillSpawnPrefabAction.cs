using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 프리팹 생성형 액티브 스킬 정의
    ///</summary>
    [CreateAssetMenu( fileName = "SkillSpawnPrefabAction", menuName = "TinyHero/Skill/Spawn Prefab Action" )]
    public sealed class CSkillSpawnPrefabAction : CSkillActionBase
    {
        [SerializeField] private GameObject skillPrefab;
        [SerializeField] private Vector3 spawnOffset;
        [SerializeField] private bool useFacingDirection = true;
        [SerializeField] private bool alignPrefabScaleToFacing = true;
        [SerializeField] private bool attachToOwner;
        [SerializeField] private float lifetimeSeconds = 5.0f;

        ///<summary>
        /// 스킬 실행 가능 여부 판정
        ///</summary>
        public override bool CanExecute( CSkillContext _skillContext )
        {
            if ( base.CanExecute( _skillContext ) == false )
            {
                return false;
            }

            bool result = skillPrefab != null;
            return result;
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
            float facingDirection = ResolveFacingDirection( ownerTransform );
            Vector3 adjustedOffset = ResolveSpawnOffset( facingDirection );
            Vector3 spawnPosition = ownerTransform.position + adjustedOffset;
            Quaternion spawnRotation = ownerTransform.rotation;
            Transform parentTransform = attachToOwner ? ownerTransform : null;
            GameObject createdSkillObject = Instantiate( skillPrefab, spawnPosition, spawnRotation, parentTransform );
            createdSkillObject.name = skillPrefab.name;
            CSkillRenderUtility.ApplyForegroundSorting( createdSkillObject );
            ApplyFacingScale( createdSkillObject.transform, facingDirection );
            DestroySkillObjectAfterLifetime( createdSkillObject );
            return true;
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
        /// 생성 오프셋 계산
        ///</summary>
        private Vector3 ResolveSpawnOffset( float _facingDirection )
        {
            Vector3 resolvedOffset = spawnOffset;

            if ( useFacingDirection == false )
            {
                return resolvedOffset;
            }

            resolvedOffset.x *= _facingDirection;
            return resolvedOffset;
        }

        ///<summary>
        /// 방향 기반 스케일 반영
        ///</summary>
        private void ApplyFacingScale( Transform _targetTransform, float _facingDirection )
        {
            if ( alignPrefabScaleToFacing == false || _targetTransform == null )
            {
                return;
            }

            Vector3 localScale = _targetTransform.localScale;
            float absoluteScaleX = Mathf.Abs( localScale.x );
            localScale.x = absoluteScaleX * _facingDirection;
            _targetTransform.localScale = localScale;
        }

        ///<summary>
        /// 수명 종료 예약 처리
        ///</summary>
        private void DestroySkillObjectAfterLifetime( GameObject _createdSkillObject )
        {
            if ( _createdSkillObject == null )
            {
                return;
            }

            if ( lifetimeSeconds <= 0.0f )
            {
                return;
            }

            Destroy( _createdSkillObject, lifetimeSeconds );
        }
    }
}
