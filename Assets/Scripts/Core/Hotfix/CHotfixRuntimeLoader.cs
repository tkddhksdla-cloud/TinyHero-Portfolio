using System;
using System.Reflection;
using TinyHero.HotfixContracts;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// Hotfix DLL 런타임 로드 매니저
    ///</summary>
    public sealed class CHotfixRuntimeLoader : CSingleTon<CHotfixRuntimeLoader>
    {
        private const string HotfixAssemblyName = "TinyHero.Hotfix";
        private const string HotfixPayloadAddressableKey = "Hotfix/TinyHero.Hotfix.dll";
        private const string HotfixPayloadFallbackResourcePath = "Hotfix/TinyHero.Hotfix.dll";

        private Assembly loadedHotfixAssembly;
        private bool isLoadRequested;
        private bool isLoaded;

        ///<summary>
        /// Hotfix 로더 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            LoadHotfixAssemblyAsync();
        }

        ///<summary>Hotfix 로드 완료 여부 반환</summary>
        public bool IsLoaded()
        {
            bool result = isLoaded;
            return result;
        }

        ///<summary>Hotfix 어셈블리 반환</summary>
        public Assembly GetLoadedAssembly()
        {
            Assembly result = loadedHotfixAssembly;
            return result;
        }

        ///<summary>Hotfix 로드 요청</summary>
        public void LoadHotfixAssemblyAsync()
        {
            if ( isLoadRequested )
            {
                return;
            }

            isLoadRequested = true;
            Assembly existingAssembly = FindLoadedHotfixAssembly();

            if ( existingAssembly != null )
            {
                CompleteLoad( existingAssembly, "already loaded" );
                return;
            }

            CResourceManager.Instance.LoadAssetAsync<TextAsset>( HotfixPayloadAddressableKey, HotfixPayloadFallbackResourcePath, HandleHotfixPayloadLoaded );
        }

        ///<summary>Hotfix 실행 결과 반환</summary>
        public CHotfixExecutionResult ExecuteOrFallback( CHotfixExecutionContext _context )
        {
            CHotfixExecutionResult result = CHotfixModuleRegistry.ExecuteOrFallback( _context );
            return result;
        }

        ///<summary>Hotfix 페이로드 로드 완료 처리</summary>
        private void HandleHotfixPayloadLoaded( TextAsset _textAsset )
        {
            if ( _textAsset == null || _textAsset.bytes == null || _textAsset.bytes.Length == 0 )
            {
                Debug.LogWarning( $"[TinyHero Hotfix] Hotfix payload load failed. Key: {HotfixPayloadAddressableKey}" );
                return;
            }

            try
            {
                Assembly hotfixAssembly = Assembly.Load( _textAsset.bytes );
                CompleteLoad( hotfixAssembly, "payload" );
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"[TinyHero Hotfix] Hotfix assembly load failed. Message: {exception.Message}" );
            }
        }

        ///<summary>Hotfix 로드 완료 처리</summary>
        private void CompleteLoad( Assembly _assembly, string _source )
        {
            if ( _assembly == null )
            {
                return;
            }

            loadedHotfixAssembly = _assembly;
            isLoaded = true;
            CHotfixModuleRegistry.ClearCache();
            Debug.Log( $"[TinyHero Hotfix] Hotfix assembly ready. Source: {_source}, Assembly: {_assembly.GetName().Name}" );
        }

        ///<summary>이미 로드된 Hotfix 어셈블리 반환</summary>
        private Assembly FindLoadedHotfixAssembly()
        {
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();

            for ( int index = 0; index < assemblyArray.Length; index++ )
            {
                Assembly assembly = assemblyArray[ index ];

                if ( assembly == null )
                {
                    continue;
                }

                string assemblyName = assembly.GetName().Name;

                if ( string.Equals( assemblyName, HotfixAssemblyName, StringComparison.Ordinal ) )
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}
