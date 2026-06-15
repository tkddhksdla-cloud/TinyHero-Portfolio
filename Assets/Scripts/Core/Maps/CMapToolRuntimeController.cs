using System.Collections.Generic;
using System.IO;
using TMPro;
using TinyHero.Player;
using TinyHero.Tools;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴 런타임 제어 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class CMapToolRuntimeController : MonoBehaviour
    {
        private enum eMapToolMode
        {
            NONE,
            PLACE_PORTAL,
            PLACE_MONSTER
        }

        private const string BackgroundPrefabResourcePath = "Prefabs/BackgroundObject/BackgroundObject";
        private const string PlayerPrefabResourcePath = "Prefabs/Character/Player/PlayerObject";
        private const string PlayerObjectName = "PlayerObject";
        private const string PortalPrefabResourcePath = "Prefabs/Portal/PortalObject";
        private const string MonsterPrefabResourceFolderPath = "Prefabs/Character/Monster";
        private const string BackgroundSpriteResourceFolderPath = "RawImages/BG";
        private const string MapDataFolderPath = "Assets/Resources/MapData";
        private const string DefaultPortalTargetMapId = "SceneMap";
        private const string PortalIdPrefix = "Portal_";
        private const string ToolbarObjectName = "Toolbar";
        private const string BackgroundPanelObjectName = "BackgroundPanel";
        private const string MonsterPanelObjectName = "MonsterPanel";
        private const string PortalPanelObjectName = "PortalPanel";
        private const string PortalIdTitleObjectName = "PortalIdTitle";
        private const string PortalTargetMapTitleObjectName = "PortalTargetMapTitle";
        private const string PortalTargetPortalTitleObjectName = "PortalTargetPortalTitle";
        private const string MapIdTitleObjectName = "MapIdTitle";
        private const string MapNameTitleObjectName = "MapNameTitle";
        private const string MapInfoPanelObjectName = "MapInfoPanel";
        private const string LoadMapPanelObjectName = "LoadMapPanel";
        private const string ButtonObjectPrefix = "Button_";
        private const string LabelObjectName = "Label";
        private const string InputFieldTextObjectName = "Text";
        private const string InputFieldPlaceholderObjectName = "Placeholder";
        private const string InputFieldViewportObjectName = "Viewport";
        private const string ScrollViewportObjectName = "ScrollViewport";
        private const string ScrollRectObjectName = "ScrollRect";
        private const string ListRootObjectName = "ListRoot";
        private const float PreviewAlpha = 0.5f;
        private const float PanelWidth = 360.0f;
        private const float PanelHeight = 440.0f;
        private const float PanelTopOffset = -110.0f;
        private const float ToolbarTopOffset = -20.0f;
        private const float ToolbarSpacing = 16.0f;
        private const float ToolbarButtonWidth = 180.0f;
        private const float ToolbarButtonHeight = 56.0f;
        private const float ListButtonHeight = 44.0f;
        private const float PortalActionButtonHeight = 50.0f;
        private const float PortalFieldTitleFontSize = 18.0f;
        private const float PanelPadding = 14.0f;
        private const float ItemSpacing = 8.0f;
        private const float MapInfoPanelWidth = 360.0f;
        private const float MapInfoPanelHeight = 340.0f;
        private const float MapInfoInputWidth = 300.0f;
        private const float MapInfoButtonWidth = 142.0f;
        private const float MapInfoPanelLeftOffset = 20.0f;
        private const float MapInfoPanelTopOffset = -20.0f;
        private const float LoadPanelWidth = 360.0f;
        private const float LoadPanelHeight = 360.0f;
        private const float LoadPanelLeftOffset = 400.0f;
        private const float LoadPanelTopOffset = -200.0f;
        private const int MouseButtonLeft = 0;
        private const int MouseButtonRight = 1;
        private const int SortingOrderPanel = 10;

        [Header( "Map" )]
        [SerializeField] private string customMapId;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private WorldSpaceBackgroundFitter backgroundFitter;
        [SerializeField] private CMapToolBackgroundColliderVisualizer backgroundColliderVisualizer;
        [SerializeField] private Vector3 defaultPlayerSpawnPosition = Vector3.zero;

        [Header( "UI" )]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private EventSystem targetEventSystem;
        [SerializeField] private RectTransform toolbarRoot;
        [SerializeField] private RectTransform backgroundPanelRoot;
        [SerializeField] private RectTransform monsterPanelRoot;
        [SerializeField] private RectTransform portalPanelRoot;
        [SerializeField] private RectTransform mapInfoPanelRoot;
        [SerializeField] private RectTransform loadMapPanelRoot;
        [SerializeField] private RectTransform backgroundListRoot;
        [SerializeField] private RectTransform monsterListRoot;
        [SerializeField] private RectTransform loadMapListRoot;
        [SerializeField] private TMP_InputField mapIdInputField;
        [SerializeField] private TMP_InputField mapNameInputField;
        [SerializeField] private TMP_InputField portalIdInputField;
        [SerializeField] private TMP_InputField portalTargetMapIdInputField;
        [SerializeField] private TMP_InputField portalTargetPortalIdInputField;
        [SerializeField] private Toggle disableMonsterBehaviorToggle;
        [SerializeField] private CButtonEx backgroundModeButton;
        [SerializeField] private CButtonEx monsterModeButton;
        [SerializeField] private CButtonEx portalModeButton;
        [SerializeField] private CButtonEx clearObjectsButton;
        [SerializeField] private CButtonEx saveMapButton;
        [SerializeField] private CButtonEx loadMapButton;
        [SerializeField] private CButtonEx startPortalPlacementButton;

        private readonly List<MapToolPlacedObject> placedObjects = new List<MapToolPlacedObject>();
        private readonly List<Sprite> backgroundSprites = new List<Sprite>();
        private readonly List<GameObject> monsterPrefabs = new List<GameObject>();
        private readonly Dictionary<string, Sprite> backgroundSpriteByName = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private eMapToolMode currentMode;
        private GameObject previewInstance;
        private MapToolPlacedObject draggedPlacedObject;
        private Vector3 draggedObjectOffset;
        private string selectedMonsterResourcePath = string.Empty;
        private string selectedMonsterPrefabName = string.Empty;
        private string selectedPortalId = string.Empty;
        private string selectedPortalTargetMapId = DefaultPortalTargetMapId;
        private string selectedPortalTargetPortalId = string.Empty;
        private bool isMonsterBehaviorDisabledInMapTool;
        private bool isDraggingPlacedObject;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveSceneReferences();
            EnsureBackgroundObjectExists();
            EnsureBackgroundColliderVisualizerExists();
            EnsurePlayerObjectExists();
            EnsureUiRootExists();
            EnsureToolbarExists();
            EnsurePanelsExist();
            EnsureMapInfoPanelExists();
            EnsureLoadMapPanelExists();
        }

        ///<summary>
        /// 초기 상태 설정
        ///</summary>
        private void Start()
        {
            LoadResourceCatalog();
            InitializeInputFields();
            BindUiEvents();
            RebuildBackgroundPanel();
            RebuildMonsterPanel();
            RebuildLoadMapPanel();
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
        }

        ///<summary>
        /// 프레임 상태 처리
        ///</summary>
        private void Update()
        {
            HandleCancelInput();
            UpdatePreviewTransform();
            HandlePlacedObjectDragInput();
            HandlePlacementInput();
            HandleDeleteInput();
        }

        ///<summary>
        /// 씬 참조 결정
        ///</summary>
        private void ResolveSceneReferences()
        {
            if ( worldCamera == null )
            {
                Camera resolvedCamera = Camera.main;
                worldCamera = resolvedCamera;
            }

            if ( backgroundFitter == null )
            {
                WorldSpaceBackgroundFitter[] fitters = FindObjectsByType<WorldSpaceBackgroundFitter>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );

                if ( fitters.Length > 0 )
                {
                    WorldSpaceBackgroundFitter resolvedFitter = fitters[ 0 ];
                    backgroundFitter = resolvedFitter;
                }
            }

            if ( backgroundRenderer == null && backgroundFitter != null )
            {
                SpriteRenderer resolvedBackgroundRenderer = backgroundFitter.GetComponent<SpriteRenderer>();
                backgroundRenderer = resolvedBackgroundRenderer;
            }

            if ( backgroundColliderVisualizer == null && backgroundRenderer != null )
            {
                CMapToolBackgroundColliderVisualizer resolvedVisualizer = backgroundRenderer.GetComponent<CMapToolBackgroundColliderVisualizer>();
                backgroundColliderVisualizer = resolvedVisualizer;
            }
        }

        ///<summary>
        /// 배경 오브젝트 존재 보장
        ///</summary>
        private void EnsureBackgroundObjectExists()
        {
            if ( backgroundRenderer != null && backgroundFitter != null )
            {
                return;
            }

            GameObject backgroundPrefab = Resources.Load<GameObject>( BackgroundPrefabResourcePath );

            if ( backgroundPrefab == null )
            {
                return;
            }

            GameObject backgroundObject = Instantiate( backgroundPrefab );
            backgroundObject.name = backgroundPrefab.name;
            backgroundFitter = backgroundObject.GetComponent<WorldSpaceBackgroundFitter>();
            backgroundRenderer = backgroundObject.GetComponent<SpriteRenderer>();
        }

        ///<summary>
        /// 배경 콜라이더 시각화 존재 보장
        ///</summary>
        private void EnsureBackgroundColliderVisualizerExists()
        {
            if ( backgroundRenderer == null )
            {
                return;
            }

            if ( backgroundColliderVisualizer == null )
            {
                CMapToolBackgroundColliderVisualizer resolvedVisualizer = backgroundRenderer.GetComponent<CMapToolBackgroundColliderVisualizer>();

                if ( resolvedVisualizer == null )
                {
                    resolvedVisualizer = backgroundRenderer.gameObject.AddComponent<CMapToolBackgroundColliderVisualizer>();
                }

                backgroundColliderVisualizer = resolvedVisualizer;
            }

            backgroundColliderVisualizer.RefreshColliderVisual();
        }

        ///<summary>
        /// 플레이어 오브젝트 존재 보장
        ///</summary>
        private void EnsurePlayerObjectExists()
        {
            PlayerController existingPlayerController = FindFirstObjectByType<PlayerController>();

            if ( existingPlayerController != null )
            {
                return;
            }

            GameObject existingPlayerObject = GameObject.Find( PlayerObjectName );

            if ( existingPlayerObject != null )
            {
                return;
            }

            GameObject playerPrefab = Resources.Load<GameObject>( PlayerPrefabResourcePath );

            if ( playerPrefab == null )
            {
                return;
            }

            Vector3 spawnPosition = defaultPlayerSpawnPosition;
            GameObject playerObject = Instantiate( playerPrefab, spawnPosition, Quaternion.identity );
            playerObject.name = PlayerObjectName;
        }

        ///<summary>
        /// UI 루트 존재 보장
        ///</summary>
        private void EnsureUiRootExists()
        {
            if ( rootCanvas == null )
            {
                Canvas existingCanvas = FindFirstObjectByType<Canvas>();

                if ( existingCanvas != null )
                {
                    rootCanvas = existingCanvas;
                }
            }

            if ( rootCanvas == null )
            {
                GameObject canvasObject = new GameObject( "Canvas", typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
                Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                createdCanvas.sortingOrder = SortingOrderPanel;
                CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );
                rootCanvas = createdCanvas;
            }

            if ( targetEventSystem == null )
            {
                EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();

                if ( existingEventSystem != null )
                {
                    targetEventSystem = existingEventSystem;
                }
            }

            if ( targetEventSystem == null )
            {
                GameObject eventSystemObject = new GameObject( "EventSystem", typeof( EventSystem ), typeof( StandaloneInputModule ) );
                EventSystem createdEventSystem = eventSystemObject.GetComponent<EventSystem>();
                targetEventSystem = createdEventSystem;
            }
        }

        ///<summary>
        /// 툴바 존재 보장
        ///</summary>
        private void EnsureToolbarExists()
        {
            if ( toolbarRoot != null )
            {
                return;
            }

            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
            RectTransform toolbarRectTransform = FindChildRectTransform( canvasRectTransform, ToolbarObjectName );

            if ( toolbarRectTransform == null )
            {
                toolbarRectTransform = CreatePanelRoot( ToolbarObjectName, canvasRectTransform, new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ) );
                HorizontalLayoutGroup horizontalLayoutGroup = toolbarRectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayoutGroup.spacing = ToolbarSpacing;
                horizontalLayoutGroup.childControlWidth = false;
                horizontalLayoutGroup.childControlHeight = false;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.childForceExpandHeight = false;
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                ContentSizeFitter contentSizeFitter = toolbarRectTransform.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                toolbarRectTransform.anchoredPosition = new Vector2( 0.0f, ToolbarTopOffset );
            }

            toolbarRoot = toolbarRectTransform;

            if ( backgroundModeButton == null )
            {
                CButtonEx createdBackgroundButton = CreateTextButton( "BackgroundModeButton", toolbarRoot, "배경 변경", ToolbarButtonWidth, ToolbarButtonHeight );
                backgroundModeButton = createdBackgroundButton;
            }

            if ( portalModeButton == null )
            {
                CButtonEx createdPortalButton = CreateTextButton( "PortalModeButton", toolbarRoot, "포탈 배치", ToolbarButtonWidth, ToolbarButtonHeight );
                portalModeButton = createdPortalButton;
            }

            if ( monsterModeButton == null )
            {
                CButtonEx createdMonsterButton = CreateTextButton( "MonsterModeButton", toolbarRoot, "몬스터 배치", ToolbarButtonWidth, ToolbarButtonHeight );
                monsterModeButton = createdMonsterButton;
            }

            if ( clearObjectsButton == null )
            {
                CButtonEx createdClearObjectsButton = CreateTextButton( "ClearObjectsButton", toolbarRoot, "오브젝트 초기화", ToolbarButtonWidth, ToolbarButtonHeight );
                clearObjectsButton = createdClearObjectsButton;
            }
        }

        ///<summary>
        /// 패널 구성 보장
        ///</summary>
        private void EnsurePanelsExist()
        {
            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
            backgroundPanelRoot = EnsureSelectionPanel( backgroundPanelRoot, BackgroundPanelObjectName, canvasRectTransform, out backgroundListRoot );
            monsterPanelRoot = EnsureSelectionPanel( monsterPanelRoot, MonsterPanelObjectName, canvasRectTransform, out monsterListRoot );
            portalPanelRoot = EnsurePortalPanel( portalPanelRoot, canvasRectTransform );
        }

        ///<summary>
        /// 맵 정보 패널 존재 보장
        ///</summary>
        private void EnsureMapInfoPanelExists()
        {
            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();

            if ( mapInfoPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( canvasRectTransform, MapInfoPanelObjectName );
                mapInfoPanelRoot = foundPanelRoot;
            }

            if ( mapInfoPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreatePanelRoot( MapInfoPanelObjectName, canvasRectTransform, new Vector2( 0.0f, 1.0f ), new Vector2( 0.0f, 1.0f ), new Vector2( 0.0f, 1.0f ) );
                Image panelImage = createdPanelRoot.gameObject.AddComponent<Image>();
                panelImage.color = new Color( 0.12f, 0.14f, 0.18f, 0.92f );
                LayoutElement layoutElement = createdPanelRoot.gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                createdPanelRoot.sizeDelta = new Vector2( MapInfoPanelWidth, MapInfoPanelHeight );
                createdPanelRoot.anchoredPosition = new Vector2( MapInfoPanelLeftOffset, MapInfoPanelTopOffset );
                mapInfoPanelRoot = createdPanelRoot;
            }

            if ( mapIdInputField == null )
            {
                TMP_InputField createdMapIdInputField = CreateInputField( "MapIdInputField", mapInfoPanelRoot, "맵 ID", string.Empty );
                RectTransform mapIdRectTransform = createdMapIdInputField.GetComponent<RectTransform>();
                mapIdRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                mapIdRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                mapIdRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                mapIdRectTransform.sizeDelta = new Vector2( MapInfoInputWidth, 54.0f );
                mapIdRectTransform.anchoredPosition = new Vector2( PanelPadding, -52.0f );
                mapIdInputField = createdMapIdInputField;
            }

            EnsureInputFieldTitle( MapIdTitleObjectName, mapInfoPanelRoot, "Map ID", new Vector2( 0.0f, -12.0f ) );
            UpdateInputFieldPlaceholder( mapIdInputField, "맵 ID" );

            if ( mapNameInputField == null )
            {
                TMP_InputField createdMapNameInputField = CreateInputField( "MapNameInputField", mapInfoPanelRoot, "맵 이름", string.Empty );
                RectTransform mapNameRectTransform = createdMapNameInputField.GetComponent<RectTransform>();
                mapNameRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                mapNameRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                mapNameRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                mapNameRectTransform.sizeDelta = new Vector2( MapInfoInputWidth, 54.0f );
                mapNameRectTransform.anchoredPosition = new Vector2( PanelPadding, -158.0f );
                mapNameInputField = createdMapNameInputField;
            }

            EnsureInputFieldTitle( MapNameTitleObjectName, mapInfoPanelRoot, "Map Name", new Vector2( 0.0f, -126.0f ) );
            UpdateInputFieldPlaceholder( mapNameInputField, "맵 이름" );

            if ( saveMapButton == null )
            {
                CButtonEx createdSaveMapButton = CreateTextButton( "SaveMapButton", mapInfoPanelRoot, "맵 저장", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform saveButtonRectTransform = createdSaveMapButton.GetComponent<RectTransform>();
                saveButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchoredPosition = new Vector2( PanelPadding, -236.0f );
                saveMapButton = createdSaveMapButton;
            }

            if ( loadMapButton == null )
            {
                CButtonEx createdLoadMapButton = CreateTextButton( "LoadMapButton", mapInfoPanelRoot, "맵 불러오기", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform loadButtonRectTransform = createdLoadMapButton.GetComponent<RectTransform>();
                loadButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchoredPosition = new Vector2( PanelPadding + MapInfoButtonWidth + 12.0f, -236.0f );
                loadMapButton = createdLoadMapButton;
            }

            if ( disableMonsterBehaviorToggle == null )
            {
                Toggle createdDisableMonsterBehaviorToggle = CreateToggle( "DisableMonsterBehaviorToggle", mapInfoPanelRoot, "몬스터 행동 정지" );
                RectTransform toggleRectTransform = createdDisableMonsterBehaviorToggle.GetComponent<RectTransform>();
                toggleRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchoredPosition = new Vector2( PanelPadding, -300.0f );
                disableMonsterBehaviorToggle = createdDisableMonsterBehaviorToggle;
            }

            disableMonsterBehaviorToggle.isOn = isMonsterBehaviorDisabledInMapTool;
            UpdateButtonLabel( saveMapButton, "맵 저장" );
            UpdateButtonLabel( loadMapButton, "맵 불러오기" );
        }

        ///<summary>
        /// 로드 맵 패널 존재 보장
        ///</summary>
        private void EnsureLoadMapPanelExists()
        {
            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();

            if ( loadMapPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( canvasRectTransform, LoadMapPanelObjectName );
                loadMapPanelRoot = foundPanelRoot;
            }

            if ( loadMapPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreatePanelRoot( LoadMapPanelObjectName, canvasRectTransform, new Vector2( 0.0f, 1.0f ), new Vector2( 0.0f, 1.0f ), new Vector2( 0.0f, 1.0f ) );
                Image panelImage = createdPanelRoot.gameObject.AddComponent<Image>();
                panelImage.color = new Color( 0.12f, 0.14f, 0.18f, 0.92f );
                LayoutElement layoutElement = createdPanelRoot.gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                createdPanelRoot.sizeDelta = new Vector2( LoadPanelWidth, LoadPanelHeight );
                createdPanelRoot.anchoredPosition = new Vector2( LoadPanelLeftOffset, LoadPanelTopOffset );
                loadMapPanelRoot = createdPanelRoot;
            }

            if ( loadMapListRoot == null )
            {
                RectTransform foundListRoot = EnsureScrollableListRoot( loadMapPanelRoot );
                loadMapListRoot = foundListRoot;
            }
        }

        ///<summary>
        /// 입력 필드 초기화
        ///</summary>
        private void InitializeInputFields()
        {
            if ( mapIdInputField != null && string.IsNullOrWhiteSpace( mapIdInputField.text ) )
            {
                string initialMapId = ResolveInitialMapId();
                mapIdInputField.text = initialMapId;
            }

            if ( mapNameInputField != null && string.IsNullOrWhiteSpace( mapNameInputField.text ) )
            {
                string initialMapName = ResolveInitialMapName();
                mapNameInputField.text = initialMapName;
            }

            if ( portalIdInputField != null && string.IsNullOrWhiteSpace( portalIdInputField.text ) )
            {
                string generatedPortalId = GenerateNextPortalId();
                portalIdInputField.text = generatedPortalId;
            }

            if ( portalTargetMapIdInputField != null && string.IsNullOrWhiteSpace( portalTargetMapIdInputField.text ) )
            {
                portalTargetMapIdInputField.text = DefaultPortalTargetMapId;
            }

            if ( portalTargetPortalIdInputField != null && string.IsNullOrWhiteSpace( portalTargetPortalIdInputField.text ) )
            {
                portalTargetPortalIdInputField.text = string.Empty;
            }

            if ( disableMonsterBehaviorToggle != null )
            {
                disableMonsterBehaviorToggle.isOn = isMonsterBehaviorDisabledInMapTool;
            }
        }

        ///<summary>
        /// UI 이벤트 연결
        ///</summary>
        private void BindUiEvents()
        {
            backgroundModeButton.onClick.RemoveAllListeners();
            backgroundModeButton.onClick.AddListener( OnBackgroundModeButtonClicked );
            monsterModeButton.onClick.RemoveAllListeners();
            monsterModeButton.onClick.AddListener( OnMonsterModeButtonClicked );
            portalModeButton.onClick.RemoveAllListeners();
            portalModeButton.onClick.AddListener( OnPortalModeButtonClicked );
            clearObjectsButton.onClick.RemoveAllListeners();
            clearObjectsButton.onClick.AddListener( OnClearObjectsButtonClicked );
            startPortalPlacementButton.onClick.RemoveAllListeners();
            startPortalPlacementButton.onClick.AddListener( OnStartPortalPlacementButtonClicked );
            saveMapButton.onClick.RemoveAllListeners();
            saveMapButton.onClick.AddListener( OnSaveMapButtonClicked );
            loadMapButton.onClick.RemoveAllListeners();
            loadMapButton.onClick.AddListener( OnLoadMapButtonClicked );

            if ( disableMonsterBehaviorToggle != null )
            {
                disableMonsterBehaviorToggle.onValueChanged.RemoveAllListeners();
                disableMonsterBehaviorToggle.onValueChanged.AddListener( OnDisableMonsterBehaviorToggleValueChanged );
            }
        }

        ///<summary>
        /// 리소스 목록 로드
        ///</summary>
        private void LoadResourceCatalog()
        {
            backgroundSprites.Clear();
            monsterPrefabs.Clear();
            backgroundSpriteByName.Clear();
            monsterPrefabByName.Clear();

            Sprite[] loadedBackgroundSprites = Resources.LoadAll<Sprite>( BackgroundSpriteResourceFolderPath );
            int backgroundSpriteCount = loadedBackgroundSprites.Length;

            for ( int index = 0; index < backgroundSpriteCount; index++ )
            {
                Sprite backgroundSprite = loadedBackgroundSprites[ index ];

                if ( backgroundSprite == null )
                {
                    continue;
                }

                backgroundSprites.Add( backgroundSprite );
                backgroundSpriteByName[ backgroundSprite.name ] = backgroundSprite;
            }

            GameObject[] loadedMonsterPrefabs = Resources.LoadAll<GameObject>( MonsterPrefabResourceFolderPath );
            int monsterPrefabCount = loadedMonsterPrefabs.Length;

            for ( int index = 0; index < monsterPrefabCount; index++ )
            {
                GameObject monsterPrefab = loadedMonsterPrefabs[ index ];

                if ( monsterPrefab == null )
                {
                    continue;
                }

                monsterPrefabs.Add( monsterPrefab );
                monsterPrefabByName[ monsterPrefab.name ] = monsterPrefab;
            }
        }

        ///<summary>
        /// 배경 패널 재구성
        ///</summary>
        private void RebuildBackgroundPanel()
        {
            ClearChildren( backgroundListRoot );
            int backgroundCount = backgroundSprites.Count;

            for ( int index = 0; index < backgroundCount; index++ )
            {
                Sprite backgroundSprite = backgroundSprites[ index ];
                string spriteName = backgroundSprite.name;
                CButtonEx listButton = CreateTextButton( ButtonObjectPrefix + spriteName, backgroundListRoot, spriteName, PanelWidth - ( PanelPadding * 2.0f ), ListButtonHeight );
                listButton.onClick.AddListener( delegate
                {
                    ApplyBackgroundSpriteByName( spriteName );
                } );
            }
        }

        ///<summary>
        /// 몬스터 패널 재구성
        ///</summary>
        private void RebuildMonsterPanel()
        {
            ClearChildren( monsterListRoot );
            int monsterCount = monsterPrefabs.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                GameObject monsterPrefab = monsterPrefabs[ index ];
                string prefabName = monsterPrefab.name;
                string resourcePath = MonsterPrefabResourceFolderPath + "/" + prefabName;
                CButtonEx listButton = CreateTextButton( ButtonObjectPrefix + prefabName, monsterListRoot, prefabName, PanelWidth - ( PanelPadding * 2.0f ), ListButtonHeight );
                listButton.onClick.AddListener( delegate
                {
                    BeginMonsterPlacement( prefabName, resourcePath );
                } );
            }
        }

        ///<summary>
        /// 로드 맵 패널 재구성
        ///</summary>
        private void RebuildLoadMapPanel()
        {
            ClearChildren( loadMapListRoot );
            List<string> savedMapIds = GetSavedMapIdList();
            int savedMapCount = savedMapIds.Count;

            for ( int index = 0; index < savedMapCount; index++ )
            {
                string mapId = savedMapIds[ index ];
                CButtonEx listButton = CreateTextButton( ButtonObjectPrefix + mapId, loadMapListRoot, mapId, LoadPanelWidth - ( PanelPadding * 2.0f ), ListButtonHeight );
                listButton.onClick.AddListener( delegate
                {
                    LoadSelectedMap( mapId );
                } );
            }
        }

        ///<summary>
        /// 저장된 맵 ID 목록 반환
        ///</summary>
        private List<string> GetSavedMapIdList()
        {
            List<string> savedMapIds = new List<string>();

            if ( Directory.Exists( MapDataFolderPath ) == false )
            {
                return savedMapIds;
            }

            string[] filePaths = Directory.GetFiles( MapDataFolderPath, "*.json" );
            int fileCount = filePaths.Length;

            for ( int index = 0; index < fileCount; index++ )
            {
                string filePath = filePaths[ index ];
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension( filePath );

                if ( string.IsNullOrWhiteSpace( fileNameWithoutExtension ) )
                {
                    continue;
                }

                savedMapIds.Add( fileNameWithoutExtension );
            }

            savedMapIds.Sort();
            return savedMapIds;
        }

        ///<summary>
        /// 선택 맵 로드
        ///</summary>
        private void LoadSelectedMap(string _mapId)
        {
            if ( mapIdInputField != null )
            {
                mapIdInputField.text = _mapId;
            }

            SetPanelVisible( loadMapPanelRoot, false );
            LoadSavedMapData();
        }

        ///<summary>
        /// 저장 맵 버튼 클릭 처리
        ///</summary>
        private void OnSaveMapButtonClicked()
        {
            SaveMapData();
            RebuildLoadMapPanel();
        }

        ///<summary>
        /// 로드 맵 버튼 클릭 처리
        ///</summary>
        private void OnLoadMapButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            RebuildLoadMapPanel();
            ToggleLoadMapPanel();
        }

        ///<summary>
        /// 몬스터 행동 정지 토글 값 변경 처리
        ///</summary>
        private void OnDisableMonsterBehaviorToggleValueChanged( bool _isOn )
        {
            isMonsterBehaviorDisabledInMapTool = _isOn;
            ApplyMonsterBehaviorToggleToAllMonsters();
        }

        ///<summary>
        /// 오브젝트 초기화 버튼 클릭 처리
        ///</summary>
        private void OnClearObjectsButtonClicked()
        {
            ClearMonsterAndPortalObjects();
        }

        ///<summary>
        /// 로드 맵 패널 전환
        ///</summary>
        private void ToggleLoadMapPanel()
        {
            bool shouldActivate = loadMapPanelRoot.gameObject.activeSelf == false;
            SetPanelVisible( loadMapPanelRoot, shouldActivate );
        }

        ///<summary>
        /// 저장된 맵 데이터 로드
        ///</summary>
        private void LoadSavedMapData()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            string saveFilePath = GetSaveFilePath();

            if ( File.Exists( saveFilePath ) == false )
            {
                return;
            }

            string jsonText = File.ReadAllText( saveFilePath );

            if ( string.IsNullOrWhiteSpace( jsonText ) )
            {
                return;
            }

            CMapToolSaveData loadedData = JsonUtility.FromJson<CMapToolSaveData>( jsonText );

            if ( loadedData == null )
            {
                return;
            }

            ApplyLoadedData( loadedData );
        }

        ///<summary>
        /// 로드된 데이터 적용
        ///</summary>
        private void ApplyLoadedData(CMapToolSaveData _loadedData)
        {
            ClearPlacedObjects();

            if ( _loadedData.portals == null )
            {
                _loadedData.portals = new List<CMapToolPortalSaveData>();
            }

            if ( _loadedData.monsters == null )
            {
                _loadedData.monsters = new List<CMapToolMonsterSaveData>();
            }

            if ( string.IsNullOrWhiteSpace( _loadedData.mapId ) == false && mapIdInputField != null )
            {
                mapIdInputField.text = _loadedData.mapId;
            }

            if ( mapNameInputField != null )
            {
                string resolvedMapName = string.IsNullOrWhiteSpace( _loadedData.mapName ) ? _loadedData.mapId : _loadedData.mapName;
                mapNameInputField.text = resolvedMapName;
            }

            if ( string.IsNullOrEmpty( _loadedData.backgroundSpriteName ) == false )
            {
                ApplyBackgroundSpriteByName( _loadedData.backgroundSpriteName, false );
            }

            int portalCount = _loadedData.portals.Count;

            for ( int index = 0; index < portalCount; index++ )
            {
                CMapToolPortalSaveData portalSaveData = _loadedData.portals[ index ];
                SpawnSavedPortal( portalSaveData );
            }

            int monsterCount = _loadedData.monsters.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = _loadedData.monsters[ index ];
                SpawnSavedMonster( monsterSaveData );
            }

            RefreshPortalPlacementInputFields();
        }

        ///<summary>
        /// 저장된 포탈 생성
        ///</summary>
        private void SpawnSavedPortal(CMapToolPortalSaveData _portalSaveData)
        {
            GameObject portalPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );

            if ( portalPrefab == null || _portalSaveData == null )
            {
                return;
            }

            CMapToolTransformData transformData = _portalSaveData.transform;

            if ( transformData == null )
            {
                transformData = BuildTransformData( portalPrefab.transform );
            }

            Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
            Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
            Vector3 spawnScale = CreateVector3FromTransformData( transformData.scale, portalPrefab.transform.localScale );
            GameObject portalInstance = Instantiate( portalPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
            portalInstance.transform.localScale = spawnScale;
            portalInstance.name = portalPrefab.name;
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( portalInstance );
            string resolvedPortalId = ResolvePortalIdForLoad( _portalSaveData );
            placedObject.SetupPortal( _portalSaveData.prefabName, PortalPrefabResourcePath, resolvedPortalId, _portalSaveData.targetMapId, _portalSaveData.targetPortalId );
            ApplyPortalLinkData( portalInstance, resolvedPortalId, _portalSaveData.targetMapId, _portalSaveData.targetPortalId );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 저장된 몬스터 생성
        ///</summary>
        private void SpawnSavedMonster(CMapToolMonsterSaveData _monsterSaveData)
        {
            if ( _monsterSaveData == null || string.IsNullOrEmpty( _monsterSaveData.prefabName ) )
            {
                return;
            }

            GameObject monsterPrefab = ResolveMonsterPrefab( _monsterSaveData.prefabName, _monsterSaveData.resourcePath );

            if ( monsterPrefab == null )
            {
                return;
            }

            CMapToolTransformData transformData = _monsterSaveData.transform;

            if ( transformData == null )
            {
                transformData = BuildTransformData( monsterPrefab.transform );
            }

            Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
            Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
            Vector3 spawnScale = ResolveMonsterSpawnScale( monsterPrefab );
            GameObject monsterInstance = Instantiate( monsterPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
            monsterInstance.transform.localScale = spawnScale;
            monsterInstance.name = monsterPrefab.name;
            MonsterObject spawnedMonsterObject = monsterInstance.GetComponent<MonsterObject>();

            if ( spawnedMonsterObject != null )
            {
                spawnedMonsterObject.ConfigureMonster( monsterPrefab.name, monsterPrefab.name );
                spawnedMonsterObject.SetBehaviorEnabled( isMonsterBehaviorDisabledInMapTool == false );

                if ( CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
                {
                    monsterInfoManager.RegisterMonster( spawnedMonsterObject );
                }
            }

            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( monsterInstance );
            placedObject.SetupMonster( _monsterSaveData.prefabName, _monsterSaveData.resourcePath );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 맵 데이터 저장
        ///</summary>
        private void SaveMapData()
        {
            string saveFilePath = GetSaveFilePath();
            string saveDirectoryPath = Path.GetDirectoryName( saveFilePath );

            if ( Directory.Exists( saveDirectoryPath ) == false )
            {
                Directory.CreateDirectory( saveDirectoryPath );
            }

            CMapToolSaveData saveData = BuildSaveData();
            string jsonText = JsonUtility.ToJson( saveData, true );
            File.WriteAllText( saveFilePath, jsonText );

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        ///<summary>
        /// 저장 데이터 구성
        ///</summary>
        private CMapToolSaveData BuildSaveData()
        {
            CMapToolSaveData saveData = new CMapToolSaveData();
            saveData.mapId = ResolveMapId();
            saveData.mapName = ResolveMapName();

            if ( backgroundRenderer != null && backgroundRenderer.sprite != null )
            {
                saveData.backgroundSpriteName = backgroundRenderer.sprite.name;
            }

            int placedObjectCount = placedObjects.Count;

            for ( int index = placedObjectCount - 1; index >= 0; index-- )
            {
                MapToolPlacedObject placedObject = placedObjects[ index ];

                if ( placedObject == null )
                {
                    placedObjects.RemoveAt( index );
                    continue;
                }

                if ( placedObject.GetPlacedObjectType() == MapToolPlacedObject.eMapToolPlacedObjectType.PORTAL )
                {
                    CMapToolPortalSaveData portalSaveData = BuildPortalSaveData( placedObject );
                    saveData.portals.Add( portalSaveData );
                    continue;
                }

                CMapToolMonsterSaveData monsterSaveData = BuildMonsterSaveData( placedObject );
                saveData.monsters.Add( monsterSaveData );
            }

            return saveData;
        }

        ///<summary>
        /// 포탈 저장 데이터 구성
        ///</summary>
        private CMapToolPortalSaveData BuildPortalSaveData(MapToolPlacedObject _placedObject)
        {
            CMapToolPortalSaveData portalSaveData = new CMapToolPortalSaveData();
            portalSaveData.prefabName = _placedObject.GetPrefabName();
            portalSaveData.resourcePath = _placedObject.GetResourcePath();
            portalSaveData.portalId = _placedObject.GetPortalId();
            portalSaveData.targetMapId = _placedObject.GetTargetMapId();
            portalSaveData.targetPortalId = _placedObject.GetTargetPortalId();
            portalSaveData.transform = BuildTransformData( _placedObject.transform );
            return portalSaveData;
        }

        ///<summary>
        /// 몬스터 저장 데이터 구성
        ///</summary>
        private CMapToolMonsterSaveData BuildMonsterSaveData(MapToolPlacedObject _placedObject)
        {
            CMapToolMonsterSaveData monsterSaveData = new CMapToolMonsterSaveData();
            monsterSaveData.prefabName = _placedObject.GetPrefabName();
            monsterSaveData.resourcePath = _placedObject.GetResourcePath();
            monsterSaveData.transform = BuildTransformData( _placedObject.transform );
            monsterSaveData.transform.scale = null;
            return monsterSaveData;
        }

        ///<summary>
        /// 트랜스폼 데이터 구성
        ///</summary>
        private CMapToolTransformData BuildTransformData(Transform _targetTransform)
        {
            CMapToolTransformData transformData = new CMapToolTransformData();
            Vector3 position = _targetTransform.position;
            Vector3 rotation = _targetTransform.eulerAngles;
            Vector3 scale = _targetTransform.localScale;
            transformData.position[ 0 ] = position.x;
            transformData.position[ 1 ] = position.y;
            transformData.position[ 2 ] = position.z;
            transformData.rotation[ 0 ] = rotation.x;
            transformData.rotation[ 1 ] = rotation.y;
            transformData.rotation[ 2 ] = rotation.z;
            transformData.scale[ 0 ] = scale.x;
            transformData.scale[ 1 ] = scale.y;
            transformData.scale[ 2 ] = scale.z;
            return transformData;
        }

        ///<summary>
        /// 몬스터 생성 스케일 결정
        ///</summary>
        private Vector3 ResolveMonsterSpawnScale(GameObject _monsterPrefab)
        {
            if ( _monsterPrefab == null )
            {
                return Vector3.one;
            }

            Vector3 result = _monsterPrefab.transform.localScale;
            return result;
        }

        ///<summary>
        /// 로드용 포탈 ID 결정
        ///</summary>
        private string ResolvePortalIdForLoad(CMapToolPortalSaveData _portalSaveData)
        {
            if ( _portalSaveData == null )
            {
                string generatedPortalId = GenerateNextPortalId();
                return generatedPortalId;
            }

            if ( string.IsNullOrWhiteSpace( _portalSaveData.portalId ) == false )
            {
                string trimmedPortalId = _portalSaveData.portalId.Trim();
                return trimmedPortalId;
            }

            string fallbackPortalId = GenerateNextPortalId();
            return fallbackPortalId;
        }

        ///<summary>
        /// 다음 포탈 ID 생성
        ///</summary>
        private string GenerateNextPortalId()
        {
            int nextPortalIndex = 1;
            int placedObjectCount = placedObjects.Count;

            for ( int index = 0; index < placedObjectCount; index++ )
            {
                MapToolPlacedObject placedObject = placedObjects[ index ];

                if ( placedObject == null )
                {
                    continue;
                }

                if ( placedObject.GetPlacedObjectType() != MapToolPlacedObject.eMapToolPlacedObjectType.PORTAL )
                {
                    continue;
                }

                string existingPortalId = placedObject.GetPortalId();

                if ( string.IsNullOrWhiteSpace( existingPortalId ) )
                {
                    continue;
                }

                if ( existingPortalId.StartsWith( PortalIdPrefix, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                string numericSuffix = existingPortalId.Substring( PortalIdPrefix.Length );

                if ( int.TryParse( numericSuffix, out int parsedIndex ) == false )
                {
                    continue;
                }

                if ( parsedIndex >= nextPortalIndex )
                {
                    nextPortalIndex = parsedIndex + 1;
                }
            }

            string generatedPortalId = PortalIdPrefix + nextPortalIndex.ToString( "000" );
            return generatedPortalId;
        }

        ///<summary>
        /// 포탈 배치 입력 필드 갱신
        ///</summary>
        private void RefreshPortalPlacementInputFields()
        {
            string nextPortalId = GenerateNextPortalId();
            string currentMapId = ResolveMapId();
            selectedPortalId = nextPortalId;
            selectedPortalTargetMapId = currentMapId;
            selectedPortalTargetPortalId = string.Empty;

            if ( portalIdInputField != null )
            {
                portalIdInputField.text = nextPortalId;
            }

            if ( portalTargetMapIdInputField != null )
            {
                portalTargetMapIdInputField.text = currentMapId;
            }

            if ( portalTargetPortalIdInputField != null )
            {
                portalTargetPortalIdInputField.text = string.Empty;
            }
        }

        ///<summary>
        /// 맵 ID 결정
        ///</summary>
        private string ResolveMapId()
        {
            if ( mapIdInputField != null )
            {
                string inputMapId = mapIdInputField.text;

                if ( string.IsNullOrWhiteSpace( inputMapId ) == false )
                {
                    string trimmedInputMapId = inputMapId.Trim();
                    mapIdInputField.text = trimmedInputMapId;
                    return trimmedInputMapId;
                }
            }

            string initialMapId = ResolveInitialMapId();
            return initialMapId;
        }

        ///<summary>
        /// 맵 이름 결정
        ///</summary>
        private string ResolveMapName()
        {
            if ( mapNameInputField != null )
            {
                string inputMapName = mapNameInputField.text;

                if ( string.IsNullOrWhiteSpace( inputMapName ) == false )
                {
                    string trimmedInputMapName = inputMapName.Trim();
                    mapNameInputField.text = trimmedInputMapName;
                    return trimmedInputMapName;
                }
            }

            string fallbackMapName = ResolveMapId();
            return fallbackMapName;
        }

        ///<summary>
        /// 초기 맵 ID 결정
        ///</summary>
        private string ResolveInitialMapId()
        {
            if ( string.IsNullOrWhiteSpace( customMapId ) == false )
            {
                string explicitMapId = customMapId.Trim();
                return explicitMapId;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string fallbackMapId = activeScene.name;
            return fallbackMapId;
        }

        ///<summary>
        /// 초기 맵 이름 결정
        ///</summary>
        private string ResolveInitialMapName()
        {
            string initialMapName = ResolveMapId();
            return initialMapName;
        }

        ///<summary>
        /// 저장 파일 경로 반환
        ///</summary>
        private string GetSaveFilePath()
        {
            string mapId = ResolveMapId();
            string fileName = mapId + ".json";
            string result = Path.Combine( MapDataFolderPath, fileName );
            return result;
        }

        ///<summary>
        /// 배경 모드 버튼 클릭 처리
        ///</summary>
        private void OnBackgroundModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( backgroundPanelRoot );
        }

        ///<summary>
        /// 몬스터 모드 버튼 클릭 처리
        ///</summary>
        private void OnMonsterModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( monsterPanelRoot );
        }

        ///<summary>
        /// 포탈 모드 버튼 클릭 처리
        ///</summary>
        private void OnPortalModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            RefreshPortalPlacementInputFields();
            ToggleSinglePanel( portalPanelRoot );
        }

        ///<summary>
        /// 시작 포탈 배치 버튼 클릭 처리
        ///</summary>
        private void OnStartPortalPlacementButtonClicked()
        {
            string portalIdInputValue = portalIdInputField != null ? portalIdInputField.text : string.Empty;
            string targetMapIdInputValue = portalTargetMapIdInputField.text;
            string targetPortalIdInputValue = portalTargetPortalIdInputField != null ? portalTargetPortalIdInputField.text : string.Empty;

            if ( string.IsNullOrWhiteSpace( portalIdInputValue ) )
            {
                portalIdInputValue = GenerateNextPortalId();

                if ( portalIdInputField != null )
                {
                    portalIdInputField.text = portalIdInputValue;
                }
            }

            if ( string.IsNullOrWhiteSpace( targetMapIdInputValue ) )
            {
                targetMapIdInputValue = DefaultPortalTargetMapId;
                portalTargetMapIdInputField.text = targetMapIdInputValue;
            }

            selectedPortalId = portalIdInputValue.Trim();
            selectedPortalTargetMapId = targetMapIdInputValue.Trim();
            selectedPortalTargetPortalId = string.IsNullOrWhiteSpace( targetPortalIdInputValue ) ? string.Empty : targetPortalIdInputValue.Trim();
            BeginPortalPlacement();
        }

        ///<summary>
        /// 싱글 패널 전환
        ///</summary>
        private void ToggleSinglePanel(RectTransform _targetPanel)
        {
            if ( _targetPanel == null )
            {
                return;
            }

            bool shouldActivate = _targetPanel.gameObject.activeSelf == false;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );

            if ( shouldActivate )
            {
                SetPanelVisible( _targetPanel, true );
            }
        }

        ///<summary>
        /// 모든 맵툴 몬스터에 행동 토글 적용
        ///</summary>
        private void ApplyMonsterBehaviorToggleToAllMonsters()
        {
            bool isBehaviorEnabled = isMonsterBehaviorDisabledInMapTool == false;
            MonsterObject[] monsterObjects = FindObjectsByType<MonsterObject>( FindObjectsInactive.Include, FindObjectsSortMode.None );
            int monsterObjectCount = monsterObjects.Length;

            for ( int index = 0; index < monsterObjectCount; index++ )
            {
                MonsterObject monsterObject = monsterObjects[ index ];

                if ( monsterObject == null )
                {
                    continue;
                }

                monsterObject.SetBehaviorEnabled( isBehaviorEnabled );
            }
        }

        ///<summary>
        /// 배경 스프라이트 기준 이름 적용
        ///</summary>
        private void ApplyBackgroundSpriteByName(string _spriteName)
        {
            ApplyBackgroundSpriteByName( _spriteName, true );
        }

        ///<summary>
        /// 배경 스프라이트 기준 이름 적용
        ///</summary>
        private void ApplyBackgroundSpriteByName(string _spriteName, bool _shouldHidePanel)
        {
            if ( backgroundRenderer == null )
            {
                return;
            }

            if ( backgroundSpriteByName.TryGetValue( _spriteName, out Sprite backgroundSprite ) == false )
            {
                return;
            }

            backgroundRenderer.sprite = backgroundSprite;

            if ( backgroundFitter != null )
            {
                backgroundFitter.ApplyFit();
            }

            if ( backgroundColliderVisualizer != null )
            {
                backgroundColliderVisualizer.RefreshColliderVisual();
            }

            if ( _shouldHidePanel )
            {
                SetPanelVisible( backgroundPanelRoot, false );
            }
        }

        ///<summary>
        /// 몬스터 배치 시작
        ///</summary>
        private void BeginMonsterPlacement(string _prefabName, string _resourcePath)
        {
            StopPlacedObjectDrag();
            selectedMonsterPrefabName = _prefabName;
            selectedMonsterResourcePath = _resourcePath;
            currentMode = eMapToolMode.PLACE_MONSTER;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            RebuildPreviewInstance();
        }

        ///<summary>
        /// 포탈 배치 시작
        ///</summary>
        private void BeginPortalPlacement()
        {
            StopPlacedObjectDrag();
            currentMode = eMapToolMode.PLACE_PORTAL;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            RebuildPreviewInstance();
        }

        ///<summary>
        /// 취소 입력 처리
        ///</summary>
        private void HandleCancelInput()
        {
            if ( Input.GetKeyDown( KeyCode.Escape ) == false )
            {
                return;
            }

            CancelPlacementMode();
            StopPlacedObjectDrag();
        }

        ///<summary>
        /// 배치 취소 배치 모드 가능 여부
        ///</summary>
        private void CancelPlacementMode()
        {
            currentMode = eMapToolMode.NONE;
            selectedMonsterPrefabName = string.Empty;
            selectedMonsterResourcePath = string.Empty;
            DestroyPreviewInstance();
        }

        ///<summary>
        /// 프리뷰 인스턴스 재구성
        ///</summary>
        private void RebuildPreviewInstance()
        {
            DestroyPreviewInstance();
            GameObject previewPrefab = ResolvePreviewPrefab();

            if ( previewPrefab == null )
            {
                currentMode = eMapToolMode.NONE;
                return;
            }

            GameObject createdPreviewInstance = Instantiate( previewPrefab );
            createdPreviewInstance.name = previewPrefab.name + "_Preview";
            ConfigurePreviewInstance( createdPreviewInstance );
            previewInstance = createdPreviewInstance;
        }

        ///<summary>
        /// 프리뷰 프리팹 결정
        ///</summary>
        private GameObject ResolvePreviewPrefab()
        {
            if ( currentMode == eMapToolMode.PLACE_PORTAL )
            {
                GameObject portalPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );
                return portalPrefab;
            }

            if ( currentMode == eMapToolMode.PLACE_MONSTER )
            {
                GameObject monsterPrefab = ResolveMonsterPrefab( selectedMonsterPrefabName, selectedMonsterResourcePath );
                return monsterPrefab;
            }

            return null;
        }

        ///<summary>
        /// 프리뷰 인스턴스 설정
        ///</summary>
        private void ConfigurePreviewInstance(GameObject _targetPreviewInstance)
        {
            if ( currentMode == eMapToolMode.PLACE_MONSTER )
            {
                MonsterObject previewMonsterObject = _targetPreviewInstance.GetComponent<MonsterObject>();

                if ( previewMonsterObject != null )
                {
                    previewMonsterObject.ConfigureMonster( selectedMonsterPrefabName, selectedMonsterPrefabName );
                    previewMonsterObject.SetBehaviorEnabled( isMonsterBehaviorDisabledInMapTool == false );

                    if ( CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
                    {
                        monsterInfoManager.RegisterMonster( previewMonsterObject );
                    }
                }
            }

            ApplyPreviewVisual( _targetPreviewInstance.transform );

            Collider2D[] colliders = _targetPreviewInstance.GetComponentsInChildren<Collider2D>( true );
            int colliderCount = colliders.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                Collider2D colliderComponent = colliders[ index ];
                colliderComponent.enabled = false;
            }

            Rigidbody2D[] rigidbodies = _targetPreviewInstance.GetComponentsInChildren<Rigidbody2D>( true );
            int rigidbodyCount = rigidbodies.Length;

            for ( int index = 0; index < rigidbodyCount; index++ )
            {
                Rigidbody2D rigidbodyComponent = rigidbodies[ index ];
                rigidbodyComponent.simulated = false;
            }
        }

        ///<summary>
        /// 프리뷰 시각화 적용
        ///</summary>
        private void ApplyPreviewVisual(Transform _rootTransform)
        {
            SpriteRenderer[] spriteRenderers = _rootTransform.GetComponentsInChildren<SpriteRenderer>( true );
            int spriteRendererCount = spriteRenderers.Length;

            for ( int index = 0; index < spriteRendererCount; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRenderers[ index ];
                Color rendererColor = spriteRenderer.color;
                rendererColor.a = PreviewAlpha;
                spriteRenderer.color = rendererColor;
            }
        }

        ///<summary>
        /// 프리뷰 인스턴스 제거
        ///</summary>
        private void DestroyPreviewInstance()
        {
            if ( previewInstance == null )
            {
                return;
            }

            MonsterObject previewMonsterObject = previewInstance.GetComponent<MonsterObject>();

            if ( previewMonsterObject != null && CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
            {
                monsterInfoManager.UnregisterMonster( previewMonsterObject );
            }

            Destroy( previewInstance );
            previewInstance = null;
        }

        ///<summary>
        /// 프리뷰 트랜스폼 갱신
        ///</summary>
        private void UpdatePreviewTransform()
        {
            if ( previewInstance == null || currentMode == eMapToolMode.NONE )
            {
                return;
            }

            Vector3 worldPosition = GetMouseWorldPosition();
            previewInstance.transform.position = worldPosition;
        }

        ///<summary>
        /// 배치 오브젝트 드래그 입력 처리
        ///</summary>
        private void HandlePlacedObjectDragInput()
        {
            if ( currentMode != eMapToolMode.NONE )
            {
                return;
            }

            if ( Input.GetMouseButtonDown( MouseButtonLeft ) && IsPointerOverUi() == false )
            {
                MapToolPlacedObject hitPlacedObject = TryGetPlacedObjectAtMousePosition();

                if ( hitPlacedObject != null )
                {
                    StartPlacedObjectDrag( hitPlacedObject );
                }
            }

            if ( isDraggingPlacedObject == false || draggedPlacedObject == null )
            {
                return;
            }

            Vector3 draggedWorldPosition = GetMouseWorldPosition();
            Vector3 targetPosition = draggedWorldPosition + draggedObjectOffset;
            draggedPlacedObject.transform.position = targetPosition;

            if ( Input.GetMouseButtonUp( MouseButtonLeft ) )
            {
                StopPlacedObjectDrag();
            }
        }

        ///<summary>
        /// 배치 오브젝트 드래그 시작
        ///</summary>
        private void StartPlacedObjectDrag(MapToolPlacedObject _targetPlacedObject)
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            Vector3 currentObjectPosition = _targetPlacedObject.transform.position;
            draggedObjectOffset = currentObjectPosition - mouseWorldPosition;
            draggedPlacedObject = _targetPlacedObject;
            isDraggingPlacedObject = true;
        }

        ///<summary>
        /// 배치 오브젝트 드래그 중지
        ///</summary>
        private void StopPlacedObjectDrag()
        {
            draggedPlacedObject = null;
            draggedObjectOffset = Vector3.zero;
            isDraggingPlacedObject = false;
        }

        ///<summary>
        /// 마우스 위치 배치 오브젝트 조회 시도
        ///</summary>
        private MapToolPlacedObject TryGetPlacedObjectAtMousePosition()
        {
            Vector3 worldPosition = GetMouseWorldPosition();
            Vector2 overlapPoint = new Vector2( worldPosition.x, worldPosition.y );
            Collider2D[] hitColliders = Physics2D.OverlapPointAll( overlapPoint );
            int hitColliderCount = hitColliders.Length;

            for ( int index = 0; index < hitColliderCount; index++ )
            {
                Collider2D hitCollider = hitColliders[ index ];
                MapToolPlacedObject placedObject = hitCollider.GetComponentInParent<MapToolPlacedObject>();

                if ( placedObject != null )
                {
                    return placedObject;
                }
            }

            return null;
        }

        ///<summary>
        /// 배치 입력 처리
        ///</summary>
        private void HandlePlacementInput()
        {
            if ( currentMode == eMapToolMode.NONE || previewInstance == null )
            {
                return;
            }

            if ( isDraggingPlacedObject )
            {
                return;
            }

            if ( IsPointerOverUi() )
            {
                return;
            }

            if ( Input.GetMouseButtonDown( MouseButtonLeft ) == false )
            {
                return;
            }

            if ( currentMode == eMapToolMode.PLACE_PORTAL )
            {
                PlacePortalAtMousePosition();
                return;
            }

            if ( currentMode == eMapToolMode.PLACE_MONSTER )
            {
                PlaceMonsterAtMousePosition();
            }
        }

        ///<summary>
        /// 삭제 입력 처리
        ///</summary>
        private void HandleDeleteInput()
        {
            if ( isDraggingPlacedObject )
            {
                return;
            }

            if ( Input.GetMouseButtonDown( MouseButtonRight ) == false )
            {
                return;
            }

            if ( IsPointerOverUi() )
            {
                return;
            }

            MapToolPlacedObject placedObject = TryGetPlacedObjectAtMousePosition();

            if ( placedObject == null )
            {
                return;
            }

            RemovePlacedObject( placedObject );
        }

        ///<summary>
        /// 마우스 위치 포탈 배치 처리
        ///</summary>
        private void PlacePortalAtMousePosition()
        {
            GameObject portalPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );

            if ( portalPrefab == null )
            {
                return;
            }

            Vector3 spawnPosition = GetMouseWorldPosition();
            GameObject portalInstance = Instantiate( portalPrefab, spawnPosition, Quaternion.identity );
            portalInstance.name = portalPrefab.name;
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( portalInstance );
            placedObject.SetupPortal( portalPrefab.name, PortalPrefabResourcePath, selectedPortalId, selectedPortalTargetMapId, selectedPortalTargetPortalId );
            ApplyPortalLinkData( portalInstance, selectedPortalId, selectedPortalTargetMapId, selectedPortalTargetPortalId );
            placedObjects.Add( placedObject );
            RefreshPortalPlacementInputFields();
        }

        ///<summary>
        /// 마우스 위치 몬스터 배치 처리
        ///</summary>
        private void PlaceMonsterAtMousePosition()
        {
            GameObject monsterPrefab = ResolveMonsterPrefab( selectedMonsterPrefabName, selectedMonsterResourcePath );

            if ( monsterPrefab == null )
            {
                return;
            }

            Vector3 spawnPosition = GetMouseWorldPosition();
            GameObject monsterInstance = Instantiate( monsterPrefab, spawnPosition, Quaternion.identity );
            monsterInstance.name = monsterPrefab.name;
            MonsterObject createdMonsterObject = monsterInstance.GetComponent<MonsterObject>();

            if ( createdMonsterObject != null )
            {
                createdMonsterObject.ConfigureMonster( monsterPrefab.name, monsterPrefab.name );
                createdMonsterObject.SetBehaviorEnabled( isMonsterBehaviorDisabledInMapTool == false );

                if ( CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
                {
                    monsterInfoManager.RegisterMonster( createdMonsterObject );
                }
            }

            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( monsterInstance );
            placedObject.SetupMonster( selectedMonsterPrefabName, selectedMonsterResourcePath );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 포탈 연결 데이터 적용
        ///</summary>
        private void ApplyPortalLinkData(GameObject _portalInstance, string _portalId, string _targetMapId, string _targetPortalId)
        {
            PortalObject portalObject = _portalInstance.GetComponent<PortalObject>();

            if ( portalObject == null )
            {
                return;
            }

            portalObject.ConfigurePortal( _portalId, _targetMapId, _targetPortalId );
        }

        ///<summary>
        /// 마우스 월드 위치 반환
        ///</summary>
        private Vector3 GetMouseWorldPosition()
        {
            if ( worldCamera == null )
            {
                return Vector3.zero;
            }

            Vector3 mouseScreenPosition = Input.mousePosition;
            mouseScreenPosition.z = Mathf.Abs( worldCamera.transform.position.z );
            Vector3 mouseWorldPosition = worldCamera.ScreenToWorldPoint( mouseScreenPosition );
            mouseWorldPosition.z = 0.0f;
            return mouseWorldPosition;
        }

        ///<summary>
        /// UI 위 포인터 여부
        ///</summary>
        private bool IsPointerOverUi()
        {
            if ( targetEventSystem == null )
            {
                return false;
            }

            bool isPointerOverUi = targetEventSystem.IsPointerOverGameObject();
            return isPointerOverUi;
        }

        ///<summary>
        /// 몬스터 프리팹 결정
        ///</summary>
        private GameObject ResolveMonsterPrefab(string _prefabName, string _resourcePath)
        {
            if ( string.IsNullOrEmpty( _prefabName ) == false && monsterPrefabByName.TryGetValue( _prefabName, out GameObject cachedPrefab ) )
            {
                return cachedPrefab;
            }

            if ( string.IsNullOrEmpty( _resourcePath ) == false )
            {
                GameObject loadedPrefab = Resources.Load<GameObject>( _resourcePath );
                return loadedPrefab;
            }

            return null;
        }

        ///<summary>
        /// 배치 오브젝트 컴포넌트 보장
        ///</summary>
        private MapToolPlacedObject EnsurePlacedObjectComponent(GameObject _targetObject)
        {
            MapToolPlacedObject placedObject = _targetObject.GetComponent<MapToolPlacedObject>();

            if ( placedObject != null )
            {
                return placedObject;
            }

            MapToolPlacedObject createdPlacedObject = _targetObject.AddComponent<MapToolPlacedObject>();
            return createdPlacedObject;
        }

        ///<summary>
        /// 배치 오브젝트 목록 정리
        ///</summary>
        private void ClearPlacedObjects()
        {
            int placedObjectCount = placedObjects.Count;

            for ( int index = 0; index < placedObjectCount; index++ )
            {
                MapToolPlacedObject placedObject = placedObjects[ index ];

                if ( placedObject == null )
                {
                    continue;
                }

                Destroy( placedObject.gameObject );
            }

            placedObjects.Clear();
        }

        ///<summary>
        /// 몬스터 포탈 오브젝트 초기화
        ///</summary>
        private void ClearMonsterAndPortalObjects()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );

            int placedObjectCount = placedObjects.Count;

            for ( int index = placedObjectCount - 1; index >= 0; index-- )
            {
                MapToolPlacedObject placedObject = placedObjects[ index ];

                if ( placedObject == null )
                {
                    placedObjects.RemoveAt( index );
                    continue;
                }

                MapToolPlacedObject.eMapToolPlacedObjectType placedObjectType = placedObject.GetPlacedObjectType();
                bool isClearTarget = placedObjectType == MapToolPlacedObject.eMapToolPlacedObjectType.MONSTER || placedObjectType == MapToolPlacedObject.eMapToolPlacedObjectType.PORTAL;

                if ( isClearTarget == false )
                {
                    continue;
                }

                placedObjects.RemoveAt( index );
                Destroy( placedObject.gameObject );
            }

            if ( portalIdInputField != null )
            {
                string nextPortalId = GenerateNextPortalId();
                portalIdInputField.text = nextPortalId;
            }

            if ( portalTargetMapIdInputField != null && string.IsNullOrWhiteSpace( portalTargetMapIdInputField.text ) )
            {
                string currentMapId = ResolveMapId();
                portalTargetMapIdInputField.text = currentMapId;
            }

            if ( portalTargetPortalIdInputField != null )
            {
                portalTargetPortalIdInputField.text = string.Empty;
            }
        }

        ///<summary>
        /// 배치 오브젝트 제거
        ///</summary>
        private void RemovePlacedObject(MapToolPlacedObject _placedObject)
        {
            if ( draggedPlacedObject == _placedObject )
            {
                StopPlacedObjectDrag();
            }

            placedObjects.Remove( _placedObject );
            Destroy( _placedObject.gameObject );
        }

        ///<summary>
        /// 트랜스폼 데이터 Vector3 생성
        ///</summary>
        private Vector3 CreateVector3FromTransformData(float[] _values, Vector3 _fallbackValue)
        {
            if ( _values == null || _values.Length < 3 )
            {
                return _fallbackValue;
            }

            Vector3 result = new Vector3( _values[ 0 ], _values[ 1 ], _values[ 2 ] );
            return result;
        }

        ///<summary>
        /// 패널 표시 상태 설정
        ///</summary>
        private void SetPanelVisible(RectTransform _panelRoot, bool _isVisible)
        {
            if ( _panelRoot == null )
            {
                return;
            }

            _panelRoot.gameObject.SetActive( _isVisible );
        }

        ///<summary>
        /// 선택 패널 보장
        ///</summary>
        private RectTransform EnsureSelectionPanel(RectTransform _existingPanelRoot, string _panelName, RectTransform _canvasRectTransform, out RectTransform _listRoot)
        {
            _listRoot = null;

            if ( _existingPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( _canvasRectTransform, _panelName );
                _existingPanelRoot = foundPanelRoot;
            }

            if ( _existingPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreateStandardPanel( _panelName, _canvasRectTransform );
                _existingPanelRoot = createdPanelRoot;
            }

            RectTransform foundListRoot = EnsureScrollableListRoot( _existingPanelRoot );
            _listRoot = foundListRoot;
            return _existingPanelRoot;
        }

        ///<summary>
        /// 스크롤 가능 목록 루트 보장
        ///</summary>
        private RectTransform EnsureScrollableListRoot(RectTransform _panelRoot)
        {
            ScrollRect existingScrollRect = _panelRoot.GetComponentInChildren<ScrollRect>( true );

            if ( existingScrollRect != null && existingScrollRect.content != null )
            {
                RectTransform existingContent = existingScrollRect.content;
                return existingContent;
            }

            RectTransform existingListRoot = FindChildRectTransform( _panelRoot, ListRootObjectName );

            if ( existingListRoot != null )
            {
                Destroy( existingListRoot.gameObject );
            }

            GameObject scrollRectObject = new GameObject( ScrollRectObjectName, typeof( RectTransform ), typeof( Image ), typeof( ScrollRect ) );
            RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
            scrollRectTransform.SetParent( _panelRoot, false );
            scrollRectTransform.anchorMin = new Vector2( 0.0f, 0.0f );
            scrollRectTransform.anchorMax = new Vector2( 1.0f, 1.0f );
            scrollRectTransform.offsetMin = new Vector2( PanelPadding, PanelPadding );
            scrollRectTransform.offsetMax = new Vector2( -PanelPadding, -PanelPadding );
            Image scrollRectImage = scrollRectObject.GetComponent<Image>();
            scrollRectImage.color = new Color( 0.0f, 0.0f, 0.0f, 0.0f );
            ScrollRect scrollRect = scrollRectObject.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24.0f;

            RectTransform viewportRectTransform = CreatePanelRoot( ScrollViewportObjectName, scrollRectTransform, Vector2.zero, Vector2.one, new Vector2( 0.5f, 0.5f ) );
            viewportRectTransform.offsetMin = Vector2.zero;
            viewportRectTransform.offsetMax = Vector2.zero;
            viewportRectTransform.gameObject.AddComponent<RectMask2D>();

            RectTransform createdListRoot = CreatePanelRoot( ListRootObjectName, viewportRectTransform, new Vector2( 0.0f, 1.0f ), new Vector2( 1.0f, 1.0f ), new Vector2( 0.5f, 1.0f ) );
            VerticalLayoutGroup verticalLayoutGroup = createdListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.spacing = ItemSpacing;
            verticalLayoutGroup.padding = new RectOffset( 0, 0, 0, 0 );
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            ContentSizeFitter contentSizeFitter = createdListRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            createdListRoot.offsetMin = new Vector2( 0.0f, 0.0f );
            createdListRoot.offsetMax = new Vector2( 0.0f, 0.0f );

            scrollRect.viewport = viewportRectTransform;
            scrollRect.content = createdListRoot;
            return createdListRoot;
        }

        ///<summary>
        /// 포탈 패널 보장
        ///</summary>
        private RectTransform EnsurePortalPanel(RectTransform _existingPanelRoot, RectTransform _canvasRectTransform)
        {
            if ( _existingPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( _canvasRectTransform, PortalPanelObjectName );
                _existingPanelRoot = foundPanelRoot;
            }

            if ( _existingPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreateStandardPanel( PortalPanelObjectName, _canvasRectTransform );
                _existingPanelRoot = createdPanelRoot;
            }

            if ( portalIdInputField == null )
            {
                TMP_InputField createdPortalIdInputField = CreateInputField( "PortalIdInputField", _existingPanelRoot, "포탈 고유 ID", string.Empty );
                RectTransform portalIdRectTransform = createdPortalIdInputField.GetComponent<RectTransform>();
                portalIdRectTransform.anchoredPosition = new Vector2( 0.0f, -62.0f );
                portalIdInputField = createdPortalIdInputField;
            }

            EnsureInputFieldTitle( PortalIdTitleObjectName, _existingPanelRoot, "포탈 고유 ID", new Vector2( 0.0f, -24.0f ) );
            UpdateInputFieldPlaceholder( portalIdInputField, "포탈 고유 ID" );

            if ( portalTargetMapIdInputField == null )
            {
                TMP_InputField createdInputField = CreateInputField( "PortalTargetMapIdInputField", _existingPanelRoot, "목표 맵 ID", ResolveMapId() );
                portalTargetMapIdInputField = createdInputField;
                RectTransform targetMapRectTransform = createdInputField.GetComponent<RectTransform>();
                targetMapRectTransform.anchoredPosition = new Vector2( 0.0f, -170.0f );
            }

            EnsureInputFieldTitle( PortalTargetMapTitleObjectName, _existingPanelRoot, "목표 맵 ID", new Vector2( 0.0f, -132.0f ) );
            UpdateInputFieldPlaceholder( portalTargetMapIdInputField, "목표 맵 ID" );

            if ( portalTargetPortalIdInputField == null )
            {
                TMP_InputField createdTargetPortalInputField = CreateInputField( "PortalTargetPortalIdInputField", _existingPanelRoot, "도착 포탈 ID", string.Empty );
                RectTransform targetPortalRectTransform = createdTargetPortalInputField.GetComponent<RectTransform>();
                targetPortalRectTransform.anchoredPosition = new Vector2( 0.0f, -278.0f );
                portalTargetPortalIdInputField = createdTargetPortalInputField;
            }

            EnsureInputFieldTitle( PortalTargetPortalTitleObjectName, _existingPanelRoot, "도착 포탈 ID", new Vector2( 0.0f, -240.0f ) );
            UpdateInputFieldPlaceholder( portalTargetPortalIdInputField, "도착 포탈 ID" );

            if ( startPortalPlacementButton == null )
            {
                CButtonEx createdButton = CreateTextButton( "StartPortalPlacementButton", _existingPanelRoot, "포탈 배치 시작", PanelWidth - ( PanelPadding * 2.0f ), PortalActionButtonHeight );
                RectTransform buttonRectTransform = createdButton.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.pivot = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.anchoredPosition = new Vector2( 0.0f, -358.0f );
                startPortalPlacementButton = createdButton;
            }

            UpdateButtonLabel( startPortalPlacementButton, "포탈 배치 시작" );

            return _existingPanelRoot;
        }

        ///<summary>
        /// 기본 패널 생성
        ///</summary>
        private RectTransform CreateStandardPanel(string _panelName, RectTransform _canvasRectTransform)
        {
            RectTransform panelRoot = CreatePanelRoot( _panelName, _canvasRectTransform, new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ) );
            Image panelImage = panelRoot.gameObject.AddComponent<Image>();
            panelImage.color = new Color( 0.12f, 0.14f, 0.18f, 0.92f );
            LayoutElement layoutElement = panelRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            panelRoot.sizeDelta = new Vector2( PanelWidth, PanelHeight );
            panelRoot.anchoredPosition = new Vector2( 0.0f, PanelTopOffset );
            return panelRoot;
        }

        ///<summary>
        /// 패널 루트 생성
        ///</summary>
        private RectTransform CreatePanelRoot( string _objectName, RectTransform _parent, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _pivot )
        {
            GameObject panelObject = new GameObject( _objectName, typeof( RectTransform ) );
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parent, false );
            rectTransform.anchorMin = _anchorMin;
            rectTransform.anchorMax = _anchorMax;
            rectTransform.pivot = _pivot;
            return rectTransform;
        }

        ///<summary>
        /// 입력 필드 제목 보장
        ///</summary>
        private TMP_Text EnsureInputFieldTitle(string _objectName, RectTransform _parent, string _titleText, Vector2 _anchoredPosition)
        {
            RectTransform titleRectTransform = FindChildRectTransform( _parent, _objectName );

            if ( titleRectTransform == null )
            {
                GameObject titleObject = new GameObject( _objectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
                titleRectTransform = titleObject.GetComponent<RectTransform>();
                titleRectTransform.SetParent( _parent, false );
                titleRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
                titleRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
                titleRectTransform.pivot = new Vector2( 0.5f, 1.0f );
                titleRectTransform.sizeDelta = new Vector2( PanelWidth - ( PanelPadding * 2.0f ), 20.0f );
            }

            titleRectTransform.anchoredPosition = _anchoredPosition;
            TMP_Text titleTextComponent = titleRectTransform.GetComponent<TMP_Text>();
            titleTextComponent.text = _titleText;
            titleTextComponent.fontSize = PortalFieldTitleFontSize;
            titleTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            titleTextComponent.color = new Color( 1.0f, 1.0f, 1.0f, 0.72f );
            return titleTextComponent;
        }

        ///<summary>
        /// 텍스트 버튼 생성
        ///</summary>
        private CButtonEx CreateTextButton(string _objectName, RectTransform _parent, string _labelText, float _width, float _height)
        {
            GameObject buttonObject = new GameObject( _objectName, typeof( RectTransform ), typeof( Image ), typeof( CButtonEx ) );
            RectTransform buttonRectTransform = buttonObject.GetComponent<RectTransform>();
            buttonRectTransform.SetParent( _parent, false );
            buttonRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.pivot = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.sizeDelta = new Vector2( _width, _height );
            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = _width;
            layoutElement.preferredHeight = _height;
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color( 0.22f, 0.27f, 0.34f, 0.96f );
            CButtonEx buttonComponent = buttonObject.GetComponent<CButtonEx>();
            buttonComponent.targetGraphic = buttonImage;
            ColorBlock colorBlock = buttonComponent.colors;
            colorBlock.normalColor = buttonImage.color;
            colorBlock.highlightedColor = new Color( 0.30f, 0.37f, 0.46f, 1.0f );
            colorBlock.pressedColor = new Color( 0.16f, 0.20f, 0.26f, 1.0f );
            colorBlock.selectedColor = colorBlock.highlightedColor;
            colorBlock.disabledColor = new Color( 0.18f, 0.18f, 0.18f, 0.6f );
            buttonComponent.colors = colorBlock;

            GameObject labelObject = new GameObject( LabelObjectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
            RectTransform labelRectTransform = labelObject.GetComponent<RectTransform>();
            labelRectTransform.SetParent( buttonRectTransform, false );
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = Vector2.zero;
            labelRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI labelTextComponent = labelObject.GetComponent<TextMeshProUGUI>();
            labelTextComponent.text = _labelText;
            labelTextComponent.fontSize = 24.0f;
            labelTextComponent.alignment = TextAlignmentOptions.Center;
            labelTextComponent.color = Color.white;
            return buttonComponent;
        }

        ///<summary>
        /// 토글 생성
        ///</summary>
        private Toggle CreateToggle( string _objectName, RectTransform _parent, string _labelText )
        {
            GameObject toggleObject = new GameObject( _objectName, typeof( RectTransform ), typeof( Toggle ) );
            RectTransform toggleRectTransform = toggleObject.GetComponent<RectTransform>();
            toggleRectTransform.SetParent( _parent, false );
            toggleRectTransform.sizeDelta = new Vector2( MapInfoInputWidth, 30.0f );
            Toggle toggleComponent = toggleObject.GetComponent<Toggle>();

            GameObject backgroundObject = new GameObject( "Background", typeof( RectTransform ), typeof( Image ) );
            RectTransform backgroundRectTransform = backgroundObject.GetComponent<RectTransform>();
            backgroundRectTransform.SetParent( toggleRectTransform, false );
            backgroundRectTransform.anchorMin = new Vector2( 0.0f, 0.5f );
            backgroundRectTransform.anchorMax = new Vector2( 0.0f, 0.5f );
            backgroundRectTransform.pivot = new Vector2( 0.0f, 0.5f );
            backgroundRectTransform.sizeDelta = new Vector2( 22.0f, 22.0f );
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color( 0.20f, 0.22f, 0.26f, 0.98f );

            GameObject checkmarkObject = new GameObject( "Checkmark", typeof( RectTransform ), typeof( Image ) );
            RectTransform checkmarkRectTransform = checkmarkObject.GetComponent<RectTransform>();
            checkmarkRectTransform.SetParent( backgroundRectTransform, false );
            checkmarkRectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            checkmarkRectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            checkmarkRectTransform.pivot = new Vector2( 0.5f, 0.5f );
            checkmarkRectTransform.sizeDelta = new Vector2( 14.0f, 14.0f );
            Image checkmarkImage = checkmarkObject.GetComponent<Image>();
            checkmarkImage.color = new Color( 0.35f, 0.85f, 0.45f, 1.0f );

            GameObject labelObject = new GameObject( "Label", typeof( RectTransform ), typeof( TextMeshProUGUI ) );
            RectTransform labelRectTransform = labelObject.GetComponent<RectTransform>();
            labelRectTransform.SetParent( toggleRectTransform, false );
            labelRectTransform.anchorMin = new Vector2( 0.0f, 0.0f );
            labelRectTransform.anchorMax = new Vector2( 1.0f, 1.0f );
            labelRectTransform.offsetMin = new Vector2( 32.0f, 0.0f );
            labelRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI labelTextComponent = labelObject.GetComponent<TextMeshProUGUI>();
            labelTextComponent.text = _labelText;
            labelTextComponent.fontSize = 22.0f;
            labelTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
            labelTextComponent.color = Color.white;

            toggleComponent.targetGraphic = backgroundImage;
            toggleComponent.graphic = checkmarkImage;
            return toggleComponent;
        }

        ///<summary>
        /// 입력 필드 생성
        ///</summary>
        private TMP_InputField CreateInputField(string _objectName, RectTransform _parent, string _placeholderText, string _defaultText)
        {
            GameObject inputFieldObject = new GameObject( _objectName, typeof( RectTransform ), typeof( Image ), typeof( TMP_InputField ) );
            RectTransform inputFieldRectTransform = inputFieldObject.GetComponent<RectTransform>();
            inputFieldRectTransform.SetParent( _parent, false );
            inputFieldRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.pivot = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.sizeDelta = new Vector2( PanelWidth - ( PanelPadding * 1.2f ), 54.0f );
            inputFieldRectTransform.anchoredPosition = new Vector2( 0.0f, -40.0f );
            Image inputFieldImage = inputFieldObject.GetComponent<Image>();
            inputFieldImage.color = new Color( 0.20f, 0.22f, 0.26f, 0.98f );
            TMP_InputField inputField = inputFieldObject.GetComponent<TMP_InputField>();
            inputField.targetGraphic = inputFieldImage;

            RectTransform viewportRectTransform = CreatePanelRoot( InputFieldViewportObjectName, inputFieldRectTransform, Vector2.zero, Vector2.one, new Vector2( 0.5f, 0.5f ) );
            viewportRectTransform.offsetMin = new Vector2( 16.0f, 8.0f );
            viewportRectTransform.offsetMax = new Vector2( -16.0f, -8.0f );
            viewportRectTransform.gameObject.AddComponent<RectMask2D>();

            GameObject textObject = new GameObject( InputFieldTextObjectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
            RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
            textRectTransform.SetParent( viewportRectTransform, false );
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.fontSize = 23.0f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.text = _defaultText;

            GameObject placeholderObject = new GameObject( InputFieldPlaceholderObjectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
            RectTransform placeholderRectTransform = placeholderObject.GetComponent<RectTransform>();
            placeholderRectTransform.SetParent( viewportRectTransform, false );
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.offsetMin = Vector2.zero;
            placeholderRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholderComponent = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderComponent.fontSize = 23.0f;
            placeholderComponent.text = _placeholderText;
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderComponent.color = new Color( 1.0f, 1.0f, 1.0f, 0.45f );

            inputField.textViewport = viewportRectTransform;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;
            inputField.text = _defaultText;
            return inputField;
        }

        ///<summary>
        /// 버튼 라벨 갱신
        ///</summary>
        private void UpdateButtonLabel(CButtonEx _button, string _labelText)
        {
            if ( _button == null )
            {
                return;
            }

            TMP_Text labelTextComponent = _button.GetComponentInChildren<TMP_Text>( true );

            if ( labelTextComponent == null )
            {
                return;
            }

            labelTextComponent.text = _labelText;
        }

        ///<summary>
        /// 입력 필드 플레이스홀더 갱신
        ///</summary>
        private void UpdateInputFieldPlaceholder(TMP_InputField _inputField, string _placeholderText)
        {
            if ( _inputField == null )
            {
                return;
            }

            TMP_Text placeholderTextComponent = _inputField.placeholder as TMP_Text;

            if ( placeholderTextComponent == null )
            {
                return;
            }

            placeholderTextComponent.text = _placeholderText;
        }

        ///<summary>
        /// 자식 사각형 트랜스폼 탐색
        ///</summary>
        private RectTransform FindChildRectTransform(RectTransform _parent, string _childName)
        {
            int childCount = _parent.childCount;

            for ( int index = 0; index < childCount; index++ )
            {
                Transform childTransform = _parent.GetChild( index );
                RectTransform childRectTransform = childTransform as RectTransform;

                if ( childRectTransform == null )
                {
                    continue;
                }

                if ( childRectTransform.name == _childName )
                {
                    return childRectTransform;
                }
            }

            return null;
        }

        ///<summary>
        /// 자식 정리
        ///</summary>
        private void ClearChildren(RectTransform _parent)
        {
            if ( _parent == null )
            {
                return;
            }

            int childCount = _parent.childCount;

            for ( int index = childCount - 1; index >= 0; index-- )
            {
                Transform childTransform = _parent.GetChild( index );
                Destroy( childTransform.gameObject );
            }
        }
    }
}


