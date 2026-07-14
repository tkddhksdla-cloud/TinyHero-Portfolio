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

            bool isRemoteDistributionConfigured = CTinyHeroRemoteContentBuildUtility.TryConfigureRemoteDistribution( _issueList );

            if ( isRemoteDistributionConfigured == false )
            {
                return false;
            }

            settings.AddLabel( CTinyHeroDataValidationRules.RuntimeAddressableLabel, false );
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();
            RegisterRuleLabels( settings, syncRuleList );

            for ( int index = 0; index < syncRuleList.Count; index++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = syncRuleList[ index ];
                AddressableAssetGroup targetGroup = settings.FindGroup( syncRule.targetGroupName );

                if ( targetGroup == null )
                {
                    AddIssue( _issueList, $"Addressables 그룹을 찾을 수 없습니다. Group: {syncRule.targetGroupName}" );
                    continue;
                }

                _registeredCount += SyncRule( settings, targetGroup, syncRule, _issueList );
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
                ApplyRuleLabels( entry, _syncRule.labelArray );
                registeredCount++;
            }

            return registeredCount;
        }

        ///<summary>
        /// 동기화 규칙 라벨 등록
        ///</summary>
        private static void RegisterRuleLabels( AddressableAssetSettings _settings, List<CTinyHeroDataValidationRules.CAddressableSyncRule> _syncRuleList )
        {
            if ( _settings == null || _syncRuleList == null )
            {
                return;
            }

            for ( int ruleIndex = 0; ruleIndex < _syncRuleList.Count; ruleIndex++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = _syncRuleList[ ruleIndex ];

                if ( syncRule == null || syncRule.labelArray == null )
                {
                    continue;
                }

                for ( int labelIndex = 0; labelIndex < syncRule.labelArray.Length; labelIndex++ )
                {
                    string label = syncRule.labelArray[ labelIndex ];

                    if ( string.IsNullOrWhiteSpace( label ) )
                    {
                        continue;
                    }

                    _settings.AddLabel( label, false );
                }
            }
        }

        ///<summary>
        /// 엔트리에 동기화 규칙 라벨 적용
        ///</summary>
        private static void ApplyRuleLabels( AddressableAssetEntry _entry, string[] _labelArray )
        {
            if ( _entry == null || _labelArray == null )
            {
                return;
            }

            for ( int index = 0; index < _labelArray.Length; index++ )
            {
                string label = _labelArray[ index ];

                if ( string.IsNullOrWhiteSpace( label ) )
                {
                    continue;
                }

                _entry.SetLabel( label, true, false, false );
            }
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
