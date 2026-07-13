using System;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyHero.Core
{
    ///<summary>
    /// 게임 세션과 플레이어 런타임 조립을 관리하는 코어 매니저
    ///</summary>
    public sealed class CGameManager : CSingleTon<CGameManager>
    {
        private const string GameplaySceneName = "SceneMap";
        private const string MapToolSceneName = "SceneMapTool";
        private const string PlayerObjectName = "PlayerObject";
        private const string PlayerPrefabAddressableKey = "Prefabs/Character/Player/PlayerObject";
        private const string PlayerPrefabResourcePath = "Prefabs/Character/Player/PlayerObject";

        [Header( "참조" )]
        [SerializeField] private CPlayerRuntimeContext playerRuntimeContext;
        [SerializeField] private GameObject playerPrefab;

        private PlayerController activePlayerController;
        private bool isPlayerPrefabLoadRequested;

        public event Action<PlayerController> OnPlayerReady;
        public event Action<PlayerController> OnPlayerReleased;

        ///<summary>
        /// 코어 게임 매니저 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RequestPlayerPrefabIfNeeded();
        }

        ///<summary>
        /// 현재 활성 플레이어 조회 시도
        ///</summary>
        public bool TryGetActivePlayerController( out PlayerController _playerController )
        {
            _playerController = activePlayerController;
            bool result = _playerController != null
                && _playerController.gameObject.activeInHierarchy
                && _playerController.enabled;
            return result;
        }

        ///<summary>
        /// 플레이어 런타임 컨텍스트 조회 시도
        ///</summary>
        public bool TryGetPlayerRuntimeContext( out CPlayerRuntimeContext _playerRuntimeContext )
        {
            _playerRuntimeContext = playerRuntimeContext;
            bool result = _playerRuntimeContext != null;
            return result;
        }

        ///<summary>
        /// 현재 씬 플레이어 존재 보장
        ///</summary>
        public bool EnsurePlayerForActiveScene( Vector3 _spawnPosition, out PlayerController _playerController )
        {
            _playerController = null;
            Scene activeScene = SceneManager.GetActiveScene();

            if ( IsPlayerScene( activeScene.name ) == false )
            {
                return false;
            }

            if ( TryGetActivePlayerController( out _playerController ) )
            {
                return true;
            }

            PlayerController existingPlayerController = ResolveExistingPlayerController();

            if ( existingPlayerController != null )
            {
                bool didBindExistingPlayer = BindPlayerController( existingPlayerController );
                _playerController = didBindExistingPlayer ? existingPlayerController : null;
                return didBindExistingPlayer;
            }

            if ( playerPrefab == null )
            {
                RequestPlayerPrefabIfNeeded();
                return false;
            }

            GameObject createdPlayerObject = Instantiate( playerPrefab, _spawnPosition, Quaternion.identity );
            createdPlayerObject.name = PlayerObjectName;
            PlayerController createdPlayerController = createdPlayerObject.GetComponent<PlayerController>();

            if ( createdPlayerController == null )
            {
                Debug.LogError( "[ GameManager ] Player prefab does not contain PlayerController.", playerPrefab );
                Destroy( createdPlayerObject );
                return false;
            }

            bool didBindPlayer = BindPlayerController( createdPlayerController );

            if ( didBindPlayer == false )
            {
                Destroy( createdPlayerObject );
                return false;
            }

            _playerController = createdPlayerController;
            return true;
        }

        ///<summary>
        /// 플레이어 제어 컴포넌트와 런타임 컨텍스트 연결
        ///</summary>
        private bool BindPlayerController( PlayerController _playerController )
        {
            if ( _playerController == null || playerRuntimeContext == null )
            {
                Debug.LogError( "[ GameManager ] Player runtime context is missing.", this );
                return false;
            }

            bool didBindPlayer = playerRuntimeContext.BindPlayerController( _playerController );

            if ( didBindPlayer == false )
            {
                return false;
            }

            activePlayerController = _playerController;
            OnPlayerReady?.Invoke( activePlayerController );
            return true;
        }

        ///<summary>
        /// 기존 플레이어 제어 컴포넌트 결정
        ///</summary>
        private PlayerController ResolveExistingPlayerController()
        {
            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int playerControllerCount = playerControllerArray.Length;

            for ( int index = 0; index < playerControllerCount; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null || playerController.gameObject.activeInHierarchy == false || playerController.enabled == false )
                {
                    continue;
                }

                return playerController;
            }

            return null;
        }

        ///<summary>
        /// 플레이어 프리팹 비동기 선로딩 요청
        ///</summary>
        private void RequestPlayerPrefabIfNeeded()
        {
            if ( playerPrefab != null || isPlayerPrefabLoadRequested )
            {
                return;
            }

            isPlayerPrefabLoadRequested = true;
            CResourceManager resourceManager = CResourceManager.Instance;
            resourceManager.LoadAssetAsync<GameObject>( PlayerPrefabAddressableKey, PlayerPrefabResourcePath, HandlePlayerPrefabLoaded );
        }

        ///<summary>
        /// 플레이어 프리팹 로드 완료 처리
        ///</summary>
        private void HandlePlayerPrefabLoaded( GameObject _playerPrefab )
        {
            isPlayerPrefabLoadRequested = false;
            playerPrefab = _playerPrefab;

            if ( playerPrefab == null )
            {
                Debug.LogWarning( "[ GameManager ] Player prefab load failed.", this );
                return;
            }

            EnsurePlayerForActiveScene( Vector3.zero, out PlayerController createdPlayerController );
        }

        ///<summary>
        /// 씬 로드 후 플레이어 세션 연결 처리
        ///</summary>
        private void HandleSceneLoaded( Scene _scene, LoadSceneMode _loadSceneMode )
        {
            if ( IsPlayerScene( _scene.name ) == false )
            {
                ReleaseActivePlayerReference();
                return;
            }

            EnsurePlayerForActiveScene( Vector3.zero, out PlayerController playerController );
        }

        ///<summary>
        /// 플레이어가 존재하는 씬 여부 반환
        ///</summary>
        private bool IsPlayerScene( string _sceneName )
        {
            bool result = string.Equals( _sceneName, GameplaySceneName, StringComparison.Ordinal )
                || string.Equals( _sceneName, MapToolSceneName, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 활성 플레이어 참조 해제
        ///</summary>
        private void ReleaseActivePlayerReference()
        {
            PlayerController releasedPlayerController = activePlayerController;

            if ( releasedPlayerController == null )
            {
                return;
            }

            if ( playerRuntimeContext != null )
            {
                playerRuntimeContext.UnbindPlayerController( releasedPlayerController );
            }

            activePlayerController = null;
            OnPlayerReleased?.Invoke( releasedPlayerController );
        }

        ///<summary>
        /// 게임 매니저 종료 정리
        ///</summary>
        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }
    }
}
