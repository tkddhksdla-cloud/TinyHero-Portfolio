using System;
using System.Collections.Generic;
using System.IO;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 상점 정의 목록 정보
    ///</summary>
    [Serializable]
    public sealed class ShopDefinitionInfo
    {
        public string shopName;
        public string shopId;
        public string assetPath;
        public int entryCount;
    }

    ///<summary>
    /// 아이템 참조 목록 정보
    ///</summary>
    [Serializable]
    public sealed class ShopItemReferenceInfo
    {
        public string itemId;
        public string itemName;
        public string assetPath;
    }

    ///<summary>
    /// 상점 정의 편집 창
    ///</summary>
    public sealed class ShopDefinitionEditorWindow : EditorWindow
    {
        private const string ShopDefinitionFolderPath = "Assets/Resources/Data/Shop/Definitions";
        private const string ItemDefinitionFolderPath = "Assets/Resources/Data/Item/Definitions";
        private const string DefaultPriceItemId = "GOLD";
        private const float ListViewHeight = 480.0f;
        private const float ListItemHeight = 42.0f;
        private const float ListItemSpacing = 4.0f;
        private const int ShopSlotCapacity = 15;

        [SerializeField] private List<ShopDefinitionInfo> shopDefinitionInfoList = new List<ShopDefinitionInfo>();
        [SerializeField] private List<ShopItemReferenceInfo> itemReferenceInfoList = new List<ShopItemReferenceInfo>();
        [SerializeField] private int selectedShopIndex = -1;
        [SerializeField] private string searchText = string.Empty;

        private Vector2 shopListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "상점 정의 목록을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private bool hasPendingAssetChanges;

        ///<summary>
        /// 상점 정의 편집 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Shop Definition Editor" )]
        private static void ShowWindow()
        {
            ShopDefinitionEditorWindow window = GetWindow<ShopDefinitionEditorWindow>();
            window.titleContent = new GUIContent( "Shop Definition Editor" );
            window.minSize = new Vector2( 1280.0f, 780.0f );
            window.Show();
        }

        ///<summary>
        /// 상점 정의 편집 창 열기
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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Shop Definition Editor", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "상점 데이터를 검색, 생성, 복제, 삭제하고 판매 품목과 구매 재화를 즉시 구성합니다.", MessageType.None );
            EditorGUILayout.Space();
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawShopListSection();
            DrawEditorSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            string updatedSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( updatedSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = updatedSearchText;
            }

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 110.0f ) ) )
            {
                RefreshAll();
            }

            if ( GUILayout.Button( "Create", GUILayout.Width( 110.0f ) ) )
            {
                CreateNewShopDefinition();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상점 목록 영역 렌더링
        ///</summary>
        private void DrawShopListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( 420.0f ) );
            EditorGUILayout.LabelField( "Shop Definitions", EditorStyles.boldLabel );
            List<ShopDefinitionInfo> filteredInfoList = GetFilteredShopDefinitionInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfoList.Count}개", MessageType.None );
            shopListScrollPosition = EditorGUILayout.BeginScrollView( shopListScrollPosition, GUILayout.Height( ListViewHeight ) );

            for ( int index = 0; index < filteredInfoList.Count; index++ )
            {
                ShopDefinitionInfo shopInfo = filteredInfoList[ index ];
                DrawShopListItem( shopInfo, filteredInfoList.Count, index );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 상점 목록 항목 렌더링
        ///</summary>
        private void DrawShopListItem( ShopDefinitionInfo _shopInfo, int _filteredItemCount, int _filteredIndex )
        {
            if ( _shopInfo == null )
            {
                return;
            }

            int sourceIndex = shopDefinitionInfoList.IndexOf( _shopInfo );
            bool isSelected = sourceIndex == selectedShopIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string controlName = BuildShopControlName( sourceIndex );
            string buttonLabel = $"{_shopInfo.shopName}\n{_shopInfo.shopId}  |  품목 {_shopInfo.entryCount}개";
            GUI.SetNextControlName( controlName );
            bool wasClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( wasClicked )
            {
                SelectShopByIndex( sourceIndex, _filteredIndex, _filteredItemCount );
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
        /// 편집 영역 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            CShopDefinition selectedShopDefinition = GetSelectedShopDefinition();

            if ( selectedShopDefinition == null )
            {
                EditorGUILayout.HelpBox( "편집할 상점 정의를 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawAssetInfoSection( selectedShopDefinition );
            EditorGUILayout.Space();
            DrawShopPropertySection( selectedShopDefinition );
            EditorGUILayout.Space();
            DrawActionButtonSection( selectedShopDefinition );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 에셋 정보 영역 렌더링
        ///</summary>
        private void DrawAssetInfoSection( CShopDefinition _shopDefinition )
        {
            string assetPath = AssetDatabase.GetAssetPath( _shopDefinition );
            EditorGUILayout.LabelField( "Asset Info", EditorStyles.boldLabel );
            EditorGUILayout.LabelField( "Asset Name", _shopDefinition.name );
            EditorGUILayout.LabelField( "Asset Path", assetPath );
        }

        ///<summary>
        /// 상점 속성 영역 렌더링
        ///</summary>
        private void DrawShopPropertySection( CShopDefinition _shopDefinition )
        {
            SerializedObject serializedObject = new SerializedObject( _shopDefinition );
            serializedObject.Update();
            SerializedProperty shopIdProperty = serializedObject.FindProperty( "shopId" );
            SerializedProperty shopNameProperty = serializedObject.FindProperty( "shopName" );
            SerializedProperty shopEntryDataListProperty = serializedObject.FindProperty( "shopEntryDataList" );
            EditorGUILayout.LabelField( "Shop Settings", EditorStyles.boldLabel );

            if ( shopIdProperty != null )
            {
                EditorGUILayout.PropertyField( shopIdProperty );
            }

            if ( shopNameProperty != null )
            {
                EditorGUILayout.PropertyField( shopNameProperty );
            }

            EditorGUILayout.Space();
            DrawShopEntryListSection( shopEntryDataListProperty );
            bool hasModifiedProperties = serializedObject.ApplyModifiedProperties();

            if ( hasModifiedProperties )
            {
                EditorUtility.SetDirty( _shopDefinition );
                hasPendingAssetChanges = true;
            }
        }

        ///<summary>
        /// 상점 판매 목록 영역 렌더링
        ///</summary>
        private void DrawShopEntryListSection( SerializedProperty _shopEntryDataListProperty )
        {
            if ( _shopEntryDataListProperty == null )
            {
                EditorGUILayout.HelpBox( "판매 목록 프로퍼티를 찾지 못했습니다.", MessageType.Warning );
                return;
            }

            EditorGUILayout.LabelField( "Shop Entries", EditorStyles.boldLabel );
            int entryCount = _shopEntryDataListProperty.arraySize;
            EditorGUILayout.HelpBox( $"현재 품목 {entryCount}개 / UI 슬롯 {ShopSlotCapacity}개", MessageType.None );

            if ( entryCount > ShopSlotCapacity )
            {
                EditorGUILayout.HelpBox( $"현재 PopupShop은 앞의 {ShopSlotCapacity}개만 표시합니다. 슬롯을 더 늘리거나 품목 수를 줄이세요.", MessageType.Warning );
            }

            if ( entryCount == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 판매 품목이 없습니다.", MessageType.None );
            }

            for ( int index = 0; index < entryCount; index++ )
            {
                SerializedProperty entryProperty = _shopEntryDataListProperty.GetArrayElementAtIndex( index );
                DrawShopEntryElement( _shopEntryDataListProperty, entryProperty, index );
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "품목 추가", GUILayout.Height( 28.0f ) ) )
            {
                AddShopEntry( _shopEntryDataListProperty );
            }

            if ( GUILayout.Button( "15칸 맞추기", GUILayout.Height( 28.0f ) ) )
            {
                ExpandShopEntriesToCapacity( _shopEntryDataListProperty );
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상점 판매 항목 렌더링
        ///</summary>
        private void DrawShopEntryElement( SerializedProperty _shopEntryDataListProperty, SerializedProperty _entryProperty, int _entryIndex )
        {
            if ( _shopEntryDataListProperty == null || _entryProperty == null )
            {
                return;
            }

            SerializedProperty itemIdProperty = _entryProperty.FindPropertyRelative( "itemId" );
            SerializedProperty itemCountProperty = _entryProperty.FindPropertyRelative( "itemCount" );
            SerializedProperty priceItemIdProperty = _entryProperty.FindPropertyRelative( "priceItemId" );
            SerializedProperty priceAmountProperty = _entryProperty.FindPropertyRelative( "priceAmount" );
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Entry {_entryIndex + 1}", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Up", GUILayout.Width( 50.0f ) ) )
            {
                MoveShopEntry( _shopEntryDataListProperty, _entryIndex, -1 );
            }

            if ( GUILayout.Button( "Down", GUILayout.Width( 58.0f ) ) )
            {
                MoveShopEntry( _shopEntryDataListProperty, _entryIndex, 1 );
            }

            if ( GUILayout.Button( "Remove", GUILayout.Width( 80.0f ) ) )
            {
                _shopEntryDataListProperty.DeleteArrayElementAtIndex( _entryIndex );
                hasPendingAssetChanges = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            DrawItemSelectorField( "판매 아이템", itemIdProperty, false );

            if ( itemCountProperty != null )
            {
                int resolvedItemCount = Mathf.Max( 1, EditorGUILayout.IntField( "판매 수량", itemCountProperty.intValue ) );
                itemCountProperty.intValue = resolvedItemCount;
            }

            EditorGUILayout.Space( 2.0f );
            DrawItemSelectorField( "구매 재화", priceItemIdProperty, true );

            if ( priceAmountProperty != null )
            {
                int resolvedPriceAmount = Mathf.Max( 0, EditorGUILayout.IntField( "가격 수량", priceAmountProperty.intValue ) );
                priceAmountProperty.intValue = resolvedPriceAmount;
            }

            string entrySummaryText = BuildEntrySummaryText( itemIdProperty, itemCountProperty, priceItemIdProperty, priceAmountProperty );
            EditorGUILayout.HelpBox( entrySummaryText, MessageType.None );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 아이템 선택 필드 렌더링
        ///</summary>
        private void DrawItemSelectorField( string _label, SerializedProperty _itemIdProperty, bool _useDefaultPriceItem )
        {
            if ( _itemIdProperty == null )
            {
                return;
            }

            if ( _useDefaultPriceItem && string.IsNullOrWhiteSpace( _itemIdProperty.stringValue ) )
            {
                _itemIdProperty.stringValue = DefaultPriceItemId;
            }

            string currentItemId = string.IsNullOrWhiteSpace( _itemIdProperty.stringValue ) == false ? _itemIdProperty.stringValue.Trim() : string.Empty;
            string[] optionLabelArray = BuildItemOptionLabelArray();
            int selectedOptionIndex = ResolveItemOptionIndex( currentItemId );
            int updatedOptionIndex = EditorGUILayout.Popup( _label, selectedOptionIndex, optionLabelArray );

            if ( updatedOptionIndex != selectedOptionIndex )
            {
                string selectedItemId = ResolveItemIdByOptionIndex( updatedOptionIndex, currentItemId );
                _itemIdProperty.stringValue = selectedItemId;
                currentItemId = selectedItemId;
            }

            bool isManualInput = updatedOptionIndex == 0;

            if ( isManualInput )
            {
                string updatedItemId = EditorGUILayout.TextField( $"{_label} ID", currentItemId );
                _itemIdProperty.stringValue = updatedItemId.Trim();
                currentItemId = _itemIdProperty.stringValue;
            }

            string resolvedItemName = ResolveItemName( currentItemId );

            if ( string.IsNullOrWhiteSpace( currentItemId ) )
            {
                EditorGUILayout.HelpBox( $"{_label} ID를 입력하세요.", MessageType.Warning );
                return;
            }

            if ( string.Equals( resolvedItemName, currentItemId, StringComparison.Ordinal ) )
            {
                EditorGUILayout.HelpBox( $"{_label}: {currentItemId} (등록되지 않은 아이템 ID)", MessageType.Warning );
                return;
            }

            EditorGUILayout.HelpBox( $"{_label}: {resolvedItemName} ({currentItemId})", MessageType.Info );
        }

        ///<summary>
        /// 액션 버튼 영역 렌더링
        ///</summary>
        private void DrawActionButtonSection( CShopDefinition _shopDefinition )
        {
            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );
            string validationMessage = ValidateShopDefinition( _shopDefinition );

            if ( string.IsNullOrWhiteSpace( validationMessage ) == false )
            {
                EditorGUILayout.HelpBox( validationMessage, MessageType.Warning );
            }

            EditorGUILayout.BeginHorizontal();

            using ( new EditorGUI.DisabledScope( hasPendingAssetChanges == false || string.IsNullOrWhiteSpace( validationMessage ) == false ) )
            {
                if ( GUILayout.Button( "Save", GUILayout.Height( 32.0f ) ) )
                {
                    SaveShopDefinition( _shopDefinition );
                }
            }

            if ( GUILayout.Button( "Duplicate", GUILayout.Height( 32.0f ) ) )
            {
                DuplicateShopDefinition( _shopDefinition );
            }

            if ( GUILayout.Button( "Ping", GUILayout.Height( 32.0f ) ) )
            {
                EditorGUIUtility.PingObject( _shopDefinition );
                Selection.activeObject = _shopDefinition;
            }

            if ( GUILayout.Button( "Delete", GUILayout.Height( 32.0f ) ) )
            {
                DeleteShopDefinition( _shopDefinition );
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상점 목록 및 아이템 참조 갱신
        ///</summary>
        private void RefreshAll()
        {
            RefreshItemReferenceInfos();
            RefreshShopDefinitionInfos();
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

                ShopItemReferenceInfo itemReferenceInfo = new ShopItemReferenceInfo();
                itemReferenceInfo.itemId = itemDefinition.GetItemId();
                itemReferenceInfo.itemName = itemDefinition.GetItemName();
                itemReferenceInfo.assetPath = assetPath;
                itemReferenceInfoList.Add( itemReferenceInfo );
            }
        }

        ///<summary>
        /// 상점 정의 목록 갱신
        ///</summary>
        private void RefreshShopDefinitionInfos()
        {
            shopDefinitionInfoList.Clear();

            if ( AssetDatabase.IsValidFolder( ShopDefinitionFolderPath ) )
            {
                string[] assetGuidArray = AssetDatabase.FindAssets( "t:CShopDefinition", new string[] { ShopDefinitionFolderPath } );
                Array.Sort( assetGuidArray, CompareAssetGuid );

                for ( int index = 0; index < assetGuidArray.Length; index++ )
                {
                    string assetGuid = assetGuidArray[ index ];
                    string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                    CShopDefinition shopDefinition = AssetDatabase.LoadAssetAtPath<CShopDefinition>( assetPath );

                    if ( shopDefinition == null )
                    {
                        continue;
                    }

                    ShopDefinitionInfo shopInfo = new ShopDefinitionInfo();
                    shopInfo.shopName = string.IsNullOrWhiteSpace( shopDefinition.GetShopName() ) ? shopDefinition.name : shopDefinition.GetShopName();
                    shopInfo.shopId = shopDefinition.GetShopId();
                    shopInfo.assetPath = assetPath;
                    List<CShopEntryData> entryDataList = shopDefinition.GetShopEntryDataList();
                    shopInfo.entryCount = entryDataList != null ? entryDataList.Count : 0;
                    shopDefinitionInfoList.Add( shopInfo );
                }
            }

            if ( shopDefinitionInfoList.Count == 0 )
            {
                selectedShopIndex = -1;
                hasPendingAssetChanges = false;
                SetStatus( "상점 정의를 찾지 못했습니다.", MessageType.Warning );
                return;
            }

            if ( selectedShopIndex < 0 || selectedShopIndex >= shopDefinitionInfoList.Count )
            {
                selectedShopIndex = 0;
            }

            hasPendingAssetChanges = false;
            SetStatus( $"상점 정의 {shopDefinitionInfoList.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 신규 상점 정의 생성
        ///</summary>
        private void CreateNewShopDefinition()
        {
            EnsureShopDefinitionFolderExists();
            string nextAssetPath = AssetDatabase.GenerateUniqueAssetPath( $"{ShopDefinitionFolderPath}/ShopDefinition.asset" );
            string createdName = Path.GetFileNameWithoutExtension( nextAssetPath );
            CShopDefinition createdShopDefinition = ScriptableObject.CreateInstance<CShopDefinition>();
            createdShopDefinition.SetShopId( createdName.ToUpperInvariant() );
            createdShopDefinition.SetShopName( createdName );
            AssetDatabase.CreateAsset( createdShopDefinition, nextAssetPath );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshShopDefinitionInfos();
            SelectShopByAssetPath( nextAssetPath );
            Selection.activeObject = createdShopDefinition;
            hasPendingAssetChanges = false;
            SetStatus( $"상점 정의를 생성했습니다: {nextAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 상점 정의 저장 처리
        ///</summary>
        private void SaveShopDefinition( CShopDefinition _shopDefinition )
        {
            if ( _shopDefinition == null )
            {
                return;
            }

            EditorUtility.SetDirty( _shopDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshShopDefinitionInfos();
            hasPendingAssetChanges = false;
            string assetPath = AssetDatabase.GetAssetPath( _shopDefinition );
            SetStatus( $"상점 정의를 저장했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 상점 정의 복제
        ///</summary>
        private void DuplicateShopDefinition( CShopDefinition _shopDefinition )
        {
            if ( _shopDefinition == null )
            {
                return;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( _shopDefinition );
            string duplicatedAssetPath = AssetDatabase.GenerateUniqueAssetPath( sourceAssetPath );
            bool isCopied = AssetDatabase.CopyAsset( sourceAssetPath, duplicatedAssetPath );

            if ( isCopied == false )
            {
                SetStatus( "상점 정의 복제에 실패했습니다.", MessageType.Error );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CShopDefinition duplicatedShopDefinition = AssetDatabase.LoadAssetAtPath<CShopDefinition>( duplicatedAssetPath );

            if ( duplicatedShopDefinition != null )
            {
                string duplicatedName = Path.GetFileNameWithoutExtension( duplicatedAssetPath );
                duplicatedShopDefinition.SetShopId( $"{duplicatedName.ToUpperInvariant()}_COPY" );
                duplicatedShopDefinition.SetShopName( $"{duplicatedShopDefinition.GetShopName()} Copy" );
                EditorUtility.SetDirty( duplicatedShopDefinition );
                AssetDatabase.SaveAssets();
            }

            RefreshShopDefinitionInfos();
            SelectShopByAssetPath( duplicatedAssetPath );
            hasPendingAssetChanges = false;
            SetStatus( $"상점 정의를 복제했습니다: {duplicatedAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 상점 정의 삭제
        ///</summary>
        private void DeleteShopDefinition( CShopDefinition _shopDefinition )
        {
            if ( _shopDefinition == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _shopDefinition );
            bool isConfirmed = EditorUtility.DisplayDialog( "Delete Shop Definition", $"{assetPath}\n삭제하시겠습니까?", "Delete", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            bool isDeleted = AssetDatabase.DeleteAsset( assetPath );

            if ( isDeleted == false )
            {
                SetStatus( "상점 정의 삭제에 실패했습니다.", MessageType.Error );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshShopDefinitionInfos();
            hasPendingAssetChanges = false;
            SetStatus( $"상점 정의를 삭제했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 상점 항목 추가
        ///</summary>
        private void AddShopEntry( SerializedProperty _shopEntryDataListProperty )
        {
            if ( _shopEntryDataListProperty == null )
            {
                return;
            }

            int nextIndex = _shopEntryDataListProperty.arraySize;
            _shopEntryDataListProperty.InsertArrayElementAtIndex( nextIndex );
            SerializedProperty createdEntryProperty = _shopEntryDataListProperty.GetArrayElementAtIndex( nextIndex );
            InitializeShopEntryProperty( createdEntryProperty );
            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 상점 항목 초기값 설정
        ///</summary>
        private void InitializeShopEntryProperty( SerializedProperty _entryProperty )
        {
            if ( _entryProperty == null )
            {
                return;
            }

            SerializedProperty itemIdProperty = _entryProperty.FindPropertyRelative( "itemId" );
            SerializedProperty itemCountProperty = _entryProperty.FindPropertyRelative( "itemCount" );
            SerializedProperty priceItemIdProperty = _entryProperty.FindPropertyRelative( "priceItemId" );
            SerializedProperty priceAmountProperty = _entryProperty.FindPropertyRelative( "priceAmount" );

            if ( itemIdProperty != null )
            {
                itemIdProperty.stringValue = string.Empty;
            }

            if ( itemCountProperty != null )
            {
                itemCountProperty.intValue = 1;
            }

            if ( priceItemIdProperty != null )
            {
                priceItemIdProperty.stringValue = DefaultPriceItemId;
            }

            if ( priceAmountProperty != null )
            {
                priceAmountProperty.intValue = 1;
            }
        }

        ///<summary>
        /// 상점 항목 15칸 확장
        ///</summary>
        private void ExpandShopEntriesToCapacity( SerializedProperty _shopEntryDataListProperty )
        {
            if ( _shopEntryDataListProperty == null )
            {
                return;
            }

            while ( _shopEntryDataListProperty.arraySize < ShopSlotCapacity )
            {
                AddShopEntry( _shopEntryDataListProperty );
            }
        }

        ///<summary>
        /// 상점 항목 순서 이동
        ///</summary>
        private void MoveShopEntry( SerializedProperty _shopEntryDataListProperty, int _entryIndex, int _direction )
        {
            if ( _shopEntryDataListProperty == null )
            {
                return;
            }

            int targetIndex = _entryIndex + _direction;

            if ( targetIndex < 0 || targetIndex >= _shopEntryDataListProperty.arraySize )
            {
                return;
            }

            _shopEntryDataListProperty.MoveArrayElement( _entryIndex, targetIndex );
            hasPendingAssetChanges = true;
        }

        ///<summary>
        /// 판매 항목 요약 문구 구성
        ///</summary>
        private string BuildEntrySummaryText( SerializedProperty _itemIdProperty, SerializedProperty _itemCountProperty, SerializedProperty _priceItemIdProperty, SerializedProperty _priceAmountProperty )
        {
            string itemId = _itemIdProperty != null ? _itemIdProperty.stringValue.Trim() : string.Empty;
            int itemCount = _itemCountProperty != null ? Mathf.Max( 1, _itemCountProperty.intValue ) : 1;
            string priceItemId = _priceItemIdProperty != null ? _priceItemIdProperty.stringValue.Trim() : DefaultPriceItemId;
            int priceAmount = _priceAmountProperty != null ? Mathf.Max( 0, _priceAmountProperty.intValue ) : 0;
            string itemName = ResolveItemName( itemId );
            string priceItemName = ResolveItemName( priceItemId );
            string result = $"판매: {itemName} x{itemCount}\n가격: {priceItemName} x{priceAmount}";
            return result;
        }

        ///<summary>
        /// 상점 정의 검증
        ///</summary>
        private string ValidateShopDefinition( CShopDefinition _shopDefinition )
        {
            if ( _shopDefinition == null )
            {
                return "상점 정의를 찾지 못했습니다.";
            }

            if ( string.IsNullOrWhiteSpace( _shopDefinition.GetShopId() ) )
            {
                return "Shop Id를 입력하세요.";
            }

            if ( string.IsNullOrWhiteSpace( _shopDefinition.GetShopName() ) )
            {
                return "Shop Name을 입력하세요.";
            }

            List<CShopEntryData> entryDataList = _shopDefinition.GetShopEntryDataList();

            if ( entryDataList == null || entryDataList.Count == 0 )
            {
                return "판매 품목을 하나 이상 추가하세요.";
            }

            for ( int index = 0; index < entryDataList.Count; index++ )
            {
                CShopEntryData entryData = entryDataList[ index ];

                if ( entryData == null )
                {
                    return $"Entry {index + 1} 데이터가 비어 있습니다.";
                }

                if ( string.IsNullOrWhiteSpace( entryData.GetItemId() ) )
                {
                    return $"Entry {index + 1} 판매 아이템 ID를 입력하세요.";
                }

                if ( entryData.GetItemCount() <= 0 )
                {
                    return $"Entry {index + 1} 판매 수량을 확인하세요.";
                }

                if ( string.IsNullOrWhiteSpace( entryData.GetPriceItemId() ) )
                {
                    return $"Entry {index + 1} 구매 재화 ID를 입력하세요.";
                }

                if ( entryData.GetPriceAmount() < 0 )
                {
                    return $"Entry {index + 1} 가격 수량을 확인하세요.";
                }
            }

            return string.Empty;
        }

        ///<summary>
        /// 아이템 옵션 라벨 배열 반환
        ///</summary>
        private string[] BuildItemOptionLabelArray()
        {
            List<string> optionLabelList = new List<string>();
            optionLabelList.Add( "Manual Input" );

            for ( int index = 0; index < itemReferenceInfoList.Count; index++ )
            {
                ShopItemReferenceInfo itemReferenceInfo = itemReferenceInfoList[ index ];

                if ( itemReferenceInfo == null )
                {
                    continue;
                }

                optionLabelList.Add( $"{itemReferenceInfo.itemName} ({itemReferenceInfo.itemId})" );
            }

            string[] result = optionLabelList.ToArray();
            return result;
        }

        ///<summary>
        /// 아이템 옵션 인덱스 반환
        ///</summary>
        private int ResolveItemOptionIndex( string _itemId )
        {
            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                return 0;
            }

            string normalizedItemId = _itemId.Trim();

            for ( int index = 0; index < itemReferenceInfoList.Count; index++ )
            {
                ShopItemReferenceInfo itemReferenceInfo = itemReferenceInfoList[ index ];

                if ( itemReferenceInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemReferenceInfo.itemId, normalizedItemId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                return index + 1;
            }

            return 0;
        }

        ///<summary>
        /// 옵션 인덱스 기준 아이템 ID 반환
        ///</summary>
        private string ResolveItemIdByOptionIndex( int _optionIndex, string _fallbackItemId )
        {
            if ( _optionIndex <= 0 )
            {
                string fallbackItemId = string.IsNullOrWhiteSpace( _fallbackItemId ) ? string.Empty : _fallbackItemId.Trim();
                return fallbackItemId;
            }

            int itemReferenceIndex = _optionIndex - 1;

            if ( itemReferenceIndex < 0 || itemReferenceIndex >= itemReferenceInfoList.Count )
            {
                return string.Empty;
            }

            ShopItemReferenceInfo itemReferenceInfo = itemReferenceInfoList[ itemReferenceIndex ];
            string result = itemReferenceInfo != null ? itemReferenceInfo.itemId : string.Empty;
            return result;
        }

        ///<summary>
        /// 아이템 표시 이름 반환
        ///</summary>
        private string ResolveItemName( string _itemId )
        {
            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                return string.Empty;
            }

            string normalizedItemId = _itemId.Trim();

            for ( int index = 0; index < itemReferenceInfoList.Count; index++ )
            {
                ShopItemReferenceInfo itemReferenceInfo = itemReferenceInfoList[ index ];

                if ( itemReferenceInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemReferenceInfo.itemId, normalizedItemId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                string resolvedItemName = string.IsNullOrWhiteSpace( itemReferenceInfo.itemName ) ? normalizedItemId : itemReferenceInfo.itemName;
                return resolvedItemName;
            }

            return normalizedItemId;
        }

        ///<summary>
        /// 상점 정의 폴더 생성 보장
        ///</summary>
        private void EnsureShopDefinitionFolderExists()
        {
            if ( AssetDatabase.IsValidFolder( ShopDefinitionFolderPath ) )
            {
                return;
            }

            string[] folderSegmentArray = ShopDefinitionFolderPath.Split( '/' );
            string currentPath = folderSegmentArray[ 0 ];

            for ( int index = 1; index < folderSegmentArray.Length; index++ )
            {
                string folderName = folderSegmentArray[ index ];
                string combinedPath = $"{currentPath}/{folderName}";

                if ( AssetDatabase.IsValidFolder( combinedPath ) == false )
                {
                    AssetDatabase.CreateFolder( currentPath, folderName );
                }

                currentPath = combinedPath;
            }
        }

        ///<summary>
        /// 에셋 GUID 비교
        ///</summary>
        private int CompareAssetGuid( string _leftGuid, string _rightGuid )
        {
            string leftPath = AssetDatabase.GUIDToAssetPath( _leftGuid );
            string rightPath = AssetDatabase.GUIDToAssetPath( _rightGuid );
            int result = string.Compare( leftPath, rightPath, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 필터 적용 상점 목록 반환
        ///</summary>
        private List<ShopDefinitionInfo> GetFilteredShopDefinitionInfos()
        {
            List<ShopDefinitionInfo> filteredInfoList = new List<ShopDefinitionInfo>();

            for ( int index = 0; index < shopDefinitionInfoList.Count; index++ )
            {
                ShopDefinitionInfo shopInfo = shopDefinitionInfoList[ index ];

                if ( IsMatchedSearch( shopInfo ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( shopInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 검색 일치 여부 반환
        ///</summary>
        private bool IsMatchedSearch( ShopDefinitionInfo _shopInfo )
        {
            if ( _shopInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();
            bool isShopNameMatched = _shopInfo.shopName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;

            if ( isShopNameMatched )
            {
                return true;
            }

            bool isShopIdMatched = _shopInfo.shopId.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return isShopIdMatched;
        }

        ///<summary>
        /// 현재 선택 상점 정의 반환
        ///</summary>
        private CShopDefinition GetSelectedShopDefinition()
        {
            if ( selectedShopIndex < 0 || selectedShopIndex >= shopDefinitionInfoList.Count )
            {
                return null;
            }

            ShopDefinitionInfo shopInfo = shopDefinitionInfoList[ selectedShopIndex ];

            if ( shopInfo == null )
            {
                return null;
            }

            CShopDefinition result = AssetDatabase.LoadAssetAtPath<CShopDefinition>( shopInfo.assetPath );
            return result;
        }

        ///<summary>
        /// 에셋 경로 기준 선택 처리
        ///</summary>
        private void SelectShopByAssetPath( string _assetPath )
        {
            for ( int index = 0; index < shopDefinitionInfoList.Count; index++ )
            {
                ShopDefinitionInfo shopInfo = shopDefinitionInfoList[ index ];

                if ( shopInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( shopInfo.assetPath, _assetPath, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                selectedShopIndex = index;
                isPendingFocusToSelection = true;
                Repaint();
                return;
            }
        }

        ///<summary>
        /// 상점 선택 처리
        ///</summary>
        private void SelectShopByIndex( int _sourceIndex, int _filteredIndex, int _filteredItemCount )
        {
            if ( _sourceIndex < 0 || _sourceIndex >= shopDefinitionInfoList.Count )
            {
                return;
            }

            selectedShopIndex = _sourceIndex;
            isPendingFocusToSelection = true;
            EnsureSelectionVisibleByIndex( _filteredIndex, _filteredItemCount );
            Repaint();
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

            if ( itemTop < shopListScrollPosition.y )
            {
                shopListScrollPosition.y = itemTop;
            }
            else if ( itemBottom > shopListScrollPosition.y + ListViewHeight )
            {
                shopListScrollPosition.y = itemBottom - ListViewHeight;
            }

            shopListScrollPosition.y = Mathf.Clamp( shopListScrollPosition.y, 0.0f, maxScrollY );
        }

        ///<summary>
        /// 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildShopControlName( int _sourceIndex )
        {
            string result = $"ShopDefinitionItem_{_sourceIndex}";
            return result;
        }

        ///<summary>
        /// 키보드 선택 이동 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            Event currentEvent = Event.current;

            if ( currentEvent == null )
            {
                return;
            }

            if ( EditorGUIUtility.editingTextField )
            {
                return;
            }

            if ( currentEvent.type != EventType.KeyDown )
            {
                return;
            }

            if ( currentEvent.keyCode == KeyCode.DownArrow )
            {
                MoveSelectionInFilteredList( 1 );
                currentEvent.Use();
                return;
            }

            if ( currentEvent.keyCode == KeyCode.UpArrow )
            {
                MoveSelectionInFilteredList( -1 );
                currentEvent.Use();
            }
        }

        ///<summary>
        /// 필터 목록 선택 이동
        ///</summary>
        private void MoveSelectionInFilteredList( int _direction )
        {
            List<ShopDefinitionInfo> filteredInfoList = GetFilteredShopDefinitionInfos();

            if ( filteredInfoList.Count == 0 )
            {
                return;
            }

            int filteredSelectedIndex = 0;
            ShopDefinitionInfo selectedInfo = null;

            if ( selectedShopIndex >= 0 && selectedShopIndex < shopDefinitionInfoList.Count )
            {
                selectedInfo = shopDefinitionInfoList[ selectedShopIndex ];
            }

            if ( selectedInfo != null )
            {
                int resolvedIndex = filteredInfoList.IndexOf( selectedInfo );

                if ( resolvedIndex >= 0 )
                {
                    filteredSelectedIndex = resolvedIndex;
                }
            }

            int lastIndex = filteredInfoList.Count - 1;
            int nextFilteredIndex = Mathf.Clamp( filteredSelectedIndex + _direction, 0, lastIndex );
            ShopDefinitionInfo nextInfo = filteredInfoList[ nextFilteredIndex ];
            int sourceIndex = shopDefinitionInfoList.IndexOf( nextInfo );
            SelectShopByIndex( sourceIndex, nextFilteredIndex, filteredInfoList.Count );
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
