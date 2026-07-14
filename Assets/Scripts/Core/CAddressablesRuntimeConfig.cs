using System;
using System.IO;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// Addressables 원격 콘텐츠 엔드포인트 런타임 설정
    ///</summary>
    public static class CAddressablesRuntimeConfig
    {
        private const string ContentUrlArgumentName = "-tinyHeroContentUrl";
        private const string ContentUrlEnvironmentName = "TINYHERO_CONTENT_URL";
        private const string RequireRemoteContentEnvironmentName = "TINYHERO_REQUIRE_REMOTE_CONTENT";
        private const string ConfigFileName = "TinyHeroContentEndpoint.json";
        private const string DefaultRemoteBaseUrl = "http://127.0.0.1:8082/TinyHeroContent";

        private static string cachedRemoteBaseUrl;
        private static bool? cachedIsRemoteContentRequired;

        [Serializable]
        private sealed class CEndpointConfigData
        {
            public string remoteBaseUrl;
            public bool requireRemoteContent;
        }

        ///<summary>
        /// 원격 콘텐츠 기본 URL 반환
        ///</summary>
        public static string RemoteBaseUrl
        {
            get
            {
                if ( string.IsNullOrWhiteSpace( cachedRemoteBaseUrl ) )
                {
                    cachedRemoteBaseUrl = ResolveRemoteBaseUrl();
                }

                string result = cachedRemoteBaseUrl;
                return result;
            }
        }

        ///<summary>
        /// 원격 콘텐츠 필수 여부 반환
        ///</summary>
        public static bool IsRemoteContentRequired
        {
            get
            {
                if ( cachedIsRemoteContentRequired.HasValue == false )
                {
                    cachedIsRemoteContentRequired = ResolveIsRemoteContentRequired();
                }

                bool result = cachedIsRemoteContentRequired.Value;
                return result;
            }
        }

        ///<summary>
        /// 원격 콘텐츠 URL 결정
        ///</summary>
        private static string ResolveRemoteBaseUrl()
        {
            string commandLineUrl = FindCommandLineValue( Environment.GetCommandLineArgs(), ContentUrlArgumentName );

            if ( string.IsNullOrWhiteSpace( commandLineUrl ) == false )
            {
                string commandLineResult = NormalizeBaseUrl( commandLineUrl );
                return commandLineResult;
            }

            string environmentUrl = Environment.GetEnvironmentVariable( ContentUrlEnvironmentName );

            if ( string.IsNullOrWhiteSpace( environmentUrl ) == false )
            {
                string environmentResult = NormalizeBaseUrl( environmentUrl );
                return environmentResult;
            }

            string configUrl = LoadConfigUrl();

            if ( string.IsNullOrWhiteSpace( configUrl ) == false )
            {
                string configResult = NormalizeBaseUrl( configUrl );
                return configResult;
            }

            string result = DefaultRemoteBaseUrl;
            return result;
        }

        ///<summary>
        /// StreamingAssets 엔드포인트 설정 URL 반환
        ///</summary>
        private static string LoadConfigUrl()
        {
            try
            {
                string configPath = Path.Combine( Application.streamingAssetsPath, ConfigFileName );

                if ( File.Exists( configPath ) == false )
                {
                    return string.Empty;
                }

                string jsonText = File.ReadAllText( configPath );
                CEndpointConfigData configData = JsonUtility.FromJson<CEndpointConfigData>( jsonText );
                string result = configData != null ? configData.remoteBaseUrl : string.Empty;
                return result;
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[ Addressables ] Remote endpoint config load failed. {exception.Message}" );
                return string.Empty;
            }
        }

        ///<summary>
        /// 원격 콘텐츠 필수 정책 결정
        ///</summary>
        private static bool ResolveIsRemoteContentRequired()
        {
            string environmentValue = Environment.GetEnvironmentVariable( RequireRemoteContentEnvironmentName );

            if ( bool.TryParse( environmentValue, out bool environmentResult ) )
            {
                return environmentResult;
            }

            try
            {
                string configPath = Path.Combine( Application.streamingAssetsPath, ConfigFileName );

                if ( File.Exists( configPath ) == false )
                {
                    return false;
                }

                string jsonText = File.ReadAllText( configPath );
                CEndpointConfigData configData = JsonUtility.FromJson<CEndpointConfigData>( jsonText );
                bool result = configData != null && configData.requireRemoteContent;
                return result;
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[ Addressables ] Remote content policy load failed. {exception.Message}" );
                return false;
            }
        }

        ///<summary>
        /// 커맨드라인 인자 값 반환
        ///</summary>
        private static string FindCommandLineValue( string[] _argumentArray, string _argumentName )
        {
            if ( _argumentArray == null || string.IsNullOrWhiteSpace( _argumentName ) )
            {
                return string.Empty;
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
                    return string.Empty;
                }

                string result = _argumentArray[ valueIndex ];
                return result;
            }

            return string.Empty;
        }

        ///<summary>
        /// 기본 URL 형식 정규화
        ///</summary>
        private static string NormalizeBaseUrl( string _url )
        {
            string result = string.IsNullOrWhiteSpace( _url ) ? DefaultRemoteBaseUrl : _url.Trim().TrimEnd( '/', '\\' );
            return result;
        }
    }
}
