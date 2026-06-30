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
        private GameObject shopPopupPrefabObject;
        private PopupShop popupShopInstance;

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

            PopupShop popupShop = ResolveOrCreatePopupShop();
            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;

            if ( popupShop == null || itemInventoryUiManager == null )
            {
                return false;
            }

            Canvas targetCanvas = popupShop.GetComponentInParent<Canvas>();
            popupShop.SetTargetCanvas( targetCanvas );

            itemInventoryUiManager.SetInventoryToggleLocked( true );
            PopupItemInventory inventoryUi = itemInventoryUiManager.OpenInventoryForShop( _targetInventoryManager );

            if ( inventoryUi == null )
            {
                itemInventoryUiManager.SetInventoryToggleLocked( false );
                return false;
            }

            popupShop.ShowShop( shopDefinition, _shopDisplayName, _targetInventoryManager, inventoryUi );
            return true;
        }

        ///<summary>
        /// 상점 UI 닫기 처리
        ///</summary>
        public void CloseShop()
        {
            if ( popupShopInstance == null )
            {
                return;
            }

            popupShopInstance.CloseShop();
        }

        ///<summary>
        /// 상점 UI 표시 여부 반환
        ///</summary>
        public bool IsShopVisible()
        {
            bool result = popupShopInstance != null && popupShopInstance.IsNavigationVisible();
            return result;
        }

        ///<summary>
        /// 상점 팝업 인스턴스 보장
        ///</summary>
        private PopupShop ResolveOrCreatePopupShop()
        {
            if ( popupShopInstance != null )
            {
                return popupShopInstance;
            }

            if ( shopPopupPrefabObject == null )
            {
                CResourceManager resourceManager = CResourceManager.Instance;
                shopPopupPrefabObject = resourceManager != null ? resourceManager.GetShopPopupPrefab() : null;
            }

            if ( shopPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupShop createdPopupShop = navigationController.AddPopup<PopupShop>( shopPopupPrefabObject, true );

            if ( createdPopupShop == null )
            {
                return null;
            }

            popupShopInstance = createdPopupShop;
            return popupShopInstance;
        }
    }
}
