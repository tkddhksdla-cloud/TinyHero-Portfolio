using System;
using System.Collections.Generic;
using System.Reflection;
using TinyHero.HotfixContracts;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// Hotfix 모듈 검색 및 실행 레지스트리
    ///</summary>
    public static class CHotfixModuleRegistry
    {
        private static readonly List<IHotfixModule> cachedModuleList = new List<IHotfixModule>();
        private static bool isInitialized;

        ///<summary>Hotfix 모듈 실행 결과 반환</summary>
        public static CHotfixExecutionResult ExecuteOrFallback( CHotfixExecutionContext _context )
        {
            if ( _context == null )
            {
                CHotfixExecutionResult failedResult = CHotfixExecutionResult.CreateFailed( "Hotfix context is null." );
                return failedResult;
            }

            EnsureModuleCache();

            for ( int index = 0; index < cachedModuleList.Count; index++ )
            {
                IHotfixModule hotfixModule = cachedModuleList[ index ];

                if ( hotfixModule == null || hotfixModule.CanExecute( _context ) == false )
                {
                    continue;
                }

                try
                {
                    CHotfixExecutionResult executionResult = hotfixModule.Execute( _context );

                    if ( executionResult != null )
                    {
                        return executionResult;
                    }
                }
                catch ( Exception exception )
                {
                    Debug.LogWarning( $"[TinyHero Hotfix] Module execution failed. Module: {hotfixModule.GetModuleId()}, Message: {exception.Message}" );
                    CHotfixExecutionResult failedResult = CHotfixExecutionResult.CreateFailed( exception.Message );
                    return failedResult;
                }
            }

            CHotfixExecutionResult fallbackResult = CHotfixExecutionResult.CreateFallback( "No matched hotfix module." );
            return fallbackResult;
        }

        ///<summary>Hotfix 모듈 캐시 초기화</summary>
        public static void ClearCache()
        {
            cachedModuleList.Clear();
            isInitialized = false;
        }

        ///<summary>Hotfix 모듈 캐시 생성 보장</summary>
        private static void EnsureModuleCache()
        {
            if ( isInitialized )
            {
                return;
            }

            cachedModuleList.Clear();
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();

            for ( int assemblyIndex = 0; assemblyIndex < assemblyArray.Length; assemblyIndex++ )
            {
                Assembly assembly = assemblyArray[ assemblyIndex ];
                Type[] typeArray = GetTypesSafely( assembly );

                for ( int typeIndex = 0; typeIndex < typeArray.Length; typeIndex++ )
                {
                    Type type = typeArray[ typeIndex ];

                    if ( type == null || type.IsAbstract || type.IsInterface )
                    {
                        continue;
                    }

                    if ( typeof( IHotfixModule ).IsAssignableFrom( type ) == false )
                    {
                        continue;
                    }

                    IHotfixModule hotfixModule = CreateModuleInstance( type );

                    if ( hotfixModule == null )
                    {
                        continue;
                    }

                    cachedModuleList.Add( hotfixModule );
                }
            }

            isInitialized = true;
        }

        ///<summary>어셈블리 타입 목록 안전 반환</summary>
        private static Type[] GetTypesSafely( Assembly _assembly )
        {
            if ( _assembly == null )
            {
                return Array.Empty<Type>();
            }

            try
            {
                Type[] result = _assembly.GetTypes();
                return result;
            }
            catch ( ReflectionTypeLoadException exception )
            {
                Type[] result = exception.Types != null ? exception.Types : Array.Empty<Type>();
                return result;
            }
        }

        ///<summary>Hotfix 모듈 인스턴스 생성</summary>
        private static IHotfixModule CreateModuleInstance( Type _type )
        {
            ConstructorInfo constructorInfo = _type.GetConstructor( Type.EmptyTypes );

            if ( constructorInfo == null )
            {
                return null;
            }

            object instance = Activator.CreateInstance( _type );
            IHotfixModule result = instance as IHotfixModule;
            return result;
        }
    }
}
