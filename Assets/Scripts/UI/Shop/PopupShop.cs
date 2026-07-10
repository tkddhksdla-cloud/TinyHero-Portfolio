using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 상점 팝업 UI 컴포넌트
    ///</summary>
    public sealed class PopupShop : CUIPopup
    {
        private const string DefaultPriceItemId = "GOLD";
        private const float WindowPairGap = 120.0f;

        [SerializeField] private RectTransform popupRootRectTransform;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private RectTransform contentRootRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private List<CShopSlot> shopSlotList = new List<CShopSlot>();

        private CPlayerInventoryManager targetInventoryManager;
        private CShopDefinition activeShopDefinition;
        private PopupItemInventory linkedInventoryUi;
        private bool isClosingLinkedUi;
        private bool isShopVisible;

        ///<summary>
        /// 상점 UI 초기화 처리
        ///</summary>
        private void Awake()
        {
            EnsureSlotObjects();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSellDropTarget();
            SetShopVisible( false );
        }

        ///<summary>
        /// 상점 UI 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSellDropTarget();
            BringLayerToFront();

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 상점 UI 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }

            UnbindInventoryUi();
        }

        ///<summary>
        /// 대상 캔버스 설정
        ///</summary>
        public void SetTargetCanvas( Canvas _targetCanvas )
        {
            targetCanvas = _targetCanvas;
        }

        ///<summary>
        /// 상점 표시 처리
        ///</summary>
        public void ShowShop( CShopDefinition _shopDefinition, string _shopDisplayName, CPlayerInventoryManager _targetInventoryManager, PopupItemInventory _linkedInventoryUi )
        {
            activeShopDefinition = _shopDefinition;
            targetInventoryManager = _targetInventoryManager;
            BindInventoryUi( _linkedInventoryUi );
            EnsureSlotObjects();
            RefreshSlotViews();
            SnapWindowToLeftSide();
            SetShopVisible( true );
        }

        ///<summary>
        /// 상점 닫기 처리
        ///</summary>
        public void CloseShop()
        {
            if ( isClosingLinkedUi )
            {
                return;
            }

            isClosingLinkedUi = true;
            SetShopVisible( false );
            HideTooltip( null );
            CInventoryUiDragState.EndDrag();
            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;

            if ( itemInventoryUiManager != null )
            {
                itemInventoryUiManager.SetInventoryToggleLocked( false );
                itemInventoryUiManager.CloseInventoryForShop();
            }

            UnbindInventoryUi();
            targetInventoryManager = null;
            activeShopDefinition = null;
            isClosingLinkedUi = false;
        }

        ///<summary>
        /// 슬롯 툴팁 표시 처리
        ///</summary>
        public void ShowTooltip( CShopSlot _shopSlot )
        {
            if ( _shopSlot == null || _shopSlot.HasItem() == false || linkedInventoryUi == null )
            {
                return;
            }

            int slotIndex = _shopSlot.GetSlotIndex();
            List<CShopEntryData> entryDataList = activeShopDefinition != null ? activeShopDefinition.GetShopEntryDataList() : null;

            if ( entryDataList == null || slotIndex < 0 || slotIndex >= entryDataList.Count )
            {
                return;
            }

            CShopEntryData entryData = entryDataList[ slotIndex ];
            string additionalInfoText = BuildShopTooltipText( entryData, _shopSlot.GetCurrentItemDefinition() );
            CUITooltipManager.ShowItemTooltip( _shopSlot.GetCurrentItemDefinition(), null, additionalInfoText );
        }

        ///<summary>
        /// 슬롯 툴팁 숨김 처리
        ///</summary>
        public void HideTooltip( CShopSlot _shopSlot )
        {
            CUITooltipManager.HideItemTooltip();
        }

        ///<summary>
        /// 슬롯 클릭 처리
        ///</summary>
        public void HandleSlotPointerClick( CShopSlot _shopSlot, PointerEventData _eventData )
        {
            if ( _shopSlot == null || _shopSlot.HasItem() == false )
            {
                return;
            }

            if ( _eventData != null && _eventData.button != PointerEventData.InputButton.Left && _eventData.button != PointerEventData.InputButton.Right )
            {
                return;
            }

            TryPromptPurchase( _shopSlot.GetSlotIndex() );
        }

        ///<summary>
        /// 드래그 판매 확인 팝업 표시 시도
        ///</summary>
        public void TryPromptSellDraggedInventoryItem()
        {
            int draggedSlotIndex = CInventoryUiDragState.GetDraggedSlotIndex();

            if ( draggedSlotIndex < 0 )
            {
                return;
            }

            TryPromptSellInventoryItem( draggedSlotIndex );
        }

        ///<summary>
        /// 네비게이션 표시 상태 반영
        ///</summary>
        public override void SetLayerVisible( bool _isVisible )
        {
            SetShopVisible( _isVisible );
        }

        ///<summary>
        /// 네비게이션 표시 상태 반환
        ///</summary>
        public override bool IsNavigationVisible()
        {
            bool result = isShopVisible;
            return result;
        }

        ///<summary>
        /// 네비게이션 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            CloseShop();
        }

        ///<summary>
        /// 상점 창 좌측 배치
        ///</summary>
        public void SnapWindowToLeftSide()
        {
            SnapWindowToHorizontalSide( false );
        }

        ///<summary>
        /// 슬롯 목록 갱신
        ///</summary>
        private void RefreshSlotViews()
        {
            EnsureSlotObjects();
            List<CShopEntryData> entryDataList = activeShopDefinition != null ? activeShopDefinition.GetShopEntryDataList() : null;

            for ( int index = 0; index < shopSlotList.Count; index++ )
            {
                CShopSlot shopSlot = shopSlotList[ index ];

                if ( shopSlot == null )
                {
                    continue;
                }

                if ( entryDataList == null || index >= entryDataList.Count )
                {
                    shopSlot.RefreshSlot( null, 0L );
                    continue;
                }

                CShopEntryData entryData = entryDataList[ index ];
                CItemDefinition itemDefinition = null;
                bool hasItemDefinition = entryData != null && CItemDefinitionDatabase.TryGetItemDefinition( entryData.GetItemId(), out itemDefinition );
                long itemCount = entryData != null ? entryData.GetItemCount() : 0L;
                shopSlot.RefreshSlot( hasItemDefinition ? itemDefinition : null, itemCount );
            }
        }

        ///<summary>
        /// 슬롯 목록 보장
        ///</summary>
        private void EnsureSlotObjects()
        {
            if ( contentRootRectTransform == null )
            {
                return;
            }

            if ( shopSlotList.Count == 0 )
            {
                int childCount = contentRootRectTransform.childCount;
                shopSlotList.Clear();

                for ( int index = 0; index < childCount; index++ )
                {
                    Transform childTransform = contentRootRectTransform.GetChild( index );
                    CShopSlot shopSlot = childTransform.GetComponent<CShopSlot>();

                    if ( shopSlot == null )
                    {
                        continue;
                    }

                    shopSlotList.Add( shopSlot );
                }
            }

            for ( int index = 0; index < shopSlotList.Count; index++ )
            {
                CShopSlot shopSlot = shopSlotList[ index ];

                if ( shopSlot == null )
                {
                    continue;
                }

                shopSlot.Initialize( this, index );
            }
        }

        ///<summary>
        /// 상점 표시 상태 반영
        ///</summary>
        private void SetShopVisible( bool _isVisible )
        {
            isShopVisible = _isVisible;

            if ( popupRootRectTransform == null )
            {
                return;
            }

            GameObject popupRootObject = popupRootRectTransform.gameObject;

            if ( popupRootObject.activeSelf != _isVisible )
            {
                popupRootObject.SetActive( _isVisible );
            }

            if ( _isVisible )
            {
                CUINavigationController navigationController = CUINavigationController.Instance;

                if ( navigationController != null )
                {
                    navigationController.RegisterPopup( this );
                }

                BringLayerToFront();
            }
        }

        ///<summary>
        /// 상점 창 좌우 배치
        ///</summary>
        private void SnapWindowToHorizontalSide( bool _isRightSide )
        {
            if ( windowRootRectTransform == null )
            {
                return;
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            RectTransform canvasRectTransform = targetCanvas != null ? targetCanvas.transform as RectTransform : null;

            if ( canvasRectTransform == null )
            {
                return;
            }

            Vector2 sideAnchoredPosition = ResolveSideWindowAnchoredPosition( canvasRectTransform, windowRootRectTransform, _isRightSide );
            windowRootRectTransform.anchoredPosition = sideAnchoredPosition;
        }

        ///<summary>
        /// 상점 창 좌우 배치 좌표 계산
        ///</summary>
        private Vector2 ResolveSideWindowAnchoredPosition( RectTransform _canvasRectTransform, RectTransform _windowRectTransform, bool _isRightSide )
        {
            Vector2 currentAnchoredPosition = _windowRectTransform.anchoredPosition;
            float windowWidth = _windowRectTransform.rect.width;
            float anchoredPosX = ( windowWidth * 0.5f ) + ( WindowPairGap * 0.5f );

            if ( _isRightSide == false )
            {
                anchoredPosX *= -1.0f;
            }

            Vector2 result = new Vector2( anchoredPosX, currentAnchoredPosition.y );
            return result;
        }

        ///<summary>
        /// 구매 확인 팝업 표시 처리
        ///</summary>
        private void TryPromptPurchase( int _shopEntryIndex )
        {
            if ( activeShopDefinition == null || targetInventoryManager == null )
            {
                return;
            }

            List<CShopEntryData> entryDataList = activeShopDefinition.GetShopEntryDataList();

            if ( entryDataList == null || _shopEntryIndex < 0 || _shopEntryIndex >= entryDataList.Count )
            {
                return;
            }

            CShopEntryData entryData = entryDataList[ _shopEntryIndex ];

            if ( entryData == null || CItemDefinitionDatabase.TryGetItemDefinition( entryData.GetItemId(), out CItemDefinition itemDefinition ) == false || itemDefinition == null )
            {
                CToastMessageSystem.Show( "판매 아이템 정보를 찾지 못했습니다." );
                return;
            }

            string descriptionText = BuildPurchaseNoticeText( entryData, itemDefinition );
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                ProcessPurchase( _shopEntryIndex );
                return;
            }

            navigationController.ShowCommonNotice( descriptionText, "구매", () => ProcessPurchase( _shopEntryIndex ), "취소", null );
        }

        ///<summary>
        /// 구매 실행 처리
        ///</summary>
        private void ProcessPurchase( int _shopEntryIndex )
        {
            if ( activeShopDefinition == null || targetInventoryManager == null )
            {
                return;
            }

            List<CShopEntryData> entryDataList = activeShopDefinition.GetShopEntryDataList();

            if ( entryDataList == null || _shopEntryIndex < 0 || _shopEntryIndex >= entryDataList.Count )
            {
                return;
            }

            CShopEntryData entryData = entryDataList[ _shopEntryIndex ];

            if ( entryData == null || CItemDefinitionDatabase.TryGetItemDefinition( entryData.GetItemId(), out CItemDefinition itemDefinition ) == false || itemDefinition == null )
            {
                CToastMessageSystem.Show( "판매 아이템 정보를 찾지 못했습니다." );
                return;
            }

            string priceItemId = entryData.GetPriceItemId();
            long priceAmount = entryData.GetPriceAmount();
            long itemCount = entryData.GetItemCount();

            if ( priceAmount > 0 )
            {
                bool didRemovePriceItem = targetInventoryManager.TryRemoveItem( priceItemId, priceAmount );

                if ( didRemovePriceItem == false )
                {
                    CToastMessageSystem.Show( "구매 재화가 부족합니다." );
                    return;
                }
            }

            bool didAddItem = targetInventoryManager.TryAddItem( itemDefinition, itemCount );

            if ( didAddItem == false )
            {
                if ( priceAmount > 0 )
                {
                    targetInventoryManager.TryAddItemById( priceItemId, priceAmount );
                }

                CToastMessageSystem.Show( "인벤토리 공간이 부족합니다." );
                return;
            }

            string purchasedItemToastMessage = $"[ {itemDefinition.GetItemName()} ] x{itemCount} (을)를 구매했습니다.";
            CToastMessageSystem.Show( purchasedItemToastMessage );
        }

        ///<summary>
        /// 판매 확인 팝업 표시 처리
        ///</summary>
        private void TryPromptSellInventoryItem( int _inventorySlotIndex )
        {
            if ( targetInventoryManager == null )
            {
                return;
            }

            CInventoryItemEntryData sourceEntryData = targetInventoryManager.GetItemEntryData( _inventorySlotIndex );
            CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot( _inventorySlotIndex );

            if ( sourceEntryData == null || sourceEntryData.IsEmpty() || itemDefinition == null )
            {
                return;
            }

            if ( itemDefinition.HasSellPrice() == false )
            {
                CToastMessageSystem.Show( "해당 아이템은 판매 가격이 설정되지 않았습니다." );
                return;
            }

            long itemQuantity = sourceEntryData.GetQuantity();
            long totalPrice = itemDefinition.GetSellPrice() * itemQuantity;
            string priceItemId = itemDefinition.GetSellPriceItemId();
            string priceText = BuildPriceText( priceItemId, totalPrice );
            string descriptionText = $"{itemDefinition.GetItemName()} x{itemQuantity}\n{priceText}\n판매하시겠습니까?";
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                ProcessSellInventoryItem( _inventorySlotIndex );
                return;
            }

            navigationController.ShowCommonNotice( descriptionText, "판매", () => ProcessSellInventoryItem( _inventorySlotIndex ), "취소", null );
        }

        ///<summary>
        /// 판매 실행 처리
        ///</summary>
        private void ProcessSellInventoryItem( int _inventorySlotIndex )
        {
            if ( targetInventoryManager == null )
            {
                return;
            }

            CInventoryItemEntryData sourceEntryData = targetInventoryManager.GetItemEntryData( _inventorySlotIndex );
            CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot( _inventorySlotIndex );

            if ( sourceEntryData == null || sourceEntryData.IsEmpty() || itemDefinition == null )
            {
                return;
            }

            CInventoryItemEntryData copiedEntryData = sourceEntryData.CreateCopy();
            long itemQuantity = copiedEntryData.GetQuantity();
            string priceItemId = itemDefinition.GetSellPriceItemId();
            long totalPrice = itemDefinition.GetSellPrice() * itemQuantity;
            bool didRemoveItem = targetInventoryManager.TryRemoveItemAtSlot( _inventorySlotIndex, itemQuantity );

            if ( didRemoveItem == false )
            {
                CToastMessageSystem.Show( "판매할 아이템을 찾지 못했습니다." );
                return;
            }

            bool didAddPriceItem = totalPrice <= 0 || targetInventoryManager.TryAddItemById( priceItemId, totalPrice );

            if ( didAddPriceItem == false )
            {
                targetInventoryManager.TryReplaceSlotItem( _inventorySlotIndex, copiedEntryData );
                CToastMessageSystem.Show( "판매 대금을 보관할 공간이 부족합니다." );
                return;
            }

            CInventoryUiDragState.EndDrag();
            CToastMessageSystem.Show( $"{itemDefinition.GetItemName()} 판매 완료" );
        }

        ///<summary>
        /// 상점 툴팁 추가 문구 구성
        ///</summary>
        private string BuildShopTooltipText( CShopEntryData _entryData, CItemDefinition _itemDefinition )
        {
            if ( _entryData == null )
            {
                return string.Empty;
            }

            string buyPriceText = BuildPriceText( _entryData.GetPriceItemId(), _entryData.GetPriceAmount() );
            string sellPriceText = string.Empty;

            if ( _itemDefinition != null && _itemDefinition.HasSellPrice() )
            {
                sellPriceText = BuildPriceText( _itemDefinition.GetSellPriceItemId(), _itemDefinition.GetSellPrice() );
            }

            if ( string.IsNullOrWhiteSpace( sellPriceText ) )
            {
                return $"구매 가격\n{buyPriceText}";
            }

            string result = $"구매 가격\n{buyPriceText}\n\n판매 가격\n{sellPriceText}";
            return result;
        }

        ///<summary>
        /// 가격 표시 문구 구성
        ///</summary>
        private string BuildPriceText( string _priceItemId, long _priceAmount )
        {
            string resolvedPriceItemId = string.IsNullOrWhiteSpace( _priceItemId ) ? DefaultPriceItemId : _priceItemId.Trim();
            string priceItemName = ResolveItemDisplayName( resolvedPriceItemId );
            string result = $"{priceItemName} x{System.Math.Max( 0L, _priceAmount )}";
            return result;
        }

        ///<summary>
        /// 구매 확인 안내 문구 구성
        ///</summary>
        private string BuildPurchaseNoticeText( CShopEntryData _entryData, CItemDefinition _itemDefinition )
        {
            if ( _entryData == null || _itemDefinition == null )
            {
                return string.Empty;
            }

            string itemName = _itemDefinition.GetItemName();
            string priceItemName = ResolveItemDisplayName( _entryData.GetPriceItemId() );
            long itemCount = _entryData.GetItemCount();
            long priceAmount = _entryData.GetPriceAmount();
            string result = $"[ {itemName} ] x{itemCount} 을(를)\n[ {priceItemName} ] x{priceAmount} 으로\n구매하시겠습니까?";
            return result;
        }

        ///<summary>
        /// 아이템 표시 이름 결정
        ///</summary>
        private string ResolveItemDisplayName( string _itemId )
        {
            bool hasItemDefinition = CItemDefinitionDatabase.TryGetItemDefinition( _itemId, out CItemDefinition itemDefinition );

            if ( hasItemDefinition && itemDefinition != null && string.IsNullOrWhiteSpace( itemDefinition.GetItemName() ) == false )
            {
                return itemDefinition.GetItemName();
            }

            string result = string.IsNullOrWhiteSpace( _itemId ) ? DefaultPriceItemId : _itemId.Trim();
            return result;
        }

        ///<summary>
        /// 인벤토리 UI 바인딩 처리
        ///</summary>
        private void BindInventoryUi( PopupItemInventory _linkedInventoryUi )
        {
            UnbindInventoryUi();
            linkedInventoryUi = _linkedInventoryUi;

            if ( linkedInventoryUi == null )
            {
                return;
            }

            linkedInventoryUi.OnInventoryVisibilityChanged -= HandleInventoryVisibilityChanged;
            linkedInventoryUi.OnInventoryVisibilityChanged += HandleInventoryVisibilityChanged;
        }

        ///<summary>
        /// 인벤토리 UI 바인딩 해제 처리
        ///</summary>
        private void UnbindInventoryUi()
        {
            if ( linkedInventoryUi != null )
            {
                linkedInventoryUi.OnInventoryVisibilityChanged -= HandleInventoryVisibilityChanged;
                linkedInventoryUi.SetEquipmentStatusPanelVisible( true );
            }

            linkedInventoryUi = null;
        }

        ///<summary>
        /// 인벤토리 표시 상태 변경 처리
        ///</summary>
        private void HandleInventoryVisibilityChanged( bool _isVisible )
        {
            if ( _isVisible || isClosingLinkedUi || isShopVisible == false )
            {
                return;
            }

            CloseShop();
        }

        ///<summary>
        /// 닫기 버튼 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            CloseShop();
        }

        ///<summary>
        /// 창 드래그 핸들 보장
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            EnsurePopupWindowDragHandle( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 포커스 핸들 보장
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;
            EnsurePopupWindowFocusHandlers( windowRootRectTransform, siblingTargetRectTransform );
        }

        ///<summary>
        /// 판매 드롭 타겟 보장
        ///</summary>
        private void EnsureSellDropTarget()
        {
            if ( windowRootRectTransform == null )
            {
                return;
            }

            CShopSellDropTarget sellDropTarget = windowRootRectTransform.GetComponent<CShopSellDropTarget>();

            if ( sellDropTarget == null )
            {
                sellDropTarget = windowRootRectTransform.gameObject.AddComponent<CShopSellDropTarget>();
            }

            sellDropTarget.Configure( this );
        }
    }
}
