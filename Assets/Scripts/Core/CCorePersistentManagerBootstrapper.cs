using TinyHero.Core.Data;
using TinyHero.Maps;
using TinyHero.UI;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 코어 영속 매니저 초기 부트스트랩
    ///</summary>
    public static class CCorePersistentManagerBootstrapper
    {
        ///<summary>
        /// 영속 매니저 선생성 처리
        ///</summary>
        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
        private static void Bootstrap()
        {
            CInputManager inputManager = CInputManager.Instance;
            CDataManager dataManager = CDataManager.Instance;
            CUINavigationController uiNavigationController = CUINavigationController.Instance;
            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;
            CCubeUiManager cubeUiManager = CCubeUiManager.Instance;
            CSkillUiManager skillUiManager = CSkillUiManager.Instance;
            CQuestUiManager questUiManager = CQuestUiManager.Instance;
            CSaveManager saveManager = CSaveManager.Instance;
            CPopupCommonNoticeManager popupCommonNoticeManager = CPopupCommonNoticeManager.Instance;
            CMapManager mapManager = CMapManager.Instance;
            CNPCInteractionManager npcInteractionManager = CNPCInteractionManager.Instance;
            CMonsterInfoManager monsterInfoManager = CMonsterInfoManager.Instance;
            CNPCNameTagManager npcNameTagManager = CNPCNameTagManager.Instance;
        }
    }
}
