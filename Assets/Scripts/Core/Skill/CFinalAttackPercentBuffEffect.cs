using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 플레이어 최종 공격력 비율 증가 버프 정의
    ///</summary>
    [CreateAssetMenu( fileName = "FinalAttackPercentBuffEffect", menuName = "TinyHero/Skill/Effect/Buff/Final Attack Percent" )]
    public sealed class CFinalAttackPercentBuffEffect : CPlayerBuffEffectBase
    {
        [SerializeField] private float durationSeconds = 5.0f;
        [SerializeField] [Range( 0.0f, 5.0f )] private float increasePercent = 0.25f;

        ///<summary>
        /// 공격력 증가 버프 데이터 구성
        ///</summary>
        public void Configure( float _durationSeconds, float _increasePercent )
        {
            durationSeconds = Mathf.Max( 0.0f, _durationSeconds );
            increasePercent = Mathf.Max( 0.0f, _increasePercent );
        }

        ///<summary>
        /// 최종 공격력 증가 유지 시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            float result = Mathf.Max( 0.0f, durationSeconds );
            return result;
        }

        ///<summary>
        /// 최종 공격력 증가 비율 반환
        ///</summary>
        public float GetIncreasePercent()
        {
            float result = Mathf.Max( 0.0f, increasePercent );
            return result;
        }

        ///<summary>
        /// 버프 적용 처리
        ///</summary>
        public override bool ApplyBuff( CSkillContext _skillContext )
        {
            if ( _skillContext == null )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();

            if ( playerController == null )
            {
                return false;
            }

            playerController.ApplyFinalAttackPercentBuff( increasePercent, durationSeconds );
            return true;
        }
    }
}
