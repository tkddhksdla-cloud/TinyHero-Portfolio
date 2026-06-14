using System.Collections;
using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Player;
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
        private const float DefaultFadeDuration = 0.35f;
        private const int FadeSortingOrder = 1000;
        private const string FadeCanvasObjectName = "MapFadeCanvas";
        private const string FadeImageObjectName = "FadeImage";
        private const string GameplaySceneName = "SceneMap";

        [SerializeField] private float fadeDuration = DefaultFadeDuration;

        private static string pendingMapId = string.Empty;
        private static string pendingEntryPortalId = string.Empty;
        private readonly List<MapRuntimeSpawnMarker> spawnedRuntimeObjects = new List<MapRuntimeSpawnMarker>();
        private readonly Dictionary<string, Sprite> backgroundSpriteByName = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private Canvas fadeCanvas;
        private Image fadeImage;
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
            yield return IE_FadeAlpha( 1.0f, 0.0f );
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

            if ( fadeDuration <= 0.0f )
            {
                fadeColor.a = _endAlpha;
                fadeImage.color = fadeColor;
                yield break;
            }

            while ( elapsedTime < fadeDuration )
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01( elapsedTime / fadeDuration );
                float alpha = Mathf.Lerp( _startAlpha, _endAlpha, normalizedTime );
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
                yield return null;
            }

            fadeColor.a = _endAlpha;
            fadeImage.color = fadeColor;
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
            isTransitionInProgress = false;
        }

        ///<summary>
        /// 씬 로드 후 대기 맵 적용 처리
        ///</summary>
        private void HandleSceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
        {
            TryLoadPendingMapForActiveScene();
        }

        ///<summary>
        /// 현재 씬 대기 맵 로드 처리
        ///</summary>
        private void TryLoadPendingMapForActiveScene()
        {
            if ( string.IsNullOrWhiteSpace( pendingMapId ) )
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if ( activeScene.name != GameplaySceneName )
            {
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
                fadeCanvas = existingCanvas;
                fadeImage = existingFadeImage;

                if ( fadeImage != null )
                {
                    Color existingColor = fadeImage.color;
                    existingColor.a = 0.0f;
                    fadeImage.color = existingColor;
                }

                return;
            }

            GameObject fadeCanvasObject = new GameObject( FadeCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
            Canvas createdCanvas = fadeCanvasObject.GetComponent<Canvas>();
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

            fadeCanvas = createdCanvas;
            fadeImage = createdFadeImage;
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

            ClearSpawnedRuntimeObjects();
            ApplyBackgroundSprite( _loadedData.backgroundSpriteName );
            SpawnPortals( _loadedData.portals );
            SpawnMonsters( _loadedData.monsters );
            MovePlayerToEntryPortal( _entryPortalId );
        }

        ///<summary>
        /// 배경 스프라이트 적용
        ///</summary>
        private void ApplyBackgroundSprite(string _backgroundSpriteName)
        {
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

                Vector3 spawnPosition = CreateVector3FromTransformData( transformData.position, Vector3.zero );
                Vector3 spawnRotation = CreateVector3FromTransformData( transformData.rotation, Vector3.zero );
                Vector3 spawnScale = ResolveMonsterSpawnScale( monsterPrefab );
                GameObject monsterObject = Instantiate( monsterPrefab, spawnPosition, Quaternion.Euler( spawnRotation ) );
                monsterObject.transform.localScale = spawnScale;
                monsterObject.name = monsterPrefab.name;
                MonsterObject monsterComponent = monsterObject.GetComponent<MonsterObject>();

                if ( monsterComponent != null )
                {
                    monsterComponent.ConfigureMonster( monsterPrefab.name, monsterPrefab.name );

                    if ( CMonsterInfoManager.TryGetInstance( out CMonsterInfoManager monsterInfoManager ) )
                    {
                        monsterInfoManager.RegisterMonster( monsterComponent );
                    }
                }

                RegisterSpawnedRuntimeObject( monsterObject );
            }
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
            PlayerController playerController = FindFirstObjectByType<PlayerController>();

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
        }

        ///<summary>
        /// 포탈 ID 위치 결정
        ///</summary>
        private Vector3 ResolvePortalPositionById(string _portalId)
        {
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
        }

        ///<summary>
        /// 인스턴스 참조 정리
        ///</summary>
        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }
    }
}


