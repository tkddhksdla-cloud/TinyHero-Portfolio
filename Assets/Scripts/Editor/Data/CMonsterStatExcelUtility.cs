using System.Text;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    ///<summary>
    /// 몬스터 스탯 엑셀 유틸리티 클래스
    ///</summary>
    public static class CMonsterStatExcelUtility
    {
        private const string ExcelAssetPath = "Assets/RawData/Excel/Monster/MonsterStatData.xlsx";
        private const string WorksheetName = "MonsterStats";
        private const string TableDataAssetPath = "Assets/Resources/Data/Monster/MonsterStatTableData.asset";
        private const string ImportProfileAssetPath = "Assets/Data/Monster/MonsterStatImportProfile.asset";

        ///<summary>
        /// 몬스터 스탯 에셋 생성
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Prepare Monster Stat Assets" )]
        public static void PrepareAssets()
        {
            EnsureFolderPath( "Assets/Data" );
            EnsureFolderPath( "Assets/Data/Monster" );
            EnsureFolderPath( "Assets/Resources" );
            EnsureFolderPath( "Assets/Resources/Data" );
            EnsureFolderPath( "Assets/Resources/Data/Monster" );

            CMonsterStatTableData tableData = AssetDatabase.LoadAssetAtPath<CMonsterStatTableData>( TableDataAssetPath );

            if ( tableData == null )
            {
                tableData = ScriptableObject.CreateInstance<CMonsterStatTableData>();
                AssetDatabase.CreateAsset( tableData, TableDataAssetPath );
            }

            CExcelImportProfile importProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( ImportProfileAssetPath );

            if ( importProfile == null )
            {
                importProfile = ScriptableObject.CreateInstance<CExcelImportProfile>();
                AssetDatabase.CreateAsset( importProfile, ImportProfileAssetPath );
            }

            DefaultAsset sourceExcelFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>( ExcelAssetPath );
            SerializedObject serializedProfile = new SerializedObject( importProfile );
            SerializedProperty sourceExcelFileProperty = serializedProfile.FindProperty( "sourceExcelFile" );
            SerializedProperty worksheetNameProperty = serializedProfile.FindProperty( "worksheetName" );
            SerializedProperty targetTableDataProperty = serializedProfile.FindProperty( "targetTableData" );
            sourceExcelFileProperty.objectReferenceValue = sourceExcelFile;
            worksheetNameProperty.stringValue = WorksheetName;
            targetTableDataProperty.objectReferenceValue = tableData;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( importProfile );
            AssetDatabase.SaveAssets();
            Debug.Log( $"Prepared monster stat assets at {TableDataAssetPath} and {ImportProfileAssetPath}.", importProfile );
        }

        ///<summary>
        /// 몬스터 스탯 가져오기 검증
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Verify Monster Stat Import" )]
        public static void VerifyImport()
        {
            PrepareAssets();
            CExcelImportProfile importProfile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( ImportProfileAssetPath );

            if ( importProfile == null )
            {
                Debug.LogError( "Monster stat import profile was not found." );
                return;
            }

            bool isImported = CExcelTableImporter.ImportProfile( importProfile );

            if ( isImported == false )
            {
                Debug.LogError( "Monster stat import failed.", importProfile );
                return;
            }

            CMonsterStatTableData tableData = AssetDatabase.LoadAssetAtPath<CMonsterStatTableData>( TableDataAssetPath );

            if ( tableData == null )
            {
                Debug.LogError( "Monster stat table data asset was not found after import." );
                return;
            }

            StringBuilder logBuilder = new StringBuilder();
            logBuilder.AppendLine( $"Verified worksheet '{WorksheetName}' from '{ExcelAssetPath}'." );
            logBuilder.AppendLine( $"Imported row count: {tableData.GetRowList().Count}" );
            AppendRowSummary( logBuilder, tableData, "Monster_0001" );
            AppendRowSummary( logBuilder, tableData, "Monster_0002" );
            AppendRowSummary( logBuilder, tableData, "Monster_0003" );
            AppendRowSummary( logBuilder, tableData, "Monster_0004" );
            Debug.Log( logBuilder.ToString(), tableData );
        }

        ///<summary>
        /// 폴더 경로 생성 보장
        ///</summary>
        private static void EnsureFolderPath(string _folderPath)
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
        /// 특정 행 요약 추가
        ///</summary>
        private static void AppendRowSummary(StringBuilder _logBuilder, CMonsterStatTableData _tableData, string _id)
        {
            bool isFound = _tableData.TryGetRow( _id, out CMonsterStatRow rowData );

            if ( isFound == false )
            {
                _logBuilder.AppendLine( $"Missing row: {_id}" );
                return;
            }

            string rowSummary = $"{rowData.GetId()} | NAME={rowData.GetName()} | HP={rowData.GetHp()} | LV={rowData.GetLv()} | ATK={rowData.GetAtk()} | DEF={rowData.GetDef()} | ATS={rowData.GetAts()} | MVS={rowData.GetMvs()} | EXP={rowData.GetExp()} | AT_Available={rowData.GetAtAvailable()}";
            _logBuilder.AppendLine( rowSummary );
        }
    }
}
