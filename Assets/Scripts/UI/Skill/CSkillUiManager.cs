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
        private readonly CPopupAsyncHandle<PopupSkillList> skillPopupHandle = new CPopupAsyncHandle<PopupSkillList>( eResourceKey.POPUP_SKILL_LIST, true );

        ///<summary>
        /// 스킬 매니저 바인딩
        ///</summary>
        public void BindSkillManager( CSkillManager _targetSkillManager )
        {
            targetSkillManager = _targetSkillManager;

            PopupSkillList cachedSkillUiController = skillPopupHandle.GetCachedPopup();

            if ( cachedSkillUiController == null )
            {
                return;
            }

            cachedSkillUiController.BindSkillManager( targetSkillManager );
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

            PopupSkillList cachedSkillUiController = skillPopupHandle.GetCachedPopup();

            if ( cachedSkillUiController != null )
            {
                cachedSkillUiController.ToggleSkillWindow();
                return;
            }

            skillPopupHandle.Request(
                ( PopupSkillList _createdSkillUiController ) =>
                {
                    if ( _createdSkillUiController == null )
                    {
                        return;
                    }

                    _createdSkillUiController.BindSkillManager( targetSkillManager );
                } );
        }
    }
}
