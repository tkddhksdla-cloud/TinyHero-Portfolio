using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 패시브 스탯 증가 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "PassiveStatSkillEffect", menuName = "TinyHero/Skill/Effect/Passive/Stat Bonus" )]
    public sealed class CPassiveStatSkillEffect : CPassiveSkillEffectBase
    {
        [SerializeField] private ePlayerStatType targetStatType = ePlayerStatType.ATK;
        [SerializeField] private float bonusValue;

        ///<summary>
        /// 패시브 스탯 효과 데이터 구성
        ///</summary>
        public void Configure( ePlayerStatType _targetStatType, float _bonusValue )
        {
            targetStatType = _targetStatType;
            bonusValue = _bonusValue;
        }

        ///<summary>
        /// 대상 스탯 종류 반환
        ///</summary>
        public ePlayerStatType GetTargetStatType()
        {
            ePlayerStatType result = targetStatType;
            return result;
        }

        ///<summary>
        /// 기본 스탯 보너스 반환
        ///</summary>
        public float GetBonusValue()
        {
            float result = bonusValue;
            return result;
        }

        ///<summary>
        /// 패시브 효과를 스탯 보너스에 반영
        ///</summary>
        public override void ApplyPassiveEffect( CPlayerStatRuntimeData _targetStatBonusData )
        {
            if ( _targetStatBonusData == null )
            {
                return;
            }

            _targetStatBonusData.AddStatValue( targetStatType, bonusValue );
        }
    }
}
