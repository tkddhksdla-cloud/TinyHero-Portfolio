using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 적 공격력 고정 감소 디버프 정의
    ///</summary>
    [CreateAssetMenu( fileName = "AtkReductionDebuffEffect", menuName = "TinyHero/Skill/Effect/Debuff/Attack Reduction" )]
    public sealed class CAtkReductionDebuffEffect : CEnemyDebuffEffectBase
    {
        [SerializeField] private float durationSeconds = 3.0f;
        [SerializeField] private long reductionAmount = 1;

        ///<summary>
        /// 공격력 감소 디버프 데이터 구성
        ///</summary>
        public void Configure( float _durationSeconds, long _reductionAmount, float _applyChance )
        {
            durationSeconds = Mathf.Max( 0.0f, _durationSeconds );
            reductionAmount = _reductionAmount > 0 ? _reductionAmount : 0;
            SetApplyChance( _applyChance );
        }

        ///<summary>
        /// 공격력 감소 지속시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            float result = Mathf.Max( 0.0f, durationSeconds );
            return result;
        }

        ///<summary>
        /// 공격력 감소 수치 반환
        ///</summary>
        public long GetReductionAmount()
        {
            long result = reductionAmount > 0 ? reductionAmount : 0;
            return result;
        }

        ///<summary>
        /// 디버프 적용 처리
        ///</summary>
        protected override bool ApplyDebuff( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            _monsterObject.ApplyAtkReductionDebuff( reductionAmount, durationSeconds );
            return true;
        }
    }
}
