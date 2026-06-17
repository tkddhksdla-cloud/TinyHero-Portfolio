using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 적 대상 디버프 효과 베이스 정의
    ///</summary>
    public abstract class CEnemyDebuffEffectBase : ScriptableObject
    {
        [SerializeField] [Range( 0.0f, 1.0f )] private float applyChance = 1.0f;

        ///<summary>
        /// 디버프 적용 확률 설정
        ///</summary>
        protected void SetApplyChance( float _applyChance )
        {
            applyChance = Mathf.Clamp01( _applyChance );
        }

        ///<summary>
        /// 디버프 적용 시도
        ///</summary>
        public bool TryApply( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            if ( _skillContext == null || _monsterObject == null )
            {
                return false;
            }

            float randomValue = Random.value;

            if ( randomValue > applyChance )
            {
                return false;
            }

            bool result = ApplyDebuff( _skillContext, _monsterObject );
            return result;
        }

        ///<summary>
        /// 디버프 적용 처리
        ///</summary>
        protected abstract bool ApplyDebuff( CSkillContext _skillContext, MonsterObject _monsterObject );
    }
}
