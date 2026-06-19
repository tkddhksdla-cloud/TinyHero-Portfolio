using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 플레이어 레벨 기반 해금 조건 정의
    ///</summary>
    [CreateAssetMenu( fileName = "LevelUnlockCondition", menuName = "TinyHero/Skill/Condition/Level Unlock" )]
    public sealed class CLevelUnlockCondition : CSkillUnlockConditionBase
    {
        [SerializeField] private int requiredLevel = 1;

        ///<summary>
        /// 요구 레벨 설정
        ///</summary>
        public void Configure( int _requiredLevel )
        {
            requiredLevel = Mathf.Max( 1, _requiredLevel );
        }

        ///<summary>
        /// 요구 레벨 반환
        ///</summary>
        public int GetRequiredLevel()
        {
            int result = Mathf.Max( 1, requiredLevel );
            return result;
        }

        ///<summary>
        /// 해금 조건 충족 여부 반환
        ///</summary>
        public override bool IsSatisfied( CSkillManager _skillManager, int _playerLevel, CQuestStateProvider _questStateProvider )
        {
            bool result = _playerLevel >= GetRequiredLevel();
            return result;
        }
    }
}
