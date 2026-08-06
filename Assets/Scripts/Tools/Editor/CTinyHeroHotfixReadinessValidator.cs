using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// HybridCLR 도입 준비 상태 검증 메뉴
    ///</summary>
    public static class CTinyHeroHotfixReadinessValidator
    {
        private const string MenuPath = "TinyHero/HybridCLR/Validate Hotfix Readiness";
        private const string HybridClrPackageName = "com.code-philosophy.hybridclr";
        private const string PackageManifestPath = "Packages/manifest.json";
        private const string HybridClrSettingsPath = "ProjectSettings/HybridCLRSettings.asset";
        private const string HotfixAssemblyName = "TinyHero.Hotfix";
        private const string HotfixContractsAssemblyName = "TinyHero.HotfixContracts";
        private const string HotfixScriptRootPath = "Assets/Scripts/Hotfix";
        private const string HotfixContractsRootPath = "Assets/Scripts/HotfixContracts";
        private const string HotfixPayloadRootPath = "Assets/Resources/Hotfix";
        private const string HotfixPayloadAssetPath = "Assets/Resources/Hotfix/TinyHero.Hotfix.dll.bytes";

        ///<summary>HybridCLR 도입 준비 상태 검증 실행</summary>
        [MenuItem( MenuPath )]
        public static void ValidateHotfixReadiness()
        {
            List<string> issueList = new List<string>();
            bool isPassed = ValidateHotfixReadiness( issueList );

            if ( isPassed )
            {
                Debug.Log( "[TinyHero HybridCLR] Hotfix readiness validation passed." );
                return;
            }

            for ( int index = 0; index < issueList.Count; index++ )
            {
                string issue = issueList[ index ];
                Debug.LogWarning( $"[TinyHero HybridCLR] {issue}" );
            }
        }

        ///<summary>HybridCLR 도입 준비 상태 검증 실행</summary>
        public static bool ValidateHotfixReadiness( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return false;
            }

            ValidateHybridClrPackage( _issueList );
            ValidateHybridClrInstallerState( _issueList );
            ValidateHybridClrSettings( _issueList );
            ValidateHotfixContractsAssemblyDefinition( _issueList );
            ValidateHotfixAssemblyDefinition( _issueList );
            ValidateHotfixAssemblyReferences( _issueList );
            ValidateHotfixScriptRoot( _issueList );
            ValidateHotfixContractsRoot( _issueList );
            ValidateHotfixPayloadRoot( _issueList );
            ValidateHotfixPayloadAsset( _issueList );
            bool result = _issueList.Count == 0;
            return result;
        }

        ///<summary>HybridCLR 패키지 등록 상태 검증</summary>
        private static void ValidateHybridClrPackage( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            string manifestFullPath = ResolveProjectPath( PackageManifestPath );

            if ( File.Exists( manifestFullPath ) == false )
            {
                _issueList.Add( $"Package manifest를 찾을 수 없습니다. Path: {PackageManifestPath}" );
                return;
            }

            string manifestText = File.ReadAllText( manifestFullPath );
            bool hasHybridClrPackage = manifestText.IndexOf( HybridClrPackageName, StringComparison.Ordinal ) >= 0;

            if ( hasHybridClrPackage == false )
            {
                _issueList.Add( $"HybridCLR 패키지가 아직 등록되지 않았습니다. Package: {HybridClrPackageName}" );
            }
        }

        ///<summary>HybridCLR Installer 초기화 상태 검증</summary>
        private static void ValidateHybridClrInstallerState( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            string installedVersionFullPath = Path.Combine( SettingsUtil.GeneratedCppDir, "libil2cpp-version.txt" );

            if ( File.Exists( installedVersionFullPath ) == false )
            {
                _issueList.Add( $"HybridCLR Installer 초기화가 아직 완료되지 않았습니다. Menu: HybridCLR/Installer..." );
            }
        }

        ///<summary>HybridCLR 설정 상태 검증</summary>
        private static void ValidateHybridClrSettings( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            string settingsFullPath = ResolveProjectPath( HybridClrSettingsPath );

            if ( File.Exists( settingsFullPath ) == false )
            {
                _issueList.Add( $"HybridCLR 설정 파일이 없습니다. Path: {HybridClrSettingsPath}" );
                return;
            }

            string settingsText = File.ReadAllText( settingsFullPath );
            bool hasHotfixAssembly = settingsText.IndexOf( HotfixAssemblyName, StringComparison.Ordinal ) >= 0;

            if ( hasHotfixAssembly == false )
            {
                _issueList.Add( $"HybridCLR hot update assembly에 {HotfixAssemblyName}가 등록되지 않았습니다." );
            }

            ValidateHybridClrAssemblyListDuplication( _issueList, settingsText );
        }

        ///<summary>HybridCLR Hotfix 어셈블리 중복 등록 상태 검증</summary>
        private static void ValidateHybridClrAssemblyListDuplication( List<string> _issueList, string _settingsText )
        {
            if ( _issueList == null || string.IsNullOrWhiteSpace( _settingsText ) )
            {
                return;
            }

            List<string> hotUpdateAssemblyList = ExtractYamlStringList( _settingsText, "hotUpdateAssemblies:" );
            List<string> preserveAssemblyList = ExtractYamlStringList( _settingsText, "preserveHotUpdateAssemblies:" );

            for ( int index = 0; index < preserveAssemblyList.Count; index++ )
            {
                string preserveAssemblyName = preserveAssemblyList[ index ];

                if ( hotUpdateAssemblyList.Contains( preserveAssemblyName ) == false )
                {
                    continue;
                }

                _issueList.Add( $"HybridCLR preserveHotUpdateAssemblies에 hotUpdateAssemblies와 중복된 어셈블리가 있습니다. Assembly: {preserveAssemblyName}" );
            }
        }

        ///<summary>YAML 문자열 목록 추출</summary>
        private static List<string> ExtractYamlStringList( string _settingsText, string _sectionHeader )
        {
            List<string> resultList = new List<string>();

            if ( string.IsNullOrWhiteSpace( _settingsText ) || string.IsNullOrWhiteSpace( _sectionHeader ) )
            {
                return resultList;
            }

            string[] lineArray = _settingsText.Split( new string[] { "\r\n", "\n" }, StringSplitOptions.None );
            bool isInSection = false;

            for ( int index = 0; index < lineArray.Length; index++ )
            {
                string line = lineArray[ index ];
                string trimmedLine = line.Trim();

                if ( string.Equals( trimmedLine, _sectionHeader, StringComparison.Ordinal ) )
                {
                    isInSection = true;
                    continue;
                }

                if ( isInSection == false )
                {
                    continue;
                }

                if ( trimmedLine.StartsWith( "- ", StringComparison.Ordinal ) )
                {
                    string value = trimmedLine.Substring( 2 ).Trim();

                    if ( string.IsNullOrWhiteSpace( value ) == false )
                    {
                        resultList.Add( value );
                    }

                    continue;
                }

                if ( trimmedLine.Length > 0 )
                {
                    break;
                }
            }

            return resultList;
        }

        ///<summary>Hotfix 계약 어셈블리 정의 상태 검증</summary>
        private static void ValidateHotfixContractsAssemblyDefinition( List<string> _issueList )
        {
            ValidateAssemblyDefinition( _issueList, HotfixContractsAssemblyName );
        }

        ///<summary>Hotfix 어셈블리 정의 상태 검증</summary>
        private static void ValidateHotfixAssemblyDefinition( List<string> _issueList )
        {
            ValidateAssemblyDefinition( _issueList, HotfixAssemblyName );
        }

        ///<summary>Hotfix 어셈블리 참조 상태 검증</summary>
        private static void ValidateHotfixAssemblyReferences( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            string hotfixAsmdefPath = FindAssemblyDefinitionPath( HotfixAssemblyName );

            if ( string.IsNullOrWhiteSpace( hotfixAsmdefPath ) )
            {
                return;
            }

            string asmdefFullPath = ResolveProjectPath( hotfixAsmdefPath );

            if ( File.Exists( asmdefFullPath ) == false )
            {
                _issueList.Add( $"Hotfix Assembly Definition 파일을 찾을 수 없습니다. Path: {hotfixAsmdefPath}" );
                return;
            }

            string asmdefText = File.ReadAllText( asmdefFullPath );
            string contractsAsmdefPath = FindAssemblyDefinitionPath( HotfixContractsAssemblyName );
            string contractsGuid = string.IsNullOrWhiteSpace( contractsAsmdefPath ) ? string.Empty : AssetDatabase.AssetPathToGUID( contractsAsmdefPath );
            string contractsGuidReference = string.IsNullOrWhiteSpace( contractsGuid ) ? string.Empty : $"GUID:{contractsGuid}";
            bool hasContractsReference = asmdefText.IndexOf( HotfixContractsAssemblyName, StringComparison.Ordinal ) >= 0
                || string.IsNullOrWhiteSpace( contractsGuidReference ) == false && asmdefText.IndexOf( contractsGuidReference, StringComparison.Ordinal ) >= 0;

            if ( hasContractsReference == false )
            {
                _issueList.Add( $"Hotfix Assembly가 계약 어셈블리를 참조하지 않습니다. Reference: {HotfixContractsAssemblyName}" );
            }
        }

        ///<summary>어셈블리 정의 존재 상태 검증</summary>
        private static void ValidateAssemblyDefinition( List<string> _issueList, string _assemblyName )
        {
            if ( _issueList == null )
            {
                return;
            }

            string asmdefPath = FindAssemblyDefinitionPath( _assemblyName );

            if ( string.IsNullOrWhiteSpace( asmdefPath ) )
            {
                _issueList.Add( $"Assembly Definition이 아직 없습니다. Expected: {_assemblyName}.asmdef" );
            }
        }

        ///<summary>어셈블리 정의 경로 반환</summary>
        private static string FindAssemblyDefinitionPath( string _assemblyName )
        {
            if ( string.IsNullOrWhiteSpace( _assemblyName ) )
            {
                return string.Empty;
            }

            string[] guidArray = AssetDatabase.FindAssets( $"{_assemblyName} t:AssemblyDefinitionAsset" );

            for ( int index = 0; index < guidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( guidArray[ index ] );
                string assetName = Path.GetFileNameWithoutExtension( assetPath );

                if ( string.Equals( assetName, _assemblyName, StringComparison.Ordinal ) )
                {
                    return assetPath;
                }
            }

            return string.Empty;
        }

        ///<summary>Hotfix 스크립트 루트 폴더 상태 검증</summary>
        private static void ValidateHotfixScriptRoot( List<string> _issueList )
        {
            ValidateFolder( _issueList, HotfixScriptRootPath, "Hotfix 스크립트 루트 폴더" );
        }

        ///<summary>Hotfix 계약 루트 폴더 상태 검증</summary>
        private static void ValidateHotfixContractsRoot( List<string> _issueList )
        {
            ValidateFolder( _issueList, HotfixContractsRootPath, "Hotfix 계약 루트 폴더" );
        }

        ///<summary>Hotfix 페이로드 루트 폴더 상태 검증</summary>
        private static void ValidateHotfixPayloadRoot( List<string> _issueList )
        {
            ValidateFolder( _issueList, HotfixPayloadRootPath, "Hotfix 페이로드 루트 폴더" );
        }

        ///<summary>Hotfix 페이로드 에셋 상태 검증</summary>
        private static void ValidateHotfixPayloadAsset( List<string> _issueList )
        {
            if ( _issueList == null )
            {
                return;
            }

            string payloadFullPath = ResolveProjectPath( HotfixPayloadAssetPath );

            if ( File.Exists( payloadFullPath ) == false )
            {
                _issueList.Add( $"Hotfix DLL 페이로드가 없습니다. Menu: TinyHero/HybridCLR/Sync Hotfix Payload" );
            }
        }

        ///<summary>에셋 폴더 존재 상태 검증</summary>
        private static void ValidateFolder( List<string> _issueList, string _folderPath, string _label )
        {
            if ( _issueList == null )
            {
                return;
            }

            if ( AssetDatabase.IsValidFolder( _folderPath ) )
            {
                return;
            }

            _issueList.Add( $"{_label}가 없습니다. Path: {_folderPath}" );
        }

        ///<summary>프로젝트 기준 전체 경로 반환</summary>
        private static string ResolveProjectPath( string _relativePath )
        {
            string projectRootPath = Directory.GetParent( Application.dataPath ).FullName;
            string result = Path.Combine( projectRootPath, _relativePath );
            return result;
        }
    }
}
