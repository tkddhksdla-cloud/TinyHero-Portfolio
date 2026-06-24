using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 퀘스트 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CQuestUiManager : CSingleTon<CQuestUiManager>
    {
        private const string NpcQuestPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupQuestList";
        private const string PlayerQuestPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupQuestList_Mine";

        private PlayerController targetPlayerController;
        private GameObject npcQuestPopupPrefabObject;
        private GameObject playerQuestPopupPrefabObject;
        private PopupQuestList npcQuestUiController;
        private PopupQuestList playerQuestUiController;

        ///<summary>
        /// 플레이어 제어 컴포넌트 바인딩
        ///</summary>
        public void BindPlayerController( PlayerController _targetPlayerController )
        {
            targetPlayerController = _targetPlayerController;
        }

        ///<summary>
        /// 플레이어 퀘스트 저널 토글 처리
        ///</summary>
        public void TogglePlayerQuestListUi()
        {
            PlayerController resolvedPlayerController = ResolvePlayerController();

            if ( resolvedPlayerController == null )
            {
                return;
            }

            bool shouldCreateQuestUi = playerQuestUiController == null;
            PopupQuestList resolvedQuestUiController = ResolveOrCreatePlayerQuestUiController();

            if ( resolvedQuestUiController == null )
            {
                return;
            }

            if ( shouldCreateQuestUi )
            {
                resolvedQuestUiController.SetLayerVisible( false );
            }

            bool hasVisibleQuestUi = PopupQuestList.IsAnyQuestUiVisible();
            bool isPlayerQuestUiVisible = resolvedQuestUiController.IsQuestListVisible();

            if ( isPlayerQuestUiVisible )
            {
                resolvedQuestUiController.TogglePlayerQuestListUi( resolvedPlayerController );
                return;
            }

            if ( hasVisibleQuestUi )
            {
                return;
            }

            resolvedQuestUiController.TogglePlayerQuestListUi( resolvedPlayerController );
        }

        ///<summary>
        /// NPC 퀘스트 목록 UI 표시
        ///</summary>
        public void ShowNpcQuestListUi( CNPCObject _npcObject, PlayerController _playerController )
        {
            if ( _npcObject == null || _playerController == null )
            {
                return;
            }

            PopupQuestList resolvedQuestUiController = ResolveOrCreateNpcQuestUiController();

            if ( resolvedQuestUiController == null )
            {
                return;
            }

            resolvedQuestUiController.SetLayerVisible( false );
            resolvedQuestUiController.ShowQuestListUi( _npcObject, _playerController );
        }

        ///<summary>
        /// 플레이어 퀘스트 UI 컨트롤러 결정
        ///</summary>
        private PopupQuestList ResolveOrCreatePlayerQuestUiController()
        {
            if ( playerQuestUiController != null )
            {
                return playerQuestUiController;
            }

            if ( playerQuestPopupPrefabObject == null )
            {
                playerQuestPopupPrefabObject = Resources.Load<GameObject>( PlayerQuestPopupPrefabResourcePath );
            }

            if ( playerQuestPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupQuestList createdQuestUiController = navigationController.AddPopup<PopupQuestList>( playerQuestPopupPrefabObject, true );

            if ( createdQuestUiController == null )
            {
                return null;
            }

            playerQuestUiController = createdQuestUiController;
            return playerQuestUiController;
        }

        ///<summary>
        /// NPC 퀘스트 UI 컨트롤러 결정
        ///</summary>
        private PopupQuestList ResolveOrCreateNpcQuestUiController()
        {
            if ( npcQuestUiController != null )
            {
                return npcQuestUiController;
            }

            if ( npcQuestPopupPrefabObject == null )
            {
                npcQuestPopupPrefabObject = Resources.Load<GameObject>( NpcQuestPopupPrefabResourcePath );
            }

            if ( npcQuestPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupQuestList createdQuestUiController = navigationController.AddPopup<PopupQuestList>( npcQuestPopupPrefabObject, true );

            if ( createdQuestUiController == null )
            {
                return null;
            }

            npcQuestUiController = createdQuestUiController;
            return npcQuestUiController;
        }

        ///<summary>
        /// 활성 플레이어 제어 컴포넌트 결정
        ///</summary>
        private PlayerController ResolvePlayerController()
        {
            if ( targetPlayerController != null && targetPlayerController.gameObject.activeInHierarchy )
            {
                return targetPlayerController;
            }

            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );

            for ( int index = 0; index < playerControllerArray.Length; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null || playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                targetPlayerController = playerController;
                return targetPlayerController;
            }

            return null;
        }
    }
}
