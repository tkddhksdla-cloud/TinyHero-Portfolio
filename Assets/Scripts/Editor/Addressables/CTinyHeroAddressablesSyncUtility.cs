using System.Collections.Generic;
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
        private const string MenuPath = "TinyHero/Addressables/Sync Runtime Resources";

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

            AddressableAssetGroup group = settings.FindGroup( CTinyHeroDataValidationRules.AddressableGroupName );

            if ( group == null )
            {
                AddIssue( _issueList, $"Addressables 그룹을 찾을 수 없습니다. Group: {CTinyHeroDataValidationRules.AddressableGroupName}" );
                return false;
            }

            settings.AddLabel( CTinyHeroDataValidationRules.RuntimeAddressableLabel, false );
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();

            for ( int index = 0; index < syncRuleList.Count; index++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = syncRuleList[ index ];
                _registeredCount += SyncRule( settings, group, syncRule, _issueList );
            }

            settings.SetDirty( AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true );
            AssetDatabase.SaveAssets();
            return true;
        }

        ///<summary>
        /// 단일 규칙 기준 Addressables 엔트리 동기화
        ///</summary>
        private static int SyncRule( AddressableAssetSettings _settings, AddressableAssetGroup _group, CTinyHeroDataValidationRules.CAddressableSyncRule _syncRule, List<string> _issueList )
        {
            if ( _settings == null || _group == null || _syncRule == null )
            {
                return 0;
            }

            if ( AssetDatabase.IsValidFolder( _syncRule.searchRootPath ) == false )
            {
                if ( _syncRule.isRequiredFolder )
                {
                    AddIssue( _issueList, $"동기화 대상 폴더를 찾을 수 없습니다. Path: {_syncRule.searchRootPath}" );
                }

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

                if ( CTinyHeroDataValidationRules.IsAddressableSyncTargetAssetPath( assetPath ) == false )
                {
                    continue;
                }

                string addressableKey = CTinyHeroDataValidationRules.BuildAddressableKey( assetPath );

                if ( string.IsNullOrWhiteSpace( addressableKey ) )
                {
                    continue;
                }

                AddressableAssetEntry entry = _settings.CreateOrMoveEntry( guid, _group, false, false );
                entry.SetAddress( addressableKey, false );
                entry.SetLabel( CTinyHeroDataValidationRules.RuntimeAddressableLabel, true, false, false );
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

    }
}
