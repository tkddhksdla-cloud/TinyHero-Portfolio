using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    /// <summary>
    /// 엑셀 파일과 테이블 자산 매칭을 한 화면에서 관리하는 가져오기 창이다.
    /// </summary>
    public sealed class CExcelTableImportWindow : EditorWindow
    {
        [Serializable]
        private sealed class CImportEntry
        {
            [SerializeField] private DefaultAsset sourceExcelFile;
            [SerializeField] private string worksheetName = string.Empty;
            [SerializeField] private CExcelTableDataBase targetTableData;
            [SerializeField] private bool isFoldout = true;

            /// <summary>
            /// 원본 엑셀 파일을 반환한다.
            /// </summary>
            public DefaultAsset GetSourceExcelFile()
            {
                DefaultAsset result = sourceExcelFile;
                return result;
            }

            /// <summary>
            /// 원본 엑셀 파일을 저장한다.
            /// </summary>
            public void SetSourceExcelFile( DefaultAsset newSourceExcelFile )
            {
                sourceExcelFile = newSourceExcelFile;
            }

            /// <summary>
            /// 워크시트 이름을 반환한다.
            /// </summary>
            public string GetWorksheetName()
            {
                string result = worksheetName;
                return result;
            }

            /// <summary>
            /// 워크시트 이름을 저장한다.
            /// </summary>
            public void SetWorksheetName( string newWorksheetName )
            {
                worksheetName = newWorksheetName;
            }

            /// <summary>
            /// 대상 테이블 자산을 반환한다.
            /// </summary>
            public CExcelTableDataBase GetTargetTableData()
            {
                CExcelTableDataBase result = targetTableData;
                return result;
            }

            /// <summary>
            /// 대상 테이블 자산을 저장한다.
            /// </summary>
            public void SetTargetTableData( CExcelTableDataBase newTargetTableData )
            {
                targetTableData = newTargetTableData;
            }

            /// <summary>
            /// 접힘 상태를 반환한다.
            /// </summary>
            public bool GetIsFoldout()
            {
                bool result = isFoldout;
                return result;
            }

            /// <summary>
            /// 접힘 상태를 저장한다.
            /// </summary>
            public void SetIsFoldout( bool newIsFoldout )
            {
                isFoldout = newIsFoldout;
            }
        }

        [SerializeField] private List<CImportEntry> importEntryList = new List<CImportEntry>();
        [SerializeField] private Vector2 scrollPosition;

        private SerializedObject serializedWindowObject;
        private SerializedProperty importEntryListProperty;

        /// <summary>
        /// 엑셀 테이블 가져오기 창을 연다.
        /// </summary>
        [MenuItem( "Tools/TinyHero/Data/Excel Table Import Window" )]
        private static void OpenWindow()
        {
            CExcelTableImportWindow window = GetWindow<CExcelTableImportWindow>();
            GUIContent titleContent = new GUIContent( "Excel Table Import" );
            Vector2 minSize = new Vector2( 720.0f, 420.0f );
            window.titleContent = titleContent;
            window.minSize = minSize;
            window.Show();
        }

        /// <summary>
        /// 직렬화 프로퍼티 캐시를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            serializedWindowObject = new SerializedObject( this );
            importEntryListProperty = serializedWindowObject.FindProperty( "importEntryList" );

            if ( importEntryList.Count == 0 )
            {
                AddEntry();
            }
        }

        /// <summary>
        /// 가져오기 창의 전체 UI를 그린다.
        /// </summary>
        private void OnGUI()
        {
            EnsureSerializedState();

            serializedWindowObject.Update();

            DrawToolbar();
            EditorGUILayout.Space();
            DrawEntryList();

            serializedWindowObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 직렬화 상태가 없으면 다시 연결한다.
        /// </summary>
        private void EnsureSerializedState()
        {
            if ( serializedWindowObject != null && importEntryListProperty != null )
            {
                return;
            }

            serializedWindowObject = new SerializedObject( this );
            importEntryListProperty = serializedWindowObject.FindProperty( "importEntryList" );
        }

        /// <summary>
        /// 상단 도구 버튼 영역을 그린다.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Add Entry", GUILayout.Height( 28.0f ) ) )
            {
                AddEntry();
            }

            if ( GUILayout.Button( "Import All", GUILayout.Height( 28.0f ) ) )
            {
                ImportAllEntries();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 등록된 매칭 항목 목록을 스크롤 영역에 그린다.
        /// </summary>
        private void DrawEntryList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition );

            for ( int entryIndex = 0; entryIndex < importEntryList.Count; entryIndex++ )
            {
                SerializedProperty entryProperty = importEntryListProperty.GetArrayElementAtIndex( entryIndex );
                DrawEntry( entryProperty, entryIndex );
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 개별 매칭 항목의 입력 UI를 그린다.
        /// </summary>
        private void DrawEntry( SerializedProperty entryProperty, int entryIndex )
        {
            SerializedProperty sourceExcelFileProperty = entryProperty.FindPropertyRelative( "sourceExcelFile" );
            SerializedProperty worksheetNameProperty = entryProperty.FindPropertyRelative( "worksheetName" );
            SerializedProperty targetTableDataProperty = entryProperty.FindPropertyRelative( "targetTableData" );
            SerializedProperty isFoldoutProperty = entryProperty.FindPropertyRelative( "isFoldout" );
            string title = BuildEntryTitle( sourceExcelFileProperty, targetTableDataProperty, entryIndex );

            EditorGUILayout.BeginVertical( GUI.skin.box );
            isFoldoutProperty.boolValue = EditorGUILayout.Foldout( isFoldoutProperty.boolValue, title, true );

            if ( isFoldoutProperty.boolValue )
            {
                EditorGUILayout.Space();
                DrawSourceField( sourceExcelFileProperty );
                DrawWorksheetField( sourceExcelFileProperty, worksheetNameProperty );
                DrawTargetField( targetTableDataProperty );
                EditorGUILayout.Space();
                DrawEntryButtons( sourceExcelFileProperty, worksheetNameProperty, targetTableDataProperty, entryIndex );
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 엑셀 파일 선택 필드를 그린다.
        /// </summary>
        private void DrawSourceField( SerializedProperty sourceExcelFileProperty )
        {
            GUIContent label = new GUIContent( "Source Excel File" );
            EditorGUILayout.PropertyField( sourceExcelFileProperty, label );
        }

        /// <summary>
        /// 워크시트 선택 필드를 그린다.
        /// </summary>
        private void DrawWorksheetField( SerializedProperty sourceExcelFileProperty, SerializedProperty worksheetNameProperty )
        {
            DefaultAsset sourceExcelFile = sourceExcelFileProperty.objectReferenceValue as DefaultAsset;

            if ( sourceExcelFile == null )
            {
                GUIContent label = new GUIContent( "Worksheet Name" );
                EditorGUILayout.PropertyField( worksheetNameProperty, label );
                return;
            }

            string[] worksheetNameArray = CExcelTableImporter.GetWorksheetNameArray( sourceExcelFile );

            if ( worksheetNameArray.Length == 0 )
            {
                GUIContent label = new GUIContent( "Worksheet Name" );
                EditorGUILayout.PropertyField( worksheetNameProperty, label );
                return;
            }

            int selectedIndex = GetWorksheetIndex( worksheetNameArray, worksheetNameProperty.stringValue );
            int newSelectedIndex = EditorGUILayout.Popup( "Worksheet Name", selectedIndex, worksheetNameArray );

            if ( newSelectedIndex < 0 || newSelectedIndex >= worksheetNameArray.Length )
            {
                return;
            }

            string selectedWorksheetName = worksheetNameArray[ newSelectedIndex ];
            worksheetNameProperty.stringValue = selectedWorksheetName;
        }

        /// <summary>
        /// 대상 테이블 자산 선택 필드를 그린다.
        /// </summary>
        private void DrawTargetField( SerializedProperty targetTableDataProperty )
        {
            GUIContent label = new GUIContent( "Target Table Data" );
            EditorGUILayout.PropertyField( targetTableDataProperty, label );
        }

        /// <summary>
        /// 항목별 실행 버튼을 그린다.
        /// </summary>
        private void DrawEntryButtons( SerializedProperty sourceExcelFileProperty, SerializedProperty worksheetNameProperty, SerializedProperty targetTableDataProperty, int entryIndex )
        {
            DefaultAsset sourceExcelFile = sourceExcelFileProperty.objectReferenceValue as DefaultAsset;
            string worksheetName = worksheetNameProperty.stringValue;
            CExcelTableDataBase targetTableData = targetTableDataProperty.objectReferenceValue as CExcelTableDataBase;

            EditorGUILayout.BeginHorizontal();

            bool canImport = sourceExcelFile != null && targetTableData != null;
            GUI.enabled = canImport;

            if ( GUILayout.Button( "Import", GUILayout.Height( 26.0f ) ) )
            {
                ImportEntry( sourceExcelFile, worksheetName, targetTableData );
            }

            GUI.enabled = true;

            if ( GUILayout.Button( "Select Table", GUILayout.Height( 26.0f ) ) )
            {
                Selection.activeObject = targetTableData;
            }

            if ( GUILayout.Button( "Remove", GUILayout.Height( 26.0f ) ) )
            {
                RemoveEntry( entryIndex );
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 매칭 항목 제목 문자열을 구성한다.
        /// </summary>
        private string BuildEntryTitle( SerializedProperty sourceExcelFileProperty, SerializedProperty targetTableDataProperty, int entryIndex )
        {
            DefaultAsset sourceExcelFile = sourceExcelFileProperty.objectReferenceValue as DefaultAsset;
            CExcelTableDataBase targetTableData = targetTableDataProperty.objectReferenceValue as CExcelTableDataBase;
            string excelName = sourceExcelFile != null ? sourceExcelFile.name : "None";
            string tableName = targetTableData != null ? targetTableData.name : "None";
            string title = $"Entry {entryIndex + 1} : {excelName} -> {tableName}";
            return title;
        }

        /// <summary>
        /// 워크시트 이름 배열에서 현재 선택 인덱스를 찾는다.
        /// </summary>
        private int GetWorksheetIndex( string[] worksheetNameArray, string worksheetName )
        {
            for ( int sheetIndex = 0; sheetIndex < worksheetNameArray.Length; sheetIndex++ )
            {
                string currentWorksheetName = worksheetNameArray[ sheetIndex ];
                bool isMatched = string.Equals( currentWorksheetName, worksheetName, StringComparison.Ordinal );

                if ( isMatched )
                {
                    return sheetIndex;
                }
            }

            return 0;
        }

        /// <summary>
        /// 새 매칭 항목을 목록 끝에 추가한다.
        /// </summary>
        private void AddEntry()
        {
            CImportEntry entry = new CImportEntry();
            importEntryList.Add( entry );
            SaveWindowState();
        }

        /// <summary>
        /// 지정된 인덱스의 매칭 항목을 제거한다.
        /// </summary>
        private void RemoveEntry( int entryIndex )
        {
            bool isInRange = entryIndex >= 0 && entryIndex < importEntryList.Count;

            if ( isInRange == false )
            {
                return;
            }

            importEntryList.RemoveAt( entryIndex );

            if ( importEntryList.Count == 0 )
            {
                CImportEntry entry = new CImportEntry();
                importEntryList.Add( entry );
            }

            SaveWindowState();
        }

        /// <summary>
        /// 현재 창의 모든 매칭 항목을 순차적으로 가져온다.
        /// </summary>
        private void ImportAllEntries()
        {
            int successCount = 0;

            for ( int entryIndex = 0; entryIndex < importEntryList.Count; entryIndex++ )
            {
                CImportEntry entry = importEntryList[ entryIndex ];

                if ( entry == null )
                {
                    continue;
                }

                DefaultAsset sourceExcelFile = entry.GetSourceExcelFile();
                string worksheetName = entry.GetWorksheetName();
                CExcelTableDataBase targetTableData = entry.GetTargetTableData();
                bool isSuccess = ImportEntry( sourceExcelFile, worksheetName, targetTableData );

                if ( isSuccess )
                {
                    successCount++;
                }
            }

            Debug.Log( $"Excel window import completed. Success: {successCount}, Total: {importEntryList.Count}." );
        }

        /// <summary>
        /// 단일 매칭 항목을 가져온다.
        /// </summary>
        private bool ImportEntry( DefaultAsset sourceExcelFile, string worksheetName, CExcelTableDataBase targetTableData )
        {
            bool result = CExcelTableImporter.ImportTable( sourceExcelFile, worksheetName, targetTableData );
            return result;
        }

        /// <summary>
        /// 창 상태를 저장 대상으로 표시한다.
        /// </summary>
        private void SaveWindowState()
        {
            EditorUtility.SetDirty( this );
        }
    }
}
