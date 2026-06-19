using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;
using UnityEngine.UI;


    ///<summary>
    /// NPC 이름표 UI 관리 컴포넌트
    ///</summary>
    public sealed class CNPCNameTagManager : CSingleTon<CNPCNameTagManager>
    {
        private const string SceneNpcNameAreaCanvasObjectName = "Canvas_NPCNameArea";
        private const string NpcNameTagCanvasObjectName = "Canvas_NPCNameTag";
        private const string NpcNameTagPoolObjectName = "NpcNameTagPool";
        private const string NpcNameTagRootCanvasObjectName = "Canvas_NPCNameTag_Root";
        private const string NpcNameTagPrefabResourcePath = "Prefabs/UI/NameTag/NpcNameTag";
        private const float NpcNameTagScreenOffsetY = 18.0f;
        private const int NpcNameTagSortingOrder = 55;

        private readonly Dictionary<CNPCObject, CNPCNameTagView> nameTagViewByNpc = new Dictionary<CNPCObject, CNPCNameTagView>();

        private RectTransform npcNameTagCanvasRectTransform;
        private RectTransform npcNameTagPoolRectTransform;
        private Canvas targetCanvas;
        private Camera targetCamera;
        private GameObject npcNameTagPrefab;
        private GameObject createdRootCanvasObject;
        private CObjectPool<CNPCNameTagView> npcNameTagViewPool;

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

            ResolveSceneReferences();
            EnsurePoolInitialized();
        }

        ///<summary>
        /// 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CNPCNameTagManager _instance )
        {
            CNPCNameTagManager resolvedInstance = Instance;
            _instance = resolvedInstance;
            bool hasInstance = _instance != null;
            return hasInstance;
        }

        ///<summary>
        /// 프레임 위치 갱신
        ///</summary>
        private void LateUpdate()
        {
            ResolveSceneReferences();
            UpdateNpcNameTagPositions();
        }

        ///<summary>
        /// NPC 이름표 등록
        ///</summary>
        public void RegisterNpc( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return;
            }

            ResolveSceneReferences();
            EnsurePoolInitialized();

            if ( npcNameTagPoolRectTransform == null || npcNameTagViewPool == null )
            {
                return;
            }

            bool hasExistingView = nameTagViewByNpc.TryGetValue( _npcObject, out CNPCNameTagView existingView );

            if ( hasExistingView == false || existingView == null )
            {
                CNPCNameTagView createdView = npcNameTagViewPool.Get();
                nameTagViewByNpc[ _npcObject ] = createdView;
                existingView = createdView;
            }

            UpdateNpcNameTagView( _npcObject, existingView );
        }

        ///<summary>
        /// NPC 이름표 해제
        ///</summary>
        public void UnregisterNpc( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return;
            }

            bool hasView = nameTagViewByNpc.TryGetValue( _npcObject, out CNPCNameTagView nameTagView );

            if ( hasView == false )
            {
                return;
            }

            nameTagViewByNpc.Remove( _npcObject );

            if ( npcNameTagViewPool == null )
            {
                return;
            }

            npcNameTagViewPool.Release( nameTagView );
        }

        ///<summary>
        /// 씬 참조 결정
        ///</summary>
        private void ResolveSceneReferences()
        {
            EnsureCanvasHierarchyExists();

            if ( targetCanvas == null || npcNameTagCanvasRectTransform == null )
            {
                GameObject canvasObject = FindCanvasObject();

                if ( canvasObject != null )
                {
                    Canvas resolvedCanvas = canvasObject.GetComponent<Canvas>();
                    RectTransform resolvedRectTransform = canvasObject.transform as RectTransform;
                    targetCanvas = resolvedCanvas;
                    npcNameTagCanvasRectTransform = resolvedRectTransform;
                }
            }

            if ( npcNameTagPoolRectTransform == null )
            {
                if ( npcNameTagCanvasRectTransform != null )
                {
                    EnsurePoolObjectExists( npcNameTagCanvasRectTransform );
                    Transform resolvedPoolTransform = npcNameTagCanvasRectTransform.Find( NpcNameTagPoolObjectName );
                    npcNameTagPoolRectTransform = resolvedPoolTransform as RectTransform;
                }
            }

            if ( npcNameTagPoolRectTransform == null )
            {
                GameObject poolObject = GameObject.Find( NpcNameTagPoolObjectName );

                if ( poolObject != null )
                {
                    RectTransform resolvedPoolRectTransform = poolObject.transform as RectTransform;
                    npcNameTagPoolRectTransform = resolvedPoolRectTransform;
                }
            }

            Camera mainCamera = Camera.main;
            targetCamera = mainCamera;

            if ( targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                targetCamera = null;
            }
            else if ( targetCanvas != null )
            {
                targetCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : mainCamera;
            }
        }

        ///<summary>
        /// 이름표 캔버스 계층 존재 보장
        ///</summary>
        private void EnsureCanvasHierarchyExists()
        {
            GameObject existingCanvasObject = FindCanvasObject();

            if ( existingCanvasObject != null )
            {
                EnsurePoolObjectExists( existingCanvasObject.transform );
                return;
            }

            if ( createdRootCanvasObject == null )
            {
                GameObject rootCanvasObject = new GameObject( NpcNameTagRootCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
                Canvas rootCanvas = rootCanvasObject.GetComponent<Canvas>();
                CanvasScaler rootCanvasScaler = rootCanvasObject.GetComponent<CanvasScaler>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rootCanvas.sortingOrder = NpcNameTagSortingOrder;
                rootCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                rootCanvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );
                createdRootCanvasObject = rootCanvasObject;
            }

            GameObject canvasObject = new GameObject( NpcNameTagCanvasObjectName, typeof( RectTransform ), typeof( Canvas ) );
            RectTransform canvasRectTransform = canvasObject.transform as RectTransform;
            Canvas childCanvas = canvasObject.GetComponent<Canvas>();
            canvasRectTransform.SetParent( createdRootCanvasObject.transform, false );
            canvasRectTransform.anchorMin = Vector2.zero;
            canvasRectTransform.anchorMax = Vector2.one;
            canvasRectTransform.offsetMin = Vector2.zero;
            canvasRectTransform.offsetMax = Vector2.zero;
            childCanvas.overrideSorting = false;
            EnsurePoolObjectExists( canvasRectTransform );
        }

        ///<summary>
        /// NPC 이름표 대상 캔버스 결정
        ///</summary>
        private GameObject FindCanvasObject()
        {
            GameObject canvasObject = GameObject.Find( SceneNpcNameAreaCanvasObjectName );

            if ( canvasObject != null )
            {
                return canvasObject;
            }

            GameObject fallbackCanvasObject = GameObject.Find( NpcNameTagCanvasObjectName );
            return fallbackCanvasObject;
        }

        ///<summary>
        /// 이름표 풀 오브젝트 생성 보장
        ///</summary>
        private void EnsurePoolObjectExists( Transform _parentTransform )
        {
            if ( _parentTransform == null )
            {
                return;
            }

            Transform existingPoolTransform = _parentTransform.Find( NpcNameTagPoolObjectName );

            if ( existingPoolTransform != null )
            {
                return;
            }

            GameObject poolObject = new GameObject( NpcNameTagPoolObjectName, typeof( RectTransform ) );
            RectTransform poolRectTransform = poolObject.transform as RectTransform;
            poolRectTransform.SetParent( _parentTransform, false );
            poolRectTransform.anchorMin = Vector2.zero;
            poolRectTransform.anchorMax = Vector2.one;
            poolRectTransform.offsetMin = Vector2.zero;
            poolRectTransform.offsetMax = Vector2.zero;
        }

        ///<summary>
        /// 이름표 풀 초기화 보장
        ///</summary>
        private void EnsurePoolInitialized()
        {
            if ( npcNameTagViewPool != null )
            {
                return;
            }

            npcNameTagPrefab = Resources.Load<GameObject>( NpcNameTagPrefabResourcePath );

            if ( npcNameTagPrefab == null )
            {
                return;
            }

            CObjectPool<CNPCNameTagView> createdPool = new CObjectPool<CNPCNameTagView>( CreateNameTagView, OnGetNameTagView, OnReleaseNameTagView );
            npcNameTagViewPool = createdPool;
        }

        ///<summary>
        /// 이름표 뷰 생성
        ///</summary>
        private CNPCNameTagView CreateNameTagView()
        {
            ResolveSceneReferences();

            if ( npcNameTagPoolRectTransform == null || npcNameTagPrefab == null )
            {
                return null;
            }

            GameObject createdObject = Instantiate( npcNameTagPrefab, npcNameTagPoolRectTransform );
            createdObject.name = npcNameTagPrefab.name;
            CNPCNameTagView nameTagView = createdObject.GetComponent<CNPCNameTagView>();

            if ( nameTagView == null )
            {
                nameTagView = createdObject.AddComponent<CNPCNameTagView>();
            }

            nameTagView.PrepareLayout();
            return nameTagView;
        }

        ///<summary>
        /// 이름표 뷰 대여 처리
        ///</summary>
        private void OnGetNameTagView( CNPCNameTagView _nameTagView )
        {
            if ( _nameTagView == null )
            {
                return;
            }

            _nameTagView.PrepareLayout();
            _nameTagView.gameObject.SetActive( true );
        }

        ///<summary>
        /// 이름표 뷰 반환 처리
        ///</summary>
        private void OnReleaseNameTagView( CNPCNameTagView _nameTagView )
        {
            if ( _nameTagView == null )
            {
                return;
            }

            ResolveSceneReferences();

            if ( npcNameTagPoolRectTransform != null )
            {
                _nameTagView.transform.SetParent( npcNameTagPoolRectTransform, false );
            }

            _nameTagView.ResetView();
            _nameTagView.gameObject.SetActive( false );
        }

        ///<summary>
        /// NPC 이름표 위치 갱신
        ///</summary>
        private void UpdateNpcNameTagPositions()
        {
            if ( npcNameTagPoolRectTransform == null )
            {
                return;
            }

            List<CNPCObject> npcObjectList = new List<CNPCObject>( nameTagViewByNpc.Keys );

            for ( int index = 0; index < npcObjectList.Count; index++ )
            {
                CNPCObject npcObject = npcObjectList[ index ];

                if ( npcObject == null || npcObject.gameObject.activeInHierarchy == false )
                {
                    UnregisterNpc( npcObject );
                    continue;
                }

                bool hasView = nameTagViewByNpc.TryGetValue( npcObject, out CNPCNameTagView nameTagView );

                if ( hasView == false || nameTagView == null )
                {
                    continue;
                }

                UpdateNpcNameTagView( npcObject, nameTagView );
            }
        }

        ///<summary>
        /// 개별 NPC 이름표 갱신
        ///</summary>
        private void UpdateNpcNameTagView( CNPCObject _npcObject, CNPCNameTagView _nameTagView )
        {
            if ( _npcObject == null || _nameTagView == null || npcNameTagPoolRectTransform == null )
            {
                return;
            }

            Vector3 worldPosition = _npcObject.GetNameTagWorldPosition();
            Vector3 screenPosition = ResolveScreenPosition( worldPosition );
            bool isBehindCamera = screenPosition.z < 0.0f;

            if ( isBehindCamera )
            {
                _nameTagView.gameObject.SetActive( false );
                return;
            }

            if ( _nameTagView.gameObject.activeSelf == false )
            {
                _nameTagView.gameObject.SetActive( true );
            }

            Vector2 screenPoint = new Vector2( screenPosition.x, screenPosition.y + NpcNameTagScreenOffsetY );
            RectTransformUtility.ScreenPointToLocalPointInRectangle( npcNameTagPoolRectTransform, screenPoint, targetCamera, out Vector2 localPoint );
            _nameTagView.SetAnchoredPosition( localPoint );
            _nameTagView.ApplyName( _npcObject.GetDisplayName() );
        }

        ///<summary>
        /// 월드 좌표 스크린 위치 결정
        ///</summary>
        private Vector3 ResolveScreenPosition( Vector3 _worldPosition )
        {
            Camera resolvedCamera = Camera.main;

            if ( resolvedCamera == null )
            {
                return _worldPosition;
            }

            Vector3 result = resolvedCamera.WorldToScreenPoint( _worldPosition );
            return result;
        }
    }
