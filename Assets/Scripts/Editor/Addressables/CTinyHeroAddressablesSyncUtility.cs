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
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if ( settings == null )
            {
                Debug.LogError( "[ Addressables Sync ] AddressableAssetSettings not found." );
                return;
            }

            AddressableAssetGroup group = settings.FindGroup( AddressableGroupName );

            if ( group == null )
            {
                Debug.LogError( $"[ Addressables Sync ] Addressables group not found: {AddressableGroupName}" );
                return;
            }

            List<CAddressableSyncRule> syncRuleList = CreateSyncRuleList();
            int registeredCount = 0;

            for ( int index = 0; index < syncRuleList.Count; index++ )
            {
                CAddressableSyncRule syncRule = syncRuleList[ index ];
                registeredCount += SyncRule( settings, group, syncRule );
            }

            settings.SetDirty( AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true );
            AssetDatabase.SaveAssets();
            Debug.Log( $"[ Addressables Sync ] Runtime resources synced. Count: {registeredCount}" );
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
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Character/Monster", "t:Prefab" ) );
            syncRuleList.Add( new CAddressableSyncRule( "Assets/Resources/Prefabs/Character/NPC", "t:Prefab" ) );
            return syncRuleList;
        }

        ///<summary>
        /// 단일 규칙 기준 Addressables 엔트리 동기화
        ///</summary>
        private static int SyncRule( AddressableAssetSettings _settings, AddressableAssetGroup _group, CAddressableSyncRule _syncRule )
        {
            if ( _settings == null || _group == null || _syncRule == null )
            {
                return 0;
            }

            if ( AssetDatabase.IsValidFolder( _syncRule.searchRootPath ) == false )
            {
                Debug.LogWarning( $"[ Addressables Sync ] Folder not found: {_syncRule.searchRootPath}" );
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
