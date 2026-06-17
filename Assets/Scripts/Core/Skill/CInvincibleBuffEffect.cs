using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 플레이어 무적 버프 정의
    ///</summary>
    [CreateAssetMenu( fileName = "InvincibleBuffEffect", menuName = "TinyHero/Skill/Effect/Buff/Invincible" )]
    public sealed class CInvincibleBuffEffect : CPlayerBuffEffectBase
    {
        [SerializeField] private float durationSeconds = 2.0f;

        ///<summary>
        /// 무적 버프 데이터 구성
        ///</summary>
        public void Configure( float _durationSeconds )
        {
            durationSeconds = Mathf.Max( 0.0f, _durationSeconds );
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

            playerController.ApplySkillInvincibility( durationSeconds );
            return true;
        }
    }
}
