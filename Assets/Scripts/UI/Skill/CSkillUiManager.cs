using TinyHero.Core;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CSkillUiManager : CSingleTon<CSkillUiManager>
    {
        private CSkillManager targetSkillManager;
        private GameObject skillPopupPrefabObject;
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

            bool shouldCreateSkillUi = skillUiController == null;
            PopupSkillList resolvedSkillUiController = ResolveOrCreateSkillUiController();

            if ( resolvedSkillUiController == null )
            {
                return;
            }

            if ( shouldCreateSkillUi )
            {
                return;
            }

            resolvedSkillUiController.ToggleSkillWindow();
        }

        ///<summary>
        /// 스킬 UI 컨트롤러 결정
        ///</summary>
        private PopupSkillList ResolveOrCreateSkillUiController()
        {
            if ( skillUiController != null )
            {
                skillUiController.BindSkillManager( targetSkillManager );
                return skillUiController;
            }

            if ( skillPopupPrefabObject == null )
            {
                CResourceManager resourceManager = CResourceManager.Instance;

                if ( resourceManager == null )
                {
                    return null;
                }

                skillPopupPrefabObject = resourceManager.GetSkillPopupPrefab();
            }

            if ( skillPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupSkillList createdSkillUiController = navigationController.AddPopup<PopupSkillList>( skillPopupPrefabObject, true );

            if ( createdSkillUiController == null )
            {
                return null;
            }

            createdSkillUiController.BindSkillManager( targetSkillManager );
            skillUiController = createdSkillUiController;
            return skillUiController;
        }
    }
}
