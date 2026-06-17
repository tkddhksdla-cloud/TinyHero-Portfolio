using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 해금 조건 베이스 정의
    ///</summary>
    public abstract class CSkillUnlockConditionBase : ScriptableObject
    {
        ///<summary>
        /// 해금 조건 충족 여부 반환
        ///</summary>
        public abstract bool IsSatisfied( CSkillManager _skillManager, int _playerLevel, CQuestStateProvider _questStateProvider );
    }
}
