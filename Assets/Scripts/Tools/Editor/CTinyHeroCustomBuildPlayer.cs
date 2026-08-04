using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 커스텀 플레이어 빌드 파이프라인
    ///</summary>
    public static class CTinyHeroCustomBuildPlayer
    {
        private const string MenuPath = "TinyHero/Build/Build Windows Player";
        private const string AndroidMenuPath = "TinyHero/Build/Build Android Player";
        private const string IosMenuPath = "TinyHero/Build/Build iOS Player";
        private const string HybridClrGenerateAllMenuPath = "HybridCLR/Generate/All";
        private const string DefaultBuildOutputPath = "Builds/Windows/TinyHero.exe";
        private const string DefaultAndroidBuildOutputPath = "Builds/Android/TinyHero.aab";
        private const string DefaultIosBuildOutputPath = "Builds/iOS";
        private const string GameVersionPattern = @"^\d+\.\d+\.\d+$";
        private const string MethodBridgeGeneratedPath = "HybridCLRData/LocalIl2CppData-WindowsEditor/il2cpp/libil2cpp/hybridclr/generated/MethodBridge.cpp";
        private const string VisualStudioVcToolsComponentId = "Microsoft.VisualStudio.Component.VC.Tools.x86.x64";
        private const string StandalonePlatformName = "Standalone";
        private const string CreateSolutionPlatformSettingName = "CreateSolution";
        private const BuildTarget WindowsBuildTarget = BuildTarget.StandaloneWindows64;
        private const BuildTarget AndroidBuildTarget = BuildTarget.Android;
        private const BuildTarget IosBuildTarget = BuildTarget.iOS;

        ///<summary>
        /// Windows 플레이어 빌드 메뉴 실행
        ///</summary>
        [MenuItem( MenuPath )]
        public static void BuildWindowsPlayerFromMenu()
        {
            bool isBuilt = BuildWindowsPlayer( DefaultBuildOutputPath, PlayerSettings.bundleVersion );

            if ( isBuilt )
            {
                Debug.Log( $"[TinyHero Build] Windows Player build completed. Path: {DefaultBuildOutputPath}" );
            }
        }

        ///<summary>
        /// Windows 플레이어 빌드 실행
        ///</summary>
        public static bool BuildWindowsPlayer( string _outputPath, string _gameVersion )
        {
            string normalizedOutputPath = NormalizeOutputPath( _outputPath );
            Debug.Log( $"[TinyHero Build] Windows Player build started. Version: {_gameVersion}, Output: {normalizedOutputPath}" );
            bool isGameVersionApplied = TryApplyGameVersion( _gameVersion, out string resolvedGameVersion );

            if ( isGameVersionApplied == false )
            {
                return false;
            }

            Debug.Log( "[TinyHero Build] Preparing Windows IL2CPP build settings." );
            bool isBuildSettingsPrepared = PrepareWindowsIl2CppBuildSettings();

            if ( isBuildSettingsPrepared == false )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. Windows IL2CPP build settings preparation failed." );
                return false;
            }

            Debug.Log( "[TinyHero Build] Validating HybridCLR installer state." );
            bool isHybridClrInstalled = EnsureHybridClrInstalled();

            if ( isHybridClrInstalled == false )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. HybridCLR installer preparation failed." );
                return false;
            }

            Debug.Log( "[TinyHero Build] Generating HybridCLR build artifacts." );
            bool isHybridClrGenerated = GenerateHybridClrBuildArtifacts();

            if ( isHybridClrGenerated == false )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. HybridCLR Generate All failed." );
                return false;
            }

            Debug.Log( "[TinyHero Build] Preparing hotfix payload and validating prebuild state." );
            bool isPrepared = CTinyHeroHotfixBuildPreparationUtility.PrepareHotfixBuild( true );

            if ( isPrepared == false )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. Hotfix build preparation failed." );
                return false;
            }

            Debug.Log( "[TinyHero Build] Building Addressables player content." );
            bool isAddressablesBuilt = BuildAddressablesContent();

            if ( isAddressablesBuilt == false )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. Addressables content build failed." );
                return false;
            }

            string[] scenePathArray = BuildScenePathArray();

            if ( scenePathArray.Length == 0 )
            {
                Debug.LogError( "[TinyHero Build] Windows Player build stopped. Enabled build scenes not found." );
                return false;
            }

            EnsureOutputDirectory( normalizedOutputPath );
            BuildPlayerOptions buildPlayerOptions = CreateWindowsBuildPlayerOptions( scenePathArray, normalizedOutputPath );
            Debug.Log( $"[TinyHero Build] Starting Unity BuildPipeline. Scenes: {scenePathArray.Length}" );
            BuildReport buildReport = BuildPipeline.BuildPlayer( buildPlayerOptions );
            bool result = ReportBuildResult( buildReport, normalizedOutputPath );

            if ( result )
            {
                Debug.Log( $"[TinyHero Build] Game version: {resolvedGameVersion}" );
            }

            return result;
        }

        [MenuItem( AndroidMenuPath )]
        public static void BuildAndroidPlayerFromMenu()
        {
            BuildMobilePlayer( AndroidBuildTarget, DefaultAndroidBuildOutputPath, PlayerSettings.bundleVersion );
        }

        [MenuItem( IosMenuPath )]
        public static void BuildIosPlayerFromMenu()
        {
            BuildMobilePlayer( IosBuildTarget, DefaultIosBuildOutputPath, PlayerSettings.bundleVersion );
        }

        public static bool BuildAndroidPlayer( string _outputPath, string _gameVersion )
        {
            bool result = BuildMobilePlayer( AndroidBuildTarget, _outputPath, _gameVersion );
            return result;
        }

        public static bool BuildIosPlayer( string _outputPath, string _gameVersion )
        {
            bool result = BuildMobilePlayer( IosBuildTarget, _outputPath, _gameVersion );
            return result;
        }

        ///<summary>
        /// Android 및 iOS 플레이어 빌드 공통 실행
        ///</summary>
        private static bool BuildMobilePlayer( BuildTarget _buildTarget, string _outputPath, string _gameVersion )
        {
            string normalizedOutputPath = NormalizeOutputPath( _outputPath );
            bool isGameVersionApplied = TryApplyGameVersion( _gameVersion, out string resolvedGameVersion );

            if ( isGameVersionApplied == false || PrepareMobileIl2CppBuildSettings( _buildTarget ) == false || EnsureHybridClrInstalled() == false || GenerateHybridClrBuildArtifacts() == false || CTinyHeroHotfixBuildPreparationUtility.PrepareHotfixBuild( true ) == false || BuildAddressablesContent() == false )
            {
                return false;
            }

            string[] scenePathArray = BuildScenePathArray();

            if ( scenePathArray.Length == 0 )
            {
                Debug.LogError( "[TinyHero Build] Mobile Player build stopped. Enabled build scenes not found." );
                return false;
            }

            EnsureOutputDirectory( normalizedOutputPath );
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenePathArray;
            buildPlayerOptions.locationPathName = normalizedOutputPath;
            buildPlayerOptions.target = _buildTarget;
            buildPlayerOptions.options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport;
            BuildReport buildReport = BuildPipeline.BuildPlayer( buildPlayerOptions );
            bool isIosBuild = _buildTarget == IosBuildTarget;
            bool result = ReportMobileBuildResult( buildReport, normalizedOutputPath, isIosBuild );

            if ( result )
            {
                Debug.Log( $"[TinyHero Build] {_buildTarget} game version: {resolvedGameVersion}" );
            }

            return result;
        }

        ///<summary>
        /// 모바일 IL2CPP 빌드 설정 준비
        ///</summary>
        private static bool PrepareMobileIl2CppBuildSettings( BuildTarget _buildTarget )
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup( _buildTarget );
            bool isSwitched = EditorUserBuildSettings.SwitchActiveBuildTarget( buildTargetGroup, _buildTarget );

            if ( isSwitched == false )
            {
                Debug.LogError( $"[TinyHero Build] Build target switch failed. Target: {_buildTarget}" );
                return false;
            }

            EditorUserBuildSettings.development = false;
#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup( buildTargetGroup );
            PlayerSettings.SetScriptingBackend( namedBuildTarget, ScriptingImplementation.IL2CPP );
#else
            PlayerSettings.SetScriptingBackend( buildTargetGroup, ScriptingImplementation.IL2CPP );
#endif
            return true;
        }

        ///<summary>
        /// Player 빌드 버전 적용 시도
        ///</summary>
        private static bool TryApplyGameVersion( string _gameVersion, out string _resolvedGameVersion )
        {
            string resolvedGameVersion = string.IsNullOrWhiteSpace( _gameVersion ) ? PlayerSettings.bundleVersion : _gameVersion.Trim();
            bool isValid = string.IsNullOrWhiteSpace( resolvedGameVersion ) == false && Regex.IsMatch( resolvedGameVersion, GameVersionPattern );

            if ( isValid == false )
            {
                Debug.LogError( $"[TinyHero Build] Invalid game version. Expected format: 0.0.01. Value: {resolvedGameVersion}" );
                _resolvedGameVersion = string.Empty;
                return false;
            }

            PlayerSettings.bundleVersion = resolvedGameVersion;
            _resolvedGameVersion = resolvedGameVersion;
            Debug.Log( $"[TinyHero Build] Player version applied: {resolvedGameVersion}" );
            return true;
        }

        ///<summary>
        /// Windows IL2CPP 빌드 설정 준비
        ///</summary>
        private static bool PrepareWindowsIl2CppBuildSettings()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup( WindowsBuildTarget );
            bool isSwitched = EditorUserBuildSettings.SwitchActiveBuildTarget( buildTargetGroup, WindowsBuildTarget );

            if ( isSwitched == false )
            {
                Debug.LogError( $"[TinyHero Build] Build target switch failed. Target: {WindowsBuildTarget}" );
                return false;
            }

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.SetPlatformSettings( StandalonePlatformName, CreateSolutionPlatformSettingName, bool.FalseString );
#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup( buildTargetGroup );
            PlayerSettings.SetScriptingBackend( namedBuildTarget, ScriptingImplementation.IL2CPP );
#else
            PlayerSettings.SetScriptingBackend( buildTargetGroup, ScriptingImplementation.IL2CPP );
#endif
            bool hasToolchain = ValidateWindowsIl2CppToolchain();

            if ( hasToolchain == false )
            {
                return false;
            }

            Debug.Log( "[TinyHero Build] Windows IL2CPP build settings prepared." );
            return true;
        }

        ///<summary>
        /// HybridCLR Installer 초기화 보장
        ///</summary>
        private static bool EnsureHybridClrInstalled()
        {
            try
            {
                InstallerController installerController = new InstallerController();

                if ( installerController.HasInstalledHybridCLR() && string.Equals( installerController.PackageVersion, installerController.InstalledLibil2cppVersion, StringComparison.Ordinal ) )
                {
                    Debug.Log( "[TinyHero Build] HybridCLR installer state is already prepared." );
                    return true;
                }

                Debug.Log( "[TinyHero Build] HybridCLR installer state is missing or outdated. Running default installer." );
                installerController.InstallDefaultHybridCLR();
                InstallerController refreshedInstallerController = new InstallerController();
                bool result = refreshedInstallerController.HasInstalledHybridCLR() && string.Equals( refreshedInstallerController.PackageVersion, refreshedInstallerController.InstalledLibil2cppVersion, StringComparison.Ordinal );

                if ( result == false )
                {
                    Debug.LogError( "[TinyHero Build] HybridCLR installer did not complete with a valid installed state." );
                }

                return result;
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"[TinyHero Build] HybridCLR installer preparation failed. {exception.Message}" );
                return false;
            }
        }

        ///<summary>
        /// Windows IL2CPP C++ 툴체인 상태 검증
        ///</summary>
        private static bool ValidateWindowsIl2CppToolchain()
        {
            bool hasVisualStudioComponent = HasVisualStudioVcToolsComponent();
            bool hasClCompiler = HasWindowsX64ClCompiler();

            if ( hasVisualStudioComponent && hasClCompiler )
            {
                return true;
            }

            Debug.LogError( "[TinyHero Build] Windows IL2CPP C++ toolchain is missing. Visual Studio Installer에서 'Desktop development with C++' 워크로드와 'MSVC v143 x64/x86 build tools' 구성요소를 설치해야 합니다." );
            return false;
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
        /// HybridCLR 빌드 산출물 생성
        ///</summary>
        private static bool GenerateHybridClrBuildArtifacts()
        {
            try
            {
                bool isExecuted = EditorApplication.ExecuteMenuItem( HybridClrGenerateAllMenuPath );

                if ( isExecuted == false )
                {
                    Debug.LogError( $"[TinyHero Build] HybridCLR menu was not executed. Menu: {HybridClrGenerateAllMenuPath}" );
                    return false;
                }

                bool isMethodBridgeValid = ValidateMethodBridgeDevelopmentFlag();
                return isMethodBridgeValid;
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"[TinyHero Build] HybridCLR Generate All failed. {exception.Message}" );
                return false;
            }
        }

        ///<summary>
        /// MethodBridge 개발 빌드 플래그 검증
        ///</summary>
        private static bool ValidateMethodBridgeDevelopmentFlag()
        {
            if ( File.Exists( MethodBridgeGeneratedPath ) == false )
            {
                Debug.LogError( $"[TinyHero Build] MethodBridge.cpp was not generated. Path: {MethodBridgeGeneratedPath}" );
                return false;
            }

            string methodBridgeText = File.ReadAllText( MethodBridgeGeneratedPath );
            string expectedFlagText = EditorUserBuildSettings.development ? "// DEVELOPMENT=1" : "// DEVELOPMENT=0";

            if ( methodBridgeText.IndexOf( expectedFlagText, StringComparison.Ordinal ) >= 0 )
            {
                Debug.Log( $"[TinyHero Build] HybridCLR Generate All completed. {expectedFlagText}" );
                return true;
            }

            Debug.LogError( $"[TinyHero Build] MethodBridge.cpp development flag mismatch. Expected: {expectedFlagText}" );
            return false;
        }

        ///<summary>
        /// Addressables 콘텐츠 빌드 실행
        ///</summary>
        private static bool BuildAddressablesContent()
        {
            AddressableAssetSettings.BuildPlayerContent( out AddressablesPlayerBuildResult buildResult );

            if ( buildResult == null )
            {
                Debug.LogError( "[TinyHero Build] Addressables build result is null." );
                return false;
            }

            if ( string.IsNullOrEmpty( buildResult.Error ) )
            {
                Debug.Log( "[TinyHero Build] Addressables content build completed." );
                return true;
            }

            Debug.LogError( $"[TinyHero Build] Addressables content build failed. {buildResult.Error}" );
            return false;
        }

        ///<summary>
        /// 빌드 씬 경로 배열 생성
        ///</summary>
        private static string[] BuildScenePathArray()
        {
            List<string> scenePathList = new List<string>();
            EditorBuildSettingsScene[] buildSceneArray = EditorBuildSettings.scenes;

            for ( int index = 0; index < buildSceneArray.Length; index++ )
            {
                EditorBuildSettingsScene buildScene = buildSceneArray[ index ];

                if ( buildScene == null || buildScene.enabled == false )
                {
                    continue;
                }

                if ( IsCustomBuildScenePath( buildScene.path ) == false )
                {
                    continue;
                }

                scenePathList.Add( buildScene.path );
            }

            string[] result = scenePathList.ToArray();
            return result;
        }

        ///<summary>
        /// 커스텀 빌드 대상 씬 경로 여부
        ///</summary>
        private static bool IsCustomBuildScenePath( string _scenePath )
        {
            if ( string.Equals( _scenePath, "Assets/Scenes/SceneTitle.unity", StringComparison.Ordinal ) )
            {
                return true;
            }

            if ( string.Equals( _scenePath, "Assets/Scenes/SceneMap.unity", StringComparison.Ordinal ) )
            {
                return true;
            }

            if ( string.Equals( _scenePath, "Assets/Scenes/SceneMapTool.unity", StringComparison.Ordinal ) )
            {
                return true;
            }

            return false;
        }

        ///<summary>
        /// Windows BuildPlayerOptions 생성
        ///</summary>
        private static BuildPlayerOptions CreateWindowsBuildPlayerOptions( string[] _scenePathArray, string _outputPath )
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = _scenePathArray;
            buildPlayerOptions.locationPathName = _outputPath;
            buildPlayerOptions.target = WindowsBuildTarget;
            buildPlayerOptions.options = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport;
            return buildPlayerOptions;
        }

        ///<summary>
        /// 빌드 결과 출력
        ///</summary>
        private static bool ReportBuildResult( BuildReport _buildReport, string _outputPath )
        {
            if ( _buildReport == null )
            {
                Debug.LogError( "[TinyHero Build] Build report is null." );
                return false;
            }

            BuildSummary summary = _buildReport.summary;

            if ( summary.result == BuildResult.Succeeded )
            {
                if ( File.Exists( _outputPath ) == false )
                {
                    Debug.LogError( $"[TinyHero Build] Build reported success, but output executable was not found. Path: {_outputPath}" );
                    return false;
                }

                Debug.Log( $"[TinyHero Build] Build succeeded. Path: {_outputPath}, Size: {summary.totalSize}, Time: {summary.totalTime}" );
                return true;
            }

            Debug.LogError( $"[TinyHero Build] Build failed. Result: {summary.result}, Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}" );
            return false;
        }

        ///<summary>
        /// 모바일 플레이어 빌드 결과 검증
        ///</summary>
        private static bool ReportMobileBuildResult( BuildReport _buildReport, string _outputPath, bool _isDirectoryOutput )
        {
            if ( _buildReport == null || _buildReport.summary.result != BuildResult.Succeeded )
            {
                Debug.LogError( "[TinyHero Build] Mobile Player build failed." );
                return false;
            }

            bool isOutputCreated = _isDirectoryOutput ? Directory.Exists( _outputPath ) : File.Exists( _outputPath );

            if ( isOutputCreated == false )
            {
                Debug.LogError( $"[TinyHero Build] Mobile Player output was not found. Path: {_outputPath}" );
                return false;
            }

            return true;
        }

        ///<summary>
        /// 출력 경로 정규화
        ///</summary>
        private static string NormalizeOutputPath( string _outputPath )
        {
            if ( string.IsNullOrWhiteSpace( _outputPath ) )
            {
                return DefaultBuildOutputPath;
            }

            string result = _outputPath.Replace( "\\", "/" );
            return result;
        }

        ///<summary>
        /// 출력 폴더 생성 보장
        ///</summary>
        private static void EnsureOutputDirectory( string _outputPath )
        {
            string directoryPath = Path.GetDirectoryName( _outputPath );

            if ( string.IsNullOrWhiteSpace( directoryPath ) )
            {
                return;
            }

            if ( Directory.Exists( directoryPath ) )
            {
                return;
            }

            Directory.CreateDirectory( directoryPath );
        }
    }
}
