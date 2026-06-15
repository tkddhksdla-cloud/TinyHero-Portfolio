using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 몬스터 정보 UI 관리 컴포넌트
    ///</summary>
    public sealed class CMonsterInfoManager : CSingleTon<CMonsterInfoManager>
    {
        private const string MonsterInfoCanvasObjectName = "Canvas_MonsterInfo";
        private const string MonsterInfoPoolObjectName = "MonsterInfoPool";
        private const string MonsterInfoRootCanvasObjectName = "Canvas_MonsterInfo_Root";
        private const string PrimaryMonsterInfoPrefabResourcePath = "Prefabs/UI/Monster/MonsterInfo";
        private const string FallbackMonsterInfoPrefabResourcePath = "Prefabs/UI/HpBar/MonsterInfo";
        private const float MonsterInfoScreenOffsetY = 20.0f;
        private const int MonsterInfoSortingOrder = 50;

        private readonly Dictionary<MonsterObject, CMonsterInfoView> monsterInfoViewByMonster = new Dictionary<MonsterObject, CMonsterInfoView>();

        private RectTransform monsterInfoCanvasRectTransform;
        private RectTransform monsterInfoPoolRectTransform;
        private Canvas targetCanvas;
        private Camera targetCamera;
        private GameObject monsterInfoPrefab;
        private GameObject createdRootCanvasObject;
        private CObjectPool<CMonsterInfoView> monsterInfoViewPool;

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
        public static bool TryGetInstance(out CMonsterInfoManager _instance)
        {
            CMonsterInfoManager resolvedInstance = Instance;
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
            UpdateMonsterInfoViewPositions();
        }

        ///<summary>
        /// 몬스터 UI 등록
        ///</summary>
        public void RegisterMonster(MonsterObject _monsterObject)
        {
            if ( _monsterObject == null )
            {
                return;
            }

            ResolveSceneReferences();
            EnsurePoolInitialized();

            if ( monsterInfoPoolRectTransform == null || monsterInfoViewPool == null )
            {
                return;
            }

            bool hasExistingView = monsterInfoViewByMonster.TryGetValue( _monsterObject, out CMonsterInfoView existingView );

            if ( hasExistingView == false || existingView == null )
            {
                CMonsterInfoView createdView = monsterInfoViewPool.Get();
                monsterInfoViewByMonster[ _monsterObject ] = createdView;
                existingView = createdView;
            }

            UpdateMonsterInfoView( _monsterObject, existingView );
        }

        ///<summary>
        /// 몬스터 UI 해제
        ///</summary>
        public void UnregisterMonster(MonsterObject _monsterObject)
        {
            if ( _monsterObject == null )
            {
                return;
            }

            bool hasView = monsterInfoViewByMonster.TryGetValue( _monsterObject, out CMonsterInfoView monsterInfoView );

            if ( hasView == false )
            {
                return;
            }

            monsterInfoViewByMonster.Remove( _monsterObject );

            if ( monsterInfoViewPool == null )
            {
                return;
            }

            monsterInfoViewPool.Release( monsterInfoView );
        }

        ///<summary>
        /// 몬스터 UI 내용 갱신
        ///</summary>
        public void RefreshMonsterInfo(MonsterObject _monsterObject)
        {
            if ( _monsterObject == null )
            {
                return;
            }

            bool hasView = monsterInfoViewByMonster.TryGetValue( _monsterObject, out CMonsterInfoView monsterInfoView );

            if ( hasView == false || monsterInfoView == null )
            {
                RegisterMonster( _monsterObject );
                return;
            }

            UpdateMonsterInfoView( _monsterObject, monsterInfoView );
        }

        ///<summary>
        /// 씬 참조 결정
        ///</summary>
        private void ResolveSceneReferences()
        {
            EnsureCanvasHierarchyExists();

            bool hasValidCanvas = targetCanvas != null && monsterInfoCanvasRectTransform != null;

            if ( hasValidCanvas == false )
            {
                GameObject monsterInfoCanvasObject = GameObject.Find( MonsterInfoCanvasObjectName );

                if ( monsterInfoCanvasObject != null )
                {
                    Canvas resolvedCanvas = monsterInfoCanvasObject.GetComponent<Canvas>();
                    RectTransform resolvedCanvasRectTransform = monsterInfoCanvasObject.transform as RectTransform;
                    targetCanvas = resolvedCanvas;
                    monsterInfoCanvasRectTransform = resolvedCanvasRectTransform;
                }
            }

            bool hasValidPool = monsterInfoPoolRectTransform != null;

            if ( hasValidPool == false )
            {
                GameObject monsterInfoPoolObject = GameObject.Find( MonsterInfoPoolObjectName );

                if ( monsterInfoPoolObject != null )
                {
                    RectTransform resolvedPoolRectTransform = monsterInfoPoolObject.transform as RectTransform;
                    monsterInfoPoolRectTransform = resolvedPoolRectTransform;
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
        /// 몬스터 정보 캔버스 계층 존재 보장
        ///</summary>
        private void EnsureCanvasHierarchyExists()
        {
            GameObject existingCanvasObject = GameObject.Find( MonsterInfoCanvasObjectName );

            if ( existingCanvasObject != null )
            {
                return;
            }

            if ( createdRootCanvasObject == null )
            {
                GameObject rootCanvasObject = new GameObject( MonsterInfoRootCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
                Canvas rootCanvas = rootCanvasObject.GetComponent<Canvas>();
                CanvasScaler rootCanvasScaler = rootCanvasObject.GetComponent<CanvasScaler>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rootCanvas.sortingOrder = MonsterInfoSortingOrder;
                rootCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                rootCanvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );
                createdRootCanvasObject = rootCanvasObject;
            }

            GameObject canvasObject = new GameObject( MonsterInfoCanvasObjectName, typeof( RectTransform ), typeof( Canvas ) );
            RectTransform canvasRectTransform = canvasObject.transform as RectTransform;
            Canvas childCanvas = canvasObject.GetComponent<Canvas>();
            canvasRectTransform.SetParent( createdRootCanvasObject.transform, false );
            canvasRectTransform.anchorMin = Vector2.zero;
            canvasRectTransform.anchorMax = Vector2.one;
            canvasRectTransform.offsetMin = Vector2.zero;
            canvasRectTransform.offsetMax = Vector2.zero;
            childCanvas.overrideSorting = false;

            GameObject poolObject = new GameObject( MonsterInfoPoolObjectName, typeof( RectTransform ) );
            RectTransform poolRectTransform = poolObject.transform as RectTransform;
            poolRectTransform.SetParent( canvasRectTransform, false );
            poolRectTransform.anchorMin = Vector2.zero;
            poolRectTransform.anchorMax = Vector2.one;
            poolRectTransform.offsetMin = Vector2.zero;
            poolRectTransform.offsetMax = Vector2.zero;
        }

        ///<summary>
        /// 몬스터 정보 풀 초기화
        ///</summary>
        private void EnsurePoolInitialized()
        {
            if ( monsterInfoViewPool != null )
            {
                return;
            }

            monsterInfoPrefab = ResolveMonsterInfoPrefab();

            if ( monsterInfoPrefab == null )
            {
                return;
            }

            CObjectPool<CMonsterInfoView> createdPool = new CObjectPool<CMonsterInfoView>( CreateMonsterInfoView, OnGetMonsterInfoView, OnReleaseMonsterInfoView );
            monsterInfoViewPool = createdPool;
        }

        ///<summary>
        /// 몬스터 정보 프리팹 결정
        ///</summary>
        private GameObject ResolveMonsterInfoPrefab()
        {
            if ( monsterInfoPrefab != null )
            {
                return monsterInfoPrefab;
            }

            GameObject primaryPrefab = Resources.Load<GameObject>( PrimaryMonsterInfoPrefabResourcePath );

            if ( primaryPrefab != null )
            {
                monsterInfoPrefab = primaryPrefab;
                return monsterInfoPrefab;
            }

            GameObject fallbackPrefab = Resources.Load<GameObject>( FallbackMonsterInfoPrefabResourcePath );
            monsterInfoPrefab = fallbackPrefab;
            return monsterInfoPrefab;
        }

        ///<summary>
        /// 몬스터 정보 뷰 생성
        ///</summary>
        private CMonsterInfoView CreateMonsterInfoView()
        {
            ResolveSceneReferences();

            if ( monsterInfoPoolRectTransform == null || monsterInfoPrefab == null )
            {
                return null;
            }

            GameObject createdObject = Instantiate( monsterInfoPrefab, monsterInfoPoolRectTransform );
            createdObject.name = monsterInfoPrefab.name;
            CMonsterInfoView monsterInfoView = createdObject.GetComponent<CMonsterInfoView>();

            if ( monsterInfoView == null )
            {
                monsterInfoView = createdObject.AddComponent<CMonsterInfoView>();
            }

            monsterInfoView.PrepareTrackingLayout();
            return monsterInfoView;
        }

        ///<summary>
        /// 몬스터 정보 뷰 대여 처리
        ///</summary>
        private void OnGetMonsterInfoView(CMonsterInfoView _monsterInfoView)
        {
            if ( _monsterInfoView == null )
            {
                return;
            }

            _monsterInfoView.PrepareTrackingLayout();
            _monsterInfoView.gameObject.SetActive( true );
        }

        ///<summary>
        /// 몬스터 정보 뷰 반환 처리
        ///</summary>
        private void OnReleaseMonsterInfoView(CMonsterInfoView _monsterInfoView)
        {
            if ( _monsterInfoView == null )
            {
                return;
            }

            ResolveSceneReferences();

            if ( monsterInfoPoolRectTransform != null )
            {
                _monsterInfoView.transform.SetParent( monsterInfoPoolRectTransform, false );
            }

            _monsterInfoView.PrepareTrackingLayout();
            _monsterInfoView.ResetView();
            _monsterInfoView.gameObject.SetActive( false );
        }

        ///<summary>
        /// 몬스터 정보 뷰 위치 갱신
        ///</summary>
        private void UpdateMonsterInfoViewPositions()
        {
            if ( monsterInfoPoolRectTransform == null )
            {
                return;
            }

            List<MonsterObject> monsterList = new List<MonsterObject>( monsterInfoViewByMonster.Keys );

            for ( int i = 0; i < monsterList.Count; i++ )
            {
                MonsterObject monsterObject = monsterList[ i ];

                if ( monsterObject == null || monsterObject.gameObject.activeInHierarchy == false )
                {
                    UnregisterMonster( monsterObject );
                    continue;
                }

                bool hasView = monsterInfoViewByMonster.TryGetValue( monsterObject, out CMonsterInfoView monsterInfoView );

                if ( hasView == false || monsterInfoView == null )
                {
                    continue;
                }

                UpdateMonsterInfoView( monsterObject, monsterInfoView );
            }
        }

        ///<summary>
        /// 개별 몬스터 정보 뷰 갱신
        ///</summary>
        private void UpdateMonsterInfoView(MonsterObject _monsterObject, CMonsterInfoView _monsterInfoView)
        {
            if ( _monsterObject == null || _monsterInfoView == null || monsterInfoPoolRectTransform == null )
            {
                return;
            }

            Vector3 worldPosition = _monsterObject.GetMonsterInfoWorldPosition();
            Vector3 screenPosition = ResolveScreenPosition( worldPosition );
            bool isBehindCamera = screenPosition.z < 0.0f;

            if ( isBehindCamera )
            {
                _monsterInfoView.gameObject.SetActive( false );
                return;
            }

            if ( _monsterInfoView.gameObject.activeSelf == false )
            {
                _monsterInfoView.gameObject.SetActive( true );
            }

            Vector2 localPoint;
            Vector2 screenPoint = new Vector2( screenPosition.x, screenPosition.y + MonsterInfoScreenOffsetY );
            RectTransformUtility.ScreenPointToLocalPointInRectangle( monsterInfoPoolRectTransform, screenPoint, targetCamera, out localPoint );
            _monsterInfoView.SetAnchoredPosition( localPoint );
            _monsterInfoView.ApplyMonsterInfo( _monsterObject.GetMonsterName(), _monsterObject.GetLevel(), _monsterObject.GetCurrentHp(), _monsterObject.GetMaxHp() );
        }

        ///<summary>
        /// 월드 좌표의 스크린 위치 결정
        ///</summary>
        private Vector3 ResolveScreenPosition(Vector3 _worldPosition)
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
}
