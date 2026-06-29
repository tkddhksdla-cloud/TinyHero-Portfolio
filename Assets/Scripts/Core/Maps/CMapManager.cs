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
        private const string MapTitleLogoUiPoolObjectName = "MapTitleLogoUIPool";
        private const string TempUiCanvasObjectName = "Canvas_TempUI";

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
        private readonly Dictionary<string, CObjectPool<MonsterObject>> monsterPoolByKey = new Dictionary<string, CObjectPool<MonsterObject>>();
        private readonly Dictionary<string, CObjectPool<CWorldItemDropObject>> worldItemDropPoolByKey = new Dictionary<string, CObjectPool<CWorldItemDropObject>>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> npcPrefabByName = new Dictionary<string, GameObject>();
        private Canvas fadeCanvas;
        private Image fadeImage;
        private GraphicRaycaster fadeGraphicRaycaster;
        private RectTransform mapTitleLogoUiPoolRectTransform;
        private GameObject mapTitleLogoUiPrefab;
        private CObjectPool<MapTitleLogoUI> mapTitleLogoUiPool;
        private Sprite currentBackgroundSprite;
        private string currentMapId = string.Empty;
        private string currentMapName = string.Empty;
        private int currentMapRuntimeVersion;
        private bool isTransitionInProgress;

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
            CMapToolSaveData loadedData = LoadMapSaveData( trimmedMapId );

            if ( loadedData == null )
            {
                return false;
            }

            ApplyMapData( loadedData, trimmedEntryPortalId );
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
            LoadMapImmediately( _mapId, _entryPortalId );
            yield return null;
            yield return new WaitForSeconds( MapTransitionBlackHoldSeconds );
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
            LoadMapImmediately( _mapId, _entryPortalId );
            yield return null;
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
                return;
            }

            GameObject existingCanvasObject = GameObject.Find( FadeCanvasObjectName );

            if ( existingCanvasObject != null )
            {
                Canvas existingCanvas = existingCanvasObject.GetComponent<Canvas>();
                Image existingFadeImage = existingCanvasObject.GetComponentInChildren<Image>( true );
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
        }

        ///<summary>
        /// 맵 저장 데이터 로드
        ///</summary>
        private CMapToolSaveData LoadMapSaveData(string _mapId)
        {
            string resourcePath = MapDataResourceFolderPath + _mapId;
            TextAsset textAsset = Resources.Load<TextAsset>( resourcePath );

            if ( textAsset == null )
            {
                return null;
            }

            string jsonText = textAsset.text;

            if ( string.IsNullOrWhiteSpace( jsonText ) )
            {
                return null;
            }

            CMapToolSaveData loadedData = JsonUtility.FromJson<CMapToolSaveData>( jsonText );
            return loadedData;
        }

        ///<summary>
        /// 맵 데이터 적용
        ///</summary>
        private void ApplyMapData(CMapToolSaveData _loadedData, string _entryPortalId)
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
            ApplyBackgroundSprite( _loadedData.backgroundSpriteName );
            SpawnPortals( _loadedData.portals );
            SpawnMonsters( _loadedData.monsters );
            SpawnNpcs( _loadedData.npcs );
            MovePlayerToEntryPortal( _entryPortalId );
        }

        ///<summary>
        /// 배경 스프라이트 적용
        ///</summary>
        private void ApplyBackgroundSprite(string _backgroundSpriteName)
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

            if ( backgroundSpriteByName.TryGetValue( _backgroundSpriteName, out Sprite backgroundSprite ) == false )
            {
                return;
            }

            currentBackgroundSprite = backgroundSprite;
            targetBackgroundRenderer.sprite = backgroundSprite;
            targetBackgroundFitter.ApplyFit();
            CMapToolBackgroundColliderVisualizer colliderVisualizer = targetBackgroundRenderer.GetComponent<CMapToolBackgroundColliderVisualizer>();

            if ( colliderVisualizer != null )
            {
                colliderVisualizer.RefreshColliderVisual();
            }
        }

        ///<summary>
        /// 포탈 목록 생성
        ///</summary>
        private void SpawnPortals(List<CMapToolPortalSaveData> _portalSaveDataList)
        {
            GameObject portalPrefab = Resources.Load<GameObject>( PortalPrefabResourcePath );

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

            foreach ( KeyValuePair<string, CObjectPool<MonsterObject>> pairData in monsterPoolByKey )
            {
                string poolKey = pairData.Key;

                if ( _requiredMonsterPoolKeySet.Contains( poolKey ) )
                {
                    continue;
                }

                CObjectPool<MonsterObject> monsterPool = pairData.Value;

                if ( monsterPool != null )
                {
                    monsterPool.Clear();
                }

                removalKeyList.Add( poolKey );
            }

            for ( int index = 0; index < removalKeyList.Count; index++ )
            {
                string removalKey = removalKeyList[ index ];
                monsterPoolByKey.Remove( removalKey );
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
        private CObjectPool<MonsterObject> GetOrCreateMonsterPool( string _monsterPoolKey )
        {
            if ( string.IsNullOrWhiteSpace( _monsterPoolKey ) )
            {
                return null;
            }

            if ( monsterPoolByKey.TryGetValue( _monsterPoolKey, out CObjectPool<MonsterObject> existingPool ) )
            {
                return existingPool;
            }

            GameObject monsterPrefab = ResolveMonsterPrefab( _monsterPoolKey, _monsterPoolKey );

            if ( monsterPrefab == null )
            {
                monsterPrefab = ResolveMonsterPrefab( _monsterPoolKey, string.Empty );
            }

            if ( monsterPrefab == null )
            {
                return null;
            }

            CObjectPool<MonsterObject> createdPool = new CObjectPool<MonsterObject>(
                () => CreatePooledMonsterInstance( _monsterPoolKey, monsterPrefab ),
                OnGetPooledMonsterInstance,
                OnReleasePooledMonsterInstance,
                OnDestroyPooledMonsterInstance );
            monsterPoolByKey[ _monsterPoolKey ] = createdPool;
            return createdPool;
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

            CObjectPool<MonsterObject> monsterPool = GetOrCreateMonsterPool( _monsterPoolKey );

            if ( monsterPool == null )
            {
                return null;
            }

            MonsterObject monsterObject = monsterPool.Get();

            if ( monsterObject == null )
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
        public bool TrySpawnWorldItemDrop( GameObject _worldItemDropPrefab, CItemDefinition _itemDefinition, int _itemCount, Vector3 _dropPosition )
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

            if ( worldItemDropPoolByKey.TryGetValue( _worldItemDropPoolKey, out CObjectPool<CWorldItemDropObject> worldItemDropPool ) == false || worldItemDropPool == null )
            {
                return false;
            }

            activePooledWorldItemDropObjects.Remove( _worldItemDropObject );
            _worldItemDropObject.SetMapRuntimePoolKey( string.Empty );
            worldItemDropPool.Release( _worldItemDropObject );
            return true;
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

            if ( monsterPoolByKey.TryGetValue( _monsterPoolKey, out CObjectPool<MonsterObject> monsterPool ) == false || monsterPool == null )
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
            monsterPool.Release( _monsterObject );

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
        private CObjectPool<CWorldItemDropObject> GetOrCreateWorldItemDropPool( string _worldItemDropPoolKey, GameObject _worldItemDropPrefab )
        {
            if ( string.IsNullOrWhiteSpace( _worldItemDropPoolKey ) || _worldItemDropPrefab == null )
            {
                return null;
            }

            if ( worldItemDropPoolByKey.TryGetValue( _worldItemDropPoolKey, out CObjectPool<CWorldItemDropObject> existingPool ) )
            {
                return existingPool;
            }

            CObjectPool<CWorldItemDropObject> createdPool = new CObjectPool<CWorldItemDropObject>(
                () => CreatePooledWorldItemDropInstance( _worldItemDropPoolKey, _worldItemDropPrefab ),
                OnGetPooledWorldItemDropInstance,
                OnReleasePooledWorldItemDropInstance,
                OnDestroyPooledWorldItemDropInstance );
            worldItemDropPoolByKey[ _worldItemDropPoolKey ] = createdPool;
            return createdPool;
        }

        ///<summary>
        /// 월드 드랍 오브젝트 대여
        ///</summary>
        private CWorldItemDropObject AcquirePooledWorldItemDrop( string _worldItemDropPoolKey, GameObject _worldItemDropPrefab )
        {
            CObjectPool<CWorldItemDropObject> worldItemDropPool = GetOrCreateWorldItemDropPool( _worldItemDropPoolKey, _worldItemDropPrefab );

            if ( worldItemDropPool == null )
            {
                return null;
            }

            CWorldItemDropObject worldItemDropObject = worldItemDropPool.Get();

            if ( worldItemDropObject == null )
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
        private void InitializeSpawnedWorldItemDrop( CWorldItemDropObject _worldItemDropObject, string _worldItemDropPoolKey, CItemDefinition _itemDefinition, int _itemCount, Vector3 _dropPosition )
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

            if ( mapTitleLogoUiPool == null )
            {
                return;
            }

            MapTitleLogoUI mapTitleLogoUi = mapTitleLogoUiPool.Get();

            if ( mapTitleLogoUi == null )
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
            if ( mapTitleLogoUiPool != null )
            {
                return;
            }

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

            CObjectPool<MapTitleLogoUI> createdPool = new CObjectPool<MapTitleLogoUI>(
                CreateMapTitleLogoUi,
                OnGetMapTitleLogoUi,
                OnReleaseMapTitleLogoUi );
            mapTitleLogoUiPool = createdPool;
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
            if ( _autoPoolReturnObject is MapTitleLogoUI mapTitleLogoUi == false || mapTitleLogoUiPool == null )
            {
                return;
            }

            mapTitleLogoUiPool.Release( mapTitleLogoUi );
        }

        ///<summary>
        /// 활성 맵 제목 UI 일괄 반환
        ///</summary>
        private void ReturnAllActiveMapTitleLogoUis()
        {
            if ( mapTitleLogoUiPool == null )
            {
                return;
            }

            List<MapTitleLogoUI> activeUiList = new List<MapTitleLogoUI>( activeMapTitleLogoUiList );

            for ( int index = 0; index < activeUiList.Count; index++ )
            {
                MapTitleLogoUI mapTitleLogoUi = activeUiList[ index ];

                if ( mapTitleLogoUi == null )
                {
                    continue;
                }

                mapTitleLogoUiPool.Release( mapTitleLogoUi );
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

            foreach ( KeyValuePair<string, CObjectPool<MonsterObject>> pairData in monsterPoolByKey )
            {
                CObjectPool<MonsterObject> monsterPool = pairData.Value;

                if ( monsterPool == null )
                {
                    continue;
                }

                monsterPool.Clear();
            }

            monsterPoolByKey.Clear();
            activePooledMonsterObjects.Clear();
        }

        ///<summary>
        /// 모든 월드 드랍 풀 정리
        ///</summary>
        private void ClearAllWorldItemDropPools()
        {
            ReturnAllActivePooledWorldItemDrops();

            foreach ( KeyValuePair<string, CObjectPool<CWorldItemDropObject>> pairData in worldItemDropPoolByKey )
            {
                CObjectPool<CWorldItemDropObject> worldItemDropPool = pairData.Value;

                if ( worldItemDropPool == null )
                {
                    continue;
                }

                worldItemDropPool.Clear();
            }

            worldItemDropPoolByKey.Clear();
            activePooledWorldItemDropObjects.Clear();
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


