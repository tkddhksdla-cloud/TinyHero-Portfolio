using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 런타임 리소스 Addressables 동기화 유틸리티
    ///</summary>
    public static class CTinyHeroAddressablesSyncUtility
    {
        private const string AddressableGroupName = "TinyHero_Local";
        private const string ResourcesRootPath = "Assets/Resources/";
        private const string MenuPath = "TinyHero/Addressables/Sync Runtime Resources";

        private sealed class CAddressableSyncRule
        {
            public string searchRootPath;
            public string searchFilter;

            ///<summary>
            /// Addressables 동기화 규칙 초기화
            ///</summary>
            public CAddressableSyncRule( string _searchRootPath, string _searchFilter )
            {
                searchRootPath = _searchRootPath;
                searchFilter = _searchFilter;
            }
        }

        ///<summary>
        /// 런타임 리소스 Addressables 동기화 메뉴 실행
        ///</summary>
        [MenuItem( MenuPath )]
        public static void SyncRuntimeResources()
        {
            List<string> issueList = new List<string>();
            bool isSynced = TrySyncRuntimeResources( issueList, out int registeredCount );

            if ( isSynced )
            {
                Debug.Log( $"[ Addressables Sync ] Runtime resources synced. Count: {registeredCount}" );
                return;
            }

            LogIssues( issueList );
        }

        ///<summary>
        /// 런타임 리소스 Addressables 동기화 시도
        ///</summary>
        public static bool TrySyncRuntimeResources( List<string> _issueList, out int _registeredCount )
        {
            _registeredCount = 0;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if ( settings == null )
            {
                AddIssue( _issueList, "AddressableAssetSettings를 찾을 수 없습니다." );
                return false;
            }

            AddressableAssetGroup group = settings.FindGroup( AddressableGroupName );

            if ( group == null )
            {
                AddIssue( _issueList, $"Addressables 그룹을 찾을 수 없습니다. Group: {AddressableGroupName}" );
                return false;
            }

            List<CAddressableSyncRule> syncRuleList = CreateSyncRuleList();

            for ( int index = 0; index < syncRuleList.Count; index++ )
            {
                CAddressableSyncRule syncRule = syncRuleList[ index ];
                _registeredCount += SyncRule( settings, group, syncRule, _issueList );
            }

            settings.SetDirty( AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true );
            AssetDatabase.SaveAssets();
            return true;
        }

        ///<summary>
        /// Addressables 동기화 규칙 목록 생성
        ///</summary>
        private static List<CAddressableSyncRule> CreateSyncRuleList()
        {
            List<CAddressableSyncRule> syncRuleList = new List<CAddressableSyncRule>();
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/MapData", "t:TextAsset" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/RawImages/BG", "t:Texture2D" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/UI/Popup", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Portal", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Character/Player", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Character/Monster", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Character/NPC", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Hotfix", "t:TextAsset" ) );
            return syncRuleList;
        }

        ///<summary>
        /// 단일 규칙 기준 Addressables 엔트리 동기화
        ///</summary>
        private static int SyncRule( AddressableAssetSettings _settings, AddressableAssetGroup _group, CAddressableSyncRule _syncRule, List<string> _issueList )
        {
            if ( _settings == null || _group == null || _syncRule == null )
            {
                return 0;
            }

            if ( AssetDatabase.IsValidFolder( _syncRule.searchRootPath ) == false )
            {
                AddIssue( _issueList, $"동기화 대상 폴더를 찾을 수 없습니다. Path: {_syncRule.searchRootPath}" );
                return 0;
            }

            string[] searchRootPathArray = new string[]
            {
                _syncRule.searchRootPath
            };
            string[] guidArray = AssetDatabase.FindAssets( _syncRule.searchFilter, searchRootPathArray );
            int registeredCount = 0;

            for ( int index = 0; index < guidArray.Length; index++ )
            {
                string guid = guidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( guid );

                if ( IsSyncTargetAssetPath( assetPath ) == false )
                {
                    continue;
                }

                string addressableKey = BuildAddressableKey( assetPath );

                if ( string.IsNullOrWhiteSpace( addressableKey ) )
                {
                    continue;
                }

                AddressableAssetEntry entry = _settings.CreateOrMoveEntry( guid, _group, false, false );
                entry.SetAddress( addressableKey, false );
                registeredCount++;
            }

            return registeredCount;
        }

        ///<summary>
        /// 동기화 이슈 추가
        ///</summary>
        private static void AddIssue( List<string> _issueList, string _issue )
        {
            if ( _issueList == null )
            {
                return;
            }

            _issueList.Add( _issue );
        }

        ///<summary>
        /// 동기화 이슈 출력
        ///</summary>
        private static void LogIssues( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            for ( int index = 0; index < _issueList.Count; index++ )
            {
                string issue = _issueList[ index ];
                Debug.LogError( $"[ Addressables Sync ] {issue}" );
            }
        }

        ///<summary>
        /// 동기화 대상 에셋 경로 여부
        ///</summary>
        private static bool IsSyncTargetAssetPath( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return false;
            }

            if ( _assetPath.EndsWith( ".meta", System.StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            if ( _assetPath.StartsWith( ResourcesRootPath, System.StringComparison.Ordinal ) == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// Resources 기준 Addressables 키 구성
        ///</summary>
        private static string BuildAddressableKey( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return string.Empty;
            }

            string normalizedAssetPath = _assetPath.Replace( "\\", "/" );

            if ( normalizedAssetPath.StartsWith( ResourcesRootPath, System.StringComparison.Ordinal ) == false )
            {
                return string.Empty;
            }

            string resourcesRelativePath = normalizedAssetPath.Substring( ResourcesRootPath.Length );
            string addressableKey = Path.ChangeExtension( resourcesRelativePath, null );
            string result = addressableKey.Replace( "\\", "/" );
            return result;
        }
    }
}
