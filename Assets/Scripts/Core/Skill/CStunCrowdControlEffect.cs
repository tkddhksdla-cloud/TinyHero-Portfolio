using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 기절 군중제어 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "StunCrowdControlEffect", menuName = "TinyHero/Skill/Effect/Crowd Control/Stun" )]
    public sealed class CStunCrowdControlEffect : CEnemyCrowdControlEffectBase
    {
        ///<summary>
        /// 기절 군중제어 적용 처리
        ///</summary>
        protected override bool ApplyCrowdControl( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            float duration = GetDurationSeconds();

            if ( duration <= 0.0f )
            {
                return false;
            }

            _monsterObject.ApplyStunCrowdControl( duration, GetCrowdControlVfxPrefab(), GetCrowdControlVfxOffset() );
            return true;
        }
    }
}
