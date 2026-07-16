using System;
using TinyHero.Core;
using TinyHero.Player;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CItemInventoryUiManager : CSingleTon<CItemInventoryUiManager>
    {
        private CPlayerInventoryManager targetInventoryManager;
        private readonly CPopupAsyncHandle<PopupItemInventory> inventoryPopupHandle = new CPopupAsyncHandle<PopupItemInventory>( eResourceKey.POPUP_ITEM_INVENTORY, true );
        private bool isInventoryToggleLocked;

        ///<summary>
        /// 플레이어 인벤토리 매니저 바인딩
        ///</summary>
        public void BindInventoryManager( CPlayerInventoryManager _targetInventoryManager )
        {
            targetInventoryManager = _targetInventoryManager;

            PopupItemInventory cachedInventoryUiController = inventoryPopupHandle.GetCachedPopup();

            if ( cachedInventoryUiController == null )
            {
                return;
            }

            cachedInventoryUiController.BindInventoryManager( targetInventoryManager );
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

            PopupItemInventory cachedInventoryUiController = inventoryPopupHandle.GetCachedPopup();

            if ( cachedInventoryUiController != null )
            {
                bool nextVisibleState = cachedInventoryUiController.IsInventoryVisible() == false;
                cachedInventoryUiController.SetInventoryVisible( nextVisibleState );
                return;
            }

            RequestInventoryUiController(
                ( PopupItemInventory _createdInventoryUiController ) =>
                {
                    if ( _createdInventoryUiController == null )
                    {
                        return;
                    }
                } );
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

            PopupItemInventory cachedInventoryUiController = inventoryPopupHandle.GetCachedPopup();

            if ( cachedInventoryUiController == null )
            {
                return null;
            }

            ConfigureInventoryForShop( cachedInventoryUiController );
            return cachedInventoryUiController;
        }

        ///<summary>
        /// 상점 연동용 인벤토리 비동기 열기 처리
        ///</summary>
        public void OpenInventoryForShopAsync( CPlayerInventoryManager _targetInventoryManager, Action<PopupItemInventory> _onCompleted )
        {
            if ( _targetInventoryManager != null )
            {
                targetInventoryManager = _targetInventoryManager;
            }

            RequestInventoryUiController(
                ( PopupItemInventory _resolvedInventoryUiController ) =>
                {
                    if ( _resolvedInventoryUiController != null )
                    {
                        ConfigureInventoryForShop( _resolvedInventoryUiController );
                    }

                    InvokeInventoryUiControllerCompletedHandler( _onCompleted, _resolvedInventoryUiController );
                } );
        }

        ///<summary>
        /// 상점 연동용 인벤토리 닫기 처리
        ///</summary>
        public void CloseInventoryForShop()
        {
            PopupItemInventory cachedInventoryUiController = inventoryPopupHandle.GetCachedPopup();

            if ( cachedInventoryUiController == null )
            {
                return;
            }

            cachedInventoryUiController.SetEquipmentStatusPanelVisible( true );
            cachedInventoryUiController.SetInventoryVisible( false );
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
            PopupItemInventory result = inventoryPopupHandle.GetCachedPopup();
            return result;
        }

        ///<summary>
        /// 인벤토리 UI 컨트롤러 비동기 요청
        ///</summary>
        private void RequestInventoryUiController( Action<PopupItemInventory> _onCompleted )
        {
            inventoryPopupHandle.Request(
                ( PopupItemInventory _createdInventoryUiController ) =>
                {
                    if ( _createdInventoryUiController != null )
                    {
                        _createdInventoryUiController.BindInventoryManager( targetInventoryManager );
                    }

                    InvokeInventoryUiControllerCompletedHandler( _onCompleted, _createdInventoryUiController );
                } );
        }

        ///<summary>
        /// 상점 연동용 인벤토리 표시 상태 구성
        ///</summary>
        private void ConfigureInventoryForShop( PopupItemInventory _inventoryUiController )
        {
            if ( _inventoryUiController == null )
            {
                return;
            }

            _inventoryUiController.BindInventoryManager( targetInventoryManager );
            _inventoryUiController.SetEquipmentStatusPanelVisible( false );
            _inventoryUiController.SetInventoryVisible( true );
            _inventoryUiController.SnapWindowToRightSide();
        }

        ///<summary>
        /// 인벤토리 UI 컨트롤러 요청 완료 콜백 호출
        ///</summary>
        private void InvokeInventoryUiControllerCompletedHandler( Action<PopupItemInventory> _onCompleted, PopupItemInventory _inventoryUiController )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _inventoryUiController );
        }
    }
}
