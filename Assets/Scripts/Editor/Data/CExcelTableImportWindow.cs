using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    ///<summary>
    /// 엑셀 테이블 가져오기 창 에디터 창
    ///</summary>
    public sealed class CExcelTableImportWindow : EditorWindow
    {
        [Serializable]
        private sealed class CImportEntry
        {
            [SerializeField] private DefaultAsset sourceExcelFile;
            [SerializeField] private string worksheetName = string.Empty;
            [SerializeField] private CExcelTableDataBase targetTableData;
            [SerializeField] private bool isFoldout = true;

            ///<summary>
            /// 원본 엑셀 파일 반환
            ///</summary>
            public DefaultAsset GetSourceExcelFile()
            {
                DefaultAsset result = sourceExcelFile;
                return result;
            }

            ///<summary>
            /// 원본 엑셀 파일 설정
            ///</summary>
            public void SetSourceExcelFile(DefaultAsset _newSourceExcelFile)
            {
                sourceExcelFile = _newSourceExcelFile;
            }

            ///<summary>
            /// 워크시트 이름 반환
            ///</summary>
            public string GetWorksheetName()
            {
                string result = worksheetName;
                return result;
            }

            ///<summary>
            /// 워크시트 이름 설정
            ///</summary>
            public void SetWorksheetName(string _newWorksheetName)
            {
                worksheetName = _newWorksheetName;
            }

            ///<summary>
            /// 대상 테이블 데이터 반환
            ///</summary>
            public CExcelTableDataBase GetTargetTableData()
            {
                CExcelTableDataBase result = targetTableData;
                return result;
            }

            ///<summary>
            /// 대상 테이블 데이터 설정
            ///</summary>
            public void SetTargetTableData(CExcelTableDataBase _newTargetTableData)
            {
                targetTableData = _newTargetTableData;
            }

            ///<summary>
            /// 폴드아웃 상태 반환
            ///</summary>
            public bool GetIsFoldout()
            {
                bool result = isFoldout;
                return result;
            }

            ///<summary>
            /// 폴드아웃 상태 설정
            ///</summary>
            public void SetIsFoldout(bool _newIsFoldout)
            {
                isFoldout = _newIsFoldout;
            }
        }

        [SerializeField] private List<CImportEntry> importEntryList = new List<CImportEntry>();
        [SerializeField] private Vector2 scrollPosition;

        private SerializedObject serializedWindowObject;
        private SerializedProperty importEntryListProperty;

        ///<summary>
        /// 에디터 창 표시
        ///</summary>
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

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            serializedWindowObject = new SerializedObject( this );
            importEntryListProperty = serializedWindowObject.FindProperty( "importEntryList" );

            if ( importEntryList.Count == 0 )
            {
                AddEntry();
            }
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EnsureSerializedState();

            serializedWindowObject.Update();

            DrawToolbar();
            EditorGUILayout.Space();
            DrawEntryList();

            serializedWindowObject.ApplyModifiedProperties();
        }

        ///<summary>
        /// 직렬화 상태 보장
        ///</summary>
        private void EnsureSerializedState()
        {
            if ( serializedWindowObject != null && importEntryListProperty != null )
            {
                return;
            }

            serializedWindowObject = new SerializedObject( this );
            importEntryListProperty = serializedWindowObject.FindProperty( "importEntryList" );
        }

        ///<summary>
        /// 툴바 렌더링
        ///</summary>
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

        ///<summary>
        /// 항목 목록 렌더링
        ///</summary>
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

        ///<summary>
        /// 항목 렌더링
        ///</summary>
        private void DrawEntry(SerializedProperty _entryProperty, int _entryIndex)
        {
            SerializedProperty sourceExcelFileProperty = _entryProperty.FindPropertyRelative( "sourceExcelFile" );
            SerializedProperty worksheetNameProperty = _entryProperty.FindPropertyRelative( "worksheetName" );
            SerializedProperty targetTableDataProperty = _entryProperty.FindPropertyRelative( "targetTableData" );
            SerializedProperty isFoldoutProperty = _entryProperty.FindPropertyRelative( "isFoldout" );
            string title = BuildEntryTitle( sourceExcelFileProperty, targetTableDataProperty, _entryIndex );

            EditorGUILayout.BeginVertical( GUI.skin.box );
            isFoldoutProperty.boolValue = EditorGUILayout.Foldout( isFoldoutProperty.boolValue, title, true );

            if ( isFoldoutProperty.boolValue )
            {
                EditorGUILayout.Space();
                DrawSourceField( sourceExcelFileProperty );
                DrawWorksheetField( sourceExcelFileProperty, worksheetNameProperty );
                DrawTargetField( targetTableDataProperty );
                EditorGUILayout.Space();
                DrawEntryButtons( sourceExcelFileProperty, worksheetNameProperty, targetTableDataProperty, _entryIndex );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 원본 필드 렌더링
        ///</summary>
        private void DrawSourceField(SerializedProperty _sourceExcelFileProperty)
        {
            GUIContent label = new GUIContent( "Source Excel File" );
            EditorGUILayout.PropertyField( _sourceExcelFileProperty, label );
        }

        ///<summary>
        /// 워크시트 필드 렌더링
        ///</summary>
        private void DrawWorksheetField(SerializedProperty _sourceExcelFileProperty, SerializedProperty _worksheetNameProperty)
        {
            DefaultAsset sourceExcelFile = _sourceExcelFileProperty.objectReferenceValue as DefaultAsset;

            if ( sourceExcelFile == null )
            {
                GUIContent label = new GUIContent( "Worksheet Name" );
                EditorGUILayout.PropertyField( _worksheetNameProperty, label );
                return;
            }

            string[] worksheetNameArray = CExcelTableImporter.GetWorksheetNameArray( sourceExcelFile );

            if ( worksheetNameArray.Length == 0 )
            {
                GUIContent label = new GUIContent( "Worksheet Name" );
                EditorGUILayout.PropertyField( _worksheetNameProperty, label );
                return;
            }

            int selectedIndex = GetWorksheetIndex( worksheetNameArray, _worksheetNameProperty.stringValue );
            int newSelectedIndex = EditorGUILayout.Popup( "Worksheet Name", selectedIndex, worksheetNameArray );

            if ( newSelectedIndex < 0 || newSelectedIndex >= worksheetNameArray.Length )
            {
                return;
            }

            string selectedWorksheetName = worksheetNameArray[ newSelectedIndex ];
            _worksheetNameProperty.stringValue = selectedWorksheetName;
        }

        ///<summary>
        /// 대상 필드 렌더링
        ///</summary>
        private void DrawTargetField(SerializedProperty _targetTableDataProperty)
        {
            GUIContent label = new GUIContent( "Target Table Data" );
            EditorGUILayout.PropertyField( _targetTableDataProperty, label );
        }

        ///<summary>
        /// 항목 버튼 렌더링
        ///</summary>
        private void DrawEntryButtons(SerializedProperty _sourceExcelFileProperty, SerializedProperty _worksheetNameProperty, SerializedProperty _targetTableDataProperty, int _entryIndex)
        {
            DefaultAsset sourceExcelFile = _sourceExcelFileProperty.objectReferenceValue as DefaultAsset;
            string worksheetName = _worksheetNameProperty.stringValue;
            CExcelTableDataBase targetTableData = _targetTableDataProperty.objectReferenceValue as CExcelTableDataBase;

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
                RemoveEntry( _entryIndex );
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 항목 제목 구성
        ///</summary>
        private string BuildEntryTitle(SerializedProperty _sourceExcelFileProperty, SerializedProperty _targetTableDataProperty, int _entryIndex)
        {
            DefaultAsset sourceExcelFile = _sourceExcelFileProperty.objectReferenceValue as DefaultAsset;
            CExcelTableDataBase targetTableData = _targetTableDataProperty.objectReferenceValue as CExcelTableDataBase;
            string excelName = sourceExcelFile != null ? sourceExcelFile.name : "None";
            string tableName = targetTableData != null ? targetTableData.name : "None";
            string title = $"Entry {_entryIndex + 1} : {excelName} -> {tableName}";
            return title;
        }

        ///<summary>
        /// 워크시트 인덱스 반환
        ///</summary>
        private int GetWorksheetIndex(string[] _worksheetNameArray, string _worksheetName)
        {
            for ( int sheetIndex = 0; sheetIndex < _worksheetNameArray.Length; sheetIndex++ )
            {
                string currentWorksheetName = _worksheetNameArray[ sheetIndex ];
                bool isMatched = string.Equals( currentWorksheetName, _worksheetName, StringComparison.Ordinal );

                if ( isMatched )
                {
                    return sheetIndex;
                }
            }

            return 0;
        }

        ///<summary>
        /// 항목 추가
        ///</summary>
        private void AddEntry()
        {
            CImportEntry entry = new CImportEntry();
            importEntryList.Add( entry );
            SaveWindowState();
        }

        ///<summary>
        /// 항목 제거
        ///</summary>
        private void RemoveEntry(int _entryIndex)
        {
            bool isInRange = _entryIndex >= 0 && _entryIndex < importEntryList.Count;

            if ( isInRange == false )
            {
                return;
            }

            importEntryList.RemoveAt( _entryIndex );

            if ( importEntryList.Count == 0 )
            {
                CImportEntry entry = new CImportEntry();
                importEntryList.Add( entry );
            }

            SaveWindowState();
        }

        ///<summary>
        /// 전체 항목 가져오기
        ///</summary>
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

        ///<summary>
        /// 항목 가져오기
        ///</summary>
        private bool ImportEntry(DefaultAsset _sourceExcelFile, string _worksheetName, CExcelTableDataBase _targetTableData)
        {
            bool result = CExcelTableImporter.ImportTable( _sourceExcelFile, _worksheetName, _targetTableData );
            return result;
        }

        ///<summary>
        /// 창 상태 저장
        ///</summary>
        private void SaveWindowState()
        {
            EditorUtility.SetDirty( this );
        }
    }
}


