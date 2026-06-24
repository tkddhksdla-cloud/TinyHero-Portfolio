using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 몬스터 군중제어 효과 베이스 정의
    ///</summary>
    public abstract class CEnemyCrowdControlEffectBase : ScriptableObject
    {
        [SerializeField] [Range( 0.0f, 1.0f )] private float applyChance = 1.0f;
        [SerializeField] private float durationSeconds = 0.5f;
        [SerializeField] private GameObject crowdControlVfxPrefab;
        [SerializeField] private Vector3 crowdControlVfxOffset;

        ///<summary>
        /// 군중제어 발동 확률 반환
        ///</summary>
        public float GetApplyChance()
        {
            float result = Mathf.Clamp01( applyChance );
            return result;
        }

        ///<summary>
        /// 군중제어 지속 시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            float result = Mathf.Max( 0.0f, durationSeconds );
            return result;
        }

        ///<summary>
        /// 군중제어 이펙트 프리팹 반환
        ///</summary>
        public GameObject GetCrowdControlVfxPrefab()
        {
            GameObject result = crowdControlVfxPrefab;
            return result;
        }

        ///<summary>
        /// 군중제어 이펙트 오프셋 반환
        ///</summary>
        public Vector3 GetCrowdControlVfxOffset()
        {
            Vector3 result = crowdControlVfxOffset;
            return result;
        }

        ///<summary>
        /// 군중제어 발동 시도
        ///</summary>
        public bool TryApply( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            if ( _skillContext == null || _monsterObject == null )
            {
                return false;
            }

            float randomValue = Random.value;

            if ( randomValue > GetApplyChance() )
            {
                return false;
            }

            bool result = ApplyCrowdControl( _skillContext, _monsterObject );
            return result;
        }

        ///<summary>
        /// 군중제어 적용 처리
        ///</summary>
        protected abstract bool ApplyCrowdControl( CSkillContext _skillContext, MonsterObject _monsterObject );
    }
}
