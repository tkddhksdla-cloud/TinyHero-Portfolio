using System.Text;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    ///<summary>
    /// 플레이어 스탯 엑셀 유틸리티 클래스
    ///</summary>
    public static class CPlayerStatExcelUtility
    {
        private const string ExcelAssetPath = "Assets/RawData/Excel/Player/PlayerStatData.xlsx";
        private const string DefaultWorksheetName = "PlayerDefaultStats";
        private const string LevelWorksheetName = "PlayerStats";
        private const string DefaultTableDataAssetPath = "Assets/Resources/Data/Player/PlayerDefaultStatTableData.asset";
        private const string LevelTableDataAssetPath = "Assets/Resources/Data/Player/PlayerLevelStatTableData.asset";
        private const string DefaultImportProfileAssetPath = "Assets/Data/Player/PlayerDefaultStatImportProfile.asset";
        private const string LevelImportProfileAssetPath = "Assets/Data/Player/PlayerLevelStatImportProfile.asset";

        ///<summary>
        /// 플레이어 스탯 에셋 생성
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Prepare Player Stat Assets" )]
        public static void PrepareAssets()
        {
            EnsureFolderPath( "Assets/Data" );
            EnsureFolderPath( "Assets/Data/Player" );
            EnsureFolderPath( "Assets/Resources" );
            EnsureFolderPath( "Assets/Resources/Data" );
            EnsureFolderPath( "Assets/Resources/Data/Player" );

            CPlayerDefaultStatTableData defaultTableData = AssetDatabase.LoadAssetAtPath<CPlayerDefaultStatTableData>( DefaultTableDataAssetPath );

            if ( defaultTableData == null )
            {
                defaultTableData = ScriptableObject.CreateInstance<CPlayerDefaultStatTableData>();
                AssetDatabase.CreateAsset( defaultTableData, DefaultTableDataAssetPath );
            }

            CPlayerLevelStatTableData levelTableData = AssetDatabase.LoadAssetAtPath<CPlayerLevelStatTableData>( LevelTableDataAssetPath );

            if ( levelTableData == null )
            {
                levelTableData = ScriptableObject.CreateInstance<CPlayerLevelStatTableData>();
                AssetDatabase.CreateAsset( levelTableData, LevelTableDataAssetPath );
            }

            CExcelImportProfile defaultImportProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( DefaultImportProfileAssetPath );

            if ( defaultImportProfile == null )
            {
                defaultImportProfile = ScriptableObject.CreateInstance<CExcelImportProfile>();
                AssetDatabase.CreateAsset( defaultImportProfile, DefaultImportProfileAssetPath );
            }

            CExcelImportProfile levelImportProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( LevelImportProfileAssetPath );

            if ( levelImportProfile == null )
            {
                levelImportProfile = ScriptableObject.CreateInstance<CExcelImportProfile>();
                AssetDatabase.CreateAsset( levelImportProfile, LevelImportProfileAssetPath );
            }

            DefaultAsset sourceExcelFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>( ExcelAssetPath );
            ConfigureImportProfile( defaultImportProfile, sourceExcelFile, DefaultWorksheetName, defaultTableData );
            ConfigureImportProfile( levelImportProfile, sourceExcelFile, LevelWorksheetName, levelTableData );
            AssetDatabase.SaveAssets();
            Debug.Log( $"Prepared player stat assets at {DefaultTableDataAssetPath} and {LevelTableDataAssetPath}.", defaultTableData );
        }

        ///<summary>
        /// 플레이어 스탯 가져오기 검증
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Verify Player Stat Import" )]
        public static void VerifyImport()
        {
            PrepareAssets();
            CExcelImportProfile defaultImportProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( DefaultImportProfileAssetPath );
            CExcelImportProfile levelImportProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( LevelImportProfileAssetPath );

            if ( defaultImportProfile == null || levelImportProfile == null )
            {
                Debug.LogError( "Player stat import profile was not found." );
                return;
            }

            bool isDefaultImported = CExcelTableImporter.ImportProfile( defaultImportProfile );
            bool isLevelImported = CExcelTableImporter.ImportProfile( levelImportProfile );

            if ( isDefaultImported == false || isLevelImported == false )
            {
                Debug.LogError( "Player stat import failed." );
                return;
            }

            CPlayerDefaultStatTableData defaultTableData = AssetDatabase.LoadAssetAtPath<CPlayerDefaultStatTableData>( DefaultTableDataAssetPath );
            CPlayerLevelStatTableData levelTableData = AssetDatabase.LoadAssetAtPath<CPlayerLevelStatTableData>( LevelTableDataAssetPath );

            if ( defaultTableData == null || levelTableData == null )
            {
                Debug.LogError( "Player stat table data asset was not found after import." );
                return;
            }

            StringBuilder logBuilder = new StringBuilder();
            CPlayerDefaultStatRow defaultRow = defaultTableData.GetDefaultRow();
            logBuilder.AppendLine( $"Verified worksheets '{DefaultWorksheetName}' and '{LevelWorksheetName}' from '{ExcelAssetPath}'." );
            logBuilder.AppendLine( $"Default row exists: {defaultRow != null}" );

            if ( defaultRow != null )
            {
                logBuilder.AppendLine( $"DEFAULT | ATS={defaultRow.GetAts()} | MOV={defaultRow.GetMov()}" );
            }

            logBuilder.AppendLine( $"Imported level row count: {levelTableData.GetRowList().Count}" );
            AppendLevelRowSummary( logBuilder, levelTableData, 1 );
            AppendLevelRowSummary( logBuilder, levelTableData, 2 );
            AppendLevelRowSummary( logBuilder, levelTableData, 3 );
            Debug.Log( logBuilder.ToString(), levelTableData );
        }

        ///<summary>
        /// 가져오기 프로필 구성
        ///</summary>
        private static void ConfigureImportProfile( CExcelImportProfile _importProfile, DefaultAsset _sourceExcelFile, string _worksheetName, CExcelTableDataBase _tableData )
        {
            if ( _importProfile == null )
            {
                return;
            }

            SerializedObject serializedProfile = new SerializedObject( _importProfile );
            SerializedProperty sourceExcelFileProperty = serializedProfile.FindProperty( "sourceExcelFile" );
            SerializedProperty worksheetNameProperty = serializedProfile.FindProperty( "worksheetName" );
            SerializedProperty targetTableDataProperty = serializedProfile.FindProperty( "targetTableData" );
            sourceExcelFileProperty.objectReferenceValue = _sourceExcelFile;
            worksheetNameProperty.stringValue = _worksheetName;
            targetTableDataProperty.objectReferenceValue = _tableData;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( _importProfile );
        }

        ///<summary>
        /// 폴더 경로 생성 보장
        ///</summary>
        private static void EnsureFolderPath( string _folderPath )
        {
            bool isRootAssets = string.Equals( _folderPath, "Assets", System.StringComparison.OrdinalIgnoreCase );

            if ( isRootAssets )
            {
                return;
            }

            bool isFolderExists = AssetDatabase.IsValidFolder( _folderPath );

            if ( isFolderExists )
            {
                return;
            }

            int lastSlashIndex = _folderPath.LastIndexOf( '/' );
            string parentFolderPath = _folderPath.Substring( 0, lastSlashIndex );
            string folderName = _folderPath.Substring( lastSlashIndex + 1 );
            EnsureFolderPath( parentFolderPath );
            AssetDatabase.CreateFolder( parentFolderPath, folderName );
        }

        ///<summary>
        /// 특정 레벨 행 요약 추가
        ///</summary>
        private static void AppendLevelRowSummary( StringBuilder _logBuilder, CPlayerLevelStatTableData _tableData, int _level )
        {
            bool isFound = _tableData.TryGetRow( _level, out CPlayerLevelStatRow rowData );

            if ( isFound == false )
            {
                _logBuilder.AppendLine( $"Missing level row: {_level}" );
                return;
            }

            string rowSummary = $"LV={rowData.GetLv()} | NeedExp={rowData.GetNeedExp()} | HP={rowData.GetHp()} | MP={rowData.GetMp()} | ATK={rowData.GetAtk()} | DEF={rowData.GetDef()}";
            _logBuilder.AppendLine( rowSummary );
        }
    }
}
