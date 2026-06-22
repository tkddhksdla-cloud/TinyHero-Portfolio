using System.Collections.Generic;
using TinyHero.Player;
using TinyHero.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 목록 UI 제어 컴포넌트
    ///</summary>
    public sealed class CSkillListUIController : MonoBehaviour
    {
        private const string SkillTooltipPrefabResourcePath = "Prefabs/UI/Skill/SkillTooltipUI";
        private const string SkillSlotCloneNamePrefix = "SkillSlot";
        private const KeyCode ToggleKeyCode = KeyCode.K;

        [SerializeField] private CSkillManager targetSkillManager;
        [SerializeField] private RectTransform skillWindowRootRectTransform;
        [SerializeField] private RectTransform skillListContentRootRectTransform;
        [SerializeField] private ScrollRect skillListScrollRect;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text skillPointText;
        [SerializeField] private CItemInventoryWindowDragHandle windowDragHandle;
        [SerializeField] private CSkillQuickSlotUI targetSkillQuickSlotUi;
        [SerializeField] private List<CSkillListSlotView> skillSlotViewComponentList = new List<CSkillListSlotView>();

        private readonly List<CSkillListSlotView> runtimeSlotViewList = new List<CSkillListSlotView>();

        private CSkillTooltipUI runtimeTooltipUi;
        private GameObject tooltipPrefabObject;
        private bool isSkillWindowVisible;

        ///<summary>
        /// 스킬 UI 초기 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureHeaderUi();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSkillManagerBinding();
            EnsureContentLayout();
            EnsureSlotViewComponentList();
            EnsureSlotViewList();
            RefreshSkillList();
            SetSkillWindowVisible( false );
        }

        ///<summary>
        /// 스킬 UI 활성 구독 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            EnsureHeaderUi();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            EnsureSkillManagerBinding();
            BringWindowToFront();

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 스킬 UI 비활성 정리
        ///</summary>
        private void OnDisable()
        {
            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }

            UnsubscribeSkillManager();
            HideSkillTooltip();
        }

        ///<summary>
        /// 스킬 UI 입력 처리
        ///</summary>
        private void Update()
        {
            EnsureSkillManagerBinding();

            if ( Input.GetKeyDown( ToggleKeyCode ) )
            {
                ToggleSkillWindow();
            }

            if ( isSkillWindowVisible == false )
            {
                return;
            }

            if ( Input.GetKeyDown( KeyCode.Escape ) )
            {
                CloseSkillWindow();
                return;
            }

            UpdateTooltipPosition();
        }

        ///<summary>
        /// 스킬 툴팁 표시 처리
        ///</summary>
        public void ShowSkillTooltip( string _skillId )
        {
            if ( targetSkillManager == null || string.IsNullOrWhiteSpace( _skillId ) )
            {
                HideSkillTooltip();
                return;
            }

            CSkillRuntimeData runtimeData = targetSkillManager.GetSkillRuntimeData( _skillId );

            if ( runtimeData == null )
            {
                HideSkillTooltip();
                return;
            }

            CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                HideSkillTooltip();
                return;
            }

            EnsureTooltipUi();

            if ( runtimeTooltipUi == null )
            {
                return;
            }

            bool isUnlocked = runtimeData.IsUnlocked();
            int currentLevel = runtimeData.GetSkillLevel();
            int displayCurrentLevel = isUnlocked ? Mathf.Max( 1, currentLevel ) : 1;
            int nextLevel = Mathf.Min( skillDefinition.GetMaxSkillLevel(), displayCurrentLevel + 1 );
            bool hasNextLevel = isUnlocked == false || currentLevel < skillDefinition.GetMaxSkillLevel();
            string currentLevelTitleValue = isUnlocked ? $"현재 레벨 Lv.{currentLevel}" : "습득 시 효과 Lv.1";
            string currentDescriptionValue = skillDefinition.GetFormattedDescription( displayCurrentLevel );
            string nextLevelTitleValue = hasNextLevel ? $"다음 레벨 Lv.{nextLevel}" : "다음 레벨";
            string nextDescriptionValue = hasNextLevel ? skillDefinition.GetFormattedDescription( nextLevel ) : "최대 레벨에 도달한 스킬입니다.";
            string infoTextValue = BuildTooltipInfoText( skillDefinition, currentLevel, isUnlocked );

            runtimeTooltipUi.SetTooltipContent( skillDefinition.GetSkillName(), infoTextValue, currentLevelTitleValue, currentDescriptionValue, nextLevelTitleValue, nextDescriptionValue, hasNextLevel );
            runtimeTooltipUi.transform.SetAsLastSibling();
            runtimeTooltipUi.SetVisible( true );
            UpdateTooltipPosition();
        }

        ///<summary>
        /// 스킬 툴팁 숨김 처리
        ///</summary>
        public void HideSkillTooltip()
        {
            if ( runtimeTooltipUi == null )
            {
                return;
            }

            runtimeTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 스킬 액션 처리
        ///</summary>
        public void TryProcessSkillAction( string _skillId )
        {
            if ( targetSkillManager == null || string.IsNullOrWhiteSpace( _skillId ) )
            {
                return;
            }

            bool isUnlocked = targetSkillManager.IsSkillUnlocked( _skillId );
            bool didProcess = false;

            if ( isUnlocked )
            {
                didProcess = targetSkillManager.TryLevelUpSkill( _skillId );
            }
            else
            {
                didProcess = targetSkillManager.TryLearnSkill( _skillId );
            }

            RefreshSkillList();

            if ( didProcess )
            {
                ShowSkillTooltip( _skillId );
            }
        }

        ///<summary>
        /// 스킬 목록 드래그 시작 처리
        ///</summary>
        public void TryBeginSkillDrag( CSkillListSlotView _slotView, PointerEventData _eventData )
        {
            EnsureSkillManagerBinding();
            ResolveQuickSlotUi();
            HideSkillTooltip();

            if ( _slotView == null || targetSkillQuickSlotUi == null || targetSkillManager == null )
            {
                return;
            }

            string skillId = _slotView.GetCurrentSkillId();

            if ( string.IsNullOrWhiteSpace( skillId ) )
            {
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( skillId );

            if ( skillDefinition == null || skillDefinition.GetSkillType() != eSkillType.ACTIVE )
            {
                return;
            }

            bool isUnlocked = targetSkillManager.IsSkillUnlocked( skillId );

            if ( isUnlocked == false )
            {
                return;
            }

            targetSkillQuickSlotUi.TryBeginDragFromSkillList( skillId, _eventData );
        }

        ///<summary>
        /// 스킬 목록 드래그 진행 처리
        ///</summary>
        public void UpdateSkillDrag( PointerEventData _eventData )
        {
            ResolveQuickSlotUi();

            if ( targetSkillQuickSlotUi == null )
            {
                return;
            }

            targetSkillQuickSlotUi.UpdateSkillDrag( _eventData );
        }

        ///<summary>
        /// 스킬 목록 드래그 종료 처리
        ///</summary>
        public void EndSkillDrag( PointerEventData _eventData )
        {
            ResolveQuickSlotUi();

            if ( targetSkillQuickSlotUi == null )
            {
                return;
            }

            targetSkillQuickSlotUi.EndSkillDrag( _eventData );
        }

        ///<summary>
        /// 슬롯 요약 문자 구성
        ///</summary>
/// <summary>
/// 스킬 슬롯 요약 문자열 구성
/// </summary>
public string BuildSkillSummaryText( CSkillDefinition _skillDefinition, int _skillLevel, bool _isUnlocked )
{
    if ( _skillDefinition == null )
    {
        return string.Empty;
    }

    if ( _isUnlocked == false )
    {
        int requiredLevel = _skillDefinition.GetRequiredLevel();
        string result = $"필요 레벨 Lv.{requiredLevel}";
        return result;
    }

    List<string> summaryLineList = new List<string>();
    string damageText = ResolveTokenText( _skillDefinition, _skillLevel, "damage" );
    string cooldownText = ResolveTokenText( _skillDefinition, _skillLevel, "cooldown" );
    string durationText = ResolveTokenText( _skillDefinition, _skillLevel, "duration" );
    string tickIntervalText = ResolveTokenText( _skillDefinition, _skillLevel, "tickInterval" );
    string attackReductionText = ResolveTokenText( _skillDefinition, _skillLevel, "atkReduction" );
    string defenseReductionText = ResolveTokenText( _skillDefinition, _skillLevel, "defReduction" );
    string debuffDurationText = ResolveTokenText( _skillDefinition, _skillLevel, "debuffDuration" );

    if ( string.IsNullOrWhiteSpace( damageText ) == false )
    {
        summaryLineList.Add( $"피해 {damageText}%" );
    }

    if ( string.IsNullOrWhiteSpace( cooldownText ) == false )
    {
        summaryLineList.Add( $"쿨타임 {cooldownText}초" );
    }

    if ( string.IsNullOrWhiteSpace( durationText ) == false )
    {
        summaryLineList.Add( $"지속시간 {durationText}초" );
    }

    if ( string.IsNullOrWhiteSpace( tickIntervalText ) == false )
    {
        summaryLineList.Add( $"틱간격 {tickIntervalText}초" );
    }

    if ( string.IsNullOrWhiteSpace( attackReductionText ) == false )
    {
        summaryLineList.Add( $"공격력 감소 {attackReductionText}" );
    }

    if ( string.IsNullOrWhiteSpace( defenseReductionText ) == false )
    {
        summaryLineList.Add( $"방어력 감소 {defenseReductionText}%" );
    }

    if ( string.IsNullOrWhiteSpace( debuffDurationText ) == false )
    {
        summaryLineList.Add( $"디버프 {debuffDurationText}초" );
    }

    if ( summaryLineList.Count == 0 )
    {
        string descriptionText = _skillDefinition.GetFormattedDescription( Mathf.Max( 1, _skillLevel ) );
        return descriptionText;
    }

    string resultValue = string.Join( "  |  ", summaryLineList );
    return resultValue;
}

        ///<summary>
        /// 슬롯 비용 문자 구성
        ///</summary>
        public string BuildSkillCostText( CSkillDefinition _skillDefinition, int _skillLevel, bool _isUnlocked )
        {
            if ( _skillDefinition == null )
            {
                return string.Empty;
            }

            if ( _isUnlocked == false )
            {
                string result = $"습득 SP {_skillDefinition.GetLearnSpCost()}";
                return result;
            }

            if ( _skillLevel >= _skillDefinition.GetMaxSkillLevel() )
            {
                return "최대 레벨";
            }

            string levelUpCostText = $"강화 SP {_skillDefinition.GetLevelUpSpCost()}";
            return levelUpCostText;
        }

        ///<summary>
        /// 스킬 상태 변경 반영
        ///</summary>
        private void HandleSkillStateChanged()
        {
            RefreshSkillList();
        }

        ///<summary>
        /// 닫기 버튼 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            CloseSkillWindow();
        }

        ///<summary>
        /// 스킬 창 토글 처리
        ///</summary>
        private void ToggleSkillWindow()
        {
            bool nextVisible = isSkillWindowVisible == false;
            SetSkillWindowVisible( nextVisible );

            if ( nextVisible )
            {
                RefreshSkillList();
                return;
            }

            HideSkillTooltip();
        }

        ///<summary>
        /// 스킬 창 닫기 처리
        ///</summary>
        private void CloseSkillWindow()
        {
            HideSkillTooltip();
            SetSkillWindowVisible( false );
        }

        ///<summary>
        /// 스킬 창 표시 상태 반영
        ///</summary>
        private void SetSkillWindowVisible( bool _isVisible )
        {
            isSkillWindowVisible = _isVisible;

            if ( skillWindowRootRectTransform == null )
            {
                return;
            }

            skillWindowRootRectTransform.gameObject.SetActive( _isVisible );

            if ( _isVisible )
            {
                BringWindowToFront();
                ResetScrollPosition();
            }
        }

        ///<summary>
        /// 스킬 목록 전체 갱신
        ///</summary>
        private void RefreshSkillList()
        {
            EnsureSkillManagerBinding();
            RefreshSkillPointText();

            if ( targetSkillManager == null )
            {
                HideAllSlotViews();
                return;
            }

            int requiredSkillCount = targetSkillManager.GetSkillCount();
            EnsureSlotViewCount( requiredSkillCount );
            EnsureSlotViewList();

            for ( int index = 0; index < runtimeSlotViewList.Count; index++ )
            {
                CSkillListSlotView slotView = runtimeSlotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                if ( index >= requiredSkillCount )
                {
                    slotView.GetSlotRootObject().SetActive( false );
                    continue;
                }

                CSkillRuntimeData runtimeData = targetSkillManager.GetSkillRuntimeData( index );
                string skillId = runtimeData != null && runtimeData.GetSkillDefinition() != null ? runtimeData.GetSkillDefinition().GetSkillId() : string.Empty;
                slotView.GetSlotRootObject().SetActive( true );
                slotView.Bind( this, targetSkillManager, skillId );
                slotView.RefreshView();
            }
        }

        ///<summary>
        /// 스킬 스크롤 위치 초기화
        ///</summary>
        private void ResetScrollPosition()
        {
            if ( skillListScrollRect == null )
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            skillListScrollRect.StopMovement();
            skillListScrollRect.verticalNormalizedPosition = 1.0f;
        }

        ///<summary>
        /// 스킬 포인트 문자 갱신
        ///</summary>
        private void RefreshSkillPointText()
        {
            if ( skillPointText == null )
            {
                return;
            }

            int currentSkillPoint = targetSkillManager != null ? targetSkillManager.GetCurrentSkillPoint() : 0;
            skillPointText.text = $"남은 SP {currentSkillPoint}";
        }

        ///<summary>
        /// 슬롯 숨김 처리
        ///</summary>
        private void HideAllSlotViews()
        {
            for ( int index = 0; index < runtimeSlotViewList.Count; index++ )
            {
                CSkillListSlotView slotView = runtimeSlotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.GetSlotRootObject().SetActive( false );
            }
        }

        ///<summary>
        /// 스킬 매니저 바인딩 보장
        ///</summary>
        private void EnsureSkillManagerBinding()
        {
            CSkillManager previousSkillManager = targetSkillManager;
            ResolveSkillManager();

            if ( previousSkillManager == targetSkillManager )
            {
                return;
            }

            if ( previousSkillManager != null )
            {
                previousSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
            }

            if ( targetSkillManager != null )
            {
                targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
                targetSkillManager.OnSkillStateChanged += HandleSkillStateChanged;
            }
        }

        ///<summary>
        /// 스킬 매니저 탐색 처리
        ///</summary>
        private void ResolveSkillManager()
        {
            if ( targetSkillManager != null && targetSkillManager.gameObject.activeInHierarchy )
            {
                return;
            }

            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            CSkillManager resolvedSkillManager = null;

            for ( int index = 0; index < playerControllerArray.Length; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null || playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                CSkillManager skillManager = playerController.GetComponent<CSkillManager>();

                if ( skillManager == null || skillManager.enabled == false )
                {
                    continue;
                }

                resolvedSkillManager = skillManager;
                break;
            }

            targetSkillManager = resolvedSkillManager;
        }

        ///<summary>
        /// 스킬 매니저 이벤트 해제
        ///</summary>
        private void UnsubscribeSkillManager()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
        }

        ///<summary>
        /// 슬롯 개수 보장
        ///</summary>
        private void EnsureSlotViewCount( int _requiredCount )
        {
            if ( skillListContentRootRectTransform == null )
            {
                return;
            }

            EnsureSlotViewComponentList();

            if ( skillSlotViewComponentList.Count == 0 )
            {
                return;
            }

            CSkillListSlotView templateSlotView = skillSlotViewComponentList[ 0 ];

            if ( templateSlotView == null )
            {
                return;
            }

            GameObject slotTemplateObject = templateSlotView.GetSlotRootObject();

            if ( slotTemplateObject == null )
            {
                return;
            }

            while ( skillSlotViewComponentList.Count < _requiredCount )
            {
                GameObject createdSlotObject = Instantiate( slotTemplateObject, skillListContentRootRectTransform );
                createdSlotObject.name = $"{SkillSlotCloneNamePrefix}_{skillSlotViewComponentList.Count + 1:00}";
                CSkillListSlotView createdSlotView = createdSlotObject.GetComponent<CSkillListSlotView>();

                if ( createdSlotView == null )
                {
                    createdSlotView = createdSlotObject.AddComponent<CSkillListSlotView>();
                }

                createdSlotView.AutoAssignReferences();
                skillSlotViewComponentList.Add( createdSlotView );
            }
        }

        ///<summary>
        /// 슬롯 컴포넌트 목록 보장
        ///</summary>
        private void EnsureSlotViewComponentList()
        {
            if ( skillListContentRootRectTransform == null )
            {
                return;
            }

            bool hasConfiguredSlot = false;

            for ( int index = 0; index < skillSlotViewComponentList.Count; index++ )
            {
                CSkillListSlotView slotView = skillSlotViewComponentList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.AutoAssignReferences();

                if ( slotView.IsValid() )
                {
                    hasConfiguredSlot = true;
                }
            }

            if ( hasConfiguredSlot )
            {
                return;
            }

            skillSlotViewComponentList.Clear();
            int childCount = skillListContentRootRectTransform.childCount;

            for ( int index = 0; index < childCount; index++ )
            {
                Transform childTransform = skillListContentRootRectTransform.GetChild( index );
                CSkillListSlotView slotView = childTransform.GetComponent<CSkillListSlotView>();

                if ( slotView == null )
                {
                    slotView = childTransform.gameObject.AddComponent<CSkillListSlotView>();
                }

                slotView.AutoAssignReferences();
                skillSlotViewComponentList.Add( slotView );
            }
        }

        ///<summary>
        /// 슬롯 캐시 목록 보장
        ///</summary>
        private void EnsureSlotViewList()
        {
            runtimeSlotViewList.Clear();
            EnsureSlotViewComponentList();

            for ( int index = 0; index < skillSlotViewComponentList.Count; index++ )
            {
                CSkillListSlotView slotView = skillSlotViewComponentList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                runtimeSlotViewList.Add( slotView );
            }
        }

        ///<summary>
        /// 헤더 UI 보정
        ///</summary>
        private void EnsureHeaderUi()
        {
            if ( titleText != null )
            {
                titleText.text = "SKILL";
            }
        }

        ///<summary>
        /// 창 드래그 핸들 보정
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            if ( windowDragHandle == null || skillWindowRootRectTransform == null || targetCanvas == null )
            {
                return;
            }

            windowDragHandle.Configure( skillWindowRootRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 클릭 최상단 정렬 핸들러 구성
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            RectTransform siblingTargetRectTransform = transform as RectTransform;

            if ( skillWindowRootRectTransform == null || siblingTargetRectTransform == null )
            {
                return;
            }

            Graphic[] graphicArray = skillWindowRootRectTransform.GetComponentsInChildren<Graphic>( true );

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
        /// 스크롤 콘텐츠 레이아웃 보정
        ///</summary>
        private void EnsureContentLayout()
        {
            if ( skillListContentRootRectTransform == null )
            {
                return;
            }

            VerticalLayoutGroup verticalLayoutGroup = skillListContentRootRectTransform.GetComponent<VerticalLayoutGroup>();

            if ( verticalLayoutGroup == null )
            {
                verticalLayoutGroup = skillListContentRootRectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            verticalLayoutGroup.spacing = 8.0f;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = false;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = skillListContentRootRectTransform.GetComponent<ContentSizeFitter>();

            if ( contentSizeFitter == null )
            {
                contentSizeFitter = skillListContentRootRectTransform.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        ///<summary>
        /// 툴팁 UI 생성 보장
        ///</summary>
        private void EnsureTooltipUi()
        {
            if ( tooltipPrefabObject == null )
            {
                tooltipPrefabObject = Resources.Load<GameObject>( SkillTooltipPrefabResourcePath );
            }

            if ( runtimeTooltipUi != null || tooltipPrefabObject == null || targetCanvas == null )
            {
                return;
            }

            GameObject createdTooltipObject = Instantiate( tooltipPrefabObject, targetCanvas.transform );
            createdTooltipObject.name = tooltipPrefabObject.name;
            runtimeTooltipUi = createdTooltipObject.GetComponent<CSkillTooltipUI>();
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
        /// 툴팁 정보 문자 구성
        ///</summary>
        private string BuildTooltipInfoText( CSkillDefinition _skillDefinition, int _currentLevel, bool _isUnlocked )
        {
            int displayLevel = _isUnlocked ? _currentLevel : 0;
            string result = $"레벨 Lv.{displayLevel}/{_skillDefinition.GetMaxSkillLevel()}  |  필요 레벨 Lv.{_skillDefinition.GetRequiredLevel()}\n습득 SP {_skillDefinition.GetLearnSpCost()}  |  강화 SP {_skillDefinition.GetLevelUpSpCost()}";
            return result;
        }

        ///<summary>
        /// 설명 토큰 문자 결정
        ///</summary>
        private string ResolveTokenText( CSkillDefinition _skillDefinition, int _skillLevel, string _tokenName )
        {
            bool isResolved = CSkillDescriptionTokenResolver.TryResolveTokenValue( _skillDefinition, Mathf.Max( 1, _skillLevel ), _tokenName, out string resolvedText );

            if ( isResolved == false )
            {
                return string.Empty;
            }

            return resolvedText;
        }

        ///<summary>
        /// 참조 컴포넌트 보정
        ///</summary>
        private void ResolveReferences()
        {
            if ( skillWindowRootRectTransform == null || string.Equals( skillWindowRootRectTransform.name, "SkillList", System.StringComparison.Ordinal ) )
            {
                Transform windowTransform = transform.Find( "SkillUI" );

                if ( windowTransform == null )
                {
                    windowTransform = transform.Find( "SkillUI/SkillList" );
                }

                skillWindowRootRectTransform = windowTransform != null ? windowTransform as RectTransform : null;
            }

            if ( skillListContentRootRectTransform == null )
            {
                Transform contentTransform = transform.Find( "SkillUI/SkillList/BG/Scroll View/Viewport/Content" );
                skillListContentRootRectTransform = contentTransform != null ? contentTransform as RectTransform : null;
            }

            if ( skillListScrollRect == null )
            {
                Transform scrollViewTransform = transform.Find( "SkillUI/SkillList/BG/Scroll View" );
                skillListScrollRect = scrollViewTransform != null ? scrollViewTransform.GetComponent<ScrollRect>() : null;
            }

            if ( closeButton == null )
            {
                Transform closeButtonTransform = transform.Find( "SkillUI/SkillList/ButtonClose" );
                closeButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<CButtonEx>() : null;
            }

            if ( titleText == null )
            {
                Transform titleTransform = transform.Find( "SkillUI/SkillList/BG/HeaderArea/TitleText" );
                titleText = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
            }

            if ( skillPointText == null )
            {
                Transform skillPointTransform = transform.Find( "SkillUI/SkillList/BG/HeaderArea/SkillPointText" );
                skillPointText = skillPointTransform != null ? skillPointTransform.GetComponent<TMP_Text>() : null;
            }

            if ( windowDragHandle == null )
            {
                Transform dragHandleTransform = transform.Find( "SkillUI/SkillList/BG/WindowDragHandle" );
                windowDragHandle = dragHandleTransform != null ? dragHandleTransform.GetComponent<CItemInventoryWindowDragHandle>() : null;
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            ResolveQuickSlotUi();
        }

        ///<summary>
        /// 퀵슬롯 UI 참조 결정
        ///</summary>
        private void ResolveQuickSlotUi()
        {
            if ( targetSkillQuickSlotUi != null )
            {
                return;
            }

            CSkillQuickSlotUI resolvedQuickSlotUi = FindFirstObjectByType<CSkillQuickSlotUI>();
            targetSkillQuickSlotUi = resolvedQuickSlotUi;
        }

        ///<summary>
        /// 스킬 창 최상단 정렬
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
        /// 창 최상위 RectTransform 결정
        ///</summary>
        private RectTransform ResolveTopLevelWindowRectTransform()
        {
            if ( skillWindowRootRectTransform == null )
            {
                return null;
            }

            RectTransform canvasRectTransform = targetCanvas != null ? targetCanvas.transform as RectTransform : null;
            RectTransform currentRectTransform = skillWindowRootRectTransform;

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
