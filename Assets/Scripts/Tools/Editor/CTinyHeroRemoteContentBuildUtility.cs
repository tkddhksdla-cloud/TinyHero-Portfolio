using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 원격 Addressables 구성 및 콘텐츠 업데이트 빌드 유틸리티
    ///</summary>
    public static class CTinyHeroRemoteContentBuildUtility
    {
        private const string ConfigureMenuPath = "TinyHero/Addressables/Configure Remote Distribution";
        private const string InitialBuildMenuPath = "TinyHero/Addressables/Build Initial Remote Content";
        private const string UpdateBuildMenuPath = "TinyHero/Addressables/Build Remote Content Update";
        private const string RemoteBuildPathVariableName = "Remote.BuildPath";
        private const string RemoteLoadPathVariableName = "Remote.LoadPath";
        private const string RemoteBuildPathValue = "ServerData/[BuildTarget]";
        private const string RemoteLoadPathValue = "{TinyHero.Core.CAddressablesRuntimeConfig.RemoteBaseUrl}/[BuildTarget]";
        private const int RemoteRequestTimeoutSeconds = 5;
        private const int RemoteRequestRetryCount = 1;

        ///<summary>
        /// 원격 배포 설정 메뉴 실행
        ///</summary>
        [MenuItem( ConfigureMenuPath )]
        public static void ConfigureRemoteDistribution()
        {
            List<string> issueList = new List<string>();
            bool isConfigured = TryConfigureRemoteDistribution( issueList );

            if ( isConfigured )
            {
                Debug.Log( "[ Addressables Remote ] Remote distribution configured." );
                return;
            }

            LogIssues( issueList );
        }

        ///<summary>
        /// 최초 원격 콘텐츠 빌드 메뉴 실행
        ///</summary>
        [MenuItem( InitialBuildMenuPath )]
        public static void BuildInitialRemoteContentFromMenu()
        {
            bool isBuilt = BuildInitialRemoteContent();

            if ( isBuilt )
            {
                Debug.Log( "[ Addressables Remote ] Initial remote content build completed." );
            }
        }

        ///<summary>
        /// 원격 콘텐츠 업데이트 빌드 메뉴 실행
        ///</summary>
        [MenuItem( UpdateBuildMenuPath )]
        public static void BuildRemoteContentUpdateFromMenu()
        {
            string contentStatePath = EditorUtility.OpenFilePanel( "Addressables Content State 선택", Application.dataPath, "bin" );

            if ( string.IsNullOrWhiteSpace( contentStatePath ) )
            {
                return;
            }

            bool isBuilt = BuildRemoteContentUpdate( contentStatePath );

            if ( isBuilt )
            {
                Debug.Log( "[ Addressables Remote ] Content update build completed." );
            }
        }

        ///<summary>
        /// 원격 배포 설정 보장
        ///</summary>
        public static bool TryConfigureRemoteDistribution( List<string> _issueList )
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if ( settings == null )
            {
                AddIssue( _issueList, "AddressableAssetSettings를 찾을 수 없습니다." );
                return false;
            }

            AddressableAssetGroup localGroup = settings.FindGroup( CTinyHeroDataValidationRules.AddressableGroupName );

            if ( localGroup == null )
            {
                AddIssue( _issueList, $"로컬 Addressables 그룹을 찾을 수 없습니다. Group: {CTinyHeroDataValidationRules.AddressableGroupName}" );
                return false;
            }

            AddressableAssetGroup remoteGroup = EnsureRemoteGroup( settings );

            if ( remoteGroup == null )
            {
                AddIssue( _issueList, $"원격 Addressables 그룹 생성에 실패했습니다. Group: {CTinyHeroDataValidationRules.RemoteAddressableGroupName}" );
                return false;
            }

            settings.profileSettings.SetValue( settings.activeProfileId, RemoteBuildPathVariableName, RemoteBuildPathValue );
            settings.profileSettings.SetValue( settings.activeProfileId, RemoteLoadPathVariableName, RemoteLoadPathValue );
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.CatalogRequestsTimeout = RemoteRequestTimeoutSeconds;
            settings.RemoteCatalogBuildPath.SetVariableByName( settings, RemoteBuildPathVariableName );
            settings.RemoteCatalogLoadPath.SetVariableByName( settings, RemoteLoadPathVariableName );
            ConfigureRemoteGroupSchemas( settings, remoteGroup );
            EditorUtility.SetDirty( settings );
            EditorUtility.SetDirty( remoteGroup );
            AssetDatabase.SaveAssets();
            return true;
        }

        ///<summary>
        /// 최초 원격 Addressables 콘텐츠 빌드
        ///</summary>
        public static bool BuildInitialRemoteContent()
        {
            List<string> issueList = new List<string>();
            bool isConfigured = TryConfigureRemoteDistribution( issueList );

            if ( isConfigured == false )
            {
                LogIssues( issueList );
                return false;
            }

            bool isSynced = CTinyHeroAddressablesSyncUtility.TrySyncRuntimeResources( issueList, out int registeredCount );

            if ( isSynced == false )
            {
                LogIssues( issueList );
                return false;
            }

            AddressableAssetSettings.BuildPlayerContent( out AddressablesPlayerBuildResult buildResult );

            if ( buildResult == null || string.IsNullOrWhiteSpace( buildResult.Error ) == false )
            {
                string errorMessage = buildResult != null ? buildResult.Error : "Build result is null.";
                Debug.LogError( $"[ Addressables Remote ] Initial content build failed. {errorMessage}" );
                return false;
            }

            Debug.Log( $"[ Addressables Remote ] Initial content build completed. Synced: {registeredCount}, State: {buildResult.ContentStateFilePath}" );
            return true;
        }

        ///<summary>
        /// 이전 콘텐츠 상태 기준 원격 업데이트 빌드
        ///</summary>
        public static bool BuildRemoteContentUpdate( string _contentStatePath )
        {
            Debug.Log( $"[ Addressables Remote ] Content update build started. State: {_contentStatePath}" );

            if ( string.IsNullOrWhiteSpace( _contentStatePath ) || File.Exists( _contentStatePath ) == false )
            {
                Debug.LogError( $"[ Addressables Remote ] Content state file not found. Path: {_contentStatePath}" );
                return false;
            }

            List<string> issueList = new List<string>();
            Debug.Log( "[ Addressables Remote ] Configuring remote distribution." );
            bool isConfigured = TryConfigureRemoteDistribution( issueList );

            if ( isConfigured == false )
            {
                LogIssues( issueList );
                return false;
            }

            Debug.Log( "[ Addressables Remote ] Synchronizing runtime Addressables resources." );
            bool isSynced = CTinyHeroAddressablesSyncUtility.TrySyncRuntimeResources( issueList, out int registeredCount );

            if ( isSynced == false )
            {
                LogIssues( issueList );
                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Debug.Log( "[ Addressables Remote ] Starting Addressables ContentUpdateScript." );
            AddressablesPlayerBuildResult buildResult = ContentUpdateScript.BuildContentUpdate( settings, _contentStatePath );

            if ( buildResult == null || string.IsNullOrWhiteSpace( buildResult.Error ) == false )
            {
                string errorMessage = buildResult != null ? buildResult.Error : "Build result is null.";
                Debug.LogError( $"[ Addressables Remote ] Content update build failed. {errorMessage}" );
                return false;
            }

            Debug.Log( $"[ Addressables Remote ] Content update build completed. Synced: {registeredCount}, Catalog: {buildResult.RemoteCatalogJsonFilePath}" );
            return true;
        }

        ///<summary>
        /// 원격 그룹 보장
        ///</summary>
        private static AddressableAssetGroup EnsureRemoteGroup( AddressableAssetSettings _settings )
        {
            AddressableAssetGroup remoteGroup = _settings.FindGroup( CTinyHeroDataValidationRules.RemoteAddressableGroupName );

            if ( remoteGroup != null )
            {
                return remoteGroup;
            }

            AddressableAssetGroup createdGroup = _settings.CreateGroup(
                CTinyHeroDataValidationRules.RemoteAddressableGroupName,
                false,
                false,
                true,
                null,
                typeof( ContentUpdateGroupSchema ),
                typeof( BundledAssetGroupSchema )
            );
            return createdGroup;
        }

        ///<summary>
        /// 원격 그룹 스키마 구성
        ///</summary>
        private static void ConfigureRemoteGroupSchemas( AddressableAssetSettings _settings, AddressableAssetGroup _remoteGroup )
        {
            BundledAssetGroupSchema bundledSchema = _remoteGroup.GetSchema<BundledAssetGroupSchema>();

            if ( bundledSchema != null )
            {
                bundledSchema.BuildPath.SetVariableByName( _settings, RemoteBuildPathVariableName );
                bundledSchema.LoadPath.SetVariableByName( _settings, RemoteLoadPathVariableName );
                bundledSchema.UseAssetBundleCache = true;
                bundledSchema.Timeout = RemoteRequestTimeoutSeconds;
                bundledSchema.RetryCount = RemoteRequestRetryCount;
                EditorUtility.SetDirty( bundledSchema );
            }

            ContentUpdateGroupSchema contentUpdateSchema = _remoteGroup.GetSchema<ContentUpdateGroupSchema>();

            if ( contentUpdateSchema != null )
            {
                contentUpdateSchema.StaticContent = false;
                EditorUtility.SetDirty( contentUpdateSchema );
            }
        }

        ///<summary>
        /// 이슈 추가
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
        /// 이슈 로그 출력
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
                Debug.LogError( $"[ Addressables Remote ] {issue}" );
            }
        }
    }
}
