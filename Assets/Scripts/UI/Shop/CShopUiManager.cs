using System;
using System.Collections;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 상점 UI 생성 및 제어 매니저
    ///</summary>
    public sealed class CShopUiManager : CSingleTon<CShopUiManager>
    {
        private readonly CPopupAsyncHandle<PopupShop> shopPopupHandle = new CPopupAsyncHandle<PopupShop>( eResourceKey.POPUP_SHOP, true );
        private bool isShopOpening;

        ///<summary>
        /// 상점 UI 열기 처리
        ///</summary>
        public bool OpenShop( string _shopId, string _shopDisplayName, CPlayerInventoryManager _targetInventoryManager )
        {
            if ( string.IsNullOrWhiteSpace( _shopId ) || _targetInventoryManager == null )
            {
                return false;
            }

            bool hasShopDefinition = CShopDefinitionDatabase.TryGetShopDefinition( _shopId, out CShopDefinition shopDefinition );

            if ( hasShopDefinition == false || shopDefinition == null )
            {
                CToastMessageSystem.Show( "상점 데이터를 찾지 못했습니다." );
                return false;
            }

            if ( isShopOpening )
            {
                return false;
            }

            StartCoroutine( IE_OpenShop( shopDefinition, _shopDisplayName, _targetInventoryManager ) );
            return true;
        }

        ///<summary>
        /// 상점 UI 닫기 처리
        ///</summary>
        public void CloseShop()
        {
            PopupShop cachedPopupShop = shopPopupHandle.GetCachedPopup();

            if ( cachedPopupShop == null )
            {
                return;
            }

            cachedPopupShop.CloseShop();
        }

        ///<summary>
        /// 상점 UI 표시 여부 반환
        ///</summary>
        public bool IsShopVisible()
        {
            PopupShop cachedPopupShop = shopPopupHandle.GetCachedPopup();
            bool result = cachedPopupShop != null && cachedPopupShop.IsNavigationVisible();
            return result;
        }

        ///<summary>
        /// 상점 UI 비동기 열기 코루틴
        ///</summary>
        private IEnumerator IE_OpenShop( CShopDefinition _shopDefinition, string _shopDisplayName, CPlayerInventoryManager _targetInventoryManager )
        {
            isShopOpening = true;
            PopupShop resolvedPopupShop = null;
            bool isShopPopupResolved = false;
            RequestPopupShop(
                ( PopupShop _popupShop ) =>
                {
                    resolvedPopupShop = _popupShop;
                    isShopPopupResolved = true;
                } );

            while ( isShopPopupResolved == false )
            {
                yield return null;
            }

            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;

            if ( resolvedPopupShop == null || itemInventoryUiManager == null )
            {
                isShopOpening = false;
                yield break;
            }

            Canvas targetCanvas = resolvedPopupShop.GetComponentInParent<Canvas>();
            resolvedPopupShop.SetTargetCanvas( targetCanvas );
            itemInventoryUiManager.SetInventoryToggleLocked( true );
            PopupItemInventory inventoryUi = null;
            bool isInventoryResolved = false;
            itemInventoryUiManager.OpenInventoryForShopAsync(
                _targetInventoryManager,
                ( PopupItemInventory _inventoryUi ) =>
                {
                    inventoryUi = _inventoryUi;
                    isInventoryResolved = true;
                } );

            while ( isInventoryResolved == false )
            {
                yield return null;
            }

            if ( inventoryUi == null )
            {
                itemInventoryUiManager.SetInventoryToggleLocked( false );
                isShopOpening = false;
                yield break;
            }

            resolvedPopupShop.ShowShop( _shopDefinition, _shopDisplayName, _targetInventoryManager, inventoryUi );
            isShopOpening = false;
        }

        ///<summary>
        /// 상점 팝업 인스턴스 비동기 요청
        ///</summary>
        private void RequestPopupShop( Action<PopupShop> _onCompleted )
        {
            shopPopupHandle.Request(
                ( PopupShop _createdPopupShop ) =>
                {
                    InvokePopupShopCompletedHandler( _onCompleted, _createdPopupShop );
                } );
        }

        ///<summary>
        /// 상점 팝업 요청 완료 콜백 호출
        ///</summary>
        private void InvokePopupShopCompletedHandler( Action<PopupShop> _onCompleted, PopupShop _popupShop )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _popupShop );
        }
    }
}
