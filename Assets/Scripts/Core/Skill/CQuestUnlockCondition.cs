using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 퀘스트 완료 기반 해금 조건 정의
    ///</summary>
    [CreateAssetMenu( fileName = "QuestUnlockCondition", menuName = "TinyHero/Skill/Condition/Quest Unlock" )]
    public sealed class CQuestUnlockCondition : CSkillUnlockConditionBase
    {
        [SerializeField] private string requiredQuestId = string.Empty;

        ///<summary>
        /// 요구 퀘스트 식별자 설정
        ///</summary>
        public void Configure( string _requiredQuestId )
        {
            requiredQuestId = _requiredQuestId;
        }

        ///<summary>
        /// 해금 조건 충족 여부 반환
        ///</summary>
        public override bool IsSatisfied( CSkillManager _skillManager, int _playerLevel, CQuestStateProvider _questStateProvider )
        {
            if ( _questStateProvider == null )
            {
                return false;
            }

            bool result = _questStateProvider.IsQuestCompleted( requiredQuestId );
            return result;
        }
    }
}
