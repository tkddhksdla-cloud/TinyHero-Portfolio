using System.Collections.Generic;
using System.Collections;
using System.IO;
using TMPro;
using TinyHero.Core;
using TinyHero.Player;
using TinyHero.Skill;
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
            PLACE_MONSTER,
            PLACE_NPC,
            SET_RIGHT_BOUNDARY
        }

        private const string BackgroundPrefabResourcePath = "Prefabs/BackgroundObject/BackgroundObject";
        private const string PortalPrefabResourcePath = "Prefabs/Portal/PortalObject";
        private const string MonsterPrefabResourceFolderPath = "Prefabs/Character/Monster";
        private const string NpcPrefabResourceFolderPath = "Prefabs/Character/NPC";
        private const string BackgroundSpriteResourceFolderPath = "RawImages/BG";
        private const string MapDataFolderPath = "Assets/Resources/MapData";
        private const string DefaultPortalTargetMapId = "SceneMap";
        private const string PortalIdPrefix = "Portal_";
        private const string ToolbarObjectName = "Toolbar";
        private const string BackgroundPanelObjectName = "BackgroundPanel";
        private const string MonsterPanelObjectName = "MonsterPanel";
        private const string NpcPanelObjectName = "NpcPanel";
        private const string PortalPanelObjectName = "PortalPanel";
        private const string SkillTestPanelObjectName = "SkillTestPanel";
        private const string BottomMenuObjectName = "BottomMenu";
        private const string PortalIdTitleObjectName = "PortalIdTitle";
        private const string PortalTargetMapTitleObjectName = "PortalTargetMapTitle";
        private const string PortalTargetPortalTitleObjectName = "PortalTargetPortalTitle";
        private const string MapIdTitleObjectName = "MapIdTitle";
        private const string MapNameTitleObjectName = "MapNameTitle";
        private const string BgmClipNameTitleObjectName = "BgmClipNameTitle";
        private const string MapInfoPanelObjectName = "MapInfoPanel";
        private const string LoadMapPanelObjectName = "LoadMapPanel";
        private const string SaveConfirmPopupObjectName = "SaveConfirmPopup";
        private const string SaveConfirmMessageObjectName = "SaveConfirmMessage";
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
        private const float BottomMenuBottomOffset = 24.0f;
        private const float ListButtonHeight = 44.0f;
        private const float PortalActionButtonHeight = 50.0f;
        private const float PortalFieldTitleFontSize = 18.0f;
        private const float PanelPadding = 14.0f;
        private const float ItemSpacing = 8.0f;
        private const float MapInfoPanelWidth = 360.0f;
        private const float MapInfoPanelHeight = 494.0f;
        private const float MapInfoInputWidth = MapInfoPanelWidth - ( PanelPadding * 2.0f );
        private const float MapInfoButtonWidth = ( MapInfoInputWidth - 12.0f ) * 0.5f;
        private const float MapInfoPanelLeftOffset = 20.0f;
        private const float MapInfoPanelTopOffset = -20.0f;
        private const float LoadPanelWidth = 360.0f;
        private const float LoadPanelHeight = 360.0f;
        private const float LoadPanelLeftOffset = 400.0f;
        private const float LoadPanelTopOffset = -200.0f;
        private const float SaveConfirmPopupWidth = 420.0f;
        private const float SaveConfirmPopupHeight = 220.0f;
        private const float SaveConfirmButtonWidth = 150.0f;
        private const float SaveConfirmButtonHeight = 54.0f;
        private const float SkillPreviewDisplayDurationSeconds = 1.2f;
        private const float MapOverviewCameraPaddingMultiplier = 1.04f;
        private const float MinimumOrthographicSize = 0.1f;
        private const int MouseButtonLeft = 0;
        private const int MouseButtonRight = 1;
        private const int SortingOrderPanel = 10;
        private const KeyCode NpcFlipKey = KeyCode.X;

        [Header( "Map" )]
        [SerializeField] private string customMapId;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private WorldSpaceBackgroundFitter backgroundFitter;
        [SerializeField] private CMapBackgroundLayoutController backgroundLayoutController;
        [SerializeField] private CMapToolBackgroundColliderVisualizer backgroundColliderVisualizer;
        [SerializeField] private Vector3 defaultPlayerSpawnPosition = Vector3.zero;

        [Header( "UI" )]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private EventSystem targetEventSystem;
        [SerializeField] private RectTransform toolbarRoot;
        [SerializeField] private RectTransform bottomMenuRoot;
        [SerializeField] private RectTransform backgroundPanelRoot;
        [SerializeField] private RectTransform monsterPanelRoot;
        [SerializeField] private RectTransform npcPanelRoot;
        [SerializeField] private RectTransform portalPanelRoot;
        [SerializeField] private RectTransform mapInfoPanelRoot;
        [SerializeField] private RectTransform loadMapPanelRoot;
        [SerializeField] private RectTransform skillTestPanelRoot;
        [SerializeField] private RectTransform saveConfirmPopupRoot;
        [SerializeField] private RectTransform backgroundListRoot;
        [SerializeField] private RectTransform monsterListRoot;
        [SerializeField] private RectTransform npcListRoot;
        [SerializeField] private RectTransform loadMapListRoot;
        [SerializeField] private RectTransform skillTestListRoot;
        [SerializeField] private TMP_Text saveConfirmMessageText;
        [SerializeField] private TMP_InputField mapIdInputField;
        [SerializeField] private TMP_InputField mapNameInputField;
        [SerializeField] private TMP_InputField bgmClipNameInputField;
        [SerializeField] private TMP_InputField portalIdInputField;
        [SerializeField] private TMP_InputField portalTargetMapIdInputField;
        [SerializeField] private TMP_InputField portalTargetPortalIdInputField;
        [SerializeField] private Toggle disableMonsterBehaviorToggle;
        [SerializeField] private Toggle disableMonsterContactHitToggle;
        [SerializeField] private CButtonEx backgroundModeButton;
        [SerializeField] private CButtonEx monsterModeButton;
        [SerializeField] private CButtonEx npcModeButton;
        [SerializeField] private CButtonEx portalModeButton;
        [SerializeField] private CButtonEx skillTestModeButton;
        [SerializeField] private CButtonEx clearObjectsButton;
        [SerializeField] private CButtonEx setRightBoundaryButton;
        [SerializeField] private CButtonEx clearRightBoundaryButton;
        [SerializeField] private CButtonEx showMapOverviewButton;
        [SerializeField] private CButtonEx restoreCameraViewButton;
        [SerializeField] private CButtonEx saveMapButton;
        [SerializeField] private CButtonEx loadMapButton;
        [SerializeField] private CButtonEx confirmSaveButton;
        [SerializeField] private CButtonEx cancelSaveButton;
        [SerializeField] private CButtonEx startPortalPlacementButton;
        [SerializeField] private CSkillManager skillManager;
        [SerializeField] private CMapToolSkillRangeVisualizer hoverSkillRangeVisualizer;
        [SerializeField] private CMapToolSkillRangeVisualizer activeSkillRangeVisualizer;

        private readonly List<MapToolPlacedObject> placedObjects = new List<MapToolPlacedObject>();
        private readonly List<Sprite> backgroundSprites = new List<Sprite>();
        private readonly List<GameObject> monsterPrefabs = new List<GameObject>();
        private readonly List<GameObject> npcPrefabs = new List<GameObject>();
        private readonly Dictionary<string, Sprite> backgroundSpriteByName = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> npcPrefabByName = new Dictionary<string, GameObject>();
        private eMapToolMode currentMode;
        private GameObject previewInstance;
        private MapToolPlacedObject draggedPlacedObject;
        private Vector3 draggedObjectOffset;
        private string selectedMonsterResourcePath = string.Empty;
        private string selectedMonsterPrefabName = string.Empty;
        private string selectedNpcResourcePath = string.Empty;
        private string selectedNpcPrefabName = string.Empty;
        private string selectedPortalId = string.Empty;
        private string selectedPortalTargetMapId = DefaultPortalTargetMapId;
        private string selectedPortalTargetPortalId = string.Empty;
        private float selectedNpcFacingSignX = 1.0f;
        private bool isMonsterBehaviorDisabledInMapTool;
        private bool isMonsterContactHitDisabledInMapTool;
        private bool isDraggingPlacedObject;
        private bool hasCachedOriginalCameraView;
        private bool wasCameraFollowEnabledBeforeOverview;
        private float cachedOriginalOrthographicSize;
        private Vector3 cachedOriginalCameraPosition;
        private CSkillDefinition hoveredSkillDefinition;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveSceneReferences();
            EnsureBackgroundObjectExists();
            EnsureBackgroundLayoutControllerExists();
            EnsureBackgroundColliderVisualizerExists();
            EnsurePlayerObjectExists();
            EnsureCameraFollowControllerExists();
            EnsureSkillManagerExists();
            EnsureUiRootExists();
            EnsureToolbarExists();
            EnsureBottomMenuExists();
            EnsurePanelsExist();
            EnsureMapInfoPanelExists();
            EnsureLoadMapPanelExists();
            EnsureSaveConfirmPopupExists();
            EnsureSkillRangeVisualizersExist();
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
            RebuildNpcPanel();
            RebuildLoadMapPanel();
            RebuildSkillTestPanel();
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( npcPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );
            SetPanelVisible( saveConfirmPopupRoot, false );
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
            HandleNpcFlipInput();
            HandleDeleteInput();
            UpdateSkillPreviewTarget();
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

            if ( backgroundLayoutController == null && backgroundRenderer != null )
            {
                CMapBackgroundLayoutController resolvedLayoutController = backgroundRenderer.GetComponent<CMapBackgroundLayoutController>();
                backgroundLayoutController = resolvedLayoutController;
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
            backgroundLayoutController = backgroundObject.GetComponent<CMapBackgroundLayoutController>();
        }

        ///<summary>
        /// 배경 레이아웃 컨트롤러 존재 보장
        ///</summary>
        private void EnsureBackgroundLayoutControllerExists()
        {
            if ( backgroundRenderer == null )
            {
                return;
            }

            if ( backgroundLayoutController == null )
            {
                CMapBackgroundLayoutController resolvedLayoutController = backgroundRenderer.GetComponent<CMapBackgroundLayoutController>();

                if ( resolvedLayoutController == null )
                {
                    resolvedLayoutController = backgroundRenderer.gameObject.AddComponent<CMapBackgroundLayoutController>();
                }

                backgroundLayoutController = resolvedLayoutController;
            }

            backgroundLayoutController.RefreshLayout();
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
            CGameManager gameManager = CGameManager.Instance;
            bool hasPlayerController = gameManager.EnsurePlayerForActiveScene( defaultPlayerSpawnPosition, out PlayerController playerController );

            if ( hasPlayerController )
            {
                ApplyMonsterContactHitEnabledToPlayer( playerController );
            }
        }

        ///<summary>
        /// 카메라 플레이어 추적 컴포넌트 존재 보장
        ///</summary>
        private void EnsureCameraFollowControllerExists()
        {
            if ( worldCamera == null )
            {
                return;
            }

            CPlayerCameraFollowController cameraFollowController = worldCamera.GetComponent<CPlayerCameraFollowController>();

            if ( cameraFollowController == null )
            {
                cameraFollowController = worldCamera.gameObject.AddComponent<CPlayerCameraFollowController>();
            }

            CGameManager gameManager = CGameManager.Instance;
            gameManager.TryGetActivePlayerController( out PlayerController playerController );

            if ( playerController == null )
            {
                return;
            }

            cameraFollowController.SetTarget( playerController.transform );
        }

        ///<summary>
        /// 플레이어 접촉 피격 토글 적용
        ///</summary>
        private void ApplyMonsterContactHitToggleToPlayer()
        {
            CGameManager gameManager = CGameManager.Instance;
            gameManager.TryGetActivePlayerController( out PlayerController playerController );
            ApplyMonsterContactHitEnabledToPlayer( playerController );
        }

        ///<summary>
        /// 플레이어 접촉 피격 활성 상태 적용
        ///</summary>
        private void ApplyMonsterContactHitEnabledToPlayer( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            bool isEnabled = isMonsterContactHitDisabledInMapTool == false;
            _playerController.SetMonsterContactHitEnabled( isEnabled );
        }

        ///<summary>
        /// 스킬 매니저 참조 보장
        ///</summary>
        private void EnsureSkillManagerExists()
        {
            if ( skillManager != null )
            {
                return;
            }

            CGameManager gameManager = CGameManager.Instance;
            bool hasRuntimeContext = gameManager.TryGetPlayerRuntimeContext( out CPlayerRuntimeContext playerRuntimeContext );
            skillManager = hasRuntimeContext ? playerRuntimeContext.GetSkillManager() : null;
        }

        ///<summary>
        /// 스킬 범위 시각화 참조 보장
        ///</summary>
        private void EnsureSkillRangeVisualizersExist()
        {
            if ( hoverSkillRangeVisualizer == null )
            {
                CMapToolSkillRangeVisualizer createdHoverVisualizer = CreateSkillRangeVisualizer( "HoverSkillRangeVisualizer" );
                hoverSkillRangeVisualizer = createdHoverVisualizer;
            }

            if ( activeSkillRangeVisualizer == null )
            {
                CMapToolSkillRangeVisualizer createdActiveVisualizer = CreateSkillRangeVisualizer( "ActiveSkillRangeVisualizer" );
                activeSkillRangeVisualizer = createdActiveVisualizer;
            }
        }

        ///<summary>
        /// 스킬 범위 시각화 오브젝트 생성
        ///</summary>
        private CMapToolSkillRangeVisualizer CreateSkillRangeVisualizer( string _objectName )
        {
            GameObject visualizerObject = new GameObject( _objectName );
            visualizerObject.transform.SetParent( transform, false );
            CMapToolSkillRangeVisualizer createdVisualizer = visualizerObject.AddComponent<CMapToolSkillRangeVisualizer>();
            return createdVisualizer;
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

            if ( npcModeButton == null )
            {
                CButtonEx createdNpcButton = CreateTextButton( "NpcModeButton", toolbarRoot, "NPC 배치", ToolbarButtonWidth, ToolbarButtonHeight );
                npcModeButton = createdNpcButton;
            }

            if ( skillTestModeButton == null )
            {
                CButtonEx createdSkillTestButton = CreateTextButton( "SkillTestModeButton", toolbarRoot, "스킬 테스트", ToolbarButtonWidth, ToolbarButtonHeight );
                skillTestModeButton = createdSkillTestButton;
            }

            if ( clearObjectsButton == null )
            {
                CButtonEx createdClearObjectsButton = CreateTextButton( "ClearObjectsButton", toolbarRoot, "오브젝트 초기화", ToolbarButtonWidth, ToolbarButtonHeight );
                clearObjectsButton = createdClearObjectsButton;
            }
        }

        ///<summary>
        /// 하단 메뉴 존재 보장
        ///</summary>
        private void EnsureBottomMenuExists()
        {
            if ( bottomMenuRoot != null )
            {
                return;
            }

            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();
            RectTransform bottomMenuRectTransform = FindChildRectTransform( canvasRectTransform, BottomMenuObjectName );

            if ( bottomMenuRectTransform == null )
            {
                bottomMenuRectTransform = CreatePanelRoot( BottomMenuObjectName, canvasRectTransform, new Vector2( 0.5f, 0.0f ), new Vector2( 0.5f, 0.0f ), new Vector2( 0.5f, 0.0f ) );
                HorizontalLayoutGroup horizontalLayoutGroup = bottomMenuRectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontalLayoutGroup.spacing = ToolbarSpacing;
                horizontalLayoutGroup.childControlWidth = false;
                horizontalLayoutGroup.childControlHeight = false;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.childForceExpandHeight = false;
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                ContentSizeFitter contentSizeFitter = bottomMenuRectTransform.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                bottomMenuRectTransform.anchoredPosition = new Vector2( 0.0f, BottomMenuBottomOffset );
            }

            bottomMenuRoot = bottomMenuRectTransform;

            if ( setRightBoundaryButton == null )
            {
                setRightBoundaryButton = CreateTextButton( "SetRightBoundaryButton", bottomMenuRoot, "우측 경계 지정", ToolbarButtonWidth, ToolbarButtonHeight );
            }

            if ( clearRightBoundaryButton == null )
            {
                clearRightBoundaryButton = CreateTextButton( "ClearRightBoundaryButton", bottomMenuRoot, "우측 경계 기본값", ToolbarButtonWidth, ToolbarButtonHeight );
            }

            if ( showMapOverviewButton == null )
            {
                showMapOverviewButton = CreateTextButton( "ShowMapOverviewButton", bottomMenuRoot, "맵 전체 보기", ToolbarButtonWidth, ToolbarButtonHeight );
            }

            if ( restoreCameraViewButton == null )
            {
                restoreCameraViewButton = CreateTextButton( "RestoreCameraViewButton", bottomMenuRoot, "카메라 원래 크기", ToolbarButtonWidth, ToolbarButtonHeight );
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
            npcPanelRoot = EnsureSelectionPanel( npcPanelRoot, NpcPanelObjectName, canvasRectTransform, out npcListRoot );
            portalPanelRoot = EnsurePortalPanel( portalPanelRoot, canvasRectTransform );
            skillTestPanelRoot = EnsureSelectionPanel( skillTestPanelRoot, SkillTestPanelObjectName, canvasRectTransform, out skillTestListRoot );
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

            if ( bgmClipNameInputField == null )
            {
                TMP_InputField createdBgmClipNameInputField = CreateInputField( "BgmClipNameInputField", mapInfoPanelRoot, "BGM 클립 이름", string.Empty );
                RectTransform bgmClipNameRectTransform = createdBgmClipNameInputField.GetComponent<RectTransform>();
                bgmClipNameRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                bgmClipNameRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                bgmClipNameRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                bgmClipNameRectTransform.sizeDelta = new Vector2( MapInfoInputWidth, 54.0f );
                bgmClipNameRectTransform.anchoredPosition = new Vector2( PanelPadding, -264.0f );
                bgmClipNameInputField = createdBgmClipNameInputField;
            }

            EnsureInputFieldTitle( BgmClipNameTitleObjectName, mapInfoPanelRoot, "BGM Clip Name", new Vector2( 0.0f, -232.0f ) );
            UpdateInputFieldPlaceholder( bgmClipNameInputField, "Audio/BGM 클립 이름" );

            if ( saveMapButton == null )
            {
                CButtonEx createdSaveMapButton = CreateTextButton( "SaveMapButton", mapInfoPanelRoot, "맵 저장", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform saveButtonRectTransform = createdSaveMapButton.GetComponent<RectTransform>();
                saveButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                saveButtonRectTransform.anchoredPosition = new Vector2( PanelPadding, -342.0f );
                saveMapButton = createdSaveMapButton;
            }

            if ( loadMapButton == null )
            {
                CButtonEx createdLoadMapButton = CreateTextButton( "LoadMapButton", mapInfoPanelRoot, "맵 불러오기", MapInfoButtonWidth, ToolbarButtonHeight );
                RectTransform loadButtonRectTransform = createdLoadMapButton.GetComponent<RectTransform>();
                loadButtonRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                loadButtonRectTransform.anchoredPosition = new Vector2( PanelPadding + MapInfoButtonWidth + 12.0f, -342.0f );
                loadMapButton = createdLoadMapButton;
            }

            if ( disableMonsterBehaviorToggle == null )
            {
                Toggle createdDisableMonsterBehaviorToggle = CreateToggle( "DisableMonsterBehaviorToggle", mapInfoPanelRoot, "몬스터 행동 정지" );
                RectTransform toggleRectTransform = createdDisableMonsterBehaviorToggle.GetComponent<RectTransform>();
                toggleRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchoredPosition = new Vector2( PanelPadding, -406.0f );
                disableMonsterBehaviorToggle = createdDisableMonsterBehaviorToggle;
            }

            disableMonsterBehaviorToggle.isOn = isMonsterBehaviorDisabledInMapTool;

            if ( disableMonsterContactHitToggle == null )
            {
                Toggle createdDisableMonsterContactHitToggle = CreateToggle( "DisableMonsterContactHitToggle", mapInfoPanelRoot, "몬스터 접촉 피격 끄기" );
                RectTransform toggleRectTransform = createdDisableMonsterContactHitToggle.GetComponent<RectTransform>();
                toggleRectTransform.anchorMin = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchorMax = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.pivot = new Vector2( 0.0f, 1.0f );
                toggleRectTransform.anchoredPosition = new Vector2( PanelPadding, -440.0f );
                disableMonsterContactHitToggle = createdDisableMonsterContactHitToggle;
            }

            disableMonsterContactHitToggle.isOn = isMonsterContactHitDisabledInMapTool;
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

            if ( bgmClipNameInputField != null && string.IsNullOrWhiteSpace( bgmClipNameInputField.text ) )
            {
                bgmClipNameInputField.text = string.Empty;
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

            if ( disableMonsterContactHitToggle != null )
            {
                disableMonsterContactHitToggle.isOn = isMonsterContactHitDisabledInMapTool;
            }

            ApplyMonsterContactHitToggleToPlayer();
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
            npcModeButton.onClick.RemoveAllListeners();
            npcModeButton.onClick.AddListener( OnNpcModeButtonClicked );
            portalModeButton.onClick.RemoveAllListeners();
            portalModeButton.onClick.AddListener( OnPortalModeButtonClicked );
            skillTestModeButton.onClick.RemoveAllListeners();
            skillTestModeButton.onClick.AddListener( OnSkillTestModeButtonClicked );
            clearObjectsButton.onClick.RemoveAllListeners();
            clearObjectsButton.onClick.AddListener( OnClearObjectsButtonClicked );
            setRightBoundaryButton.onClick.RemoveAllListeners();
            setRightBoundaryButton.onClick.AddListener( BeginRightBoundaryPlacement );
            clearRightBoundaryButton.onClick.RemoveAllListeners();
            clearRightBoundaryButton.onClick.AddListener( ClearRightBoundaryOverride );
            showMapOverviewButton.onClick.RemoveAllListeners();
            showMapOverviewButton.onClick.AddListener( ApplyMapOverviewCameraView );
            restoreCameraViewButton.onClick.RemoveAllListeners();
            restoreCameraViewButton.onClick.AddListener( RestoreOriginalCameraView );
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

            if ( disableMonsterContactHitToggle != null )
            {
                disableMonsterContactHitToggle.onValueChanged.RemoveAllListeners();
                disableMonsterContactHitToggle.onValueChanged.AddListener( OnDisableMonsterContactHitToggleValueChanged );
            }

            if ( confirmSaveButton != null )
            {
                confirmSaveButton.onClick.RemoveAllListeners();
                confirmSaveButton.onClick.AddListener( OnConfirmSaveButtonClicked );
            }

            if ( cancelSaveButton != null )
            {
                cancelSaveButton.onClick.RemoveAllListeners();
                cancelSaveButton.onClick.AddListener( OnCancelSaveButtonClicked );
            }
        }

        ///<summary>
        /// 저장 확인 팝업 존재 보장
        ///</summary>
        private void EnsureSaveConfirmPopupExists()
        {
            RectTransform canvasRectTransform = rootCanvas.GetComponent<RectTransform>();

            if ( saveConfirmPopupRoot == null )
            {
                RectTransform foundPopupRoot = FindChildRectTransform( canvasRectTransform, SaveConfirmPopupObjectName );
                saveConfirmPopupRoot = foundPopupRoot;
            }

            if ( saveConfirmPopupRoot == null )
            {
                RectTransform createdPopupRoot = CreatePanelRoot( SaveConfirmPopupObjectName, canvasRectTransform, new Vector2( 0.5f, 0.5f ), new Vector2( 0.5f, 0.5f ), new Vector2( 0.5f, 0.5f ) );
                Image popupImage = createdPopupRoot.gameObject.AddComponent<Image>();
                popupImage.color = new Color( 0.12f, 0.14f, 0.18f, 0.97f );
                LayoutElement layoutElement = createdPopupRoot.gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                createdPopupRoot.sizeDelta = new Vector2( SaveConfirmPopupWidth, SaveConfirmPopupHeight );
                createdPopupRoot.anchoredPosition = Vector2.zero;
                saveConfirmPopupRoot = createdPopupRoot;
            }

            if ( saveConfirmMessageText == null )
            {
                GameObject messageObject = new GameObject( SaveConfirmMessageObjectName, typeof( RectTransform ), typeof( TextMeshProUGUI ) );
                RectTransform messageRectTransform = messageObject.GetComponent<RectTransform>();
                messageRectTransform.SetParent( saveConfirmPopupRoot, false );
                messageRectTransform.anchorMin = new Vector2( 0.5f, 1.0f );
                messageRectTransform.anchorMax = new Vector2( 0.5f, 1.0f );
                messageRectTransform.pivot = new Vector2( 0.5f, 1.0f );
                messageRectTransform.sizeDelta = new Vector2( SaveConfirmPopupWidth - 40.0f, 96.0f );
                messageRectTransform.anchoredPosition = new Vector2( 0.0f, -28.0f );
                TextMeshProUGUI createdMessageText = messageObject.GetComponent<TextMeshProUGUI>();
                createdMessageText.fontSize = 24.0f;
                createdMessageText.alignment = TextAlignmentOptions.Center;
                createdMessageText.color = Color.white;
                createdMessageText.text = "이미 존재하는 맵 데이터입니다.\n덮어쓰시겠습니까?";
                saveConfirmMessageText = createdMessageText;
            }

            if ( confirmSaveButton == null )
            {
                CButtonEx createdConfirmSaveButton = CreateTextButton( "ConfirmSaveButton", saveConfirmPopupRoot, "저장", SaveConfirmButtonWidth, SaveConfirmButtonHeight );
                RectTransform buttonRectTransform = createdConfirmSaveButton.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.anchorMax = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.pivot = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.anchoredPosition = new Vector2( -88.0f, 24.0f );
                confirmSaveButton = createdConfirmSaveButton;
            }

            if ( cancelSaveButton == null )
            {
                CButtonEx createdCancelSaveButton = CreateTextButton( "CancelSaveButton", saveConfirmPopupRoot, "취소", SaveConfirmButtonWidth, SaveConfirmButtonHeight );
                RectTransform buttonRectTransform = createdCancelSaveButton.GetComponent<RectTransform>();
                buttonRectTransform.anchorMin = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.anchorMax = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.pivot = new Vector2( 0.5f, 0.0f );
                buttonRectTransform.anchoredPosition = new Vector2( 88.0f, 24.0f );
                cancelSaveButton = createdCancelSaveButton;
            }
        }

        ///<summary>
        /// 리소스 목록 로드
        ///</summary>
        private void LoadResourceCatalog()
        {
            backgroundSprites.Clear();
            monsterPrefabs.Clear();
            npcPrefabs.Clear();
            backgroundSpriteByName.Clear();
            monsterPrefabByName.Clear();
            npcPrefabByName.Clear();

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

            GameObject[] loadedNpcPrefabs = Resources.LoadAll<GameObject>( NpcPrefabResourceFolderPath );
            int npcPrefabCount = loadedNpcPrefabs.Length;

            for ( int index = 0; index < npcPrefabCount; index++ )
            {
                GameObject npcPrefab = loadedNpcPrefabs[ index ];

                if ( npcPrefab == null )
                {
                    continue;
                }

                npcPrefabs.Add( npcPrefab );
                npcPrefabByName[ npcPrefab.name ] = npcPrefab;
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
        /// NPC 패널 재구성
        ///</summary>
        private void RebuildNpcPanel()
        {
            ClearChildren( npcListRoot );
            int npcCount = npcPrefabs.Count;

            for ( int index = 0; index < npcCount; index++ )
            {
                GameObject npcPrefab = npcPrefabs[ index ];
                string prefabName = npcPrefab.name;
                string resourcePath = NpcPrefabResourceFolderPath + "/" + prefabName;
                CButtonEx listButton = CreateTextButton( ButtonObjectPrefix + prefabName, npcListRoot, prefabName, PanelWidth - ( PanelPadding * 2.0f ), ListButtonHeight );
                listButton.onClick.AddListener( delegate
                {
                    BeginNpcPlacement( prefabName, resourcePath );
                } );
            }
        }

        ///<summary>
        /// 스킬 테스트 패널 재구성
        ///</summary>
        private void RebuildSkillTestPanel()
        {
            ClearChildren( skillTestListRoot );

            if ( skillManager == null || skillTestListRoot == null )
            {
                return;
            }

            int skillCount = skillManager.GetSkillCount();

            for ( int index = 0; index < skillCount; index++ )
            {
                CSkillRuntimeData runtimeData = skillManager.GetSkillRuntimeData( index );

                if ( runtimeData == null )
                {
                    continue;
                }

                CSkillDefinition skillDefinition = runtimeData.GetSkillDefinition();

                if ( skillDefinition == null )
                {
                    continue;
                }

                string skillId = skillDefinition.GetSkillId();
                string skillLabel = BuildSkillTestButtonLabel( skillDefinition );
                CButtonEx listButton = CreateTextButton( ButtonObjectPrefix + skillId, skillTestListRoot, skillLabel, PanelWidth - ( PanelPadding * 2.0f ), ListButtonHeight * 1.45f );
                CMapToolSkillTestItemUI itemUi = listButton.gameObject.AddComponent<CMapToolSkillTestItemUI>();
                itemUi.Initialize( this, skillDefinition );
                listButton.onClick.AddListener( delegate
                {
                    OnSkillTestItemClicked( skillDefinition );
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
            SetPanelVisible( skillTestPanelRoot, false );
            LoadSavedMapData();
        }

        ///<summary>
        /// 저장 맵 버튼 클릭 처리
        ///</summary>
        private void OnSaveMapButtonClicked()
        {
            bool isExistingMapData = IsCurrentMapDataAlreadySaved();

            if ( isExistingMapData )
            {
                ShowSaveConfirmPopup();
                return;
            }

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
        /// 몬스터 접촉 피격 토글 값 변경 처리
        ///</summary>
        private void OnDisableMonsterContactHitToggleValueChanged( bool _isOn )
        {
            isMonsterContactHitDisabledInMapTool = _isOn;
            ApplyMonsterContactHitToggleToPlayer();
        }

        ///<summary>
        /// 저장 확인 버튼 클릭 처리
        ///</summary>
        private void OnConfirmSaveButtonClicked()
        {
            HideSaveConfirmPopup();
            SaveMapData();
            RebuildLoadMapPanel();
        }

        ///<summary>
        /// 저장 취소 버튼 클릭 처리
        ///</summary>
        private void OnCancelSaveButtonClicked()
        {
            HideSaveConfirmPopup();
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
            hoveredSkillDefinition = null;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, shouldActivate );

            if ( hoverSkillRangeVisualizer != null )
            {
                hoverSkillRangeVisualizer.HidePreview();
            }
        }

        ///<summary>
        /// 저장된 맵 데이터 로드
        ///</summary>
        private void LoadSavedMapData()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            RestoreOriginalCameraViewForMapLoad();
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

            if ( _loadedData.npcs == null )
            {
                _loadedData.npcs = new List<CMapToolNpcSaveData>();
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

            if ( bgmClipNameInputField != null )
            {
                string resolvedBgmClipName = string.IsNullOrWhiteSpace( _loadedData.bgmClipName ) ? string.Empty : _loadedData.bgmClipName.Trim();
                bgmClipNameInputField.text = resolvedBgmClipName;
                PreviewLoadedBgm( resolvedBgmClipName );
            }

            if ( string.IsNullOrEmpty( _loadedData.backgroundSpriteName ) == false )
            {
                ApplyBackgroundSpriteByName( _loadedData.backgroundSpriteName, false );
            }

            ApplyLoadedRightBoundary( _loadedData );

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

            int npcCount = _loadedData.npcs.Count;

            for ( int index = 0; index < npcCount; index++ )
            {
                CMapToolNpcSaveData npcSaveData = _loadedData.npcs[ index ];
                SpawnSavedNpc( npcSaveData );
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
        /// 저장된 NPC 생성
        ///</summary>
        private void SpawnSavedNpc( CMapToolNpcSaveData _npcSaveData )
        {
            if ( _npcSaveData == null || string.IsNullOrEmpty( _npcSaveData.prefabName ) )
            {
                return;
            }

            GameObject npcPrefab = ResolveNpcPrefab( _npcSaveData.prefabName, _npcSaveData.resourcePath );

            if ( npcPrefab == null )
            {
                return;
            }

            CMapToolTransformData transformData = _npcSaveData.transform;

            if ( transformData == null )
            {
                transformData = BuildTransformData( npcPrefab.transform );
            }

            Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
            Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
            Vector3 spawnScale = CreateVector3FromTransformData( transformData.scale, npcPrefab.transform.localScale );
            GameObject npcInstance = Instantiate( npcPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
            npcInstance.transform.localScale = spawnScale;
            npcInstance.name = npcPrefab.name;
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( npcInstance );
            placedObject.SetupNpc( _npcSaveData.prefabName, _npcSaveData.resourcePath );
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
        /// 현재 맵 데이터 저장 여부 확인
        ///</summary>
        private bool IsCurrentMapDataAlreadySaved()
        {
            string saveFilePath = GetSaveFilePath();
            bool result = File.Exists( saveFilePath );
            return result;
        }

        ///<summary>
        /// 저장 확인 팝업 표시
        ///</summary>
        private void ShowSaveConfirmPopup()
        {
            if ( saveConfirmPopupRoot == null )
            {
                return;
            }

            if ( saveConfirmMessageText != null )
            {
                string mapId = ResolveMapId();
                saveConfirmMessageText.text = $"'{mapId}' 맵 데이터가 이미 존재합니다.\n덮어쓰시겠습니까?";
            }

            SetPanelVisible( saveConfirmPopupRoot, true );
        }

        ///<summary>
        /// 저장 확인 팝업 숨김
        ///</summary>
        private void HideSaveConfirmPopup()
        {
            SetPanelVisible( saveConfirmPopupRoot, false );
        }

        ///<summary>
        /// 저장 데이터 구성
        ///</summary>
        private CMapToolSaveData BuildSaveData()
        {
            CMapToolSaveData saveData = new CMapToolSaveData();
            saveData.mapId = ResolveMapId();
            saveData.mapName = ResolveMapName();
            saveData.bgmClipName = ResolveBgmClipName();

            if ( backgroundRenderer != null && backgroundRenderer.sprite != null )
            {
                saveData.backgroundSpriteName = backgroundRenderer.sprite.name;
            }

            if ( backgroundLayoutController != null && backgroundLayoutController.TryGetCustomRightBoundaryX( out float customRightBoundaryX ) )
            {
                saveData.hasCustomRightBoundary = true;
                saveData.customRightBoundaryX = customRightBoundaryX;
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

                if ( placedObject.GetPlacedObjectType() == MapToolPlacedObject.eMapToolPlacedObjectType.NPC )
                {
                    CMapToolNpcSaveData npcSaveData = BuildNpcSaveData( placedObject );
                    saveData.npcs.Add( npcSaveData );
                    continue;
                }

                CMapToolMonsterSaveData monsterSaveData = BuildMonsterSaveData( placedObject );
                saveData.monsters.Add( monsterSaveData );
            }

            return saveData;
        }

        ///<summary>
        /// BGM 클립 이름 반환
        ///</summary>
        private string ResolveBgmClipName()
        {
            if ( bgmClipNameInputField == null )
            {
                return string.Empty;
            }

            string inputBgmClipName = bgmClipNameInputField.text;

            if ( string.IsNullOrWhiteSpace( inputBgmClipName ) )
            {
                return string.Empty;
            }

            string result = inputBgmClipName.Trim();
            bgmClipNameInputField.text = result;
            return result;
        }

        ///<summary>
        /// 로드된 BGM 미리듣기 처리
        ///</summary>
        private void PreviewLoadedBgm( string _bgmClipName )
        {
            if ( string.IsNullOrWhiteSpace( _bgmClipName ) )
            {
                return;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            audioManager.PlayBgm( _bgmClipName );
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
        /// NPC 저장 데이터 구성
        ///</summary>
        private CMapToolNpcSaveData BuildNpcSaveData( MapToolPlacedObject _placedObject )
        {
            CMapToolNpcSaveData npcSaveData = new CMapToolNpcSaveData();
            npcSaveData.prefabName = _placedObject.GetPrefabName();
            npcSaveData.resourcePath = _placedObject.GetResourcePath();
            npcSaveData.transform = BuildTransformData( _placedObject.transform );
            return npcSaveData;
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
        /// NPC 모드 버튼 클릭 처리
        ///</summary>
        private void OnNpcModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( npcPanelRoot );
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
        /// 스킬 테스트 버튼 클릭 처리
        ///</summary>
        private void OnSkillTestModeButtonClicked()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            ToggleSinglePanel( skillTestPanelRoot );
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
        /// 스킬 테스트 항목 클릭 처리
        ///</summary>
        private void OnSkillTestItemClicked( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null )
            {
                return;
            }

            ShowSkillActivePreview( _skillDefinition );
            StartCoroutine( IE_ExecuteSkillForMapTool( _skillDefinition ) );
        }

        ///<summary>
        /// 스킬 테스트 항목 포인터 진입 처리
        ///</summary>
        public void HandleSkillTestItemPointerEnter( CSkillDefinition _skillDefinition )
        {
            hoveredSkillDefinition = _skillDefinition;
            ShowHoveredSkillPreview();
        }

        ///<summary>
        /// 스킬 테스트 항목 포인터 이탈 처리
        ///</summary>
        public void HandleSkillTestItemPointerExit( CSkillDefinition _skillDefinition )
        {
            if ( hoveredSkillDefinition != _skillDefinition )
            {
                return;
            }

            hoveredSkillDefinition = null;

            if ( hoverSkillRangeVisualizer != null )
            {
                hoverSkillRangeVisualizer.HidePreview();
            }
        }

        ///<summary>
        /// 스킬 테스트 버튼 라벨 생성
        ///</summary>
        private string BuildSkillTestButtonLabel( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null )
            {
                return string.Empty;
            }

            string skillName = _skillDefinition.GetSkillName();
            string skillId = _skillDefinition.GetSkillId();
            eSkillType skillType = _skillDefinition.GetSkillType();
            string result = $"[ {skillType} ] {skillName}\n{skillId}";
            return result;
        }

        ///<summary>
        /// 스킬 호버 미리보기 대상 갱신
        ///</summary>
        private void UpdateSkillPreviewTarget()
        {
            if ( hoveredSkillDefinition == null || hoverSkillRangeVisualizer == null )
            {
                return;
            }

            ShowHoveredSkillPreview();
        }

        ///<summary>
        /// 스킬 호버 미리보기 표시
        ///</summary>
        private void ShowHoveredSkillPreview()
        {
            if ( hoveredSkillDefinition == null || hoverSkillRangeVisualizer == null )
            {
                return;
            }

            Transform ownerTransform = ResolveSkillPreviewOwnerTransform();

            if ( ownerTransform == null )
            {
                hoverSkillRangeVisualizer.HidePreview();
                return;
            }

            CActiveSkillEffectBase activeSkillEffect = hoveredSkillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect != null )
            {
                hoverSkillRangeVisualizer.ShowFollowingPreview( activeSkillEffect, ownerTransform );
                return;
            }

            bool hasPreviewData = TryGetFallbackSkillPreviewData( hoveredSkillDefinition, ownerTransform, out CSkillToolRangePreviewData previewData );

            if ( hasPreviewData == false )
            {
                hoverSkillRangeVisualizer.HidePreview();
                return;
            }

            hoverSkillRangeVisualizer.ShowFixedPreview( previewData, 0.0f );
        }

        ///<summary>
        /// 스킬 사용 미리보기 표시
        ///</summary>
        private void ShowSkillActivePreview( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null || activeSkillRangeVisualizer == null )
            {
                return;
            }

            Transform ownerTransform = ResolveSkillPreviewOwnerTransform();

            if ( ownerTransform == null )
            {
                return;
            }

            bool hasPreviewData = TryGetSkillPreviewData( _skillDefinition, ownerTransform, out CSkillToolRangePreviewData previewData );

            if ( hasPreviewData == false )
            {
                return;
            }

            float previewDurationSeconds = GetSkillPreviewDurationSeconds( _skillDefinition ) + _skillDefinition.GetCastLockDurationSeconds();
            activeSkillRangeVisualizer.ShowFixedPreview( previewData, previewDurationSeconds );
        }

        ///<summary>
        /// 맵 툴 전용 스킬 시전 코루틴 처리
        ///</summary>
        private IEnumerator IE_ExecuteSkillForMapTool( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null )
            {
                yield break;
            }

            if ( _skillDefinition.GetSkillType() == eSkillType.PASSIVE )
            {
                ExecuteSkillForMapTool( _skillDefinition );
                yield break;
            }

            bool hasExecutionContext = TryResolveToolSkillExecutionContext( _skillDefinition, out CSkillManager _, out CSkillRuntimeData _, out PlayerController playerController, out CPlayerStatManager _, out Transform _ );

            if ( hasExecutionContext == false )
            {
                yield break;
            }

            if ( playerController != null )
            {
                string castAnimationName = _skillDefinition.GetResolvedCastAnimationName();
                float castAnimationSpeed = _skillDefinition.GetCastAnimationSpeed();
                float castLockDurationSeconds = _skillDefinition.GetCastLockDurationSeconds();
                bool didStartCast = playerController.TryBeginToolSkillCast( castAnimationName, castAnimationSpeed, castLockDurationSeconds );

                if ( didStartCast && castLockDurationSeconds > 0.0f )
                {
                    yield return new WaitForSeconds( castLockDurationSeconds );
                }
            }

            ExecuteSkillForMapTool( _skillDefinition );
        }

        ///<summary>
        /// 맵 툴 전용 스킬 실행 처리
        ///</summary>
        private bool ExecuteSkillForMapTool( CSkillDefinition _skillDefinition )
        {
            bool hasExecutionContext = TryResolveToolSkillExecutionContext( _skillDefinition, out CSkillManager resolvedSkillManager, out CSkillRuntimeData runtimeData, out PlayerController playerController, out CPlayerStatManager playerStatManager, out Transform ownerTransform );

            if ( hasExecutionContext == false )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();
            CSkillActionBase activeAction = _skillDefinition.GetActiveAction();
            CSkillContext skillContext = new CSkillContext( resolvedSkillManager, playerController, playerStatManager, _skillDefinition, runtimeData, ownerTransform );

            if ( activeSkillEffect != null )
            {
                bool canExecute = activeSkillEffect.CanExecute( skillContext );

                if ( canExecute == false )
                {
                    return false;
                }

                activeSkillEffect.Execute( skillContext );
                return true;
            }

            if ( activeAction == null )
            {
                return false;
            }

            bool canExecuteAction = activeAction.CanExecute( skillContext );

            if ( canExecuteAction == false )
            {
                return false;
            }

            activeAction.Execute( skillContext );
            return true;
        }

        ///<summary>
        /// 툴 스킬 실행 문맥 결정
        ///</summary>
        private bool TryResolveToolSkillExecutionContext( CSkillDefinition _skillDefinition, out CSkillManager _resolvedSkillManager, out CSkillRuntimeData _runtimeData, out PlayerController _playerController, out CPlayerStatManager _playerStatManager, out Transform _ownerTransform )
        {
            _resolvedSkillManager = null;
            _runtimeData = null;
            _playerController = null;
            _playerStatManager = null;
            _ownerTransform = null;

            if ( _skillDefinition == null )
            {
                return false;
            }

            EnsurePlayerObjectExists();
            CGameManager gameManager = CGameManager.Instance;
            gameManager.TryGetActivePlayerController( out PlayerController resolvedPlayerController );
            _playerController = resolvedPlayerController;

            if ( resolvedPlayerController != null )
            {
                CSkillManager playerSkillManager = resolvedPlayerController.GetSkillManager();

                if ( playerSkillManager != null )
                {
                    skillManager = playerSkillManager;
                }
            }

            EnsureSkillManagerExists();
            _resolvedSkillManager = skillManager;

            if ( _resolvedSkillManager == null )
            {
                return false;
            }

            _playerStatManager = resolvedPlayerController != null ? resolvedPlayerController.GetPlayerStatManager() : null;
            _ownerTransform = resolvedPlayerController != null ? resolvedPlayerController.transform : _resolvedSkillManager.transform;

            string skillId = _skillDefinition.GetSkillId();
            _runtimeData = _resolvedSkillManager.GetSkillRuntimeData( skillId );

            if ( _runtimeData == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();
            CSkillActionBase activeAction = _skillDefinition.GetActiveAction();
            bool hasExecutableContent = activeSkillEffect != null || activeAction != null || _skillDefinition.GetSkillType() == eSkillType.PASSIVE;
            return hasExecutableContent;
        }

        ///<summary>
        /// 스킬 미리보기 데이터 반환
        ///</summary>
        private bool TryGetSkillPreviewData( CSkillDefinition _skillDefinition, Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _skillDefinition == null || _ownerTransform == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect != null )
            {
                bool hasPreviewData = activeSkillEffect.TryGetToolRangePreviewData( _ownerTransform, out _previewData );
                return hasPreviewData;
            }

            bool hasFallbackPreviewData = TryGetFallbackSkillPreviewData( _skillDefinition, _ownerTransform, out _previewData );
            return hasFallbackPreviewData;
        }

        ///<summary>
        /// 스킬 대체 미리보기 데이터 반환
        ///</summary>
        private bool TryGetFallbackSkillPreviewData( CSkillDefinition _skillDefinition, Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _skillDefinition == null || _ownerTransform == null )
            {
                return false;
            }

            if ( _skillDefinition.GetSkillType() != eSkillType.PASSIVE )
            {
                return false;
            }

            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = _ownerTransform.position;
            _previewData.radius = 0.75f;
            return true;
        }

        ///<summary>
        /// 스킬 미리보기 표시 시간 반환
        ///</summary>
        private float GetSkillPreviewDurationSeconds( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null )
            {
                return SkillPreviewDisplayDurationSeconds;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect != null )
            {
                float previewDurationSeconds = activeSkillEffect.GetToolPreviewDurationSeconds();
                return previewDurationSeconds;
            }

            return SkillPreviewDisplayDurationSeconds;
        }

        ///<summary>
        /// 스킬 미리보기 기준 트랜스폼 반환
        ///</summary>
        private Transform ResolveSkillPreviewOwnerTransform()
        {
            if ( skillManager == null )
            {
                CGameManager gameManager = CGameManager.Instance;
                gameManager.TryGetActivePlayerController( out PlayerController resolvedPlayerController );

                if ( resolvedPlayerController != null )
                {
                    return resolvedPlayerController.transform;
                }

                return null;
            }

            PlayerController playerController = skillManager.GetPlayerController();
            Transform ownerTransform = playerController != null ? playerController.transform : skillManager.transform;
            return ownerTransform;
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
            hoveredSkillDefinition = null;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );

            if ( hoverSkillRangeVisualizer != null )
            {
                hoverSkillRangeVisualizer.HidePreview();
            }

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

            EnsureBackgroundLayoutControllerExists();

            if ( backgroundLayoutController != null )
            {
                backgroundLayoutController.RefreshLayout();
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
        /// 저장된 우측 경계 적용
        ///</summary>
        private void ApplyLoadedRightBoundary( CMapToolSaveData _loadedData )
        {
            EnsureBackgroundLayoutControllerExists();

            if ( backgroundLayoutController == null )
            {
                return;
            }

            if ( _loadedData != null && _loadedData.hasCustomRightBoundary )
            {
                backgroundLayoutController.SetCustomRightBoundaryX( _loadedData.customRightBoundaryX );
                RefreshBackgroundColliderVisual();
                return;
            }

            backgroundLayoutController.ClearCustomRightBoundary();
            RefreshBackgroundColliderVisual();
        }

        ///<summary>
        /// 우측 경계 지정 모드 시작
        ///</summary>
        private void BeginRightBoundaryPlacement()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            currentMode = eMapToolMode.SET_RIGHT_BOUNDARY;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( npcPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );
        }

        ///<summary>
        /// 우측 경계 기본값 복원
        ///</summary>
        private void ClearRightBoundaryOverride()
        {
            EnsureBackgroundLayoutControllerExists();

            if ( backgroundLayoutController == null )
            {
                return;
            }

            backgroundLayoutController.ClearCustomRightBoundary();
            RefreshBackgroundColliderVisual();
        }

        ///<summary>
        /// 맵 전체 너비 기준 카메라 보기 적용
        ///</summary>
        private void ApplyMapOverviewCameraView()
        {
            if ( worldCamera == null || worldCamera.orthographic == false )
            {
                return;
            }

            EnsureBackgroundLayoutControllerExists();

            if ( backgroundLayoutController == null )
            {
                return;
            }

            if ( backgroundLayoutController.TryGetCombinedWorldBounds( out Bounds backgroundBounds ) == false )
            {
                return;
            }

            CacheOriginalCameraViewIfNeeded();
            SetCameraFollowEnabled( false );
            float targetOrthographicSize = CalculateOverviewOrthographicSize( backgroundBounds );
            Vector3 targetCameraPosition = worldCamera.transform.position;
            targetCameraPosition.x = backgroundBounds.center.x;
            targetCameraPosition.y = backgroundBounds.center.y;
            worldCamera.orthographicSize = targetOrthographicSize;
            worldCamera.transform.position = targetCameraPosition;
            SetPanelVisible( backgroundPanelRoot, false );
        }

        ///<summary>
        /// 맵툴 카메라 원래 보기 복원
        ///</summary>
        private void RestoreOriginalCameraView()
        {
            if ( worldCamera == null || hasCachedOriginalCameraView == false )
            {
                return;
            }

            worldCamera.orthographicSize = cachedOriginalOrthographicSize;
            worldCamera.transform.position = cachedOriginalCameraPosition;
            SetCameraFollowEnabled( wasCameraFollowEnabledBeforeOverview );
            hasCachedOriginalCameraView = false;
            SetPanelVisible( backgroundPanelRoot, false );
        }

        ///<summary>
        /// 맵 로드 전 맵툴 카메라 보기 복원
        ///</summary>
        private void RestoreOriginalCameraViewForMapLoad()
        {
            if ( worldCamera == null )
            {
                return;
            }

            if ( hasCachedOriginalCameraView )
            {
                worldCamera.orthographicSize = cachedOriginalOrthographicSize;
                worldCamera.transform.position = cachedOriginalCameraPosition;
                SetCameraFollowEnabled( wasCameraFollowEnabledBeforeOverview );
                hasCachedOriginalCameraView = false;
                return;
            }

            SetCameraFollowEnabled( true );
        }

        ///<summary>
        /// 맵툴 카메라 원래 보기 캐시
        ///</summary>
        private void CacheOriginalCameraViewIfNeeded()
        {
            if ( worldCamera == null || hasCachedOriginalCameraView )
            {
                return;
            }

            cachedOriginalOrthographicSize = worldCamera.orthographicSize;
            cachedOriginalCameraPosition = worldCamera.transform.position;
            CPlayerCameraFollowController cameraFollowController = worldCamera.GetComponent<CPlayerCameraFollowController>();
            wasCameraFollowEnabledBeforeOverview = cameraFollowController != null && cameraFollowController.IsFollowEnabled();
            hasCachedOriginalCameraView = true;
        }

        ///<summary>
        /// 맵 전체 보기 Orthographic 크기 계산
        ///</summary>
        private float CalculateOverviewOrthographicSize( Bounds _backgroundBounds )
        {
            float cameraAspect = Mathf.Max( MinimumOrthographicSize, worldCamera.aspect );
            float sizeByWidth = ( _backgroundBounds.size.x * 0.5f ) / cameraAspect;
            float result = Mathf.Max( MinimumOrthographicSize, sizeByWidth * MapOverviewCameraPaddingMultiplier );
            return result;
        }

        ///<summary>
        /// 맵툴 카메라 추적 활성 상태 설정
        ///</summary>
        private void SetCameraFollowEnabled( bool _isEnabled )
        {
            if ( worldCamera == null )
            {
                return;
            }

            CPlayerCameraFollowController cameraFollowController = worldCamera.GetComponent<CPlayerCameraFollowController>();

            if ( cameraFollowController == null )
            {
                return;
            }

            cameraFollowController.SetFollowEnabled( _isEnabled );
        }

        ///<summary>
        /// 마우스 위치 기준 우측 경계 적용
        ///</summary>
        private void ApplyRightBoundaryAtMousePosition()
        {
            EnsureBackgroundLayoutControllerExists();

            if ( backgroundLayoutController == null )
            {
                currentMode = eMapToolMode.NONE;
                return;
            }

            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            backgroundLayoutController.SetCustomRightBoundaryX( mouseWorldPosition.x );
            RefreshBackgroundColliderVisual();
            currentMode = eMapToolMode.NONE;
        }

        ///<summary>
        /// 배경 콜라이더 시각화 갱신
        ///</summary>
        private void RefreshBackgroundColliderVisual()
        {
            if ( backgroundColliderVisualizer == null )
            {
                return;
            }

            backgroundColliderVisualizer.RefreshColliderVisual();
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
            SetPanelVisible( skillTestPanelRoot, false );
            RebuildPreviewInstance();
        }

        ///<summary>
        /// NPC 배치 시작
        ///</summary>
        private void BeginNpcPlacement( string _prefabName, string _resourcePath )
        {
            StopPlacedObjectDrag();
            selectedNpcPrefabName = _prefabName;
            selectedNpcResourcePath = _resourcePath;
            GameObject npcPrefab = ResolveNpcPrefab( _prefabName, _resourcePath );
            selectedNpcFacingSignX = ResolveFacingSignFromPrefab( npcPrefab );
            currentMode = eMapToolMode.PLACE_NPC;
            SetPanelVisible( backgroundPanelRoot, false );
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( npcPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );
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
            SetPanelVisible( npcPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( loadMapPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );
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
            selectedNpcPrefabName = string.Empty;
            selectedNpcResourcePath = string.Empty;
            selectedNpcFacingSignX = 1.0f;
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

            if ( currentMode == eMapToolMode.PLACE_NPC )
            {
                GameObject npcPrefab = ResolveNpcPrefab( selectedNpcPrefabName, selectedNpcResourcePath );
                return npcPrefab;
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

            if ( currentMode == eMapToolMode.PLACE_NPC )
            {
                ApplyFacingSignToTransform( _targetPreviewInstance.transform, selectedNpcFacingSignX );
                CNPCObject previewNpcObject = _targetPreviewInstance.GetComponent<CNPCObject>();

                if ( previewNpcObject != null )
                {
                    previewNpcObject.enabled = false;
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
        /// NPC 배치 방향 전환 입력 처리
        ///</summary>
        private void HandleNpcFlipInput()
        {
            if ( Input.GetKeyDown( NpcFlipKey ) == false )
            {
                return;
            }

            if ( isDraggingPlacedObject && draggedPlacedObject != null )
            {
                MapToolPlacedObject.eMapToolPlacedObjectType placedObjectType = draggedPlacedObject.GetPlacedObjectType();

                if ( placedObjectType != MapToolPlacedObject.eMapToolPlacedObjectType.NPC )
                {
                    return;
                }

                ToggleFacingDirection( draggedPlacedObject.transform );
                return;
            }

            if ( currentMode != eMapToolMode.PLACE_NPC || previewInstance == null )
            {
                return;
            }

            selectedNpcFacingSignX *= -1.0f;
            ApplyFacingSignToTransform( previewInstance.transform, selectedNpcFacingSignX );
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
            if ( currentMode == eMapToolMode.NONE )
            {
                return;
            }

            if ( currentMode != eMapToolMode.SET_RIGHT_BOUNDARY && previewInstance == null )
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

            if ( currentMode == eMapToolMode.SET_RIGHT_BOUNDARY )
            {
                ApplyRightBoundaryAtMousePosition();
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
                return;
            }

            if ( currentMode == eMapToolMode.PLACE_NPC )
            {
                PlaceNpcAtMousePosition();
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
        /// 마우스 위치 NPC 배치 처리
        ///</summary>
        private void PlaceNpcAtMousePosition()
        {
            GameObject npcPrefab = ResolveNpcPrefab( selectedNpcPrefabName, selectedNpcResourcePath );

            if ( npcPrefab == null )
            {
                return;
            }

            Vector3 spawnPosition = GetMouseWorldPosition();
            GameObject npcInstance = Instantiate( npcPrefab, spawnPosition, Quaternion.identity );
            npcInstance.name = npcPrefab.name;
            ApplyFacingSignToTransform( npcInstance.transform, selectedNpcFacingSignX );
            MapToolPlacedObject placedObject = EnsurePlacedObjectComponent( npcInstance );
            placedObject.SetupNpc( selectedNpcPrefabName, selectedNpcResourcePath );
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
        /// NPC 프리팹 결정
        ///</summary>
        private GameObject ResolveNpcPrefab( string _prefabName, string _resourcePath )
        {
            if ( string.IsNullOrEmpty( _prefabName ) == false && npcPrefabByName.TryGetValue( _prefabName, out GameObject cachedPrefab ) )
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
        /// NPC 초기 방향 부호 결정
        ///</summary>
        private float ResolveFacingSignFromPrefab( GameObject _prefab )
        {
            if ( _prefab == null )
            {
                return 1.0f;
            }

            Vector3 localScale = _prefab.transform.localScale;

            if ( localScale.x < 0.0f )
            {
                return -1.0f;
            }

            return 1.0f;
        }

        ///<summary>
        /// NPC 방향 반전 적용
        ///</summary>
        private void ToggleFacingDirection( Transform _targetTransform )
        {
            if ( _targetTransform == null )
            {
                return;
            }

            Vector3 localScale = _targetTransform.localScale;
            float nextFacingSignX = localScale.x < 0.0f ? 1.0f : -1.0f;
            ApplyFacingSignToTransform( _targetTransform, nextFacingSignX );
        }

        ///<summary>
        /// NPC 방향 부호 적용
        ///</summary>
        private void ApplyFacingSignToTransform( Transform _targetTransform, float _facingSignX )
        {
            if ( _targetTransform == null )
            {
                return;
            }

            Vector3 localScale = _targetTransform.localScale;
            float resolvedSignX = _facingSignX < 0.0f ? -1.0f : 1.0f;
            float scaleMagnitudeX = Mathf.Abs( localScale.x );

            if ( scaleMagnitudeX <= 0.0f )
            {
                scaleMagnitudeX = 1.0f;
            }

            localScale.x = scaleMagnitudeX * resolvedSignX;
            _targetTransform.localScale = localScale;
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
        /// 몬스터 포탈 NPC 오브젝트 초기화
        ///</summary>
        private void ClearMonsterAndPortalObjects()
        {
            CancelPlacementMode();
            StopPlacedObjectDrag();
            SetPanelVisible( monsterPanelRoot, false );
            SetPanelVisible( npcPanelRoot, false );
            SetPanelVisible( portalPanelRoot, false );
            SetPanelVisible( skillTestPanelRoot, false );

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
                bool isClearTarget = placedObjectType == MapToolPlacedObject.eMapToolPlacedObjectType.MONSTER || placedObjectType == MapToolPlacedObject.eMapToolPlacedObjectType.PORTAL || placedObjectType == MapToolPlacedObject.eMapToolPlacedObjectType.NPC;

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


