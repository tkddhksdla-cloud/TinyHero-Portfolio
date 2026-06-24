using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 넉백 군중제어 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "KnockbackCrowdControlEffect", menuName = "TinyHero/Skill/Effect/Crowd Control/Knockback" )]
    public sealed class CKnockbackCrowdControlEffect : CEnemyCrowdControlEffectBase
    {
        [SerializeField] private float distance = 1.5f;

        ///<summary>
        /// 넉백 거리 반환
        ///</summary>
        public float GetDistance()
        {
            float result = Mathf.Max( 0.0f, distance );
            return result;
        }

        ///<summary>
        /// 넉백 군중제어 적용 처리
        ///</summary>
        protected override bool ApplyCrowdControl( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            float duration = GetDurationSeconds();
            float knockbackDistance = GetDistance();

            if ( duration <= 0.0f || knockbackDistance <= 0.0f )
            {
                return false;
            }

            Transform ownerTransform = _skillContext.GetOwnerTransform();
            Vector3 sourceWorldPosition = ownerTransform != null ? ownerTransform.position : _monsterObject.transform.position;
            _monsterObject.ApplyKnockbackCrowdControl( duration, knockbackDistance, sourceWorldPosition, GetCrowdControlVfxPrefab(), GetCrowdControlVfxOffset() );
            return true;
        }
    }
}
