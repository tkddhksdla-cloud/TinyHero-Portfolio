using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 적 방어력 비율 감소 디버프 정의
    ///</summary>
    [CreateAssetMenu( fileName = "DefReductionDebuffEffect", menuName = "TinyHero/Skill/Effect/Debuff/Defense Reduction" )]
    public sealed class CDefReductionDebuffEffect : CEnemyDebuffEffectBase
    {
        [SerializeField] private float durationSeconds = 3.0f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float reductionPercent = 0.2f;

        ///<summary>
        /// 방어력 감소 디버프 데이터 구성
        ///</summary>
        public void Configure( float _durationSeconds, float _reductionPercent, float _applyChance )
        {
            durationSeconds = Mathf.Max( 0.0f, _durationSeconds );
            reductionPercent = Mathf.Clamp01( _reductionPercent );
            SetApplyChance( _applyChance );
        }

        ///<summary>
        /// 방어력 감소 지속시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            float result = Mathf.Max( 0.0f, durationSeconds );
            return result;
        }

        ///<summary>
        /// 방어력 감소 비율 반환
        ///</summary>
        public float GetReductionPercent()
        {
            float result = Mathf.Clamp01( reductionPercent );
            return result;
        }

        ///<summary>
        /// 디버프 적용 처리
        ///</summary>
        protected override bool ApplyDebuff( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            _monsterObject.ApplyDefReductionDebuff( reductionPercent, durationSeconds );
            return true;
        }
    }
}
