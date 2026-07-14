using System;
using System.Collections;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Quest;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyHero.Core
{
    ///<summary>
    /// 공용 리소스 로드 및 캐시 매니저
    ///</summary>
    public sealed class CResourceManager : CSingleTon<CResourceManager>
    {
        private const string InventoryPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupItemInventory";
        private const string LegacyInventoryPopupPrefabResourcePath = "Prefabs/UI/Inventory/PopupItemInventory";
        private const string SkillPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupSkillList";
        private const string ShopPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupShop";
        private const string NpcQuestPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupQuestList";
        private const string PlayerQuestPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupQuestList_Mine";
        private const string RewardPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupReward";
        private const string CubePopupPrefabResourcePath = "Prefabs/UI/Popup/PopupCube";
        private const string LegacyCubePopupPrefabResourcePath = "Prefabs/UI/Inventory/CubeUI";
        private const string CommonNoticePopupPrefabResourcePath = "Prefabs/UI/Popup/PopupCommonNotice";
        private const string CommonInputFieldPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupCommonInputField";
        private const string ContentDownloadPopupPrefabResourcePath = "Prefabs/UI/Popup/PopupContentDownload";
        private const string ItemTooltipPrefabResourcePath = "Prefabs/UI/Inventory/ItemTooltipUI";
        private const string SkillTooltipPrefabResourcePath = "Prefabs/UI/Skill/SkillTooltipUI";
        private const string MapLoadingUiPrefabResourcePath = "Prefabs/UI/Map/MapLoadingUI";
        private const string ItemDefinitionResourcePath = "Data/Item/Definitions";
        private const string ShopDefinitionResourcePath = "Data/Shop/Definitions";
        private const string QuestDefinitionResourcePath = "Data/Quest/Definitions";
        private const string ItemDataAddressableLabel = "TinyHero.Data.Item";
        private const string ShopDataAddressableLabel = "TinyHero.Data.Shop";
        private const string QuestDataAddressableLabel = "TinyHero.Data.Quest";
        private const string RuntimeAddressableLabel = "TinyHero.RuntimeResource";
        private const string PlayerDefaultStatTablePath = "Data/Player/PlayerDefaultStatTableData";
        private const string PlayerLevelStatTablePath = "Data/Player/PlayerLevelStatTableData";
        private const string MonsterStatTablePath = "Data/Monster/MonsterStatTableData";
        private const string TextTableDataPath = "Data/Text/TextTableData";
        private const string EquipmentPotentialTablePath = "Data/Item/EquipmentPotentialTableData";
        private const int RemoteDataLoadCount = 8;
        private const int RemoteDataLoadMaxAttempts = 3;
        private const float RemoteDataLoadRetryDelaySeconds = 0.25f;
        private const float RemoteCatalogOperationTimeoutSeconds = 8.0f;

        private readonly Dictionary<string, Object> cachedResourceDictionary = new Dictionary<string, Object>();
        private readonly Dictionary<string, Object[]> cachedResourceArrayDictionary = new Dictionary<string, Object[]>();
        private readonly Dictionary<string, AsyncOperationHandle> cachedAddressableHandleDictionary = new Dictionary<string, AsyncOperationHandle>();
        private readonly Dictionary<eResourceKey, CResourceLoadEntry> resourceLoadEntryDictionary = new Dictionary<eResourceKey, CResourceLoadEntry>();
        private bool isRemoteDataPreloadRequested;
        private bool isRemoteDataReady;
        private bool hasRemoteDataLoadFailed;
        private bool isRequiredRemoteUpdateDetected;
        private eRemoteContentDownloadState remoteContentDownloadState = eRemoteContentDownloadState.CHECKING;
        private long remoteContentTotalDownloadBytes;
        private long remoteContentDownloadedBytes;
        private int pendingRemoteDataLoadCount;
        private string remoteDataFailureReason = string.Empty;

        private sealed class CResourceLoadEntry
        {
            public string addressableKey;
            public string[] fallbackResourcePathArray;

            ///<summary>
            /// 리소스 로드 엔트리 초기화
            ///</summary>
            public CResourceLoadEntry( string _addressableKey, string[] _fallbackResourcePathArray )
            {
                addressableKey = _addressableKey;
                fallbackResourcePathArray = _fallbackResourcePathArray != null ? _fallbackResourcePathArray : new string[ 0 ];
            }
        }

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

            InitializeResourceCatalog();
            PreloadCoreResources();
        }

        ///<summary>
        /// 리소스 카탈로그 초기화
        ///</summary>
        private void InitializeResourceCatalog()
        {
            resourceLoadEntryDictionary.Clear();
            RegisterResourceLoadEntry( eResourceKey.POPUP_ITEM_INVENTORY, InventoryPopupPrefabResourcePath, InventoryPopupPrefabResourcePath, LegacyInventoryPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_SKILL_LIST, SkillPopupPrefabResourcePath, SkillPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_SHOP, ShopPopupPrefabResourcePath, ShopPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_QUEST_LIST_NPC, NpcQuestPopupPrefabResourcePath, NpcQuestPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_QUEST_LIST_PLAYER, PlayerQuestPopupPrefabResourcePath, PlayerQuestPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_REWARD, RewardPopupPrefabResourcePath, RewardPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_CUBE, CubePopupPrefabResourcePath, CubePopupPrefabResourcePath, LegacyCubePopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_COMMON_NOTICE, CommonNoticePopupPrefabResourcePath, CommonNoticePopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_COMMON_INPUT_FIELD, CommonInputFieldPopupPrefabResourcePath, CommonInputFieldPopupPrefabResourcePath );
            RegisterResourceLoadEntry( eResourceKey.POPUP_CONTENT_DOWNLOAD, ContentDownloadPopupPrefabResourcePath, ContentDownloadPopupPrefabResourcePath );
        }

        ///<summary>
        /// 리소스 로드 엔트리 등록
        ///</summary>
        private void RegisterResourceLoadEntry( eResourceKey _resourceKey, string _addressableKey, params string[] _fallbackResourcePathArray )
        {
            if ( _resourceKey == eResourceKey.NONE )
            {
                return;
            }

            CResourceLoadEntry resourceLoadEntry = new CResourceLoadEntry( _addressableKey, _fallbackResourcePathArray );
            resourceLoadEntryDictionary[ _resourceKey ] = resourceLoadEntry;
        }

        ///<summary>
        /// 핵심 리소스 프리로드
        ///</summary>
        public void PreloadCoreResources()
        {
            InitializeResourceCatalog();
            string[] inventoryPopupResourcePathArray = new string[]
            {
                InventoryPopupPrefabResourcePath,
                LegacyInventoryPopupPrefabResourcePath
            };

            LoadFirstAvailableResource<GameObject>( inventoryPopupResourcePathArray );
            LoadResource<GameObject>( SkillPopupPrefabResourcePath );
            LoadResource<GameObject>( ShopPopupPrefabResourcePath );
            LoadResource<GameObject>( NpcQuestPopupPrefabResourcePath );
            LoadResource<GameObject>( PlayerQuestPopupPrefabResourcePath );
            LoadResource<GameObject>( RewardPopupPrefabResourcePath );
            LoadResource<GameObject>( CommonNoticePopupPrefabResourcePath );
            LoadResource<GameObject>( CommonInputFieldPopupPrefabResourcePath );
            LoadResource<GameObject>( ContentDownloadPopupPrefabResourcePath );
            LoadResource<GameObject>( ItemTooltipPrefabResourcePath );
            LoadResource<GameObject>( SkillTooltipPrefabResourcePath );
            LoadResource<GameObject>( MapLoadingUiPrefabResourcePath );
            LoadResourceAll<CItemDefinition>( ItemDefinitionResourcePath );
            LoadResourceAll<CShopDefinition>( ShopDefinitionResourcePath );
            LoadResourceAll<CQuestDefinition>( QuestDefinitionResourcePath );
            RequestRemoteDataPreload();
        }

        ///<summary>
        /// 원격 정의 데이터 준비 여부 반환
        ///</summary>
        public bool IsRemoteDataReady()
        {
            bool result = isRemoteDataReady;
            return result;
        }

        ///<summary>
        /// 원격 데이터 로드 실패 여부 반환
        ///</summary>
        public bool HasRemoteDataLoadFailed()
        {
            bool result = hasRemoteDataLoadFailed;
            return result;
        }

        ///<summary>
        /// 원격 데이터 로드 실패 사유 반환
        ///</summary>
        public string GetRemoteDataFailureReason()
        {
            string result = remoteDataFailureReason;
            return result;
        }

        public bool IsRemoteContentDownloadConfirmationRequired()
        {
            bool result = remoteContentDownloadState == eRemoteContentDownloadState.AWAITING_CONFIRMATION;
            return result;
        }

        public bool IsRemoteContentDownloading()
        {
            bool result = remoteContentDownloadState == eRemoteContentDownloadState.DOWNLOADING;
            return result;
        }

        ///<summary>
        /// 원격 콘텐츠 검증 상태 반환
        ///</summary>
        public bool IsRemoteContentVerifying()
        {
            bool result = remoteContentDownloadState == eRemoteContentDownloadState.VERIFYING;
            return result;
        }

        public long GetRemoteContentTotalDownloadBytes()
        {
            long result = remoteContentTotalDownloadBytes;
            return result;
        }

        public long GetRemoteContentDownloadedBytes()
        {
            long result = remoteContentDownloadedBytes;
            return result;
        }

        public void ConfirmRemoteContentDownload()
        {
            if ( remoteContentDownloadState != eRemoteContentDownloadState.AWAITING_CONFIRMATION )
            {
                return;
            }

            StartCoroutine( IE_DownloadRequiredRemoteContent() );
        }

        public void RejectRemoteContentDownload()
        {
            if ( remoteContentDownloadState != eRemoteContentDownloadState.AWAITING_CONFIRMATION )
            {
                return;
            }

            MarkRemoteDataLoadFailed( "필수 업데이트 다운로드가 취소되었습니다." );
        }

        ///<summary>
        /// 인벤토리 팝업 프리팹 반환
        ///</summary>
        public GameObject GetInventoryPopupPrefab()
        {
            string[] inventoryPopupResourcePathArray = new string[]
            {
                InventoryPopupPrefabResourcePath,
                LegacyInventoryPopupPrefabResourcePath
            };

            GameObject result = LoadFirstAvailableResource<GameObject>( inventoryPopupResourcePathArray );
            return result;
        }

        ///<summary>
        /// 스킬 팝업 프리팹 반환
        ///</summary>
        public GameObject GetSkillPopupPrefab()
        {
            GameObject result = LoadResource<GameObject>( SkillPopupPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 상점 팝업 프리팹 반환
        ///</summary>
        public GameObject GetShopPopupPrefab()
        {
            GameObject result = LoadResource<GameObject>( ShopPopupPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// NPC 퀘스트 팝업 프리팹 반환
        ///</summary>
        public GameObject GetNpcQuestPopupPrefab()
        {
            GameObject result = LoadResource<GameObject>( NpcQuestPopupPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 플레이어 퀘스트 팝업 프리팹 반환
        ///</summary>
        public GameObject GetPlayerQuestPopupPrefab()
        {
            GameObject result = LoadResource<GameObject>( PlayerQuestPopupPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 보상 팝업 프리팹 반환
        ///</summary>
        public GameObject GetRewardPopupPrefab()
        {
            GameObject result = LoadResource<GameObject>( RewardPopupPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 큐브 팝업 프리팹 반환
        ///</summary>
        public GameObject GetCubePopupPrefab()
        {
            string[] cubePopupResourcePathArray = new string[]
            {
                CubePopupPrefabResourcePath,
                LegacyCubePopupPrefabResourcePath
            };

            GameObject result = LoadFirstAvailableResource<GameObject>( cubePopupResourcePathArray );
            return result;
        }

        ///<summary>
        /// 리소스 키 기반 프리팹 비동기 반환
        ///</summary>
        public void LoadPrefabAsync( eResourceKey _resourceKey, Action<GameObject> _onCompleted )
        {
            bool hasResourceLoadEntry = resourceLoadEntryDictionary.TryGetValue( _resourceKey, out CResourceLoadEntry resourceLoadEntry );

            if ( hasResourceLoadEntry == false || resourceLoadEntry == null )
            {
                Debug.LogWarning( $"[ ResourceManager ] Resource key is not registered: {_resourceKey}" );
                InvokeLoadCompletedHandler( _onCompleted, null );
                return;
            }

            LoadAddressableResourceWithFallbackAsync<GameObject>( resourceLoadEntry.addressableKey, resourceLoadEntry.fallbackResourcePathArray, _onCompleted );
        }

        ///<summary>
        /// Addressables 우선 에셋 비동기 반환
        ///</summary>
        public void LoadAssetAsync<T>( string _addressableKey, string _fallbackResourcePath, Action<T> _onCompleted ) where T : Object
        {
            LoadAddressableResourceWithFallbackAsync( _addressableKey, _fallbackResourcePath, _onCompleted );
        }

        ///<summary>
        /// Addressables 우선 에셋 비동기 반환
        ///</summary>
        public void LoadAssetAsync<T>( string _addressableKey, string[] _fallbackResourcePathArray, Action<T> _onCompleted ) where T : Object
        {
            LoadAddressableResourceWithFallbackAsync( _addressableKey, _fallbackResourcePathArray, _onCompleted );
        }

        ///<summary>
        /// 아이템 툴팁 프리팹 반환
        ///</summary>
        public GameObject GetItemTooltipPrefab()
        {
            GameObject result = LoadResource<GameObject>( ItemTooltipPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 스킬 툴팁 프리팹 반환
        ///</summary>
        public GameObject GetSkillTooltipPrefab()
        {
            GameObject result = LoadResource<GameObject>( SkillTooltipPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 맵 로딩 UI 프리팹 반환
        ///</summary>
        public GameObject GetMapLoadingUiPrefab()
        {
            GameObject result = LoadResource<GameObject>( MapLoadingUiPrefabResourcePath );
            return result;
        }

        ///<summary>
        /// 아이템 정의 목록 반환
        ///</summary>
        public CItemDefinition[] GetItemDefinitionArray()
        {
            CItemDefinition[] result = LoadResourceAll<CItemDefinition>( ItemDefinitionResourcePath );
            return result;
        }

        ///<summary>
        /// 상점 정의 목록 반환
        ///</summary>
        public CShopDefinition[] GetShopDefinitionArray()
        {
            CShopDefinition[] result = LoadResourceAll<CShopDefinition>( ShopDefinitionResourcePath );
            return result;
        }

        ///<summary>
        /// 퀘스트 정의 목록 반환
        ///</summary>
        public CQuestDefinition[] GetQuestDefinitionArray()
        {
            CQuestDefinition[] result = LoadResourceAll<CQuestDefinition>( QuestDefinitionResourcePath );
            return result;
        }

        ///<summary>
        /// 플레이어 기본 스탯 테이블 반환
        ///</summary>
        public CPlayerDefaultStatTableData GetPlayerDefaultStatTableData()
        {
            CPlayerDefaultStatTableData result = GetAddressableCachedResource<CPlayerDefaultStatTableData>( PlayerDefaultStatTablePath );
            return result;
        }

        ///<summary>
        /// 플레이어 레벨 스탯 테이블 반환
        ///</summary>
        public CPlayerLevelStatTableData GetPlayerLevelStatTableData()
        {
            CPlayerLevelStatTableData result = GetAddressableCachedResource<CPlayerLevelStatTableData>( PlayerLevelStatTablePath );
            return result;
        }

        ///<summary>
        /// 몬스터 스탯 테이블 반환
        ///</summary>
        public CMonsterStatTableData GetMonsterStatTableData()
        {
            CMonsterStatTableData result = GetAddressableCachedResource<CMonsterStatTableData>( MonsterStatTablePath );
            return result;
        }

        ///<summary>
        /// 장비 잠재능력 테이블 반환
        ///</summary>
        public CEquipmentPotentialTableData GetEquipmentPotentialTableData()
        {
            CEquipmentPotentialTableData result = GetAddressableCachedResource<CEquipmentPotentialTableData>( EquipmentPotentialTablePath );
            return result;
        }

        ///<summary>
        /// 텍스트 테이블 목록 반환
        ///</summary>
        public CTextTableData[] GetTextTableDataArray()
        {
            CTextTableData loadedTableData = GetAddressableCachedResource<CTextTableData>( TextTableDataPath );

            if ( loadedTableData != null )
            {
                CTextTableData[] addressableResult = new CTextTableData[]
                {
                    loadedTableData
                };
                return addressableResult;
            }

            CTextTableData[] result = LoadResourceAll<CTextTableData>( "Data/Text" );
            return result;
        }

        ///<summary>
        /// 원격 정의 데이터 선로딩 요청
        ///</summary>
        private void RequestRemoteDataPreload()
        {
            if ( isRemoteDataPreloadRequested )
            {
                return;
            }

            isRemoteDataPreloadRequested = true;
            isRemoteDataReady = false;
            hasRemoteDataLoadFailed = false;
            isRequiredRemoteUpdateDetected = false;
            remoteContentDownloadState = eRemoteContentDownloadState.CHECKING;
            remoteContentTotalDownloadBytes = 0L;
            remoteContentDownloadedBytes = 0L;
            remoteDataFailureReason = string.Empty;
            StartCoroutine( IE_PrepareRemoteData() );
        }

        ///<summary>
        /// 원격 카탈로그 확인 후 데이터 로드 준비
        ///</summary>
        private IEnumerator IE_PrepareRemoteData()
        {
            AsyncOperationHandle<List<string>> checkHandle = Addressables.CheckForCatalogUpdates( false );
            float checkElapsedTime = 0.0f;

            while ( checkHandle.IsValid() && checkHandle.IsDone == false && checkElapsedTime < RemoteCatalogOperationTimeoutSeconds )
            {
                checkElapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            if ( checkHandle.IsValid() == false || checkHandle.IsDone == false )
            {
                if ( checkHandle.IsValid() )
                {
                    Addressables.Release( checkHandle );
                }

                HandleRemoteCatalogFailure( "원격 콘텐츠 업데이트 확인 시간이 초과되었습니다." );
                yield break;
            }

            if ( checkHandle.Status != AsyncOperationStatus.Succeeded )
            {
                if ( checkHandle.IsValid() )
                {
                    Addressables.Release( checkHandle );
                }

                HandleRemoteCatalogFailure( "원격 콘텐츠 업데이트 확인에 실패했습니다." );
                yield break;
            }

            List<string> catalogIdList = checkHandle.Result != null ? new List<string>( checkHandle.Result ) : new List<string>();

            if ( checkHandle.IsValid() )
            {
                Addressables.Release( checkHandle );
            }

            if ( catalogIdList.Count > 0 )
            {
                isRequiredRemoteUpdateDetected = true;
                AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs( true, catalogIdList, false );
                float updateElapsedTime = 0.0f;

                while ( updateHandle.IsValid() && updateHandle.IsDone == false && updateElapsedTime < RemoteCatalogOperationTimeoutSeconds )
                {
                    updateElapsedTime += Time.unscaledDeltaTime;
                    yield return null;
                }

                bool isUpdated = updateHandle.IsValid() && updateHandle.IsDone && updateHandle.Status == AsyncOperationStatus.Succeeded;

                if ( updateHandle.IsValid() )
                {
                    Addressables.Release( updateHandle );
                }

                if ( isUpdated == false )
                {
                    MarkRemoteDataLoadFailed( "필수 원격 콘텐츠 카탈로그 갱신에 실패했습니다." );
                    yield break;
                }
            }

            yield return IE_CheckRequiredRemoteContentDownload();
        }

        private IEnumerator IE_CheckRequiredRemoteContentDownload()
        {
            AsyncOperationHandle<long> downloadSizeHandle = Addressables.GetDownloadSizeAsync( RuntimeAddressableLabel );
            yield return downloadSizeHandle;

            if ( downloadSizeHandle.Status != AsyncOperationStatus.Succeeded )
            {
                if ( downloadSizeHandle.IsValid() )
                {
                    Addressables.Release( downloadSizeHandle );
                }

                HandleRemoteCatalogFailure( "원격 콘텐츠 다운로드 크기 확인에 실패했습니다." );
                yield break;
            }

            remoteContentTotalDownloadBytes = Math.Max( 0L, downloadSizeHandle.Result );
            remoteContentDownloadedBytes = 0L;

            if ( downloadSizeHandle.IsValid() )
            {
                Addressables.Release( downloadSizeHandle );
            }

            if ( remoteContentTotalDownloadBytes <= 0L )
            {
                BeginRemoteDataLoads();
                yield break;
            }

            isRequiredRemoteUpdateDetected = true;
            remoteContentDownloadState = eRemoteContentDownloadState.AWAITING_CONFIRMATION;
        }

        private IEnumerator IE_DownloadRequiredRemoteContent()
        {
            remoteContentDownloadState = eRemoteContentDownloadState.DOWNLOADING;
            remoteContentDownloadedBytes = 0L;
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync( RuntimeAddressableLabel, false );

            while ( downloadHandle.IsValid() && downloadHandle.IsDone == false )
            {
                DownloadStatus downloadStatus = downloadHandle.GetDownloadStatus();
                remoteContentDownloadedBytes = Math.Max( 0L, downloadStatus.DownloadedBytes );
                remoteContentTotalDownloadBytes = Math.Max( remoteContentTotalDownloadBytes, downloadStatus.TotalBytes );
                yield return null;
            }

            bool isDownloaded = downloadHandle.IsValid() && downloadHandle.Status == AsyncOperationStatus.Succeeded;

            if ( downloadHandle.IsValid() )
            {
                DownloadStatus downloadStatus = downloadHandle.GetDownloadStatus();
                remoteContentDownloadedBytes = Math.Max( 0L, downloadStatus.DownloadedBytes );
                remoteContentTotalDownloadBytes = Math.Max( remoteContentTotalDownloadBytes, downloadStatus.TotalBytes );
                Addressables.Release( downloadHandle );
            }

            if ( isDownloaded == false )
            {
                MarkRemoteDataLoadFailed( "필수 원격 콘텐츠 다운로드에 실패했습니다." );
                yield break;
            }

            remoteContentDownloadedBytes = remoteContentTotalDownloadBytes;
            BeginRemoteDataLoads();
        }

        ///<summary>
        /// 원격 카탈로그 실패 정책 처리
        ///</summary>
        private void HandleRemoteCatalogFailure( string _failureReason )
        {
            if ( CAddressablesRuntimeConfig.IsRemoteContentRequired )
            {
                MarkRemoteDataLoadFailed( _failureReason );
                return;
            }

            Debug.LogWarning( $"[ ResourceManager ] {_failureReason} Resources fallback을 사용합니다." );
            BeginRemoteDataLoads();
        }

        ///<summary>
        /// 원격 데이터 비동기 로드 시작
        ///</summary>
        private void BeginRemoteDataLoads()
        {
            remoteContentDownloadState = eRemoteContentDownloadState.VERIFYING;
            pendingRemoteDataLoadCount = RemoteDataLoadCount;
            StartCoroutine( IE_LoadAddressableResourceArrayWithFallback<CItemDefinition>( ItemDataAddressableLabel, ItemDefinitionResourcePath, HandleItemDefinitionArrayLoaded ) );
            StartCoroutine( IE_LoadAddressableResourceArrayWithFallback<CShopDefinition>( ShopDataAddressableLabel, ShopDefinitionResourcePath, HandleShopDefinitionArrayLoaded ) );
            StartCoroutine( IE_LoadAddressableResourceArrayWithFallback<CQuestDefinition>( QuestDataAddressableLabel, QuestDefinitionResourcePath, HandleQuestDefinitionArrayLoaded ) );
            StartCoroutine( IE_LoadAddressableResourceWithFallback<CPlayerDefaultStatTableData>( PlayerDefaultStatTablePath, new string[] { PlayerDefaultStatTablePath }, HandleRemoteTableLoaded, true ) );
            StartCoroutine( IE_LoadAddressableResourceWithFallback<CPlayerLevelStatTableData>( PlayerLevelStatTablePath, new string[] { PlayerLevelStatTablePath }, HandleRemoteTableLoaded, true ) );
            StartCoroutine( IE_LoadAddressableResourceWithFallback<CMonsterStatTableData>( MonsterStatTablePath, new string[] { MonsterStatTablePath }, HandleRemoteTableLoaded, true ) );
            StartCoroutine( IE_LoadAddressableResourceWithFallback<CTextTableData>( TextTableDataPath, new string[] { TextTableDataPath }, HandleTextTableLoaded, true ) );
            StartCoroutine( IE_LoadAddressableResourceWithFallback<CEquipmentPotentialTableData>( EquipmentPotentialTablePath, new string[] { EquipmentPotentialTablePath }, HandleEquipmentPotentialTableLoaded, true ) );
        }

        ///<summary>
        /// Addressables 캐시 우선 단일 데이터 반환
        ///</summary>
        private T GetAddressableCachedResource<T>( string _resourcePath ) where T : Object
        {
            bool hasCachedAddressable = cachedResourceDictionary.TryGetValue( _resourcePath, out Object cachedResourceObject );
            T cachedAddressable = cachedResourceObject as T;

            if ( hasCachedAddressable && cachedAddressable != null )
            {
                return cachedAddressable;
            }

            T result = LoadResource<T>( _resourcePath );
            return result;
        }

        ///<summary>
        /// Addressables 라벨 기반 복수 리소스 로드 후 fallback 처리
        ///</summary>
        private IEnumerator IE_LoadAddressableResourceArrayWithFallback<T>( string _addressableLabel, string _fallbackResourcePath, Action<T[]> _onCompleted ) where T : Object
        {
            for ( int attempt = 1; attempt <= RemoteDataLoadMaxAttempts; attempt++ )
            {
                AsyncOperationHandle<IList<T>> loadHandle = Addressables.LoadAssetsAsync<T>( _addressableLabel, null );
                yield return loadHandle;

                if ( loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null && loadHandle.Result.Count > 0 )
                {
                    T[] loadedResourceArray = new T[ loadHandle.Result.Count ];
                    Object[] cachedObjectArray = new Object[ loadHandle.Result.Count ];

                    for ( int index = 0; index < loadHandle.Result.Count; index++ )
                    {
                        T loadedResource = loadHandle.Result[ index ];
                        loadedResourceArray[ index ] = loadedResource;
                        cachedObjectArray[ index ] = loadedResource;
                    }

                    cachedResourceArrayDictionary[ _fallbackResourcePath ] = cachedObjectArray;
                    cachedAddressableHandleDictionary[ _addressableLabel ] = loadHandle;
                    Debug.Log( $"[ ResourceManager ] Addressables data load success: {_addressableLabel}, Count: {loadedResourceArray.Length}, Attempt: {attempt}" );
                    _onCompleted?.Invoke( loadedResourceArray );
                    yield break;
                }

                if ( loadHandle.IsValid() )
                {
                    Addressables.Release( loadHandle );
                }

                bool shouldRetry = ShouldBlockRemoteDataFallback() && attempt < RemoteDataLoadMaxAttempts;

                if ( shouldRetry == false )
                {
                    break;
                }

                Debug.LogWarning( $"[ ResourceManager ] Addressables data load retry scheduled. Label: {_addressableLabel}, Attempt: {attempt}" );
                yield return new WaitForSecondsRealtime( RemoteDataLoadRetryDelaySeconds );
            }

            if ( ShouldBlockRemoteDataFallback() )
            {
                MarkRemoteDataLoadFailed( $"필수 원격 데이터 로드에 실패했습니다. Label: {_addressableLabel}" );
                _onCompleted?.Invoke( null );
                yield break;
            }

            T[] fallbackResourceArray = LoadResourceAll<T>( _fallbackResourcePath );
            Debug.LogWarning( $"[ ResourceManager ] Addressables data load failed. Fallback: {_fallbackResourcePath}" );
            _onCompleted?.Invoke( fallbackResourceArray );
        }

        ///<summary>
        /// 아이템 정의 데이터 로드 완료 처리
        ///</summary>
        private void HandleItemDefinitionArrayLoaded( CItemDefinition[] _definitionArray )
        {
            CItemDefinitionDatabase.Reload();
            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 상점 정의 데이터 로드 완료 처리
        ///</summary>
        private void HandleShopDefinitionArrayLoaded( CShopDefinition[] _definitionArray )
        {
            CShopDefinitionDatabase.Reload();
            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 퀘스트 정의 데이터 로드 완료 처리
        ///</summary>
        private void HandleQuestDefinitionArrayLoaded( CQuestDefinition[] _definitionArray )
        {
            CQuestDefinitionDatabase.Reload();
            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 원격 단일 테이블 로드 완료 처리
        ///</summary>
        private void HandleRemoteTableLoaded<T>( T _tableData ) where T : Object
        {
            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 원격 텍스트 테이블 로드 완료 처리
        ///</summary>
        private void HandleTextTableLoaded( CTextTableData _tableData )
        {
            bool hasDataManager = CDataManager.TryGetExistingInstance( out CDataManager dataManager );

            if ( hasDataManager && dataManager != null )
            {
                dataManager.RebuildTableCache();
            }

            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 원격 장비 잠재능력 테이블 로드 완료 처리
        ///</summary>
        private void HandleEquipmentPotentialTableLoaded( CEquipmentPotentialTableData _tableData )
        {
            CEquipmentPotentialDatabase.Reload();
            CompleteRemoteDataLoad();
        }

        ///<summary>
        /// 원격 정의 데이터 단위 로드 완료 처리
        ///</summary>
        private void CompleteRemoteDataLoad()
        {
            pendingRemoteDataLoadCount = Mathf.Max( 0, pendingRemoteDataLoadCount - 1 );

            if ( pendingRemoteDataLoadCount > 0 )
            {
                return;
            }

            isRemoteDataReady = true;

            if ( hasRemoteDataLoadFailed )
            {
                return;
            }

            remoteContentDownloadState = eRemoteContentDownloadState.COMPLETED;
            Debug.Log( "[ ResourceManager ] Remote definition data is ready." );
        }

        ///<summary>
        /// 원격 데이터 fallback 차단 여부 반환
        ///</summary>
        private bool ShouldBlockRemoteDataFallback()
        {
            bool result = isRequiredRemoteUpdateDetected || CAddressablesRuntimeConfig.IsRemoteContentRequired;
            return result;
        }

        ///<summary>
        /// 원격 데이터 로드 실패 상태 설정
        ///</summary>
        private void MarkRemoteDataLoadFailed( string _failureReason )
        {
            hasRemoteDataLoadFailed = true;
            isRemoteDataReady = true;
            remoteContentDownloadState = eRemoteContentDownloadState.FAILED;
            remoteDataFailureReason = _failureReason;
            Debug.LogError( $"[ ResourceManager ] {_failureReason}" );
        }

        ///<summary>
        /// 단일 리소스 로드
        ///</summary>
        private T LoadResource<T>( string _resourcePath ) where T : Object
        {
            if ( string.IsNullOrWhiteSpace( _resourcePath ) )
            {
                return null;
            }

            bool hasCachedResource = cachedResourceDictionary.TryGetValue( _resourcePath, out Object cachedResourceObject );
            T cachedResource = cachedResourceObject as T;

            if ( hasCachedResource && cachedResource != null )
            {
                return cachedResource;
            }

            T loadedResource = Resources.Load<T>( _resourcePath );

            if ( loadedResource == null )
            {
                Debug.LogWarning( $"[ ResourceManager ] Resource load failed: {_resourcePath}" );
                return null;
            }

            cachedResourceDictionary[ _resourcePath ] = loadedResource;
            return loadedResource;
        }

        ///<summary>
        /// Addressables 우선 단일 리소스 비동기 로드
        ///</summary>
        private void LoadAddressableResourceWithFallbackAsync<T>( string _addressableKey, string _fallbackResourcePath, Action<T> _onCompleted ) where T : Object
        {
            string[] fallbackResourcePathArray = new string[]
            {
                _fallbackResourcePath
            };

            LoadAddressableResourceWithFallbackAsync( _addressableKey, fallbackResourcePathArray, _onCompleted );
        }

        ///<summary>
        /// Addressables 우선 단일 리소스 비동기 로드
        ///</summary>
        private void LoadAddressableResourceWithFallbackAsync<T>( string _addressableKey, string[] _fallbackResourcePathArray, Action<T> _onCompleted ) where T : Object
        {
            if ( string.IsNullOrWhiteSpace( _addressableKey ) )
            {
                T fallbackResource = LoadFirstAvailableResource<T>( _fallbackResourcePathArray );
                InvokeLoadCompletedHandler( _onCompleted, fallbackResource );
                return;
            }

            bool hasCachedResource = cachedResourceDictionary.TryGetValue( _addressableKey, out Object cachedResourceObject );
            T cachedResource = cachedResourceObject as T;

            if ( hasCachedResource && cachedResource != null )
            {
                InvokeLoadCompletedHandler( _onCompleted, cachedResource );
                return;
            }

            StartCoroutine( IE_LoadAddressableResourceWithFallback( _addressableKey, _fallbackResourcePathArray, _onCompleted ) );
        }

        ///<summary>
        /// Addressables 로드 후 fallback 처리 코루틴
        ///</summary>
        private IEnumerator IE_LoadAddressableResourceWithFallback<T>( string _addressableKey, string[] _fallbackResourcePathArray, Action<T> _onCompleted, bool _isRemoteDataLoad = false ) where T : Object
        {
            for ( int attempt = 1; attempt <= RemoteDataLoadMaxAttempts; attempt++ )
            {
                AsyncOperationHandle<IList<IResourceLocation>> locationHandle = Addressables.LoadResourceLocationsAsync( _addressableKey, typeof( T ) );
                yield return locationHandle;

                bool hasAddressableLocation = locationHandle.Status == AsyncOperationStatus.Succeeded && locationHandle.Result != null && locationHandle.Result.Count > 0;

                if ( locationHandle.IsValid() )
                {
                    Addressables.Release( locationHandle );
                }

                if ( hasAddressableLocation )
                {
                    AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>( _addressableKey );
                    yield return loadHandle;

                    if ( loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null )
                    {
                        T loadedResource = loadHandle.Result;
                        cachedResourceDictionary[ _addressableKey ] = loadedResource;
                        cachedAddressableHandleDictionary[ _addressableKey ] = loadHandle;
                        Debug.Log( $"[ ResourceManager ] Addressables load success: {_addressableKey}, Attempt: {attempt}" );
                        InvokeLoadCompletedHandler( _onCompleted, loadedResource );
                        yield break;
                    }

                    if ( loadHandle.IsValid() )
                    {
                        Addressables.Release( loadHandle );
                    }
                }

                bool shouldRetry = _isRemoteDataLoad && ShouldBlockRemoteDataFallback() && attempt < RemoteDataLoadMaxAttempts;

                if ( shouldRetry == false )
                {
                    break;
                }

                Debug.LogWarning( $"[ ResourceManager ] Addressables load retry scheduled. Key: {_addressableKey}, Attempt: {attempt}" );
                yield return new WaitForSecondsRealtime( RemoteDataLoadRetryDelaySeconds );
            }

            Debug.LogWarning( $"[ ResourceManager ] Addressables load failed: {_addressableKey}" );

            if ( _isRemoteDataLoad && ShouldBlockRemoteDataFallback() )
            {
                MarkRemoteDataLoadFailed( $"필수 원격 데이터 다운로드에 실패했습니다. Key: {_addressableKey}" );
                InvokeLoadCompletedHandler( _onCompleted, null );
                yield break;
            }

            T fallbackResource = LoadFirstAvailableResource<T>( _fallbackResourcePathArray );
            InvokeLoadCompletedHandler( _onCompleted, fallbackResource );
        }

        ///<summary>
        /// 비동기 로드 완료 콜백 호출
        ///</summary>
        private void InvokeLoadCompletedHandler<T>( Action<T> _onCompleted, T _loadedResource ) where T : Object
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _loadedResource );
        }

        ///<summary>
        /// 대체 경로 포함 단일 리소스 로드
        ///</summary>
        private T LoadFirstAvailableResource<T>( string[] _resourcePathArray ) where T : Object
        {
            if ( _resourcePathArray == null || _resourcePathArray.Length == 0 )
            {
                return null;
            }

            for ( int index = 0; index < _resourcePathArray.Length; index++ )
            {
                string resourcePath = _resourcePathArray[ index ];
                T loadedResource = LoadResource<T>( resourcePath );

                if ( loadedResource != null )
                {
                    return loadedResource;
                }
            }

            return null;
        }

        ///<summary>
        /// 복수 리소스 로드
        ///</summary>
        private T[] LoadResourceAll<T>( string _resourcePath ) where T : Object
        {
            if ( string.IsNullOrWhiteSpace( _resourcePath ) )
            {
                return new T[ 0 ];
            }

            bool hasCachedResourceArray = cachedResourceArrayDictionary.TryGetValue( _resourcePath, out Object[] cachedResourceObjectArray );

            if ( hasCachedResourceArray )
            {
                T[] cachedResourceArray = ConvertResourceArray<T>( cachedResourceObjectArray );
                return cachedResourceArray;
            }

            T[] loadedResourceArray = Resources.LoadAll<T>( _resourcePath );
            Object[] cachedObjectArray = new Object[ loadedResourceArray.Length ];

            for ( int index = 0; index < loadedResourceArray.Length; index++ )
            {
                cachedObjectArray[ index ] = loadedResourceArray[ index ];
            }

            cachedResourceArrayDictionary[ _resourcePath ] = cachedObjectArray;
            return loadedResourceArray;
        }

        ///<summary>
        /// 캐시 배열 타입 변환
        ///</summary>
        private T[] ConvertResourceArray<T>( Object[] _resourceObjectArray ) where T : Object
        {
            if ( _resourceObjectArray == null || _resourceObjectArray.Length == 0 )
            {
                return new T[ 0 ];
            }

            T[] convertedResourceArray = new T[ _resourceObjectArray.Length ];

            for ( int index = 0; index < _resourceObjectArray.Length; index++ )
            {
                convertedResourceArray[ index ] = _resourceObjectArray[ index ] as T;
            }

            return convertedResourceArray;
        }

        ///<summary>
        /// Addressables 핸들 해제 및 인스턴스 참조 정리
        ///</summary>
        protected override void OnDestroy()
        {
            foreach ( KeyValuePair<string, AsyncOperationHandle> handleEntry in cachedAddressableHandleDictionary )
            {
                AsyncOperationHandle handle = handleEntry.Value;

                if ( handle.IsValid() == false )
                {
                    continue;
                }

                Addressables.Release( handle );
            }

            cachedAddressableHandleDictionary.Clear();
            base.OnDestroy();
        }
    }
}
