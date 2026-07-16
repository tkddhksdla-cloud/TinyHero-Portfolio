using System.Collections;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string ProtectedSaveMagic = "TinyHeroSaveProtectedV1";
        private const string SaveCryptoSecret = "TinyHero.Save.Security.Local.2026";

        private CGameSaveData pendingLoadSaveData;
        private bool isPendingLoadRequested;
        private bool isPendingLoadApplying;

        [Serializable]
        private sealed class CProtectedSavePayloadData
        {
            public string magic = ProtectedSaveMagic;
            public string salt = string.Empty;
            public string iv = string.Empty;
            public string cipherText = string.Empty;
            public string hmac = string.Empty;
        }

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
        /// 대기 중인 저장 로드의 플레이어 월드 위치 조회 시도
        ///</summary>
        public bool TryGetPendingLoadPlayerWorldPosition( out Vector3 _playerWorldPosition )
        {
            _playerWorldPosition = Vector3.zero;

            if ( isPendingLoadRequested == false || pendingLoadSaveData == null )
            {
                return false;
            }

            _playerWorldPosition = pendingLoadSaveData.playerWorldPosition;
            return true;
        }

        ///<summary>
        /// 대기 중인 저장 데이터를 지정 플레이어에 즉시 적용
        ///</summary>
        public bool TryApplyPendingLoadToPlayer( PlayerController _playerController )
        {
            if ( _playerController == null || isPendingLoadRequested == false || pendingLoadSaveData == null )
            {
                return false;
            }

            isPendingLoadApplying = true;
            CGameSaveData saveDataToApply = pendingLoadSaveData;
            ApplyPendingSaveDataToPlayer( _playerController, saveDataToApply );
            _playerController.transform.position = saveDataToApply.playerWorldPosition;
            Rigidbody2D playerRigidbody = _playerController.GetComponent<Rigidbody2D>();

            if ( playerRigidbody != null )
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.angularVelocity = 0.0f;
            }

            ClearPendingLoadRequest();
            return true;
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
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController != null )
            {
                navigationController.ShowCommonNotice( descriptionText, ConfirmButtonText, null, string.Empty, null );
            }
            else
            {
                Debug.LogWarning( "[ SaveDebug ] UI navigation controller was null during save popup request.", this );
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

            TryApplyPendingLoadToPlayer( resolvedPlayerController );
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
            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;

            if ( playerStatManager == null || playerInventoryManager == null || playerEquipmentManager == null || questManager == null || skillManager == null || playerProfileManager == null )
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
            createdSaveData.playerProfileSnapshotData = playerProfileManager.CreateSnapshotData();
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
            bool isProtected = TryProtectSaveJsonText( serializedJsonText, out string protectedSaveText );

            if ( isProtected == false )
            {
                Debug.LogWarning( "[ SaveDebug ] Save write failed because save protection failed.", this );
                return false;
            }

            File.WriteAllText( saveFilePath, protectedSaveText, Encoding.UTF8 );
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

            string serializedSaveText = File.ReadAllText( saveFilePath, Encoding.UTF8 );

            if ( string.IsNullOrWhiteSpace( serializedSaveText ) )
            {
                Debug.LogWarning( "[ SaveDebug ] Save file existed but contents were empty.", this );
                return false;
            }

            bool isUnprotected = TryResolveReadableSaveJsonText( serializedSaveText, out string readableJsonText );

            if ( isUnprotected == false )
            {
                Debug.LogWarning( "[ SaveDebug ] Save file integrity validation failed.", this );
                return false;
            }

            string migratedJsonText = MigrateLegacySaveJsonText( readableJsonText );
            CGameSaveData loadedSaveData = JsonUtility.FromJson<CGameSaveData>( migratedJsonText );

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
        /// 저장 JSON 보호 문자열 생성
        ///</summary>
        private bool TryProtectSaveJsonText( string _serializedJsonText, out string _protectedSaveText )
        {
            _protectedSaveText = string.Empty;

            if ( string.IsNullOrWhiteSpace( _serializedJsonText ) )
            {
                return false;
            }

            try
            {
                byte[] saltByteArray = CreateRandomByteArray( 16 );
                byte[] ivByteArray = CreateRandomByteArray( 16 );
                byte[] encryptionKeyByteArray = DeriveSaveKey( "AES", saltByteArray );
                byte[] hmacKeyByteArray = DeriveSaveKey( "HMAC", saltByteArray );
                byte[] plainByteArray = Encoding.UTF8.GetBytes( _serializedJsonText );
                byte[] cipherByteArray = EncryptSaveByteArray( plainByteArray, encryptionKeyByteArray, ivByteArray );

                CProtectedSavePayloadData payloadData = new CProtectedSavePayloadData();
                payloadData.magic = ProtectedSaveMagic;
                payloadData.salt = Convert.ToBase64String( saltByteArray );
                payloadData.iv = Convert.ToBase64String( ivByteArray );
                payloadData.cipherText = Convert.ToBase64String( cipherByteArray );
                payloadData.hmac = CalculatePayloadHmac( payloadData, hmacKeyByteArray );
                _protectedSaveText = JsonUtility.ToJson( payloadData, true );
                return true;
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[ SaveDebug ] Save protection exception. Message: {exception.Message}", this );
                return false;
            }
        }

        ///<summary>
        /// 읽기 가능한 저장 JSON 문자열 결정
        ///</summary>
        private bool TryResolveReadableSaveJsonText( string _serializedSaveText, out string _readableJsonText )
        {
            _readableJsonText = string.Empty;

            if ( string.IsNullOrWhiteSpace( _serializedSaveText ) )
            {
                return false;
            }

            if ( IsProtectedSaveText( _serializedSaveText ) == false )
            {
                _readableJsonText = _serializedSaveText;
                return true;
            }

            return TryUnprotectSaveJsonText( _serializedSaveText, out _readableJsonText );
        }

        ///<summary>
        /// 보호 저장 문자열 여부 반환
        ///</summary>
        private bool IsProtectedSaveText( string _serializedSaveText )
        {
            bool result = _serializedSaveText.Contains( ProtectedSaveMagic );
            return result;
        }

        ///<summary>
        /// 보호 저장 문자열 복호화
        ///</summary>
        private bool TryUnprotectSaveJsonText( string _protectedSaveText, out string _serializedJsonText )
        {
            _serializedJsonText = string.Empty;

            try
            {
                CProtectedSavePayloadData payloadData = JsonUtility.FromJson<CProtectedSavePayloadData>( _protectedSaveText );

                if ( payloadData == null || string.Equals( payloadData.magic, ProtectedSaveMagic, StringComparison.Ordinal ) == false )
                {
                    return false;
                }

                byte[] saltByteArray = Convert.FromBase64String( payloadData.salt );
                byte[] ivByteArray = Convert.FromBase64String( payloadData.iv );
                byte[] cipherByteArray = Convert.FromBase64String( payloadData.cipherText );
                byte[] encryptionKeyByteArray = DeriveSaveKey( "AES", saltByteArray );
                byte[] hmacKeyByteArray = DeriveSaveKey( "HMAC", saltByteArray );
                string expectedHmac = CalculatePayloadHmac( payloadData, hmacKeyByteArray );

                if ( IsSameText( expectedHmac, payloadData.hmac ) == false )
                {
                    return false;
                }

                byte[] plainByteArray = DecryptSaveByteArray( cipherByteArray, encryptionKeyByteArray, ivByteArray );
                _serializedJsonText = Encoding.UTF8.GetString( plainByteArray );
                return string.IsNullOrWhiteSpace( _serializedJsonText ) == false;
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[ SaveDebug ] Save unprotect exception. Message: {exception.Message}", this );
                return false;
            }
        }

        ///<summary>
        /// 저장 보호 난수 바이트 배열 생성
        ///</summary>
        private byte[] CreateRandomByteArray( int _byteCount )
        {
            byte[] byteArray = new byte[ Mathf.Max( 1, _byteCount ) ];

            using ( RNGCryptoServiceProvider randomProvider = new RNGCryptoServiceProvider() )
            {
                randomProvider.GetBytes( byteArray );
            }

            return byteArray;
        }

        ///<summary>
        /// 저장 보호 키 생성
        ///</summary>
        private byte[] DeriveSaveKey( string _purpose, byte[] _saltByteArray )
        {
            string applicationIdentifier = string.IsNullOrWhiteSpace( Application.identifier ) ? "TinyHero" : Application.identifier;
            string seedText = $"{SaveCryptoSecret}|{applicationIdentifier}|{_purpose}|{Convert.ToBase64String( _saltByteArray )}";
            byte[] seedByteArray = Encoding.UTF8.GetBytes( seedText );

            using ( SHA256 sha256 = SHA256.Create() )
            {
                byte[] result = sha256.ComputeHash( seedByteArray );
                return result;
            }
        }

        ///<summary>
        /// 저장 바이트 배열 암호화
        ///</summary>
        private byte[] EncryptSaveByteArray( byte[] _plainByteArray, byte[] _keyByteArray, byte[] _ivByteArray )
        {
            using ( Aes aes = Aes.Create() )
            {
                aes.Key = _keyByteArray;
                aes.IV = _ivByteArray;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ( ICryptoTransform encryptor = aes.CreateEncryptor() )
                {
                    byte[] result = encryptor.TransformFinalBlock( _plainByteArray, 0, _plainByteArray.Length );
                    return result;
                }
            }
        }

        ///<summary>
        /// 저장 바이트 배열 복호화
        ///</summary>
        private byte[] DecryptSaveByteArray( byte[] _cipherByteArray, byte[] _keyByteArray, byte[] _ivByteArray )
        {
            using ( Aes aes = Aes.Create() )
            {
                aes.Key = _keyByteArray;
                aes.IV = _ivByteArray;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ( ICryptoTransform decryptor = aes.CreateDecryptor() )
                {
                    byte[] result = decryptor.TransformFinalBlock( _cipherByteArray, 0, _cipherByteArray.Length );
                    return result;
                }
            }
        }

        ///<summary>
        /// 보호 저장 페이로드 HMAC 생성
        ///</summary>
        private string CalculatePayloadHmac( CProtectedSavePayloadData _payloadData, byte[] _hmacKeyByteArray )
        {
            string signedText = $"{_payloadData.magic}|{_payloadData.salt}|{_payloadData.iv}|{_payloadData.cipherText}";
            byte[] signedByteArray = Encoding.UTF8.GetBytes( signedText );

            using ( HMACSHA256 hmac = new HMACSHA256( _hmacKeyByteArray ) )
            {
                byte[] hmacByteArray = hmac.ComputeHash( signedByteArray );
                string result = Convert.ToBase64String( hmacByteArray );
                return result;
            }
        }

        ///<summary>
        /// 문자열 고정 시간 비교 결과 반환
        ///</summary>
        private bool IsSameText( string _leftText, string _rightText )
        {
            if ( _leftText == null || _rightText == null || _leftText.Length != _rightText.Length )
            {
                return false;
            }

            int difference = 0;

            for ( int index = 0; index < _leftText.Length; index++ )
            {
                difference |= _leftText[ index ] ^ _rightText[ index ];
            }

            bool result = difference == 0;
            return result;
        }

        ///<summary>
        /// 구버전 저장 JSON 필드명 변환
        ///</summary>
        private string MigrateLegacySaveJsonText( string _serializedJsonText )
        {
            if ( string.IsNullOrWhiteSpace( _serializedJsonText ) )
            {
                return string.Empty;
            }

            string quantityPattern = "\"quantity\"\\s*:";
            string quantityReplacement = "\"quantityValue\":";
            string result = Regex.Replace( _serializedJsonText, quantityPattern, quantityReplacement );
            return result;
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
            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;

            if ( playerStatManager == null || playerInventoryManager == null || playerEquipmentManager == null || questManager == null || skillManager == null || playerProfileManager == null )
            {
                return;
            }

            CQuestStateProvider questStateProvider = questManager.GetQuestStateProvider();

            if ( questStateProvider == null )
            {
                return;
            }

            playerProfileManager.LoadSnapshotData( _gameSaveData.playerProfileSnapshotData );
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
            bool hasPlayerController = CActivePlayerResolver.TryGetActivePlayerController( out PlayerController playerController );
            PlayerController result = hasPlayerController ? playerController : null;
            return result;
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
