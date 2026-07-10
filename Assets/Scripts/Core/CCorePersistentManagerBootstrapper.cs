using TinyHero.Core.Data;
using TinyHero.Maps;
using TinyHero.Player;
using TinyHero.UI;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 코어 영속 매니저 초기 부트스트랩
    ///</summary>
    public static class CCorePersistentManagerBootstrapper
    {
        private const string AudioManagerPrefabResourcePath = "Prefabs/Core/CAudioManager";

        ///<summary>
        /// 영속 매니저 선생성 처리
        ///</summary>
        [RuntimeInitializeOnLoadMethod( RuntimeInitializeLoadType.BeforeSceneLoad )]
        private static void Bootstrap()
        {
            CResourceManager.Instance.PreloadCoreResources();
            _ = CHotfixRuntimeLoader.Instance;
            _ = CObjectPoolManager.Instance;
            EnsureAudioManagerExists();
            CInputManager inputManager = CInputManager.Instance;
            CDataManager dataManager = CDataManager.Instance;
            _ = CGameSettingManager.Instance;
            CUINavigationController uiNavigationController = CUINavigationController.Instance;
            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;
            CShopUiManager shopUiManager = CShopUiManager.Instance;
            CCubeUiManager cubeUiManager = CCubeUiManager.Instance;
            CSkillUiManager skillUiManager = CSkillUiManager.Instance;
            CQuestUiManager questUiManager = CQuestUiManager.Instance;
            CSaveManager saveManager = CSaveManager.Instance;
            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;
            CToastMessageSystem toastMessageSystem = CToastMessageSystem.EnsureInstance();
            CMapManager mapManager = CMapManager.Instance;
            CNPCInteractionManager npcInteractionManager = CNPCInteractionManager.Instance;
            CMonsterInfoManager monsterInfoManager = CMonsterInfoManager.Instance;
            CNPCNameTagManager npcNameTagManager = CNPCNameTagManager.Instance;
            CPlayerNameTagManager playerNameTagManager = CPlayerNameTagManager.Instance;
        }

        ///<summary>
        /// 오디오 매니저 프리팹 인스턴스 보장
        ///</summary>
        private static void EnsureAudioManagerExists()
        {
            bool hasExistingAudioManager = CAudioManager.TryGetExistingInstance( out CAudioManager existingAudioManager );

            if ( hasExistingAudioManager && existingAudioManager != null )
            {
                return;
            }

            GameObject audioManagerPrefab = Resources.Load<GameObject>( AudioManagerPrefabResourcePath );

            if ( audioManagerPrefab == null )
            {
                _ = CAudioManager.Instance;
                return;
            }

            GameObject audioManagerObject = Object.Instantiate( audioManagerPrefab );
            audioManagerObject.name = audioManagerPrefab.name;
        }
    }
}
