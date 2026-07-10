using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 플레이어 이름표 UI 관리 컴포넌트
    ///</summary>
    public sealed class CPlayerNameTagManager : CSingleTon<CPlayerNameTagManager>
    {
        private const string ScenePlayerNameAreaCanvasObjectName = "Canvas_PlayerNameArea";
        private const string PlayerNameTagCanvasObjectName = "Canvas_PlayerNameTag";
        private const string PlayerNameTagRootCanvasObjectName = "Canvas_PlayerNameTag_Root";
        private const string PlayerNameTagPrefabResourcePath = "Prefabs/UI/NameTag/PlayerNameTag";
        private const float PlayerNameTagScreenOffsetY = 18.0f;
        private const int PlayerNameTagSortingOrder = -2;

        private PlayerController targetPlayerController;
        private CNPCNameTagView playerNameTagView;
        private RectTransform playerNameTagCanvasRectTransform;
        private Canvas targetCanvas;
        private Camera targetCamera;
        private GameObject playerNameTagPrefab;
        private GameObject createdRootCanvasObject;
        private bool isPlayerNameTagVisible = true;

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
            LoadNameTagPrefab();
            SubscribePlayerProfile();
        }

        ///<summary>
        /// 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CPlayerNameTagManager _instance )
        {
            CPlayerNameTagManager resolvedInstance = Instance;
            _instance = resolvedInstance;
            bool hasInstance = _instance != null;
            return hasInstance;
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            SubscribePlayerProfile();
        }

        ///<summary>
        /// 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            UnsubscribePlayerProfile();
        }

        ///<summary>
        /// 프레임 위치 갱신
        ///</summary>
        private void LateUpdate()
        {
            if ( targetCanvas == null || playerNameTagCanvasRectTransform == null )
            {
                ResolveSceneReferences();
            }

            ResolveTargetCamera();
            UpdatePlayerNameTagView();
        }

        ///<summary>
        /// 플레이어 이름표 등록
        ///</summary>
        public void RegisterPlayer( PlayerController _playerController )
        {
            if ( _playerController == null )
            {
                return;
            }

            targetPlayerController = _playerController;
            EnsureNameTagViewExists();
            RefreshPlayerName();
        }

        ///<summary>
        /// 플레이어 이름표 해제
        ///</summary>
        public void UnregisterPlayer( PlayerController _playerController )
        {
            if ( _playerController == null || targetPlayerController != _playerController )
            {
                return;
            }

            targetPlayerController = null;

            if ( playerNameTagView != null )
            {
                playerNameTagView.ResetView();
                playerNameTagView.gameObject.SetActive( false );
            }
        }

        public void SetPlayerNameTagVisible( bool _isVisible )
        {
            isPlayerNameTagVisible = _isVisible;

            if ( playerNameTagView != null )
            {
                playerNameTagView.gameObject.SetActive( _isVisible );
            }
        }

        ///<summary>
        /// 프로필 이름 변경 처리
        ///</summary>
        private void HandlePlayerNameChanged( string _playerName )
        {
            RefreshPlayerName();
        }

        ///<summary>
        /// 플레이어 이름 표시 갱신
        ///</summary>
        private void RefreshPlayerName()
        {
            if ( playerNameTagView == null )
            {
                return;
            }

            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;
            string playerName = playerProfileManager != null ? playerProfileManager.GetPlayerName() : string.Empty;
            playerNameTagView.ApplyName( playerName );
        }

        ///<summary>
        /// 플레이어 이름표 위치 갱신
        ///</summary>
        private void UpdatePlayerNameTagView()
        {
            if ( targetPlayerController == null || targetPlayerController.gameObject.activeInHierarchy == false )
            {
                if ( playerNameTagView != null )
                {
                    playerNameTagView.gameObject.SetActive( false );
                }

                return;
            }

            if ( isPlayerNameTagVisible == false )
            {
                if ( playerNameTagView != null )
                {
                    playerNameTagView.gameObject.SetActive( false );
                }

                return;
            }

            EnsureNameTagViewExists();

            if ( playerNameTagView == null || playerNameTagCanvasRectTransform == null )
            {
                return;
            }

            Vector3 worldPosition = ResolvePlayerNameTagWorldPosition();
            Vector3 screenPosition = ResolveScreenPosition( worldPosition );
            bool isBehindCamera = screenPosition.z < 0.0f;

            if ( isBehindCamera )
            {
                playerNameTagView.gameObject.SetActive( false );
                return;
            }

            if ( playerNameTagView.gameObject.activeSelf == false )
            {
                playerNameTagView.gameObject.SetActive( true );
            }

            Vector2 screenPoint = new Vector2( screenPosition.x, screenPosition.y + PlayerNameTagScreenOffsetY );
            RectTransformUtility.ScreenPointToLocalPointInRectangle( playerNameTagCanvasRectTransform, screenPoint, targetCamera, out Vector2 localPoint );
            playerNameTagView.SetAnchoredPosition( localPoint );
            RefreshPlayerName();
        }

        ///<summary>
        /// 플레이어 이름표 월드 위치 결정
        ///</summary>
        private Vector3 ResolvePlayerNameTagWorldPosition()
        {
            if ( targetPlayerController == null )
            {
                return Vector3.zero;
            }

            Vector3 result = targetPlayerController.GetNameTagWorldPosition();
            return result;
        }

        ///<summary>
        /// 이름표 뷰 생성 보장
        ///</summary>
        private void EnsureNameTagViewExists()
        {
            if ( playerNameTagView != null )
            {
                return;
            }

            ResolveSceneReferences();
            LoadNameTagPrefab();

            if ( playerNameTagPrefab == null || playerNameTagCanvasRectTransform == null )
            {
                return;
            }

            GameObject createdObject = Instantiate( playerNameTagPrefab, playerNameTagCanvasRectTransform );
            createdObject.name = playerNameTagPrefab.name;
            playerNameTagView = createdObject.GetComponent<CNPCNameTagView>();

            if ( playerNameTagView == null )
            {
                playerNameTagView = createdObject.AddComponent<CNPCNameTagView>();
            }

            playerNameTagView.PrepareLayout();
            playerNameTagView.gameObject.SetActive( true );
        }

        ///<summary>
        /// 씬 참조 결정
        ///</summary>
        private void ResolveSceneReferences()
        {
            EnsureCanvasHierarchyExists();

            Camera mainCamera = Camera.main;
            targetCamera = mainCamera;

            ResolveTargetCamera();
        }

        ///<summary>
        /// 대상 카메라 결정
        ///</summary>
        private void ResolveTargetCamera()
        {
            Camera mainCamera = Camera.main;
            targetCamera = mainCamera;

            if ( targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                targetCamera = null;
                return;
            }

            if ( targetCanvas == null )
            {
                return;
            }

            targetCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : mainCamera;
        }

        ///<summary>
        /// 이름표 캔버스 계층 존재 보장
        ///</summary>
        private void EnsureCanvasHierarchyExists()
        {
            if ( targetCanvas != null && playerNameTagCanvasRectTransform != null )
            {
                return;
            }

            GameObject existingCanvasObject = FindCanvasObject();

            if ( existingCanvasObject != null )
            {
                Canvas existingCanvas = existingCanvasObject.GetComponent<Canvas>();
                RectTransform existingRectTransform = existingCanvasObject.transform as RectTransform;

                if ( existingCanvas != null )
                {
                    existingCanvas.overrideSorting = true;
                    existingCanvas.sortingOrder = PlayerNameTagSortingOrder;
                }

                targetCanvas = existingCanvas;
                playerNameTagCanvasRectTransform = existingRectTransform;
                return;
            }

            if ( createdRootCanvasObject == null )
            {
                GameObject rootCanvasObject = new GameObject( PlayerNameTagRootCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
                Canvas rootCanvas = rootCanvasObject.GetComponent<Canvas>();
                CanvasScaler rootCanvasScaler = rootCanvasObject.GetComponent<CanvasScaler>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rootCanvas.sortingOrder = PlayerNameTagSortingOrder;
                rootCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                rootCanvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );
                createdRootCanvasObject = rootCanvasObject;
            }

            GameObject canvasObject = new GameObject( PlayerNameTagCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( GraphicRaycaster ) );
            RectTransform canvasRectTransform = canvasObject.transform as RectTransform;
            Canvas childCanvas = canvasObject.GetComponent<Canvas>();
            canvasRectTransform.SetParent( createdRootCanvasObject.transform, false );
            canvasRectTransform.anchorMin = Vector2.zero;
            canvasRectTransform.anchorMax = Vector2.one;
            canvasRectTransform.offsetMin = Vector2.zero;
            canvasRectTransform.offsetMax = Vector2.zero;
            childCanvas.overrideSorting = false;
            targetCanvas = childCanvas;
            playerNameTagCanvasRectTransform = canvasRectTransform;
        }

        ///<summary>
        /// 플레이어 이름표 대상 캔버스 결정
        ///</summary>
        private GameObject FindCanvasObject()
        {
            GameObject canvasObject = GameObject.Find( ScenePlayerNameAreaCanvasObjectName );

            if ( canvasObject != null )
            {
                return canvasObject;
            }

            GameObject fallbackCanvasObject = GameObject.Find( PlayerNameTagCanvasObjectName );
            return fallbackCanvasObject;
        }

        ///<summary>
        /// 이름표 프리팹 로드
        ///</summary>
        private void LoadNameTagPrefab()
        {
            if ( playerNameTagPrefab != null )
            {
                return;
            }

            playerNameTagPrefab = Resources.Load<GameObject>( PlayerNameTagPrefabResourcePath );
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

        ///<summary>
        /// 플레이어 프로필 이벤트 구독
        ///</summary>
        private void SubscribePlayerProfile()
        {
            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;

            if ( playerProfileManager == null )
            {
                return;
            }

            playerProfileManager.OnPlayerNameChanged -= HandlePlayerNameChanged;
            playerProfileManager.OnPlayerNameChanged += HandlePlayerNameChanged;
        }

        ///<summary>
        /// 플레이어 프로필 이벤트 해제
        ///</summary>
        private void UnsubscribePlayerProfile()
        {
            if ( CPlayerProfileManager.TryGetExistingInstance( out CPlayerProfileManager playerProfileManager ) == false || playerProfileManager == null )
            {
                return;
            }

            playerProfileManager.OnPlayerNameChanged -= HandlePlayerNameChanged;
        }
    }
}
