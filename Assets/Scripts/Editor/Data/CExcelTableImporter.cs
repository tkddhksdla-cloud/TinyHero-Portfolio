using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NPOI.SS.UserModel;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    /// <summary>
    /// NPOI를 사용해 헤더 기반 엑셀 데이터를 ScriptableObject로 가져오는 importer이다.
    /// </summary>
    public static class CExcelTableImporter
    {
        private const int HeaderRowIndex = 0;
        private const int DataRowStartIndex = 1;

        /// <summary>
        /// 선택된 import profile 자산을 모두 가져온다.
        /// </summary>
        [MenuItem( "Tools/TinyHero/Data/Import Selected Excel Profiles" )]
        private static void ImportSelectedProfiles()
        {
            CExcelImportProfile[] profileArray = GetSelectedProfiles();
            ImportProfiles( profileArray );
        }

        /// <summary>
        /// 선택된 import profile 자산이 있을 때만 메뉴를 활성화한다.
        /// </summary>
        [MenuItem( "Tools/TinyHero/Data/Import Selected Excel Profiles", true )]
        private static bool ValidateImportSelectedProfiles()
        {
            CExcelImportProfile[] profileArray = GetSelectedProfiles();
            bool hasProfile = profileArray.Length > 0;
            return hasProfile;
        }

        /// <summary>
        /// 프로젝트 내 모든 import profile 자산을 가져온다.
        /// </summary>
        [MenuItem( "Tools/TinyHero/Data/Import All Excel Profiles" )]
        private static void ImportAllProfiles()
        {
            string[] profileGuidArray = AssetDatabase.FindAssets( "t:CExcelImportProfile" );
            List<CExcelImportProfile> profileList = new List<CExcelImportProfile>();

            for ( int i = 0; i < profileGuidArray.Length; i++ )
            {
                string profileGuid = profileGuidArray[ i ];
                string profileAssetPath = AssetDatabase.GUIDToAssetPath( profileGuid );
                CExcelImportProfile profile = AssetDatabase.LoadAssetAtPath<CExcelImportProfile>( profileAssetPath );

                if ( profile == null )
                {
                    continue;
                }

                profileList.Add( profile );
            }

            CExcelImportProfile[] profileArray = profileList.ToArray();
            ImportProfiles( profileArray );
        }

        /// <summary>
        /// 지정된 엑셀 파일과 테이블 자산으로 데이터를 직접 가져온다.
        /// </summary>
        public static bool ImportTable( DefaultAsset sourceExcelFile, string worksheetName, CExcelTableDataBase targetTableData )
        {
            bool isValid = ValidateImportArguments( sourceExcelFile, targetTableData, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, targetTableData );
                return false;
            }

            RegisterCodePageProvider();

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( sourceExcelFile );
            string sourceExcelFullPath = Path.GetFullPath( sourceExcelAssetPath );

            try
            {
                using ( FileStream fileStream = new FileStream( sourceExcelFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                {
                    IWorkbook workbook = WorkbookFactory.Create( fileStream );
                    ISheet worksheet = ResolveWorksheet( workbook, worksheetName );

                    if ( worksheet == null )
                    {
                        Debug.LogError( $"Worksheet was not found in {sourceExcelAssetPath}.", targetTableData );
                        return false;
                    }

                    Dictionary<string, int> headerIndexDictionary = BuildHeaderIndexDictionary( worksheet );
                    Type rowType = targetTableData.GetRowType();
                    IList rowList = BuildRowList( worksheet, rowType, headerIndexDictionary );
                    targetTableData.ReplaceRowList( rowList );
                    targetTableData.SetSourceExcelAssetPath( sourceExcelAssetPath );
                    EditorUtility.SetDirty( targetTableData );
                    AssetDatabase.SaveAssets();
                    Debug.Log( $"Imported excel data from {sourceExcelAssetPath} to {targetTableData.name}.", targetTableData );
                    return true;
                }
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"Failed to import excel file {sourceExcelAssetPath}. {exception.Message}", targetTableData );
                return false;
            }
        }

        /// <summary>
        /// 지정된 엑셀 파일의 워크시트 이름 목록을 반환한다.
        /// </summary>
        public static string[] GetWorksheetNameArray( DefaultAsset sourceExcelFile )
        {
            bool isValid = ValidateExcelFileOnly( sourceExcelFile, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, sourceExcelFile );
                string[] emptyArray = Array.Empty<string>();
                return emptyArray;
            }

            RegisterCodePageProvider();

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( sourceExcelFile );
            string sourceExcelFullPath = Path.GetFullPath( sourceExcelAssetPath );
            List<string> worksheetNameList = new List<string>();

            try
            {
                using ( FileStream fileStream = new FileStream( sourceExcelFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                {
                    IWorkbook workbook = WorkbookFactory.Create( fileStream );

                    for ( int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++ )
                    {
                        string sheetName = workbook.GetSheetName( sheetIndex );

                        if ( string.IsNullOrWhiteSpace( sheetName ) )
                        {
                            continue;
                        }

                        worksheetNameList.Add( sheetName );
                    }
                }
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"Failed to read worksheet names from {sourceExcelAssetPath}. {exception.Message}", sourceExcelFile );
            }

            string[] result = worksheetNameList.ToArray();
            return result;
        }

        /// <summary>
        /// 지정된 import profile 하나를 가져온다.
        /// </summary>
        public static bool ImportProfile( CExcelImportProfile profile )
        {
            bool isValid = ValidateProfile( profile, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, profile );
                return false;
            }

            DefaultAsset sourceExcelFile = profile.GetSourceExcelFile();
            string worksheetName = profile.GetWorksheetName();
            CExcelTableDataBase targetTableData = profile.GetTargetTableData();
            bool result = ImportTable( sourceExcelFile, worksheetName, targetTableData );
            return result;
        }

        /// <summary>
        /// 선택된 오브젝트에서 import profile만 추려낸다.
        /// </summary>
        private static CExcelImportProfile[] GetSelectedProfiles()
        {
            UnityEngine.Object[] selectedObjectArray = Selection.objects;
            List<CExcelImportProfile> profileList = new List<CExcelImportProfile>();

            for ( int i = 0; i < selectedObjectArray.Length; i++ )
            {
                CExcelImportProfile profile = selectedObjectArray[ i ] as CExcelImportProfile;

                if ( profile == null )
                {
                    continue;
                }

                profileList.Add( profile );
            }

            CExcelImportProfile[] result = profileList.ToArray();
            return result;
        }

        /// <summary>
        /// 여러 import profile을 순차적으로 가져온다.
        /// </summary>
        private static void ImportProfiles( CExcelImportProfile[] profileArray )
        {
            int successCount = 0;

            for ( int i = 0; i < profileArray.Length; i++ )
            {
                CExcelImportProfile profile = profileArray[ i ];
                bool isSuccess = ImportProfile( profile );

                if ( isSuccess )
                {
                    successCount++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log( $"Excel import completed. Success: {successCount}, Total: {profileArray.Length}." );
        }

        /// <summary>
        /// 엑셀 파일과 대상 테이블 자산의 유효성을 검사한다.
        /// </summary>
        private static bool ValidateImportArguments( DefaultAsset sourceExcelFile, CExcelTableDataBase targetTableData, out string validationMessage )
        {
            bool isExcelValid = ValidateExcelFileOnly( sourceExcelFile, out validationMessage );

            if ( isExcelValid == false )
            {
                return false;
            }

            if ( targetTableData == null )
            {
                validationMessage = "Target table asset is missing.";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// import profile의 필요한 참조와 파일 확장자를 검사한다.
        /// </summary>
        private static bool ValidateProfile( CExcelImportProfile profile, out string validationMessage )
        {
            if ( profile == null )
            {
                validationMessage = "Excel import profile is null.";
                return false;
            }

            DefaultAsset sourceExcelFile = profile.GetSourceExcelFile();
            CExcelTableDataBase targetTableData = profile.GetTargetTableData();
            bool result = ValidateImportArguments( sourceExcelFile, targetTableData, out validationMessage );
            return result;
        }

        /// <summary>
        /// 엑셀 파일 참조와 확장자의 유효성을 검사한다.
        /// </summary>
        private static bool ValidateExcelFileOnly( DefaultAsset sourceExcelFile, out string validationMessage )
        {
            if ( sourceExcelFile == null )
            {
                validationMessage = "Source excel file is missing.";
                return false;
            }

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( sourceExcelFile );
            string extension = Path.GetExtension( sourceExcelAssetPath );
            bool isSupportedExtension = string.Equals( extension, ".xlsx", StringComparison.OrdinalIgnoreCase ) || string.Equals( extension, ".xls", StringComparison.OrdinalIgnoreCase );

            if ( isSupportedExtension == false )
            {
                validationMessage = $"Unsupported excel extension on {sourceExcelAssetPath}.";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 코드 페이지 인코딩 공급자를 등록한다.
        /// </summary>
        private static void RegisterCodePageProvider()
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
        }

        /// <summary>
        /// 지정된 이름 또는 첫 번째 워크시트를 반환한다.
        /// </summary>
        private static ISheet ResolveWorksheet( IWorkbook workbook, string worksheetName )
        {
            bool hasWorksheetName = string.IsNullOrWhiteSpace( worksheetName ) == false;

            if ( hasWorksheetName )
            {
                ISheet namedWorksheet = workbook.GetSheet( worksheetName );
                return namedWorksheet;
            }

            ISheet firstWorksheet = workbook.NumberOfSheets > 0 ? workbook.GetSheetAt( 0 ) : null;
            return firstWorksheet;
        }

        /// <summary>
        /// 헤더 row를 읽어 헤더명과 컬럼 인덱스 사전을 만든다.
        /// </summary>
        private static Dictionary<string, int> BuildHeaderIndexDictionary( ISheet worksheet )
        {
            Dictionary<string, int> headerIndexDictionary = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
            IRow headerRow = worksheet.GetRow( HeaderRowIndex );

            if ( headerRow == null )
            {
                return headerIndexDictionary;
            }

            for ( int cellIndex = headerRow.FirstCellNum; cellIndex < headerRow.LastCellNum; cellIndex++ )
            {
                ICell cell = headerRow.GetCell( cellIndex );
                string headerName = GetCellStringValue( cell );

                if ( string.IsNullOrWhiteSpace( headerName ) )
                {
                    continue;
                }

                if ( headerIndexDictionary.ContainsKey( headerName ) )
                {
                    continue;
                }

                headerIndexDictionary.Add( headerName, cellIndex );
            }

            return headerIndexDictionary;
        }

        /// <summary>
        /// row 타입에 맞는 객체 목록을 워크시트에서 생성한다.
        /// </summary>
        private static IList BuildRowList( ISheet worksheet, Type rowType, Dictionary<string, int> headerIndexDictionary )
        {
            Type listType = typeof( List<> ).MakeGenericType( rowType );
            IList rowList = ( IList )Activator.CreateInstance( listType );
            FieldInfo[] fieldInfoArray = GetRowFieldInfoArray( rowType );

            for ( int rowIndex = DataRowStartIndex; rowIndex <= worksheet.LastRowNum; rowIndex++ )
            {
                IRow excelRow = worksheet.GetRow( rowIndex );

                if ( IsEmptyRow( excelRow ) )
                {
                    continue;
                }

                object rowData = Activator.CreateInstance( rowType );

                for ( int fieldIndex = 0; fieldIndex < fieldInfoArray.Length; fieldIndex++ )
                {
                    FieldInfo fieldInfo = fieldInfoArray[ fieldIndex ];
                    string headerName = GetHeaderName( fieldInfo );
                    bool hasColumn = headerIndexDictionary.TryGetValue( headerName, out int columnIndex );

                    if ( hasColumn == false )
                    {
                        continue;
                    }

                    ICell cell = excelRow.GetCell( columnIndex );
                    Type fieldType = fieldInfo.FieldType;
                    object convertedValue = ConvertCellValue( cell, fieldType, headerName, rowIndex );
                    fieldInfo.SetValue( rowData, convertedValue );
                }

                rowList.Add( rowData );
            }

            return rowList;
        }

        /// <summary>
        /// row 타입에서 직렬화 가능한 필드 목록을 추린다.
        /// </summary>
        private static FieldInfo[] GetRowFieldInfoArray( Type rowType )
        {
            FieldInfo[] allFieldInfoArray = rowType.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            List<FieldInfo> fieldInfoList = new List<FieldInfo>();

            for ( int i = 0; i < allFieldInfoArray.Length; i++ )
            {
                FieldInfo fieldInfo = allFieldInfoArray[ i ];
                bool isPublicField = fieldInfo.IsPublic;
                bool hasSerializeField = Attribute.IsDefined( fieldInfo, typeof( SerializeField ) );

                if ( isPublicField == false && hasSerializeField == false )
                {
                    continue;
                }

                fieldInfoList.Add( fieldInfo );
            }

            FieldInfo[] result = fieldInfoList.ToArray();
            return result;
        }

        /// <summary>
        /// 필드에 대응되는 엑셀 헤더명을 반환한다.
        /// </summary>
        private static string GetHeaderName( FieldInfo fieldInfo )
        {
            Type headerAttributeType = typeof( CExcelHeaderAttribute );
            CExcelHeaderAttribute headerAttribute = Attribute.GetCustomAttribute( fieldInfo, headerAttributeType ) as CExcelHeaderAttribute;

            if ( headerAttribute != null )
            {
                string attributedHeaderName = headerAttribute.GetHeaderName();
                return attributedHeaderName;
            }

            string normalizedFieldName = fieldInfo.Name;

            if ( normalizedFieldName.StartsWith( "m_", StringComparison.Ordinal ) )
            {
                normalizedFieldName = normalizedFieldName.Substring( 2 );
            }

            string result = normalizedFieldName;
            return result;
        }

        /// <summary>
        /// 비어 있는 엑셀 row인지 검사한다.
        /// </summary>
        private static bool IsEmptyRow( IRow excelRow )
        {
            if ( excelRow == null )
            {
                return true;
            }

            for ( int cellIndex = excelRow.FirstCellNum; cellIndex < excelRow.LastCellNum; cellIndex++ )
            {
                ICell cell = excelRow.GetCell( cellIndex );
                string cellValue = GetCellStringValue( cell );

                if ( string.IsNullOrWhiteSpace( cellValue ) == false )
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 셀 값을 문자열 기준으로 읽는다.
        /// </summary>
        private static string GetCellStringValue( ICell cell )
        {
            if ( cell == null )
            {
                return string.Empty;
            }

            DataFormatter dataFormatter = new DataFormatter( CultureInfo.InvariantCulture );
            string result = dataFormatter.FormatCellValue( cell );
            return result;
        }

        /// <summary>
        /// 셀 값을 대상 필드 타입에 맞게 변환한다.
        /// </summary>
        private static object ConvertCellValue( ICell cell, Type targetType, string headerName, int rowIndex )
        {
            Type resolvedType = Nullable.GetUnderlyingType( targetType ) ?? targetType;
            string rawCellValue = GetCellStringValue( cell );
            string cellStringValue = rawCellValue.Trim();

            if ( string.IsNullOrWhiteSpace( cellStringValue ) )
            {
                object defaultValue = GetDefaultValue( resolvedType );
                return defaultValue;
            }

            try
            {
                if ( resolvedType == typeof( string ) )
                {
                    string result = cellStringValue;
                    return result;
                }

                if ( resolvedType == typeof( int ) )
                {
                    int result = int.Parse( cellStringValue, NumberStyles.Any, CultureInfo.InvariantCulture );
                    return result;
                }

                if ( resolvedType == typeof( long ) )
                {
                    long result = long.Parse( cellStringValue, NumberStyles.Any, CultureInfo.InvariantCulture );
                    return result;
                }

                if ( resolvedType == typeof( float ) )
                {
                    float result = float.Parse( cellStringValue, NumberStyles.Any, CultureInfo.InvariantCulture );
                    return result;
                }

                if ( resolvedType == typeof( double ) )
                {
                    double result = double.Parse( cellStringValue, NumberStyles.Any, CultureInfo.InvariantCulture );
                    return result;
                }

                if ( resolvedType == typeof( bool ) )
                {
                    bool result = ParseBoolean( cellStringValue );
                    return result;
                }

                if ( resolvedType.IsEnum )
                {
                    object enumValue = Enum.Parse( resolvedType, cellStringValue, true );
                    return enumValue;
                }

                object convertedValue = Convert.ChangeType( cellStringValue, resolvedType, CultureInfo.InvariantCulture );
                return convertedValue;
            }
            catch ( Exception exception )
            {
                throw new InvalidOperationException( $"Failed to parse header '{headerName}' at excel row {rowIndex + 1}. {exception.Message}" );
            }
        }

        /// <summary>
        /// 문자열을 bool 값으로 변환한다.
        /// </summary>
        private static bool ParseBoolean( string cellStringValue )
        {
            if ( string.Equals( cellStringValue, "1", StringComparison.OrdinalIgnoreCase ) )
            {
                return true;
            }

            if ( string.Equals( cellStringValue, "0", StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            bool result = bool.Parse( cellStringValue );
            return result;
        }

        /// <summary>
        /// 타입에 맞는 기본값을 반환한다.
        /// </summary>
        private static object GetDefaultValue( Type targetType )
        {
            if ( targetType == typeof( string ) )
            {
                string emptyValue = string.Empty;
                return emptyValue;
            }

            object defaultValue = targetType.IsValueType ? Activator.CreateInstance( targetType ) : null;
            return defaultValue;
        }
    }
}
