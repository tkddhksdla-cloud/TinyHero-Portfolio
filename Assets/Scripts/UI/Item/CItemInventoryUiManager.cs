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
        private CPlayerInventoryManager targetInventoryManager;
        private GameObject inventoryPopupPrefabObject;
        private PopupItemInventory inventoryUiController;
        private bool isInventoryToggleLocked;

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
        /// 상점 연동용 인벤토리 열기 처리
        ///</summary>
        public PopupItemInventory OpenInventoryForShop( CPlayerInventoryManager _targetInventoryManager )
        {
            if ( _targetInventoryManager != null )
            {
                targetInventoryManager = _targetInventoryManager;
            }

            PopupItemInventory resolvedInventoryUiController = ResolveOrCreateInventoryUiController();

            if ( resolvedInventoryUiController == null )
            {
                return null;
            }

            resolvedInventoryUiController.BindInventoryManager( targetInventoryManager );
            resolvedInventoryUiController.SetEquipmentStatusPanelVisible( false );
            resolvedInventoryUiController.SetInventoryVisible( true );
            resolvedInventoryUiController.SnapWindowToRightSide();
            return resolvedInventoryUiController;
        }

        ///<summary>
        /// 상점 연동용 인벤토리 닫기 처리
        ///</summary>
        public void CloseInventoryForShop()
        {
            if ( inventoryUiController == null )
            {
                return;
            }

            inventoryUiController.SetEquipmentStatusPanelVisible( true );
            inventoryUiController.SetInventoryVisible( false );
        }

        ///<summary>
        /// 인벤토리 토글 잠금 상태 설정
        ///</summary>
        public void SetInventoryToggleLocked( bool _isLocked )
        {
            isInventoryToggleLocked = _isLocked;
        }

        ///<summary>
        /// 인벤토리 토글 잠금 상태 반환
        ///</summary>
        public bool IsInventoryToggleLocked()
        {
            bool result = isInventoryToggleLocked;
            return result;
        }

        ///<summary>
        /// 인벤토리 UI 컨트롤러 반환
        ///</summary>
        public PopupItemInventory GetInventoryUiController()
        {
            PopupItemInventory result = inventoryUiController;
            return result;
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
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                return null;
            }

            GameObject loadedPrefabObject = resourceManager.GetInventoryPopupPrefab();
            return loadedPrefabObject;
        }
    }
}
