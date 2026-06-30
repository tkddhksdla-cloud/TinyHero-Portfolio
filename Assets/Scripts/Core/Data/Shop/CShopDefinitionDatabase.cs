using System.Collections.Generic;
using TinyHero.Core;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 상점 정의 조회 데이터베이스
    ///</summary>
    public static class CShopDefinitionDatabase
    {
        private static readonly Dictionary<string, CShopDefinition> shopDefinitionDictionary = new Dictionary<string, CShopDefinition>();
        private static readonly List<CShopDefinition> shopDefinitionList = new List<CShopDefinition>();
        private static bool isInitialized;

        ///<summary>
        /// 상점 정의 목록 반환
        ///</summary>
        public static IReadOnlyList<CShopDefinition> GetShopDefinitionList()
        {
            EnsureInitialized();
            IReadOnlyList<CShopDefinition> result = shopDefinitionList;
            return result;
        }

        ///<summary>
        /// 상점 정의 조회 시도
        ///</summary>
        public static bool TryGetShopDefinition( string _shopId, out CShopDefinition _shopDefinition )
        {
            EnsureInitialized();

            if ( string.IsNullOrWhiteSpace( _shopId ) )
            {
                _shopDefinition = null;
                return false;
            }

            string normalizedShopId = _shopId.Trim();
            bool result = shopDefinitionDictionary.TryGetValue( normalizedShopId, out CShopDefinition resolvedShopDefinition );
            _shopDefinition = resolvedShopDefinition;
            return result;
        }

        ///<summary>
        /// 상점 정의 강제 재로드
        ///</summary>
        public static void Reload()
        {
            isInitialized = false;
            shopDefinitionDictionary.Clear();
            shopDefinitionList.Clear();
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
            shopDefinitionDictionary.Clear();
            shopDefinitionList.Clear();
            CResourceManager resourceManager = CResourceManager.Instance;
            CShopDefinition[] loadedDefinitionArray = resourceManager != null ? resourceManager.GetShopDefinitionArray() : new CShopDefinition[ 0 ];

            for ( int index = 0; index < loadedDefinitionArray.Length; index++ )
            {
                CShopDefinition shopDefinition = loadedDefinitionArray[ index ];

                if ( shopDefinition == null )
                {
                    continue;
                }

                string shopId = shopDefinition.GetShopId();

                if ( string.IsNullOrWhiteSpace( shopId ) || shopDefinitionDictionary.ContainsKey( shopId ) )
                {
                    continue;
                }

                shopDefinitionDictionary.Add( shopId, shopDefinition );
                shopDefinitionList.Add( shopDefinition );
            }
        }
    }
}
