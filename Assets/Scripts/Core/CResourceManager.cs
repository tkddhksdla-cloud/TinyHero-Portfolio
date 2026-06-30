using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Quest;
using UnityEngine;

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
        private const string ItemDefinitionResourcePath = "Data/Item/Definitions";
        private const string ShopDefinitionResourcePath = "Data/Shop/Definitions";
        private const string QuestDefinitionResourcePath = "Data/Quest/Definitions";

        private readonly Dictionary<string, Object> cachedResourceDictionary = new Dictionary<string, Object>();
        private readonly Dictionary<string, Object[]> cachedResourceArrayDictionary = new Dictionary<string, Object[]>();

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

            PreloadCoreResources();
        }

        ///<summary>
        /// 핵심 리소스 프리로드
        ///</summary>
        public void PreloadCoreResources()
        {
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
    }
}
