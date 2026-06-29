using System.Collections.Generic;
using TinyHero.Core;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 아이템 정의 조회 데이터베이스
    ///</summary>
    public static class CItemDefinitionDatabase
    {
        private static readonly Dictionary<string, CItemDefinition> itemDefinitionDictionary = new Dictionary<string, CItemDefinition>();
        private static readonly List<CItemDefinition> itemDefinitionList = new List<CItemDefinition>();
        private static bool isInitialized;

        ///<summary>
        /// 전체 아이템 정의 목록 반환
        ///</summary>
        public static IReadOnlyList<CItemDefinition> GetItemDefinitionList()
        {
            EnsureInitialized();
            IReadOnlyList<CItemDefinition> result = itemDefinitionList;
            return result;
        }

        ///<summary>
        /// 아이템 정의 조회 시도
        ///</summary>
        public static bool TryGetItemDefinition( string _itemId, out CItemDefinition _itemDefinition )
        {
            EnsureInitialized();

            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                _itemDefinition = null;
                return false;
            }

            string normalizedItemId = _itemId.Trim();
            bool isFound = itemDefinitionDictionary.TryGetValue( normalizedItemId, out CItemDefinition resolvedItemDefinition );
            _itemDefinition = resolvedItemDefinition;
            return isFound;
        }

        ///<summary>
        /// 아이템 정의 강제 재로드
        ///</summary>
        public static void Reload()
        {
            isInitialized = false;
            itemDefinitionDictionary.Clear();
            itemDefinitionList.Clear();
            EnsureInitialized();
        }

        ///<summary>
        /// 데이터베이스 초기화 보장
        ///</summary>
        private static void EnsureInitialized()
        {
            if ( isInitialized )
            {
                return;
            }

            isInitialized = true;
            itemDefinitionDictionary.Clear();
            itemDefinitionList.Clear();
            CResourceManager resourceManager = CResourceManager.Instance;
            CItemDefinition[] loadedDefinitionArray = resourceManager != null ? resourceManager.GetItemDefinitionArray() : new CItemDefinition[ 0 ];

            for ( int index = 0; index < loadedDefinitionArray.Length; index++ )
            {
                CItemDefinition itemDefinition = loadedDefinitionArray[ index ];

                if ( itemDefinition == null )
                {
                    continue;
                }

                string itemId = itemDefinition.GetItemId();

                if ( string.IsNullOrWhiteSpace( itemId ) )
                {
                    continue;
                }

                if ( itemDefinitionDictionary.ContainsKey( itemId ) )
                {
                    continue;
                }

                itemDefinitionDictionary.Add( itemId, itemDefinition );
                itemDefinitionList.Add( itemDefinition );
            }
        }
    }
}
