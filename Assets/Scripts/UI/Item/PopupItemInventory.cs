using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 UI 제어 컴포넌트
    ///</summary>
    public sealed class PopupItemInventory : CUIPopup
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
        private const float DragGhostAlpha = 0.55f;
        private const float InventoryWindowRightOffset = 300.0f;
        private const string EquipmentTabDisplayName = "\uC7A5\uBE44";
        private const string ConsumableTabDisplayName = "\uC18C\uBE44";
        private const string CurrencyTabDisplayName = "\uC7AC\uD654";
        private const string MaterialTabDisplayName = "\uAE30\uD0C0";
        private const string QuestTabDisplayName = "\uD018\uC2A4\uD2B8";
        private static readonly Color SelectedTabBackgroundColor = new Color32(0x8D, 0xC3, 0xFF, 0xFF);
        private static readonly Color SelectedTabInnerColor = new Color32(0x86, 0xCD, 0xFF, 0xFF);
        private static readonly Color UnselectedTabBackgroundColor = new Color32(0xA6, 0xA6, 0xA6, 0xFF);
        private static readonly Color UnselectedTabInnerColor = new Color32(0xB2, 0xB2, 0xB2, 0xFF);

        [System.Serializable]
        private sealed class CInventoryTabReference
        {
            [SerializeField] private string displayName;
            [SerializeField] private eItemType itemType;
            [SerializeField] private RectTransform rootRectTransform;
            [SerializeField] private CButtonEx button;
            [SerializeField] private Image backgroundImage;
            [SerializeField] private Image innerImage;
            [SerializeField] private TMP_Text nameText;

            public string DisplayName => displayName;
            public eItemType ItemType => itemType;
            public RectTransform RootRectTransform => rootRectTransform;
            public CButtonEx Button => button;
            public Image BackgroundImage => backgroundImage;
            public Image InnerImage => innerImage;
            public TMP_Text NameText => nameText;
        }

        private sealed class CInventoryTabContext
        {
            public CInventoryTabReference tabReference;
            public UnityAction clickAction;
        }

        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private RectTransform contentRootRectTransform;
        [SerializeField] private RectTransform equipmentStatusPanelRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private CanvasGroup targetCanvasGroup;

        [Header("인벤토리 탭")]
        [SerializeField] private CInventoryTabReference equipmentTabReference;
        [SerializeField] private CInventoryTabReference consumableTabReference;
        [SerializeField] private CInventoryTabReference currencyTabReference;
        [SerializeField] private CInventoryTabReference materialTabReference;
        [SerializeField] private CInventoryTabReference questTabReference;

        private readonly List<CItemSlot> itemSlotList = new List<CItemSlot>();
        private readonly List<CInventoryTabContext> inventoryTabContextList = new List<CInventoryTabContext>();

        private CPlayerInventoryManager targetInventoryManager;
        private CPlayerEquipmentManager targetEquipmentManager;
        private CSkillManager targetSkillManager;
        private CPlayerStatManager targetStatManager;
        private PlayerController targetPlayerController;
        private CPlayerEquipmentStatusPanelUI equipmentStatusPanelUi;
        private CItemSlot slotPrefab;
        private RectTransform dragGhostRectTransform;
        private Image dragGhostImage;
        private CItemSlot draggedSlot;
        private eItemType selectedItemType = eItemType.EQUIPMENT;
        private bool isInventoryVisible;

        public event System.Action<bool> OnInventoryVisibilityChanged;

        ///<summary>
        /// 인벤토리 UI 초기화 처리
        ///</summary>
        private void Awake()
        {
            RefreshCachedReferences();
            EnsurePrefabReferences();
            EnsureTabObjects();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSlotObjects();
            SetInventoryVisible(false);
        }

        ///<summary>
        /// 인벤토리 UI 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshCachedReferences();
            EnsureTabObjects();
            BindTabButtonEvents();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            RefreshEquipmentStatusPanelBinding();
            BringLayerToFront();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
                closeButton.onClick.AddListener(HandleCloseButtonClicked);
            }
        }

        ///<summary>
        /// 인벤토리 UI 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            }

            UnbindTabButtonEvents();
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

            if (isInventoryVisible == false)
            {
                return;
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 인벤토리 매니저 바인딩
        ///</summary>
        public void BindInventoryManager(CPlayerInventoryManager _targetInventoryManager)
        {
            if (targetInventoryManager == _targetInventoryManager)
            {
                RefreshSlotViews();
                RefreshEquipmentStatusPanelBinding();
                return;
            }

            UnbindInventoryManager();
            targetInventoryManager = _targetInventoryManager;

            if (targetInventoryManager != null)
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
        public void ShowTooltip(CItemSlot _itemSlot)
        {
            if (_itemSlot == null || _itemSlot.HasItem() == false || draggedSlot != null)
            {
                CUITooltipManager.HideItemTooltip();
                return;
            }

            int localSlotIndex = _itemSlot.GetBoundInventorySlotIndex();
            CInventoryItemEntryData itemEntryData = targetInventoryManager != null ? targetInventoryManager.GetItemEntryData(selectedItemType, localSlotIndex) : null;
            CEquipmentPotentialData equipmentPotentialData = itemEntryData != null ? itemEntryData.GetEquipmentPotentialData() : null;
            CUITooltipManager.ShowItemTooltip( _itemSlot.GetCurrentItemDefinition(), equipmentPotentialData );
        }

        ///<summary>
        /// 문자열 툴팁 표시 요청
        ///</summary>
        public void ShowTextTooltip(string _titleText, string _descriptionText)
        {
            CUITooltipManager.ShowTextTooltip( _titleText, _descriptionText );
        }

        ///<summary>
        /// 아이템 정의 툴팁 표시 요청
        ///</summary>
        public void ShowItemDefinitionTooltip(CItemDefinition _itemDefinition)
        {
            ShowItemDefinitionTooltip(_itemDefinition, null, string.Empty);
        }

        ///<summary>
        /// 아이템 정의와 잠재 툴팁 표시 요청
        ///</summary>
        public void ShowItemDefinitionTooltip(CItemDefinition _itemDefinition, CEquipmentPotentialData _equipmentPotentialData)
        {
            ShowItemDefinitionTooltip(_itemDefinition, _equipmentPotentialData, string.Empty);
        }

        ///<summary>
        /// 아이템 정의와 추가 정보 툴팁 표시 요청
        ///</summary>
        public void ShowItemDefinitionTooltip(CItemDefinition _itemDefinition, CEquipmentPotentialData _equipmentPotentialData, string _additionalInfoText)
        {
            if (_itemDefinition == null)
            {
                CUITooltipManager.HideItemTooltip();
                return;
            }

            CUITooltipManager.ShowItemTooltip( _itemDefinition, _equipmentPotentialData, _additionalInfoText );
        }

        ///<summary>
        /// 인벤토리 툴팁 숨김 요청
        ///</summary>
        public void HideTooltip(CItemSlot _itemSlot)
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
        public void TryBeginSlotDrag(CItemSlot _itemSlot, PointerEventData _eventData)
        {
            if (isInventoryVisible == false || _itemSlot == null || _itemSlot.HasItem() == false)
            {
                return;
            }

            int localSlotIndex = _itemSlot.GetBoundInventorySlotIndex();
            int inventorySlotIndex = targetInventoryManager != null ? targetInventoryManager.GetSlotIndex(selectedItemType, localSlotIndex) : -1;

            if (inventorySlotIndex < 0)
            {
                return;
            }

            EnsureDragGhost();
            draggedSlot = _itemSlot;
            CInventoryUiDragState.BeginDrag(inventorySlotIndex);
            HideTooltipInternal();

            if (dragGhostImage != null)
            {
                dragGhostImage.sprite = _itemSlot.GetCurrentItemDefinition().GetIconSprite();
                Color ghostColor = dragGhostImage.color;
                ghostColor.a = DragGhostAlpha;
                dragGhostImage.color = ghostColor;
                dragGhostImage.enabled = dragGhostImage.sprite != null;
            }

            if (dragGhostRectTransform != null)
            {
                dragGhostRectTransform.SetAsLastSibling();
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 슬롯 드래그 진행 처리
        ///</summary>
        public void UpdateSlotDrag(PointerEventData _eventData)
        {
            if (draggedSlot == null)
            {
                return;
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 슬롯 드래그 종료 처리
        ///</summary>
        public void EndSlotDrag(PointerEventData _eventData)
        {
            EndSlotDragInternal();
        }

        ///<summary>
        /// 슬롯 우클릭 처리
        ///</summary>
        public void HandleSlotPointerClick(CItemSlot _itemSlot, PointerEventData _eventData)
        {
            if (_itemSlot == null || _eventData == null)
            {
                return;
            }

            if (_eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            TryResolveEquipmentManager();
            TryResolveSkillManager();

            if (targetInventoryManager == null)
            {
                return;
            }

            int localSlotIndex = _itemSlot.GetBoundInventorySlotIndex();
            int slotIndex = targetInventoryManager != null ? targetInventoryManager.GetSlotIndex(selectedItemType, localSlotIndex) : -1;

            if (slotIndex < 0)
            {
                return;
            }

            bool didEquipItem = targetEquipmentManager != null && targetEquipmentManager.TryEquipFromInventorySlot(targetInventoryManager, slotIndex);

            if (didEquipItem)
            {
                HideTooltipInternal();
                EndSlotDragInternal();
                RefreshEquipmentStatusPanelBinding();
                return;
            }

            bool didUseItem = TryUseConsumableItemFromSlot(slotIndex);

            if (didUseItem == false)
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
        public void HandleSlotDrop(CItemSlot _targetSlot)
        {
            if (draggedSlot == null || _targetSlot == null || targetInventoryManager == null)
            {
                return;
            }

            int fromLocalSlotIndex = draggedSlot.GetBoundInventorySlotIndex();
            int toLocalSlotIndex = _targetSlot.GetBoundInventorySlotIndex();

            if (fromLocalSlotIndex < 0 || toLocalSlotIndex < 0)
            {
                return;
            }

            targetInventoryManager.TrySwapSlotItems(selectedItemType, fromLocalSlotIndex, selectedItemType, toLocalSlotIndex);
        }

        ///<summary>
        /// 장비 슬롯 드롭 착용 처리
        ///</summary>
        public bool TryEquipDraggedSlotToEquipmentType(eEquipmentType _equipmentType)
        {
            if (draggedSlot == null)
            {
                return false;
            }

            TryResolveEquipmentManager();

            if (targetInventoryManager == null || targetEquipmentManager == null)
            {
                return false;
            }

            int localSlotIndex = draggedSlot.GetBoundInventorySlotIndex();
            int slotIndex = targetInventoryManager != null ? targetInventoryManager.GetSlotIndex(selectedItemType, localSlotIndex) : -1;

            if (slotIndex < 0)
            {
                return false;
            }

            bool didEquipItem = targetEquipmentManager.TryEquipFromInventorySlot(targetInventoryManager, slotIndex, _equipmentType);

            if (didEquipItem == false)
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
            EnsureTabObjects();
            RefreshTabVisualState();

            if (targetInventoryManager == null)
            {
                for (int index = 0; index < itemSlotList.Count; index++)
                {
                    CItemSlot itemSlot = itemSlotList[index];

                    if (itemSlot == null)
                    {
                        continue;
                    }

                    itemSlot.SetBoundInventorySlotIndex(-1);
                    itemSlot.RefreshSlot(null, 0L);
                }

                RefreshEquipmentStatusPanelBinding();
                return;
            }

            for (int index = 0; index < itemSlotList.Count; index++)
            {
                CItemSlot itemSlot = itemSlotList[index];

                if (itemSlot == null)
                {
                    continue;
                }

                CInventoryItemEntryData itemEntryData = targetInventoryManager.GetItemEntryData(selectedItemType, index);
                CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(selectedItemType, index);
                long quantity = itemEntryData != null ? itemEntryData.GetQuantity() : 0L;
                itemSlot.SetBoundInventorySlotIndex(index);
                itemSlot.RefreshSlot(itemDefinition, quantity);
            }

            RefreshEquipmentStatusPanelBinding();
        }

        ///<summary>
        /// 인벤토리 표시 상태 설정
        ///</summary>
        public void SetInventoryVisible(bool _isVisible)
        {
            bool hasVisibilityChanged = isInventoryVisible != _isVisible;
            isInventoryVisible = _isVisible;
            SetInventoryRootActiveState(_isVisible);

            if (_isVisible == false)
            {
                HideTooltipInternal();
                EndSlotDragInternal();
            }
            else
            {
                CUINavigationController navigationController = CUINavigationController.Instance;

                if (navigationController != null)
                {
                    navigationController.RegisterPopup(this);
                }

                BringLayerToFront();
                RefreshTabVisualState();
                RefreshSlotViews();
                RefreshEquipmentStatusPanelBinding();
            }

            if (hasVisibilityChanged && OnInventoryVisibilityChanged != null)
            {
                OnInventoryVisibilityChanged(_isVisible);
            }
        }

        ///<summary>
        /// 인벤토리 창 우측 배치
        ///</summary>
        public void SnapWindowToRightSide()
        {
            SnapWindowToHorizontalSide( true );
        }

        ///<summary>
        /// 네비게이션 레이어 표시 상태 반영
        ///</summary>
        public override void SetLayerVisible(bool _isVisible)
        {
            SetInventoryVisible(_isVisible);
        }

        ///<summary>
        /// 네비게이션 표시 상태 반환
        ///</summary>
        public override bool IsNavigationVisible()
        {
            bool result = isInventoryVisible;
            return result;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            SetInventoryVisible(false);
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
        /// 인벤토리 매니저 반환
        ///</summary>
        public CPlayerInventoryManager GetInventoryManager()
        {
            CPlayerInventoryManager result = targetInventoryManager;
            return result;
        }

        ///<summary>
        /// 장비 상태 패널 표시 상태 설정
        ///</summary>
        public void SetEquipmentStatusPanelVisible(bool _isVisible)
        {
            if (equipmentStatusPanelRectTransform == null)
            {
                return;
            }

            GameObject targetObject = equipmentStatusPanelRectTransform.gameObject;

            if (targetObject.activeSelf == _isVisible)
            {
                return;
            }

            targetObject.SetActive(_isVisible);
        }

        ///<summary>
        /// 인벤토리 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveInventoryManager()
        {
            if (targetInventoryManager != null)
            {
                return;
            }

            CPlayerInventoryManager resolvedInventoryManager = FindFirstObjectByType<CPlayerInventoryManager>();

            if (resolvedInventoryManager == null)
            {
                return;
            }

            BindInventoryManager(resolvedInventoryManager);
        }

        ///<summary>
        /// 장비 매니저 자동 결정 시도
        ///</summary>
        private void TryResolveEquipmentManager()
        {
            if (targetEquipmentManager != null)
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
            if (targetSkillManager != null)
            {
                return;
            }

            if (targetPlayerController != null)
            {
                CSkillManager resolvedFromPlayer = targetPlayerController.GetSkillManager();

                if (resolvedFromPlayer != null)
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
            if (targetStatManager != null)
            {
                return;
            }

            if (targetEquipmentManager != null)
            {
                CPlayerStatManager resolvedFromEquipment = targetPlayerController != null ? targetPlayerController.GetPlayerStatManager() : null;

                if (resolvedFromEquipment != null)
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
        private bool TryUseConsumableItemFromSlot(int _slotIndex)
        {
            if (targetInventoryManager == null)
            {
                return false;
            }

            CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(_slotIndex);

            if (itemDefinition == null || itemDefinition.GetItemType() != eItemType.CONSUMABLE)
            {
                return false;
            }

            if ( itemDefinition.IsSkillPointBook() )
            {
                bool didUseSkillPointBook = TryUseSkillPointBook( itemDefinition );
                return didUseSkillPointBook;
            }

            if (itemDefinition.IsSkillBook() == false)
            {
                if (itemDefinition.IsCube())
                {
                    bool didOpenCubeUi = TryOpenCubeUi(_slotIndex);
                    return didOpenCubeUi;
                }

                if (itemDefinition.IsRandomBox())
                {
                    bool didUseRandomBox = TryUseRandomBoxItem(itemDefinition);
                    return didUseRandomBox;
                }

                return false;
            }

            if (targetSkillManager == null)
            {
                return false;
            }

            string skillId = itemDefinition.GetLinkedSkillId();
            bool didLearnSkill = targetSkillManager.TryForceLearnSkill(skillId);

            if (didLearnSkill == false)
            {
                return false;
            }

            bool didRemoveItem = targetInventoryManager.TryRemoveItem(itemDefinition.GetItemId(), 1);
            return didRemoveItem;
        }

        ///<summary>
        /// 스킬 포인트 북 사용 처리
        ///</summary>
        private bool TryUseSkillPointBook( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null || targetInventoryManager == null || targetSkillManager == null )
            {
                return false;
            }

            int grantAmount = _itemDefinition.GetSkillPointGrantAmount();
            string itemId = _itemDefinition.GetItemId();
            bool didRemoveItem = targetInventoryManager.TryRemoveItem( itemId, 1 );

            if ( didRemoveItem == false )
            {
                return false;
            }

            bool didAddSkillPoint = targetSkillManager.TryAddSkillPoint( grantAmount );

            if ( didAddSkillPoint == false )
            {
                targetInventoryManager.TryAddItem( _itemDefinition, 1 );
                return false;
            }

            string toastMessage = $"스킬 포인트 +{grantAmount}";
            CToastMessageSystem.Show( toastMessage );
            return true;
        }

        ///<summary>
        /// 큐브 UI 열기 시도
        ///</summary>
        private bool TryOpenCubeUi(int _slotIndex)
        {
            CCubeUiManager cubeUiManager = CCubeUiManager.Instance;

            if (cubeUiManager == null || targetEquipmentManager == null)
            {
                return false;
            }

            bool result = cubeUiManager.OpenCubeUi(targetInventoryManager, targetEquipmentManager, _slotIndex);
            return result;
        }

        ///<summary>
        /// 랜덤상자 아이템 사용 처리
        ///</summary>
        private bool TryUseRandomBoxItem(CItemDefinition _randomBoxItemDefinition)
        {
            if (_randomBoxItemDefinition == null || targetInventoryManager == null)
            {
                return false;
            }

            CRandomBoxRewardTable rewardTable = _randomBoxItemDefinition.GetRandomBoxRewardTable();

            if (rewardTable == null)
            {
                CToastMessageSystem.Show("랜덤상자 보상 테이블이 없습니다.");
                return false;
            }

            bool didRollReward = rewardTable.TryRollReward(out CItemDefinition rewardItemDefinition, out long rewardCount);

            if (didRollReward == false || rewardItemDefinition == null || rewardCount <= 0L)
            {
                CToastMessageSystem.Show("획득 가능한 랜덤상자 보상이 없습니다.");
                return false;
            }

            bool didRemoveBoxItem = targetInventoryManager.TryRemoveItem(_randomBoxItemDefinition.GetItemId(), 1L);

            if (didRemoveBoxItem == false)
            {
                return false;
            }

            bool canAddReward = targetInventoryManager.CanAddItem(rewardItemDefinition, rewardCount);

            if (canAddReward == false)
            {
                targetInventoryManager.TryAddItem(_randomBoxItemDefinition, 1L);
                CToastMessageSystem.Show("인벤토리 공간이 부족합니다.");
                return false;
            }

            bool didAddReward = targetInventoryManager.TryAddItem(rewardItemDefinition, rewardCount);

            if (didAddReward == false)
            {
                targetInventoryManager.TryAddItem(_randomBoxItemDefinition, 1L);
                return false;
            }

            string rewardItemName = rewardItemDefinition.GetItemName();

            if (string.IsNullOrWhiteSpace(rewardItemName))
            {
                rewardItemName = rewardItemDefinition.GetItemId();
            }

            CToastMessageSystem.Show($"{rewardItemName} x{rewardCount} 획득");
            return true;
        }

        ///<summary>
        /// 플레이어 컨트롤러 자동 결정 시도
        ///</summary>
        private void TryResolvePlayerController()
        {
            if (targetPlayerController != null)
            {
                return;
            }

            bool hasPlayerController = CActivePlayerResolver.TryGetActivePlayerController( out PlayerController playerController );
            targetPlayerController = hasPlayerController ? playerController : null;
        }

        /// 인벤토리 매니저 바인딩 해제
        ///</summary>
        private void UnbindInventoryManager()
        {
            if (targetInventoryManager == null)
            {
                return;
            }

            targetInventoryManager.OnInventoryChanged -= HandleInventoryChanged;
            targetInventoryManager = null;
        }

        ///<summary>
        /// 인벤토리 변경 이벤트 반영
        ///</summary>
        private void HandleInventoryChanged(CPlayerInventoryManager _inventoryManager)
        {
            RefreshSlotViews();
        }

        /// 닫기 버튼 클릭 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            SetInventoryVisible(false);
        }

        ///<summary>
        /// 통합 장비 패널 바인딩 반영
        ///</summary>
        private void RefreshEquipmentStatusPanelBinding()
        {
            if (equipmentStatusPanelUi == null)
            {
                return;
            }

            equipmentStatusPanelUi.Bind(targetInventoryManager, targetEquipmentManager, targetStatManager, targetPlayerController);
        }

        ///<summary>
        /// 드래그 고스트 위치 갱신
        ///</summary>
        private void UpdateDragGhostPosition()
        {
            if (dragGhostRectTransform == null || draggedSlot == null || targetCanvas == null)
            {
                return;
            }

            RectTransform canvasRectTransform = ResolveDragGhostParentRectTransform();

            if (canvasRectTransform == null)
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            Vector2 localPoint;
            Camera eventCamera = ResolveDragEventCamera(canvasRectTransform);
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, mousePosition, eventCamera, out localPoint);

            if (isConverted == false)
            {
                return;
            }

            dragGhostRectTransform.anchoredPosition = localPoint;
            dragGhostRectTransform.SetAsLastSibling();
        }

        ///<summary>
        /// 툴팁 숨김 처리
        ///</summary>
        private void HideTooltipInternal()
        {
            CUITooltipManager.HideItemTooltip();
        }

        ///<summary>
        /// 슬롯 드래그 종료 내부 처리
        ///</summary>
        private void EndSlotDragInternal()
        {
            draggedSlot = null;
            CInventoryUiDragState.EndDrag();

            if (dragGhostImage != null)
            {
                dragGhostImage.enabled = false;
            }
        }

        ///<summary>
        /// 드래그 고스트 생성 보장
        ///</summary>
        private void EnsureDragGhost()
        {
            if (dragGhostRectTransform != null && dragGhostImage != null)
            {
                return;
            }

            RectTransform dragGhostParentRectTransform = ResolveDragGhostParentRectTransform();

            if (dragGhostParentRectTransform == null)
            {
                return;
            }

            GameObject dragGhostObject = new GameObject("ItemDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            RectTransform rectTransform = dragGhostObject.GetComponent<RectTransform>();
            rectTransform.SetParent(dragGhostParentRectTransform, false);
            rectTransform.sizeDelta = new Vector2(72.0f, 72.0f);
            Image image = dragGhostObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            LayoutElement layoutElement = dragGhostObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            rectTransform.SetAsLastSibling();
            dragGhostRectTransform = rectTransform;
            dragGhostImage = image;
        }

        ///<summary>
        /// 슬롯 프리팹 결정
        ///</summary>
        private void EnsurePrefabReferences()
        {
            if (slotPrefab == null)
            {
                GameObject slotPrefabObject = Resources.Load<GameObject>(SlotPrefabResourcePath);
                slotPrefab = slotPrefabObject != null ? slotPrefabObject.GetComponent<CItemSlot>() : null;
            }
        }

        ///<summary>
        /// 창 드래그 핸들 생성 보장
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            EnsurePopupWindowDragHandle( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 클릭 최상단 정렬 핸들러 구성
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;
            EnsurePopupWindowFocusHandlers( windowRootRectTransform, siblingTargetRectTransform );
        }

        ///<summary>
        /// 인벤토리 탭 참조 구성
        ///</summary>
        private void EnsureTabObjects()
        {
            if (inventoryTabContextList.Count == 0)
            {
                AddTabContext(equipmentTabReference, EquipmentTabDisplayName);
                AddTabContext(consumableTabReference, ConsumableTabDisplayName);
                AddTabContext(currencyTabReference, CurrencyTabDisplayName);
                AddTabContext(materialTabReference, MaterialTabDisplayName);
                AddTabContext(questTabReference, QuestTabDisplayName);
            }

            for (int index = 0; index < inventoryTabContextList.Count; index++)
            {
                CInventoryTabContext tabContext = inventoryTabContextList[index];
                CInventoryTabReference tabReference = tabContext != null ? tabContext.tabReference : null;

                if (tabReference == null)
                {
                    continue;
                }

                ApplyTabDisplayName(tabContext, tabReference.DisplayName);
            }

            RefreshTabVisualState();
        }

        ///<summary>
        /// 인벤토리 탭 컨텍스트 추가
        ///</summary>
        private void AddTabContext(CInventoryTabReference _tabReference, string _fallbackDisplayName)
        {
            if (_tabReference == null)
            {
                return;
            }

            CInventoryTabContext createdContext = new CInventoryTabContext();
            createdContext.tabReference = _tabReference;
            inventoryTabContextList.Add(createdContext);
            ApplyTabDisplayName(createdContext, _fallbackDisplayName);
        }

        ///<summary>
        /// 인벤토리 탭 버튼 이벤트 바인딩
        ///</summary>
        private void BindTabButtonEvents()
        {
            for (int index = 0; index < inventoryTabContextList.Count; index++)
            {
                CInventoryTabContext tabContext = inventoryTabContextList[index];
                CInventoryTabReference tabReference = tabContext != null ? tabContext.tabReference : null;
                CButtonEx button = tabReference != null ? tabReference.Button : null;

                if (button == null)
                {
                    continue;
                }

                UnityAction clickAction = tabContext.clickAction;

                if (clickAction != null)
                {
                    button.onClick.RemoveListener(clickAction);
                }

                eItemType itemType = tabReference.ItemType;
                clickAction = () => HandleTabButtonClicked(itemType);
                tabContext.clickAction = clickAction;
                button.onClick.AddListener(clickAction);
            }
        }

        ///<summary>
        /// 인벤토리 탭 버튼 이벤트 해제
        ///</summary>
        private void UnbindTabButtonEvents()
        {
            for (int index = 0; index < inventoryTabContextList.Count; index++)
            {
                CInventoryTabContext tabContext = inventoryTabContextList[index];
                CInventoryTabReference tabReference = tabContext != null ? tabContext.tabReference : null;
                CButtonEx button = tabReference != null ? tabReference.Button : null;
                UnityAction clickAction = tabContext != null ? tabContext.clickAction : null;

                if (button == null || clickAction == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(clickAction);
                tabContext.clickAction = null;
            }
        }

        ///<summary>
        /// 인벤토리 탭 이름 반영
        ///</summary>
        private void ApplyTabDisplayName(CInventoryTabContext _tabContext, string _fallbackDisplayName)
        {
            if (_tabContext == null || _tabContext.tabReference == null)
            {
                return;
            }

            TMP_Text nameText = _tabContext.tabReference.NameText;

            if (nameText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(_tabContext.tabReference.DisplayName) ? _fallbackDisplayName : _tabContext.tabReference.DisplayName;
            nameText.text = displayName;
        }

        ///<summary>
        /// 인벤토리 탭 시각 상태 반영
        ///</summary>
        private void RefreshTabVisualState()
        {
            for (int index = 0; index < inventoryTabContextList.Count; index++)
            {
                CInventoryTabContext tabContext = inventoryTabContextList[index];
                CInventoryTabReference tabReference = tabContext != null ? tabContext.tabReference : null;

                if (tabReference == null)
                {
                    continue;
                }

                bool isSelected = tabReference.ItemType == selectedItemType;
                Color backgroundColor = isSelected ? SelectedTabBackgroundColor : UnselectedTabBackgroundColor;
                Color innerColor = isSelected ? SelectedTabInnerColor : UnselectedTabInnerColor;
                Image backgroundImage = tabReference.BackgroundImage;
                Image innerImage = tabReference.InnerImage;

                if (backgroundImage != null)
                {
                    backgroundImage.color = backgroundColor;
                }

                if (innerImage != null)
                {
                    innerImage.color = innerColor;
                }
            }
        }

        ///<summary>
        /// 인벤토리 탭 선택 처리
        ///</summary>
        private void HandleTabButtonClicked(eItemType _itemType)
        {
            if (selectedItemType == _itemType)
            {
                return;
            }

            selectedItemType = _itemType;
            CUITooltipManager.HideItemTooltip();
            EndSlotDragInternal();
            RefreshSlotViews();
        }

        ///<summary>
        /// 인벤토리 슬롯 오브젝트 구성 보장
        ///</summary>
        private void EnsureSlotObjects()
        {
            EnsurePrefabReferences();

            if (contentRootRectTransform == null || slotPrefab == null)
            {
                return;
            }

            itemSlotList.Clear();
            int childCount = contentRootRectTransform.childCount;

            for (int index = childCount - 1; index >= 0; index--)
            {
                Transform childTransform = contentRootRectTransform.GetChild(index);
                CItemSlot itemSlot = childTransform.GetComponent<CItemSlot>();

                if (itemSlot == null)
                {
                    continue;
                }

                itemSlotList.Insert(0, itemSlot);
            }

            int requiredSlotCount = targetInventoryManager != null ? targetInventoryManager.GetSlotCountPerItemType() : 48;

            while (itemSlotList.Count < requiredSlotCount)
            {
                CItemSlot createdItemSlot = Instantiate(slotPrefab, contentRootRectTransform);
                createdItemSlot.name = $"ItemSlot_{itemSlotList.Count + 1:D2}";
                itemSlotList.Add(createdItemSlot);
            }

            while (itemSlotList.Count > requiredSlotCount)
            {
                CItemSlot removedItemSlot = itemSlotList[itemSlotList.Count - 1];
                itemSlotList.RemoveAt(itemSlotList.Count - 1);

                if (removedItemSlot != null)
                {
                    Destroy(removedItemSlot.gameObject);
                }
            }

            for (int index = 0; index < itemSlotList.Count; index++)
            {
                CItemSlot itemSlot = itemSlotList[index];

                if (itemSlot == null)
                {
                    continue;
                }

                itemSlot.Initialize(this, index);
                itemSlot.SetBoundInventorySlotIndex(index);
            }
        }

        ///<summary>
        /// 인벤토리 루트 활성 상태 반영
        ///</summary>
        private void SetInventoryRootActiveState(bool _isVisible)
        {
            if (windowRootRectTransform == null)
            {
                return;
            }

            GameObject windowRootObject = windowRootRectTransform.gameObject;

            if (windowRootObject.activeSelf == _isVisible)
            {
                return;
            }

            windowRootObject.SetActive(_isVisible);
        }

        ///<summary>
        /// 인벤토리 창 좌우 배치
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
        /// 인벤토리 창 좌우 배치 좌표 계산
        ///</summary>
        private Vector2 ResolveSideWindowAnchoredPosition( RectTransform _canvasRectTransform, RectTransform _windowRectTransform, bool _isRightSide )
        {
            Vector2 currentAnchoredPosition = _windowRectTransform.anchoredPosition;
            float anchoredPosX = InventoryWindowRightOffset;

            if ( _isRightSide == false )
            {
                anchoredPosX *= -1.0f;
            }

            Vector2 result = new Vector2( anchoredPosX, currentAnchoredPosition.y );
            return result;
        }

        ///<summary>
        /// 인벤토리 창 최상단 정렬
        ///</summary>
        public override void BringLayerToFront()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;
            BringPopupWindowToFront( siblingTargetRectTransform );
        }

        ///<summary>
        /// UI 참조 결정
        ///</summary>
        private void RefreshCachedReferences()
        {
            if (equipmentStatusPanelUi == null && equipmentStatusPanelRectTransform != null)
            {
                equipmentStatusPanelUi = equipmentStatusPanelRectTransform.GetComponent<CPlayerEquipmentStatusPanelUI>();
            }

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        ///<summary>
        /// 창 최상위 RectTransform 결정
        ///</summary>
        private RectTransform ResolveTopLevelWindowRectTransform()
        {
            if (windowRootRectTransform == null)
            {
                return null;
            }

            RectTransform canvasRectTransform = targetCanvas != null ? targetCanvas.transform as RectTransform : null;
            RectTransform currentRectTransform = windowRootRectTransform;

            while (currentRectTransform != null)
            {
                RectTransform parentRectTransform = currentRectTransform.parent as RectTransform;

                if (parentRectTransform == null)
                {
                    break;
                }

                if (parentRectTransform == canvasRectTransform)
                {
                    break;
                }

                Transform grandParentTransform = parentRectTransform.parent;

                if (grandParentTransform == canvasRectTransform)
                {
                    break;
                }

                currentRectTransform = parentRectTransform;
            }

            RectTransform result = currentRectTransform;
            return result;
        }
        ///<summary>
        /// 드래그 고스트 부모 RectTransform 결정
        ///</summary>
        private RectTransform ResolveDragGhostParentRectTransform()
        {
            if (targetCanvas == null)
            {
                return null;
            }

            RectTransform fallbackRectTransform = targetCanvas.transform as RectTransform;
            return fallbackRectTransform;
        }

        ///<summary>
        /// 드래그 좌표 변환 카메라 결정
        ///</summary>
        private Camera ResolveDragEventCamera(RectTransform _dragGhostParentRectTransform)
        {
            if (_dragGhostParentRectTransform == null)
            {
                return null;
            }

            Canvas parentCanvas = _dragGhostParentRectTransform.GetComponent<Canvas>();

            if (parentCanvas == null)
            {
                parentCanvas = targetCanvas;
            }

            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            Camera result = parentCanvas.worldCamera;
            return result;
        }
    }
}
