using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 패시브 스킬 효과 베이스 정의
    ///</summary>
    public abstract class CPassiveSkillEffectBase : ScriptableObject
    {
        ///<summary>
        /// 패시브 효과를 스탯 보너스에 반영
        ///</summary>
        public abstract void ApplyPassiveEffect( CPlayerStatRuntimeData _targetStatBonusData );
    }
}
