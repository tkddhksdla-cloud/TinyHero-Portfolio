using System.Collections;
using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Skill;
using TinyHero.Tools;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 관리 컴포넌트
    ///</summary>
    public sealed class CMapManager : CSingleTon<CMapManager>
    {
        private const string MapDataResourceFolderPath = "MapData/";
        private const string BackgroundSpriteResourceFolderPath = "RawImages/BG";
        private const string PortalPrefabResourcePath = "Prefabs/Portal/PortalObject";
        private const string MonsterPrefabResourceFolderPath = "Prefabs/Character/Monster";
        private const string NpcPrefabResourceFolderPath = "Prefabs/Character/NPC";
        private const string PlayerPrefabResourcePath = "Prefabs/Character/Player/PlayerObject";
        private const string PlayerObjectName = "PlayerObject";
        private const float DefaultFadeDuration = 0.35f;
        private const float MapTransitionBlackHoldSeconds = 0.1f;
        private const int FadeSortingOrder = 1000;
        private const string FadeCanvasObjectName = "MapFadeCanvas";
        private const string FadeImageObjectName = "FadeImage";
        private const string GameplaySceneName = "SceneMap";
        private const string DefaultGameplayMapId = "MAP_STARTER_000_VILLAGE";
        private const string MapTitleLogoUiPrefabResourcePath = "Prefabs/UI/Map/MapTitleLogoUI";
        private const string MapLoadingUiObjectName = "MapLoadingUI";
        private const string MapTitleLogoUiPoolObjectName = "MapTitleLogoUIPool";
        private const string TempUiCanvasObjectName = "Canvas_TempUI";
        private const string MapTitleLogoUiPoolKey = "Maps.MapTitleLogoUI";
        private const string MonsterPoolKeyPrefix = "Maps.Monster";
        private const string WorldItemDropPoolKeyPrefix = "Maps.WorldItemDrop";
        private const string LoadingMapDataText = "Loading Map Data...";
        private const string LoadingBackgroundText = "Loading Background...";
        private const string LoadingPortalText = "Loading Portals...";
        private const string LoadingMonsterText = "Loading Monsters...";
        private const string LoadingNpcText = "Loading NPCs...";
        private const string ApplyingMapText = "Applying Map...";
        private const string LoadingCompleteText = "Loading Complete";
        private const string LoadingFailedText = "Map Load Failed";
        private const float LoadingMapDataProgress = 0.1f;
        private const float LoadingBackgroundProgress = 0.28f;
        private const float LoadingPortalProgress = 0.44f;
        private const float LoadingMonsterProgress = 0.62f;
        private const float LoadingNpcProgress = 0.78f;
        private const float ApplyingMapProgress = 0.92f;
        private const float LoadingCompleteProgress = 1.0f;

        private sealed class CMapMonsterRespawnContext
        {
            public int mapRuntimeVersion;
            public string monsterId;
            public string monsterName;
            public string monsterPoolKey;
            public Vector3 spawnPosition;
            public Vector3 spawnRotation;
            public Vector3 spawnScale;
            public float respawnDelaySeconds;
        }

        [SerializeField] private float fadeDuration = DefaultFadeDuration;

        private static string pendingMapId = string.Empty;
        private static string pendingEntryPortalId = string.Empty;
        private readonly List<MapRuntimeSpawnMarker> spawnedRuntimeObjects = new List<MapRuntimeSpawnMarker>();
        private readonly List<MonsterObject> activePooledMonsterObjects = new List<MonsterObject>();
        private readonly List<CWorldItemDropObject> activePooledWorldItemDropObjects = new List<CWorldItemDropObject>();
        private readonly List<MapTitleLogoUI> activeMapTitleLogoUiList = new List<MapTitleLogoUI>();
        private readonly Dictionary<string, Sprite> backgroundSpriteByName = new Dictionary<string, Sprite>();
        private readonly HashSet<string> monsterPoolKeySet = new HashSet<string>();
        private readonly HashSet<string> worldItemDropPoolKeySet = new HashSet<string>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> npcPrefabByName = new Dictionary<string, GameObject>();
        private Canvas fadeCanvas;
        private Image fadeImage;
        private GraphicRaycaster fadeGraphicRaycaster;
        private CMapLoadingUI mapLoadingUi;
        private RectTransform mapTitleLogoUiPoolRectTransform;
        private GameObject mapTitleLogoUiPrefab;
        private Sprite currentBackgroundSprite;
        private string currentMapId = string.Empty;
        private string currentMapName = string.Empty;
        private int currentMapRuntimeVersion;
        private bool isTransitionInProgress;
        private bool isMapLoadInProgress;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            CacheResourceCatalog();
            EnsureFadeOverlayExists();
            EnsureGameplayScenePlayerPrefabInstance();
            EnsureGameplayCameraFollowController();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryLoadPendingMapForActiveScene();
        }

        ///<summary>
        /// 맵 전환 진행 여부
        ///</summary>
        public bool IsTransitionInProgress()
        {
            bool result = isTransitionInProgress;
            return result;
        }

        ///<summary>
        /// 현재 맵 ID 반환
        ///</summary>
        public string GetCurrentMapId()
        {
            string result = currentMapId;
            return result;
        }

        ///<summary>
        /// 현재 맵 이름 반환
        ///</summary>
        public string GetCurrentMapName()
        {
            string result = currentMapName;
            return result;
        }

        ///<summary>
        /// 현재 적용 중인 배경 스프라이트 반환
        ///</summary>
        public Sprite GetCurrentBackgroundSprite()
        {
            Sprite result = currentBackgroundSprite;
            return result;
        }

        ///<summary>
        /// 현재 배경 스프라이트의 월드 경계 조회 시도
        ///</summary>
        public bool TryGetCurrentBackgroundBounds( out Bounds _backgroundBounds )
        {
            _backgroundBounds = default;

            WorldSpaceBackgroundFitter targetBackgroundFitter = FindFirstObjectByType<WorldSpaceBackgroundFitter>();

            if ( targetBackgroundFitter == null )
            {
                return false;
            }

            CMapBackgroundLayoutController backgroundLayoutController = targetBackgroundFitter.GetComponent<CMapBackgroundLayoutController>();

            if ( backgroundLayoutController != null )
            {
                bool hasCombinedBounds = backgroundLayoutController.TryGetCombinedWorldBounds( out _backgroundBounds );

                if ( hasCombinedBounds )
                {
                    return true;
                }
            }

            SpriteRenderer targetBackgroundRenderer = targetBackgroundFitter.GetComponent<SpriteRenderer>();

            if ( targetBackgroundRenderer == null || targetBackgroundRenderer.sprite == null )
            {
                return false;
            }

            _backgroundBounds = targetBackgroundRenderer.bounds;
            return true;
        }

        ///<summary>
        /// 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CMapManager _instance )
        {
            CMapManager resolvedInstance = Instance;
            _instance = resolvedInstance;
            bool hasInstance = _instance != null;
            return hasInstance;
        }

        ///<summary>
        /// 대기 맵 로드 정보 설정
        ///</summary>
        public static void SetPendingMapLoad(string _mapId)
        {
            SetPendingMapLoad( _mapId, string.Empty );
        }

        ///<summary>
        /// 대기 맵 로드 정보 설정
        ///</summary>
        public static void SetPendingMapLoad(string _mapId, string _entryPortalId)
        {
            pendingMapId = string.IsNullOrWhiteSpace( _mapId ) ? string.Empty : _mapId.Trim();
            pendingEntryPortalId = string.IsNullOrWhiteSpace( _entryPortalId ) ? string.Empty : _entryPortalId.Trim();
        }

        ///<summary>
        /// 맵 즉시 로드
        ///</summary>
        public bool LoadMapImmediately(string _mapId)
        {
            bool result = LoadMapImmediately( _mapId, string.Empty );
            return result;
        }

        ///<summary>
        /// 맵 즉시 로드
        ///</summary>
        public bool LoadMapImmediately(string _mapId, string _entryPortalId)
        {
            if ( string.IsNullOrWhiteSpace( _mapId ) )
            {
                return false;
            }

            string trimmedMapId = _mapId.Trim();
            string trimmedEntryPortalId = string.IsNullOrWhiteSpace( _entryPortalId ) ? string.Empty : _entryPortalId.Trim();
            CMapToolSaveData loadedData = LoadMapSaveDataFromResources( trimmedMapId );

            if ( loadedData == null )
            {
                return false;
            }

            ApplyMapData( loadedData, trimmedEntryPortalId, null, null );
            return true;
        }

        ///<summary>
        /// 맵 전환 시작
        ///</summary>
        public bool TransitionToMap(string _mapId)
        {
            bool result = TransitionToMap( _mapId, string.Empty );
            return result;
        }

        ///<summary>
        /// 맵 전환 시작
        ///</summary>
        public bool TransitionToMap(string _mapId, string _entryPortalId)
        {
            if ( isTransitionInProgress )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( _mapId ) )
            {
                return false;
            }

            string trimmedMapId = _mapId.Trim();
            string trimmedEntryPortalId = string.IsNullOrWhiteSpace( _entryPortalId ) ? string.Empty : _entryPortalId.Trim();
            StartCoroutine( IE_TransitionToMap( trimmedMapId, trimmedEntryPortalId ) );
            return true;
        }

        ///<summary>
        /// 맵 전환 코루틴 처리
        ///</summary>
        private IEnumerator IE_TransitionToMap(string _mapId, string _entryPortalId)
        {
            isTransitionInProgress = true;
            EnsureFadeOverlayExists();
            SetFadeAlpha( 0.0f );
            yield return IE_FadeAlpha( 0.0f, 1.0f );
            yield return IE_LoadMapDataAndApply( _mapId, _entryPortalId, null );
            yield return null;
            yield return new WaitForSeconds( MapTransitionBlackHoldSeconds );
            HideMapLoadingProgress();
            yield return IE_FadeAlpha( 1.0f, 0.0f );
            ShowCurrentMapTitleLogoUi();
            isTransitionInProgress = false;
        }

        ///<summary>
        /// 페이드 알파 코루틴 처리
        ///</summary>
        private IEnumerator IE_FadeAlpha(float _startAlpha, float _endAlpha)
        {
            if ( fadeImage == null )
            {
                yield break;
            }

            float elapsedTime = 0.0f;
            Color fadeColor = fadeImage.color;
            fadeColor.a = _startAlpha;
            fadeImage.color = fadeColor;
            UpdateFadeRaycastState( _startAlpha );

            if ( fadeDuration <= 0.0f )
            {
                fadeColor.a = _endAlpha;
                fadeImage.color = fadeColor;
                UpdateFadeRaycastState( _endAlpha );
                yield break;
            }

            while ( elapsedTime < fadeDuration )
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01( elapsedTime / fadeDuration );
                float alpha = Mathf.Lerp( _startAlpha, _endAlpha, normalizedTime );
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
                UpdateFadeRaycastState( alpha );
                yield return null;
            }

            fadeColor.a = _endAlpha;
            fadeImage.color = fadeColor;
            UpdateFadeRaycastState( _endAlpha );
        }

        ///<summary>
        /// 초기 맵 로드 코루틴 처리
        ///</summary>
        private IEnumerator IE_LoadPendingMap(string _mapId, string _entryPortalId)
        {
            isTransitionInProgress = true;
            EnsureFadeOverlayExists();
            SetFadeAlpha( 1.0f );
            yield return IE_LoadMapDataAndApply( _mapId, _entryPortalId, null );
            yield return null;
            HideMapLoadingProgress();
            yield return IE_FadeAlpha( 1.0f, 0.0f );
            ShowCurrentMapTitleLogoUi();
            isTransitionInProgress = false;
        }

        ///<summary>
        /// 씬 로드 후 대기 맵 적용 처리
        ///</summary>
        private void HandleSceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
        {
            EnsureGameplayScenePlayerPrefabInstance();
            EnsureGameplayCameraFollowController();
            TryLoadPendingMapForActiveScene();
        }

        ///<summary>
        /// 게임플레이 씬 플레이어 프리팹 인스턴스 보장
        ///</summary>
        private void EnsureGameplayScenePlayerPrefabInstance()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if ( activeScene.name != GameplaySceneName )
            {
                return;
            }

            PlayerController existingPlayerController = ResolveActivePlayerController();

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

            GameObject createdPlayerObject = Instantiate( playerPrefab, Vector3.zero, Quaternion.identity );
            createdPlayerObject.name = PlayerObjectName;
        }

        ///<summary>
        /// 게임플레이 씬 카메라 추적 컴포넌트 보장
        ///</summary>
        private void EnsureGameplayCameraFollowController()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if ( activeScene.name != GameplaySceneName )
            {
                return;
            }

            Camera mainCamera = Camera.main;

            if ( mainCamera == null )
            {
                return;
            }

            CPlayerCameraFollowController cameraFollowController = mainCamera.GetComponent<CPlayerCameraFollowController>();

            if ( cameraFollowController == null )
            {
                cameraFollowController = mainCamera.gameObject.AddComponent<CPlayerCameraFollowController>();
            }

            PlayerController playerController = ResolveActivePlayerController();

            if ( playerController == null )
            {
                return;
            }

            cameraFollowController.SetTarget( playerController.transform );
        }

        ///<summary>
        /// 현재 씬 대기 맵 로드 처리
        ///</summary>
        private void TryLoadPendingMapForActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if ( activeScene.name != GameplaySceneName )
            {
                return;
            }

            if ( string.IsNullOrWhiteSpace( pendingMapId ) )
            {
                if ( string.IsNullOrWhiteSpace( currentMapId ) )
                {
                    StartCoroutine( IE_LoadPendingMap( DefaultGameplayMapId, string.Empty ) );
                }

                return;
            }

            string mapIdToLoad = pendingMapId;
            string entryPortalIdToLoad = pendingEntryPortalId;
            pendingMapId = string.Empty;
            pendingEntryPortalId = string.Empty;
            StartCoroutine( IE_LoadPendingMap( mapIdToLoad, entryPortalIdToLoad ) );
        }

        ///<summary>
        /// 리소스 목록 캐시
        ///</summary>
        private void CacheResourceCatalog()
        {
            CacheBackgroundSprites();
            CacheMonsterPrefabs();
            CacheNpcPrefabs();
        }

        ///<summary>
        /// 배경 스프라이트 목록 캐시
        ///</summary>
        private void CacheBackgroundSprites()
        {
            backgroundSpriteByName.Clear();
            Sprite[] loadedBackgroundSprites = Resources.LoadAll<Sprite>( BackgroundSpriteResourceFolderPath );
            int backgroundSpriteCount = loadedBackgroundSprites.Length;

            for ( int index = 0; index < backgroundSpriteCount; index++ )
            {
                Sprite backgroundSprite = loadedBackgroundSprites[ index ];

                if ( backgroundSprite == null )
                {
                    continue;
                }

                backgroundSpriteByName[ backgroundSprite.name ] = backgroundSprite;
            }
        }

        ///<summary>
        /// 몬스터 프리팹 목록 캐시
        ///</summary>
        private void CacheMonsterPrefabs()
        {
            monsterPrefabByName.Clear();
            GameObject[] loadedMonsterPrefabs = Resources.LoadAll<GameObject>( MonsterPrefabResourceFolderPath );
            int monsterPrefabCount = loadedMonsterPrefabs.Length;

            for ( int index = 0; index < monsterPrefabCount; index++ )
            {
                GameObject monsterPrefab = loadedMonsterPrefabs[ index ];

                if ( monsterPrefab == null )
                {
                    continue;
                }

                monsterPrefabByName[ monsterPrefab.name ] = monsterPrefab;
            }
        }

        ///<summary>
        /// NPC 프리팹 목록 캐시
        ///</summary>
        private void CacheNpcPrefabs()
        {
            npcPrefabByName.Clear();
            GameObject[] loadedNpcPrefabs = Resources.LoadAll<GameObject>( NpcPrefabResourceFolderPath );
            int npcPrefabCount = loadedNpcPrefabs.Length;

            for ( int index = 0; index < npcPrefabCount; index++ )
            {
                GameObject npcPrefab = loadedNpcPrefabs[ index ];

                if ( npcPrefab == null )
                {
                    continue;
                }

                npcPrefabByName[ npcPrefab.name ] = npcPrefab;
            }
        }

        ///<summary>
        /// 페이드 오버레이 존재 보장
        ///</summary>
        private void EnsureFadeOverlayExists()
        {
            if ( fadeCanvas != null && fadeImage != null )
            {
                EnsureMapLoadingUiExists();
                return;
            }

            GameObject existingCanvasObject = GameObject.Find( FadeCanvasObjectName );

            if ( existingCanvasObject != null )
            {
                Canvas existingCanvas = existingCanvasObject.GetComponent<Canvas>();
                Transform existingFadeImageTransform = existingCanvasObject.transform.Find( FadeImageObjectName );
                Image existingFadeImage = existingFadeImageTransform != null ? existingFadeImageTransform.GetComponent<Image>() : existingCanvasObject.GetComponentInChildren<Image>( true );
                GraphicRaycaster existingGraphicRaycaster = existingCanvasObject.GetComponent<GraphicRaycaster>();
                fadeCanvas = existingCanvas;
                fadeImage = existingFadeImage;
                fadeGraphicRaycaster = existingGraphicRaycaster;

                if ( fadeImage != null )
                {
                    Color existingColor = fadeImage.color;
                    existingColor.a = 0.0f;
                    fadeImage.color = existingColor;
                }

                UpdateFadeRaycastState( 0.0f );
                EnsureMapLoadingUiExists();

                return;
            }

            GameObject fadeCanvasObject = new GameObject( FadeCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
            Canvas createdCanvas = fadeCanvasObject.GetComponent<Canvas>();
            GraphicRaycaster createdGraphicRaycaster = fadeCanvasObject.GetComponent<GraphicRaycaster>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.sortingOrder = FadeSortingOrder;
            CanvasScaler canvasScaler = fadeCanvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );

            GameObject fadeImageObject = new GameObject( FadeImageObjectName, typeof( RectTransform ), typeof( Image ) );
            RectTransform fadeImageRectTransform = fadeImageObject.GetComponent<RectTransform>();
            fadeImageRectTransform.SetParent( fadeCanvasObject.transform, false );
            fadeImageRectTransform.anchorMin = Vector2.zero;
            fadeImageRectTransform.anchorMax = Vector2.one;
            fadeImageRectTransform.offsetMin = Vector2.zero;
            fadeImageRectTransform.offsetMax = Vector2.zero;
            Image createdFadeImage = fadeImageObject.GetComponent<Image>();
            createdFadeImage.color = new Color( 0.0f, 0.0f, 0.0f, 0.0f );
            createdFadeImage.raycastTarget = false;

            fadeCanvas = createdCanvas;
            fadeImage = createdFadeImage;
            fadeGraphicRaycaster = createdGraphicRaycaster;
            UpdateFadeRaycastState( 0.0f );
            EnsureMapLoadingUiExists();
        }

        ///<summary>
        /// 맵 로딩 UI 존재 보장
        ///</summary>
        private void EnsureMapLoadingUiExists()
        {
            if ( mapLoadingUi != null )
            {
                return;
            }

            if ( fadeCanvas == null )
            {
                return;
            }

            RectTransform fadeCanvasRectTransform = fadeCanvas.transform as RectTransform;

            if ( fadeCanvasRectTransform == null )
            {
                return;
            }

            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                Debug.LogWarning( "[ MapManager ] ResourceManager is not ready for MapLoadingUI." );
                return;
            }

            GameObject mapLoadingUiPrefab = resourceManager.GetMapLoadingUiPrefab();

            if ( mapLoadingUiPrefab == null )
            {
                return;
            }

            GameObject createdUiObject = Instantiate( mapLoadingUiPrefab, fadeCanvasRectTransform );
            createdUiObject.name = MapLoadingUiObjectName;
            mapLoadingUi = createdUiObject.GetComponent<CMapLoadingUI>();

            if ( mapLoadingUi == null )
            {
                Debug.LogWarning( "[ MapManager ] MapLoadingUI prefab is missing CMapLoadingUI component." );
                Destroy( createdUiObject );
                return;
            }

            RectTransform createdUiRectTransform = createdUiObject.transform as RectTransform;

            if ( createdUiRectTransform != null )
            {
                createdUiRectTransform.SetAsLastSibling();
            }

            mapLoadingUi.Hide();
        }

        ///<summary>
        /// 맵 로딩 진행 상태 표시
        ///</summary>
        private void ShowMapLoadingProgress( string _statusText, float _progress )
        {
            EnsureMapLoadingUiExists();

            if ( mapLoadingUi == null )
            {
                return;
            }

            mapLoadingUi.Show( _statusText, _progress );
        }

        ///<summary>
        /// 맵 로딩 진행 상태 숨김
        ///</summary>
        private void HideMapLoadingProgress()
        {
            if ( mapLoadingUi == null )
            {
                return;
            }

            mapLoadingUi.Hide();
        }

        ///<summary>
        /// 맵 저장 데이터 Resources 로드
        ///</summary>
        private CMapToolSaveData LoadMapSaveDataFromResources(string _mapId)
        {
            string resourcePath = BuildMapDataResourcePath( _mapId );
            TextAsset textAsset = Resources.Load<TextAsset>( resourcePath );
            CMapToolSaveData loadedData = CreateMapSaveDataFromTextAsset( textAsset );
            return loadedData;
        }

        ///<summary>
        /// 맵 데이터 리소스 경로 구성
        ///</summary>
        private string BuildMapDataResourcePath( string _mapId )
        {
            string trimmedMapId = string.IsNullOrWhiteSpace( _mapId ) ? string.Empty : _mapId.Trim();
            string result = MapDataResourceFolderPath + trimmedMapId;
            return result;
        }

        ///<summary>
        /// TextAsset 기반 맵 저장 데이터 생성
        ///</summary>
        private CMapToolSaveData CreateMapSaveDataFromTextAsset( TextAsset _textAsset )
        {
            if ( _textAsset == null )
            {
                return null;
            }

            string jsonText = _textAsset.text;

            if ( string.IsNullOrWhiteSpace( jsonText ) )
            {
                return null;
            }

            CMapToolSaveData loadedData = JsonUtility.FromJson<CMapToolSaveData>( jsonText );
            return loadedData;
        }

        ///<summary>
        /// 맵 데이터 비동기 로드 및 적용
        ///</summary>
        private IEnumerator IE_LoadMapDataAndApply( string _mapId, string _entryPortalId, System.Action<bool> _onCompleted )
        {
            if ( isMapLoadInProgress )
            {
                InvokeMapLoadCompletedHandler( _onCompleted, false );
                yield break;
            }

            isMapLoadInProgress = true;
            bool isLoadCompleted = false;
            TextAsset loadedTextAsset = null;
            ShowMapLoadingProgress( LoadingMapDataText, LoadingMapDataProgress );
            LoadMapTextAssetAsync( _mapId, delegate( TextAsset _loadedTextAsset )
            {
                loadedTextAsset = _loadedTextAsset;
                isLoadCompleted = true;
            } );

            while ( isLoadCompleted == false )
            {
                yield return null;
            }

            CMapToolSaveData loadedData = CreateMapSaveDataFromTextAsset( loadedTextAsset );
            Sprite loadedBackgroundSprite = null;
            GameObject loadedPortalPrefab = null;
            bool wasApplied = loadedData != null;

            if ( wasApplied )
            {
                bool isBackgroundLoadCompleted = false;
                ShowMapLoadingProgress( LoadingBackgroundText, LoadingBackgroundProgress );
                LoadBackgroundSpriteAsync( loadedData.backgroundSpriteName, delegate( Sprite _loadedBackgroundSprite )
                {
                    loadedBackgroundSprite = _loadedBackgroundSprite;
                    isBackgroundLoadCompleted = true;
                } );

                while ( isBackgroundLoadCompleted == false )
                {
                    yield return null;
                }

                bool isPortalLoadCompleted = false;
                ShowMapLoadingProgress( LoadingPortalText, LoadingPortalProgress );
                LoadPortalPrefabAsync( delegate( GameObject _loadedPortalPrefab )
                {
                    loadedPortalPrefab = _loadedPortalPrefab;
                    isPortalLoadCompleted = true;
                } );

                while ( isPortalLoadCompleted == false )
                {
                    yield return null;
                }

                bool isMonsterPrefabLoadCompleted = false;
                ShowMapLoadingProgress( LoadingMonsterText, LoadingMonsterProgress );
                LoadRequiredMonsterPrefabsAsync( loadedData.monsters, delegate
                {
                    isMonsterPrefabLoadCompleted = true;
                } );

                while ( isMonsterPrefabLoadCompleted == false )
                {
                    yield return null;
                }

                bool isNpcPrefabLoadCompleted = false;
                ShowMapLoadingProgress( LoadingNpcText, LoadingNpcProgress );
                LoadRequiredNpcPrefabsAsync( loadedData.npcs, delegate
                {
                    isNpcPrefabLoadCompleted = true;
                } );

                while ( isNpcPrefabLoadCompleted == false )
                {
                    yield return null;
                }
            }

            if ( wasApplied )
            {
                ShowMapLoadingProgress( ApplyingMapText, ApplyingMapProgress );
                ApplyMapData( loadedData, _entryPortalId, loadedBackgroundSprite, loadedPortalPrefab );
                ShowMapLoadingProgress( LoadingCompleteText, LoadingCompleteProgress );
            }
            else
            {
                ShowMapLoadingProgress( LoadingFailedText, LoadingCompleteProgress );
                Debug.LogWarning( $"[ MapManager ] MapData load failed: {_mapId}" );
            }

            isMapLoadInProgress = false;
            InvokeMapLoadCompletedHandler( _onCompleted, wasApplied );
        }

        ///<summary>
        /// 맵 TextAsset 비동기 로드
        ///</summary>
        private void LoadMapTextAssetAsync( string _mapId, System.Action<TextAsset> _onCompleted )
        {
            string resourcePath = BuildMapDataResourcePath( _mapId );
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                TextAsset fallbackTextAsset = Resources.Load<TextAsset>( resourcePath );
                InvokeMapTextAssetLoadedHandler( _onCompleted, fallbackTextAsset );
                return;
            }

            resourceManager.LoadAssetAsync<TextAsset>( resourcePath, resourcePath, _onCompleted );
        }

        ///<summary>
        /// 맵 TextAsset 로드 콜백 호출
        ///</summary>
        private void InvokeMapTextAssetLoadedHandler( System.Action<TextAsset> _onCompleted, TextAsset _textAsset )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _textAsset );
        }

        ///<summary>
        /// 맵 로드 완료 콜백 호출
        ///</summary>
        private void InvokeMapLoadCompletedHandler( System.Action<bool> _onCompleted, bool _wasLoaded )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _wasLoaded );
        }

        ///<summary>
        /// 배경 스프라이트 리소스 경로 구성
        ///</summary>
        private string BuildBackgroundSpriteResourcePath( string _backgroundSpriteName )
        {
            string trimmedBackgroundSpriteName = string.IsNullOrWhiteSpace( _backgroundSpriteName ) ? string.Empty : _backgroundSpriteName.Trim();
            string result = BackgroundSpriteResourceFolderPath + "/" + trimmedBackgroundSpriteName;
            return result;
        }

        ///<summary>
        /// 배경 스프라이트 비동기 로드
        ///</summary>
        private void LoadBackgroundSpriteAsync( string _backgroundSpriteName, System.Action<Sprite> _onCompleted )
        {
            if ( string.IsNullOrWhiteSpace( _backgroundSpriteName ) )
            {
                InvokeBackgroundSpriteLoadedHandler( _onCompleted, null );
                return;
            }

            string resourcePath = BuildBackgroundSpriteResourcePath( _backgroundSpriteName );
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                Sprite fallbackSprite = ResolveCachedBackgroundSprite( _backgroundSpriteName );
                InvokeBackgroundSpriteLoadedHandler( _onCompleted, fallbackSprite );
                return;
            }

            resourceManager.LoadAssetAsync<Sprite>( resourcePath, resourcePath, delegate( Sprite _loadedSprite )
            {
                Sprite resolvedSprite = _loadedSprite != null ? _loadedSprite : ResolveCachedBackgroundSprite( _backgroundSpriteName );
                InvokeBackgroundSpriteLoadedHandler( _onCompleted, resolvedSprite );
            } );
        }

        ///<summary>
        /// 배경 스프라이트 로드 콜백 호출
        ///</summary>
        private void InvokeBackgroundSpriteLoadedHandler( System.Action<Sprite> _onCompleted, Sprite _loadedSprite )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _loadedSprite );
        }

        ///<summary>
        /// 캐시된 배경 스프라이트 결정
        ///</summary>
        private Sprite ResolveCachedBackgroundSprite( string _backgroundSpriteName )
        {
            if ( string.IsNullOrWhiteSpace( _backgroundSpriteName ) )
            {
                return null;
            }

            string trimmedBackgroundSpriteName = _backgroundSpriteName.Trim();
            bool hasCachedSprite = backgroundSpriteByName.TryGetValue( trimmedBackgroundSpriteName, out Sprite cachedSprite );
            Sprite result = hasCachedSprite ? cachedSprite : null;
            return result;
        }

        ///<summary>
        /// 포탈 프리팹 비동기 로드
        ///</summary>
        private void LoadPortalPrefabAsync( System.Action<GameObject> _onCompleted )
        {
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                GameObject fallbackPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );
                InvokePortalPrefabLoadedHandler( _onCompleted, fallbackPrefab );
                return;
            }

            resourceManager.LoadAssetAsync<GameObject>( PortalPrefabResourcePath, PortalPrefabResourcePath, delegate( GameObject _loadedPrefab )
            {
                GameObject resolvedPrefab = _loadedPrefab != null ? _loadedPrefab : Resources.Load<GameObject>( PortalPrefabResourcePath );
                InvokePortalPrefabLoadedHandler( _onCompleted, resolvedPrefab );
            } );
        }

        ///<summary>
        /// 포탈 프리팹 로드 콜백 호출
        ///</summary>
        private void InvokePortalPrefabLoadedHandler( System.Action<GameObject> _onCompleted, GameObject _portalPrefab )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _portalPrefab );
        }

        ///<summary>
        /// 필요 몬스터 프리팹 목록 비동기 로드
        ///</summary>
        private void LoadRequiredMonsterPrefabsAsync( List<CMapToolMonsterSaveData> _monsterSaveDataList, System.Action _onCompleted )
        {
            List<CMapToolMonsterSaveData> uniqueMonsterSaveDataList = CollectUniqueMonsterSaveDataList( _monsterSaveDataList );

            if ( uniqueMonsterSaveDataList.Count == 0 )
            {
                InvokeMonsterPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            int remainingLoadCount = uniqueMonsterSaveDataList.Count;

            for ( int index = 0; index < uniqueMonsterSaveDataList.Count; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = uniqueMonsterSaveDataList[ index ];
                LoadMonsterPrefabAsync( monsterSaveData, delegate
                {
                    remainingLoadCount--;

                    if ( remainingLoadCount <= 0 )
                    {
                        InvokeMonsterPrefabLoadCompletedHandler( _onCompleted );
                    }
                } );
            }
        }

        ///<summary>
        /// 중복 제거된 몬스터 저장 데이터 목록 구성
        ///</summary>
        private List<CMapToolMonsterSaveData> CollectUniqueMonsterSaveDataList( List<CMapToolMonsterSaveData> _monsterSaveDataList )
        {
            List<CMapToolMonsterSaveData> uniqueMonsterSaveDataList = new List<CMapToolMonsterSaveData>();
            HashSet<string> monsterKeySet = new HashSet<string>();

            if ( _monsterSaveDataList == null )
            {
                return uniqueMonsterSaveDataList;
            }

            int monsterCount = _monsterSaveDataList.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = _monsterSaveDataList[ index ];

                if ( monsterSaveData == null )
                {
                    continue;
                }

                string monsterKey = ResolveMonsterPoolKey( monsterSaveData );

                if ( string.IsNullOrWhiteSpace( monsterKey ) )
                {
                    continue;
                }

                if ( monsterKeySet.Add( monsterKey ) == false )
                {
                    continue;
                }

                uniqueMonsterSaveDataList.Add( monsterSaveData );
            }

            return uniqueMonsterSaveDataList;
        }

        ///<summary>
        /// 몬스터 프리팹 비동기 로드
        ///</summary>
        private void LoadMonsterPrefabAsync( CMapToolMonsterSaveData _monsterSaveData, System.Action _onCompleted )
        {
            if ( _monsterSaveData == null )
            {
                InvokeMonsterPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            string addressableKey = BuildMonsterPrefabAddressableKey( _monsterSaveData );
            string fallbackResourcePath = ResolveMonsterFallbackResourcePath( _monsterSaveData );
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                GameObject fallbackPrefab = Resources.Load<GameObject>( fallbackResourcePath );
                CacheLoadedMonsterPrefab( _monsterSaveData, fallbackPrefab );
                InvokeMonsterPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            resourceManager.LoadAssetAsync<GameObject>( addressableKey, fallbackResourcePath, delegate( GameObject _loadedPrefab )
            {
                GameObject resolvedPrefab = _loadedPrefab != null ? _loadedPrefab : Resources.Load<GameObject>( fallbackResourcePath );
                CacheLoadedMonsterPrefab( _monsterSaveData, resolvedPrefab );
                InvokeMonsterPrefabLoadCompletedHandler( _onCompleted );
            } );
        }

        ///<summary>
        /// 몬스터 프리팹 Addressables 키 구성
        ///</summary>
        private string BuildMonsterPrefabAddressableKey( CMapToolMonsterSaveData _monsterSaveData )
        {
            string fallbackResourcePath = ResolveMonsterFallbackResourcePath( _monsterSaveData );

            if ( string.IsNullOrWhiteSpace( fallbackResourcePath ) == false )
            {
                return fallbackResourcePath;
            }

            string prefabName = _monsterSaveData != null && string.IsNullOrWhiteSpace( _monsterSaveData.prefabName ) == false ? _monsterSaveData.prefabName.Trim() : string.Empty;
            string result = string.IsNullOrWhiteSpace( prefabName ) ? string.Empty : MonsterPrefabResourceFolderPath + "/" + prefabName;
            return result;
        }

        ///<summary>
        /// 몬스터 프리팹 fallback 리소스 경로 결정
        ///</summary>
        private string ResolveMonsterFallbackResourcePath( CMapToolMonsterSaveData _monsterSaveData )
        {
            if ( _monsterSaveData == null )
            {
                return string.Empty;
            }

            if ( string.IsNullOrWhiteSpace( _monsterSaveData.resourcePath ) == false )
            {
                string resultFromResourcePath = _monsterSaveData.resourcePath.Trim();
                return resultFromResourcePath;
            }

            if ( string.IsNullOrWhiteSpace( _monsterSaveData.prefabName ) == false )
            {
                string resultFromPrefabName = MonsterPrefabResourceFolderPath + "/" + _monsterSaveData.prefabName.Trim();
                return resultFromPrefabName;
            }

            return string.Empty;
        }

        ///<summary>
        /// 로드된 몬스터 프리팹 캐시
        ///</summary>
        private void CacheLoadedMonsterPrefab( CMapToolMonsterSaveData _monsterSaveData, GameObject _monsterPrefab )
        {
            if ( _monsterSaveData == null || _monsterPrefab == null )
            {
                return;
            }

            monsterPrefabByName[ _monsterPrefab.name ] = _monsterPrefab;

            if ( string.IsNullOrWhiteSpace( _monsterSaveData.prefabName ) == false )
            {
                string prefabName = _monsterSaveData.prefabName.Trim();
                monsterPrefabByName[ prefabName ] = _monsterPrefab;
            }

            string monsterPoolKey = ResolveMonsterPoolKey( _monsterSaveData );

            if ( string.IsNullOrWhiteSpace( monsterPoolKey ) == false )
            {
                monsterPrefabByName[ monsterPoolKey ] = _monsterPrefab;
            }
        }

        ///<summary>
        /// 몬스터 프리팹 로드 완료 콜백 호출
        ///</summary>
        private void InvokeMonsterPrefabLoadCompletedHandler( System.Action _onCompleted )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke();
        }

        ///<summary>
        /// 필요 NPC 프리팹 목록 비동기 로드
        ///</summary>
        private void LoadRequiredNpcPrefabsAsync( List<CMapToolNpcSaveData> _npcSaveDataList, System.Action _onCompleted )
        {
            List<CMapToolNpcSaveData> uniqueNpcSaveDataList = CollectUniqueNpcSaveDataList( _npcSaveDataList );

            if ( uniqueNpcSaveDataList.Count == 0 )
            {
                InvokeNpcPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            int remainingLoadCount = uniqueNpcSaveDataList.Count;

            for ( int index = 0; index < uniqueNpcSaveDataList.Count; index++ )
            {
                CMapToolNpcSaveData npcSaveData = uniqueNpcSaveDataList[ index ];
                LoadNpcPrefabAsync( npcSaveData, delegate
                {
                    remainingLoadCount--;

                    if ( remainingLoadCount <= 0 )
                    {
                        InvokeNpcPrefabLoadCompletedHandler( _onCompleted );
                    }
                } );
            }
        }

        ///<summary>
        /// 중복 제거된 NPC 저장 데이터 목록 구성
        ///</summary>
        private List<CMapToolNpcSaveData> CollectUniqueNpcSaveDataList( List<CMapToolNpcSaveData> _npcSaveDataList )
        {
            List<CMapToolNpcSaveData> uniqueNpcSaveDataList = new List<CMapToolNpcSaveData>();
            HashSet<string> npcKeySet = new HashSet<string>();

            if ( _npcSaveDataList == null )
            {
                return uniqueNpcSaveDataList;
            }

            int npcCount = _npcSaveDataList.Count;

            for ( int index = 0; index < npcCount; index++ )
            {
                CMapToolNpcSaveData npcSaveData = _npcSaveDataList[ index ];

                if ( npcSaveData == null )
                {
                    continue;
                }

                string npcKey = ResolveNpcPrefabKey( npcSaveData );

                if ( string.IsNullOrWhiteSpace( npcKey ) )
                {
                    continue;
                }

                if ( npcKeySet.Add( npcKey ) == false )
                {
                    continue;
                }

                uniqueNpcSaveDataList.Add( npcSaveData );
            }

            return uniqueNpcSaveDataList;
        }

        ///<summary>
        /// NPC 프리팹 비동기 로드
        ///</summary>
        private void LoadNpcPrefabAsync( CMapToolNpcSaveData _npcSaveData, System.Action _onCompleted )
        {
            if ( _npcSaveData == null )
            {
                InvokeNpcPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            string addressableKey = BuildNpcPrefabAddressableKey( _npcSaveData );
            string fallbackResourcePath = ResolveNpcFallbackResourcePath( _npcSaveData );
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                GameObject fallbackPrefab = Resources.Load<GameObject>( fallbackResourcePath );
                CacheLoadedNpcPrefab( _npcSaveData, fallbackPrefab );
                InvokeNpcPrefabLoadCompletedHandler( _onCompleted );
                return;
            }

            resourceManager.LoadAssetAsync<GameObject>( addressableKey, fallbackResourcePath, delegate( GameObject _loadedPrefab )
            {
                GameObject resolvedPrefab = _loadedPrefab != null ? _loadedPrefab : Resources.Load<GameObject>( fallbackResourcePath );
                CacheLoadedNpcPrefab( _npcSaveData, resolvedPrefab );
                InvokeNpcPrefabLoadCompletedHandler( _onCompleted );
            } );
        }

        ///<summary>
        /// NPC 프리팹 Addressables 키 구성
        ///</summary>
        private string BuildNpcPrefabAddressableKey( CMapToolNpcSaveData _npcSaveData )
        {
            string fallbackResourcePath = ResolveNpcFallbackResourcePath( _npcSaveData );

            if ( string.IsNullOrWhiteSpace( fallbackResourcePath ) == false )
            {
                return fallbackResourcePath;
            }

            string prefabName = _npcSaveData != null && string.IsNullOrWhiteSpace( _npcSaveData.prefabName ) == false ? _npcSaveData.prefabName.Trim() : string.Empty;
            string result = string.IsNullOrWhiteSpace( prefabName ) ? string.Empty : NpcPrefabResourceFolderPath + "/" + prefabName;
            return result;
        }

        ///<summary>
        /// NPC 프리팹 fallback 리소스 경로 결정
        ///</summary>
        private string ResolveNpcFallbackResourcePath( CMapToolNpcSaveData _npcSaveData )
        {
            if ( _npcSaveData == null )
            {
                return string.Empty;
            }

            if ( string.IsNullOrWhiteSpace( _npcSaveData.resourcePath ) == false )
            {
                string resultFromResourcePath = _npcSaveData.resourcePath.Trim();
                return resultFromResourcePath;
            }

            if ( string.IsNullOrWhiteSpace( _npcSaveData.prefabName ) == false )
            {
                string resultFromPrefabName = NpcPrefabResourceFolderPath + "/" + _npcSaveData.prefabName.Trim();
                return resultFromPrefabName;
            }

            return string.Empty;
        }

        ///<summary>
        /// NPC 프리팹 키 결정
        ///</summary>
        private string ResolveNpcPrefabKey( CMapToolNpcSaveData _npcSaveData )
        {
            if ( _npcSaveData == null )
            {
                return string.Empty;
            }

            if ( string.IsNullOrWhiteSpace( _npcSaveData.prefabName ) == false )
            {
                string resultFromPrefabName = _npcSaveData.prefabName.Trim();
                return resultFromPrefabName;
            }

            if ( string.IsNullOrWhiteSpace( _npcSaveData.resourcePath ) == false )
            {
                string resultFromResourcePath = _npcSaveData.resourcePath.Trim();
                return resultFromResourcePath;
            }

            return string.Empty;
        }

        ///<summary>
        /// 로드된 NPC 프리팹 캐시
        ///</summary>
        private void CacheLoadedNpcPrefab( CMapToolNpcSaveData _npcSaveData, GameObject _npcPrefab )
        {
            if ( _npcSaveData == null || _npcPrefab == null )
            {
                return;
            }

            npcPrefabByName[ _npcPrefab.name ] = _npcPrefab;

            if ( string.IsNullOrWhiteSpace( _npcSaveData.prefabName ) == false )
            {
                string prefabName = _npcSaveData.prefabName.Trim();
                npcPrefabByName[ prefabName ] = _npcPrefab;
            }

            string npcPrefabKey = ResolveNpcPrefabKey( _npcSaveData );

            if ( string.IsNullOrWhiteSpace( npcPrefabKey ) == false )
            {
                npcPrefabByName[ npcPrefabKey ] = _npcPrefab;
            }
        }

        ///<summary>
        /// NPC 프리팹 로드 완료 콜백 호출
        ///</summary>
        private void InvokeNpcPrefabLoadCompletedHandler( System.Action _onCompleted )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke();
        }

        ///<summary>
        /// 맵 데이터 적용
        ///</summary>
        private void ApplyMapData(CMapToolSaveData _loadedData, string _entryPortalId, Sprite _backgroundSprite, GameObject _portalPrefab)
        {
            if ( _loadedData == null )
            {
                return;
            }

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

            ReleaseMapTransitionPooledObjects();
            currentMapId = string.IsNullOrWhiteSpace( _loadedData.mapId ) ? string.Empty : _loadedData.mapId.Trim();
            currentMapName = string.IsNullOrWhiteSpace( _loadedData.mapName ) ? currentMapId : _loadedData.mapName.Trim();
            currentMapRuntimeVersion++;
            HashSet<string> requiredMonsterPoolKeySet = CollectRequiredMonsterPoolKeys( _loadedData.monsters );
            ReturnAllActivePooledMonsters();
            ReturnAllActivePooledWorldItemDrops();
            ClearSpawnedRuntimeObjects();
            PrepareMonsterPools( requiredMonsterPoolKeySet );
            ApplyMapBgm( _loadedData.bgmClipName );
            ApplyBackgroundSprite( _loadedData.backgroundSpriteName, _backgroundSprite );
            ApplyBackgroundRightBoundary( _loadedData );
            SpawnPortals( _loadedData.portals, _portalPrefab );
            SpawnMonsters( _loadedData.monsters );
            SpawnNpcs( _loadedData.npcs );
            MovePlayerToEntryPortal( _entryPortalId );
            EnsureGameplayCameraFollowController();
        }

        ///<summary>
        /// 맵 BGM 적용
        ///</summary>
        private void ApplyMapBgm( string _bgmClipName )
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

            string trimmedBgmClipName = _bgmClipName.Trim();
            audioManager.PlayBgm( trimmedBgmClipName );
        }

        ///<summary>
        /// 배경 스프라이트 적용
        ///</summary>
        private void ApplyBackgroundSprite(string _backgroundSpriteName, Sprite _loadedBackgroundSprite)
        {
            currentBackgroundSprite = null;

            if ( string.IsNullOrWhiteSpace( _backgroundSpriteName ) )
            {
                return;
            }

            WorldSpaceBackgroundFitter targetBackgroundFitter = FindFirstObjectByType<WorldSpaceBackgroundFitter>();

            if ( targetBackgroundFitter == null )
            {
                return;
            }

            SpriteRenderer targetBackgroundRenderer = targetBackgroundFitter.GetComponent<SpriteRenderer>();

            if ( targetBackgroundRenderer == null )
            {
                return;
            }

            Sprite backgroundSprite = _loadedBackgroundSprite != null ? _loadedBackgroundSprite : ResolveCachedBackgroundSprite( _backgroundSpriteName );

            if ( backgroundSprite == null )
            {
                return;
            }

            currentBackgroundSprite = backgroundSprite;
            targetBackgroundRenderer.sprite = backgroundSprite;
            targetBackgroundFitter.ApplyFit();
            CMapBackgroundLayoutController backgroundLayoutController = EnsureBackgroundLayoutController( targetBackgroundRenderer );

            if ( backgroundLayoutController != null )
            {
                backgroundLayoutController.RefreshLayout();
            }

            CMapToolBackgroundColliderVisualizer colliderVisualizer = targetBackgroundRenderer.GetComponent<CMapToolBackgroundColliderVisualizer>();

            if ( colliderVisualizer != null )
            {
                colliderVisualizer.RefreshColliderVisual();
            }
        }

        ///<summary>
        /// 배경 레이아웃 컨트롤러 보장
        ///</summary>
        private CMapBackgroundLayoutController EnsureBackgroundLayoutController( SpriteRenderer _targetBackgroundRenderer )
        {
            if ( _targetBackgroundRenderer == null )
            {
                return null;
            }

            CMapBackgroundLayoutController backgroundLayoutController = _targetBackgroundRenderer.GetComponent<CMapBackgroundLayoutController>();

            if ( backgroundLayoutController == null )
            {
                backgroundLayoutController = _targetBackgroundRenderer.gameObject.AddComponent<CMapBackgroundLayoutController>();
            }

            return backgroundLayoutController;
        }

        ///<summary>
        /// 저장된 배경 우측 경계 적용
        ///</summary>
        private void ApplyBackgroundRightBoundary( CMapToolSaveData _loadedData )
        {
            WorldSpaceBackgroundFitter targetBackgroundFitter = FindFirstObjectByType<WorldSpaceBackgroundFitter>();

            if ( targetBackgroundFitter == null )
            {
                return;
            }

            SpriteRenderer targetBackgroundRenderer = targetBackgroundFitter.GetComponent<SpriteRenderer>();
            CMapBackgroundLayoutController backgroundLayoutController = EnsureBackgroundLayoutController( targetBackgroundRenderer );

            if ( backgroundLayoutController == null )
            {
                return;
            }

            if ( _loadedData != null && _loadedData.hasCustomRightBoundary )
            {
                backgroundLayoutController.SetCustomRightBoundaryX( _loadedData.customRightBoundaryX );
                RefreshBackgroundColliderVisual( targetBackgroundRenderer );
                return;
            }

            backgroundLayoutController.ClearCustomRightBoundary();
            RefreshBackgroundColliderVisual( targetBackgroundRenderer );
        }

        ///<summary>
        /// 배경 콜라이더 시각화 갱신
        ///</summary>
        private void RefreshBackgroundColliderVisual( SpriteRenderer _targetBackgroundRenderer )
        {
            if ( _targetBackgroundRenderer == null )
            {
                return;
            }

            CMapToolBackgroundColliderVisualizer colliderVisualizer = _targetBackgroundRenderer.GetComponent<CMapToolBackgroundColliderVisualizer>();

            if ( colliderVisualizer == null )
            {
                return;
            }

            colliderVisualizer.RefreshColliderVisual();
        }

        ///<summary>
        /// 포탈 목록 생성
        ///</summary>
        private void SpawnPortals(List<CMapToolPortalSaveData> _portalSaveDataList, GameObject _loadedPortalPrefab)
        {
            GameObject portalPrefab = _loadedPortalPrefab != null ? _loadedPortalPrefab : Resources.Load<GameObject>( PortalPrefabResourcePath );

            if ( portalPrefab == null )
            {
                return;
            }

            int portalCount = _portalSaveDataList.Count;

            for ( int index = 0; index < portalCount; index++ )
            {
                CMapToolPortalSaveData portalSaveData = _portalSaveDataList[ index ];

                if ( portalSaveData == null )
                {
                    continue;
                }

                CMapToolTransformData transformData = portalSaveData.transform;

                if ( transformData == null )
                {
                    transformData = BuildDefaultTransformData( portalPrefab.transform );
                }

                Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
                Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
                Vector3 spawnScale = CreateVector3FromTransformData( transformData.scale, portalPrefab.transform.localScale );
                GameObject portalObject = Instantiate( portalPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
                portalObject.transform.localScale = spawnScale;
                portalObject.name = portalPrefab.name;
                PortalObject portalComponent = portalObject.GetComponent<PortalObject>();

                if ( portalComponent != null )
                {
                    portalComponent.ConfigurePortal( portalSaveData.portalId, portalSaveData.targetMapId, portalSaveData.targetPortalId );
                }

                RegisterSpawnedRuntimeObject( portalObject );
            }
        }

        ///<summary>
        /// 몬스터 목록 생성
        ///</summary>
        private void SpawnMonsters(List<CMapToolMonsterSaveData> _monsterSaveDataList)
        {
            int monsterCount = _monsterSaveDataList.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = _monsterSaveDataList[ index ];

                if ( monsterSaveData == null || string.IsNullOrWhiteSpace( monsterSaveData.prefabName ) )
                {
                    continue;
                }

                GameObject monsterPrefab = ResolveMonsterPrefab( monsterSaveData.prefabName, monsterSaveData.resourcePath );

                if ( monsterPrefab == null )
                {
                    continue;
                }

                CMapToolTransformData transformData = monsterSaveData.transform;

                if ( transformData == null )
                {
                    transformData = BuildDefaultTransformData( monsterPrefab.transform );
                }

                string monsterPoolKey = ResolveMonsterPoolKey( monsterSaveData );
                Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
                Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
                Vector3 spawnScale = ResolveMonsterSpawnScale( monsterPrefab );
                MonsterObject monsterComponent = AcquirePooledMonster( monsterPoolKey, monsterPrefab );

                if ( monsterComponent != null )
                {
                    InitializeSpawnedMonster( monsterComponent, monsterPoolKey, monsterPrefab.name, monsterPrefab.name, spawnPosition, spawnRotation, spawnScale );
                    RegisterActivePooledMonster( monsterComponent );
                }
            }
        }

        ///<summary>
        /// NPC 목록 생성
        ///</summary>
        private void SpawnNpcs( List<CMapToolNpcSaveData> _npcSaveDataList )
        {
            int npcCount = _npcSaveDataList.Count;

            for ( int index = 0; index < npcCount; index++ )
            {
                CMapToolNpcSaveData npcSaveData = _npcSaveDataList[ index ];

                if ( npcSaveData == null || string.IsNullOrWhiteSpace( npcSaveData.prefabName ) )
                {
                    continue;
                }

                GameObject npcPrefab = ResolveNpcPrefab( npcSaveData.prefabName, npcSaveData.resourcePath );

                if ( npcPrefab == null )
                {
                    continue;
                }

                CMapToolTransformData transformData = npcSaveData.transform;

                if ( transformData == null )
                {
                    transformData = BuildDefaultTransformData( npcPrefab.transform );
                }

                Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
                Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
                Vector3 spawnScale = CreateVector3FromTransformData( transformData.scale, npcPrefab.transform.localScale );
                GameObject npcInstance = Instantiate( npcPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
                npcInstance.transform.localScale = spawnScale;
                npcInstance.name = npcPrefab.name;
                RegisterSpawnedRuntimeObject( npcInstance );
            }
        }

        ///<summary>
        /// 맵 사용 몬스터 풀 키 수집
        ///</summary>
        private HashSet<string> CollectRequiredMonsterPoolKeys( List<CMapToolMonsterSaveData> _monsterSaveDataList )
        {
            HashSet<string> requiredPoolKeySet = new HashSet<string>();

            if ( _monsterSaveDataList == null )
            {
                return requiredPoolKeySet;
            }

            int monsterCount = _monsterSaveDataList.Count;

            for ( int index = 0; index < monsterCount; index++ )
            {
                CMapToolMonsterSaveData monsterSaveData = _monsterSaveDataList[ index ];

                if ( monsterSaveData == null )
                {
                    continue;
                }

                string monsterPoolKey = ResolveMonsterPoolKey( monsterSaveData );

                if ( string.IsNullOrWhiteSpace( monsterPoolKey ) )
                {
                    continue;
                }

                requiredPoolKeySet.Add( monsterPoolKey );
            }

            return requiredPoolKeySet;
        }

        ///<summary>
        /// 현재 맵 몬스터 풀 구성
        ///</summary>
        private void PrepareMonsterPools( HashSet<string> _requiredMonsterPoolKeySet )
        {
            if ( _requiredMonsterPoolKeySet == null )
            {
                return;
            }

            EnsureMonsterPools( _requiredMonsterPoolKeySet );
            ClearUnusedMonsterPools( _requiredMonsterPoolKeySet );
        }

        ///<summary>
        /// 필요 몬스터 풀 생성 보장
        ///</summary>
        private void EnsureMonsterPools( HashSet<string> _requiredMonsterPoolKeySet )
        {
            foreach ( string poolKey in _requiredMonsterPoolKeySet )
            {
                if ( string.IsNullOrWhiteSpace( poolKey ) )
                {
                    continue;
                }

                GetOrCreateMonsterPool( poolKey );
            }
        }

        ///<summary>
        /// 미사용 몬스터 풀 정리
        ///</summary>
        private void ClearUnusedMonsterPools( HashSet<string> _requiredMonsterPoolKeySet )
        {
            List<string> removalKeyList = new List<string>();

            foreach ( string poolKey in monsterPoolKeySet )
            {
                if ( _requiredMonsterPoolKeySet.Contains( poolKey ) )
                {
                    continue;
                }

                string managedPoolKey = BuildManagedMonsterPoolKey( poolKey );
                CObjectPoolManager.TryClearPool( managedPoolKey );

                removalKeyList.Add( poolKey );
            }

            for ( int index = 0; index < removalKeyList.Count; index++ )
            {
                string removalKey = removalKeyList[ index ];
                monsterPoolKeySet.Remove( removalKey );
            }
        }

        ///<summary>
        /// 활성 풀 몬스터 일괄 반환
        ///</summary>
        private void ReturnAllActivePooledMonsters()
        {
            List<MonsterObject> activeMonsterList = new List<MonsterObject>( activePooledMonsterObjects );

            for ( int index = 0; index < activeMonsterList.Count; index++ )
            {
                MonsterObject monsterObject = activeMonsterList[ index ];

                if ( monsterObject == null )
                {
                    continue;
                }

                ReleasePooledMonsterInternal( monsterObject, monsterObject.GetMapRuntimePoolKey(), false );
            }

            activePooledMonsterObjects.Clear();
        }

        ///<summary>
        /// 몬스터 풀 키 결정
        ///</summary>
        private string ResolveMonsterPoolKey( CMapToolMonsterSaveData _monsterSaveData )
        {
            if ( _monsterSaveData == null )
            {
                return string.Empty;
            }

            if ( string.IsNullOrWhiteSpace( _monsterSaveData.prefabName ) == false )
            {
                string resultFromPrefabName = _monsterSaveData.prefabName.Trim();
                return resultFromPrefabName;
            }

            if ( string.IsNullOrWhiteSpace( _monsterSaveData.resourcePath ) == false )
            {
                string resultFromResourcePath = _monsterSaveData.resourcePath.Trim();
                return resultFromResourcePath;
            }

            return string.Empty;
        }

        ///<summary>
        /// 몬스터 풀 획득 또는 생성
        ///</summary>
        private bool GetOrCreateMonsterPool( string _monsterPoolKey )
        {
            if ( string.IsNullOrWhiteSpace( _monsterPoolKey ) )
            {
                return false;
            }

            if ( monsterPoolKeySet.Contains( _monsterPoolKey ) )
            {
                return true;
            }

            GameObject monsterPrefab = ResolveMonsterPrefab( _monsterPoolKey, _monsterPoolKey );

            if ( monsterPrefab == null )
            {
                monsterPrefab = ResolveMonsterPrefab( _monsterPoolKey, string.Empty );
            }

            if ( monsterPrefab == null )
            {
                return false;
            }

            string managedPoolKey = BuildManagedMonsterPoolKey( _monsterPoolKey );
            bool isRegistered = CObjectPoolManager.TryEnsurePoolRegistered<MonsterObject>(
                managedPoolKey,
                () => CreatePooledMonsterInstance( _monsterPoolKey, monsterPrefab ),
                OnGetPooledMonsterInstance,
                OnReleasePooledMonsterInstance,
                OnDestroyPooledMonsterInstance );

            if ( isRegistered == false )
            {
                return false;
            }

            monsterPoolKeySet.Add( _monsterPoolKey );
            return true;
        }

        ///<summary>
        /// 풀 몬스터 대여
        ///</summary>
        private MonsterObject AcquirePooledMonster( string _monsterPoolKey, GameObject _monsterPrefab )
        {
            if ( string.IsNullOrWhiteSpace( _monsterPoolKey ) )
            {
                return null;
            }

            bool isReady = GetOrCreateMonsterPool( _monsterPoolKey );

            if ( isReady == false )
            {
                return null;
            }

            string managedPoolKey = BuildManagedMonsterPoolKey( _monsterPoolKey );

            if ( CObjectPoolManager.TryGet( managedPoolKey, out MonsterObject monsterObject ) == false || monsterObject == null )
            {
                return null;
            }

            monsterObject.SetMapRuntimePoolKey( _monsterPoolKey );
            GameObject monsterGameObject = monsterObject.gameObject;
            monsterGameObject.name = _monsterPrefab != null ? _monsterPrefab.name : _monsterPoolKey;
            return monsterObject;
        }

        ///<summary>
        /// 활성 풀 몬스터 등록
        ///</summary>
        private void RegisterActivePooledMonster( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            bool isAlreadyRegistered = activePooledMonsterObjects.Contains( _monsterObject );

            if ( isAlreadyRegistered )
            {
                return;
            }

            activePooledMonsterObjects.Add( _monsterObject );
        }

        ///<summary>
        /// 풀 몬스터 반환 처리
        ///</summary>
        public bool ReleasePooledMonster( MonsterObject _monsterObject, string _monsterPoolKey )
        {
            bool result = ReleasePooledMonsterInternal( _monsterObject, _monsterPoolKey, true );
            return result;
        }

        ///<summary>
        /// 월드 드랍 오브젝트 풀 생성 시도
        ///</summary>
        public bool TrySpawnWorldItemDrop( GameObject _worldItemDropPrefab, CItemDefinition _itemDefinition, long _itemCount, Vector3 _dropPosition )
        {
            if ( _worldItemDropPrefab == null || _itemDefinition == null || _itemCount <= 0 )
            {
                return false;
            }

            string worldItemDropPoolKey = ResolveWorldItemDropPoolKey( _worldItemDropPrefab );

            if ( string.IsNullOrWhiteSpace( worldItemDropPoolKey ) )
            {
                return false;
            }

            CWorldItemDropObject worldItemDropObject = AcquirePooledWorldItemDrop( worldItemDropPoolKey, _worldItemDropPrefab );

            if ( worldItemDropObject == null )
            {
                return false;
            }

            InitializeSpawnedWorldItemDrop( worldItemDropObject, worldItemDropPoolKey, _itemDefinition, _itemCount, _dropPosition );
            RegisterActivePooledWorldItemDrop( worldItemDropObject );
            return true;
        }

        ///<summary>
        /// 월드 드랍 오브젝트 풀 반환
        ///</summary>
        public bool ReleasePooledWorldItemDrop( CWorldItemDropObject _worldItemDropObject, string _worldItemDropPoolKey )
        {
            if ( _worldItemDropObject == null || string.IsNullOrWhiteSpace( _worldItemDropPoolKey ) )
            {
                return false;
            }

            string managedPoolKey = BuildManagedWorldItemDropPoolKey( _worldItemDropPoolKey );

            if ( worldItemDropPoolKeySet.Contains( _worldItemDropPoolKey ) == false )
            {
                return false;
            }

            activePooledWorldItemDropObjects.Remove( _worldItemDropObject );
            _worldItemDropObject.SetMapRuntimePoolKey( string.Empty );
            bool result = CObjectPoolManager.TryRelease( managedPoolKey, _worldItemDropObject );
            return result;
        }

        ///<summary>
        /// 풀 몬스터 반환 공통 처리
        ///</summary>
        private bool ReleasePooledMonsterInternal( MonsterObject _monsterObject, string _monsterPoolKey, bool _shouldScheduleRespawn )
        {
            if ( _monsterObject == null || string.IsNullOrWhiteSpace( _monsterPoolKey ) )
            {
                return false;
            }

            if ( monsterPoolKeySet.Contains( _monsterPoolKey ) == false )
            {
                return false;
            }

            CMapMonsterRespawnContext respawnContext = null;

            if ( _shouldScheduleRespawn )
            {
                respawnContext = BuildMonsterRespawnContext( _monsterObject, _monsterPoolKey );
            }

            activePooledMonsterObjects.Remove( _monsterObject );
            _monsterObject.ClearMapRuntimePoolKey();
            string managedPoolKey = BuildManagedMonsterPoolKey( _monsterPoolKey );
            bool wasReleased = CObjectPoolManager.TryRelease( managedPoolKey, _monsterObject );

            if ( wasReleased == false )
            {
                return false;
            }

            if ( respawnContext != null )
            {
                StartCoroutine( IE_RespawnPooledMonster( respawnContext ) );
            }

            return true;
        }

        ///<summary>
        /// 활성 월드 드랍 오브젝트 일괄 반환
        ///</summary>
        private void ReturnAllActivePooledWorldItemDrops()
        {
            List<CWorldItemDropObject> activeWorldItemDropList = new List<CWorldItemDropObject>( activePooledWorldItemDropObjects );

            for ( int index = 0; index < activeWorldItemDropList.Count; index++ )
            {
                CWorldItemDropObject worldItemDropObject = activeWorldItemDropList[ index ];

                if ( worldItemDropObject == null )
                {
                    continue;
                }

                ReleasePooledWorldItemDrop( worldItemDropObject, worldItemDropObject.GetMapRuntimePoolKey() );
            }

            activePooledWorldItemDropObjects.Clear();
        }

        ///<summary>
        /// 월드 드랍 풀 키 결정
        ///</summary>
        private string ResolveWorldItemDropPoolKey( GameObject _worldItemDropPrefab )
        {
            if ( _worldItemDropPrefab == null )
            {
                return string.Empty;
            }

            int prefabInstanceId = _worldItemDropPrefab.GetInstanceID();
            string result = prefabInstanceId.ToString();
            return result;
        }

        ///<summary>
        /// 월드 드랍 풀 획득 또는 생성
        ///</summary>
        private bool GetOrCreateWorldItemDropPool( string _worldItemDropPoolKey, GameObject _worldItemDropPrefab )
        {
            if ( string.IsNullOrWhiteSpace( _worldItemDropPoolKey ) || _worldItemDropPrefab == null )
            {
                return false;
            }

            if ( worldItemDropPoolKeySet.Contains( _worldItemDropPoolKey ) )
            {
                return true;
            }

            string managedPoolKey = BuildManagedWorldItemDropPoolKey( _worldItemDropPoolKey );
            bool isRegistered = CObjectPoolManager.TryEnsurePoolRegistered<CWorldItemDropObject>(
                managedPoolKey,
                () => CreatePooledWorldItemDropInstance( _worldItemDropPoolKey, _worldItemDropPrefab ),
                OnGetPooledWorldItemDropInstance,
                OnReleasePooledWorldItemDropInstance,
                OnDestroyPooledWorldItemDropInstance );

            if ( isRegistered == false )
            {
                return false;
            }

            worldItemDropPoolKeySet.Add( _worldItemDropPoolKey );
            return true;
        }

        ///<summary>
        /// 월드 드랍 오브젝트 대여
        ///</summary>
        private CWorldItemDropObject AcquirePooledWorldItemDrop( string _worldItemDropPoolKey, GameObject _worldItemDropPrefab )
        {
            bool isReady = GetOrCreateWorldItemDropPool( _worldItemDropPoolKey, _worldItemDropPrefab );

            if ( isReady == false )
            {
                return null;
            }

            string managedPoolKey = BuildManagedWorldItemDropPoolKey( _worldItemDropPoolKey );

            if ( CObjectPoolManager.TryGet( managedPoolKey, out CWorldItemDropObject worldItemDropObject ) == false || worldItemDropObject == null )
            {
                return null;
            }

            worldItemDropObject.SetMapRuntimePoolKey( _worldItemDropPoolKey );
            worldItemDropObject.gameObject.name = _worldItemDropPrefab.name;
            return worldItemDropObject;
        }

        ///<summary>
        /// 활성 월드 드랍 오브젝트 등록
        ///</summary>
        private void RegisterActivePooledWorldItemDrop( CWorldItemDropObject _worldItemDropObject )
        {
            if ( _worldItemDropObject == null )
            {
                return;
            }

            bool isAlreadyRegistered = activePooledWorldItemDropObjects.Contains( _worldItemDropObject );

            if ( isAlreadyRegistered )
            {
                return;
            }

            activePooledWorldItemDropObjects.Add( _worldItemDropObject );
        }

        ///<summary>
        /// 월드 드랍 오브젝트 초기화
        ///</summary>
        private void InitializeSpawnedWorldItemDrop( CWorldItemDropObject _worldItemDropObject, string _worldItemDropPoolKey, CItemDefinition _itemDefinition, long _itemCount, Vector3 _dropPosition )
        {
            if ( _worldItemDropObject == null || _itemDefinition == null || _itemCount <= 0 )
            {
                return;
            }

            GameObject worldItemDropGameObject = _worldItemDropObject.gameObject;
            worldItemDropGameObject.transform.SetPositionAndRotation( _dropPosition, Quaternion.identity );
            worldItemDropGameObject.SetActive( true );
            _worldItemDropObject.SetMapRuntimePoolKey( _worldItemDropPoolKey );
            _worldItemDropObject.ConfigureDrop( _itemDefinition, _itemCount );
            _worldItemDropObject.SetPickupTriggerEnabled( true );
        }

        ///<summary>
        /// 월드 드랍 인스턴스 생성
        ///</summary>
        private CWorldItemDropObject CreatePooledWorldItemDropInstance( string _worldItemDropPoolKey, GameObject _worldItemDropPrefab )
        {
            if ( _worldItemDropPrefab == null )
            {
                return null;
            }

            GameObject createdWorldItemDropObject = Instantiate( _worldItemDropPrefab );
            createdWorldItemDropObject.name = _worldItemDropPrefab.name;
            createdWorldItemDropObject.SetActive( false );
            CWorldItemDropObject worldItemDropObject = createdWorldItemDropObject.GetComponent<CWorldItemDropObject>();

            if ( worldItemDropObject == null )
            {
                Destroy( createdWorldItemDropObject );
                return null;
            }

            worldItemDropObject.SetMapRuntimePoolKey( _worldItemDropPoolKey );
            worldItemDropObject.PrepareForRelease();
            return worldItemDropObject;
        }

        ///<summary>
        /// 월드 드랍 대여 후처리
        ///</summary>
        private void OnGetPooledWorldItemDropInstance( CWorldItemDropObject _worldItemDropObject )
        {
            if ( _worldItemDropObject == null )
            {
                return;
            }

            _worldItemDropObject.gameObject.SetActive( false );
        }

        ///<summary>
        /// 월드 드랍 반환 후처리
        ///</summary>
        private void OnReleasePooledWorldItemDropInstance( CWorldItemDropObject _worldItemDropObject )
        {
            if ( _worldItemDropObject == null )
            {
                return;
            }

            _worldItemDropObject.PrepareForRelease();
            _worldItemDropObject.gameObject.SetActive( false );
        }

        ///<summary>
        /// 월드 드랍 파기 처리
        ///</summary>
        private void OnDestroyPooledWorldItemDropInstance( CWorldItemDropObject _worldItemDropObject )
        {
            if ( _worldItemDropObject == null )
            {
                return;
            }

            Destroy( _worldItemDropObject.gameObject );
        }

        ///<summary>
        /// 몬스터 리스폰 문맥 구성
        ///</summary>
        private CMapMonsterRespawnContext BuildMonsterRespawnContext( MonsterObject _monsterObject, string _monsterPoolKey )
        {
            if ( _monsterObject == null || string.IsNullOrWhiteSpace( _monsterPoolKey ) )
            {
                return null;
            }

            float respawnDelaySeconds = _monsterObject.GetRespawnDelaySeconds();

            if ( respawnDelaySeconds <= 0.0f )
            {
                return null;
            }

            CMapMonsterRespawnContext respawnContext = new CMapMonsterRespawnContext();
            respawnContext.mapRuntimeVersion = currentMapRuntimeVersion;
            respawnContext.monsterId = _monsterObject.GetMonsterId();
            respawnContext.monsterName = _monsterObject.GetMonsterName();
            respawnContext.monsterPoolKey = _monsterPoolKey;
            respawnContext.spawnPosition = _monsterObject.GetMapRuntimeSpawnPosition();
            respawnContext.spawnRotation = _monsterObject.GetMapRuntimeSpawnRotation();
            respawnContext.spawnScale = _monsterObject.GetMapRuntimeSpawnScale();
            respawnContext.respawnDelaySeconds = respawnDelaySeconds;
            return respawnContext;
        }

        ///<summary>
        /// 풀 몬스터 리스폰 대기 처리
        ///</summary>
        private IEnumerator IE_RespawnPooledMonster( CMapMonsterRespawnContext _respawnContext )
        {
            if ( _respawnContext == null )
            {
                yield break;
            }

            float respawnDelaySeconds = _respawnContext.respawnDelaySeconds;

            if ( respawnDelaySeconds > 0.0f )
            {
                yield return new WaitForSeconds( respawnDelaySeconds );
            }

            if ( _respawnContext.mapRuntimeVersion != currentMapRuntimeVersion )
            {
                yield break;
            }

            MonsterObject monsterObject = AcquirePooledMonster( _respawnContext.monsterPoolKey, null );

            if ( monsterObject == null )
            {
                yield break;
            }

            InitializeSpawnedMonster(
                monsterObject,
                _respawnContext.monsterPoolKey,
                _respawnContext.monsterId,
                _respawnContext.monsterName,
                _respawnContext.spawnPosition,
                _respawnContext.spawnRotation,
                _respawnContext.spawnScale );
            RegisterActivePooledMonster( monsterObject );
        }

        ///<summary>
        /// 맵 런타임 몬스터 초기화
        ///</summary>
        private void InitializeSpawnedMonster( MonsterObject _monsterObject, string _monsterPoolKey, string _monsterId, string _monsterName, Vector3 _spawnPosition, Vector3 _spawnRotation, Vector3 _spawnScale )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            GameObject monsterGameObject = _monsterObject.gameObject;
            monsterGameObject.name = string.IsNullOrWhiteSpace( _monsterId ) ? _monsterPoolKey : _monsterId;
            monsterGameObject.transform.SetPositionAndRotation( _spawnPosition, Quaternion.Euler( _spawnRotation ) );
            monsterGameObject.transform.localScale = _spawnScale;
            _monsterObject.SetMapRuntimeSpawnTransform( _spawnPosition, _spawnRotation, _spawnScale );
            monsterGameObject.SetActive( true );
            _monsterObject.ResetRuntimeStateForRespawn();
            _monsterObject.ConfigureMonster( _monsterId, _monsterName );
            _monsterObject.SetMapRuntimePoolKey( _monsterPoolKey );

            if ( CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
            {
                monsterInfoManager.RegisterMonster( _monsterObject );
            }
        }

        ///<summary>
        /// 풀 몬스터 인스턴스 생성
        ///</summary>
        private MonsterObject CreatePooledMonsterInstance( string _monsterPoolKey, GameObject _monsterPrefab )
        {
            if ( _monsterPrefab == null )
            {
                return null;
            }

            GameObject createdMonsterObject = Instantiate( _monsterPrefab );
            createdMonsterObject.name = _monsterPrefab.name;
            createdMonsterObject.SetActive( false );
            MonsterObject monsterComponent = createdMonsterObject.GetComponent<MonsterObject>();

            if ( monsterComponent == null )
            {
                Destroy( createdMonsterObject );
                return null;
            }

            monsterComponent.SetMapRuntimePoolKey( _monsterPoolKey );
            return monsterComponent;
        }

        ///<summary>
        /// 풀 몬스터 대여 후처리
        ///</summary>
        private void OnGetPooledMonsterInstance( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            GameObject monsterGameObject = _monsterObject.gameObject;
            monsterGameObject.SetActive( false );
        }

        ///<summary>
        /// 풀 몬스터 반환 후처리
        ///</summary>
        private void OnReleasePooledMonsterInstance( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            GameObject monsterGameObject = _monsterObject.gameObject;
            monsterGameObject.SetActive( false );
        }

        ///<summary>
        /// 풀 몬스터 파기 처리
        ///</summary>
        private void OnDestroyPooledMonsterInstance( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            Destroy( _monsterObject.gameObject );
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
        /// 플레이어 진입 포탈 이동
        ///</summary>
        private void MovePlayerToEntryPortal(string _entryPortalId)
        {
            PlayerController playerController = ResolveActivePlayerController();

            if ( playerController == null )
            {
                return;
            }

            Vector3 spawnPosition = Vector3.zero;

            if ( string.IsNullOrWhiteSpace( _entryPortalId ) == false )
            {
                Vector3 resolvedPortalPosition = ResolvePortalPositionById( _entryPortalId.Trim() );
                spawnPosition = resolvedPortalPosition;
            }

            Transform playerTransform = playerController.transform;
            playerTransform.position = spawnPosition;
            Rigidbody2D playerRigidbody = playerController.GetComponent<Rigidbody2D>();

            if ( playerRigidbody != null )
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.angularVelocity = 0.0f;
            }

            InitializePlayerStatusUi( playerController );
            InitializeItemInventoryUi( playerController );
            InitializeSkillUi( playerController );
            InitializeQuestUi( playerController );
        }

        ///<summary>
        /// 플레이어 상태 UI 초기화 처리
        ///</summary>
        private void InitializePlayerStatusUi( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            CPlayerStatManager playerStatManager = _playerController.GetComponent<CPlayerStatManager>();
            CPlayerStatusUI playerStatusUi = FindFirstObjectByType<CPlayerStatusUI>();

            if ( playerStatusUi == null || playerStatManager == null )
            {
                return;
            }

            playerStatusUi.Bind( playerStatManager );
            playerStatusUi.InitializeStatusUi();
        }

        ///<summary>
        /// 플레이어 인벤토리 UI 초기화 처리
        ///</summary>
        private void InitializeItemInventoryUi( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            CPlayerInventoryManager inventoryManager = _playerController.GetInventoryManager();

            if ( inventoryManager == null )
            {
                return;
            }

            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;

            if ( itemInventoryUiManager == null )
            {
                return;
            }

            itemInventoryUiManager.BindInventoryManager( inventoryManager );
        }

        ///<summary>
        /// 플레이어 스킬 UI 초기화 처리
        ///</summary>
        private void InitializeSkillUi( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            CSkillManager skillManager = _playerController.GetComponent<CSkillManager>();

            if ( skillManager == null )
            {
                return;
            }

            CSkillUiManager skillUiManager = CSkillUiManager.Instance;

            if ( skillUiManager == null )
            {
                return;
            }

            skillUiManager.BindSkillManager( skillManager );
        }

        ///<summary>
        /// 플레이어 퀘스트 UI 초기화 처리
        ///</summary>
        private void InitializeQuestUi( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            CQuestUiManager questUiManager = CQuestUiManager.Instance;

            if ( questUiManager == null )
            {
                return;
            }

            questUiManager.BindPlayerController( _playerController );
        }

        ///<summary>
        /// 포탈 ID 위치 결정
        ///</summary>
        private Vector3 ResolvePortalPositionById(string _portalId)
        {
            int spawnedObjectCount = spawnedRuntimeObjects.Count;

            for ( int index = 0; index < spawnedObjectCount; index++ )
            {
                MapRuntimeSpawnMarker spawnMarker = spawnedRuntimeObjects[ index ];

                if ( spawnMarker == null )
                {
                    continue;
                }

                PortalObject portalObject = spawnMarker.GetComponent<PortalObject>();

                if ( portalObject == null )
                {
                    continue;
                }

                string currentPortalId = portalObject.GetPortalId();

                if ( string.Equals( currentPortalId, _portalId, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                Vector3 result = portalObject.transform.position;
                return result;
            }

            PortalObject[] portalObjects = FindObjectsByType<PortalObject>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int portalCount = portalObjects.Length;

            for ( int index = 0; index < portalCount; index++ )
            {
                PortalObject portalObject = portalObjects[ index ];

                if ( portalObject == null )
                {
                    continue;
                }

                string currentPortalId = portalObject.GetPortalId();

                if ( string.Equals( currentPortalId, _portalId, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                Vector3 result = portalObject.transform.position;
                return result;
            }

            return Vector3.zero;
        }

        ///<summary>
        /// 몬스터 프리팹 결정
        ///</summary>
        private GameObject ResolveMonsterPrefab(string _prefabName, string _resourcePath)
        {
            if ( string.IsNullOrWhiteSpace( _prefabName ) == false && monsterPrefabByName.TryGetValue( _prefabName, out GameObject cachedPrefab ) )
            {
                return cachedPrefab;
            }

            if ( string.IsNullOrWhiteSpace( _resourcePath ) == false )
            {
                GameObject loadedPrefab = Resources.Load<GameObject>( _resourcePath );
                return loadedPrefab;
            }

            return null;
        }

        ///<summary>
        /// 생성된 런타임 오브젝트 등록
        ///</summary>
        private void RegisterSpawnedRuntimeObject(GameObject _targetObject)
        {
            MapRuntimeSpawnMarker marker = _targetObject.GetComponent<MapRuntimeSpawnMarker>();

            if ( marker == null )
            {
                marker = _targetObject.AddComponent<MapRuntimeSpawnMarker>();
            }

            spawnedRuntimeObjects.Add( marker );
        }

        ///<summary>
        /// 생성된 런타임 오브젝트 목록 정리
        ///</summary>
        private void ClearSpawnedRuntimeObjects()
        {
            int objectCount = spawnedRuntimeObjects.Count;

            for ( int index = 0; index < objectCount; index++ )
            {
                MapRuntimeSpawnMarker marker = spawnedRuntimeObjects[ index ];

                if ( marker == null )
                {
                    continue;
                }

                Destroy( marker.gameObject );
            }

            spawnedRuntimeObjects.Clear();
        }

        ///<summary>
        /// 기본 트랜스폼 데이터 구성
        ///</summary>
        private CMapToolTransformData BuildDefaultTransformData(Transform _sourceTransform)
        {
            CMapToolTransformData transformData = new CMapToolTransformData();
            Vector3 localScale = _sourceTransform.localScale;
            transformData.position[ 0 ] = 0.0f;
            transformData.position[ 1 ] = 0.0f;
            transformData.position[ 2 ] = 0.0f;
            transformData.rotation[ 0 ] = 0.0f;
            transformData.rotation[ 1 ] = 0.0f;
            transformData.rotation[ 2 ] = 0.0f;
            transformData.scale[ 0 ] = localScale.x;
            transformData.scale[ 1 ] = localScale.y;
            transformData.scale[ 2 ] = localScale.z;
            return transformData;
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
        /// 페이드 알파 설정
        ///</summary>
        private void SetFadeAlpha(float _alpha)
        {
            if ( fadeImage == null )
            {
                return;
            }

            Color fadeColor = fadeImage.color;
            fadeColor.a = _alpha;
            fadeImage.color = fadeColor;
            UpdateFadeRaycastState( _alpha );
        }

        ///<summary>
        /// 페이드 입력 차단 상태 갱신
        ///</summary>
        private void UpdateFadeRaycastState( float _alpha )
        {
            bool shouldBlockRaycast = _alpha > 0.001f;

            if ( fadeImage != null )
            {
                fadeImage.raycastTarget = shouldBlockRaycast;
            }

            if ( fadeGraphicRaycaster != null )
            {
                fadeGraphicRaycaster.enabled = shouldBlockRaycast;
            }
        }

        ///<summary>
        /// 맵 전환 풀링 오브젝트 정리
        ///</summary>
        private void ReleaseMapTransitionPooledObjects()
        {
            ReturnAllActiveMapTitleLogoUis();
            ReleaseAllPlacedSkillAreas();
            ReleaseAllActiveProjectileSkillRuntimes();
            ReleaseAllTransientSkillVfx();
            ReleaseAllActiveDamageFonts();
            ReleaseAllPlayerPooledEffects();
        }

        ///<summary>
        /// 활성 설치형 스킬 영역 정리
        ///</summary>
        private void ReleaseAllPlacedSkillAreas()
        {
            CPlacedSkillAreaRuntime.ReleaseAllActivePlacedSkillAreas();
        }

        ///<summary>
        /// 활성 투사체 스킬 런타임 정리
        ///</summary>
        private void ReleaseAllActiveProjectileSkillRuntimes()
        {
            CProjectileSkillRuntime.ReleaseAllActiveProjectileSkillRuntimes();
        }

        ///<summary>
        /// 비지속성 스킬 시각 효과 정리
        ///</summary>
        private void ReleaseAllTransientSkillVfx()
        {
            CSkillVfxPoolManager.ReleaseAllTransientActiveVfx();
        }

        ///<summary>
        /// 활성 데미지 폰트 정리
        ///</summary>
        private void ReleaseAllActiveDamageFonts()
        {
            bool hasDamageFontManager = CDamageFontManager.TryGetInstance( out CDamageFontManager damageFontManager );

            if ( hasDamageFontManager == false || damageFontManager == null )
            {
                return;
            }

            damageFontManager.ReleaseAllActiveDamageFonts();
        }

        ///<summary>
        /// 현재 맵 제목 UI 표시
        ///</summary>
        private void ShowCurrentMapTitleLogoUi()
        {
            if ( string.IsNullOrWhiteSpace( currentMapName ) )
            {
                return;
            }

            EnsureMapTitleLogoUiPoolInitialized();

            if ( CObjectPoolManager.TryGet( MapTitleLogoUiPoolKey, out MapTitleLogoUI mapTitleLogoUi ) == false || mapTitleLogoUi == null )
            {
                return;
            }

            if ( activeMapTitleLogoUiList.Contains( mapTitleLogoUi ) == false )
            {
                activeMapTitleLogoUiList.Add( mapTitleLogoUi );
            }
        }

        ///<summary>
        /// 맵 제목 UI 풀 초기화 보장
        ///</summary>
        private void EnsureMapTitleLogoUiPoolInitialized()
        {
            mapTitleLogoUiPrefab = Resources.Load<GameObject>( MapTitleLogoUiPrefabResourcePath );
            RectTransform parentCanvasRectTransform = ResolveMapTitleLogoUiParentRectTransform();

            if ( mapTitleLogoUiPrefab == null || parentCanvasRectTransform == null )
            {
                return;
            }

            if ( mapTitleLogoUiPoolRectTransform == null )
            {
                Transform existingPoolTransform = parentCanvasRectTransform.Find( MapTitleLogoUiPoolObjectName );

                if ( existingPoolTransform != null )
                {
                    mapTitleLogoUiPoolRectTransform = existingPoolTransform as RectTransform;
                }
            }

            if ( mapTitleLogoUiPoolRectTransform == null )
            {
                GameObject poolObject = new GameObject( MapTitleLogoUiPoolObjectName, typeof( RectTransform ) );
                RectTransform poolRectTransform = poolObject.GetComponent<RectTransform>();
                poolRectTransform.SetParent( parentCanvasRectTransform, false );
                poolRectTransform.anchorMin = Vector2.zero;
                poolRectTransform.anchorMax = Vector2.one;
                poolRectTransform.offsetMin = Vector2.zero;
                poolRectTransform.offsetMax = Vector2.zero;
                mapTitleLogoUiPoolRectTransform = poolRectTransform;
            }

            CObjectPoolManager.TryEnsurePoolRegistered<MapTitleLogoUI>(
                MapTitleLogoUiPoolKey,
                CreateMapTitleLogoUi,
                OnGetMapTitleLogoUi,
                OnReleaseMapTitleLogoUi );
        }

        ///<summary>
        /// 맵 제목 UI 부모 RectTransform 결정
        ///</summary>
        private RectTransform ResolveMapTitleLogoUiParentRectTransform()
        {
            GameObject tempUiCanvasObject = GameObject.Find( TempUiCanvasObjectName );

            if ( tempUiCanvasObject != null )
            {
                RectTransform tempUiCanvasRectTransform = tempUiCanvasObject.transform as RectTransform;

                if ( tempUiCanvasRectTransform != null )
                {
                    return tempUiCanvasRectTransform;
                }
            }

            EnsureFadeOverlayExists();

            if ( fadeCanvas == null )
            {
                return null;
            }

            RectTransform fallbackRectTransform = fadeCanvas.transform as RectTransform;
            return fallbackRectTransform;
        }

        ///<summary>
        /// 맵 제목 UI 인스턴스 생성
        ///</summary>
        private MapTitleLogoUI CreateMapTitleLogoUi()
        {
            if ( mapTitleLogoUiPrefab == null || mapTitleLogoUiPoolRectTransform == null )
            {
                return null;
            }

            GameObject createdUiObject = Instantiate( mapTitleLogoUiPrefab, mapTitleLogoUiPoolRectTransform );
            createdUiObject.name = mapTitleLogoUiPrefab.name;
            MapTitleLogoUI mapTitleLogoUi = createdUiObject.GetComponent<MapTitleLogoUI>();

            if ( mapTitleLogoUi == null )
            {
                mapTitleLogoUi = createdUiObject.AddComponent<MapTitleLogoUI>();
            }

            mapTitleLogoUi.SetReturnToPoolHandler( HandleAutoReturnObjectToMapTitleLogoUiPool );
            createdUiObject.SetActive( false );
            return mapTitleLogoUi;
        }

        ///<summary>
        /// 맵 제목 UI 대여 후처리
        ///</summary>
        private void OnGetMapTitleLogoUi( MapTitleLogoUI _mapTitleLogoUi )
        {
            if ( _mapTitleLogoUi == null )
            {
                return;
            }

            _mapTitleLogoUi.transform.SetParent( mapTitleLogoUiPoolRectTransform, false );
            _mapTitleLogoUi.gameObject.SetActive( true );
        }

        ///<summary>
        /// 맵 제목 UI 반환 후처리
        ///</summary>
        private void OnReleaseMapTitleLogoUi( MapTitleLogoUI _mapTitleLogoUi )
        {
            if ( _mapTitleLogoUi == null )
            {
                return;
            }

            activeMapTitleLogoUiList.Remove( _mapTitleLogoUi );

            if ( mapTitleLogoUiPoolRectTransform != null )
            {
                _mapTitleLogoUi.transform.SetParent( mapTitleLogoUiPoolRectTransform, false );
            }

            _mapTitleLogoUi.gameObject.SetActive( false );
        }

        ///<summary>
        /// 맵 제목 UI 자동 반환 처리
        ///</summary>
        private void HandleAutoReturnObjectToMapTitleLogoUiPool( CAutoPoolReturnObject _autoPoolReturnObject )
        {
            if ( _autoPoolReturnObject is MapTitleLogoUI mapTitleLogoUi == false )
            {
                return;
            }

            CObjectPoolManager.TryRelease( MapTitleLogoUiPoolKey, mapTitleLogoUi );
        }

        ///<summary>
        /// 활성 맵 제목 UI 일괄 반환
        ///</summary>
        private void ReturnAllActiveMapTitleLogoUis()
        {
            List<MapTitleLogoUI> activeUiList = new List<MapTitleLogoUI>( activeMapTitleLogoUiList );

            for ( int index = 0; index < activeUiList.Count; index++ )
            {
                MapTitleLogoUI mapTitleLogoUi = activeUiList[ index ];

                if ( mapTitleLogoUi == null )
                {
                    continue;
                }

                CObjectPoolManager.TryRelease( MapTitleLogoUiPoolKey, mapTitleLogoUi );
            }

            activeMapTitleLogoUiList.Clear();
        }

        ///<summary>
        /// 플레이어 활성 풀링 이펙트 정리
        ///</summary>
        private void ReleaseAllPlayerPooledEffects()
        {
            PlayerController playerController = ResolveActivePlayerController();

            if ( playerController == null )
            {
                return;
            }

            playerController.ReleaseAllPooledEffects();
        }

        ///<summary>
        /// 활성 플레이어 제어 컴포넌트 결정
        ///</summary>
        private PlayerController ResolveActivePlayerController()
        {
            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int playerControllerCount = playerControllerArray.Length;

            for ( int index = 0; index < playerControllerCount; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null )
                {
                    continue;
                }

                if ( playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                return playerController;
            }

            return null;
        }

        ///<summary>
        /// NPC 프리팹 결정
        ///</summary>
        private GameObject ResolveNpcPrefab( string _prefabName, string _resourcePath )
        {
            if ( string.IsNullOrWhiteSpace( _prefabName ) == false && npcPrefabByName.TryGetValue( _prefabName, out GameObject cachedPrefab ) )
            {
                return cachedPrefab;
            }

            if ( string.IsNullOrWhiteSpace( _resourcePath ) == false )
            {
                GameObject loadedPrefab = Resources.Load<GameObject>( _resourcePath );
                return loadedPrefab;
            }

            return null;
        }

        ///<summary>
        /// 모든 몬스터 풀 정리
        ///</summary>
        private void ClearAllMonsterPools()
        {
            ReturnAllActiveMapTitleLogoUis();

            foreach ( string poolKey in monsterPoolKeySet )
            {
                string managedPoolKey = BuildManagedMonsterPoolKey( poolKey );
                CObjectPoolManager.TryClearPool( managedPoolKey );
            }

            monsterPoolKeySet.Clear();
            activePooledMonsterObjects.Clear();
        }

        ///<summary>
        /// 모든 월드 드랍 풀 정리
        ///</summary>
        private void ClearAllWorldItemDropPools()
        {
            ReturnAllActivePooledWorldItemDrops();

            foreach ( string poolKey in worldItemDropPoolKeySet )
            {
                string managedPoolKey = BuildManagedWorldItemDropPoolKey( poolKey );
                CObjectPoolManager.TryClearPool( managedPoolKey );
            }

            worldItemDropPoolKeySet.Clear();
            activePooledWorldItemDropObjects.Clear();
        }

        ///<summary>
        /// 몬스터 풀 관리 키 구성
        ///</summary>
        private string BuildManagedMonsterPoolKey( string _monsterPoolKey )
        {
            string result = MonsterPoolKeyPrefix + "." + _monsterPoolKey;
            return result;
        }

        ///<summary>
        /// 월드 드랍 풀 관리 키 구성
        ///</summary>
        private string BuildManagedWorldItemDropPoolKey( string _worldItemDropPoolKey )
        {
            string result = WorldItemDropPoolKeyPrefix + "." + _worldItemDropPoolKey;
            return result;
        }

        ///<summary>
        /// 인스턴스 참조 정리
        ///</summary>
        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            ReturnAllActivePooledWorldItemDrops();
            ReturnAllActivePooledMonsters();
            ClearAllWorldItemDropPools();
            ClearAllMonsterPools();
            base.OnDestroy();
        }
    }
}


