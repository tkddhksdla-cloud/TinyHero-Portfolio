using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 텍스트 테이블 키 변환 후보 데이터
    ///</summary>
    [Serializable]
    public sealed class TextTableMigrationCandidate
    {
        public bool isSelected = true;
        public string assetPath = string.Empty;
        public string assetName = string.Empty;
        public string propertyPath = string.Empty;
        public string originalText = string.Empty;
        public string textKey = string.Empty;
    }

    ///<summary>
    /// 한글 텍스트 테이블 키 변환 에디터 윈도우
    ///</summary>
    public sealed class TextTableMigrationWindow : EditorWindow
    {
        private const string MenuPath = "Tools/TinyHero/Data/Text Table Migration Window";
        private const string TextKeyPrefix = "KEY_TEXT_";
        private const string TextTableExcelAssetPath = "Assets/RawData/Excel/Text/TextTableData.xlsx";
        private const string TextTableWorksheetName = "TextTableData";
        private const string HeaderTextKey = "TextKey";
        private const string HeaderKr = "KR";
        private const string HeaderEn = "EN";
        private const float CandidateRowHeight = 118.0f;
        private const int MaxAutoKeySegmentLength = 32;
        private const string ExcelTempExtension = ".tmp";
        private const int MaxUniqueKeySuffix = 9999;
        private const string UnityCloneSuffix = "(Clone)";

        private static readonly Regex KoreanRegex = new Regex( "[가-힣]", RegexOptions.Compiled );
        private static readonly Regex InvalidKeyCharacterRegex = new Regex( "[^A-Z0-9_]", RegexOptions.Compiled );
        private static readonly string[] DefaultSearchRootPathArray =
        {
            "Assets/Resources/Data",
            "Assets/Data"
        };

        [SerializeField] private List<TextTableMigrationCandidate> candidateList = new List<TextTableMigrationCandidate>();
        [SerializeField] private string searchRootText = "Assets/Resources/Data;Assets/Data";
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private Vector2 candidateScrollPosition;

        private string statusMessage = "Scan 버튼으로 한글 텍스트 후보를 검색하세요.";
        private MessageType statusMessageType = MessageType.Info;

        ///<summary>
        /// 텍스트 테이블 변환 창 표시
        ///</summary>
        [MenuItem( MenuPath )]
        private static void ShowWindow()
        {
            TextTableMigrationWindow window = GetWindow<TextTableMigrationWindow>();
            window.titleContent = new GUIContent( "Text Table Migration" );
            window.minSize = new Vector2( 1180.0f, 720.0f );
            window.Show();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Text Table Migration", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "ScriptableObject 데이터의 한글 string 값을 TextKey로 치환하고, 원본 한글은 TextTableData.xlsx에 누적합니다.", MessageType.None );
            EditorGUILayout.Space();
            DrawToolbarSection();
            EditorGUILayout.Space();
            DrawCandidateSummarySection();
            EditorGUILayout.Space();
            DrawCandidateListSection();
            EditorGUILayout.Space();
            DrawActionSection();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.LabelField( "Search Roots", EditorStyles.boldLabel );
            searchRootText = EditorGUILayout.TextField( searchRootText );
            EditorGUILayout.HelpBox( "세미콜론으로 여러 경로를 구분합니다. 기본값: Assets/Resources/Data;Assets/Data", MessageType.None );

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Scan Korean Text", GUILayout.Height( 30.0f ), GUILayout.Width( 160.0f ) ) )
            {
                ScanKoreanTextCandidates();
            }

            if ( GUILayout.Button( "Select All", GUILayout.Height( 30.0f ), GUILayout.Width( 100.0f ) ) )
            {
                SetAllCandidateSelection( true );
            }

            if ( GUILayout.Button( "Deselect All", GUILayout.Height( 30.0f ), GUILayout.Width( 110.0f ) ) )
            {
                SetAllCandidateSelection( false );
            }

            if ( GUILayout.Button( "Normalize Keys", GUILayout.Height( 30.0f ), GUILayout.Width( 120.0f ) ) )
            {
                NormalizeCandidateKeys();
            }

            GUILayout.FlexibleSpace();
            searchText = EditorGUILayout.TextField( "Filter", searchText, GUILayout.Width( 360.0f ) );
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 후보 요약 영역 렌더링
        ///</summary>
        private void DrawCandidateSummarySection()
        {
            int selectedCount = CountSelectedCandidates();
            EditorGUILayout.LabelField( $"Candidates: {candidateList.Count} / Selected: {selectedCount}" );
            EditorGUILayout.LabelField( "Text Table", TextTableExcelAssetPath );
        }

        ///<summary>
        /// 후보 목록 영역 렌더링
        ///</summary>
        private void DrawCandidateListSection()
        {
            candidateScrollPosition = EditorGUILayout.BeginScrollView( candidateScrollPosition );

            for ( int index = 0; index < candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = candidateList[ index ];

                if ( IsMatchedFilter( candidate ) == false )
                {
                    continue;
                }

                DrawCandidateRow( candidate, index );
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        ///<summary>
        /// 후보 행 렌더링
        ///</summary>
        private void DrawCandidateRow( TextTableMigrationCandidate _candidate, int _index )
        {
            if ( _candidate == null )
            {
                return;
            }

            EditorGUILayout.BeginVertical( GUI.skin.box, GUILayout.MinHeight( CandidateRowHeight ) );
            EditorGUILayout.BeginHorizontal();
            _candidate.isSelected = EditorGUILayout.Toggle( _candidate.isSelected, GUILayout.Width( 22.0f ) );
            EditorGUILayout.LabelField( $"{_index + 1}. {_candidate.assetName}", EditorStyles.boldLabel, GUILayout.Width( 260.0f ) );
            EditorGUILayout.LabelField( _candidate.propertyPath );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField( "Asset", _candidate.assetPath );
            _candidate.textKey = EditorGUILayout.TextField( "TextKey", _candidate.textKey );
            EditorGUILayout.LabelField( "KR Original" );
            EditorGUILayout.SelectableLabel( _candidate.originalText, GUILayout.MinHeight( 36.0f ) );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 액션 영역 렌더링
        ///</summary>
        private void DrawActionSection()
        {
            EditorGUILayout.BeginHorizontal();

            using ( new EditorGUI.DisabledScope( candidateList.Count == 0 ) )
            {
                if ( GUILayout.Button( "Apply Selected To Assets And Excel", GUILayout.Height( 34.0f ) ) )
                {
                    ApplySelectedCandidates();
                }
            }

            if ( GUILayout.Button( "Ping Excel", GUILayout.Height( 34.0f ), GUILayout.Width( 110.0f ) ) )
            {
                PingTextTableExcel();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 한글 텍스트 후보 스캔
        ///</summary>
        private void ScanKoreanTextCandidates()
        {
            candidateList.Clear();
            string[] searchRootPathArray = ResolveSearchRootPathArray();
            HashSet<string> candidateIdentitySet = new HashSet<string>();
            HashSet<string> reservedTextKeySet = LoadExistingTextKeySet();
            int scannedAssetCount = 0;

            for ( int rootIndex = 0; rootIndex < searchRootPathArray.Length; rootIndex++ )
            {
                string searchRootPath = searchRootPathArray[ rootIndex ];

                if ( AssetDatabase.IsValidFolder( searchRootPath ) == false )
                {
                    continue;
                }

                string[] rootArray = new string[]
                {
                    searchRootPath
                };
                string[] guidArray = AssetDatabase.FindAssets( "t:ScriptableObject", rootArray );

                for ( int guidIndex = 0; guidIndex < guidArray.Length; guidIndex++ )
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath( guidArray[ guidIndex ] );
                    UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( assetPath );

                    if ( assetObject == null )
                    {
                        continue;
                    }

                    scannedAssetCount++;
                    AddAssetCandidates( assetObject, assetPath, candidateIdentitySet, reservedTextKeySet );
                }
            }

            SetStatus( $"스캔 완료. Asset: {scannedAssetCount}, Candidate: {candidateList.Count}", MessageType.Info );
        }

        ///<summary>
        /// 에셋 내 후보 추가
        ///</summary>
        private void AddAssetCandidates( UnityEngine.Object _assetObject, string _assetPath, HashSet<string> _candidateIdentitySet, HashSet<string> _reservedTextKeySet )
        {
            if ( _assetObject == null || string.IsNullOrWhiteSpace( _assetPath ) || _candidateIdentitySet == null )
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject( _assetObject );
            SerializedProperty property = serializedObject.GetIterator();
            bool shouldEnterChildren = true;

            while ( property.NextVisible( shouldEnterChildren ) )
            {
                shouldEnterChildren = true;

                if ( property.propertyType != SerializedPropertyType.String )
                {
                    continue;
                }

                string currentText = property.stringValue;

                if ( IsKoreanSourceText( currentText ) == false )
                {
                    continue;
                }

                string identity = $"{_assetPath}|{property.propertyPath}";

                if ( _candidateIdentitySet.Contains( identity ) )
                {
                    continue;
                }

                _candidateIdentitySet.Add( identity );
                TextTableMigrationCandidate candidate = new TextTableMigrationCandidate();
                string displayAssetName = NormalizeUnityObjectName( _assetObject.name );
                candidate.assetPath = _assetPath;
                candidate.assetName = displayAssetName;
                candidate.propertyPath = property.propertyPath;
                candidate.originalText = currentText;
                int candidateSequence = candidateList.Count + 1;
                candidate.textKey = BuildDefaultTextKey( displayAssetName, property.propertyPath, candidateSequence, _reservedTextKeySet );
                candidateList.Add( candidate );
            }
        }

        ///<summary>
        /// 선택 후보 일괄 적용
        ///</summary>
        private void ApplySelectedCandidates()
        {
            List<TextTableMigrationCandidate> selectedCandidateList = BuildSelectedCandidateList();

            if ( selectedCandidateList.Count == 0 )
            {
                SetStatus( "선택된 후보가 없습니다.", MessageType.Warning );
                return;
            }

            NormalizeCandidateKeys( selectedCandidateList );
            string validationMessage = ValidateCandidates( selectedCandidateList );

            if ( string.IsNullOrWhiteSpace( validationMessage ) == false )
            {
                SetStatus( validationMessage, MessageType.Error );
                return;
            }

            bool isConfirmed = EditorUtility.DisplayDialog( "Apply Text Table Migration", $"선택된 {selectedCandidateList.Count}개 항목을 TextKey로 치환하고 Excel에 추가합니다.", "Apply", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            HashSet<string> existingTextKeySet = LoadExistingTextKeySet();
            List<TextTableMigrationCandidate> appendCandidateList = BuildAppendCandidateList( selectedCandidateList, existingTextKeySet );
            bool didAppend = AppendCandidatesToExcel( appendCandidateList );

            if ( didAppend == false )
            {
                SetStatus( "TextTableData.xlsx 갱신에 실패했습니다.", MessageType.Error );
                return;
            }

            int replacedCount = ReplaceAssetTextValues( selectedCandidateList );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus( $"적용 완료. Replaced: {replacedCount}, Excel Added: {appendCandidateList.Count}", MessageType.Info );
            ScanKoreanTextCandidates();
        }

        ///<summary>
        /// 에셋 문자열 값 치환
        ///</summary>
        private int ReplaceAssetTextValues( List<TextTableMigrationCandidate> _candidateList )
        {
            int replacedCount = 0;

            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = _candidateList[ index ];
                UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( candidate.assetPath );

                if ( assetObject == null )
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject( assetObject );
                SerializedProperty property = serializedObject.FindProperty( candidate.propertyPath );

                if ( property == null || property.propertyType != SerializedPropertyType.String )
                {
                    continue;
                }

                if ( string.Equals( property.stringValue, candidate.originalText, StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                property.stringValue = candidate.textKey;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty( assetObject );
                replacedCount++;
            }

            return replacedCount;
        }

        ///<summary>
        /// 후보 목록 Excel 추가
        ///</summary>
        private bool AppendCandidatesToExcel( List<TextTableMigrationCandidate> _candidateList )
        {
            if ( _candidateList == null || _candidateList.Count == 0 )
            {
                EnsureTextTableExcelExists();
                return true;
            }

            try
            {
                RegisterCodePageProvider();
                EnsureTextTableExcelExists();
                string fullPath = Path.GetFullPath( TextTableExcelAssetPath );
                IWorkbook workbook = null;

                using ( FileStream readStream = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                {
                    workbook = WorkbookFactory.Create( readStream );
                }

                if ( workbook == null )
                {
                    return false;
                }

                ISheet sheet = ResolveTextTableSheet( workbook );
                EnsureHeaderRow( sheet );
                int nextRowIndex = ResolveNextRowIndex( sheet );

                for ( int index = 0; index < _candidateList.Count; index++ )
                {
                    TextTableMigrationCandidate candidate = _candidateList[ index ];
                    IRow row = sheet.CreateRow( nextRowIndex + index );
                    SetCellText( row, 0, candidate.textKey );
                    SetCellText( row, 1, candidate.originalText );
                    SetCellText( row, 2, candidate.textKey );
                }

                SaveWorkbookAtomically( workbook, fullPath );
                return true;
            }
            catch ( Exception exception )
            {
                Debug.LogError( $"TextTableData.xlsx append failed. {exception.Message}" );
                return false;
            }
        }

        ///<summary>
        /// 워크북 임시 저장 후 원본 교체
        ///</summary>
        private void SaveWorkbookAtomically( IWorkbook _workbook, string _fullPath )
        {
            string tempPath = $"{_fullPath}{ExcelTempExtension}";

            if ( File.Exists( tempPath ) )
            {
                File.Delete( tempPath );
            }

            using ( FileStream writeStream = new FileStream( tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None ) )
            {
                _workbook.Write( writeStream );
            }

            if ( File.Exists( _fullPath ) )
            {
                File.Delete( _fullPath );
            }

            File.Move( tempPath, _fullPath );
        }

        ///<summary>
        /// 텍스트 테이블 Excel 존재 보장
        ///</summary>
        private void EnsureTextTableExcelExists()
        {
            string directoryPath = Path.GetDirectoryName( TextTableExcelAssetPath );

            if ( string.IsNullOrWhiteSpace( directoryPath ) == false && Directory.Exists( directoryPath ) == false )
            {
                Directory.CreateDirectory( directoryPath );
            }

            if ( File.Exists( TextTableExcelAssetPath ) )
            {
                return;
            }

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet( TextTableWorksheetName );
            EnsureHeaderRow( sheet );

            using ( FileStream fileStream = new FileStream( TextTableExcelAssetPath, FileMode.Create, FileAccess.Write, FileShare.None ) )
            {
                workbook.Write( fileStream );
            }
        }

        ///<summary>
        /// 텍스트 테이블 시트 결정
        ///</summary>
        private ISheet ResolveTextTableSheet( IWorkbook _workbook )
        {
            ISheet sheet = _workbook.GetSheet( TextTableWorksheetName );

            if ( sheet != null )
            {
                return sheet;
            }

            ISheet result = _workbook.NumberOfSheets > 0 ? _workbook.GetSheetAt( 0 ) : _workbook.CreateSheet( TextTableWorksheetName );
            return result;
        }

        ///<summary>
        /// 헤더 행 존재 보장
        ///</summary>
        private void EnsureHeaderRow( ISheet _sheet )
        {
            if ( _sheet == null )
            {
                return;
            }

            IRow headerRow = _sheet.GetRow( 0 ) ?? _sheet.CreateRow( 0 );
            SetCellText( headerRow, 0, HeaderTextKey );
            SetCellText( headerRow, 1, HeaderKr );
            SetCellText( headerRow, 2, HeaderEn );
        }

        ///<summary>
        /// 문자열 셀 값 설정
        ///</summary>
        private void SetCellText( IRow _row, int _cellIndex, string _text )
        {
            ICell cell = _row.GetCell( _cellIndex ) ?? _row.CreateCell( _cellIndex );
            cell.SetCellType( CellType.String );
            cell.SetCellValue( _text ?? string.Empty );
        }

        ///<summary>
        /// 다음 추가 행 인덱스 반환
        ///</summary>
        private int ResolveNextRowIndex( ISheet _sheet )
        {
            int nextRowIndex = Mathf.Max( 1, _sheet.LastRowNum + 1 );
            return nextRowIndex;
        }

        ///<summary>
        /// 기존 텍스트 키 집합 로드
        ///</summary>
        private HashSet<string> LoadExistingTextKeySet()
        {
            HashSet<string> textKeySet = new HashSet<string>( StringComparer.Ordinal );

            if ( File.Exists( TextTableExcelAssetPath ) == false )
            {
                return textKeySet;
            }

            try
            {
                RegisterCodePageProvider();
                string fullPath = Path.GetFullPath( TextTableExcelAssetPath );

                using ( FileStream fileStream = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
                {
                    IWorkbook workbook = WorkbookFactory.Create( fileStream );
                    ISheet sheet = ResolveTextTableSheet( workbook );

                    for ( int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++ )
                    {
                        IRow row = sheet.GetRow( rowIndex );
                        string textKey = GetCellText( row, 0 );

                        if ( string.IsNullOrWhiteSpace( textKey ) )
                        {
                            continue;
                        }

                        textKeySet.Add( textKey.Trim() );
                    }
                }
            }
            catch ( Exception exception )
            {
                Debug.LogWarning( $"TextTableData.xlsx key scan failed. {exception.Message}" );
            }

            return textKeySet;
        }

        ///<summary>
        /// 셀 문자열 값 반환
        ///</summary>
        private string GetCellText( IRow _row, int _cellIndex )
        {
            if ( _row == null )
            {
                return string.Empty;
            }

            ICell cell = _row.GetCell( _cellIndex );

            if ( cell == null )
            {
                return string.Empty;
            }

            DataFormatter dataFormatter = new DataFormatter(System.Globalization.CultureInfo.InvariantCulture);
            string result = dataFormatter.FormatCellValue( cell );
            return result;
        }

        ///<summary>
        /// Excel 추가 대상 후보 목록 생성
        ///</summary>
        private List<TextTableMigrationCandidate> BuildAppendCandidateList( List<TextTableMigrationCandidate> _candidateList, HashSet<string> _existingTextKeySet )
        {
            List<TextTableMigrationCandidate> appendCandidateList = new List<TextTableMigrationCandidate>();

            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = _candidateList[ index ];

                if ( candidate == null || _existingTextKeySet.Contains( candidate.textKey ) )
                {
                    continue;
                }

                _existingTextKeySet.Add( candidate.textKey );
                appendCandidateList.Add( candidate );
            }

            return appendCandidateList;
        }

        ///<summary>
        /// 선택 후보 목록 생성
        ///</summary>
        private List<TextTableMigrationCandidate> BuildSelectedCandidateList()
        {
            List<TextTableMigrationCandidate> selectedCandidateList = new List<TextTableMigrationCandidate>();

            for ( int index = 0; index < candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = candidateList[ index ];

                if ( candidate == null || candidate.isSelected == false )
                {
                    continue;
                }

                selectedCandidateList.Add( candidate );
            }

            return selectedCandidateList;
        }

        ///<summary>
        /// 후보 검증 메시지 반환
        ///</summary>
        private string ValidateCandidates( List<TextTableMigrationCandidate> _candidateList )
        {
            HashSet<string> keySet = new HashSet<string>( StringComparer.Ordinal );

            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = _candidateList[ index ];

                if ( candidate == null )
                {
                    continue;
                }

                if ( IsValidTextKey( candidate.textKey ) == false )
                {
                    string result = $"유효하지 않은 TextKey입니다. Prefix '{TextKeyPrefix}'가 필요합니다: {candidate.textKey}";
                    return result;
                }

                if ( keySet.Contains( candidate.textKey ) )
                {
                    string result = $"선택 후보 안에 중복 TextKey가 있습니다: {candidate.textKey}";
                    return result;
                }

                keySet.Add( candidate.textKey );
            }

            return string.Empty;
        }

        ///<summary>
        /// 모든 후보 선택 상태 설정
        ///</summary>
        private void SetAllCandidateSelection( bool _isSelected )
        {
            for ( int index = 0; index < candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = candidateList[ index ];

                if ( candidate == null )
                {
                    continue;
                }

                candidate.isSelected = _isSelected;
            }
        }

        ///<summary>
        /// 전체 후보 키 정규화
        ///</summary>
        private void NormalizeCandidateKeys()
        {
            NormalizeCandidateKeys( candidateList );
        }

        ///<summary>
        /// 후보 목록 키 정규화
        ///</summary>
        private void NormalizeCandidateKeys( List<TextTableMigrationCandidate> _candidateList )
        {
            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = _candidateList[ index ];

                if ( candidate == null )
                {
                    continue;
                }

                candidate.textKey = NormalizeTextKey( candidate.textKey, candidate.assetName, candidate.propertyPath, index + 1 );
            }
        }

        ///<summary>
        /// 선택 후보 개수 반환
        ///</summary>
        private int CountSelectedCandidates()
        {
            int selectedCount = 0;

            for ( int index = 0; index < candidateList.Count; index++ )
            {
                TextTableMigrationCandidate candidate = candidateList[ index ];

                if ( candidate != null && candidate.isSelected )
                {
                    selectedCount++;
                }
            }

            return selectedCount;
        }

        ///<summary>
        /// 검색 필터 일치 여부 반환
        ///</summary>
        private bool IsMatchedFilter( TextTableMigrationCandidate _candidate )
        {
            if ( _candidate == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();

            if ( _candidate.assetPath.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0 )
            {
                return true;
            }

            if ( _candidate.propertyPath.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0 )
            {
                return true;
            }

            bool result = _candidate.originalText.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0 || _candidate.textKey.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return result;
        }

        ///<summary>
        /// 한글 원본 텍스트 여부 반환
        ///</summary>
        private bool IsKoreanSourceText( string _text )
        {
            if ( string.IsNullOrWhiteSpace( _text ) )
            {
                return false;
            }

            if ( _text.StartsWith( TextKeyPrefix, StringComparison.Ordinal ) )
            {
                return false;
            }

            bool result = KoreanRegex.IsMatch( _text );
            return result;
        }

        ///<summary>
        /// TextKey 유효 여부 반환
        ///</summary>
        private bool IsValidTextKey( string _textKey )
        {
            bool result = string.IsNullOrWhiteSpace( _textKey ) == false && _textKey.StartsWith( TextKeyPrefix, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 기본 TextKey 생성
        ///</summary>
        private string BuildDefaultTextKey( string _assetName, string _propertyPath, int _sequence, HashSet<string> _reservedTextKeySet )
        {
            string assetSegment = BuildShortKeySegment( _assetName );
            string propertySegment = BuildShortPropertySegment( _propertyPath );
            string rawKey = $"{assetSegment}_{propertySegment}";
            string normalizedKey = NormalizeTextKey( rawKey, _assetName, _propertyPath, _sequence );
            string result = BuildUniqueTextKey( normalizedKey, _reservedTextKeySet );
            return result;
        }

        ///<summary>
        /// 중복되지 않는 TextKey 생성
        ///</summary>
        private string BuildUniqueTextKey( string _baseTextKey, HashSet<string> _reservedTextKeySet )
        {
            string normalizedBaseTextKey = string.IsNullOrWhiteSpace( _baseTextKey ) ? $"{TextKeyPrefix}TEXT" : _baseTextKey.Trim();

            if ( _reservedTextKeySet == null )
            {
                return normalizedBaseTextKey;
            }

            if ( _reservedTextKeySet.Contains( normalizedBaseTextKey ) == false )
            {
                _reservedTextKeySet.Add( normalizedBaseTextKey );
                return normalizedBaseTextKey;
            }

            for ( int suffix = 2; suffix <= MaxUniqueKeySuffix; suffix++ )
            {
                string candidateTextKey = $"{normalizedBaseTextKey}_{suffix:00}";

                if ( _reservedTextKeySet.Contains( candidateTextKey ) )
                {
                    continue;
                }

                _reservedTextKeySet.Add( candidateTextKey );
                return candidateTextKey;
            }

            string fallbackTextKey = $"{normalizedBaseTextKey}_{_reservedTextKeySet.Count + 1}";
            _reservedTextKeySet.Add( fallbackTextKey );
            return fallbackTextKey;
        }

        ///<summary>
        /// 짧은 키 세그먼트 생성
        ///</summary>
        private string BuildShortKeySegment( string _sourceText )
        {
            if ( string.IsNullOrWhiteSpace( _sourceText ) )
            {
                return "TEXT";
            }

            string keySourceText = ConvertToKeySourceText( _sourceText.Trim() );
            string upperText = keySourceText.ToUpperInvariant();
            string sanitizedText = InvalidKeyCharacterRegex.Replace( upperText, "_" );
            string collapsedText = CollapseUnderscores( sanitizedText ).Trim( '_' );

            if ( collapsedText.Length <= MaxAutoKeySegmentLength )
            {
                string shortResult = string.IsNullOrWhiteSpace( collapsedText ) ? "TEXT" : collapsedText;
                return shortResult;
            }

            string result = collapsedText.Substring( 0, MaxAutoKeySegmentLength ).Trim( '_' );
            return result;
        }

        ///<summary>
        /// Unity 오브젝트 이름 표시용 정규화
        ///</summary>
        private string NormalizeUnityObjectName( string _objectName )
        {
            if ( string.IsNullOrWhiteSpace( _objectName ) )
            {
                return string.Empty;
            }

            string result = _objectName.Trim();

            while ( result.EndsWith( UnityCloneSuffix, StringComparison.Ordinal ) )
            {
                int cloneSuffixIndex = result.Length - UnityCloneSuffix.Length;
                result = result.Substring( 0, cloneSuffixIndex ).TrimEnd();
            }

            return result;
        }

        ///<summary>
        /// 프로퍼티 경로 기반 짧은 키 세그먼트 생성
        ///</summary>
        private string BuildShortPropertySegment( string _propertyPath )
        {
            if ( string.IsNullOrWhiteSpace( _propertyPath ) )
            {
                return "VALUE";
            }

            string[] pathPartArray = _propertyPath.Split( '.' );
            string semanticPathPart = string.Empty;
            int arrayIndex = -1;

            for ( int index = pathPartArray.Length - 1; index >= 0; index-- )
            {
                string pathPart = pathPartArray[ index ];

                if ( string.Equals( pathPart, "Array", StringComparison.Ordinal ) )
                {
                    continue;
                }

                if ( TryResolveArrayDataIndex( pathPart, out int resolvedArrayIndex ) )
                {
                    if ( arrayIndex < 0 )
                    {
                        arrayIndex = resolvedArrayIndex;
                    }

                    continue;
                }

                semanticPathPart = pathPart;
                break;
            }

            string result = BuildShortKeySegment( semanticPathPart );

            if ( string.IsNullOrWhiteSpace( result ) )
            {
                return "VALUE";
            }

            if ( arrayIndex >= 0 )
            {
                result = $"{result}_{arrayIndex + 1:00}";
            }

            return result;
        }

        ///<summary>
        /// 배열 데이터 인덱스 반환 여부
        ///</summary>
        private bool TryResolveArrayDataIndex( string _pathPart, out int _arrayIndex )
        {
            _arrayIndex = -1;

            if ( string.IsNullOrWhiteSpace( _pathPart ) )
            {
                return false;
            }

            if ( _pathPart.StartsWith( "data[", StringComparison.Ordinal ) == false || _pathPart.EndsWith( "]", StringComparison.Ordinal ) == false )
            {
                return false;
            }

            int indexTextLength = _pathPart.Length - 6;
            string indexText = _pathPart.Substring( 5, indexTextLength );
            bool result = int.TryParse( indexText, out _arrayIndex );
            return result;
        }

        ///<summary>
        /// 키 소스 문자열 변환
        ///</summary>
        private string ConvertToKeySourceText( string _sourceText )
        {
            if ( string.IsNullOrWhiteSpace( _sourceText ) )
            {
                return string.Empty;
            }

            string normalizedSourceText = NormalizeUnityObjectName( _sourceText );
            StringBuilder builder = new StringBuilder();

            for ( int index = 0; index < normalizedSourceText.Length; index++ )
            {
                char character = normalizedSourceText[ index ];

                if ( index > 0 && char.IsUpper( character ) )
                {
                    char previousCharacter = normalizedSourceText[ index - 1 ];

                    if ( char.IsLower( previousCharacter ) || char.IsDigit( previousCharacter ) )
                    {
                        builder.Append( '_' );
                    }
                }

                builder.Append( character );
            }

            string result = builder.ToString();
            return result;
        }

        ///<summary>
        /// TextKey 정규화
        ///</summary>
        private string NormalizeTextKey( string _textKey, string _assetName, string _propertyPath, int _sequence )
        {
            string sourceText = string.IsNullOrWhiteSpace( _textKey ) ? $"{_assetName}_{_propertyPath}_{_sequence}" : _textKey.Trim();

            if ( sourceText.StartsWith( TextKeyPrefix, StringComparison.Ordinal ) )
            {
                sourceText = sourceText.Substring( TextKeyPrefix.Length );
            }

            string keySourceText = ConvertToKeySourceText( sourceText );
            string upperText = keySourceText.ToUpperInvariant();
            string sanitizedText = InvalidKeyCharacterRegex.Replace( upperText, "_" );
            string collapsedText = CollapseUnderscores( sanitizedText ).Trim( '_' );

            if ( string.IsNullOrWhiteSpace( collapsedText ) )
            {
                collapsedText = "TEXT";
            }

            string result = $"{TextKeyPrefix}{collapsedText}";
            return result;
        }

        ///<summary>
        /// 연속 언더스코어 축약
        ///</summary>
        private string CollapseUnderscores( string _text )
        {
            if ( string.IsNullOrWhiteSpace( _text ) )
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            bool wasUnderscore = false;

            for ( int index = 0; index < _text.Length; index++ )
            {
                char character = _text[ index ];
                bool isUnderscore = character == '_';

                if ( isUnderscore && wasUnderscore )
                {
                    continue;
                }

                builder.Append( character );
                wasUnderscore = isUnderscore;
            }

            string result = builder.ToString();
            return result;
        }

        ///<summary>
        /// 검색 루트 경로 배열 결정
        ///</summary>
        private string[] ResolveSearchRootPathArray()
        {
            if ( string.IsNullOrWhiteSpace( searchRootText ) )
            {
                return DefaultSearchRootPathArray;
            }

            string[] splitArray = searchRootText.Split( ';' );
            List<string> rootPathList = new List<string>();

            for ( int index = 0; index < splitArray.Length; index++ )
            {
                string rootPath = splitArray[ index ];

                if ( string.IsNullOrWhiteSpace( rootPath ) )
                {
                    continue;
                }

                rootPathList.Add( rootPath.Trim().Replace( "\\", "/" ) );
            }

            string[] result = rootPathList.Count > 0 ? rootPathList.ToArray() : DefaultSearchRootPathArray;
            return result;
        }

        ///<summary>
        /// 텍스트 테이블 Excel 핑 처리
        ///</summary>
        private void PingTextTableExcel()
        {
            UnityEngine.Object excelAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( TextTableExcelAssetPath );

            if ( excelAsset == null )
            {
                SetStatus( $"Excel 파일을 찾지 못했습니다: {TextTableExcelAssetPath}", MessageType.Warning );
                return;
            }

            Selection.activeObject = excelAsset;
            EditorGUIUtility.PingObject( excelAsset );
        }

        ///<summary>
        /// 코드 페이지 공급자 등록
        ///</summary>
        private void RegisterCodePageProvider()
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus( string _message, MessageType _messageType )
        {
            statusMessage = _message;
            statusMessageType = _messageType;
        }
    }
}
