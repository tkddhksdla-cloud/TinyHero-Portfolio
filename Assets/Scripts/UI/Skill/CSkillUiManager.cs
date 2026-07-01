using TinyHero.Core;
using TinyHero.Skill;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CSkillUiManager : CSingleTon<CSkillUiManager>
    {
        private CSkillManager targetSkillManager;
        private PopupSkillList skillUiController;

        ///<summary>
        /// 스킬 매니저 바인딩
        ///</summary>
        public void BindSkillManager( CSkillManager _targetSkillManager )
        {
            targetSkillManager = _targetSkillManager;

            if ( skillUiController == null )
            {
                return;
            }

            skillUiController.BindSkillManager( targetSkillManager );
        }

        ///<summary>
        /// 스킬 UI 토글 처리
        ///</summary>
        public void ToggleSkillUi()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            if ( skillUiController != null )
            {
                skillUiController.ToggleSkillWindow();
                return;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return;
            }

            navigationController.AddPopupAsync<PopupSkillList>(
                eResourceKey.POPUP_SKILL_LIST,
                true,
                ( PopupSkillList _createdSkillUiController ) =>
                {
                    if ( _createdSkillUiController == null )
                    {
                        return;
                    }

                    skillUiController = _createdSkillUiController;
                    skillUiController.BindSkillManager( targetSkillManager );
                } );
        }
    }
}
