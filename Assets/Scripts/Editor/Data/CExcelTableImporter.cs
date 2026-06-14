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
    ///<summary>
    /// 엑셀 테이블 가져오기 도구 클래스
    ///</summary>
    public static class CExcelTableImporter
    {
        private const int HeaderRowIndex = 0;
        private const int DataRowStartIndex = 1;

        ///<summary>
        /// 선택 프로필 목록 가져오기
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Import Selected Excel Profiles" )]
        private static void ImportSelectedProfiles()
        {
            CExcelImportProfile[] profileArray = GetSelectedProfiles();
            ImportProfiles( profileArray );
        }

        ///<summary>
        /// 가져오기 선택 프로필 목록 검증
        ///</summary>
        [MenuItem( "Tools/TinyHero/Data/Import Selected Excel Profiles", true )]
        private static bool ValidateImportSelectedProfiles()
        {
            CExcelImportProfile[] profileArray = GetSelectedProfiles();
            bool hasProfile = profileArray.Length > 0;
            return hasProfile;
        }

        ///<summary>
        /// 전체 프로필 목록 가져오기
        ///</summary>
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

        ///<summary>
        /// 테이블 데이터 가져오기
        ///</summary>
        public static bool ImportTable(DefaultAsset _sourceExcelFile, string _worksheetName, CExcelTableDataBase _targetTableData)
        {
            bool isValid = ValidateImportArguments( _sourceExcelFile, _targetTableData, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, _targetTableData );
                return false;
            }

            RegisterCodePageProvider();

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( _sourceExcelFile );
            string sourceExcelFullPath = Path.GetFullPath( sourceExcelAssetPath );

            try
            {
                using ( FileStream fileStream = new FileStream( sourceExcelFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                {
                    IWorkbook workbook = WorkbookFactory.Create( fileStream );
                    ISheet worksheet = ResolveWorksheet( workbook, _worksheetName );

                    if ( worksheet == null )
                    {
                        Debug.LogError( $"Worksheet was not found in {sourceExcelAssetPath}.", _targetTableData );
                        return false;
                    }

                    Dictionary<string, int> headerIndexDictionary = BuildHeaderIndexDictionary( worksheet );
                    Type rowType = _targetTableData.GetRowType();
                    IList rowList = BuildRowList( worksheet, rowType, headerIndexDictionary );
                    _targetTableData.ReplaceRowList( rowList );
                    _targetTableData.SetSourceExcelAssetPath( sourceExcelAssetPath );
                    EditorUtility.SetDirty( _targetTableData );
                    AssetDatabase.SaveAssets();
                    Debug.Log( $"Imported excel data from {sourceExcelAssetPath} to {_targetTableData.name}.", _targetTableData );
                    return true;
                }
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"Failed to import excel file {sourceExcelAssetPath}. {exception.Message}", _targetTableData );
                return false;
            }
        }

        ///<summary>
        /// 워크시트 이름 배열 반환
        ///</summary>
        public static string[] GetWorksheetNameArray(DefaultAsset _sourceExcelFile)
        {
            bool isValid = ValidateExcelFileOnly( _sourceExcelFile, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, _sourceExcelFile );
                string[] emptyArray = Array.Empty<string>();
                return emptyArray;
            }

            RegisterCodePageProvider();

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( _sourceExcelFile );
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
                Debug.LogError( $"Failed to read worksheet names from {sourceExcelAssetPath}. {exception.Message}", _sourceExcelFile );
            }

            string[] result = worksheetNameList.ToArray();
            return result;
        }

        ///<summary>
        /// 프로필 데이터 가져오기
        ///</summary>
        public static bool ImportProfile(CExcelImportProfile _profile)
        {
            bool isValid = ValidateProfile( _profile, out string validationMessage );

            if ( isValid == false )
            {
                Debug.LogError( validationMessage, _profile );
                return false;
            }

            DefaultAsset sourceExcelFile = _profile.GetSourceExcelFile();
            string worksheetName = _profile.GetWorksheetName();
            CExcelTableDataBase targetTableData = _profile.GetTargetTableData();
            bool result = ImportTable( sourceExcelFile, worksheetName, targetTableData );
            return result;
        }

        ///<summary>
        /// 선택 프로필 목록 반환
        ///</summary>
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

        ///<summary>
        /// 프로필 목록 가져오기
        ///</summary>
        private static void ImportProfiles(CExcelImportProfile[] _profileArray)
        {
            int successCount = 0;

            for ( int i = 0; i < _profileArray.Length; i++ )
            {
                CExcelImportProfile profile = _profileArray[ i ];
                bool isSuccess = ImportProfile( profile );

                if ( isSuccess )
                {
                    successCount++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log( $"Excel import completed. Success: {successCount}, Total: {_profileArray.Length}." );
        }

        ///<summary>
        /// 가져오기 인자 검증
        ///</summary>
        private static bool ValidateImportArguments(DefaultAsset _sourceExcelFile, CExcelTableDataBase _targetTableData, out string _validationMessage)
        {
            bool isExcelValid = ValidateExcelFileOnly( _sourceExcelFile, out _validationMessage );

            if ( isExcelValid == false )
            {
                return false;
            }

            if ( _targetTableData == null )
            {
                _validationMessage = "Target table asset is missing.";
                return false;
            }

            _validationMessage = string.Empty;
            return true;
        }

        ///<summary>
        /// 프로필 검증
        ///</summary>
        private static bool ValidateProfile(CExcelImportProfile _profile, out string _validationMessage)
        {
            if ( _profile == null )
            {
                _validationMessage = "Excel import _profile is null.";
                return false;
            }

            DefaultAsset sourceExcelFile = _profile.GetSourceExcelFile();
            CExcelTableDataBase targetTableData = _profile.GetTargetTableData();
            bool result = ValidateImportArguments( sourceExcelFile, targetTableData, out _validationMessage );
            return result;
        }

        ///<summary>
        /// 엑셀 파일 단독 검증
        ///</summary>
        private static bool ValidateExcelFileOnly(DefaultAsset _sourceExcelFile, out string _validationMessage)
        {
            if ( _sourceExcelFile == null )
            {
                _validationMessage = "Source excel file is missing.";
                return false;
            }

            string sourceExcelAssetPath = AssetDatabase.GetAssetPath( _sourceExcelFile );
            string extension = Path.GetExtension( sourceExcelAssetPath );
            bool isSupportedExtension = string.Equals( extension, ".xlsx", StringComparison.OrdinalIgnoreCase ) || string.Equals( extension, ".xls", StringComparison.OrdinalIgnoreCase );

            if ( isSupportedExtension == false )
            {
                _validationMessage = $"Unsupported excel extension on {sourceExcelAssetPath}.";
                return false;
            }

            _validationMessage = string.Empty;
            return true;
        }

        ///<summary>
        /// 코드 페이지 공급자 등록
        ///</summary>
        private static void RegisterCodePageProvider()
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
        }

        ///<summary>
        /// 워크시트 결정
        ///</summary>
        private static ISheet ResolveWorksheet(IWorkbook _workbook, string _worksheetName)
        {
            bool hasWorksheetName = string.IsNullOrWhiteSpace( _worksheetName ) == false;

            if ( hasWorksheetName )
            {
                ISheet namedWorksheet = _workbook.GetSheet( _worksheetName );
                return namedWorksheet;
            }

            ISheet firstWorksheet = _workbook.NumberOfSheets > 0 ? _workbook.GetSheetAt( 0 ) : null;
            return firstWorksheet;
        }

        ///<summary>
        /// 헤더 인덱스 사전 구성
        ///</summary>
        private static Dictionary<string, int> BuildHeaderIndexDictionary(ISheet _worksheet)
        {
            Dictionary<string, int> headerIndexDictionary = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
            IRow headerRow = _worksheet.GetRow( HeaderRowIndex );

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

        ///<summary>
        /// 행 목록 구성
        ///</summary>
        private static IList BuildRowList(ISheet _worksheet, Type _rowType, Dictionary<string, int> _headerIndexDictionary)
        {
            Type listType = typeof( List<> ).MakeGenericType( _rowType );
            IList rowList = ( IList )Activator.CreateInstance( listType );
            FieldInfo[] fieldInfoArray = GetRowFieldInfoArray( _rowType );

            for ( int rowIndex = DataRowStartIndex; rowIndex <= _worksheet.LastRowNum; rowIndex++ )
            {
                IRow excelRow = _worksheet.GetRow( rowIndex );

                if ( IsEmptyRow( excelRow ) )
                {
                    continue;
                }

                object rowData = Activator.CreateInstance( _rowType );

                for ( int fieldIndex = 0; fieldIndex < fieldInfoArray.Length; fieldIndex++ )
                {
                    FieldInfo fieldInfo = fieldInfoArray[ fieldIndex ];
                    string headerName = GetHeaderName( fieldInfo );
                    bool hasColumn = _headerIndexDictionary.TryGetValue( headerName, out int columnIndex );

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

        ///<summary>
        /// 행 필드 정보 배열 반환
        ///</summary>
        private static FieldInfo[] GetRowFieldInfoArray(Type _rowType)
        {
            FieldInfo[] allFieldInfoArray = _rowType.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
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

        ///<summary>
        /// 헤더 이름 반환
        ///</summary>
        private static string GetHeaderName(FieldInfo _fieldInfo)
        {
            Type headerAttributeType = typeof( CExcelHeaderAttribute );
            CExcelHeaderAttribute headerAttribute = Attribute.GetCustomAttribute( _fieldInfo, headerAttributeType ) as CExcelHeaderAttribute;

            if ( headerAttribute != null )
            {
                string attributedHeaderName = headerAttribute.GetHeaderName();
                return attributedHeaderName;
            }

            string normalizedFieldName = _fieldInfo.Name;

            if ( normalizedFieldName.StartsWith( "m_", StringComparison.Ordinal ) )
            {
                normalizedFieldName = normalizedFieldName.Substring( 2 );
            }

            string result = normalizedFieldName;
            return result;
        }

        ///<summary>
        /// 빈 행 여부
        ///</summary>
        private static bool IsEmptyRow(IRow _excelRow)
        {
            if ( _excelRow == null )
            {
                return true;
            }

            for ( int cellIndex = _excelRow.FirstCellNum; cellIndex < _excelRow.LastCellNum; cellIndex++ )
            {
                ICell cell = _excelRow.GetCell( cellIndex );
                string cellValue = GetCellStringValue( cell );

                if ( string.IsNullOrWhiteSpace( cellValue ) == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 셀 문자열 값 반환
        ///</summary>
        private static string GetCellStringValue(ICell _cell)
        {
            if ( _cell == null )
            {
                return string.Empty;
            }

            DataFormatter dataFormatter = new DataFormatter( CultureInfo.InvariantCulture );
            string result = dataFormatter.FormatCellValue( _cell );
            return result;
        }

        ///<summary>
        /// 셀 값 변환
        ///</summary>
        private static object ConvertCellValue(ICell _cell, Type _targetType, string _headerName, int _rowIndex)
        {
            Type resolvedType = Nullable.GetUnderlyingType( _targetType ) ?? _targetType;
            string rawCellValue = GetCellStringValue( _cell );
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
                throw new InvalidOperationException( $"Failed to parse header '{_headerName}' at excel row {_rowIndex + 1}. {exception.Message}" );
            }
        }

        ///<summary>
        /// 불리언 파싱
        ///</summary>
        private static bool ParseBoolean(string _cellStringValue)
        {
            if ( string.Equals( _cellStringValue, "1", StringComparison.OrdinalIgnoreCase ) )
            {
                return true;
            }

            if ( string.Equals( _cellStringValue, "0", StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            bool result = bool.Parse( _cellStringValue );
            return result;
        }

        ///<summary>
        /// 기본 값 반환
        ///</summary>
        private static object GetDefaultValue(Type _targetType)
        {
            if ( _targetType == typeof( string ) )
            {
                string emptyValue = string.Empty;
                return emptyValue;
            }

            object defaultValue = _targetType.IsValueType ? Activator.CreateInstance( _targetType ) : null;
            return defaultValue;
        }
    }
}


