using TMPro;
using TinyHero.Player;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.Core
{
    ///<summary>
    /// 테스트용 치트 입력 UI
    ///</summary>
    public sealed class CCheatCommandUI : MonoBehaviour
    {
        private const string RootObjectName = "CCheatCommandUI";
        private const string EventSystemObjectName = "EventSystem";
        private const float CanvasReferenceWidth = 1920.0f;
        private const float CanvasReferenceHeight = 1080.0f;
        private const float PanelWidth = 560.0f;
        private const float PanelHeight = 360.0f;
        private const float TitleHeight = 40.0f;
        private const float RowHeight = 44.0f;
        private const float ButtonWidth = 128.0f;
        private const float CloseButtonWidth = 96.0f;
        private const float LabelWidth = 120.0f;
        private const float ItemCountWidth = 120.0f;
        private const float StatusHeight = 72.0f;
        private const float PanelSpacing = 16.0f;
        private const int DefaultGrantedItemCount = 1;
        private const int MinGrantedItemCount = 1;

        private static CCheatCommandUI instance;

        private Canvas targetCanvas;
        private CanvasScaler targetCanvasScaler;
        private GraphicRaycaster targetGraphicRaycaster;
        private RectTransform dimmedBackgroundRectTransform;
        private RectTransform panelRectTransform;
        private TMP_InputField levelInputField;
        private TMP_InputField itemIdInputField;
        private TMP_InputField itemCountInputField;
        private TMP_Text statusText;
        private CButtonEx applyLevelButton;
        private CButtonEx grantItemButton;
        private CButtonEx closeButton;
        private TMP_FontAsset defaultFontAsset;
        private CPlayerStatManager targetStatManager;
        private CPlayerInventoryManager targetInventoryManager;
        private bool isVisible;

        ///<summary>
        /// 치트 UI 인스턴스 반환
        ///</summary>
        public static CCheatCommandUI GetOrCreate()
        {
            if ( Application.isPlaying == false )
            {
                return null;
            }

            if ( instance != null )
            {
                CCheatCommandUI cachedInstance = instance;
                return cachedInstance;
            }

            CCheatCommandUI foundInstance = FindFirstObjectByType<CCheatCommandUI>( FindObjectsInactive.Include );

            if ( foundInstance != null )
            {
                instance = foundInstance;
                return foundInstance;
            }

            GameObject rootObject = new GameObject( RootObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ), typeof( CCheatCommandUI ) );
            if ( Application.isPlaying )
            {
                Object.DontDestroyOnLoad( rootObject );
            }

            CCheatCommandUI createdInstance = rootObject.GetComponent<CCheatCommandUI>();
            instance = createdInstance;
            return createdInstance;
        }

        ///<summary>
        /// 치트 UI 표시 상태 반환
        ///</summary>
        public static bool IsAnyVisible()
        {
            if ( Application.isPlaying == false )
            {
                return false;
            }

            bool result = instance != null && instance.IsVisible();
            return result;
        }

        ///<summary>
        /// 치트 UI 초기 구성
        ///</summary>
        private void Awake()
        {
            if ( Application.isPlaying == false )
            {
                DestroyImmediate( gameObject );
                return;
            }

            if ( instance != null && instance != this )
            {
                Destroy( gameObject );
                return;
            }

            instance = this;
            if ( Application.isPlaying )
            {
                DontDestroyOnLoad( gameObject );
            }

            EnsureEventSystem();
            EnsureCanvasComponents();
            EnsureUiHierarchy();
            BindUiEvents();
            SetVisible( false );
        }

        ///<summary>
        /// 치트 UI 인스턴스 정리
        ///</summary>
        private void OnDestroy()
        {
            UnbindUiEvents();

            if ( instance == this )
            {
                instance = null;
            }
        }

        ///<summary>
        /// 치트 UI 토글 처리
        ///</summary>
        public void ToggleVisible()
        {
            bool nextVisible = isVisible == false;
            SetVisible( nextVisible );
        }

        ///<summary>
        /// 치트 UI 표시 상태 설정
        ///</summary>
        public void SetVisible( bool _isVisible )
        {
            EnsureCanvasComponents();
            EnsureUiHierarchy();
            ResolveTargets();
            isVisible = _isVisible;

            if ( dimmedBackgroundRectTransform != null )
            {
                dimmedBackgroundRectTransform.gameObject.SetActive( _isVisible );
            }

            if ( _isVisible )
            {
                SetStatusMessage( "치트 창 준비 완료" );

                if ( levelInputField != null )
                {
                    levelInputField.ActivateInputField();
                }
            }
        }

        ///<summary>
        /// 치트 UI 표시 상태 반환
        ///</summary>
        public bool IsVisible()
        {
            bool result = isVisible;
            return result;
        }

        ///<summary>
        /// 플레이어 레벨 변경 처리
        ///</summary>
        private void HandleApplyLevelButtonClicked()
        {
            ResolveTargets();

            if ( targetStatManager == null )
            {
                SetStatusMessage( "플레이어 스탯 매니저를 찾지 못했습니다." );
                return;
            }

            if ( levelInputField == null )
            {
                SetStatusMessage( "레벨 입력 필드가 없습니다." );
                return;
            }

            string levelText = levelInputField.text;
            bool isParsed = int.TryParse( levelText, out int parsedLevel );

            if ( isParsed == false )
            {
                SetStatusMessage( "유효한 레벨 숫자를 입력해 주세요." );
                return;
            }

            targetStatManager.SetCurrentLevel( parsedLevel );
            int resolvedLevel = targetStatManager.GetCurrentLevel();
            float levelStartExp = targetStatManager.GetLevelStartExp( resolvedLevel );
            targetStatManager.SetCurrentExp( levelStartExp );
            SetStatusMessage( $"레벨이 {resolvedLevel} 로 변경되었습니다." );
        }

        ///<summary>
        /// 아이템 지급 처리
        ///</summary>
        private void HandleGrantItemButtonClicked()
        {
            ResolveTargets();

            if ( targetInventoryManager == null )
            {
                SetStatusMessage( "플레이어 인벤토리 매니저를 찾지 못했습니다." );
                return;
            }

            if ( itemIdInputField == null )
            {
                SetStatusMessage( "아이템 ID 입력 필드가 없습니다." );
                return;
            }

            string itemId = itemIdInputField.text != null ? itemIdInputField.text.Trim() : string.Empty;

            if ( string.IsNullOrWhiteSpace( itemId ) )
            {
                SetStatusMessage( "아이템 ID 를 입력해 주세요." );
                return;
            }

            int grantCount = ResolveGrantedItemCount();
            bool didAddItem = targetInventoryManager.TryAddItemById( itemId, grantCount );

            if ( didAddItem == false )
            {
                SetStatusMessage( "아이템 지급에 실패했습니다. ID 또는 인벤토리 공간을 확인해 주세요." );
                return;
            }

            SetStatusMessage( $"{itemId} x{grantCount} 지급 완료" );
        }

        ///<summary>
        /// 치트 UI 닫기 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            SetVisible( false );
        }

        ///<summary>
        /// 플레이어 대상 참조 결정
        ///</summary>
        private void ResolveTargets()
        {
            if ( targetStatManager == null )
            {
                targetStatManager = FindFirstObjectByType<CPlayerStatManager>();
            }

            if ( targetInventoryManager == null )
            {
                targetInventoryManager = FindFirstObjectByType<CPlayerInventoryManager>();
            }
        }

        ///<summary>
        /// 지급 아이템 수량 결정
        ///</summary>
        private int ResolveGrantedItemCount()
        {
            if ( itemCountInputField == null )
            {
                return DefaultGrantedItemCount;
            }

            string itemCountText = itemCountInputField.text;
            bool isParsed = int.TryParse( itemCountText, out int parsedCount );

            if ( isParsed == false )
            {
                return DefaultGrantedItemCount;
            }

            int result = Mathf.Max( MinGrantedItemCount, parsedCount );
            return result;
        }

        ///<summary>
        /// 상태 메시지 반영
        ///</summary>
        private void SetStatusMessage( string _message )
        {
            if ( statusText == null )
            {
                return;
            }

            statusText.text = string.IsNullOrWhiteSpace( _message ) ? string.Empty : _message;
        }

        ///<summary>
        /// 이벤트 시스템 보장
        ///</summary>
        private void EnsureEventSystem()
        {
            EventSystem currentEventSystem = EventSystem.current;

            if ( currentEventSystem != null )
            {
                return;
            }

            GameObject eventSystemObject = new GameObject( EventSystemObjectName, typeof( EventSystem ), typeof( StandaloneInputModule ) );
            if ( Application.isPlaying )
            {
                DontDestroyOnLoad( eventSystemObject );
            }
        }

        ///<summary>
        /// 캔버스 컴포넌트 보장
        ///</summary>
        private void EnsureCanvasComponents()
        {
            if ( targetCanvas == null )
            {
                targetCanvas = GetComponent<Canvas>();
            }

            if ( targetCanvasScaler == null )
            {
                targetCanvasScaler = GetComponent<CanvasScaler>();
            }

            if ( targetGraphicRaycaster == null )
            {
                targetGraphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            if ( targetCanvas != null )
            {
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 5000;
            }

            if ( targetCanvasScaler != null )
            {
                targetCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                targetCanvasScaler.referenceResolution = new Vector2( CanvasReferenceWidth, CanvasReferenceHeight );
                targetCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                targetCanvasScaler.matchWidthOrHeight = 0.5f;
            }

            defaultFontAsset = TMP_Settings.defaultFontAsset;
        }

        ///<summary>
        /// 치트 UI 계층 보장
        ///</summary>
        private void EnsureUiHierarchy()
        {
            if ( dimmedBackgroundRectTransform != null && panelRectTransform != null )
            {
                return;
            }

            RectTransform canvasRectTransform = transform as RectTransform;

            if ( canvasRectTransform == null )
            {
                return;
            }

            canvasRectTransform.anchorMin = Vector2.zero;
            canvasRectTransform.anchorMax = Vector2.one;
            canvasRectTransform.offsetMin = Vector2.zero;
            canvasRectTransform.offsetMax = Vector2.zero;
            canvasRectTransform.anchoredPosition = Vector2.zero;
            canvasRectTransform.pivot = new Vector2( 0.5f, 0.5f );

            dimmedBackgroundRectTransform = CreateStretchRect( "DimmedBackground", canvasRectTransform );
            Image dimmedBackgroundImage = dimmedBackgroundRectTransform.gameObject.AddComponent<Image>();
            dimmedBackgroundImage.color = new Color( 0.0f, 0.0f, 0.0f, 0.7f );

            panelRectTransform = CreateAnchoredRect( "Panel", dimmedBackgroundRectTransform, new Vector2( PanelWidth, PanelHeight ) );
            Image panelImage = panelRectTransform.gameObject.AddComponent<Image>();
            panelImage.color = new Color( 0.12f, 0.12f, 0.16f, 0.96f );
            VerticalLayoutGroup panelLayoutGroup = panelRectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayoutGroup.padding = new RectOffset( 20, 20, 18, 18 );
            panelLayoutGroup.spacing = PanelSpacing;
            panelLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            panelLayoutGroup.childControlWidth = true;
            panelLayoutGroup.childControlHeight = false;
            panelLayoutGroup.childForceExpandWidth = true;
            panelLayoutGroup.childForceExpandHeight = false;

            RectTransform titleRowRectTransform = CreateLayoutRow( "TitleRow", panelRectTransform, TitleHeight, true );
            HorizontalLayoutGroup titleLayoutGroup = titleRowRectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLayoutGroup.spacing = 12.0f;
            titleLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            titleLayoutGroup.childControlWidth = false;
            titleLayoutGroup.childControlHeight = true;
            titleLayoutGroup.childForceExpandWidth = false;
            titleLayoutGroup.childForceExpandHeight = false;

            TMP_Text titleText = CreateText( "TitleText", titleRowRectTransform, "Cheat Command", 24.0f, TextAlignmentOptions.MidlineLeft, true, false );
            titleText.color = Color.white;
            AddFlexibleSpacer( titleRowRectTransform, "TitleSpacer" );
            closeButton = CreateButton( "CloseButton", titleRowRectTransform, "Close", CloseButtonWidth );

            RectTransform levelRowRectTransform = CreateLayoutRow( "LevelRow", panelRectTransform, RowHeight, false );
            HorizontalLayoutGroup levelLayoutGroup = ConfigureHorizontalRow( levelRowRectTransform );
            levelLayoutGroup.spacing = 12.0f;
            CreateLabelText( levelRowRectTransform, "LevelLabel", "Level" );
            levelInputField = CreateInputField( "LevelInput", levelRowRectTransform, "Target Level", TMP_InputField.ContentType.IntegerNumber, 0.0f );
            applyLevelButton = CreateButton( "ApplyLevelButton", levelRowRectTransform, "Apply", ButtonWidth );

            RectTransform itemIdRowRectTransform = CreateLayoutRow( "ItemIdRow", panelRectTransform, RowHeight, false );
            HorizontalLayoutGroup itemIdLayoutGroup = ConfigureHorizontalRow( itemIdRowRectTransform );
            itemIdLayoutGroup.spacing = 12.0f;
            CreateLabelText( itemIdRowRectTransform, "ItemLabel", "Item ID" );
            itemIdInputField = CreateInputField( "ItemIdInput", itemIdRowRectTransform, "Item Definition ID", TMP_InputField.ContentType.Standard, 0.0f );

            RectTransform itemGrantRowRectTransform = CreateLayoutRow( "ItemGrantRow", panelRectTransform, RowHeight, false );
            HorizontalLayoutGroup itemGrantLayoutGroup = ConfigureHorizontalRow( itemGrantRowRectTransform );
            itemGrantLayoutGroup.spacing = 12.0f;
            CreateLabelText( itemGrantRowRectTransform, "CountLabel", "Count" );
            itemCountInputField = CreateInputField( "ItemCountInput", itemGrantRowRectTransform, "1", TMP_InputField.ContentType.IntegerNumber, ItemCountWidth );
            itemCountInputField.text = DefaultGrantedItemCount.ToString();
            grantItemButton = CreateButton( "GrantItemButton", itemGrantRowRectTransform, "Grant Item", 160.0f );
            AddFlexibleSpacer( itemGrantRowRectTransform, "GrantSpacer" );

            RectTransform statusAreaRectTransform = CreateLayoutRow( "StatusArea", panelRectTransform, StatusHeight, true );
            Image statusBackgroundImage = statusAreaRectTransform.gameObject.AddComponent<Image>();
            statusBackgroundImage.color = new Color( 0.08f, 0.08f, 0.11f, 0.92f );
            LayoutElement statusLayoutElement = statusAreaRectTransform.GetComponent<LayoutElement>();
            statusLayoutElement.flexibleHeight = 1.0f;
            statusText = CreateText( "StatusText", statusAreaRectTransform, string.Empty, 18.0f, TextAlignmentOptions.TopLeft, true, true );
            statusText.color = new Color( 0.92f, 0.94f, 0.98f, 1.0f );
            statusText.margin = new Vector4( 12.0f, 10.0f, 12.0f, 10.0f );
        }

        ///<summary>
        /// 치트 UI 이벤트 연결
        ///</summary>
        private void BindUiEvents()
        {
            if ( applyLevelButton != null )
            {
                applyLevelButton.onClick.RemoveListener( HandleApplyLevelButtonClicked );
                applyLevelButton.onClick.AddListener( HandleApplyLevelButtonClicked );
            }

            if ( grantItemButton != null )
            {
                grantItemButton.onClick.RemoveListener( HandleGrantItemButtonClicked );
                grantItemButton.onClick.AddListener( HandleGrantItemButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 치트 UI 이벤트 해제
        ///</summary>
        private void UnbindUiEvents()
        {
            if ( applyLevelButton != null )
            {
                applyLevelButton.onClick.RemoveListener( HandleApplyLevelButtonClicked );
            }

            if ( grantItemButton != null )
            {
                grantItemButton.onClick.RemoveListener( HandleGrantItemButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 전체 스트레치 Rect 생성
        ///</summary>
        private RectTransform CreateStretchRect( string _objectName, RectTransform _parentRectTransform )
        {
            GameObject childObject = new GameObject( _objectName, typeof( RectTransform ) );
            RectTransform rectTransform = childObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.pivot = new Vector2( 0.5f, 0.5f );
            return rectTransform;
        }

        ///<summary>
        /// 중앙 고정 Rect 생성
        ///</summary>
        private RectTransform CreateAnchoredRect( string _objectName, RectTransform _parentRectTransform, Vector2 _size )
        {
            GameObject childObject = new GameObject( _objectName, typeof( RectTransform ) );
            RectTransform rectTransform = childObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            rectTransform.pivot = new Vector2( 0.5f, 0.5f );
            rectTransform.sizeDelta = _size;
            rectTransform.anchoredPosition = Vector2.zero;
            return rectTransform;
        }

        ///<summary>
        /// 레이아웃 행 생성
        ///</summary>
        private RectTransform CreateLayoutRow( string _objectName, RectTransform _parentRectTransform, float _preferredHeight, bool _stretchHeight )
        {
            GameObject rowObject = new GameObject( _objectName, typeof( RectTransform ), typeof( LayoutElement ) );
            RectTransform rectTransform = rowObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            rectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
            rectTransform.anchorMax = new Vector2( 1.0f, 1.0f );
            rectTransform.pivot = new Vector2( 0.5f, 0.5f );
            rectTransform.sizeDelta = new Vector2( 0.0f, _preferredHeight );
            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = _preferredHeight;
            layoutElement.minHeight = _preferredHeight;
            layoutElement.flexibleHeight = _stretchHeight ? 1.0f : 0.0f;
            return rectTransform;
        }

        ///<summary>
        /// 가로 레이아웃 행 구성
        ///</summary>
        private HorizontalLayoutGroup ConfigureHorizontalRow( RectTransform _rowRectTransform )
        {
            HorizontalLayoutGroup layoutGroup = _rowRectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;
            return layoutGroup;
        }

        ///<summary>
        /// 고정 폭 라벨 텍스트 생성
        ///</summary>
        private TMP_Text CreateLabelText( RectTransform _parentRectTransform, string _objectName, string _text )
        {
            TMP_Text labelText = CreateText( _objectName, _parentRectTransform, _text, 20.0f, TextAlignmentOptions.MidlineLeft, false, false );
            LayoutElement layoutElement = labelText.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = LabelWidth;
            layoutElement.minWidth = LabelWidth;
            layoutElement.preferredHeight = RowHeight;
            return labelText;
        }

        ///<summary>
        /// 텍스트 UI 생성
        ///</summary>
        private TMP_Text CreateText( string _objectName, RectTransform _parentRectTransform, string _text, float _fontSize, TextAlignmentOptions _alignment, bool _stretchWidth, bool _stretchHeight )
        {
            GameObject textObject = new GameObject( _objectName, typeof( RectTransform ) );
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();

            if ( defaultFontAsset != null )
            {
                textComponent.font = defaultFontAsset;
            }

            textComponent.text = _text;
            textComponent.fontSize = _fontSize;
            textComponent.alignment = _alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            textComponent.enableWordWrapping = true;

            LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = _stretchWidth ? 1.0f : 0.0f;
            layoutElement.flexibleHeight = _stretchHeight ? 1.0f : 0.0f;

            TMP_Text result = textComponent;
            return result;
        }

        ///<summary>
        /// 버튼 UI 생성
        ///</summary>
        private CButtonEx CreateButton( string _objectName, RectTransform _parentRectTransform, string _labelText, float _width )
        {
            GameObject buttonObject = new GameObject( _objectName, typeof( RectTransform ), typeof( Image ), typeof( CButtonEx ), typeof( LayoutElement ) );
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            Image backgroundImage = buttonObject.GetComponent<Image>();
            backgroundImage.color = new Color( 0.24f, 0.47f, 0.82f, 1.0f );
            CButtonEx button = buttonObject.GetComponent<CButtonEx>();
            button.targetGraphic = backgroundImage;
            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = _width;
            layoutElement.minWidth = _width;
            layoutElement.preferredHeight = RowHeight;
            layoutElement.minHeight = RowHeight;

            TMP_Text buttonLabel = CreateText( "Label", rectTransform, _labelText, 18.0f, TextAlignmentOptions.Center, true, true );
            buttonLabel.margin = Vector4.zero;

            return button;
        }

        ///<summary>
        /// 입력 필드 UI 생성
        ///</summary>
        private TMP_InputField CreateInputField( string _objectName, RectTransform _parentRectTransform, string _placeholderText, TMP_InputField.ContentType _contentType, float _preferredWidth )
        {
            GameObject inputObject = new GameObject( _objectName, typeof( RectTransform ), typeof( Image ), typeof( TMP_InputField ), typeof( LayoutElement ) );
            RectTransform rootRectTransform = inputObject.GetComponent<RectTransform>();
            rootRectTransform.SetParent( _parentRectTransform, false );
            Image backgroundImage = inputObject.GetComponent<Image>();
            backgroundImage.color = new Color( 0.95f, 0.95f, 0.98f, 1.0f );
            TMP_InputField inputField = inputObject.GetComponent<TMP_InputField>();
            inputField.contentType = _contentType;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            LayoutElement layoutElement = inputObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = _preferredWidth > 0.0f ? 0.0f : 1.0f;
            layoutElement.preferredWidth = _preferredWidth > 0.0f ? _preferredWidth : 0.0f;
            layoutElement.minHeight = RowHeight;
            layoutElement.preferredHeight = RowHeight;

            RectTransform viewportRectTransform = CreateStretchRect( "Viewport", rootRectTransform );
            viewportRectTransform.offsetMin = new Vector2( 12.0f, 8.0f );
            viewportRectTransform.offsetMax = new Vector2( -12.0f, -8.0f );
            inputField.textViewport = viewportRectTransform;

            TextMeshProUGUI textComponent = viewportRectTransform.gameObject.AddComponent<TextMeshProUGUI>();

            if ( defaultFontAsset != null )
            {
                textComponent.font = defaultFontAsset;
            }

            textComponent.fontSize = 18.0f;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.color = new Color( 0.1f, 0.1f, 0.1f, 1.0f );
            textComponent.enableWordWrapping = false;
            inputField.textComponent = textComponent;

            GameObject placeholderObject = new GameObject( "Placeholder", typeof( RectTransform ) );
            RectTransform placeholderRectTransform = placeholderObject.GetComponent<RectTransform>();
            placeholderRectTransform.SetParent( viewportRectTransform, false );
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.offsetMin = Vector2.zero;
            placeholderRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholderTextComponent = placeholderObject.AddComponent<TextMeshProUGUI>();

            if ( defaultFontAsset != null )
            {
                placeholderTextComponent.font = defaultFontAsset;
            }

            placeholderTextComponent.text = _placeholderText;
            placeholderTextComponent.fontSize = 18.0f;
            placeholderTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderTextComponent.color = new Color( 0.45f, 0.45f, 0.5f, 0.9f );
            placeholderTextComponent.raycastTarget = false;
            placeholderTextComponent.enableWordWrapping = false;
            inputField.placeholder = placeholderTextComponent;

            return inputField;
        }

        ///<summary>
        /// 가변 여백 오브젝트 생성
        ///</summary>
        private void AddFlexibleSpacer( RectTransform _parentRectTransform, string _objectName )
        {
            GameObject spacerObject = new GameObject( _objectName, typeof( RectTransform ), typeof( LayoutElement ) );
            RectTransform rectTransform = spacerObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parentRectTransform, false );
            LayoutElement layoutElement = spacerObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1.0f;
        }
    }
}
