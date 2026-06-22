using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Skill;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private const string CloseButtonPath = "Inventory/ButtonClose";
        private const string LegacyCloseButtonPath = "ButtonClose";
        private const string EquipmentPanelObjectName = "EquipmentStatusPanel";
        private const string SlotPrefabResourcePath = "Prefabs/UI/Inventory/ItemSlot";
        private const string TooltipPrefabResourcePath = "Prefabs/UI/Inventory/ItemTooltipUI";
        private const float DragGhostAlpha = 0.55f;

        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private RectTransform contentRootRectTransform;
        [SerializeField] private RectTransform equipmentStatusPanelRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private CanvasGroup targetCanvasGroup;

        private readonly List<CItemSlot> itemSlotList = new List<CItemSlot>();

        private CPlayerInventoryManager targetInventoryManager;
        private CPlayerEquipmentManager targetEquipmentManager;
        private CSkillManager targetSkillManager;
        private CPlayerStatManager targetStatManager;
        private PlayerController targetPlayerController;
        private CPlayerEquipmentStatusPanelUI equipmentStatusPanelUi;
        private CItemSlot slotPrefab;
        private GameObject tooltipUiPrefabObject;
        private CItemTooltipUI runtimeTooltipUi;
        private RectTransform dragGhostRectTransform;
        private Image dragGhostImage;
        private CItemSlot draggedSlot;
        private bool isInventoryVisible;

        ///<summary>
        /// 인벤토리 UI 초기화 처리
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsurePrefabReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSlotObjects();
            SetInventoryVisible( false );
        }

        ///<summary>
        /// 인벤토리 UI 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            RefreshEquipmentStatusPanelBinding();
            BringWindowToFront();

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
            TryResolveEquipmentManager();
            TryResolveSkillManager();
            TryResolveTargetStatManager();
            TryResolvePlayerController();
            RefreshEquipmentStatusPanelBinding();
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
                RefreshEquipmentStatusPanelBinding();
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
            RefreshEquipmentStatusPanelBinding();
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
        /// 문자열 툴팁 표시 요청
        ///</summary>
        public void ShowTextTooltip( string _titleText, string _descriptionText )
        {
            EnsureTooltipUi();

            if ( runtimeTooltipUi == null )
            {
                return;
            }

            runtimeTooltipUi.SetTooltipContent( _titleText, _descriptionText );
            runtimeTooltipUi.transform.SetAsLastSibling();
            runtimeTooltipUi.SetVisible( true );
            UpdateTooltipPosition();
        }

        ///<summary>
        /// 아이템 정의 툴팁 표시 요청
        ///</summary>
        public void ShowItemDefinitionTooltip( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                HideTooltipInternal();
                return;
            }

            EnsureTooltipUi();

            if ( runtimeTooltipUi == null )
            {
                return;
            }

            runtimeTooltipUi.SetTooltipContent( _itemDefinition );
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
        /// 공용 툴팁 숨김 요청
        ///</summary>
        public void HideSharedTooltip()
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

            if ( dragGhostRectTransform != null )
            {
                dragGhostRectTransform.SetAsLastSibling();
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
        /// 슬롯 우클릭 처리
        ///</summary>
        public void HandleSlotPointerClick( CItemSlot _itemSlot, PointerEventData _eventData )
        {
            if ( _itemSlot == null || _eventData == null )
            {
                return;
            }

            if ( _eventData.button != PointerEventData.InputButton.Right )
            {
                return;
            }

            TryResolveEquipmentManager();
            TryResolveSkillManager();

            if ( targetInventoryManager == null )
            {
                return;
            }

            int slotIndex = _itemSlot.GetSlotIndex();
            bool didEquipItem = targetEquipmentManager != null && targetEquipmentManager.TryEquipFromInventorySlot( targetInventoryManager, slotIndex );

            if ( didEquipItem )
            {
                HideTooltipInternal();
                EndSlotDragInternal();
                RefreshEquipmentStatusPanelBinding();
                return;
            }

            bool didUseItem = TryUseConsumableItemFromSlot( slotIndex );

            if ( didUseItem == false )
            {
                return;
            }

            HideTooltipInternal();
            EndSlotDragInternal();
            RefreshEquipmentStatusPanelBinding();
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
        /// 장비 슬롯 드롭 착용 처리
        ///</summary>
        public bool TryEquipDraggedSlotToEquipmentType( eEquipmentType _equipmentType )
        {
            if ( draggedSlot == null )
            {
                return false;
            }

            TryResolveEquipmentManager();

            if ( targetInventoryManager == null || targetEquipmentManager == null )
            {
                return false;
            }

            int slotIndex = draggedSlot.GetSlotIndex();
            bool didEquipItem = targetEquipmentManager.TryEquipFromInventorySlot( targetInventoryManager, slotIndex, _equipmentType );

            if ( didEquipItem == false )
            {
                return false;
            }

            HideTooltipInternal();
            EndSlotDragInternal();
            RefreshEquipmentStatusPanelBinding();
            return true;
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

                RefreshEquipmentStatusPanelBinding();
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

            RefreshEquipmentStatusPanelBinding();
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
                BringWindowToFront();
                RefreshSlotViews();
                RefreshEquipmentStatusPanelBinding();
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
        /// 장비 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveEquipmentManager()
        {
            if ( targetEquipmentManager != null )
            {
                return;
            }

            targetEquipmentManager = FindFirstObjectByType<CPlayerEquipmentManager>();
        }

        ///<summary>
        /// 스킬 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveSkillManager()
        {
            if ( targetSkillManager != null )
            {
                return;
            }

            if ( targetPlayerController != null )
            {
                CSkillManager resolvedFromPlayer = targetPlayerController.GetComponent<CSkillManager>();

                if ( resolvedFromPlayer != null )
                {
                    targetSkillManager = resolvedFromPlayer;
                    return;
                }
            }

            targetSkillManager = FindFirstObjectByType<CSkillManager>();
        }

        ///<summary>
        /// 플레이어 스탯 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveTargetStatManager()
        {
            if ( targetStatManager != null )
            {
                return;
            }

            if ( targetEquipmentManager != null )
            {
                CPlayerStatManager resolvedFromEquipment = targetEquipmentManager.GetComponent<CPlayerStatManager>();

                if ( resolvedFromEquipment != null )
                {
                    targetStatManager = resolvedFromEquipment;
                    return;
                }
            }

            targetStatManager = FindFirstObjectByType<CPlayerStatManager>();
        }

        ///<summary>
        /// 소모품 사용 처리
        ///</summary>
        private bool TryUseConsumableItemFromSlot( int _slotIndex )
        {
            if ( targetInventoryManager == null )
            {
                return false;
            }

            CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot( _slotIndex );

            if ( itemDefinition == null || itemDefinition.GetItemType() != eItemType.CONSUMABLE )
            {
                return false;
            }

            if ( itemDefinition.IsSkillBook() == false )
            {
                return false;
            }

            if ( targetSkillManager == null )
            {
                return false;
            }

            string skillId = itemDefinition.GetLinkedSkillId();
            bool didLearnSkill = targetSkillManager.TryForceLearnSkill( skillId );

            if ( didLearnSkill == false )
            {
                return false;
            }

            bool didRemoveItem = targetInventoryManager.TryRemoveItem( itemDefinition.GetItemId(), 1 );
            return didRemoveItem;
        }

        ///<summary>
        /// 플레이어 컨트롤러 자동 결정 시도
        ///</summary>
        private void TryResolvePlayerController()
        {
            if ( targetPlayerController != null )
            {
                return;
            }

            targetPlayerController = FindFirstObjectByType<PlayerController>();
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
        /// 통합 장비 패널 바인딩 반영
        ///</summary>
        private void RefreshEquipmentStatusPanelBinding()
        {
            if ( equipmentStatusPanelUi == null )
            {
                return;
            }

            equipmentStatusPanelUi.Bind( targetInventoryManager, targetEquipmentManager, targetStatManager, targetPlayerController );
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
            rectTransform.SetAsLastSibling();
            dragGhostRectTransform = rectTransform;
            dragGhostImage = image;
        }

        ///<summary>
        /// 슬롯 프리팹과 툴팁 프리팹 결정
        ///</summary>
        private void EnsurePrefabReferences()
        {
            if ( slotPrefab == null )
            {
                GameObject slotPrefabObject = Resources.Load<GameObject>( SlotPrefabResourcePath );
                slotPrefab = slotPrefabObject != null ? slotPrefabObject.GetComponent<CItemSlot>() : null;
            }

            if ( tooltipUiPrefabObject == null )
            {
                tooltipUiPrefabObject = Resources.Load<GameObject>( TooltipPrefabResourcePath );
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
            if ( windowDragHandleRectTransform == null )
            {
                return;
            }

            CItemInventoryWindowDragHandle dragHandle = windowDragHandleRectTransform.GetComponent<CItemInventoryWindowDragHandle>();

            if ( dragHandle == null )
            {
                dragHandle = windowDragHandleRectTransform.gameObject.AddComponent<CItemInventoryWindowDragHandle>();
            }

            dragHandle.Configure( windowRootRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 클릭 최상단 정렬 핸들러 구성
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;

            if ( windowRootRectTransform == null || siblingTargetRectTransform == null )
            {
                return;
            }

            Graphic[] graphicArray = windowRootRectTransform.GetComponentsInChildren<Graphic>( true );

            for ( int index = 0; index < graphicArray.Length; index++ )
            {
                Graphic graphic = graphicArray[ index ];

                if ( graphic == null || graphic.raycastTarget == false )
                {
                    continue;
                }

                CWindowDragHandle focusHandler = graphic.GetComponent<CWindowDragHandle>();

                if ( focusHandler == null )
                {
                    focusHandler = graphic.gameObject.AddComponent<CWindowDragHandle>();
                }

                focusHandler.Configure( siblingTargetRectTransform );
            }
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
        /// 인벤토리 창 최상단 정렬
        ///</summary>
        private void BringWindowToFront()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;

            if ( siblingTargetRectTransform == null )
            {
                return;
            }

            siblingTargetRectTransform.SetAsLastSibling();
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

            if ( equipmentStatusPanelRectTransform == null && windowRootRectTransform != null )
            {
                Transform equipmentPanelTransform = windowRootRectTransform.Find( EquipmentPanelObjectName );
                equipmentStatusPanelRectTransform = equipmentPanelTransform as RectTransform;
            }

            if ( equipmentStatusPanelUi == null && equipmentStatusPanelRectTransform != null )
            {
                equipmentStatusPanelUi = equipmentStatusPanelRectTransform.GetComponent<CPlayerEquipmentStatusPanelUI>();
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        ///<summary>
        /// 창 최상위 RectTransform 결정
        ///</summary>
        private RectTransform ResolveTopLevelWindowRectTransform()
        {
            if ( windowRootRectTransform == null )
            {
                return null;
            }

            RectTransform canvasRectTransform = targetCanvas != null ? targetCanvas.transform as RectTransform : null;
            RectTransform currentRectTransform = windowRootRectTransform;

            while ( currentRectTransform != null )
            {
                RectTransform parentRectTransform = currentRectTransform.parent as RectTransform;

                if ( parentRectTransform == null )
                {
                    break;
                }

                if ( parentRectTransform == canvasRectTransform )
                {
                    break;
                }

                Transform grandParentTransform = parentRectTransform.parent;

                if ( grandParentTransform == canvasRectTransform )
                {
                    break;
                }

                currentRectTransform = parentRectTransform;
            }

            RectTransform result = currentRectTransform;
            return result;
        }
    }
}
