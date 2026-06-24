using System.Collections;
using System.IO;
using TinyHero.Maps;
using TinyHero.Player;
using TinyHero.Quest;
using TinyHero.Skill;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyHero.Core
{
    ///<summary>
    /// 게임 저장 관리 컴포넌트
    ///</summary>
    public sealed class CSaveManager : CSingleTon<CSaveManager>
    {
        private const string SaveFileName = "savegame.json";
        private const string GameplaySceneName = "SceneMap";
        private const string SaveCompletedDescriptionText = "저장이 완료되었습니다.";
        private const string SaveFailedDescriptionText = "저장에 실패했습니다.";
        private const string ConfirmButtonText = "확인";
        private const int MaxLoadWaitFrameCount = 300;

        private CGameSaveData pendingLoadSaveData;
        private bool isPendingLoadRequested;
        private bool isPendingLoadApplying;

        ///<summary>
        /// 저장 매니저 초기 설정
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
        }

        ///<summary>
        /// 저장 데이터 존재 여부 반환
        ///</summary>
        public bool HasSaveData()
        {
            string saveFilePath = ResolveSaveFilePath();
            bool result = File.Exists( saveFilePath );
            return result;
        }

        ///<summary>
        /// 저장 데이터 로드 대기 설정
        ///</summary>
        public bool TryPreparePendingLoad()
        {
            Debug.Log( "[ SaveDebug ] TryPreparePendingLoad requested.", this );
            bool hasSaveData = TryReadSaveDataFromDisk( out CGameSaveData loadedSaveData );

            if ( hasSaveData == false || loadedSaveData == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Pending load preparation failed because save data was not found.", this );
                isPendingLoadRequested = false;
                pendingLoadSaveData = null;
                return false;
            }

            pendingLoadSaveData = loadedSaveData;
            isPendingLoadRequested = true;
            string targetMapId = string.IsNullOrWhiteSpace( loadedSaveData.mapId ) ? string.Empty : loadedSaveData.mapId.Trim();

            if ( string.IsNullOrWhiteSpace( targetMapId ) )
            {
                Debug.LogWarning( "[ SaveDebug ] Pending load preparation failed because target map id was empty.", this );
                return false;
            }

            CMapManager.SetPendingMapLoad( targetMapId );
            Debug.Log( $"[ SaveDebug ] Pending load prepared. MapId: {targetMapId}", this );
            return true;
        }

        ///<summary>
        /// 저장 데이터 대기 해제 처리
        ///</summary>
        public void ClearPendingLoadRequest()
        {
            isPendingLoadRequested = false;
            isPendingLoadApplying = false;
            pendingLoadSaveData = null;
        }

        ///<summary>
        /// 현재 게임 저장 처리
        ///</summary>
        public bool TrySaveCurrentGame()
        {
            Debug.Log( "[ SaveDebug ] TrySaveCurrentGame requested.", this );
            bool hasSaveData = TryBuildCurrentGameSaveData( out CGameSaveData gameSaveData );

            if ( hasSaveData == false || gameSaveData == null )
            {
                Debug.LogWarning( "[ SaveDebug ] TrySaveCurrentGame failed while building save data.", this );
                return false;
            }

            bool isWritten = TryWriteSaveDataToDisk( gameSaveData );
            Debug.Log( $"[ SaveDebug ] TrySaveCurrentGame finished. Success: {isWritten}", this );
            return isWritten;
        }

        ///<summary>
        /// 저장 요청 및 안내 팝업 처리
        ///</summary>
        public bool RequestSaveWithPopup()
        {
            Debug.Log( "[ SaveDebug ] RequestSaveWithPopup requested.", this );
            bool isSaved = TrySaveCurrentGame();
            string descriptionText = isSaved ? SaveCompletedDescriptionText : SaveFailedDescriptionText;
            CPopupCommonNoticeManager popupManager = CPopupCommonNoticeManager.Instance;

            if ( popupManager != null )
            {
                popupManager.ShowNotice( descriptionText, ConfirmButtonText, null, string.Empty, null );
            }
            else
            {
                Debug.LogWarning( "[ SaveDebug ] Popup manager was null during save popup request.", this );
            }

            return isSaved;
        }

        ///<summary>
        /// 씬 로드 후 저장 후처리
        ///</summary>
        private void HandleSceneLoaded( Scene _scene, LoadSceneMode _loadSceneMode )
        {
            Debug.Log( $"[ SaveDebug ] Scene loaded. Name: {_scene.name}", this );
            if ( string.Equals( _scene.name, GameplaySceneName, System.StringComparison.Ordinal ) == false )
            {
                return;
            }

            if ( isPendingLoadRequested == false || pendingLoadSaveData == null || isPendingLoadApplying )
            {
                return;
            }

            StartCoroutine( IE_ApplyPendingLoadSaveData() );
        }

        ///<summary>
        /// 저장 데이터 적용 대기 코루틴
        ///</summary>
        private IEnumerator IE_ApplyPendingLoadSaveData()
        {
            isPendingLoadApplying = true;
            int waitFrameCount = 0;

            while ( waitFrameCount < MaxLoadWaitFrameCount )
            {
                CMapManager mapManager = CMapManager.Instance;
                PlayerController playerController = ResolveActivePlayerController();

                if ( mapManager != null && mapManager.IsTransitionInProgress() == false && playerController != null )
                {
                    break;
                }

                waitFrameCount++;
                yield return null;
            }

            PlayerController resolvedPlayerController = ResolveActivePlayerController();

            if ( resolvedPlayerController == null || pendingLoadSaveData == null )
            {
                ClearPendingLoadRequest();
                yield break;
            }

            ApplyPendingSaveDataToPlayer( resolvedPlayerController, pendingLoadSaveData );
            resolvedPlayerController.transform.position = pendingLoadSaveData.playerWorldPosition;
            Rigidbody2D playerRigidbody = resolvedPlayerController.GetComponent<Rigidbody2D>();

            if ( playerRigidbody != null )
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.angularVelocity = 0.0f;
            }

            ClearPendingLoadRequest();
        }

        ///<summary>
        /// 현재 저장 데이터 구성 처리
        ///</summary>
        private bool TryBuildCurrentGameSaveData( out CGameSaveData _gameSaveData )
        {
            _gameSaveData = null;
            PlayerController playerController = ResolveActivePlayerController();

            if ( playerController == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save build failed because player controller was null.", this );
                return false;
            }

            CMapManager mapManager = CMapManager.Instance;

            if ( mapManager == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save build failed because map manager was null.", this );
                return false;
            }

            string currentMapId = mapManager.GetCurrentMapId();

            if ( string.IsNullOrWhiteSpace( currentMapId ) )
            {
                Debug.LogWarning( "[ SaveDebug ] Save build failed because current map id was empty.", this );
                return false;
            }

            CPlayerStatManager playerStatManager = playerController.GetPlayerStatManager();
            CPlayerInventoryManager playerInventoryManager = playerController.GetInventoryManager();
            CPlayerEquipmentManager playerEquipmentManager = playerController.GetEquipmentManager();
            CQuestManager questManager = playerController.GetQuestManager();
            CSkillManager skillManager = playerController.GetSkillManager();

            if ( playerStatManager == null || playerInventoryManager == null || playerEquipmentManager == null || questManager == null || skillManager == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save build failed because one or more player managers were null.", this );
                return false;
            }

            CQuestStateProvider questStateProvider = questManager.GetQuestStateProvider();

            if ( questStateProvider == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save build failed because quest state provider was null.", this );
                return false;
            }

            CGameSaveData createdSaveData = new CGameSaveData();
            createdSaveData.mapId = currentMapId;
            createdSaveData.playerWorldPosition = playerController.transform.position;
            createdSaveData.playerStatSnapshotData = playerStatManager.CreateSnapshotData();
            createdSaveData.playerInventorySnapshotData = playerInventoryManager.CreateSnapshotData();
            createdSaveData.playerEquipmentSnapshotData = playerEquipmentManager.CreateSnapshotData();
            createdSaveData.questRuntimeSnapshotData = questStateProvider.CreateSnapshotData();
            createdSaveData.skillSnapshotData = skillManager.CreateSnapshotData();
            Debug.Log( $"[ SaveDebug ] Save build succeeded. MapId: {createdSaveData.mapId}", this );
            _gameSaveData = createdSaveData;
            return true;
        }

        ///<summary>
        /// 저장 데이터 디스크 기록 처리
        ///</summary>
        private bool TryWriteSaveDataToDisk( CGameSaveData _gameSaveData )
        {
            if ( _gameSaveData == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save write failed because save data was null.", this );
                return false;
            }

            string saveFilePath = ResolveSaveFilePath();
            string saveDirectoryPath = Path.GetDirectoryName( saveFilePath );

            if ( string.IsNullOrWhiteSpace( saveDirectoryPath ) == false && Directory.Exists( saveDirectoryPath ) == false )
            {
                Directory.CreateDirectory( saveDirectoryPath );
            }

            string serializedJsonText = JsonUtility.ToJson( _gameSaveData, true );
            File.WriteAllText( saveFilePath, serializedJsonText );
            Debug.Log( $"[ SaveDebug ] Save data written. Path: {saveFilePath}", this );
            return true;
        }

        ///<summary>
        /// 저장 데이터 디스크 읽기 처리
        ///</summary>
        private bool TryReadSaveDataFromDisk( out CGameSaveData _gameSaveData )
        {
            _gameSaveData = null;
            string saveFilePath = ResolveSaveFilePath();

            if ( File.Exists( saveFilePath ) == false )
            {
                Debug.LogWarning( $"[ SaveDebug ] Save file was not found. Path: {saveFilePath}", this );
                return false;
            }

            string serializedJsonText = File.ReadAllText( saveFilePath );

            if ( string.IsNullOrWhiteSpace( serializedJsonText ) )
            {
                Debug.LogWarning( "[ SaveDebug ] Save file existed but contents were empty.", this );
                return false;
            }

            CGameSaveData loadedSaveData = JsonUtility.FromJson<CGameSaveData>( serializedJsonText );

            if ( loadedSaveData == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save file existed but deserialization returned null.", this );
                return false;
            }

            Debug.Log( $"[ SaveDebug ] Save file read succeeded. Path: {saveFilePath}", this );
            _gameSaveData = loadedSaveData;
            return true;
        }

        ///<summary>
        /// 저장 파일 경로 구성
        ///</summary>
        private string ResolveSaveFilePath()
        {
            string persistentDataPath = Application.persistentDataPath;
            string result = Path.Combine( persistentDataPath, SaveFileName );
            return result;
        }

        ///<summary>
        /// 플레이어 대상 저장 데이터 반영
        ///</summary>
        private void ApplyPendingSaveDataToPlayer( PlayerController _playerController, CGameSaveData _gameSaveData )
        {
            if ( _playerController == null || _gameSaveData == null )
            {
                return;
            }

            CPlayerStatManager playerStatManager = _playerController.GetPlayerStatManager();
            CPlayerInventoryManager playerInventoryManager = _playerController.GetInventoryManager();
            CPlayerEquipmentManager playerEquipmentManager = _playerController.GetEquipmentManager();
            CQuestManager questManager = _playerController.GetQuestManager();
            CSkillManager skillManager = _playerController.GetSkillManager();

            if ( playerStatManager == null || playerInventoryManager == null || playerEquipmentManager == null || questManager == null || skillManager == null )
            {
                return;
            }

            CQuestStateProvider questStateProvider = questManager.GetQuestStateProvider();

            if ( questStateProvider == null )
            {
                return;
            }

            playerStatManager.LoadSnapshotData( _gameSaveData.playerStatSnapshotData );
            playerInventoryManager.LoadSnapshotData( _gameSaveData.playerInventorySnapshotData );
            playerEquipmentManager.LoadSnapshotData( _gameSaveData.playerEquipmentSnapshotData );
            questStateProvider.LoadSnapshotData( _gameSaveData.questRuntimeSnapshotData );
            skillManager.LoadSnapshotData( _gameSaveData.skillSnapshotData );
            questManager.RefreshQuestProgressState();
        }

        ///<summary>
        /// 플레이어 제어 컴포넌트 결정
        ///</summary>
        private PlayerController ResolveActivePlayerController()
        {
            PlayerController[] playerControllerArray = Object.FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
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
        /// 저장 매니저 종료 정리
        ///</summary>
        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }
    }
}
