using System;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// CI 배치모드 커스텀 빌드 실행 진입점
    ///</summary>
    public static class CTinyHeroCustomBuildCommandLine
    {
        private const string OutputPathArgumentName = "-tinyHeroBuildOutputPath";
        private const string LegacyOutputPathArgumentName = "-buildOutputPath";
        private const string GameVersionArgumentName = "-tinyHeroGameVersion";
        private const string ContentStatePathArgumentName = "-tinyHeroContentStatePath";

        ///<summary>
        /// 배치모드 Windows 플레이어 빌드 실행
        ///</summary>
        public static void BuildWindowsPlayer()
        {
            string outputPath = ResolveOutputPath();
            string[] argumentArray = Environment.GetCommandLineArgs();
            string gameVersion = FindArgumentValue( argumentArray, GameVersionArgumentName );
            bool isBuilt = CTinyHeroCustomBuildPlayer.BuildWindowsPlayer( outputPath, gameVersion );

            if ( Application.isBatchMode == false )
            {
                return;
            }

            int exitCode = isBuilt ? 0 : 1;
            EditorApplication.Exit( exitCode );
        }

        ///<summary>
        /// 배치모드 Android 플레이어 빌드 실행
        ///</summary>
        public static void BuildAndroidPlayer()
        {
            BuildMobilePlayer( true );
        }

        ///<summary>
        /// 배치모드 iOS 플레이어 빌드 실행
        ///</summary>
        public static void BuildIosPlayer()
        {
            BuildMobilePlayer( false );
        }

        ///<summary>
        /// 배치모드 Android 원격 콘텐츠 업데이트 빌드 실행
        ///</summary>
        public static void BuildAndroidContentUpdate()
        {
            BuildContentUpdate( BuildTarget.Android );
        }

        ///<summary>
        /// 배치모드 iOS 원격 콘텐츠 업데이트 빌드 실행
        ///</summary>
        public static void BuildIosContentUpdate()
        {
            BuildContentUpdate( BuildTarget.iOS );
        }

        ///<summary>
        /// 배치모드 모바일 플레이어 빌드 공통 실행
        ///</summary>
        private static void BuildMobilePlayer( bool _isAndroid )
        {
            string outputPath = ResolveOutputPath();
            string[] argumentArray = Environment.GetCommandLineArgs();
            string gameVersion = FindArgumentValue( argumentArray, GameVersionArgumentName );
            bool isBuilt = _isAndroid ? CTinyHeroCustomBuildPlayer.BuildAndroidPlayer( outputPath, gameVersion ) : CTinyHeroCustomBuildPlayer.BuildIosPlayer( outputPath, gameVersion );

            if ( Application.isBatchMode )
            {
                EditorApplication.Exit( isBuilt ? 0 : 1 );
            }
        }

        ///<summary>
        /// 배치모드 Windows 원격 콘텐츠 업데이트 빌드 실행
        ///</summary>
        public static void BuildWindowsContentUpdate()
        {
            BuildContentUpdate( BuildTarget.StandaloneWindows64 );
        }

        ///<summary>
        /// 플랫폼별 원격 콘텐츠 업데이트 빌드 공통 실행
        ///</summary>
        private static void BuildContentUpdate( BuildTarget _buildTarget )
        {
            string[] argumentArray = Environment.GetCommandLineArgs();
            string contentStatePath = FindArgumentValue( argumentArray, ContentStatePathArgumentName );
            bool isSwitched = EditorUserBuildSettings.SwitchActiveBuildTarget( BuildPipeline.GetBuildTargetGroup( _buildTarget ), _buildTarget );
            bool isBuilt = isSwitched && CTinyHeroRemoteContentBuildUtility.BuildRemoteContentUpdate( contentStatePath );

            if ( Application.isBatchMode == false )
            {
                return;
            }

            int exitCode = isBuilt ? 0 : 1;
            EditorApplication.Exit( exitCode );
        }

        ///<summary>
        /// 커맨드라인 출력 경로 인자 반환
        ///</summary>
        private static string ResolveOutputPath()
        {
            string[] argumentArray = Environment.GetCommandLineArgs();
            string outputPath = FindArgumentValue( argumentArray, OutputPathArgumentName );

            if ( string.IsNullOrWhiteSpace( outputPath ) == false )
            {
                string directResult = outputPath;
                return directResult;
            }

            string legacyOutputPath = FindArgumentValue( argumentArray, LegacyOutputPathArgumentName );
            string result = legacyOutputPath;
            return result;
        }

        ///<summary>
        /// 커맨드라인 인자 값 조회
        ///</summary>
        private static string FindArgumentValue( string[] _argumentArray, string _argumentName )
        {
            if ( _argumentArray == null || string.IsNullOrWhiteSpace( _argumentName ) )
            {
                string emptyResult = string.Empty;
                return emptyResult;
            }

            for ( int index = 0; index < _argumentArray.Length; index++ )
            {
                string argument = _argumentArray[ index ];

                if ( string.Equals( argument, _argumentName, StringComparison.OrdinalIgnoreCase ) == false )
                {
                    continue;
                }

                int valueIndex = index + 1;

                if ( valueIndex >= _argumentArray.Length )
                {
                    string emptyResult = string.Empty;
                    return emptyResult;
                }

                string value = _argumentArray[ valueIndex ];
                string foundResult = value;
                return foundResult;
            }

            string result = string.Empty;
            return result;
        }
    }
}
