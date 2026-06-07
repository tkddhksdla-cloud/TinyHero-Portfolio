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
    /// 플레이모드 맵 편집 도구의 UI와 배치 흐름을 관리한다.
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
        private const string ToolbarObjectName = "Toolbar";
        private const string BackgroundPanelObjectName = "BackgroundPanel";
        private const string MonsterPanelObjectName = "MonsterPanel";
        private const string PortalPanelObjectName = "PortalPanel";
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
        private const float PanelWidth = 280.0f;
        private const float PanelHeight = 320.0f;
        private const float PanelTopOffset = -110.0f;
        private const float ToolbarTopOffset = -20.0f;
        private const float ToolbarSpacing = 16.0f;
        private const float ToolbarButtonWidth = 180.0f;
        private const float ToolbarButtonHeight = 56.0f;
        private const float ListButtonHeight = 44.0f;
        private const float PortalActionButtonHeight = 50.0f;
        private const float PanelPadding = 14.0f;
        private const float ItemSpacing = 8.0f;
        private const float MapInfoPanelWidth = 360.0f;
        private const float MapInfoPanelHeight = 190.0f;
        private const float MapInfoInputWidth = 300.0f;
        private const float MapInfoButtonWidth = 142.0f;
        private const float MapInfoPanelLeftOffset = 20.0f;
        private const float MapInfoPanelTopOffset = -20.0f;
        private const float LoadPanelWidth = 360.0f;
        private const float LoadPanelHeight = 360.0f;
        private const float LoadPanelLeftOffset = 20.0f;
        private const float LoadPanelTopOffset = -220.0f;
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
        [SerializeField] private TMP_InputField portalTargetMapIdInputField;
        [SerializeField] private CButtonEx backgroundModeButton;
        [SerializeField] private CButtonEx monsterModeButton;
        [SerializeField] private CButtonEx portalModeButton;
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
        private string selectedPortalTargetMapId = DefaultPortalTargetMapId;
        private bool isDraggingPlacedObject;

        ///<summary>
        /// 씬 참조와 런타임 UI 골격을 준비한다.
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
        /// 카탈로그와 버튼 이벤트 및 초기 입력값을 설정한다.
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
        /// 프리뷰 이동과 편집 입력을 프레임마다 처리한다.
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
        /// 카메라와 배경 참조를 자동으로 연결한다.
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
        /// 배경 오브젝트가 없으면 기본 프리팹을 생성한다.
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
        /// 배경 충돌 시각화 컴포넌트를 보장한다.
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
        /// 플레이어 프리팹이 없으면 기본 캐릭터를 씬에 생성한다.
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
        /// 런타임 캔버스와 이벤트 시스템을 준비한다.
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
        /// 상단 중앙 툴바를 생성하거나 재사용한다.
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
        }

        ///<summary>
        /// 편집 패널들을 생성하거나 재사용한다.
        ///</summary>
        private void EnsurePanelsExist()
        {
            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
            backgroundPanelRoot = EnsureSelectionPanel( backgroundPanelRoot, BackgroundPanelObjectName, canvasRectTransform, out backgroundListRoot );
            monsterPanelRoot = EnsureSelectionPanel( monsterPanelRoot, MonsterPanelObjectName, canvasRectTransform, out monsterListRoot );
            portalPanelRoot = EnsurePortalPanel( portalPanelRoot, canvasRectTransform );
        }

        ///<summary>
        /// 좌상단 맵 정보 패널을 생성하거나 재사용한다.
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
                mapIdRectTransform.anchoredPosition = new Vector2( PanelPadding, -20.0f );
                mapIdInputField = createdMapIdInputField;
            }

            if ( saveMapButton == null )
            {
                CButtonEx createdSaveMapButton = CreateTextButton( "SaveMapButton", mapInfoPanelRoot, "맵 저장", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform saveButtonRectTransform = createdSaveMapButton.GetComponent<RectTransform>();
                saveButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchoredPosition = new Vector2( PanelPadding, -90.0f );
                saveMapButton = createdSaveMapButton;
            }

            if ( loadMapButton == null )
            {
                CButtonEx createdLoadMapButton = CreateTextButton( "LoadMapButton", mapInfoPanelRoot, "맵 불러오기", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform loadButtonRectTransform = createdLoadMapButton.GetComponent<RectTransform>();
                loadButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchoredPosition = new Vector2( PanelPadding + MapInfoButtonWidth + 12.0f, -90.0f );
                loadMapButton = createdLoadMapButton;
            }
        }

        ///<summary>
        /// 저장된 맵 목록 패널을 생성하거나 재사용한다.
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
        /// 입력 필드 기본값을 현재 맵 기준으로 채운다.
        ///</summary>
        private void InitializeInputFields()
        {
            if ( mapIdInputField != null && string.IsNullOrWhiteSpace( mapIdInputField.text ) )
            {
                string initialMapId = ResolveInitialMapId();
                mapIdInputField.text = initialMapId;
            }

            if ( portalTargetMapIdInputField != null && string.IsNullOrWhiteSpace( portalTargetMapIdInputField.text ) )
            {
                portalTargetMapIdInputField.text = DefaultPortalTargetMapId;
            }
        }

        ///<summary>
        /// 버튼 이벤트를 중복 없이 연결한다.
        ///</summary>
        private void BindUiEvents()
        {
            backgroundModeButton.onClick.RemoveAllListeners();
            backgroundModeButton.onClick.AddListener( OnBackgroundModeButtonClicked );
            monsterModeButton.onClick.RemoveAllListeners();
            monsterModeButton.onClick.AddListener( OnMonsterModeButtonClicked );
            portalModeButton.onClick.RemoveAllListeners();
            portalModeButton.onClick.AddListener( OnPortalModeButtonClicked );
            startPortalPlacementButton.onClick.RemoveAllListeners();
            startPortalPlacementButton.onClick.AddListener( OnStartPortalPlacementButtonClicked );
            saveMapButton.onClick.RemoveAllListeners();
            saveMapButton.onClick.AddListener( OnSaveMapButtonClicked );
            loadMapButton.onClick.RemoveAllListeners();
            loadMapButton.onClick.AddListener( OnLoadMapButtonClicked );
        }

        ///<summary>
        /// 배경과 몬스터 리소스 목록을 읽어온다.
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
        /// 배경 선택 패널 버튼 목록을 갱신한다.
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
        /// 몬스터 선택 패널 버튼 목록을 갱신한다.
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
        /// 저장된 맵 목록 패널 버튼을 갱신한다.
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
        /// 저장된 맵 ID 목록을 파일 시스템에서 수집한다.
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
        /// 선택한 저장 맵을 입력 필드에 반영하고 즉시 불러온다.
        ///</summary>
        private void LoadSelectedMap( string mapId )
        {
            if ( mapIdInputField != null )
            {
                mapIdInputField.text = mapId;
            }

            SetPanelVisible( loadMapPanelRoot, false );
            LoadSavedMapData();
        }

        ///<summary>
        /// 저장 버튼으로 현재 맵 상태를 파일로 기록한다.
        ///</summary>
        private void OnSaveMapButtonClicked()
        {
            SaveMapData();
            RebuildLoadMapPanel();
        }

        ///<summary>
        /// 불러오기 버튼으로 저장된 맵 목록 패널을 토글한다.
        ///</summary>
        private void OnLoadMapButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            RebuildLoadMapPanel();
            ToggleLoadMapPanel();
        }

        ///<summary>
        /// 저장된 맵 목록 패널만 토글한다.
        ///</summary>
        private void ToggleLoadMapPanel()
        {
            bool shouldActivate = loadMapPanelRoot.gameObject.activeSelf == false;
            SetPanelVisible( loadMapPanelRoot, shouldActivate );
        }

        ///<summary>
        /// 저장된 맵 정보를 읽어 현재 씬에 복원한다.
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
        /// 로드한 저장 데이터를 씬 상태로 복원한다.
        ///</summary>
        private void ApplyLoadedData( CMapToolSaveData loadedData )
        {
            ClearPlacedObjects();

            if ( loadedData.portals == null )
            {
                loadedData.portals = new List<CMapToolPortalSaveData>();
            }

            if ( loadedData.monsters == null )
            {
                loadedData.monsters = new List<CMapToolMonsterSaveData>();
            }

            if ( string.IsNullOrWhiteSpace( loadedData.mapId ) == false && mapIdInputField != null )
            {
                mapIdInputField.text = loadedData.mapId;
            }

            if ( string.IsNullOrEmpty( loadedData.backgroundSpriteName ) == false )
            {
                ApplyBackgroundSpriteByName( loadedData.backgroundSpriteName, false );
            }

            int portalCount = loadedData.portals.Count;

            for ( int index = 0; index < portalCount; index++ )
            {
                CMapToolPortalSaveData portalSaveData = loadedData.portals[ index ];
                SpawnSavedPortal( portalSaveData );
            }

            int monsterCount = loadedData.monsters.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = loadedData.monsters[ index ];
                SpawnSavedMonster( monsterSaveData );
            }
        }

        ///<summary>
        /// 저장된 포탈 데이터를 씬에 다시 배치한다.
        ///</summary>
        private void SpawnSavedPortal( CMapToolPortalSaveData portalSaveData )
        {
            GameObject portalPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );

            if ( portalPrefab == null || portalSaveData == null )
            {
                return;
            }

            CMapToolTransformData transformData = portalSaveData.transform;

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
            placedObject.SetupPortal( portalSaveData.prefabName, PortalPrefabResourcePath, portalSaveData.targetMapId );
            ApplyPortalTargetMapId( portalInstance, portalSaveData.targetMapId );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 저장된 몬스터 데이터를 씬에 다시 배치한다.
        ///</summary>
        private void SpawnSavedMonster( CMapToolMonsterSaveData monsterSaveData )
        {
            if ( monsterSaveData == null || string.IsNullOrEmpty( monsterSaveData.prefabName ) )
            {
                return;
            }

            GameObject monsterPrefab = ResolveMonsterPrefab( monsterSaveData.prefabName, monsterSaveData.resourcePath );

            if ( monsterPrefab == null )
            {
                return;
            }

            CMapToolTransformData transformData = monsterSaveData.transform;

            if ( transformData == null )
            {
                transformData = BuildTransformData( monsterPrefab.transform );
            }

            Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
            Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
            Vector3 spawnScale = CreateVector3FromTransformData( transformData.scale, monsterPrefab.transform.localScale );
            GameObject monsterInstance = Instantiate( monsterPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
            monsterInstance.transform.localScale = spawnScale;
            monsterInstance.name = monsterPrefab.name;
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( monsterInstance );
            placedObject.SetupMonster( monsterSaveData.prefabName, monsterSaveData.resourcePath );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 현재 씬의 맵 정보를 JSON 파일로 저장한다.
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
        /// 현재 씬 상태를 저장 데이터 구조로 변환한다.
        ///</summary>
        private CMapToolSaveData BuildSaveData()
        {
            CMapToolSaveData saveData = new CMapToolSaveData();
            saveData.mapId = ResolveMapId();

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
        /// 포탈 배치 정보를 저장 데이터로 변환한다.
        ///</summary>
        private CMapToolPortalSaveData BuildPortalSaveData( MapToolPlacedObject placedObject )
        {
            CMapToolPortalSaveData portalSaveData = new CMapToolPortalSaveData();
            portalSaveData.prefabName = placedObject.GetPrefabName();
            portalSaveData.resourcePath = placedObject.GetResourcePath();
            portalSaveData.targetMapId = placedObject.GetTargetMapId();
            portalSaveData.transform = BuildTransformData( placedObject.transform );
            return portalSaveData;
        }

        ///<summary>
        /// 몬스터 배치 정보를 저장 데이터로 변환한다.
        ///</summary>
        private CMapToolMonsterSaveData BuildMonsterSaveData( MapToolPlacedObject placedObject )
        {
            CMapToolMonsterSaveData monsterSaveData = new CMapToolMonsterSaveData();
            monsterSaveData.prefabName = placedObject.GetPrefabName();
            monsterSaveData.resourcePath = placedObject.GetResourcePath();
            monsterSaveData.transform = BuildTransformData( placedObject.transform );
            return monsterSaveData;
        }

        ///<summary>
        /// 트랜스폼을 저장용 데이터로 변환한다.
        ///</summary>
        private CMapToolTransformData BuildTransformData( Transform targetTransform )
        {
            CMapToolTransformData transformData = new CMapToolTransformData();
            Vector3 position = targetTransform.position;
            Vector3 rotation = targetTransform.eulerAngles;
            Vector3 scale = targetTransform.localScale;
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
        /// 저장에 사용할 맵 ID를 결정한다.
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
        /// 맵 ID 입력 초기값을 결정한다.
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
        /// 현재 맵 ID에 대응하는 저장 파일 경로를 반환한다.
        ///</summary>
        private string GetSaveFilePath()
        {
            string mapId = ResolveMapId();
            string fileName = mapId + ".json";
            string result = Path.Combine( MapDataFolderPath, fileName );
            return result;
        }

        ///<summary>
        /// 배경 선택 패널을 토글한다.
        ///</summary>
        private void OnBackgroundModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( backgroundPanelRoot );
        }

        ///<summary>
        /// 몬스터 선택 패널을 토글한다.
        ///</summary>
        private void OnMonsterModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( monsterPanelRoot );
        }

        ///<summary>
        /// 포탈 설정 패널을 토글한다.
        ///</summary>
        private void OnPortalModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( portalPanelRoot );
        }

        ///<summary>
        /// 포탈 배치 모드를 시작한다.
        ///</summary>
        private void OnStartPortalPlacementButtonClicked()
        {
            string inputValue = portalTargetMapIdInputField.text;

            if ( string.IsNullOrWhiteSpace( inputValue ) )
            {
                inputValue = DefaultPortalTargetMapId;
                portalTargetMapIdInputField.text = inputValue;
            }

            selectedPortalTargetMapId = inputValue.Trim();
            BeginPortalPlacement();
        }

        ///<summary>
        /// 단일 편집 패널만 보이도록 UI를 전환한다.
        ///</summary>
        private void ToggleSinglePanel( RectTransform targetPanel )
        {
            if ( targetPanel == null )
            {
                return;
            }

            bool shouldActivate = targetPanel.gameObject.activeSelf == false;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );

            if ( shouldActivate )
            {
                SetPanelVisible( targetPanel, true );
            }
        }

        ///<summary>
        /// 선택한 배경 스프라이트를 씬에 적용한다.
        ///</summary>
        private void ApplyBackgroundSpriteByName( string spriteName )
        {
            ApplyBackgroundSpriteByName( spriteName, true );
        }

        ///<summary>
        /// 선택한 배경 스프라이트를 적용하고 필요 시 패널을 닫는다.
        ///</summary>
        private void ApplyBackgroundSpriteByName( string spriteName, bool shouldHidePanel )
        {
            if ( backgroundRenderer == null )
            {
                return;
            }

            if ( backgroundSpriteByName.TryGetValue( spriteName, out Sprite backgroundSprite ) == false )
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

            if ( shouldHidePanel )
            {
                SetPanelVisible( backgroundPanelRoot, false );
            }
        }

        ///<summary>
        /// 선택한 몬스터로 프리뷰 배치를 시작한다.
        ///</summary>
        private void BeginMonsterPlacement( string prefabName, string resourcePath )
        {
            StopPlacedObjectDrag();
            selectedMonsterPrefabName = prefabName;
            selectedMonsterResourcePath = resourcePath;
            currentMode = eMapToolMode.PLACE_MONSTER;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            RebuildPreviewInstance();
        }

        ///<summary>
        /// 입력된 목표 맵 ID로 포탈 프리뷰 배치를 시작한다.
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
        /// ESC 입력으로 배치 모드와 드래그 상태를 해제한다.
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
        /// 현재 배치 모드를 종료하고 프리뷰를 제거한다.
        ///</summary>
        private void CancelPlacementMode()
        {
            currentMode = eMapToolMode.NONE;
            selectedMonsterPrefabName = string.Empty;
            selectedMonsterResourcePath = string.Empty;
            DestroyPreviewInstance();
        }

        ///<summary>
        /// 현재 모드에 맞는 프리뷰 오브젝트를 다시 만든다.
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
        /// 현재 모드에 맞는 프리뷰 프리팹을 반환한다.
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
        /// 프리뷰 오브젝트를 반투명 비상호작용 상태로 바꾼다.
        ///</summary>
        private void ConfigurePreviewInstance( GameObject targetPreviewInstance )
        {
            ApplyPreviewVisual( targetPreviewInstance.transform );

            Collider2D[] colliders = targetPreviewInstance.GetComponentsInChildren<Collider2D>( true );
            int colliderCount = colliders.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                Collider2D colliderComponent = colliders[ index ];
                colliderComponent.enabled = false;
            }

            Rigidbody2D[] rigidbodies = targetPreviewInstance.GetComponentsInChildren<Rigidbody2D>( true );
            int rigidbodyCount = rigidbodies.Length;

            for ( int index = 0; index < rigidbodyCount; index++ )
            {
                Rigidbody2D rigidbodyComponent = rigidbodies[ index ];
                rigidbodyComponent.simulated = false;
            }
        }

        ///<summary>
        /// 프리뷰 렌더러를 반투명하게 조정한다.
        ///</summary>
        private void ApplyPreviewVisual( Transform rootTransform )
        {
            SpriteRenderer[] spriteRenderers = rootTransform.GetComponentsInChildren<SpriteRenderer>( true );
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
        /// 프리뷰 인스턴스를 제거한다.
        ///</summary>
        private void DestroyPreviewInstance()
        {
            if ( previewInstance == null )
            {
                return;
            }

            Destroy( previewInstance );
            previewInstance = null;
        }

        ///<summary>
        /// 프리뷰를 마우스 위치에 따라 이동시킨다.
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
        /// 배치된 오브젝트의 드래그 시작과 이동과 종료를 처리한다.
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
        /// 선택한 배치 오브젝트 드래그를 시작한다.
        ///</summary>
        private void StartPlacedObjectDrag( MapToolPlacedObject targetPlacedObject )
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            Vector3 currentObjectPosition = targetPlacedObject.transform.position;
            draggedObjectOffset = currentObjectPosition - mouseWorldPosition;
            draggedPlacedObject = targetPlacedObject;
            isDraggingPlacedObject = true;
        }

        ///<summary>
        /// 현재 배치 오브젝트 드래그 상태를 종료한다.
        ///</summary>
        private void StopPlacedObjectDrag()
        {
            draggedPlacedObject = null;
            draggedObjectOffset = Vector3.zero;
            isDraggingPlacedObject = false;
        }

        ///<summary>
        /// 마우스 아래의 배치 오브젝트를 찾아 반환한다.
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
        /// 좌클릭 입력으로 프리뷰 배치를 처리한다.
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
        /// 우클릭 입력으로 배치 오브젝트 삭제를 처리한다.
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
        /// 현재 마우스 위치에 포탈을 배치한다.
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
            placedObject.SetupPortal( portalPrefab.name, PortalPrefabResourcePath, selectedPortalTargetMapId );
            ApplyPortalTargetMapId( portalInstance, selectedPortalTargetMapId );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 현재 마우스 위치에 몬스터를 배치한다.
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
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( monsterInstance );
            placedObject.SetupMonster( selectedMonsterPrefabName, selectedMonsterResourcePath );
            placedObjects.Add( placedObject );
        }

        ///<summary>
        /// 포탈 컴포넌트에 목표 맵 ID를 반영한다.
        ///</summary>
        private void ApplyPortalTargetMapId( GameObject portalInstance, string targetMapId )
        {
            PortalObject portalObject = portalInstance.GetComponent<PortalObject>();

            if ( portalObject == null )
            {
                return;
            }

            portalObject.SetTargetSceneID( targetMapId );
        }

        ///<summary>
        /// 마우스 위치를 월드 좌표로 변환한다.
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
        /// 포인터가 UI 위에 있는지 확인한다.
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
        /// 이름이나 경로 기준으로 몬스터 프리팹을 반환한다.
        ///</summary>
        private GameObject ResolveMonsterPrefab( string prefabName, string resourcePath )
        {
            if ( string.IsNullOrEmpty( prefabName ) == false && monsterPrefabByName.TryGetValue( prefabName, out GameObject cachedPrefab ) )
            {
                return cachedPrefab;
            }

            if ( string.IsNullOrEmpty( resourcePath ) == false )
            {
                GameObject loadedPrefab = Resources.Load<GameObject>( resourcePath );
                return loadedPrefab;
            }

            return null;
        }

        ///<summary>
        /// 배치 오브젝트 메타데이터 컴포넌트를 보장한다.
        ///</summary>
        private MapToolPlacedObject EnsurePlacedObjectComponent( GameObject targetObject )
        {
            MapToolPlacedObject placedObject = targetObject.GetComponent<MapToolPlacedObject>();

            if ( placedObject != null )
            {
                return placedObject;
            }

            MapToolPlacedObject createdPlacedObject = targetObject.AddComponent<MapToolPlacedObject>();
            return createdPlacedObject;
        }

        ///<summary>
        /// 배치된 모든 오브젝트를 씬에서 제거한다.
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
        /// 선택한 배치 오브젝트를 씬과 목록에서 제거한다.
        ///</summary>
        private void RemovePlacedObject( MapToolPlacedObject placedObject )
        {
            if ( draggedPlacedObject == placedObject )
            {
                StopPlacedObjectDrag();
            }

            placedObjects.Remove( placedObject );
            Destroy( placedObject.gameObject );
        }

        ///<summary>
        /// 직렬화된 float 배열을 Vector3로 복원한다.
        ///</summary>
        private Vector3 CreateVector3FromTransformData( float[] values, Vector3 fallbackValue )
        {
            if ( values == null || values.Length < 3 )
            {
                return fallbackValue;
            }

            Vector3 result = new Vector3( values[ 0 ], values[ 1 ], values[ 2 ] );
            return result;
        }

        ///<summary>
        /// 패널 활성 상태를 안전하게 변경한다.
        ///</summary>
        private void SetPanelVisible( RectTransform panelRoot, bool isVisible )
        {
            if ( panelRoot == null )
            {
                return;
            }

            panelRoot.gameObject.SetActive( isVisible );
        }

        ///<summary>
        /// 선택 리스트 패널과 목록 루트를 생성하거나 재사용한다.
        ///</summary>
        private RectTransform EnsureSelectionPanel( RectTransform existingPanelRoot, string panelName, RectTransform canvasRectTransform, out RectTransform listRoot )
        {
            listRoot = null;

            if ( existingPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( canvasRectTransform, panelName );
                existingPanelRoot = foundPanelRoot;
            }

            if ( existingPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreateStandardPanel( panelName, canvasRectTransform );
                existingPanelRoot = createdPanelRoot;
            }

            RectTransform foundListRoot = EnsureScrollableListRoot( existingPanelRoot );
            listRoot = foundListRoot;
            return existingPanelRoot;
        }

        ///<summary>
        /// 목록 패널에 스크롤 가능한 리스트 루트를 보장한다.
        ///</summary>
        private RectTransform EnsureScrollableListRoot( RectTransform panelRoot )
        {
            ScrollRect existingScrollRect = panelRoot.GetComponentInChildren<ScrollRect>( true );

            if ( existingScrollRect != null && existingScrollRect.content != null )
            {
                RectTransform existingContent = existingScrollRect.content;
                return existingContent;
            }

            RectTransform existingListRoot = FindChildRectTransform( panelRoot, ListRootObjectName );

            if ( existingListRoot != null )
            {
                Destroy( existingListRoot.gameObject );
            }

            GameObject scrollRectObject = new GameObject( ScrollRectObjectName, typeof( RectTransform ), typeof( Image ), typeof( ScrollRect ) );
            RectTransform scrollRectTransform = scrollRectObject.GetComponent<RectTransform>();
            scrollRectTransform.SetParent( panelRoot, false );
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
        /// 포탈 입력 패널을 생성하거나 재사용한다.
        ///</summary>
        private RectTransform EnsurePortalPanel( RectTransform existingPanelRoot, RectTransform canvasRectTransform )
        {
            if ( existingPanelRoot == null )
            {
                RectTransform foundPanelRoot = FindChildRectTransform( canvasRectTransform, PortalPanelObjectName );
                existingPanelRoot = foundPanelRoot;
            }

            if ( existingPanelRoot == null )
            {
                RectTransform createdPanelRoot = CreateStandardPanel( PortalPanelObjectName, canvasRectTransform );
                existingPanelRoot = createdPanelRoot;
            }

            if ( portalTargetMapIdInputField == null )
            {
                TMP_InputField createdInputField = CreateInputField( "PortalTargetMapIdInputField", existingPanelRoot, "목표 맵 ID", DefaultPortalTargetMapId );
                portalTargetMapIdInputField = createdInputField;
            }

            if ( startPortalPlacementButton == null )
            {
                CButtonEx createdButton = CreateTextButton( "StartPortalPlacementButton", existingPanelRoot, "포탈 배치 시작", PanelWidth - ( PanelPadding * 2.0f ), PortalActionButtonHeight );
                RectTransform buttonRectTransform = createdButton.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.pivot = new Vector2( 0.5f, 1.0f );
                buttonRectTransform.anchoredPosition = new Vector2( 0.0f, -110.0f );
                startPortalPlacementButton = createdButton;
            }

            return existingPanelRoot;
        }

        ///<summary>
        /// 공통 패널 루트를 생성한다.
        ///</summary>
        private RectTransform CreateStandardPanel( string panelName, RectTransform canvasRectTransform )
        {
            RectTransform panelRoot = CreatePanelRoot( panelName, canvasRectTransform, new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ), new Vector2( 0.5f, 1.0f ) );
            Image panelImage = panelRoot.gameObject.AddComponent<Image>();
            panelImage.color = new Color( 0.12f, 0.14f, 0.18f, 0.92f );
            LayoutElement layoutElement = panelRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            panelRoot.sizeDelta = new Vector2( PanelWidth, PanelHeight );
            panelRoot.anchoredPosition = new Vector2( 0.0f, PanelTopOffset );
            return panelRoot;
        }

        ///<summary>
        /// 기본 RectTransform 루트를 생성한다.
        ///</summary>
        private RectTransform CreatePanelRoot( string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot )
        {
            GameObject panelObject = new GameObject( objectName, typeof( RectTransform ) );
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.SetParent( parent, false );
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            return rectTransform;
        }

        ///<summary>
        /// 공용 텍스트 버튼을 생성한다.
        ///</summary>
        private CButtonEx CreateTextButton( string objectName, RectTransform parent, string labelText, float width, float height )
        {
            GameObject buttonObject = new GameObject( objectName, typeof( RectTransform ), typeof( Image ), typeof( CButtonEx ) );
            RectTransform buttonRectTransform = buttonObject.GetComponent<RectTransform>();
            buttonRectTransform.SetParent( parent, false );
            buttonRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.pivot = new Vector2( 0.5f, 1.0f );
            buttonRectTransform.sizeDelta = new Vector2( width, height );
            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
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
            labelTextComponent.text = labelText;
            labelTextComponent.fontSize = 24.0f;
            labelTextComponent.alignment = TextAlignmentOptions.Center;
            labelTextComponent.color = Color.white;
            return buttonComponent;
        }

        ///<summary>
        /// TMP 입력 필드를 생성한다.
        ///</summary>
        private TMP_InputField CreateInputField( string objectName, RectTransform parent, string placeholderText, string defaultText )
        {
            GameObject inputFieldObject = new GameObject( objectName, typeof( RectTransform ), typeof( Image ), typeof( TMP_InputField ) );
            RectTransform inputFieldRectTransform = inputFieldObject.GetComponent<RectTransform>();
            inputFieldRectTransform.SetParent( parent, false );
            inputFieldRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.pivot = new Vector2( 0.5f, 1.0f );
            inputFieldRectTransform.sizeDelta = new Vector2( PanelWidth - ( PanelPadding * 2.0f ), 54.0f );
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
            textComponent.fontSize = 24.0f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.text = defaultText;

            GameObject placeholderObject = new GameObject( InputFieldPlaceholderObjectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
            RectTransform placeholderRectTransform = placeholderObject.GetComponent<RectTransform>();
            placeholderRectTransform.SetParent( viewportRectTransform, false );
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.offsetMin = Vector2.zero;
            placeholderRectTransform.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholderComponent = placeholderObject.GetComponent<TextMeshProUGUI>();
            placeholderComponent.fontSize = 24.0f;
            placeholderComponent.text = placeholderText;
            placeholderComponent.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderComponent.color = new Color( 1.0f, 1.0f, 1.0f, 0.45f );

            inputField.textViewport = viewportRectTransform;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;
            inputField.text = defaultText;
            return inputField;
        }

        ///<summary>
        /// 자식 RectTransform을 이름으로 검색한다.
        ///</summary>
        private RectTransform FindChildRectTransform( RectTransform parent, string childName )
        {
            int childCount = parent.childCount;

            for ( int index = 0; index < childCount; index++ )
            {
                Transform childTransform = parent.GetChild( index );
                RectTransform childRectTransform = childTransform as RectTransform;

                if ( childRectTransform == null )
                {
                    continue;
                }

                if ( childRectTransform.name == childName )
                {
                    return childRectTransform;
                }
            }

            return null;
        }

        ///<summary>
        /// 지정한 UI 루트 아래 모든 자식을 제거한다.
        ///</summary>
        private void ClearChildren( RectTransform parent )
        {
            if ( parent == null )
            {
                return;
            }

            int childCount = parent.childCount;

            for ( int index = childCount - 1; index >= 0; index-- )
            {
                Transform childTransform = parent.GetChild( index );
                Destroy( childTransform.gameObject );
            }
        }
    }
}
