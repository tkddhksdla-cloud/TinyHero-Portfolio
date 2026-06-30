using System;
using System.Collections.Generic;
using TMPro;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Quest;
using TinyHero.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 퀘스트 UI 동작 모드
    ///</summary>
    public enum eQuestListUiMode
    {
        NPC_AVAILABLE,
        PLAYER_ACTIVE
    }

    ///<summary>
    /// 퀘스트 슬롯 표시 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestListSlotViewData
    {
        public GameObject slotObject;
        public CButtonEx button;
        public Image backgroundImage;
        public TMP_Text questNameText;
        public GameObject selectHighlightObject;
        public string questId = string.Empty;
    }

    ///<summary>
    /// 퀘스트 목록 UI 제어 컴포넌트
    ///</summary>
    public sealed class PopupQuestList : CUIPopup
    {
        private const string NormalQuestTypeLabel = "[ 일반 ]";
        private const string RepeatableQuestTypeLabel = "[ 반복 ]";
        private const string AcceptButtonText = "수락 하기";
        private const string GiveUpButtonText = "포기 하기";
        private const string RewardButtonText = "보상 받기";
        private const string CompleteButtonText = "완료 됨";
        private const string AcceptableColorHex = "#000000";
        private const string InProgressColorHex = "#009696";
        private const string CompleteWaitColorHex = "#00C10E";
        private const string CompleteColorHex = "#DEDEDE";
        private const string AcceptButtonColorHex = "#009FEE";
        private const string GiveUpButtonColorHex = "#EC2232";
        private const string RewardButtonColorHex = "#2CCF32";
        private const string CompleteButtonColorHex = "#919191";
        private const string QuestSlotCloneNamePrefix = "QuestSlot";
        private static readonly List<PopupQuestList> ActiveControllerList = new List<PopupQuestList>();

        [SerializeField] private eQuestListUiMode uiMode = eQuestListUiMode.NPC_AVAILABLE;
        [SerializeField] private GameObject questUiRootObject;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private RectTransform questSlotContentRootRectTransform;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private GameObject emptyObject;
        [SerializeField] private GameObject infoObject;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform rewardAreaRectTransform;
        [SerializeField] private CButtonEx interactionButton;
        [SerializeField] private Image interactionButtonBackgroundImage;
        [SerializeField] private TMP_Text interactionButtonText;
        [SerializeField] private Sprite expRewardIconSprite;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private List<CQuestListSlotView> questSlotViewComponentList = new List<CQuestListSlotView>();

        private readonly List<CQuestListSlotViewData> questSlotViewDataList = new List<CQuestListSlotViewData>();
        private readonly List<CQuestRewardSlotView> rewardSlotViewList = new List<CQuestRewardSlotView>();

        private CNPCObject currentNpcObject;
        private PlayerController currentPlayerController;
        private CQuestManager currentQuestManager;
        private string selectedQuestId = string.Empty;
        private bool isQuestListVisible;

        ///<summary>
        /// 퀘스트 UI 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureQuestSlotViewComponentList();
            EnsureQuestSlotViewList();
            EnsureRewardSlotViewList();
            RefreshQuestInfoPanel();
            SetQuestListVisible( false );
        }

        ///<summary>
        /// 퀘스트 UI 활성 구독 처리
        ///</summary>
        private void OnEnable()
        {
            if ( ActiveControllerList.Contains( this ) == false )
            {
                ActiveControllerList.Add( this );
            }

            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            BringLayerToFront();

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }

            if ( interactionButton != null )
            {
                interactionButton.onClick.RemoveListener( HandleInteractionButtonClicked );
                interactionButton.onClick.AddListener( HandleInteractionButtonClicked );
            }
        }

        ///<summary>
        /// 퀘스트 UI 비활성 정리
        ///</summary>
        private void OnDisable()
        {
            ActiveControllerList.Remove( this );
            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }

            if ( interactionButton != null )
            {
                interactionButton.onClick.RemoveListener( HandleInteractionButtonClicked );
            }

            HideRewardTooltip();
            UnsubscribeQuestManager();
        }

        ///<summary>
        /// 퀘스트 UI 프레임 입력 처리
        ///</summary>
        private void Update()
        {
            if ( isQuestListVisible == false )
            {
                return;
            }
        }

        ///<summary>
        /// NPC 퀘스트 목록 UI 표시
        ///</summary>
        public void ShowQuestListUi( CNPCObject _npcObject, PlayerController _playerController )
        {
            if ( uiMode != eQuestListUiMode.NPC_AVAILABLE )
            {
                return;
            }

            if ( _npcObject == null || _playerController == null )
            {
                return;
            }

            InitializeContext( _playerController, _npcObject );
            RefreshQuestList();
            SetQuestListVisible( true );
        }

        ///<summary>
        /// 내 퀘스트 목록 UI 토글
        ///</summary>
        public void TogglePlayerQuestListUi( PlayerController _playerController )
        {
            if ( uiMode != eQuestListUiMode.PLAYER_ACTIVE )
            {
                return;
            }

            if ( _playerController == null )
            {
                return;
            }

            if ( isQuestListVisible )
            {
                CloseQuestListUi();
                return;
            }

            InitializeContext( _playerController, null );
            RefreshQuestList();
            SetQuestListVisible( true );
        }

        ///<summary>
        /// 퀘스트 UI 표시 상태 반환
        ///</summary>
        public bool IsQuestListVisible()
        {
            bool result = isQuestListVisible;
            return result;
        }

        ///<summary>
        /// 현재 표시 중인 퀘스트 UI 존재 여부 반환
        ///</summary>
        public static bool IsAnyQuestUiVisible()
        {
            for ( int index = 0; index < ActiveControllerList.Count; index++ )
            {
                PopupQuestList controller = ActiveControllerList[ index ];

                if ( controller == null )
                {
                    continue;
                }

                if ( controller.IsQuestListVisible() == false )
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        ///<summary>
        /// NPC 상호작용 차단 상태 반환
        ///</summary>
        public bool IsBlockingNpcInteraction()
        {
            bool result = isQuestListVisible && uiMode == eQuestListUiMode.NPC_AVAILABLE;
            return result;
        }

        ///<summary>
        /// NPC 상호작용 차단 UI 존재 여부 반환
        ///</summary>
        public static bool IsAnyUiBlockingNpcInteraction()
        {
            for ( int index = 0; index < ActiveControllerList.Count; index++ )
            {
                PopupQuestList controller = ActiveControllerList[ index ];

                if ( controller == null )
                {
                    continue;
                }

                if ( controller.IsBlockingNpcInteraction() == false )
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        ///<summary>
        /// 내 퀘스트 창 토글 입력 처리
        ///</summary>
        public static bool TryProcessPlayerQuestJournalToggle( PlayerController _playerController, bool _isToggleDown )
        {
            if ( _isToggleDown == false )
            {
                return false;
            }

            PopupQuestList visiblePlayerController = null;
            PopupQuestList hiddenPlayerController = null;
            bool hasVisibleQuestUi = IsAnyQuestUiVisible();

            for ( int index = 0; index < ActiveControllerList.Count; index++ )
            {
                PopupQuestList controller = ActiveControllerList[ index ];

                if ( controller == null || controller.uiMode != eQuestListUiMode.PLAYER_ACTIVE )
                {
                    continue;
                }

                if ( controller.IsQuestListVisible() )
                {
                    visiblePlayerController = controller;
                    break;
                }

                if ( hiddenPlayerController == null )
                {
                    hiddenPlayerController = controller;
                }
            }

            if ( visiblePlayerController != null )
            {
                visiblePlayerController.TogglePlayerQuestListUi( _playerController );
                return true;
            }

            if ( hasVisibleQuestUi )
            {
                return true;
            }

            if ( hiddenPlayerController != null )
            {
                hiddenPlayerController.TogglePlayerQuestListUi( _playerController );
                return true;
            }

            return false;
        }

        ///<summary>
        /// 보상 아이템 툴팁 표시
        ///</summary>
        public void ShowRewardTooltip( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                HideRewardTooltip();
                return;
            }

            CUITooltipManager.ShowItemTooltip( _itemDefinition );
        }

        ///<summary>
        /// 보상 아이템 툴팁 숨김
        ///</summary>
        public void HideRewardTooltip()
        {
            CUITooltipManager.HideItemTooltip();
        }

        ///<summary>
        /// 퀘스트 표시 컨텍스트 초기화
        ///</summary>
        private void InitializeContext( PlayerController _playerController, CNPCObject _npcObject )
        {
            HideRewardTooltip();
            UnsubscribeQuestManager();
            currentPlayerController = _playerController;
            currentNpcObject = _npcObject;
            currentQuestManager = currentPlayerController != null ? currentPlayerController.GetQuestManager() : null;
            SubscribeQuestManager();

            if ( currentQuestManager != null )
            {
                currentQuestManager.RefreshQuestProgressState();
            }

            selectedQuestId = string.Empty;
        }

        ///<summary>
        /// 닫기 버튼 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            CloseQuestListUi();
        }

        ///<summary>
        /// 하단 상호작용 버튼 처리
        ///</summary>
        private void HandleInteractionButtonClicked()
        {
            if ( currentQuestManager == null || string.IsNullOrWhiteSpace( selectedQuestId ) )
            {
                return;
            }

            eQuestStatus questStatus = currentQuestManager.GetQuestStatus( selectedQuestId );

            if ( uiMode == eQuestListUiMode.PLAYER_ACTIVE )
            {
                bool canGiveUp = questStatus == eQuestStatus.IN_PROGRESS || questStatus == eQuestStatus.COMPLETE_WAIT;

                if ( canGiveUp == false )
                {
                    return;
                }

                bool abandonResult = currentQuestManager.AbandonQuest( selectedQuestId );

                if ( abandonResult )
                {
                    RefreshQuestList();
                }

                return;
            }

            if ( currentNpcObject == null )
            {
                return;
            }

            switch ( questStatus )
            {
                case eQuestStatus.ACCEPTABLE:
                {
                    bool hasInteractionManager = CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager );

                    if ( hasInteractionManager == false || interactionManager == null )
                    {
                        bool acceptResult = currentQuestManager.ProcessNpcQuestInteraction( currentNpcObject, selectedQuestId );

                        if ( acceptResult )
                        {
                            RefreshQuestList();
                        }

                        return;
                    }

                    SetQuestListVisible( false );
                    HideRewardTooltip();
                    interactionManager.ProcessQuestUiInteraction( currentNpcObject, currentPlayerController, selectedQuestId, HandleQuestInteractionCompleted );
                    return;
                }

                case eQuestStatus.IN_PROGRESS:
                {
                    bool abandonResult = currentQuestManager.AbandonQuest( selectedQuestId );

                    if ( abandonResult )
                    {
                        RefreshQuestList();
                    }

                    return;
                }

                case eQuestStatus.COMPLETE_WAIT:
                {
                    bool hasInteractionManager = CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager );

                    if ( hasInteractionManager == false || interactionManager == null )
                    {
                        bool claimResult = currentQuestManager.ProcessNpcQuestInteraction( currentNpcObject, selectedQuestId );

                        if ( claimResult )
                        {
                            RefreshQuestList();
                        }

                        return;
                    }

                    SetQuestListVisible( false );
                    HideRewardTooltip();
                    interactionManager.ProcessQuestUiInteraction( currentNpcObject, currentPlayerController, selectedQuestId, HandleQuestInteractionCompleted );
                    return;
                }

                case eQuestStatus.COMPLETE:
                    return;
            }
        }

        ///<summary>
        /// 퀘스트 상호작용 완료 후 UI 갱신
        ///</summary>
        private void HandleQuestInteractionCompleted( bool _result )
        {
            SetQuestListVisible( true );
            RefreshQuestList();
        }

        ///<summary>
        /// 퀘스트 선택 대화 완료 후 UI 갱신
        ///</summary>
        ///<summary>
        /// 퀘스트 상태 갱신 이벤트 반영
        ///</summary>
        private void HandleQuestUpdated( string _questId )
        {
            RefreshQuestList();
        }

        ///<summary>
        /// 퀘스트 목록 전체 갱신
        ///</summary>
        private void RefreshQuestList()
        {
            List<string> sortedQuestIdList = CollectSortedQuestIdList();
            int requiredQuestCount = sortedQuestIdList.Count;
            EnsureQuestSlotViewComponentList();
            EnsureQuestSlotViewCount( requiredQuestCount );
            EnsureQuestSlotViewList();

            for ( int index = 0; index < questSlotViewDataList.Count; index++ )
            {
                CQuestListSlotViewData slotViewData = questSlotViewDataList[ index ];

                if ( slotViewData == null || slotViewData.slotObject == null )
                {
                    continue;
                }

                if ( index >= requiredQuestCount )
                {
                    HideQuestSlotView( slotViewData );
                    continue;
                }

                string questId = sortedQuestIdList[ index ];
                ApplyQuestSlotView( slotViewData, questId );
            }

            bool hasSelectedQuest = string.IsNullOrWhiteSpace( selectedQuestId ) == false && sortedQuestIdList.Contains( selectedQuestId );

            if ( hasSelectedQuest == false )
            {
                selectedQuestId = string.Empty;
            }

            RefreshQuestSlotSelectionState();
            RefreshQuestInfoPanel();
        }

        ///<summary>
        /// 퀘스트 상세 정보 패널 갱신
        ///</summary>
        private void RefreshQuestInfoPanel()
        {
            bool hasSelectedQuest = string.IsNullOrWhiteSpace( selectedQuestId ) == false;

            if ( emptyObject != null )
            {
                emptyObject.SetActive( hasSelectedQuest == false );
            }

            if ( infoObject != null )
            {
                infoObject.SetActive( hasSelectedQuest );
            }

            if ( hasSelectedQuest == false || currentQuestManager == null )
            {
                ClearQuestInfoText();
                HideAllRewardSlots();
                RefreshInteractionButton();
                return;
            }

            bool hasDefinition = currentQuestManager.TryGetQuestDefinition( selectedQuestId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                ClearQuestInfoText();
                HideAllRewardSlots();
                RefreshInteractionButton();
                return;
            }

            if ( typeText != null )
            {
                typeText.text = ResolveQuestTypeLabel( questDefinition.GetQuestType() );
            }

            if ( titleText != null )
            {
                titleText.text = questDefinition.GetQuestName();
            }

            if ( descriptionText != null )
            {
                descriptionText.text = questDefinition.GetDescription();
            }

            if ( progressText != null )
            {
                bool hasProgressText = currentQuestManager.TryBuildQuestProgressText( selectedQuestId, out string progressDisplayText );
                progressText.gameObject.SetActive( hasProgressText );
                progressText.text = hasProgressText ? progressDisplayText : string.Empty;
            }

            RefreshRewardSlotViews( questDefinition.GetRewardEntryList() );
            RefreshInteractionButton();
        }

        ///<summary>
        /// 퀘스트 상세 텍스트 초기화
        ///</summary>
        private void ClearQuestInfoText()
        {
            if ( typeText != null )
            {
                typeText.text = string.Empty;
            }

            if ( titleText != null )
            {
                titleText.text = string.Empty;
            }

            if ( descriptionText != null )
            {
                descriptionText.text = string.Empty;
            }

            if ( progressText != null )
            {
                progressText.text = string.Empty;
                progressText.gameObject.SetActive( false );
            }
        }

        ///<summary>
        /// 보상 슬롯 목록 갱신
        ///</summary>
        private void RefreshRewardSlotViews( List<CQuestRewardEntry> _rewardEntryList )
        {
            EnsureRewardSlotViewList();
            HideAllRewardSlots();

            if ( _rewardEntryList == null )
            {
                return;
            }

            int rewardSlotIndex = 0;

            for ( int index = 0; index < _rewardEntryList.Count; index++ )
            {
                if ( rewardSlotIndex >= rewardSlotViewList.Count )
                {
                    return;
                }

                CQuestRewardEntry rewardEntry = _rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    continue;
                }

                CQuestRewardSlotView rewardSlotView = rewardSlotViewList[ rewardSlotIndex ];

                if ( rewardSlotView == null )
                {
                    continue;
                }

                if ( rewardEntry.GetRewardType() == eQuestRewardType.ITEM )
                {
                    CItemDefinition itemDefinition = rewardEntry.GetItemDefinition();

                    if ( itemDefinition == null )
                    {
                        continue;
                    }

                    rewardSlotView.ShowItemReward( itemDefinition, rewardEntry.GetItemCount() );
                    rewardSlotIndex++;
                    continue;
                }

                if ( rewardEntry.GetRewardType() == eQuestRewardType.EXP )
                {
                    rewardSlotView.ShowSpriteReward( expRewardIconSprite, rewardEntry.GetExpAmount() );
                    rewardSlotIndex++;
                }
            }
        }

        ///<summary>
        /// 보상 슬롯 전체 숨김
        ///</summary>
        private void HideAllRewardSlots()
        {
            for ( int index = 0; index < rewardSlotViewList.Count; index++ )
            {
                CQuestRewardSlotView rewardSlotView = rewardSlotViewList[ index ];

                if ( rewardSlotView == null )
                {
                    continue;
                }

                rewardSlotView.Hide();
            }
        }

        ///<summary>
        /// 상호작용 버튼 상태 갱신
        ///</summary>
        private void RefreshInteractionButton()
        {
            if ( interactionButton == null || interactionButtonBackgroundImage == null || interactionButtonText == null )
            {
                return;
            }

            bool hasSelectedQuest = string.IsNullOrWhiteSpace( selectedQuestId ) == false;
            interactionButton.gameObject.SetActive( hasSelectedQuest );

            if ( hasSelectedQuest == false || currentQuestManager == null )
            {
                interactionButton.interactable = false;
                interactionButtonText.text = string.Empty;
                ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, CompleteButtonColorHex );
                return;
            }

            eQuestStatus questStatus = currentQuestManager.GetQuestStatus( selectedQuestId );

            if ( uiMode == eQuestListUiMode.PLAYER_ACTIVE )
            {
                bool canGiveUp = questStatus == eQuestStatus.IN_PROGRESS || questStatus == eQuestStatus.COMPLETE_WAIT;
                interactionButton.interactable = canGiveUp;
                interactionButtonText.text = GiveUpButtonText;
                ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, GiveUpButtonColorHex );
                return;
            }

            interactionButton.interactable = questStatus != eQuestStatus.COMPLETE;

            switch ( questStatus )
            {
                case eQuestStatus.ACCEPTABLE:
                    interactionButtonText.text = AcceptButtonText;
                    ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, AcceptButtonColorHex );
                    return;

                case eQuestStatus.IN_PROGRESS:
                    interactionButtonText.text = GiveUpButtonText;
                    ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, GiveUpButtonColorHex );
                    return;

                case eQuestStatus.COMPLETE_WAIT:
                    interactionButtonText.text = RewardButtonText;
                    ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, RewardButtonColorHex );
                    return;

                case eQuestStatus.COMPLETE:
                    interactionButtonText.text = CompleteButtonText;
                    ApplyHtmlColorPreserveAlpha( interactionButtonBackgroundImage, CompleteButtonColorHex );
                    return;
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 선택 처리
        ///</summary>
        private void HandleQuestSlotClicked( string _questId )
        {
            selectedQuestId = string.IsNullOrWhiteSpace( _questId ) ? string.Empty : _questId.Trim();
            RefreshQuestSlotSelectionState();
            RefreshQuestInfoPanel();
        }

        ///<summary>
        /// 퀘스트 UI 완전 닫기 처리
        ///</summary>
        private void CloseQuestListUi()
        {
            HideRewardTooltip();
            selectedQuestId = string.Empty;
            currentNpcObject = null;
            currentPlayerController = null;
            UnsubscribeQuestManager();
            currentQuestManager = null;
            RefreshQuestSlotSelectionState();
            RefreshQuestInfoPanel();
            SetQuestListVisible( false );
        }

        ///<summary>
        /// 퀘스트 UI 표시 상태 반영
        ///</summary>
        private void SetQuestListVisible( bool _isVisible )
        {
            isQuestListVisible = _isVisible;

            if ( windowRootRectTransform == null )
            {
                return;
            }

            GameObject windowRootObject = windowRootRectTransform.gameObject;
            windowRootObject.SetActive( _isVisible );

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
        /// 네비게이션 레이어 표시 상태 반영
        ///</summary>
        public override void SetLayerVisible( bool _isVisible )
        {
            SetQuestListVisible( _isVisible );
        }

        ///<summary>
        /// 네비게이션 표시 상태 반환
        ///</summary>
        public override bool IsNavigationVisible()
        {
            bool result = isQuestListVisible;
            return result;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            CloseQuestListUi();
        }

        ///<summary>
        /// 퀘스트 목록 ID 수집
        ///</summary>
        private List<string> CollectSortedQuestIdList()
        {
            List<string> sortedQuestIdList = new List<string>();

            if ( uiMode == eQuestListUiMode.PLAYER_ACTIVE )
            {
                CollectPlayerActiveQuestIdList( sortedQuestIdList );
            }
            else
            {
                CollectNpcQuestIdList( sortedQuestIdList );
            }

            sortedQuestIdList.Sort( StringComparer.Ordinal );
            return sortedQuestIdList;
        }

        ///<summary>
        /// NPC 퀘스트 목록 수집
        ///</summary>
        ///<summary>
        /// NPC 퀘스트 목록 수집
        ///</summary>
        ///<summary>
        /// NPC 퀘스트 목록 수집
        ///</summary>
        private void CollectNpcQuestIdList( List<string> _questIdList )
        {
            if ( _questIdList == null || currentNpcObject == null )
            {
                return;
            }

            CNPCInteractionData interactionData = currentNpcObject.GetInteractionData();

            if ( interactionData == null )
            {
                return;
            }

            List<CNPCInteractionActionEntry> actionEntryList = interactionData.GetActionEntryList();

            if ( actionEntryList == null )
            {
                return;
            }

            HashSet<string> questIdSet = new HashSet<string>( StringComparer.Ordinal );

            for ( int index = 0; index < actionEntryList.Count; index++ )
            {
                CNPCInteractionActionEntry actionEntry = actionEntryList[ index ];

                if ( actionEntry == null || actionEntry.GetActionType() != eNPCInteractionAction.QUEST )
                {
                    continue;
                }

                string questId = actionEntry.GetLinkedQuestId();

                if ( string.IsNullOrWhiteSpace( questId ) )
                {
                    continue;
                }

                string normalizedQuestId = questId.Trim();

                if ( questIdSet.Add( normalizedQuestId ) )
                {
                    _questIdList.Add( normalizedQuestId );
                }
            }
        }

        ///<summary>
        /// 진행 중 퀘스트 목록 수집
        ///</summary>
        private void CollectPlayerActiveQuestIdList( List<string> _questIdList )
        {
            if ( _questIdList == null || currentQuestManager == null )
            {
                return;
            }

            CQuestStateProvider questStateProvider = currentQuestManager.GetQuestStateProvider();

            if ( questStateProvider == null )
            {
                return;
            }

            List<CQuestRuntimeEntryData> runtimeEntryList = questStateProvider.GetRuntimeEntryList();

            if ( runtimeEntryList == null )
            {
                return;
            }

            HashSet<string> questIdSet = new HashSet<string>( StringComparer.Ordinal );

            for ( int index = 0; index < runtimeEntryList.Count; index++ )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ index ];

                if ( runtimeEntryData == null )
                {
                    continue;
                }

                eQuestStatus questStatus = runtimeEntryData.GetQuestStatus();

                if ( questStatus != eQuestStatus.IN_PROGRESS && questStatus != eQuestStatus.COMPLETE_WAIT )
                {
                    continue;
                }

                string questId = runtimeEntryData.GetQuestId();

                if ( string.IsNullOrWhiteSpace( questId ) )
                {
                    continue;
                }

                string normalizedQuestId = questId.Trim();

                if ( questIdSet.Add( normalizedQuestId ) )
                {
                    _questIdList.Add( normalizedQuestId );
                }
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 표시 정보 반영
        ///</summary>
        private void ApplyQuestSlotView( CQuestListSlotViewData _slotViewData, string _questId )
        {
            if ( _slotViewData == null || _slotViewData.slotObject == null )
            {
                return;
            }

            _slotViewData.questId = _questId;
            _slotViewData.slotObject.SetActive( true );

            string questName = _questId;
            eQuestType questType = eQuestType.NORMAL;

            if ( currentQuestManager != null && currentQuestManager.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition ) && questDefinition != null )
            {
                questName = questDefinition.GetQuestName();
                questType = questDefinition.GetQuestType();
            }

            if ( _slotViewData.questNameText != null )
            {
                _slotViewData.questNameText.text = $"{ResolveQuestTypeLabel( questType )} {questName}";
            }

            if ( _slotViewData.backgroundImage != null && currentQuestManager != null )
            {
                eQuestStatus questStatus = currentQuestManager.GetQuestStatus( _questId );
                ApplyQuestSlotColor( _slotViewData.backgroundImage, questStatus );
            }

            ApplyQuestSlotSelectionState( _slotViewData );

            if ( _slotViewData.button != null )
            {
                _slotViewData.button.onClick.RemoveAllListeners();
                string clickedQuestId = _questId;
                _slotViewData.button.onClick.AddListener( () => HandleQuestSlotClicked( clickedQuestId ) );
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 숨김 처리
        ///</summary>
        private void HideQuestSlotView( CQuestListSlotViewData _slotViewData )
        {
            if ( _slotViewData == null || _slotViewData.slotObject == null )
            {
                return;
            }

            _slotViewData.questId = string.Empty;
            ApplyQuestSlotSelectionState( _slotViewData );
            _slotViewData.slotObject.SetActive( false );
        }

        ///<summary>
        /// 퀘스트 슬롯 선택 상태 일괄 반영
        ///</summary>
        private void RefreshQuestSlotSelectionState()
        {
            for ( int index = 0; index < questSlotViewDataList.Count; index++ )
            {
                CQuestListSlotViewData slotViewData = questSlotViewDataList[ index ];
                ApplyQuestSlotSelectionState( slotViewData );
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 선택 상태 반영
        ///</summary>
        private void ApplyQuestSlotSelectionState( CQuestListSlotViewData _slotViewData )
        {
            if ( _slotViewData == null || _slotViewData.selectHighlightObject == null )
            {
                return;
            }

            bool isSelectedQuest = string.IsNullOrWhiteSpace( selectedQuestId ) == false
                && string.Equals( _slotViewData.questId, selectedQuestId, StringComparison.Ordinal );
            _slotViewData.selectHighlightObject.SetActive( isSelectedQuest );
        }

        ///<summary>
        /// 퀘스트 슬롯 개수 보장
        ///</summary>
        private void EnsureQuestSlotViewCount( int _requiredCount )
        {
            if ( questSlotContentRootRectTransform == null || questSlotViewComponentList.Count == 0 )
            {
                return;
            }

            CQuestListSlotView templateSlotView = questSlotViewComponentList[ 0 ];

            if ( templateSlotView == null )
            {
                return;
            }

            GameObject slotTemplateObject = templateSlotView.GetSlotRootObject();

            if ( slotTemplateObject == null )
            {
                return;
            }

            while ( questSlotViewComponentList.Count < _requiredCount )
            {
                GameObject createdSlotObject = Instantiate( slotTemplateObject, questSlotContentRootRectTransform );
                createdSlotObject.name = $"{QuestSlotCloneNamePrefix}_{questSlotViewComponentList.Count + 1:00}";
                CQuestListSlotView createdSlotView = createdSlotObject.GetComponent<CQuestListSlotView>();

                if ( createdSlotView == null )
                {
                    createdSlotView = createdSlotObject.AddComponent<CQuestListSlotView>();
                }

                createdSlotView.AutoAssignReferences();
                questSlotViewComponentList.Add( createdSlotView );
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 캐시 구성 보장
        ///</summary>
        ///<summary>
        /// 퀘스트 슬롯 컴포넌트 목록 보장
        ///</summary>
        private void EnsureQuestSlotViewComponentList()
        {
            if ( questSlotContentRootRectTransform == null )
            {
                return;
            }

            bool hasConfiguredSlot = false;

            for ( int index = 0; index < questSlotViewComponentList.Count; index++ )
            {
                CQuestListSlotView questSlotView = questSlotViewComponentList[ index ];

                if ( questSlotView == null )
                {
                    continue;
                }

                questSlotView.AutoAssignReferences();

                if ( questSlotView.IsValid() )
                {
                    hasConfiguredSlot = true;
                }
            }

            if ( hasConfiguredSlot )
            {
                return;
            }

            questSlotViewComponentList.Clear();
            int childCount = questSlotContentRootRectTransform.childCount;

            for ( int index = 0; index < childCount; index++ )
            {
                Transform childTransform = questSlotContentRootRectTransform.GetChild( index );
                CQuestListSlotView questSlotView = childTransform.GetComponent<CQuestListSlotView>();

                if ( questSlotView == null )
                {
                    questSlotView = childTransform.gameObject.AddComponent<CQuestListSlotView>();
                }

                questSlotView.AutoAssignReferences();
                questSlotViewComponentList.Add( questSlotView );
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 캐시 구성 보장
        ///</summary>
        private void EnsureQuestSlotViewList()
        {
            questSlotViewDataList.Clear();
            EnsureQuestSlotViewComponentList();

            for ( int index = 0; index < questSlotViewComponentList.Count; index++ )
            {
                CQuestListSlotView questSlotView = questSlotViewComponentList[ index ];
                CQuestListSlotViewData slotViewData = CreateQuestSlotViewData( questSlotView );

                if ( slotViewData.slotObject == null )
                {
                    continue;
                }

                questSlotViewDataList.Add( slotViewData );
            }
        }

        ///<summary>
        /// 퀘스트 슬롯 캐시 생성
        ///</summary>
        private CQuestListSlotViewData CreateQuestSlotViewData( CQuestListSlotView _questSlotView )
        {
            CQuestListSlotViewData slotViewData = new CQuestListSlotViewData();

            if ( _questSlotView == null )
            {
                return slotViewData;
            }

            _questSlotView.AutoAssignReferences();
            slotViewData.slotObject = _questSlotView.GetSlotRootObject();
            slotViewData.button = _questSlotView.GetButton();
            slotViewData.backgroundImage = _questSlotView.GetBackgroundImage();
            slotViewData.questNameText = _questSlotView.GetQuestNameText();
            slotViewData.selectHighlightObject = _questSlotView.GetSelectHighlightObject();
            return slotViewData;
        }

        ///<summary>
        /// 보상 슬롯 캐시 구성 보장
        ///</summary>
        private void EnsureRewardSlotViewList()
        {
            if ( rewardAreaRectTransform == null )
            {
                return;
            }

            rewardSlotViewList.Clear();
            int childCount = rewardAreaRectTransform.childCount;

            for ( int index = 0; index < childCount; index++ )
            {
                Transform childTransform = rewardAreaRectTransform.GetChild( index );
                CQuestRewardSlotView rewardSlotView = childTransform.GetComponent<CQuestRewardSlotView>();

                if ( rewardSlotView == null )
                {
                    rewardSlotView = childTransform.gameObject.AddComponent<CQuestRewardSlotView>();
                }

                rewardSlotView.Configure( this );
                rewardSlotViewList.Add( rewardSlotView );
            }
        }

        ///<summary>
        /// 창 드래그 핸들 구성 보장
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
        /// 퀘스트 매니저 이벤트 구독
        ///</summary>
        private void SubscribeQuestManager()
        {
            if ( currentQuestManager == null )
            {
                return;
            }

            currentQuestManager.OnQuestUpdated -= HandleQuestUpdated;
            currentQuestManager.OnQuestUpdated += HandleQuestUpdated;
        }

        ///<summary>
        /// 퀘스트 매니저 이벤트 해제
        ///</summary>
        private void UnsubscribeQuestManager()
        {
            if ( currentQuestManager == null )
            {
                return;
            }

            currentQuestManager.OnQuestUpdated -= HandleQuestUpdated;
        }

        ///<summary>
        /// 퀘스트 타입 표시 문자열 결정
        ///</summary>
        private string ResolveQuestTypeLabel( eQuestType _questType )
        {
            string result = _questType == eQuestType.REPEATABLE ? RepeatableQuestTypeLabel : NormalQuestTypeLabel;
            return result;
        }

        ///<summary>
        /// 퀘스트 슬롯 배경 색상 반영
        ///</summary>
        private void ApplyQuestSlotColor( Image _targetImage, eQuestStatus _questStatus )
        {
            if ( _targetImage == null )
            {
                return;
            }

            string htmlColor = AcceptableColorHex;

            switch ( _questStatus )
            {
                case eQuestStatus.IN_PROGRESS:
                    htmlColor = InProgressColorHex;
                    break;

                case eQuestStatus.COMPLETE_WAIT:
                    htmlColor = CompleteWaitColorHex;
                    break;

                case eQuestStatus.COMPLETE:
                    htmlColor = CompleteColorHex;
                    break;
            }

            ApplyHtmlColorPreserveAlpha( _targetImage, htmlColor );
        }

        ///<summary>
        /// 이미지 색상 반영 및 알파 보존
        ///</summary>
        private void ApplyHtmlColorPreserveAlpha( Image _targetImage, string _htmlColor )
        {
            if ( _targetImage == null )
            {
                return;
            }

            Color resolvedColor = ResolveHtmlColor( _htmlColor );
            Color currentColor = _targetImage.color;
            resolvedColor.a = currentColor.a;
            _targetImage.color = resolvedColor;
        }

        ///<summary>
        /// HTML 색상 문자열 변환
        ///</summary>
        private Color ResolveHtmlColor( string _htmlColor )
        {
            bool isParsed = ColorUtility.TryParseHtmlString( _htmlColor, out Color colorValue );

            if ( isParsed )
            {
                return colorValue;
            }

            return Color.white;
        }

        ///<summary>
        /// 참조 컴포넌트 보정
        ///</summary>
        private void ResolveReferences()
        {
            if ( windowRootRectTransform == null )
            {
                if ( questUiRootObject != null )
                {
                    windowRootRectTransform = questUiRootObject.transform as RectTransform;
                }
                else
                {
                    windowRootRectTransform = transform as RectTransform;
                }
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        ///<summary>
        /// 퀘스트 창 최상단 정렬
        ///</summary>
        public override void BringLayerToFront()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;
            BringPopupWindowToFront( siblingTargetRectTransform );
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
