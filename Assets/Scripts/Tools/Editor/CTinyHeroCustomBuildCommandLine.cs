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

        ///<summary>
        /// 배치모드 Windows 플레이어 빌드 실행
        ///</summary>
        public static void BuildWindowsPlayer()
        {
            string outputPath = ResolveOutputPath();
            bool isBuilt = CTinyHeroCustomBuildPlayer.BuildWindowsPlayer( outputPath );

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
