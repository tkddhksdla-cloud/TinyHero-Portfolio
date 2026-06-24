using TinyHero.Core.Data;
using TinyHero.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 큐브 잠재능력 UI 제어 컴포넌트
    ///</summary>
    public sealed class PopupCube : CUIPopup, IDropHandler
    {
        private enum eCubeTargetSource
        {
            NONE,
            INVENTORY,
            EQUIPPED
        }

        private static readonly Color EmptyFrameColor = new Color32(56, 64, 82, 255);
        private static readonly Color SelectedFrameColor = new Color32(79, 118, 188, 255);
        private static readonly Color HiddenIconColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        [Header("Window")]
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private CButtonEx rerollButton;

        [Header("Selection")]
        [SerializeField] private RectTransform equipmentDropSlotRectTransform;
        [SerializeField] private Image selectedItemIconImage;
        [SerializeField] private Image selectedItemFrameImage;
        [SerializeField] private TMP_Text selectedItemHintText;
        [SerializeField] private TMP_Text selectedItemSourceText;

        [Header("Potential Panels")]
        [SerializeField] private CButtonEx currentPanelButton;
        [SerializeField] private CButtonEx previewPanelButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text cubeNameText;
        [SerializeField] private TMP_Text currentRankText;
        [SerializeField] private TMP_Text previewRankText;
        [SerializeField] private TMP_Text currentLine1Text;
        [SerializeField] private TMP_Text currentLine2Text;
        [SerializeField] private TMP_Text currentLine3Text;
        [SerializeField] private TMP_Text previewLine1Text;
        [SerializeField] private TMP_Text previewLine2Text;
        [SerializeField] private TMP_Text previewLine3Text;
        [SerializeField] private TMP_Text statusText;

        private CPlayerInventoryManager targetInventoryManager;
        private CPlayerEquipmentManager targetEquipmentManager;
        private int cubeInventorySlotIndex = -1;
        private eCubeTargetSource selectedTargetSource = eCubeTargetSource.NONE;
        private int selectedInventorySlotIndex = -1;
        private eEquipmentType selectedEquipmentType = eEquipmentType.NONE;
        private CEquipmentPotentialData previewPotentialData;
        private string cubeItemId = string.Empty;
        private bool isVisible;

        ///<summary>
        /// 큐브 UI 초기화 처리
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureWindowInteractions();
            BindButtons();
            isVisible = gameObject.activeSelf;
        }

        ///<summary>
        /// 큐브 UI 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            EnsureWindowInteractions();
            BindButtons();
        }

        ///<summary>
        /// 큐브 UI 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            UnbindButtons();
        }

        ///<summary>
        /// 큐브 UI 열기 시도
        ///</summary>
        public bool TryOpen(CPlayerInventoryManager _inventoryManager, CPlayerEquipmentManager _equipmentManager, int _cubeInventorySlotIndex)
        {
            if (_inventoryManager == null || _equipmentManager == null || _cubeInventorySlotIndex < 0)
            {
                return false;
            }

            CItemDefinition cubeItemDefinition = _inventoryManager.GetItemDefinitionAtSlot(_cubeInventorySlotIndex);

            if (cubeItemDefinition == null || cubeItemDefinition.IsCube() == false)
            {
                return false;
            }

            targetInventoryManager = _inventoryManager;
            targetEquipmentManager = _equipmentManager;
            cubeInventorySlotIndex = _cubeInventorySlotIndex;
            cubeItemId = cubeItemDefinition.GetItemId();
            ClearSelection();
            SetVisible(true);
            RefreshView();
            return true;
        }

        ///<summary>
        /// 큐브 UI 표시 상태 반환
        ///</summary>
        public bool IsVisible()
        {
            bool result = isVisible;
            return result;
        }

        ///<summary>
        /// 큐브 UI 표시 상태 설정
        ///</summary>
        public void SetVisible(bool _isVisible)
        {
            isVisible = _isVisible;
            GameObject rootObject = gameObject;

            if (rootObject.activeSelf != _isVisible)
            {
                rootObject.SetActive(_isVisible);
            }

            if (windowRootRectTransform == null)
            {
                return;
            }

            GameObject windowRootObject = windowRootRectTransform.gameObject;

            if (windowRootObject.activeSelf != _isVisible)
            {
                windowRootObject.SetActive(_isVisible);
            }

            if (_isVisible)
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
        /// 네비게이션 레이어 표시 상태 반영
        ///</summary>
        public override void SetLayerVisible( bool _isVisible )
        {
            SetVisible( _isVisible );
        }

        ///<summary>
        /// 네비게이션 표시 상태 반환
        ///</summary>
        public override bool IsNavigationVisible()
        {
            bool result = isVisible;
            return result;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            previewPotentialData = null;
            SetVisible( false );
        }

        ///<summary>
        /// 큐브 UI 전체 갱신
        ///</summary>
        public void RefreshView()
        {
            if (isVisible == false)
            {
                return;
            }

            ValidateSelection();
            CItemDefinition cubeItemDefinition = ResolveCubeItemDefinition();
            CItemDefinition selectedItemDefinition = ResolveSelectedItemDefinition();
            CEquipmentPotentialData currentPotentialData = ResolveSelectedPotentialData();

            if (cubeNameText != null)
            {
                cubeNameText.text = cubeItemDefinition != null ? cubeItemDefinition.GetItemName() : "Cube";
            }

            ApplySelectedItemVisual(selectedItemDefinition);
            ApplyPotentialText(currentPotentialData, previewPotentialData);
            UpdateButtonInteractableState(selectedItemDefinition);
            UpdateStatusText(selectedItemDefinition);
        }

        ///<summary>
        /// 큐브 미리보기 생성 처리
        ///</summary>
        private void HandleRerollClicked()
        {
            if (targetInventoryManager == null)
            {
                return;
            }

            CItemDefinition targetItemDefinition = ResolveSelectedItemDefinition();

            if (targetItemDefinition == null)
            {
                ApplyStatusText("장착 여부와 관계없이 장비를 직접 드래그해서 큐브 대상에 올려두세요.");
                return;
            }

            CEquipmentPotentialData currentPotentialData = ResolveSelectedPotentialData();

            if (currentPotentialData == null)
            {
                ApplyStatusText("현재 장비의 잠재능력 데이터를 찾을 수 없습니다.");
                return;
            }

            CItemDefinition cubeItemDefinition = ResolveCubeItemDefinition();

            if (cubeItemDefinition == null)
            {
                ApplyStatusText("큐브 아이템을 찾을 수 없습니다.");
                return;
            }

            bool didConsumeCube = targetInventoryManager.TryRemoveItem(cubeItemDefinition.GetItemId(), 1);

            if (didConsumeCube == false)
            {
                ApplyStatusText("큐브가 부족합니다.");
                return;
            }

            CEquipmentPotentialData rolledPotentialData = currentPotentialData.CreateCopy();
            bool didRoll = CEquipmentPotentialRollUtility.TryRollPotential(targetItemDefinition.GetEquipmentType(), rolledPotentialData);

            if (didRoll == false)
            {
                ApplyStatusText("잠재능력 테이블 설정을 확인하세요.");
                cubeInventorySlotIndex = ResolveCubeSlotIndexByItemId(cubeItemDefinition.GetItemId());
                return;
            }

            previewPotentialData = rolledPotentialData;
            cubeInventorySlotIndex = ResolveCubeSlotIndexByItemId(cubeItemDefinition.GetItemId());
            RefreshView();
        }

        ///<summary>
        /// 현재 영역 클릭 처리
        ///</summary>
        private void HandleCurrentPanelClicked()
        {
            if (previewPotentialData == null)
            {
                ApplyStatusText("유지할 미리보기 잠재능력이 없습니다.");
                return;
            }

            previewPotentialData = null;
            RefreshView();
        }

        ///<summary>
        /// 미리보기 영역 클릭 처리
        ///</summary>
        private void HandlePreviewPanelClicked()
        {
            if (previewPotentialData == null)
            {
                ApplyStatusText("적용할 미리보기 잠재능력이 없습니다.");
                return;
            }

            bool didApplyPotential = TryApplyPotentialToCurrentTarget(previewPotentialData);

            if (didApplyPotential == false)
            {
                ApplyStatusText("잠재능력 적용에 실패했습니다.");
                return;
            }

            previewPotentialData = null;
            RefreshView();
        }

        ///<summary>
        /// 큐브 UI 닫기 처리
        ///</summary>
        private void HandleCloseClicked()
        {
            previewPotentialData = null;
            SetVisible(false);
        }

        ///<summary>
        /// 장비 드롭 처리
        ///</summary>
        public void OnDrop(PointerEventData _eventData)
        {
            bool didSelectInventoryTarget = TrySelectInventoryDragTarget();

            if (didSelectInventoryTarget)
            {
                return;
            }

            TrySelectEquippedDragTarget();
        }

        ///<summary>
        /// 인벤토리 드래그 대상 선택 처리
        ///</summary>
        private bool TrySelectInventoryDragTarget()
        {
            if (CInventoryUiDragState.IsDragging() == false || targetInventoryManager == null)
            {
                return false;
            }

            int slotIndex = CInventoryUiDragState.GetDraggedSlotIndex();
            CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(slotIndex);

            if (itemDefinition == null || itemDefinition.IsEquipmentItem() == false)
            {
                ApplyStatusText("장비 아이템만 큐브 대상에 등록할 수 있습니다.");
                return false;
            }

            selectedTargetSource = eCubeTargetSource.INVENTORY;
            selectedInventorySlotIndex = slotIndex;
            selectedEquipmentType = eEquipmentType.NONE;
            previewPotentialData = null;
            RefreshView();
            return true;
        }

        ///<summary>
        /// 장착 드래그 대상 선택 처리
        ///</summary>
        private bool TrySelectEquippedDragTarget()
        {
            if (CEquipmentUiDragState.IsDragging() == false || targetEquipmentManager == null)
            {
                return false;
            }

            eEquipmentType draggedEquipmentType = CEquipmentUiDragState.GetDraggedEquipmentType();

            if (draggedEquipmentType == eEquipmentType.NONE || targetEquipmentManager.HasEquippedItem(draggedEquipmentType) == false)
            {
                return false;
            }

            selectedTargetSource = eCubeTargetSource.EQUIPPED;
            selectedEquipmentType = draggedEquipmentType;
            selectedInventorySlotIndex = -1;
            previewPotentialData = null;
            RefreshView();
            return true;
        }

        ///<summary>
        /// 현재 대상 잠재능력 적용 처리
        ///</summary>
        private bool TryApplyPotentialToCurrentTarget(CEquipmentPotentialData _equipmentPotentialData)
        {
            if (_equipmentPotentialData == null)
            {
                return false;
            }

            if (selectedTargetSource == eCubeTargetSource.INVENTORY)
            {
                if (targetInventoryManager == null)
                {
                    return false;
                }

                bool didApplyInventoryPotential = targetInventoryManager.TrySetItemEntryPotentialData(selectedInventorySlotIndex, _equipmentPotentialData);
                return didApplyInventoryPotential;
            }

            if (selectedTargetSource == eCubeTargetSource.EQUIPPED)
            {
                if (targetEquipmentManager == null)
                {
                    return false;
                }

                bool didApplyEquippedPotential = targetEquipmentManager.TrySetEquippedPotentialData(selectedEquipmentType, _equipmentPotentialData);
                return didApplyEquippedPotential;
            }

            return false;
        }

        ///<summary>
        /// 선택 장비 시각 요소 반영
        ///</summary>
        private void ApplySelectedItemVisual(CItemDefinition _selectedItemDefinition)
        {
            bool hasSelectedItem = _selectedItemDefinition != null;

            if (selectedItemHintText != null)
            {
                selectedItemHintText.text = hasSelectedItem ? _selectedItemDefinition.GetDescription() : "인벤토리 또는 장착 창에서 장비를 끌어와 잠재능력을 설정하세요.";
            }

            if (selectedItemSourceText != null)
            {
                selectedItemSourceText.text = ResolveSelectedSourceLabel();
            }

            if (selectedItemIconImage != null)
            {
                Sprite iconSprite = hasSelectedItem ? _selectedItemDefinition.GetIconSprite() : null;
                selectedItemIconImage.sprite = iconSprite;
                selectedItemIconImage.enabled = iconSprite != null;
                selectedItemIconImage.color = iconSprite != null ? Color.white : HiddenIconColor;
            }

            if (selectedItemFrameImage != null)
            {
                selectedItemFrameImage.color = hasSelectedItem ? SelectedFrameColor : EmptyFrameColor;
            }
        }

        ///<summary>
        /// 선택 대상 출처 문구 반환
        ///</summary>
        private string ResolveSelectedSourceLabel()
        {
            switch (selectedTargetSource)
            {
                case eCubeTargetSource.INVENTORY:
                    return $"인벤토리 슬롯 {selectedInventorySlotIndex + 1}";

                case eCubeTargetSource.EQUIPPED:
                    return $"장착 장비 / {selectedEquipmentType}";
            }

            return "대상 미선택";
        }

        ///<summary>
        /// 버튼 상호작용 상태 갱신
        ///</summary>
        private void UpdateButtonInteractableState(CItemDefinition _selectedItemDefinition)
        {
            bool hasSelectedItem = _selectedItemDefinition != null;
            bool hasPreview = previewPotentialData != null;
            bool isPreviewRankUp = IsPreviewRankUp();

            if (rerollButton != null)
            {
                rerollButton.interactable = hasSelectedItem && isPreviewRankUp == false;
            }

            if (currentPanelButton != null)
            {
                currentPanelButton.interactable = hasPreview;
            }

            if (previewPanelButton != null)
            {
                previewPanelButton.interactable = hasPreview;
            }
        }

        ///<summary>
        /// 상태 안내 문구 갱신
        ///</summary>
        private void UpdateStatusText(CItemDefinition _selectedItemDefinition)
        {
            if (_selectedItemDefinition == null)
            {
                ApplyStatusText("잠재능력을 재설정 할 장비를 지정하세요.");
                return;
            }

            if (previewPotentialData == null)
            {
                ApplyStatusText("재설정 버튼으로 잠재능력을 재설정 하세요.");
                return;
            }

            if (IsPreviewRankUp())
            {
                ApplyStatusText("등급이 상승했습니다. 잠재능력 중 하나를 선택해주세요.");
                return;
            }

            ApplyStatusText("CURRENT 영역을 클릭하면 유지하고, PREVIEW 영역을 클릭하면 새 잠재능력을 적용합니다.");
        }

        ///<summary>
        /// 미리보기 잠재능력 등급 상승 여부 반환
        ///</summary>
        private bool IsPreviewRankUp()
        {
            if (previewPotentialData == null)
            {
                return false;
            }

            CEquipmentPotentialData currentPotentialData = ResolveSelectedPotentialData();

            if (currentPotentialData == null)
            {
                return false;
            }

            int currentRankValue = (int)currentPotentialData.GetRank();
            int previewRankValue = (int)previewPotentialData.GetRank();
            bool result = previewRankValue > currentRankValue;
            return result;
        }

        ///<summary>
        /// 상태 안내 문구 반영
        ///</summary>
        private void ApplyStatusText(string _message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = _message;
        }

        ///<summary>
        /// 잠재능력 표시 텍스트 반영
        ///</summary>
        private void ApplyPotentialText(CEquipmentPotentialData _currentPotentialData, CEquipmentPotentialData _previewPotentialData)
        {
            ApplyPotentialColumn(_currentPotentialData, currentRankText, currentLine1Text, currentLine2Text, currentLine3Text);
            ApplyPotentialColumn(_previewPotentialData, previewRankText, previewLine1Text, previewLine2Text, previewLine3Text);
        }

        ///<summary>
        /// 잠재능력 컬럼 텍스트 반영
        ///</summary>
        private void ApplyPotentialColumn(CEquipmentPotentialData _potentialData, TMP_Text _rankText, TMP_Text _line1Text, TMP_Text _line2Text, TMP_Text _line3Text)
        {
            eEquipmentPotentialRank rank = _potentialData != null ? _potentialData.GetRank() : eEquipmentPotentialRank.COMMON;
            string rankLabel = CEquipmentPotentialUtility.GetRankShortLabel(rank);
            Color rankColor = _potentialData != null ? CEquipmentPotentialUtility.GetRankColor(rank) : Color.white;

            if (_rankText != null)
            {
                _rankText.text = _potentialData != null ? $"[{rankLabel}] {rank}" : "-";
                _rankText.color = rankColor;
            }

            TMP_Text[] lineTextArray = { _line1Text, _line2Text, _line3Text };

            for (int index = 0; index < lineTextArray.Length; index++)
            {
                TMP_Text lineText = lineTextArray[index];

                if (lineText == null)
                {
                    continue;
                }

                if (_potentialData == null)
                {
                    lineText.text = "-";
                    lineText.color = Color.white;
                    continue;
                }

                CEquipmentPotentialLineData lineData = _potentialData.GetLineData(index);
                eEquipmentPotentialRank lineRank = lineData != null ? lineData.GetLineRank() : rank;
                Color lineRankColor = CEquipmentPotentialUtility.GetRankColor(lineRank);
                lineText.text = CEquipmentPotentialUtility.BuildLineText(rank, lineData);
                lineText.color = lineRankColor;
            }
        }

        ///<summary>
        /// 선택 상태 초기화
        ///</summary>
        private void ClearSelection()
        {
            selectedTargetSource = eCubeTargetSource.NONE;
            selectedInventorySlotIndex = -1;
            selectedEquipmentType = eEquipmentType.NONE;
            previewPotentialData = null;
        }

        ///<summary>
        /// 선택 상태 유효성 보정
        ///</summary>
        private void ValidateSelection()
        {
            if (selectedTargetSource == eCubeTargetSource.INVENTORY)
            {
                if (targetInventoryManager == null)
                {
                    ClearSelection();
                    return;
                }

                CItemDefinition itemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(selectedInventorySlotIndex);

                if (itemDefinition == null || itemDefinition.IsEquipmentItem() == false)
                {
                    ClearSelection();
                }

                return;
            }

            if (selectedTargetSource == eCubeTargetSource.EQUIPPED)
            {
                if (targetEquipmentManager == null || targetEquipmentManager.HasEquippedItem(selectedEquipmentType) == false)
                {
                    ClearSelection();
                }
            }
        }

        ///<summary>
        /// 선택 장비 정의 결정
        ///</summary>
        private CItemDefinition ResolveSelectedItemDefinition()
        {
            if (selectedTargetSource == eCubeTargetSource.INVENTORY)
            {
                if (targetInventoryManager == null)
                {
                    return null;
                }

                CItemDefinition inventoryItemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(selectedInventorySlotIndex);
                return inventoryItemDefinition;
            }

            if (selectedTargetSource == eCubeTargetSource.EQUIPPED)
            {
                if (targetEquipmentManager == null)
                {
                    return null;
                }

                CItemDefinition equippedItemDefinition = targetEquipmentManager.GetEquippedItemDefinition(selectedEquipmentType);
                return equippedItemDefinition;
            }

            return null;
        }

        ///<summary>
        /// 선택 장비 잠재능력 결정
        ///</summary>
        private CEquipmentPotentialData ResolveSelectedPotentialData()
        {
            if (selectedTargetSource == eCubeTargetSource.INVENTORY)
            {
                if (targetInventoryManager == null)
                {
                    return null;
                }

                CInventoryItemEntryData itemEntryData = targetInventoryManager.GetItemEntryData(selectedInventorySlotIndex);

                if (itemEntryData == null || itemEntryData.IsEmpty())
                {
                    return null;
                }

                CEquipmentPotentialData inventoryPotentialData = itemEntryData.GetEquipmentPotentialData();
                return inventoryPotentialData;
            }

            if (selectedTargetSource == eCubeTargetSource.EQUIPPED)
            {
                if (targetEquipmentManager == null)
                {
                    return null;
                }

                CEquipmentPotentialData equippedPotentialData = targetEquipmentManager.GetEquippedPotentialData(selectedEquipmentType);
                return equippedPotentialData;
            }

            return null;
        }

        ///<summary>
        /// 큐브 아이템 정의 결정
        ///</summary>
        private CItemDefinition ResolveCubeItemDefinition()
        {
            if (targetInventoryManager == null)
            {
                return null;
            }

            if (cubeInventorySlotIndex >= 0)
            {
                CItemDefinition slotItemDefinition = targetInventoryManager.GetItemDefinitionAtSlot(cubeInventorySlotIndex);

                if (slotItemDefinition != null && slotItemDefinition.IsCube())
                {
                    return slotItemDefinition;
                }
            }

            if (string.IsNullOrWhiteSpace(cubeItemId))
            {
                return null;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition(cubeItemId, out CItemDefinition cubeItemDefinition);
            CItemDefinition result = hasDefinition ? cubeItemDefinition : null;
            return result;
        }

        ///<summary>
        /// 큐브 슬롯 인덱스 재탐색
        ///</summary>
        private int ResolveCubeSlotIndexByItemId(string _cubeItemId)
        {
            if (targetInventoryManager == null || string.IsNullOrWhiteSpace(_cubeItemId))
            {
                return -1;
            }

            int slotCount = targetInventoryManager.GetSlotCount();

            for (int index = 0; index < slotCount; index++)
            {
                CInventoryItemEntryData itemEntryData = targetInventoryManager.GetItemEntryData(index);

                if (itemEntryData == null || itemEntryData.IsEmpty())
                {
                    continue;
                }

                if (string.Equals(itemEntryData.GetItemId(), _cubeItemId, System.StringComparison.Ordinal) == false)
                {
                    continue;
                }

                return index;
            }

            return -1;
        }

        ///<summary>
        /// 버튼 이벤트 바인딩
        ///</summary>
        private void BindButtons()
        {
            UnbindButtons();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(HandleRerollClicked);
            }

            if (currentPanelButton != null)
            {
                currentPanelButton.onClick.AddListener(HandleCurrentPanelClicked);
            }

            if (previewPanelButton != null)
            {
                previewPanelButton.onClick.AddListener(HandlePreviewPanelClicked);
            }
        }

        ///<summary>
        /// 버튼 이벤트 해제
        ///</summary>
        private void UnbindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveListener(HandleRerollClicked);
            }

            if (currentPanelButton != null)
            {
                currentPanelButton.onClick.RemoveListener(HandleCurrentPanelClicked);
            }

            if (previewPanelButton != null)
            {
                previewPanelButton.onClick.RemoveListener(HandlePreviewPanelClicked);
            }
        }

        ///<summary>
        /// 창 드래그 및 포커스 상호작용 보장
        ///</summary>
        private void EnsureWindowInteractions()
        {
            if (windowDragHandleRectTransform != null)
            {
                CItemInventoryWindowDragHandle dragHandle = windowDragHandleRectTransform.GetComponent<CItemInventoryWindowDragHandle>();

                if (dragHandle == null)
                {
                    dragHandle = windowDragHandleRectTransform.gameObject.AddComponent<CItemInventoryWindowDragHandle>();
                }

                dragHandle.Configure(windowRootRectTransform, GetComponentInParent<Canvas>());
            }

            RectTransform siblingTargetRectTransform = transform as RectTransform;

            if (siblingTargetRectTransform == null || windowRootRectTransform == null)
            {
                return;
            }

            Graphic[] graphicArray = windowRootRectTransform.GetComponentsInChildren<Graphic>(true);

            for (int index = 0; index < graphicArray.Length; index++)
            {
                Graphic graphic = graphicArray[index];

                if (graphic == null || graphic.raycastTarget == false)
                {
                    continue;
                }

                CWindowDragHandle focusHandler = graphic.GetComponent<CWindowDragHandle>();

                if (focusHandler == null)
                {
                    focusHandler = graphic.gameObject.AddComponent<CWindowDragHandle>();
                }

                focusHandler.Configure(siblingTargetRectTransform);
            }
        }

        ///<summary>
        /// UI 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if (windowRootRectTransform == null)
            {
                windowRootRectTransform = transform as RectTransform;
            }
        }
    }
}
