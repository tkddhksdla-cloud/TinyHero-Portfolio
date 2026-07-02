using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero Hotfix 페이로드 동기화 유틸리티
    ///</summary>
    public static class CTinyHeroHotfixPayloadUtility
    {
        private const string MenuPath = "TinyHero/HybridCLR/Sync Hotfix Payload";
        private const string HotfixAssemblyName = "TinyHero.Hotfix";
        private const string HotfixDllSourcePath = "HybridCLRData/HotUpdateDlls/StandaloneWindows64/TinyHero.Hotfix.dll";
        private const string HotfixPayloadFolderPath = "Assets/Resources/Hotfix";
        private const string HotfixPayloadAssetPath = "Assets/Resources/Hotfix/TinyHero.Hotfix.dll.bytes";

        ///<summary>Hotfix DLL 페이로드 동기화 메뉴 실행</summary>
        [MenuItem( MenuPath )]
        public static void SyncHotfixPayload()
        {
            List<string> issueList = new List<string>();
            bool isSynced = TrySyncHotfixPayload( issueList );

            if ( isSynced )
            {
                Debug.Log( $"[TinyHero Hotfix] Hotfix payload synced. Assembly: {HotfixAssemblyName}, Path: {HotfixPayloadAssetPath}" );
                return;
            }

            LogIssues( issueList );
        }

        ///<summary>Hotfix DLL 페이로드 동기화 시도</summary>
        public static bool TrySyncHotfixPayload( List<string> _issueList )
        {
            string projectRootPath = Directory.GetParent( Application.dataPath ).FullName;
            string sourceFullPath = Path.Combine( projectRootPath, HotfixDllSourcePath );
            string destinationFullPath = Path.Combine( projectRootPath, HotfixPayloadAssetPath );

            if ( File.Exists( sourceFullPath ) == false )
            {
                AddIssue( _issueList, $"Hotfix DLL을 찾을 수 없습니다. 먼저 HybridCLR hot update DLL을 생성해야 합니다. Path: {HotfixDllSourcePath}" );
                return false;
            }

            EnsurePayloadFolder();
            File.Copy( sourceFullPath, destinationFullPath, true );
            AssetDatabase.ImportAsset( HotfixPayloadAssetPath, ImportAssetOptions.ForceUpdate );
            return true;
        }

        ///<summary>Hotfix 페이로드 폴더 생성 보장</summary>
        private static void EnsurePayloadFolder()
        {
            if ( AssetDatabase.IsValidFolder( HotfixPayloadFolderPath ) )
            {
                return;
            }

            AssetDatabase.CreateFolder( "Assets/Resources", "Hotfix" );
        }

        ///<summary>검증 이슈 추가</summary>
        private static void AddIssue( List<string> _issueList, string _issue )
        {
            if ( _issueList == null )
            {
                return;
            }

            _issueList.Add( _issue );
        }

        ///<summary>검증 이슈 출력</summary>
        private static void LogIssues( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            for ( int index = 0; index < _issueList.Count; index++ )
            {
                string issue = _issueList[ index ];
                Debug.LogError( $"[TinyHero Hotfix] {issue}" );
            }
        }
    }
}
