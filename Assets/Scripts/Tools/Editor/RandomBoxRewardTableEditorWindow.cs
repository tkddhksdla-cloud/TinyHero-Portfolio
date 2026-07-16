using System;
using System.Collections.Generic;
using System.IO;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 랜덤상자 보상 테이블 목록 정보
    ///</summary>
    [Serializable]
    public sealed class RandomBoxRewardTableInfo
    {
        public string tableName;
        public string assetPath;
        public int entryCount;
        public int validEntryCount;
        public float totalWeight;
    }

    ///<summary>
    /// 랜덤상자 아이템 선택 정보
    ///</summary>
    [Serializable]
    public sealed class RandomBoxItemReferenceInfo
    {
        public string itemId;
        public string itemName;
        public string assetPath;
        public CItemDefinition itemDefinition;
    }

    ///<summary>
    /// 랜덤상자 보상 테이블 에디터 윈도우
    ///</summary>
    public sealed class RandomBoxRewardTableEditorWindow : CEditorToolWindowBase<RandomBoxRewardTableInfo>
    {
        private const string RandomBoxTableFolderPath = "Assets/Resources/Data/Item/RandomBoxes";
        private const string ItemDefinitionFolderPath = "Assets/Resources/Data/Item/Definitions";
        private const float ListPanelWidth = 420.0f;
        private const float ListViewHeight = 500.0f;
        private const float ListItemHeight = 44.0f;
        private const float ListItemSpacing = 4.0f;
        private const int MaxItemSearchResultCount = 12;

        [SerializeField] private List<RandomBoxRewardTableInfo> rewardTableInfoList = new List<RandomBoxRewardTableInfo>();
        [SerializeField] private List<RandomBoxItemReferenceInfo> itemReferenceInfoList = new List<RandomBoxItemReferenceInfo>();
        [SerializeField] private int selectedTableIndex = -1;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string newTableAssetName = "RandomBoxRewardTable";
        [SerializeField] private string currentTableAssetNameDraft = string.Empty;
        [SerializeField] private string currentTableAssetPathDraft = string.Empty;
        [SerializeField] private string itemSearchText = string.Empty;
        [SerializeField] private int selectedItemSearchEntryIndex = -1;

        private Vector2 tableListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "랜덤상자 보상 테이블을 선택하세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private bool hasPendingAssetChanges;

        ///<summary>
        /// 랜덤상자 에디터 윈도우 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Random Box Reward Table Editor" )]
        private static void ShowWindow()
        {
            RandomBoxRewardTableEditorWindow window = GetWindow<RandomBoxRewardTableEditorWindow>();
            window.titleContent = new GUIContent( "Random Box Editor" );
            window.minSize = new Vector2( 1180.0f, 780.0f );
            window.Show();
        }

        ///<summary>
        /// 랜덤상자 에디터 창 열기
        ///</summary>
        public static void OpenWindow()
        {
            ShowWindow();
        }

        ///<summary>
        /// 편집 창 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshAll();
        }

        ///<summary>
        /// 편집 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            DrawWindowHeader( "Random Box Reward Table Editor", "랜덤상자 보상 테이블을 검색, 생성, 복제, 삭제하고 등장 아이템과 가중치, 수량 범위를 편집합니다." );
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawTableListSection();
            DrawTableEditorSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical( "box", GUILayout.MinHeight( 96.0f ) );
            EditorGUILayout.LabelField( "Browser", EditorStyles.boldLabel );
            string updatedSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( updatedSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = updatedSearchText;
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 110.0f ) ) )
            {
                RefreshAll();
            }

            if ( GUILayout.Button( "Ping Selected", GUILayout.Width( 120.0f ) ) )
            {
                PingSelectedTable();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox( $"Loaded Tables {rewardTableInfoList.Count} / Items {itemReferenceInfoList.Count}", MessageType.None );
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical( "box", GUILayout.MinHeight( 96.0f ) );
            EditorGUILayout.LabelField( "Create Table", EditorStyles.boldLabel );
            newTableAssetName = EditorGUILayout.TextField( "Asset Name", newTableAssetName );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Reset Name", GUILayout.Width( 120.0f ) ) )
            {
                ResetNewTableAssetName();
            }

            if ( GUILayout.Button( "Create", GUILayout.Width( 110.0f ) ) )
            {
                CreateRewardTable();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 테이블 목록 영역 렌더링
        ///</summary>
        private void DrawTableListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( ListPanelWidth ) );
            EditorGUILayout.LabelField( "Reward Tables", EditorStyles.boldLabel );
            List<RandomBoxRewardTableInfo> filteredInfoList = GetFilteredRewardTableInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfoList.Count}개", MessageType.None );
            tableListScrollPosition = EditorGUILayout.BeginScrollView( tableListScrollPosition, GUILayout.Height( ListViewHeight ) );

            for ( int index = 0; index < filteredInfoList.Count; index++ )
            {
                RandomBoxRewardTableInfo rewardTableInfo = filteredInfoList[ index ];
                DrawRewardTableListItem( rewardTableInfo, filteredInfoList.Count, index );
                GUILayout.Space( ListItemSpacing );
            }

            EditorGUILayout.EndScrollView();
            DrawStatusMessage( statusMessage, statusMessageType );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 테이블 목록 항목 렌더링
        ///</summary>
        private void DrawRewardTableListItem( RandomBoxRewardTableInfo _rewardTableInfo, int _filteredItemCount, int _filteredIndex )
        {
            if ( _rewardTableInfo == null )
            {
                return;
            }

            int sourceIndex = rewardTableInfoList.IndexOf( _rewardTableInfo );
            bool isSelected = sourceIndex == selectedTableIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string controlName = BuildTableControlName( sourceIndex );
            string buttonLabel = $"{_rewardTableInfo.tableName}\nEntry {_rewardTableInfo.entryCount} / Valid {_rewardTableInfo.validEntryCount} / Weight {_rewardTableInfo.totalWeight:0.###}";
            GUI.SetNextControlName( controlName );
            bool wasClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( wasClicked )
            {
                SelectTableByIndex( sourceIndex, _filteredIndex, _filteredItemCount );
            }

            if ( isSelected && isPendingFocusToSelection )
            {
                GUI.FocusControl( controlName );
                isPendingFocusToSelection = false;
            }

            if ( isSelected )
            {
                Rect itemRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect( itemRect, new Color( 0.2f, 0.5f, 0.85f, 0.18f ) );
            }
        }

        ///<summary>
        /// 테이블 편집 영역 렌더링
        ///</summary>
        private void DrawTableEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            CRandomBoxRewardTable rewardTable = GetSelectedRewardTable();

            if ( rewardTable == null )
            {
                EditorGUILayout.HelpBox( "편집할 랜덤상자 보상 테이블을 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawAssetInfoSection( rewardTable );
            EditorGUILayout.Space();
            DrawRewardEntryList( rewardTable );
            EditorGUILayout.Space();
            DrawActionButtons( rewardTable );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 선택 테이블 자산 정보 렌더링
        ///</summary>
        private void DrawAssetInfoSection( CRandomBoxRewardTable _rewardTable )
        {
            string assetPath = AssetDatabase.GetAssetPath( _rewardTable );
            EnsureAssetRenameDraft( _rewardTable, assetPath );
            EditorGUILayout.LabelField( "Asset Info", EditorStyles.boldLabel );
            EditorGUILayout.LabelField( "Asset Path", assetPath );
            currentTableAssetNameDraft = EditorGUILayout.TextField( "Asset Name", currentTableAssetNameDraft );
            EditorGUILayout.LabelField( "Asset File", Path.GetFileNameWithoutExtension( assetPath ) );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Rename Asset", GUILayout.Width( 120.0f ) ) )
            {
                RenameRewardTableAsset( _rewardTable, currentTableAssetNameDraft );
            }

            if ( GUILayout.Button( "Reset Name", GUILayout.Width( 120.0f ) ) )
            {
                ResetCurrentTableAssetNameDraft();
            }

            if ( GUILayout.Button( "Ping Asset", GUILayout.Width( 120.0f ) ) )
            {
                PingTable( _rewardTable );
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 보상 항목 목록 렌더링
        ///</summary>
        private void DrawRewardEntryList( CRandomBoxRewardTable _rewardTable )
        {
            SerializedObject serializedObject = new SerializedObject( _rewardTable );
            serializedObject.Update();
            SerializedProperty rewardEntryListProperty = serializedObject.FindProperty( "rewardEntryList" );

            if ( rewardEntryListProperty == null || rewardEntryListProperty.isArray == false )
            {
                EditorGUILayout.HelpBox( "보상 항목 프로퍼티를 찾지 못했습니다.", MessageType.Error );
                return;
            }

            float totalWeight = CalculateTotalWeight( rewardEntryListProperty );
            int validEntryCount = CountValidRewardEntries( rewardEntryListProperty );
            EditorGUILayout.LabelField( "Reward Entries", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( $"Entry {rewardEntryListProperty.arraySize}개 / Valid {validEntryCount}개 / Total Weight {totalWeight:0.###}", MessageType.None );

            if ( rewardEntryListProperty.arraySize == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 보상 아이템이 없습니다.", MessageType.Info );
            }

            for ( int index = 0; index < rewardEntryListProperty.arraySize; index++ )
            {
                SerializedProperty rewardEntryProperty = rewardEntryListProperty.GetArrayElementAtIndex( index );
                bool didRemoveEntry = DrawRewardEntry( rewardEntryListProperty, rewardEntryProperty, index, totalWeight );

                if ( didRemoveEntry )
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty( _rewardTable );
                    hasPendingAssetChanges = true;
                    GUI.FocusControl( null );
                    Repaint();
                    return;
                }
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Add Reward Entry", GUILayout.Height( 28.0f ) ) )
            {
                AddRewardEntry( rewardEntryListProperty );
            }

            if ( GUILayout.Button( "Normalize Counts", GUILayout.Height( 28.0f ) ) )
            {
                NormalizeRewardEntries( rewardEntryListProperty );
            }

            EditorGUILayout.EndHorizontal();
            bool hasModifiedProperties = serializedObject.ApplyModifiedProperties();

            if ( hasModifiedProperties )
            {
                EditorUtility.SetDirty( _rewardTable );
                hasPendingAssetChanges = true;
            }
        }

        ///<summary>
        /// 보상 항목 렌더링
        ///</summary>
        private bool DrawRewardEntry( SerializedProperty _rewardEntryListProperty, SerializedProperty _rewardEntryProperty, int _index, float _totalWeight )
        {
            if ( _rewardEntryListProperty == null || _rewardEntryProperty == null )
            {
                return false;
            }

            SerializedProperty itemDefinitionProperty = _rewardEntryProperty.FindPropertyRelative( "itemDefinition" );
            SerializedProperty weightProperty = _rewardEntryProperty.FindPropertyRelative( "weight" );
            SerializedProperty minRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "minRewardCountValue" );
            SerializedProperty maxRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "maxRewardCountValue" );
            float weight = weightProperty != null ? Mathf.Max( 0.0f, weightProperty.floatValue ) : 0.0f;
            float probability = _totalWeight > 0.0f ? weight / _totalWeight * 100.0f : 0.0f;
            string itemName = ResolveItemDisplayName( itemDefinitionProperty );

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Reward {_index + 1} - {itemName} ({probability:0.##}%)", EditorStyles.boldLabel );

            if ( GUILayout.Button( "▲", GUILayout.Width( 28.0f ) ) )
            {
                MoveRewardEntry( _rewardEntryListProperty, _index, -1 );
            }

            if ( GUILayout.Button( "▼", GUILayout.Width( 28.0f ) ) )
            {
                MoveRewardEntry( _rewardEntryListProperty, _index, 1 );
            }

            if ( GUILayout.Button( "Copy", GUILayout.Width( 52.0f ) ) )
            {
                DuplicateRewardEntry( _rewardEntryListProperty, _index );
            }

            if ( GUILayout.Button( "Remove", GUILayout.Width( 80.0f ) ) )
            {
                _rewardEntryListProperty.DeleteArrayElementAtIndex( _index );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }

            EditorGUILayout.EndHorizontal();
            DrawRewardEntryFields( itemDefinitionProperty, weightProperty, minRewardCountProperty, maxRewardCountProperty );
            DrawItemSearchSection( itemDefinitionProperty, _index );
            EditorGUILayout.EndVertical();
            return false;
        }

        ///<summary>
        /// 보상 항목 필드 렌더링
        ///</summary>
        private void DrawRewardEntryFields( SerializedProperty _itemDefinitionProperty, SerializedProperty _weightProperty, SerializedProperty _minRewardCountProperty, SerializedProperty _maxRewardCountProperty )
        {
            if ( _itemDefinitionProperty != null )
            {
                EditorGUILayout.PropertyField( _itemDefinitionProperty, new GUIContent( "Item Definition" ) );
            }

            if ( _weightProperty != null )
            {
                EditorGUILayout.PropertyField( _weightProperty, new GUIContent( "Weight" ) );

                if ( _weightProperty.floatValue < 0.0f )
                {
                    _weightProperty.floatValue = 0.0f;
                }
            }

            if ( _minRewardCountProperty != null )
            {
                EditorGUILayout.PropertyField( _minRewardCountProperty, new GUIContent( "Min Count" ) );

                if ( _minRewardCountProperty.longValue < 1L )
                {
                    _minRewardCountProperty.longValue = 1L;
                }
            }

            if ( _maxRewardCountProperty != null && _minRewardCountProperty != null )
            {
                EditorGUILayout.PropertyField( _maxRewardCountProperty, new GUIContent( "Max Count" ) );

                if ( _maxRewardCountProperty.longValue < _minRewardCountProperty.longValue )
                {
                    _maxRewardCountProperty.longValue = _minRewardCountProperty.longValue;
                }
            }
        }

        ///<summary>
        /// 아이템 검색 선택 영역 렌더링
        ///</summary>
        private void DrawItemSearchSection( SerializedProperty _itemDefinitionProperty, int _entryIndex )
        {
            if ( _itemDefinitionProperty == null )
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Item Search", GUILayout.Width( 110.0f ) ) )
            {
                selectedItemSearchEntryIndex = selectedItemSearchEntryIndex == _entryIndex ? -1 : _entryIndex;
            }

            if ( GUILayout.Button( "Clear Item", GUILayout.Width( 90.0f ) ) )
            {
                _itemDefinitionProperty.objectReferenceValue = null;
                hasPendingAssetChanges = true;
            }

            EditorGUILayout.EndHorizontal();

            if ( selectedItemSearchEntryIndex != _entryIndex )
            {
                return;
            }

            EditorGUI.indentLevel++;
            itemSearchText = EditorGUILayout.TextField( "Search", itemSearchText );
            List<RandomBoxItemReferenceInfo> filteredItemList = GetFilteredItemReferenceInfos( itemSearchText );
            int drawCount = Mathf.Min( filteredItemList.Count, MaxItemSearchResultCount );

            for ( int index = 0; index < drawCount; index++ )
            {
                RandomBoxItemReferenceInfo itemInfo = filteredItemList[ index ];
                DrawItemSearchResult( _itemDefinitionProperty, itemInfo );
            }

            if ( filteredItemList.Count > drawCount )
            {
                EditorGUILayout.HelpBox( $"Showing first {drawCount} results. Refine the search text for more precise selection.", MessageType.None );
            }

            if ( filteredItemList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "검색된 아이템이 없습니다.", MessageType.Info );
            }

            EditorGUI.indentLevel--;
        }

        ///<summary>
        /// 아이템 검색 결과 렌더링
        ///</summary>
        private void DrawItemSearchResult( SerializedProperty _itemDefinitionProperty, RandomBoxItemReferenceInfo _itemInfo )
        {
            if ( _itemDefinitionProperty == null || _itemInfo == null )
            {
                return;
            }

            EditorGUILayout.BeginHorizontal( "box" );
            EditorGUILayout.LabelField( _itemInfo.itemName, GUILayout.Width( 180.0f ) );
            EditorGUILayout.LabelField( _itemInfo.itemId );

            if ( GUILayout.Button( "Assign", GUILayout.Width( 80.0f ) ) )
            {
                _itemDefinitionProperty.objectReferenceValue = _itemInfo.itemDefinition;
                selectedItemSearchEntryIndex = -1;
                hasPendingAssetChanges = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 액션 버튼 영역 렌더링
        ///</summary>
        private void DrawActionButtons( CRandomBoxRewardTable _rewardTable )
        {
            string validationMessage = BuildValidationSummary( _rewardTable );

            if ( string.IsNullOrWhiteSpace( validationMessage ) == false )
            {
                EditorGUILayout.HelpBox( validationMessage, MessageType.Warning );
            }

            if ( hasPendingAssetChanges )
            {
                EditorGUILayout.HelpBox( "저장되지 않은 변경 사항이 있습니다.", MessageType.Info );
            }

            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Save", GUILayout.Height( 32.0f ) ) )
            {
                SaveRewardTable( _rewardTable );
            }

            if ( GUILayout.Button( "Duplicate", GUILayout.Height( 32.0f ) ) )
            {
                DuplicateRewardTable( _rewardTable );
            }

            if ( GUILayout.Button( "Delete", GUILayout.Height( 32.0f ) ) )
            {
                DeleteRewardTable( _rewardTable );
            }

            if ( GUILayout.Button( "Test Roll", GUILayout.Height( 32.0f ) ) )
            {
                TestRollReward( _rewardTable );
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 전체 목록 갱신
        ///</summary>
        private void RefreshAll()
        {
            string selectedAssetPath = GetSelectedRewardTableAssetPath();
            RefreshItemReferenceInfos();
            RefreshRewardTableInfos();
            RestoreSelectionByAssetPath( selectedAssetPath );
        }

        ///<summary>
        /// 아이템 참조 목록 갱신
        ///</summary>
        private void RefreshItemReferenceInfos()
        {
            itemReferenceInfoList.Clear();

            if ( AssetDatabase.IsValidFolder( ItemDefinitionFolderPath ) == false )
            {
                return;
            }

            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CItemDefinition", new string[] { ItemDefinitionFolderPath } );
            Array.Sort( assetGuidArray, CompareAssetGuid );

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetGuid = assetGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                CItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( assetPath );

                if ( itemDefinition == null )
                {
                    continue;
                }

                RandomBoxItemReferenceInfo itemInfo = new RandomBoxItemReferenceInfo();
                itemInfo.itemId = itemDefinition.GetItemId();
                itemInfo.itemName = string.IsNullOrWhiteSpace( itemDefinition.GetItemName() ) ? itemDefinition.GetItemId() : itemDefinition.GetItemName();
                itemInfo.assetPath = assetPath;
                itemInfo.itemDefinition = itemDefinition;
                itemReferenceInfoList.Add( itemInfo );
            }
        }

        ///<summary>
        /// 보상 테이블 목록 갱신
        ///</summary>
        private void RefreshRewardTableInfos()
        {
            EnsureRandomBoxTableFolderExists();
            rewardTableInfoList.Clear();
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CRandomBoxRewardTable", new string[] { RandomBoxTableFolderPath } );
            Array.Sort( assetGuidArray, CompareAssetGuid );

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetGuid = assetGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                CRandomBoxRewardTable rewardTable = AssetDatabase.LoadAssetAtPath<CRandomBoxRewardTable>( assetPath );

                if ( rewardTable == null )
                {
                    continue;
                }

                RandomBoxRewardTableInfo tableInfo = CreateRewardTableInfo( rewardTable, assetPath );
                rewardTableInfoList.Add( tableInfo );
            }

            if ( rewardTableInfoList.Count == 0 )
            {
                selectedTableIndex = -1;
                hasPendingAssetChanges = false;
                SetStatus( "랜덤상자 보상 테이블을 찾지 못했습니다.", MessageType.Warning );
                return;
            }

            if ( selectedTableIndex < 0 || selectedTableIndex >= rewardTableInfoList.Count )
            {
                selectedTableIndex = 0;
            }

            hasPendingAssetChanges = false;
            SetStatus( $"보상 테이블 {rewardTableInfoList.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 보상 테이블 목록 정보 생성
        ///</summary>
        private RandomBoxRewardTableInfo CreateRewardTableInfo( CRandomBoxRewardTable _rewardTable, string _assetPath )
        {
            IReadOnlyList<CRandomBoxRewardEntry> rewardEntryList = _rewardTable.GetRewardEntryList();
            RandomBoxRewardTableInfo tableInfo = new RandomBoxRewardTableInfo();
            tableInfo.tableName = string.IsNullOrWhiteSpace( _rewardTable.name ) ? Path.GetFileNameWithoutExtension( _assetPath ) : _rewardTable.name;
            tableInfo.assetPath = _assetPath;
            tableInfo.entryCount = rewardEntryList != null ? rewardEntryList.Count : 0;
            tableInfo.totalWeight = _rewardTable.CalculateTotalWeight();
            tableInfo.validEntryCount = CountValidRewardEntries( rewardEntryList );
            return tableInfo;
        }

        ///<summary>
        /// 보상 테이블 생성
        ///</summary>
        private void CreateRewardTable()
        {
            EnsureRandomBoxTableFolderExists();
            string sanitizedAssetName = SanitizeFileName( newTableAssetName );

            if ( string.IsNullOrWhiteSpace( sanitizedAssetName ) )
            {
                SetStatus( "Asset Name을 입력하세요.", MessageType.Warning );
                return;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath( $"{RandomBoxTableFolderPath}/{sanitizedAssetName}.asset" );
            CRandomBoxRewardTable rewardTable = CreateInstance<CRandomBoxRewardTable>();
            AssetDatabase.CreateAsset( rewardTable, assetPath );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshRewardTableInfos();
            RestoreSelectionByAssetPath( assetPath );
            Selection.activeObject = rewardTable;
            hasPendingAssetChanges = false;
            SetStatus( $"보상 테이블을 생성했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 보상 테이블 저장 처리
        ///</summary>
        private void SaveRewardTable( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            string validationMessage = BuildValidationSummary( _rewardTable );

            if ( string.IsNullOrWhiteSpace( validationMessage ) == false )
            {
                SetStatus( validationMessage, MessageType.Warning );
                return;
            }

            string selectedAssetPath = AssetDatabase.GetAssetPath( _rewardTable );
            EditorUtility.SetDirty( _rewardTable );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshRewardTableInfos();
            RestoreSelectionByAssetPath( selectedAssetPath );
            hasPendingAssetChanges = false;
            SetStatus( $"보상 테이블을 저장했습니다: {selectedAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 보상 테이블 복제
        ///</summary>
        private void DuplicateRewardTable( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( _rewardTable );
            bool isCopied = TryDuplicateAsset( _rewardTable, sourceAssetPath, out string duplicatedAssetPath );

            if ( isCopied == false )
            {
                SetStatus( "보상 테이블 복제에 실패했습니다.", MessageType.Error );
                return;
            }

            RefreshRewardTableInfos();
            RestoreSelectionByAssetPath( duplicatedAssetPath );
            hasPendingAssetChanges = false;
            SetStatus( $"보상 테이블을 복제했습니다: {duplicatedAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 보상 테이블 삭제
        ///</summary>
        private void DeleteRewardTable( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _rewardTable );
            bool isConfirmed = EditorUtility.DisplayDialog( "Delete Random Box Reward Table", $"{assetPath}\n삭제하시겠습니까?", "Delete", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            bool isDeleted = AssetDatabase.DeleteAsset( assetPath );

            if ( isDeleted == false )
            {
                SetStatus( "보상 테이블 삭제에 실패했습니다.", MessageType.Error );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            selectedTableIndex = -1;
            RefreshRewardTableInfos();
            hasPendingAssetChanges = false;
            SetStatus( $"보상 테이블을 삭제했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 보상 테이블 자산명 변경
        ///</summary>
        private void RenameRewardTableAsset( CRandomBoxRewardTable _rewardTable, string _newAssetName )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _rewardTable );
            string sanitizedAssetName = SanitizeFileName( _newAssetName );

            if ( string.IsNullOrWhiteSpace( sanitizedAssetName ) )
            {
                SetStatus( "변경할 Asset Name을 입력하세요.", MessageType.Warning );
                return;
            }

            string renameError = AssetDatabase.RenameAsset( assetPath, sanitizedAssetName );

            if ( string.IsNullOrWhiteSpace( renameError ) == false )
            {
                SetStatus( $"자산 이름 변경에 실패했습니다. ({renameError})", MessageType.Warning );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string renamedAssetPath = $"{Path.GetDirectoryName( assetPath )?.Replace( '\\', '/' )}/{sanitizedAssetName}.asset";
            RefreshRewardTableInfos();
            RestoreSelectionByAssetPath( renamedAssetPath );
            currentTableAssetNameDraft = sanitizedAssetName;
            currentTableAssetPathDraft = renamedAssetPath;
            SetStatus( $"자산 이름을 변경했습니다. ({sanitizedAssetName})", MessageType.Info );
        }

        ///<summary>
        /// 보상 추첨 테스트
        ///</summary>
        private void TestRollReward( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            bool didRollReward = _rewardTable.TryRollReward( out CItemDefinition itemDefinition, out long rewardCount );

            if ( didRollReward == false || itemDefinition == null )
            {
                SetStatus( "추첨 가능한 보상이 없습니다.", MessageType.Warning );
                return;
            }

            SetStatus( $"테스트 결과: {itemDefinition.GetItemName()} x{rewardCount}", MessageType.Info );
        }

        ///<summary>
        /// 보상 항목 추가
        ///</summary>
        private void AddRewardEntry( SerializedProperty _rewardEntryListProperty )
        {
            if ( _rewardEntryListProperty == null )
            {
                return;
            }

            int nextIndex = _rewardEntryListProperty.arraySize;
            _rewardEntryListProperty.InsertArrayElementAtIndex( nextIndex );
            SerializedProperty createdEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( nextIndex );

            if ( nextIndex > 0 )
            {
                SerializedProperty previousEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( nextIndex - 1 );
                CopyRewardEntryProperty( previousEntryProperty, createdEntryProperty );
            }
            else
            {
                ResetRewardEntryProperty( createdEntryProperty );
            }

            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 보상 항목 복제
        ///</summary>
        private void DuplicateRewardEntry( SerializedProperty _rewardEntryListProperty, int _entryIndex )
        {
            if ( _rewardEntryListProperty == null || _entryIndex < 0 || _entryIndex >= _rewardEntryListProperty.arraySize )
            {
                return;
            }

            int nextIndex = _entryIndex + 1;
            _rewardEntryListProperty.InsertArrayElementAtIndex( nextIndex );
            SerializedProperty sourceEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( _entryIndex );
            SerializedProperty targetEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( nextIndex );
            CopyRewardEntryProperty( sourceEntryProperty, targetEntryProperty );
            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 보상 항목 순서 이동
        ///</summary>
        private void MoveRewardEntry( SerializedProperty _rewardEntryListProperty, int _entryIndex, int _direction )
        {
            if ( _rewardEntryListProperty == null )
            {
                return;
            }

            int targetIndex = _entryIndex + _direction;

            if ( targetIndex < 0 || targetIndex >= _rewardEntryListProperty.arraySize )
            {
                return;
            }

            _rewardEntryListProperty.MoveArrayElement( _entryIndex, targetIndex );
            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 보상 항목 프로퍼티 복사
        ///</summary>
        private void CopyRewardEntryProperty( SerializedProperty _sourceEntryProperty, SerializedProperty _targetEntryProperty )
        {
            if ( _sourceEntryProperty == null || _targetEntryProperty == null )
            {
                return;
            }

            SerializedProperty sourceItemDefinitionProperty = _sourceEntryProperty.FindPropertyRelative( "itemDefinition" );
            SerializedProperty sourceWeightProperty = _sourceEntryProperty.FindPropertyRelative( "weight" );
            SerializedProperty sourceMinRewardCountProperty = _sourceEntryProperty.FindPropertyRelative( "minRewardCountValue" );
            SerializedProperty sourceMaxRewardCountProperty = _sourceEntryProperty.FindPropertyRelative( "maxRewardCountValue" );
            SerializedProperty targetItemDefinitionProperty = _targetEntryProperty.FindPropertyRelative( "itemDefinition" );
            SerializedProperty targetWeightProperty = _targetEntryProperty.FindPropertyRelative( "weight" );
            SerializedProperty targetMinRewardCountProperty = _targetEntryProperty.FindPropertyRelative( "minRewardCountValue" );
            SerializedProperty targetMaxRewardCountProperty = _targetEntryProperty.FindPropertyRelative( "maxRewardCountValue" );

            if ( sourceItemDefinitionProperty != null && targetItemDefinitionProperty != null )
            {
                targetItemDefinitionProperty.objectReferenceValue = sourceItemDefinitionProperty.objectReferenceValue;
            }

            if ( sourceWeightProperty != null && targetWeightProperty != null )
            {
                targetWeightProperty.floatValue = sourceWeightProperty.floatValue;
            }

            if ( sourceMinRewardCountProperty != null && targetMinRewardCountProperty != null )
            {
                targetMinRewardCountProperty.longValue = sourceMinRewardCountProperty.longValue;
            }

            if ( sourceMaxRewardCountProperty != null && targetMaxRewardCountProperty != null )
            {
                targetMaxRewardCountProperty.longValue = sourceMaxRewardCountProperty.longValue;
            }
        }

        ///<summary>
        /// 보상 항목 프로퍼티 초기화
        ///</summary>
        private void ResetRewardEntryProperty( SerializedProperty _rewardEntryProperty )
        {
            if ( _rewardEntryProperty == null )
            {
                return;
            }

            SerializedProperty itemDefinitionProperty = _rewardEntryProperty.FindPropertyRelative( "itemDefinition" );
            SerializedProperty weightProperty = _rewardEntryProperty.FindPropertyRelative( "weight" );
            SerializedProperty minRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "minRewardCountValue" );
            SerializedProperty maxRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "maxRewardCountValue" );

            if ( itemDefinitionProperty != null )
            {
                itemDefinitionProperty.objectReferenceValue = null;
            }

            if ( weightProperty != null )
            {
                weightProperty.floatValue = 1.0f;
            }

            if ( minRewardCountProperty != null )
            {
                minRewardCountProperty.longValue = 1L;
            }

            if ( maxRewardCountProperty != null )
            {
                maxRewardCountProperty.longValue = 1L;
            }
        }

        ///<summary>
        /// 보상 항목 수량 정규화
        ///</summary>
        private void NormalizeRewardEntries( SerializedProperty _rewardEntryListProperty )
        {
            if ( _rewardEntryListProperty == null )
            {
                return;
            }

            for ( int index = 0; index < _rewardEntryListProperty.arraySize; index++ )
            {
                SerializedProperty entryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( index );
                NormalizeRewardEntry( entryProperty );
            }

            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 보상 항목 수량 정규화
        ///</summary>
        private void NormalizeRewardEntry( SerializedProperty _rewardEntryProperty )
        {
            if ( _rewardEntryProperty == null )
            {
                return;
            }

            SerializedProperty weightProperty = _rewardEntryProperty.FindPropertyRelative( "weight" );
            SerializedProperty minRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "minRewardCountValue" );
            SerializedProperty maxRewardCountProperty = _rewardEntryProperty.FindPropertyRelative( "maxRewardCountValue" );

            if ( weightProperty != null && weightProperty.floatValue < 0.0f )
            {
                weightProperty.floatValue = 0.0f;
            }

            if ( minRewardCountProperty != null && minRewardCountProperty.longValue < 1L )
            {
                minRewardCountProperty.longValue = 1L;
            }

            if ( maxRewardCountProperty != null && minRewardCountProperty != null && maxRewardCountProperty.longValue < minRewardCountProperty.longValue )
            {
                maxRewardCountProperty.longValue = minRewardCountProperty.longValue;
            }
        }

        ///<summary>
        /// 검증 메시지 구성
        ///</summary>
        private string BuildValidationSummary( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return "보상 테이블을 찾지 못했습니다.";
            }

            IReadOnlyList<CRandomBoxRewardEntry> rewardEntryList = _rewardTable.GetRewardEntryList();

            if ( rewardEntryList == null || rewardEntryList.Count == 0 )
            {
                return "최소 1개 이상의 보상 항목이 필요합니다.";
            }

            for ( int index = 0; index < rewardEntryList.Count; index++ )
            {
                CRandomBoxRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    return $"Reward {index + 1} 데이터가 비어 있습니다.";
                }

                if ( rewardEntry.GetItemDefinition() == null )
                {
                    return $"Reward {index + 1}의 Item Definition을 지정하세요.";
                }

                if ( rewardEntry.GetWeight() <= 0.0f )
                {
                    return $"Reward {index + 1}의 Weight를 0보다 크게 입력하세요.";
                }

                if ( rewardEntry.GetMaxRewardCount() <= 0L )
                {
                    return $"Reward {index + 1}의 수량 범위를 확인하세요.";
                }
            }

            return string.Empty;
        }

        ///<summary>
        /// 필터 적용 보상 테이블 목록 반환
        ///</summary>
        private List<RandomBoxRewardTableInfo> GetFilteredRewardTableInfos()
        {
            List<RandomBoxRewardTableInfo> filteredInfoList = new List<RandomBoxRewardTableInfo>();

            for ( int index = 0; index < rewardTableInfoList.Count; index++ )
            {
                RandomBoxRewardTableInfo tableInfo = rewardTableInfoList[ index ];

                if ( IsSearchMatch( tableInfo, searchText ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( tableInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 필터 적용 아이템 참조 목록 반환
        ///</summary>
        private List<RandomBoxItemReferenceInfo> GetFilteredItemReferenceInfos( string _searchKeyword )
        {
            List<RandomBoxItemReferenceInfo> filteredInfoList = new List<RandomBoxItemReferenceInfo>();
            string normalizedKeyword = string.IsNullOrWhiteSpace( _searchKeyword ) ? string.Empty : _searchKeyword.Trim();

            for ( int index = 0; index < itemReferenceInfoList.Count; index++ )
            {
                RandomBoxItemReferenceInfo itemInfo = itemReferenceInfoList[ index ];

                if ( itemInfo == null )
                {
                    continue;
                }

                if ( string.IsNullOrWhiteSpace( normalizedKeyword ) == false )
                {
                    bool containsId = string.IsNullOrWhiteSpace( itemInfo.itemId ) == false && itemInfo.itemId.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;
                    bool containsName = string.IsNullOrWhiteSpace( itemInfo.itemName ) == false && itemInfo.itemName.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;

                    if ( containsId == false && containsName == false )
                    {
                        continue;
                    }
                }

                filteredInfoList.Add( itemInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 테이블 검색 일치 여부 반환
        ///</summary>
        protected override bool IsSearchMatch( RandomBoxRewardTableInfo _tableInfo, string _searchText )
        {
            if ( _tableInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( _searchText ) )
            {
                return true;
            }

            string normalizedSearchText = _searchText.Trim();
            bool containsName = string.IsNullOrWhiteSpace( _tableInfo.tableName ) == false && _tableInfo.tableName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            bool containsPath = string.IsNullOrWhiteSpace( _tableInfo.assetPath ) == false && _tableInfo.assetPath.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            bool result = containsName || containsPath;
            return result;
        }

        ///<summary>
        /// 선택 보상 테이블 반환
        ///</summary>
        private CRandomBoxRewardTable GetSelectedRewardTable()
        {
            if ( selectedTableIndex < 0 || selectedTableIndex >= rewardTableInfoList.Count )
            {
                return null;
            }

            RandomBoxRewardTableInfo tableInfo = rewardTableInfoList[ selectedTableIndex ];

            if ( tableInfo == null || string.IsNullOrWhiteSpace( tableInfo.assetPath ) )
            {
                return null;
            }

            CRandomBoxRewardTable result = AssetDatabase.LoadAssetAtPath<CRandomBoxRewardTable>( tableInfo.assetPath );
            return result;
        }

        ///<summary>
        /// 선택 보상 테이블 자산 경로 반환
        ///</summary>
        private string GetSelectedRewardTableAssetPath()
        {
            if ( selectedTableIndex < 0 || selectedTableIndex >= rewardTableInfoList.Count )
            {
                return string.Empty;
            }

            RandomBoxRewardTableInfo tableInfo = rewardTableInfoList[ selectedTableIndex ];
            string result = tableInfo != null ? tableInfo.assetPath : string.Empty;
            return result;
        }

        ///<summary>
        /// 보상 테이블 선택 처리
        ///</summary>
        private void SelectTableByIndex( int _sourceIndex, int _filteredIndex, int _filteredItemCount )
        {
            if ( _sourceIndex < 0 || _sourceIndex >= rewardTableInfoList.Count )
            {
                return;
            }

            selectedTableIndex = _sourceIndex;
            CRandomBoxRewardTable selectedRewardTable = GetSelectedRewardTable();

            if ( selectedRewardTable != null )
            {
                string assetPath = AssetDatabase.GetAssetPath( selectedRewardTable );
                EnsureAssetRenameDraft( selectedRewardTable, assetPath );
            }

            selectedItemSearchEntryIndex = -1;
            isPendingFocusToSelection = true;
            EnsureSelectionVisibleByIndex( _filteredIndex, _filteredItemCount );
            Repaint();
        }

        ///<summary>
        /// 자산 경로 기준 선택 복원
        ///</summary>
        private void RestoreSelectionByAssetPath( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return;
            }

            for ( int index = 0; index < rewardTableInfoList.Count; index++ )
            {
                RandomBoxRewardTableInfo tableInfo = rewardTableInfoList[ index ];

                if ( tableInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( tableInfo.assetPath, _assetPath, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                selectedTableIndex = index;
                return;
            }
        }

        ///<summary>
        /// 선택 항목 스크롤 보정
        ///</summary>
        private void EnsureSelectionVisibleByIndex( int _filteredSelectedIndex, int _filteredItemCount )
        {
            float itemStride = ListItemHeight + ListItemSpacing;
            float itemTop = _filteredSelectedIndex * itemStride;
            float itemBottom = itemTop + ListItemHeight;
            float contentHeight = Mathf.Max( 0.0f, _filteredItemCount * itemStride );
            float maxScrollY = Mathf.Max( 0.0f, contentHeight - ListViewHeight );

            if ( itemTop < tableListScrollPosition.y )
            {
                tableListScrollPosition.y = itemTop;
            }
            else if ( itemBottom > tableListScrollPosition.y + ListViewHeight )
            {
                tableListScrollPosition.y = itemBottom - ListViewHeight;
            }

            tableListScrollPosition.y = Mathf.Clamp( tableListScrollPosition.y, 0.0f, maxScrollY );
        }

        ///<summary>
        /// 신규 테이블 자산명 초기화
        ///</summary>
        private void ResetNewTableAssetName()
        {
            newTableAssetName = "RandomBoxRewardTable";
        }

        ///<summary>
        /// 현재 테이블 자산명 초안 보장
        ///</summary>
        private void EnsureAssetRenameDraft( CRandomBoxRewardTable _selectedRewardTable, string _assetPath )
        {
            if ( _selectedRewardTable == null || string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return;
            }

            bool isSameAsset = string.Equals( currentTableAssetPathDraft, _assetPath, StringComparison.Ordinal );

            if ( isSameAsset )
            {
                return;
            }

            currentTableAssetPathDraft = _assetPath;
            currentTableAssetNameDraft = Path.GetFileNameWithoutExtension( _assetPath );
        }

        ///<summary>
        /// 현재 테이블 자산명 초안 초기화
        ///</summary>
        private void ResetCurrentTableAssetNameDraft()
        {
            CRandomBoxRewardTable selectedRewardTable = GetSelectedRewardTable();

            if ( selectedRewardTable == null )
            {
                return;
            }

            currentTableAssetNameDraft = Path.GetFileNameWithoutExtension( AssetDatabase.GetAssetPath( selectedRewardTable ) );
        }

        ///<summary>
        /// 선택 테이블 Ping 처리
        ///</summary>
        private void PingSelectedTable()
        {
            CRandomBoxRewardTable selectedRewardTable = GetSelectedRewardTable();
            PingTable( selectedRewardTable );
        }

        ///<summary>
        /// 테이블 Ping 처리
        ///</summary>
        private void PingTable( CRandomBoxRewardTable _rewardTable )
        {
            if ( _rewardTable == null )
            {
                return;
            }

            EditorGUIUtility.PingObject( _rewardTable );
            Selection.activeObject = _rewardTable;
        }

        ///<summary>
        /// 보상 항목 전체 가중치 계산
        ///</summary>
        private float CalculateTotalWeight( SerializedProperty _rewardEntryListProperty )
        {
            if ( _rewardEntryListProperty == null )
            {
                return 0.0f;
            }

            float totalWeight = 0.0f;

            for ( int index = 0; index < _rewardEntryListProperty.arraySize; index++ )
            {
                SerializedProperty rewardEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( index );
                SerializedProperty itemDefinitionProperty = rewardEntryProperty != null ? rewardEntryProperty.FindPropertyRelative( "itemDefinition" ) : null;
                SerializedProperty weightProperty = rewardEntryProperty != null ? rewardEntryProperty.FindPropertyRelative( "weight" ) : null;
                bool hasItem = itemDefinitionProperty != null && itemDefinitionProperty.objectReferenceValue != null;
                float weight = weightProperty != null ? Mathf.Max( 0.0f, weightProperty.floatValue ) : 0.0f;

                if ( hasItem == false || weight <= 0.0f )
                {
                    continue;
                }

                totalWeight += weight;
            }

            return totalWeight;
        }

        ///<summary>
        /// 유효 보상 항목 개수 계산
        ///</summary>
        private int CountValidRewardEntries( SerializedProperty _rewardEntryListProperty )
        {
            if ( _rewardEntryListProperty == null )
            {
                return 0;
            }

            int validCount = 0;

            for ( int index = 0; index < _rewardEntryListProperty.arraySize; index++ )
            {
                SerializedProperty rewardEntryProperty = _rewardEntryListProperty.GetArrayElementAtIndex( index );
                SerializedProperty itemDefinitionProperty = rewardEntryProperty != null ? rewardEntryProperty.FindPropertyRelative( "itemDefinition" ) : null;
                SerializedProperty weightProperty = rewardEntryProperty != null ? rewardEntryProperty.FindPropertyRelative( "weight" ) : null;
                bool hasItem = itemDefinitionProperty != null && itemDefinitionProperty.objectReferenceValue != null;
                bool hasWeight = weightProperty != null && weightProperty.floatValue > 0.0f;

                if ( hasItem && hasWeight )
                {
                    validCount++;
                }
            }

            return validCount;
        }

        ///<summary>
        /// 유효 보상 항목 개수 계산
        ///</summary>
        private int CountValidRewardEntries( IReadOnlyList<CRandomBoxRewardEntry> _rewardEntryList )
        {
            if ( _rewardEntryList == null )
            {
                return 0;
            }

            int validCount = 0;

            for ( int index = 0; index < _rewardEntryList.Count; index++ )
            {
                CRandomBoxRewardEntry rewardEntry = _rewardEntryList[ index ];

                if ( rewardEntry == null || rewardEntry.IsValid() == false )
                {
                    continue;
                }

                validCount++;
            }

            return validCount;
        }

        ///<summary>
        /// 아이템 표시 이름 반환
        ///</summary>
        private string ResolveItemDisplayName( SerializedProperty _itemDefinitionProperty )
        {
            CItemDefinition itemDefinition = _itemDefinitionProperty != null ? _itemDefinitionProperty.objectReferenceValue as CItemDefinition : null;

            if ( itemDefinition == null )
            {
                return "Empty";
            }

            string itemName = string.IsNullOrWhiteSpace( itemDefinition.GetItemName() ) ? itemDefinition.GetItemId() : itemDefinition.GetItemName();
            return itemName;
        }

        ///<summary>
        /// 랜덤상자 테이블 폴더 생성 보장
        ///</summary>
        private void EnsureRandomBoxTableFolderExists()
        {
            EnsureFolderPath( "Assets/Resources" );
            EnsureFolderPath( "Assets/Resources/Data" );
            EnsureFolderPath( "Assets/Resources/Data/Item" );
            EnsureFolderPath( RandomBoxTableFolderPath );
        }

        ///<summary>
        /// 폴더 경로 존재 보장
        ///</summary>
        private void EnsureFolderPath( string _folderPath )
        {
            bool isRootAssets = string.Equals( _folderPath, "Assets", StringComparison.OrdinalIgnoreCase );

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
        /// 파일명 정리 문자열 반환
        ///</summary>
        private string SanitizeFileName( string _sourceText )
        {
            if ( string.IsNullOrWhiteSpace( _sourceText ) )
            {
                return string.Empty;
            }

            string sanitizedText = _sourceText.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for ( int index = 0; index < invalidCharacters.Length; index++ )
            {
                char invalidCharacter = invalidCharacters[ index ];
                sanitizedText = sanitizedText.Replace( invalidCharacter.ToString(), string.Empty );
            }

            string result = sanitizedText;
            return result;
        }

        ///<summary>
        /// 에셋 GUID 비교
        ///</summary>
        private int CompareAssetGuid( string _leftGuid, string _rightGuid )
        {
            string leftAssetPath = AssetDatabase.GUIDToAssetPath( _leftGuid );
            string rightAssetPath = AssetDatabase.GUIDToAssetPath( _rightGuid );
            int result = string.Compare( leftAssetPath, rightAssetPath, StringComparison.OrdinalIgnoreCase );
            return result;
        }

        ///<summary>
        /// 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildTableControlName( int _sourceIndex )
        {
            string result = $"RandomBoxRewardTableItem_{_sourceIndex}";
            return result;
        }

        ///<summary>
        /// 키보드 선택 이동 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            bool hasDirection = TryGetKeyboardNavigationDirection( out int direction );

            if ( hasDirection )
            {
                MoveSelectionInFilteredList( direction );
            }
        }

        ///<summary>
        /// 필터 목록 선택 이동
        ///</summary>
        private void MoveSelectionInFilteredList( int _direction )
        {
            List<RandomBoxRewardTableInfo> filteredInfoList = GetFilteredRewardTableInfos();

            if ( filteredInfoList.Count == 0 )
            {
                return;
            }

            RandomBoxRewardTableInfo selectedInfo = null;

            if ( selectedTableIndex >= 0 && selectedTableIndex < rewardTableInfoList.Count )
            {
                selectedInfo = rewardTableInfoList[ selectedTableIndex ];
            }

            int nextFilteredIndex = ResolveNextFilteredIndex( filteredInfoList, selectedInfo, _direction );
            RandomBoxRewardTableInfo nextInfo = filteredInfoList[ nextFilteredIndex ];
            int sourceIndex = rewardTableInfoList.IndexOf( nextInfo );
            SelectTableByIndex( sourceIndex, nextFilteredIndex, filteredInfoList.Count );
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus( string _message, MessageType _messageType )
        {
            statusMessage = _message;
            statusMessageType = _messageType;
            Repaint();
        }
    }
}
