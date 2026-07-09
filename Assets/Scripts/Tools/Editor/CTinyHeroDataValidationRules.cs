using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 데이터 검증 공용 규칙
    ///</summary>
    public static class CTinyHeroDataValidationRules
    {
        public const string AddressableGroupName = "TinyHero_Local";
        public const string RuntimeAddressableLabel = "TinyHero.RuntimeResource";

        private const string ResourcesRootPath = "Assets/Resources/";
        private const string MapDataSearchRootPath = "Assets/Resources/MapData";
        private const string BackgroundImageSearchRootPath = "Assets/Resources/RawImages/BG";
        private const string PrefabSearchRootPath = "Assets/Resources/Prefabs";
        private const string HotfixSearchRootPath = "Assets/Resources/Hotfix";
        private const string AudioSearchRootPath = "Assets/Resources/Audio";

        ///<summary>
        /// Addressables 자동 동기화 규칙 데이터
        ///</summary>
        public sealed class CAddressableSyncRule
        {
            public string searchRootPath;
            public string searchFilter;
            public bool isRequiredFolder;

            ///<summary>
            /// Addressables 자동 동기화 규칙 초기화
            ///</summary>
            public CAddressableSyncRule( string _searchRootPath, string _searchFilter, bool _isRequiredFolder )
            {
                searchRootPath = _searchRootPath;
                searchFilter = _searchFilter;
                isRequiredFolder = _isRequiredFolder;
            }
        }

        ///<summary>
        /// Addressables 자동 동기화 규칙 목록 생성
        ///</summary>
        public static List<CAddressableSyncRule> CreateAddressableSyncRuleList()
        {
            List<CAddressableSyncRule> syncRuleList = new List<CAddressableSyncRule>();
            syncRuleList.Add( new CAddressableSyncRule( MapDataSearchRootPath, "t:TextAsset", true ) );
            syncRuleList.Add( new CAddressableSyncRule( BackgroundImageSearchRootPath, "t:Texture2D", true ) );
            syncRuleList.Add( new CAddressableSyncRule( PrefabSearchRootPath, "t:Prefab", true ) );
            syncRuleList.Add( new CAddressableSyncRule( HotfixSearchRootPath, "t:TextAsset", true ) );
            syncRuleList.Add( new CAddressableSyncRule( AudioSearchRootPath, "t:AudioClip", true ) );
            syncRuleList.Add( new CAddressableSyncRule( AudioSearchRootPath, "t:AudioMixer", true ) );
            return syncRuleList;
        }

        ///<summary>
        /// Addressables 자동 동기화 대상 에셋 경로 목록 반환
        ///</summary>
        public static List<string> FindAddressableSyncTargetAssetPaths()
        {
            List<string> assetPathList = new List<string>();
            HashSet<string> assetPathSet = new HashSet<string>();
            List<CAddressableSyncRule> syncRuleList = CreateAddressableSyncRuleList();

            for ( int index = 0; index < syncRuleList.Count; index++ )
            {
                CAddressableSyncRule syncRule = syncRuleList[ index ];
                AddAddressableSyncTargetAssetPaths( assetPathList, assetPathSet, syncRule );
            }

            assetPathList.Sort( StringComparer.Ordinal );
            return assetPathList;
        }

        ///<summary>
        /// Addressables 자동 동기화 대상 에셋 경로 여부 반환
        ///</summary>
        public static bool IsAddressableSyncTargetAssetPath( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return false;
            }

            string normalizedAssetPath = _assetPath.Replace( "\\", "/" );

            if ( normalizedAssetPath.EndsWith( ".meta", StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            if ( normalizedAssetPath.StartsWith( ResourcesRootPath, StringComparison.Ordinal ) == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// Resources 기준 Addressables 키 구성
        ///</summary>
        public static string BuildAddressableKey( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return string.Empty;
            }

            string normalizedAssetPath = _assetPath.Replace( "\\", "/" );

            if ( normalizedAssetPath.StartsWith( ResourcesRootPath, StringComparison.Ordinal ) == false )
            {
                return string.Empty;
            }

            string resourcesRelativePath = normalizedAssetPath.Substring( ResourcesRootPath.Length );
            string addressableKey = Path.ChangeExtension( resourcesRelativePath, null );
            string result = addressableKey.Replace( "\\", "/" );
            return result;
        }

        ///<summary>
        /// 단일 규칙의 Addressables 동기화 대상 에셋 경로 추가
        ///</summary>
        private static void AddAddressableSyncTargetAssetPaths( List<string> _assetPathList, HashSet<string> _assetPathSet, CAddressableSyncRule _syncRule )
        {
            if ( _assetPathList == null || _assetPathSet == null || _syncRule == null )
            {
                return;
            }

            if ( AssetDatabase.IsValidFolder( _syncRule.searchRootPath ) == false )
            {
                return;
            }

            string[] searchRootPathArray = new string[]
            {
                _syncRule.searchRootPath
            };
            string[] guidArray = AssetDatabase.FindAssets( _syncRule.searchFilter, searchRootPathArray );

            for ( int index = 0; index < guidArray.Length; index++ )
            {
                string guid = guidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( guid );

                if ( IsAddressableSyncTargetAssetPath( assetPath ) == false || _assetPathSet.Contains( assetPath ) )
                {
                    continue;
                }

                _assetPathSet.Add( assetPath );
                _assetPathList.Add( assetPath );
            }
        }

        ///<summary>
        /// ID 목록 검증 결과 생성
        ///</summary>
        public static List<CTinyHeroDataValidationResult> ValidateIdSet<TAsset>( IReadOnlyList<TAsset> _assetList, string _category, string _idName, Func<TAsset, string> _idGetter, string _searchRootPath ) where TAsset : UnityEngine.Object
        {
            List<CTinyHeroDataValidationResult> resultList = ValidateIdSet( _assetList, _category, _idName, _idGetter, _searchRootPath, ResolveAssetPath );
            return resultList;
        }

        ///<summary>
        /// ID 목록 검증 결과 생성
        ///</summary>
        public static List<CTinyHeroDataValidationResult> ValidateIdSet<TAsset>( IReadOnlyList<TAsset> _assetList, string _category, string _idName, Func<TAsset, string> _idGetter, string _searchRootPath, Func<TAsset, string> _assetPathGetter ) where TAsset : UnityEngine.Object
        {
            List<CTinyHeroDataValidationResult> resultList = new List<CTinyHeroDataValidationResult>();

            if ( _assetList == null || _assetList.Count == 0 )
            {
                string message = $"{_searchRootPath} 경로에서 {_category} 에셋을 찾지 못했습니다.";
                resultList.Add( new CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity.WARNING, _category, $"No {_category} assets", message, _searchRootPath ) );
                return resultList;
            }

            Dictionary<string, string> assetPathById = new Dictionary<string, string>();

            for ( int index = 0; index < _assetList.Count; index++ )
            {
                TAsset asset = _assetList[ index ];

                if ( asset == null )
                {
                    continue;
                }

                string assetPath = _assetPathGetter != null ? _assetPathGetter( asset ) : ResolveAssetPath( asset );
                string id = _idGetter != null ? _idGetter( asset ) : string.Empty;

                if ( string.IsNullOrWhiteSpace( id ) )
                {
                    string message = $"{asset.name}의 {_idName} 값이 비어 있습니다.";
                    resultList.Add( new CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity.ERROR, _category, $"Empty {_idName}", message, assetPath ) );
                    continue;
                }

                string normalizedId = id.Trim();

                if ( assetPathById.ContainsKey( normalizedId ) )
                {
                    string message = $"{_idName}가 중복됩니다. Id: {normalizedId}, First: {assetPathById[ normalizedId ]}";
                    resultList.Add( new CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity.ERROR, _category, $"Duplicate {_idName}", message, assetPath ) );
                    continue;
                }

                assetPathById.Add( normalizedId, assetPath );
            }

            return resultList;
        }

        ///<summary>
        /// 빌드 차단 대상 결과 존재 여부 반환
        ///</summary>
        public static bool HasBlockingIssue( IReadOnlyList<CTinyHeroDataValidationResult> _resultList, bool _blockWarnings )
        {
            if ( _resultList == null || _resultList.Count == 0 )
            {
                return false;
            }

            for ( int index = 0; index < _resultList.Count; index++ )
            {
                CTinyHeroDataValidationResult result = _resultList[ index ];

                if ( result == null )
                {
                    continue;
                }

                if ( result.severity == eTinyHeroDataValidationSeverity.ERROR )
                {
                    return true;
                }

                if ( _blockWarnings && result.severity == eTinyHeroDataValidationSeverity.WARNING )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 검증 결과 정렬
        ///</summary>
        public static void SortResults( List<CTinyHeroDataValidationResult> _resultList )
        {
            if ( _resultList == null )
            {
                return;
            }

            _resultList.Sort( CompareValidationResult );
        }

        ///<summary>
        /// 에셋 경로 반환
        ///</summary>
        private static string ResolveAssetPath( UnityEngine.Object _asset )
        {
            if ( _asset == null )
            {
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath( _asset );
            string result = string.IsNullOrWhiteSpace( assetPath ) ? _asset.name : assetPath;
            return result;
        }

        ///<summary>
        /// 검증 결과 정렬 비교
        ///</summary>
        private static int CompareValidationResult( CTinyHeroDataValidationResult _left, CTinyHeroDataValidationResult _right )
        {
            if ( _left == null && _right == null )
            {
                return 0;
            }

            if ( _left == null )
            {
                return 1;
            }

            if ( _right == null )
            {
                return -1;
            }

            int severityCompare = _left.severity.CompareTo( _right.severity );

            if ( severityCompare != 0 )
            {
                return severityCompare;
            }

            int categoryCompare = string.Compare( _left.category, _right.category, StringComparison.Ordinal );

            if ( categoryCompare != 0 )
            {
                return categoryCompare;
            }

            int assetPathCompare = string.Compare( _left.assetPath, _right.assetPath, StringComparison.Ordinal );
            return assetPathCompare;
        }
    }
}
