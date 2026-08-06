using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 빌드 전 프로젝트 준비 상태 검증 유틸리티
    ///</summary>
    public static class CTinyHeroPrebuildReadinessValidator
    {
        private const string MenuPath = "TinyHero/Build/Validate Prebuild Readiness";
        private const string HybridClrPackageName = "com.code-philosophy.hybridclr";
        private const string PackageManifestPath = "Packages/manifest.json";
        private const string HybridClrSettingsPath = "ProjectSettings/HybridCLRSettings.asset";
        private const string HotfixAssemblyName = "TinyHero.Hotfix";
        private const string HotfixPayloadAssetPath = "Assets/Resources/Hotfix/TinyHero.Hotfix.dll.bytes";
        private const string NpoiXmlPluginPath = "Assets/Plugins/NPOI/System.Security.Cryptography.Xml.dll";
        private const string VisualStudioVcToolsComponentId = "Microsoft.VisualStudio.Component.VC.Tools.x86.x64";

        private static readonly string[] RequiredScenePaths =
        {
            "Assets/Scenes/SceneTitle.unity",
            "Assets/Scenes/SceneMap.unity",
            "Assets/Scenes/SceneMapTool.unity"
        };

        private static readonly string[] RequiredResourceFolderPaths =
        {
            "Assets/Resources/MapData",
            "Assets/Resources/RawImages/BG",
            "Assets/Resources/Prefabs",
            "Assets/Resources/Hotfix"
        };

        private static readonly string[] RequiredAssetPaths =
        {
            "Assets/Resources/Prefabs/Character/Player/PlayerObject.prefab",
            "Assets/Resources/Prefabs/Core/CGameManager.prefab",
            HotfixPayloadAssetPath
        };

        private static readonly string[] RequiredAddressableKeys =
        {
            "Prefabs/Character/Player/PlayerObject",
            "Prefabs/Core/CGameManager",
            "Hotfix/TinyHero.Hotfix.dll"
        };

        ///<summary>
        /// 빌드 전 준비 상태 검증 메뉴 실행
        ///</summary>
        [MenuItem( MenuPath )]
        public static void ValidatePrebuildReadinessFromMenu()
        {
            bool isPassed = ValidatePrebuildReadiness();

            if ( isPassed )
            {
                Debug.Log( "[TinyHero Build] Prebuild readiness validation passed." );
            }
        }

        ///<summary>
        /// 빌드 전 준비 상태 검증 실행
        ///</summary>
        public static bool ValidatePrebuildReadiness()
        {
            List<string> errorList = new List<string>();
            List<string> warningList = new List<string>();
            ValidateConsoleState( errorList, warningList );
            ValidateRequiredScenes( errorList );
            ValidateRequiredResourceFolders( errorList );
            ValidateRequiredAssets( errorList );
            ValidateNpoiXmlPluginImporter( errorList );
            ValidateAddressablesState( errorList );
            ValidateHybridClrState( errorList );
            ValidateWindowsIl2CppToolchain( errorList );
            ReportValidationIssues( errorList, warningList );
            bool result = errorList.Count == 0 && warningList.Count == 0;
            return result;
        }

        ///<summary>
        /// Unity 콘솔 상태 검증
        ///</summary>
        private static void ValidateConsoleState( List<string> _errorList, List<string> _warningList )
        {
            if ( _errorList == null || _warningList == null )
            {
                return;
            }

            if ( TryGetConsoleCounts( out int errorCount, out int warningCount ) == false )
            {
                _warningList.Add( "Unity Console 카운트를 읽지 못했습니다." );
                return;
            }

            if ( errorCount > 0 )
            {
                _errorList.Add( $"Unity Console에 Error가 남아 있습니다. Count: {errorCount}" );
            }

            if ( warningCount > 0 )
            {
                _warningList.Add( $"Unity Console에 Warning이 남아 있습니다. Count: {warningCount}" );
            }
        }

        ///<summary>
        /// Unity 콘솔 카운트 조회 시도
        ///</summary>
        private static bool TryGetConsoleCounts( out int _errorCount, out int _warningCount )
        {
            _errorCount = 0;
            _warningCount = 0;
            Type logEntriesType = typeof( EditorWindow ).Assembly.GetType( "UnityEditor.LogEntries" );

            if ( logEntriesType == null )
            {
                return false;
            }

            MethodInfo getCountsMethod = logEntriesType.GetMethod( "GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );

            if ( getCountsMethod == null )
            {
                return false;
            }

            object[] parameterArray =
            {
                0,
                0,
                0
            };
            getCountsMethod.Invoke( null, parameterArray );
            _errorCount = ( int )parameterArray[ 0 ];
            _warningCount = ( int )parameterArray[ 1 ];
            return true;
        }

        ///<summary>
        /// 필수 씬 에셋 상태 검증
        ///</summary>
        private static void ValidateRequiredScenes( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            for ( int index = 0; index < RequiredScenePaths.Length; index++ )
            {
                string scenePath = RequiredScenePaths[ index ];
                UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( scenePath );

                if ( sceneAsset != null )
                {
                    continue;
                }

                _errorList.Add( $"필수 씬을 찾을 수 없습니다. Path: {scenePath}" );
            }
        }

        ///<summary>
        /// 필수 Resources 폴더 상태 검증
        ///</summary>
        private static void ValidateRequiredResourceFolders( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            for ( int index = 0; index < RequiredResourceFolderPaths.Length; index++ )
            {
                string folderPath = RequiredResourceFolderPaths[ index ];

                if ( AssetDatabase.IsValidFolder( folderPath ) )
                {
                    continue;
                }

                _errorList.Add( $"필수 Resources 폴더를 찾을 수 없습니다. Path: {folderPath}" );
            }
        }

        ///<summary>
        /// 필수 에셋 상태 검증
        ///</summary>
        private static void ValidateRequiredAssets( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            for ( int index = 0; index < RequiredAssetPaths.Length; index++ )
            {
                string assetPath = RequiredAssetPaths[ index ];
                UnityEngine.Object targetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( assetPath );

                if ( targetAsset != null )
                {
                    continue;
                }

                _errorList.Add( $"필수 에셋을 찾을 수 없습니다. Path: {assetPath}" );
            }
        }

        ///<summary>
        /// NPOI XML 보조 DLL Importer 상태 검증
        ///</summary>
        private static void ValidateNpoiXmlPluginImporter( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            PluginImporter importer = AssetImporter.GetAtPath( NpoiXmlPluginPath ) as PluginImporter;

            if ( importer == null )
            {
                return;
            }

            bool isAnyPlatformEnabled = importer.GetCompatibleWithAnyPlatform();
            bool isEditorEnabled = importer.GetCompatibleWithEditor();
            bool isStandaloneEnabled = importer.GetCompatibleWithPlatform( BuildTarget.StandaloneWindows64 );

            if ( isAnyPlatformEnabled == false && isEditorEnabled == false && isStandaloneEnabled == false )
            {
                return;
            }

            _errorList.Add( $"NPOI XML 보조 DLL이 에디터 리플렉션 대상입니다. Importer 호환성을 해제해야 합니다. Path: {NpoiXmlPluginPath}" );
        }

        ///<summary>
        /// Addressables 상태 검증
        ///</summary>
        private static void ValidateAddressablesState( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if ( settings == null )
            {
                _errorList.Add( "AddressableAssetSettings를 찾을 수 없습니다." );
                return;
            }

            AddressableAssetGroup group = settings.FindGroup( CTinyHeroDataValidationRules.AddressableGroupName );

            if ( group == null )
            {
                _errorList.Add( $"Addressables 그룹을 찾을 수 없습니다. Group: {CTinyHeroDataValidationRules.AddressableGroupName}" );
                return;
            }

            AddressableAssetGroup remoteGroup = settings.FindGroup( CTinyHeroDataValidationRules.RemoteAddressableGroupName );

            if ( remoteGroup == null )
            {
                _errorList.Add( $"원격 Addressables 그룹을 찾을 수 없습니다. Group: {CTinyHeroDataValidationRules.RemoteAddressableGroupName}" );
                return;
            }

            if ( settings.BuildRemoteCatalog == false )
            {
                _errorList.Add( "Addressables 원격 카탈로그 빌드가 비활성화되어 있습니다." );
            }

            List<string> syncTargetAssetPathList = CTinyHeroDataValidationRules.FindAddressableSyncTargetAssetPaths();

            for ( int index = 0; index < syncTargetAssetPathList.Count; index++ )
            {
                string assetPath = syncTargetAssetPathList[ index ];
                string addressableKey = CTinyHeroDataValidationRules.BuildAddressableKey( assetPath );
                AddressableAssetEntry entry = FindAddressableEntryByAddress( settings, addressableKey );

                if ( entry != null )
                {
                    continue;
                }

                _errorList.Add( $"Addressables 키를 찾을 수 없습니다. Key: {addressableKey}. Menu: TinyHero/Addressables/Sync Runtime Resources" );
            }

            ValidateRequiredAddressableKeys( settings, _errorList );
        }

        ///<summary>
        /// 필수 Addressables 키 상태 검증
        ///</summary>
        private static void ValidateRequiredAddressableKeys( AddressableAssetSettings _settings, List<string> _errorList )
        {
            if ( _settings == null || _errorList == null )
            {
                return;
            }

            for ( int index = 0; index < RequiredAddressableKeys.Length; index++ )
            {
                string addressableKey = RequiredAddressableKeys[ index ];
                AddressableAssetEntry entry = FindAddressableEntryByAddress( _settings, addressableKey );

                if ( entry != null )
                {
                    continue;
                }

                _errorList.Add( $"필수 Addressables 키를 찾을 수 없습니다. Key: {addressableKey}. Menu: TinyHero/Addressables/Sync Runtime Resources" );
            }
        }

        ///<summary>
        /// Addressables 주소 기준 엔트리 반환
        ///</summary>
        private static AddressableAssetEntry FindAddressableEntryByAddress( AddressableAssetSettings _settings, string _addressableKey )
        {
            if ( _settings == null || string.IsNullOrWhiteSpace( _addressableKey ) )
            {
                return null;
            }

            List<AddressableAssetGroup> groupList = _settings.groups;

            for ( int groupIndex = 0; groupIndex < groupList.Count; groupIndex++ )
            {
                AddressableAssetGroup group = groupList[ groupIndex ];

                if ( group == null )
                {
                    continue;
                }

                List<AddressableAssetEntry> entryList = new List<AddressableAssetEntry>();
                group.GatherAllAssets( entryList, true, true, false );

                for ( int entryIndex = 0; entryIndex < entryList.Count; entryIndex++ )
                {
                    AddressableAssetEntry entry = entryList[ entryIndex ];

                    if ( entry == null )
                    {
                        continue;
                    }

                    if ( string.Equals( entry.address, _addressableKey, StringComparison.Ordinal ) )
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        ///<summary>
        /// HybridCLR 상태 검증
        ///</summary>
        private static void ValidateHybridClrState( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            string manifestFullPath = ResolveProjectPath( PackageManifestPath );

            if ( File.Exists( manifestFullPath ) == false )
            {
                _errorList.Add( $"Package manifest를 찾을 수 없습니다. Path: {PackageManifestPath}" );
                return;
            }

            string manifestText = File.ReadAllText( manifestFullPath );

            if ( manifestText.IndexOf( HybridClrPackageName, StringComparison.Ordinal ) < 0 )
            {
                _errorList.Add( $"HybridCLR 패키지가 등록되지 않았습니다. Package: {HybridClrPackageName}" );
            }

            string settingsFullPath = ResolveProjectPath( HybridClrSettingsPath );

            if ( File.Exists( settingsFullPath ) == false )
            {
                _errorList.Add( $"HybridCLR 설정 파일을 찾을 수 없습니다. Path: {HybridClrSettingsPath}" );
                return;
            }

            string settingsText = File.ReadAllText( settingsFullPath );

            if ( settingsText.IndexOf( HotfixAssemblyName, StringComparison.Ordinal ) < 0 )
            {
                _errorList.Add( $"HybridCLR Hot Update Assembly에 {HotfixAssemblyName}가 없습니다." );
            }
        }

        ///<summary>
        /// Windows IL2CPP C++ 툴체인 상태 검증
        ///</summary>
        private static void ValidateWindowsIl2CppToolchain( List<string> _errorList )
        {
            if ( _errorList == null )
            {
                return;
            }

            if ( EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 )
            {
                return;
            }

            bool hasVisualStudioComponent = HasVisualStudioVcToolsComponent();
            bool hasClCompiler = HasWindowsX64ClCompiler();

            if ( hasVisualStudioComponent && hasClCompiler )
            {
                return;
            }

            _errorList.Add( "Windows IL2CPP C++ toolchain이 없습니다. Visual Studio Installer에서 'Desktop development with C++' 워크로드와 'MSVC v143 x64/x86 build tools' 구성요소를 설치해야 합니다." );
        }

        ///<summary>
        /// Visual Studio C++ 도구 구성요소 설치 여부 반환
        ///</summary>
        private static bool HasVisualStudioVcToolsComponent()
        {
            string programFilesX86Path = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 );
            string vsWherePath = Path.Combine( programFilesX86Path, "Microsoft Visual Studio/Installer/vswhere.exe" );

            if ( File.Exists( vsWherePath ) == false )
            {
                return false;
            }

            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = vsWherePath;
                startInfo.Arguments = $"-latest -products * -requires {VisualStudioVcToolsComponentId} -property installationPath";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                using ( System.Diagnostics.Process process = System.Diagnostics.Process.Start( startInfo ) )
                {
                    if ( process == null )
                    {
                        return false;
                    }

                    string outputText = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    bool result = process.ExitCode == 0 && string.IsNullOrWhiteSpace( outputText ) == false;
                    return result;
                }
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[TinyHero Build] Visual Studio C++ toolchain check failed. {exception.Message}" );
                return false;
            }
        }

        ///<summary>
        /// Windows x64 C++ 컴파일러 파일 존재 여부 반환
        ///</summary>
        private static bool HasWindowsX64ClCompiler()
        {
            string programFilesPath = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles );
            string programFilesX86Path = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 );
            string[] visualStudioRootPathArray =
            {
                Path.Combine( programFilesPath, "Microsoft Visual Studio" ),
                Path.Combine( programFilesX86Path, "Microsoft Visual Studio" )
            };

            for ( int index = 0; index < visualStudioRootPathArray.Length; index++ )
            {
                string visualStudioRootPath = visualStudioRootPathArray[ index ];
                bool isFound = HasWindowsX64ClCompilerInVisualStudioRoot( visualStudioRootPath );

                if ( isFound )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// Visual Studio 루트 하위 Windows x64 C++ 컴파일러 존재 여부 반환
        ///</summary>
        private static bool HasWindowsX64ClCompilerInVisualStudioRoot( string _visualStudioRootPath )
        {
            if ( Directory.Exists( _visualStudioRootPath ) == false )
            {
                return false;
            }

            string[] productDirectoryArray = Directory.GetDirectories( _visualStudioRootPath, "*", SearchOption.AllDirectories );

            for ( int index = 0; index < productDirectoryArray.Length; index++ )
            {
                string directoryPath = productDirectoryArray[ index ];
                string normalizedPath = directoryPath.Replace( "\\", "/" );

                if ( normalizedPath.EndsWith( "/VC/Tools/MSVC", StringComparison.OrdinalIgnoreCase ) == false )
                {
                    continue;
                }

                string[] versionDirectoryArray = Directory.GetDirectories( directoryPath );

                for ( int versionIndex = 0; versionIndex < versionDirectoryArray.Length; versionIndex++ )
                {
                    string versionDirectoryPath = versionDirectoryArray[ versionIndex ];
                    string clCompilerPath = Path.Combine( versionDirectoryPath, "bin/Hostx64/x64/cl.exe" );

                    if ( File.Exists( clCompilerPath ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///<summary>
        /// 검증 이슈 출력
        ///</summary>
        private static void ReportValidationIssues( List<string> _errorList, List<string> _warningList )
        {
            if ( _errorList != null )
            {
                for ( int index = 0; index < _errorList.Count; index++ )
                {
                    string error = _errorList[ index ];
                    Debug.LogError( $"[TinyHero Build] {error}" );
                }
            }

            if ( _warningList != null )
            {
                for ( int index = 0; index < _warningList.Count; index++ )
                {
                    string warning = _warningList[ index ];
                    Debug.LogWarning( $"[TinyHero Build] {warning}" );
                }
            }
        }

        ///<summary>
        /// 프로젝트 기준 전체 경로 반환
        ///</summary>
        private static string ResolveProjectPath( string _relativePath )
        {
            string projectRootPath = Directory.GetParent( Application.dataPath ).FullName;
            string result = Path.Combine( projectRootPath, _relativePath );
            return result;
        }
    }
}
