using System;
using System.Collections;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Quest;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
        private const string ItemTooltipPrefabResourcePath = "Prefabs/UI/Inventory/ItemTooltipUI";
        private const string SkillTooltipPrefabResourcePath = "Prefabs/UI/Skill/SkillTooltipUI";
        private const string MapLoadingUiPrefabResourcePath = "Prefabs/UI/Map/MapLoadingUI";
        private const string ItemDefinitionResourcePath = "Data/Item/Definitions";
        private const string ShopDefinitionResourcePath = "Data/Shop/Definitions";
        private const string QuestDefinitionResourcePath = "Data/Quest/Definitions";

        private readonly Dictionary<string, Object> cachedResourceDictionary = new Dictionary<string, Object>();
        private readonly Dictionary<string, Object[]> cachedResourceArrayDictionary = new Dictionary<string, Object[]>();
        private readonly Dictionary<string, AsyncOperationHandle> cachedAddressableHandleDictionary = new Dictionary<string, AsyncOperationHandle>();
        private readonly Dictionary<eResourceKey, CResourceLoadEntry> resourceLoadEntryDictionary = new Dictionary<eResourceKey, CResourceLoadEntry>();

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
            LoadResource<GameObject>( ItemTooltipPrefabResourcePath );
            LoadResource<GameObject>( SkillTooltipPrefabResourcePath );
            LoadResource<GameObject>( MapLoadingUiPrefabResourcePath );
            LoadResourceAll<CItemDefinition>( ItemDefinitionResourcePath );
            LoadResourceAll<CShopDefinition>( ShopDefinitionResourcePath );
            LoadResourceAll<CQuestDefinition>( QuestDefinitionResourcePath );
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
        private IEnumerator IE_LoadAddressableResourceWithFallback<T>( string _addressableKey, string[] _fallbackResourcePathArray, Action<T> _onCompleted ) where T : Object
        {
            AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>( _addressableKey );
            yield return loadHandle;

            if ( loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null )
            {
                T loadedResource = loadHandle.Result;
                cachedResourceDictionary[ _addressableKey ] = loadedResource;
                cachedAddressableHandleDictionary[ _addressableKey ] = loadHandle;
                Debug.Log( $"[ ResourceManager ] Addressables load success: {_addressableKey}" );
                InvokeLoadCompletedHandler( _onCompleted, loadedResource );
                yield break;
            }

            if ( loadHandle.IsValid() )
            {
                Addressables.Release( loadHandle );
            }

            Debug.LogWarning( $"[ ResourceManager ] Addressables load failed: {_addressableKey}" );
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
