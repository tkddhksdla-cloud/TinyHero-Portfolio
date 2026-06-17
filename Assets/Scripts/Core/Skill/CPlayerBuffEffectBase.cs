using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 플레이어 대상 이로운 효과 베이스 정의
    ///</summary>
    public abstract class CPlayerBuffEffectBase : ScriptableObject
    {
        ///<summary>
        /// 버프 적용 처리
        ///</summary>
        public abstract bool ApplyBuff( CSkillContext _skillContext );
    }
}
