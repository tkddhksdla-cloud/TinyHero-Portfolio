using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CItemInventoryUiManager : CSingleTon<CItemInventoryUiManager>
    {
        private const string InventoryPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupItemInventory";
        private const string LegacyInventoryPopupPrefabResourcePath = "Prefabs/UI/Inventory/PopupItemInventory";

        private CPlayerInventoryManager targetInventoryManager;
        private GameObject inventoryPopupPrefabObject;
        private PopupItemInventory inventoryUiController;

        ///<summary>
        /// 플레이어 인벤토리 매니저 바인딩
        ///</summary>
        public void BindInventoryManager( CPlayerInventoryManager _targetInventoryManager )
        {
            targetInventoryManager = _targetInventoryManager;

            if ( inventoryUiController == null )
            {
                return;
            }

            inventoryUiController.BindInventoryManager( targetInventoryManager );
        }

        ///<summary>
        /// 인벤토리 UI 토글 처리
        ///</summary>
        public void ToggleInventoryUi()
        {
            if ( targetInventoryManager == null )
            {
                return;
            }

            bool shouldCreateInventoryUi = inventoryUiController == null;
            PopupItemInventory resolvedInventoryUiController = ResolveOrCreateInventoryUiController();

            if ( resolvedInventoryUiController == null )
            {
                return;
            }

            if ( shouldCreateInventoryUi )
            {
                return;
            }

            bool nextVisibleState = resolvedInventoryUiController.IsInventoryVisible() == false;
            resolvedInventoryUiController.SetInventoryVisible( nextVisibleState );
        }

        ///<summary>
        /// 인벤토리 UI 컨트롤러 결정
        ///</summary>
        private PopupItemInventory ResolveOrCreateInventoryUiController()
        {
            if ( inventoryUiController != null )
            {
                inventoryUiController.BindInventoryManager( targetInventoryManager );
                return inventoryUiController;
            }

            if ( inventoryPopupPrefabObject == null )
            {
                inventoryPopupPrefabObject = LoadInventoryPopupPrefabObject();
            }

            if ( inventoryPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupItemInventory createdInventoryUiController = navigationController.AddPopup<PopupItemInventory>( inventoryPopupPrefabObject, true );

            if ( createdInventoryUiController == null )
            {
                return null;
            }

            createdInventoryUiController.BindInventoryManager( targetInventoryManager );
            inventoryUiController = createdInventoryUiController;
            return inventoryUiController;
        }

        ///<summary>
        /// 인벤토리 팝업 프리팹 로드
        ///</summary>
        private GameObject LoadInventoryPopupPrefabObject()
        {
            GameObject loadedPrefabObject = Resources.Load<GameObject>( InventoryPopupPrefabResourcePath );

            if ( loadedPrefabObject != null )
            {
                return loadedPrefabObject;
            }

            GameObject legacyLoadedPrefabObject = Resources.Load<GameObject>( LegacyInventoryPopupPrefabResourcePath );
            return legacyLoadedPrefabObject;
        }
    }
}
