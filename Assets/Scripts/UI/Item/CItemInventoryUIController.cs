using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 UI 제어 컴포넌트
    ///</summary>
    public sealed class CItemInventoryUIController : MonoBehaviour
    {
        private const string InventoryRootPath = "Inventory";
        private const string ContentPathPrimary = "Inventory/BG/Content";
        private const string ContentPathFallback = "Inventory/BG/Scroll View/Viewport/Content";
        private const string LegacyContentPathPrimary = "BG/Content";
        private const string LegacyContentPathFallback = "BG/Scroll View/Viewport/Content";
        private const string BgPath = "Inventory/BG";
        private const string LegacyBgPath = "BG";
        private const string CloseButtonPath = "Inventory/ButtonClose";
        private const string LegacyCloseButtonPath = "ButtonClose";
        private const string SlotPrefabResourcePath = "Prefabs/UI/Inventory/ItemSlot";
        private const string TooltipPrefabResourcePath = "Prefabs/UI/Inventory/ItemTooltipUI";
        private const float DragGhostAlpha = 0.55f;

        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform contentRootRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private CanvasGroup targetCanvasGroup;

        private readonly List<CItemSlot> itemSlotList = new List<CItemSlot>();

        private CPlayerInventoryManager targetInventoryManager;
        private CItemSlot slotPrefab;
        private GameObject tooltipUiPrefabObject;
        private CItemTooltipUI tooltipUiPrefab;
        private CItemTooltipUI runtimeTooltipUi;
        private RectTransform dragGhostRectTransform;
        private Image dragGhostImage;
        private CItemSlot draggedSlot;
        private bool isInventoryVisible;

        ///<summary>
        /// 인벤토리 UI 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsurePrefabReferences();
            EnsureTooltipUi();
            EnsureWindowDragHandle();
            EnsureSlotObjects();
            SetInventoryVisible( false );
        }

        ///<summary>
        /// 인벤토리 UI 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 인벤토리 UI 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }

            UnbindInventoryManager();
            HideTooltipInternal();
            EndSlotDragInternal();
        }

        ///<summary>
        /// 인벤토리 입력 처리
        ///</summary>
        private void Update()
        {
            TryResolveInventoryManager();
            HandleToggleInput();

            if ( isInventoryVisible == false )
            {
                return;
            }

            if ( Input.GetKeyDown( KeyCode.Escape ) )
            {
                SetInventoryVisible( false );
                return;
            }

            UpdateTooltipPosition();
            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 인벤토리 매니저 바인딩
        ///</summary>
        public void BindInventoryManager( CPlayerInventoryManager _targetInventoryManager )
        {
            if ( targetInventoryManager == _targetInventoryManager )
            {
                RefreshSlotViews();
                return;
            }

            UnbindInventoryManager();
            targetInventoryManager = _targetInventoryManager;

            if ( targetInventoryManager != null )
            {
                targetInventoryManager.OnInventoryChanged -= HandleInventoryChanged;
                targetInventoryManager.OnInventoryChanged += HandleInventoryChanged;
            }

            RefreshSlotViews();
        }

        ///<summary>
        /// 인벤토리 툴팁 표시 요청
        ///</summary>
        public void ShowTooltip( CItemSlot _itemSlot )
        {
            if ( _itemSlot == null || _itemSlot.HasItem() == false || draggedSlot != null )
            {
                HideTooltipInternal();
                return;
            }

            EnsureTooltipUi();

            if ( runtimeTooltipUi == null )
            {
                return;
            }

            runtimeTooltipUi.SetTooltipContent( _itemSlot.GetCurrentItemDefinition() );
            runtimeTooltipUi.transform.SetAsLastSibling();
            runtimeTooltipUi.SetVisible( true );
            UpdateTooltipPosition();
        }

        ///<summary>
        /// 인벤토리 툴팁 숨김 요청
        ///</summary>
        public void HideTooltip( CItemSlot _itemSlot )
        {
            HideTooltipInternal();
        }

        ///<summary>
        /// 슬롯 드래그 시작 시도
        ///</summary>
        public void TryBeginSlotDrag( CItemSlot _itemSlot, PointerEventData _eventData )
        {
            if ( isInventoryVisible == false || _itemSlot == null || _itemSlot.HasItem() == false )
            {
                return;
            }

            EnsureDragGhost();
            draggedSlot = _itemSlot;
            HideTooltipInternal();

            if ( dragGhostImage != null )
            {
                dragGhostImage.sprite = _itemSlot.GetCurrentItemDefinition().GetIconSprite();
                Color ghostColor = dragGhostImage.color;
                ghostColor.a = DragGhostAlpha;
                dragGhostImage.color = ghostColor;
                dragGhostImage.enabled = dragGhostImage.sprite != null;
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 슬롯 드래그 진행 처리
        ///</summary>
        public void UpdateSlotDrag( PointerEventData _eventData )
        {
            if ( draggedSlot == null )
            {
                return;
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 슬롯 드래그 종료 처리
        ///</summary>
        public void EndSlotDrag( PointerEventData _eventData )
        {
            EndSlotDragInternal();
        }

        ///<summary>
        /// 슬롯 드롭 처리
        ///</summary>
        public void HandleSlotDrop( CItemSlot _targetSlot )
        {
            if ( draggedSlot == null || _targetSlot == null || targetInventoryManager == null )
            {
                return;
            }

            int fromSlotIndex = draggedSlot.GetSlotIndex();
            int toSlotIndex = _targetSlot.GetSlotIndex();
            targetInventoryManager.TrySwapSlotItems( fromSlotIndex, toSlotIndex );
        }

        ///<summary>
        /// 인벤토리 슬롯 UI 갱신
        ///</summary>
        public void RefreshSlotViews()
        {
            EnsureSlotObjects();

            if ( targetInventoryManager == null )
            {
                for ( int index = 0; index < itemSlotList.Count; index++ )
                {
                    CItemSlot itemSlot = itemSlotList[ index ];

                    if ( itemSlot == null )
                    {
                        continue;
                    }

                    itemSlot.RefreshSlot( null, 0 );
                }

                return;
            }

            int slotCount = targetInventoryManager.GetSlotCount();

            for ( int index = 0; index < itemSlotList.Count; index++ )
            {
                CItemSlot itemSlot = itemSlotList[ index ];

                if ( itemSlot == null )
                {
                    continue;
                }

                if ( index >= slotCount )
                {
                    itemSlot.RefreshSlot( null, 0 );
                    continue;
                }

                CInventoryItemEntryData itemEntryData = targetInventoryManager.GetItemEntryData( index );
                CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot( index );
                int quantity = itemEntryData != null ? itemEntryData.GetQuantity() : 0;
                itemSlot.RefreshSlot( itemDefinition, quantity );
            }
        }

        ///<summary>
        /// 인벤토리 표시 상태 설정
        ///</summary>
        public void SetInventoryVisible( bool _isVisible )
        {
            isInventoryVisible = _isVisible;
            SetInventoryRootActiveState( _isVisible );

            if ( _isVisible == false )
            {
                HideTooltipInternal();
                EndSlotDragInternal();
            }
            else
            {
                RefreshSlotViews();
            }
        }

        ///<summary>
        /// 인벤토리 표시 상태 반환
        ///</summary>
        public bool IsInventoryVisible()
        {
            bool result = isInventoryVisible;
            return result;
        }

        ///<summary>
        /// 인벤토리 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveInventoryManager()
        {
            if ( targetInventoryManager != null )
            {
                return;
            }

            CPlayerInventoryManager resolvedInventoryManager = FindFirstObjectByType<CPlayerInventoryManager>();

            if ( resolvedInventoryManager == null )
            {
                return;
            }

            BindInventoryManager( resolvedInventoryManager );
        }

        ///<summary>
        /// 인벤토리 매니저 바인딩 해제
        ///</summary>
        private void UnbindInventoryManager()
        {
            if ( targetInventoryManager == null )
            {
                return;
            }

            targetInventoryManager.OnInventoryChanged -= HandleInventoryChanged;
            targetInventoryManager = null;
        }

        ///<summary>
        /// 인벤토리 변경 이벤트 반영
        ///</summary>
        private void HandleInventoryChanged( CPlayerInventoryManager _inventoryManager )
        {
            RefreshSlotViews();
        }

        ///<summary>
        /// 인벤토리 토글 입력 처리
        ///</summary>
        private void HandleToggleInput()
        {
            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager == null )
            {
                return;
            }

            bool isInventoryDown = inputManager.GetInventoryDown();

            if ( isInventoryDown == false )
            {
                return;
            }

            bool nextVisibleState = isInventoryVisible == false;
            SetInventoryVisible( nextVisibleState );
        }

        ///<summary>
        /// 닫기 버튼 클릭 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            SetInventoryVisible( false );
        }

        ///<summary>
        /// 툴팁 위치 갱신
        ///</summary>
        private void UpdateTooltipPosition()
        {
            if ( runtimeTooltipUi == null || runtimeTooltipUi.gameObject.activeSelf == false || targetCanvas == null )
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            runtimeTooltipUi.SetScreenPosition( mousePosition, targetCanvas );
        }

        ///<summary>
        /// 드래그 고스트 위치 갱신
        ///</summary>
        private void UpdateDragGhostPosition()
        {
            if ( dragGhostRectTransform == null || draggedSlot == null || targetCanvas == null )
            {
                return;
            }

            RectTransform canvasRectTransform = targetCanvas.transform as RectTransform;

            if ( canvasRectTransform == null )
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            Vector2 localPoint;
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRectTransform, mousePosition, null, out localPoint );

            if ( isConverted == false )
            {
                return;
            }

            dragGhostRectTransform.anchoredPosition = localPoint;
        }

        ///<summary>
        /// 툴팁 숨김 처리
        ///</summary>
        private void HideTooltipInternal()
        {
            if ( runtimeTooltipUi == null )
            {
                return;
            }

            runtimeTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 슬롯 드래그 종료 내부 처리
        ///</summary>
        private void EndSlotDragInternal()
        {
            draggedSlot = null;

            if ( dragGhostImage != null )
            {
                dragGhostImage.enabled = false;
            }
        }

        ///<summary>
        /// 드래그 고스트 생성 보장
        ///</summary>
        private void EnsureDragGhost()
        {
            if ( dragGhostRectTransform != null && dragGhostImage != null )
            {
                return;
            }

            if ( targetCanvas == null )
            {
                return;
            }

            GameObject dragGhostObject = new GameObject( "ItemDragGhost", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform rectTransform = dragGhostObject.GetComponent<RectTransform>();
            rectTransform.SetParent( targetCanvas.transform, false );
            rectTransform.sizeDelta = new Vector2( 72.0f, 72.0f );
            Image image = dragGhostObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            dragGhostRectTransform = rectTransform;
            dragGhostImage = image;
        }

        ///<summary>
        /// 슬롯 프리팹 및 툴팁 프리팹 결정
        ///</summary>
        private void EnsurePrefabReferences()
        {
            if ( slotPrefab == null )
            {
                GameObject slotPrefabObject = Resources.Load<GameObject>( SlotPrefabResourcePath );
                slotPrefab = slotPrefabObject != null ? slotPrefabObject.GetComponent<CItemSlot>() : null;
            }

            if ( tooltipUiPrefab == null )
            {
                GameObject tooltipPrefabObject = Resources.Load<GameObject>( TooltipPrefabResourcePath );
                tooltipUiPrefabObject = tooltipPrefabObject;
                tooltipUiPrefab = tooltipPrefabObject != null ? tooltipPrefabObject.GetComponent<CItemTooltipUI>() : null;
            }
        }

        ///<summary>
        /// 툴팁 UI 생성 보장
        ///</summary>
        private void EnsureTooltipUi()
        {
            EnsurePrefabReferences();

            if ( runtimeTooltipUi != null || tooltipUiPrefabObject == null || targetCanvas == null )
            {
                return;
            }

            GameObject createdTooltipObject = Instantiate( tooltipUiPrefabObject, targetCanvas.transform );
            createdTooltipObject.name = tooltipUiPrefabObject.name;
            CItemTooltipUI createdTooltipUi = createdTooltipObject.GetComponent<CItemTooltipUI>();

            if ( createdTooltipUi == null )
            {
                createdTooltipUi = createdTooltipObject.AddComponent<CItemTooltipUI>();
            }

            createdTooltipUi.SetVisible( false );
            runtimeTooltipUi = createdTooltipUi;
        }

        ///<summary>
        /// 창 드래그 핸들 생성 보장
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            Transform bgTransform = transform.Find( BgPath );

            if ( bgTransform == null )
            {
                bgTransform = transform.Find( LegacyBgPath );
            }

            if ( bgTransform == null )
            {
                return;
            }

            CItemInventoryWindowDragHandle dragHandle = bgTransform.GetComponent<CItemInventoryWindowDragHandle>();

            if ( dragHandle == null )
            {
                dragHandle = bgTransform.gameObject.AddComponent<CItemInventoryWindowDragHandle>();
            }

            dragHandle.Configure( windowRootRectTransform, targetCanvas );
        }

        ///<summary>
        /// 슬롯 오브젝트 구성 보장
        ///</summary>
        private void EnsureSlotObjects()
        {
            EnsurePrefabReferences();

            if ( contentRootRectTransform == null || slotPrefab == null )
            {
                return;
            }

            itemSlotList.Clear();
            int childCount = contentRootRectTransform.childCount;

            for ( int index = childCount - 1; index >= 0; index-- )
            {
                Transform childTransform = contentRootRectTransform.GetChild( index );
                CItemSlot itemSlot = childTransform.GetComponent<CItemSlot>();

                if ( itemSlot == null )
                {
                    continue;
                }

                itemSlotList.Insert( 0, itemSlot );
            }

            int requiredSlotCount = targetInventoryManager != null ? targetInventoryManager.GetSlotCount() : 35;

            while ( itemSlotList.Count < requiredSlotCount )
            {
                CItemSlot createdItemSlot = Instantiate( slotPrefab, contentRootRectTransform );
                createdItemSlot.name = $"ItemSlot_{itemSlotList.Count + 1:D2}";
                itemSlotList.Add( createdItemSlot );
            }

            while ( itemSlotList.Count > requiredSlotCount )
            {
                CItemSlot removedItemSlot = itemSlotList[ itemSlotList.Count - 1 ];
                itemSlotList.RemoveAt( itemSlotList.Count - 1 );

                if ( removedItemSlot != null )
                {
                    Destroy( removedItemSlot.gameObject );
                }
            }

            for ( int index = 0; index < itemSlotList.Count; index++ )
            {
                CItemSlot itemSlot = itemSlotList[ index ];

                if ( itemSlot == null )
                {
                    continue;
                }

                itemSlot.Initialize( this, index );
            }
        }

        ///<summary>
        /// 인벤토리 루트 활성 상태 반영
        ///</summary>
        private void SetInventoryRootActiveState( bool _isVisible )
        {
            if ( windowRootRectTransform == null )
            {
                return;
            }

            GameObject windowRootObject = windowRootRectTransform.gameObject;

            if ( windowRootObject.activeSelf == _isVisible )
            {
                return;
            }

            windowRootObject.SetActive( _isVisible );
        }

        ///<summary>
        /// UI 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( windowRootRectTransform == null )
            {
                Transform inventoryRootTransform = transform.Find( InventoryRootPath );
                windowRootRectTransform = inventoryRootTransform as RectTransform;

                if ( windowRootRectTransform == null )
                {
                    windowRootRectTransform = transform as RectTransform;
                }
            }

            if ( contentRootRectTransform == null )
            {
                Transform contentTransform = transform.Find( ContentPathPrimary );

                if ( contentTransform == null )
                {
                    contentTransform = transform.Find( ContentPathFallback );
                }

                if ( contentTransform == null )
                {
                    contentTransform = transform.Find( LegacyContentPathPrimary );
                }

                if ( contentTransform == null )
                {
                    contentTransform = transform.Find( LegacyContentPathFallback );
                }

                contentRootRectTransform = contentTransform as RectTransform;
            }

            if ( closeButton == null )
            {
                Transform closeButtonTransform = transform.Find( CloseButtonPath );

                if ( closeButtonTransform == null )
                {
                    closeButtonTransform = transform.Find( LegacyCloseButtonPath );
                }

                closeButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<CButtonEx>() : null;
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }
    }
}
