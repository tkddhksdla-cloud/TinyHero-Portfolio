using System;
using System.Collections.Generic;
using System.IO;
using LayerLab.ArtMakerUnity;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 아이템 정의 목록 정보
    ///</summary>
    [Serializable]
    public sealed class ItemDefinitionInfo
    {
        public string itemName;
        public string itemId;
        public string assetPath;
        public eItemType itemType;
    }

    ///<summary>
    /// 아이템 정의 에디터 윈도우
    ///</summary>
    public sealed class ItemDefinitionEditorWindow : EditorWindow
    {
        private const string ItemDefinitionFolderPath = "Assets/Resources/Data/Item/Definitions";
        private const string PlayerPrefabAssetPath = "Assets/Resources/Prefabs/Character/Player/Player.prefab";
        private const string DefaultSellPriceItemId = "ITEM_CURRENCY_GOLD";
        private const float ListViewHeight = 480.0f;
        private const float ListItemHeight = 42.0f;
        private const float ListItemSpacing = 4.0f;
        private const int PreviewSize = 180;
        private const int DefaultMaxStackCount = 9999;

        [SerializeField] private List<ItemDefinitionInfo> itemDefinitionInfoList = new List<ItemDefinitionInfo>();
        [SerializeField] private int selectedItemIndex = -1;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private int selectedTypeFilterIndex;

        private Vector2 itemListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "아이템 정의 목록을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private bool hasPendingAssetChanges;

        private static readonly string[] ItemTypeFilterOptionArray =
        {
            "ALL",
            eItemType.EQUIPMENT.ToString(),
            eItemType.CONSUMABLE.ToString(),
            eItemType.CURRENCY.ToString(),
            eItemType.MATERIAL.ToString(),
            eItemType.QUEST_ITEM.ToString()
        };

        ///<summary>
        /// 아이템 정의 에디터 윈도우 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Item Definition Editor" )]
        private static void ShowWindow()
        {
            ItemDefinitionEditorWindow window = GetWindow<ItemDefinitionEditorWindow>();
            window.titleContent = new GUIContent( "Item Definition Editor" );
            window.minSize = new Vector2( 1180.0f, 760.0f );
            window.Show();
        }

        ///<summary>
        /// 아이템 정의 에디터 창 열기
        ///</summary>
        public static void OpenWindow()
        {
            ShowWindow();
        }

        ///<summary>
        /// 에디터 윈도우 초기화
        ///</summary>
        private void OnEnable()
        {
            RefreshItemDefinitionInfos();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Item Definition Editor", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "아이템 정의를 검색, 생성, 복제, 삭제하고 우측 패널에서 즉시 수정합니다.", MessageType.None );
            EditorGUILayout.Space();
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawItemListSection();
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

            int updatedTypeFilterIndex = EditorGUILayout.Popup( "Type", selectedTypeFilterIndex, ItemTypeFilterOptionArray, GUILayout.Width( 260.0f ) );

            if ( updatedTypeFilterIndex != selectedTypeFilterIndex )
            {
                selectedTypeFilterIndex = updatedTypeFilterIndex;
            }

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 100.0f ) ) )
            {
                RefreshItemDefinitionInfos();
            }

            if ( GUILayout.Button( "Create", GUILayout.Width( 100.0f ) ) )
            {
                CreateNewItemDefinition();
            }

            if ( GUILayout.Button( "Generate Samples", GUILayout.Width( 140.0f ) ) )
            {
                GenerateSampleItems();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 아이템 목록 영역 렌더링
        ///</summary>
        private void DrawItemListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( 400.0f ) );
            EditorGUILayout.LabelField( "Item Definitions", EditorStyles.boldLabel );
            List<ItemDefinitionInfo> filteredInfoList = GetFilteredItemDefinitionInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfoList.Count}개", MessageType.None );
            itemListScrollPosition = EditorGUILayout.BeginScrollView( itemListScrollPosition, GUILayout.Height( ListViewHeight ) );

            for ( int index = 0; index < filteredInfoList.Count; index++ )
            {
                ItemDefinitionInfo itemInfo = filteredInfoList[ index ];
                DrawItemListItem( itemInfo, filteredInfoList.Count, index );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 아이템 목록 항목 렌더링
        ///</summary>
        private void DrawItemListItem( ItemDefinitionInfo _itemInfo, int _filteredItemCount, int _filteredIndex )
        {
            if ( _itemInfo == null )
            {
                return;
            }

            int sourceIndex = itemDefinitionInfoList.IndexOf( _itemInfo );
            bool isSelected = sourceIndex == selectedItemIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string controlName = BuildItemControlName( sourceIndex );
            string buttonLabel = $"[ {_itemInfo.itemType} ] {_itemInfo.itemName}\n{_itemInfo.itemId}";
            GUI.SetNextControlName( controlName );
            bool wasClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( wasClicked )
            {
                SelectItemByIndex( sourceIndex, _filteredIndex, _filteredItemCount );
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
        /// 아이템 편집 영역 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            CItemDefinition selectedItemDefinition = GetSelectedItemDefinition();

            if ( selectedItemDefinition == null )
            {
                EditorGUILayout.HelpBox( "편집할 아이템 정의를 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawPreviewSection( selectedItemDefinition );
            EditorGUILayout.Space();
            DrawAssetInfoSection( selectedItemDefinition );
            EditorGUILayout.Space();
            DrawItemPropertySection( selectedItemDefinition );
            EditorGUILayout.Space();
            DrawActionButtonSection( selectedItemDefinition );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 미리보기 영역 렌더링
        ///</summary>
        private void DrawPreviewSection( CItemDefinition _itemDefinition )
        {
            Texture previewTexture = AssetPreview.GetAssetPreview( _itemDefinition );

            if ( previewTexture == null )
            {
                previewTexture = AssetPreview.GetMiniThumbnail( _itemDefinition );
                Repaint();
            }

            Rect previewRect = GUILayoutUtility.GetRect( PreviewSize, PreviewSize, GUILayout.ExpandWidth( false ) );
            EditorGUI.DrawRect( previewRect, new Color( 0.16f, 0.16f, 0.16f, 1.0f ) );

            if ( previewTexture != null )
            {
                GUI.DrawTexture( previewRect, previewTexture, ScaleMode.ScaleToFit );
            }
        }

        ///<summary>
        /// 에셋 정보 영역 렌더링
        ///</summary>
        private void DrawAssetInfoSection( CItemDefinition _itemDefinition )
        {
            string assetPath = AssetDatabase.GetAssetPath( _itemDefinition );
            EditorGUILayout.LabelField( "Asset Info", EditorStyles.boldLabel );
            EditorGUILayout.LabelField( "Asset Name", _itemDefinition.name );
            EditorGUILayout.LabelField( "Asset Path", assetPath );
        }

        ///<summary>
        /// 아이템 속성 영역 렌더링
        ///</summary>
        private void DrawItemPropertySection( CItemDefinition _itemDefinition )
        {
            SerializedObject serializedObject = new SerializedObject( _itemDefinition );
            serializedObject.Update();
            SerializedProperty itemIdProperty = serializedObject.FindProperty( "itemId" );
            SerializedProperty itemNameProperty = serializedObject.FindProperty( "itemName" );
            SerializedProperty itemTypeProperty = serializedObject.FindProperty( "itemType" );
            SerializedProperty descriptionProperty = serializedObject.FindProperty( "description" );
            SerializedProperty iconSpriteProperty = serializedObject.FindProperty( "iconSprite" );
            SerializedProperty worldDropPrefabProperty = serializedObject.FindProperty( "worldDropPrefab" );
            SerializedProperty isStackableProperty = serializedObject.FindProperty( "isStackable" );
            SerializedProperty maxStackCountProperty = serializedObject.FindProperty( "maxStackCountValue" );
            SerializedProperty sellPriceItemIdProperty = serializedObject.FindProperty( "sellPriceItemId" );
            SerializedProperty sellPriceProperty = serializedObject.FindProperty( "sellPriceValue" );
            SerializedProperty equipmentTypeProperty = serializedObject.FindProperty( "equipmentType" );
            SerializedProperty consumableTypeProperty = serializedObject.FindProperty( "consumableType" );
            SerializedProperty linkedSkillIdProperty = serializedObject.FindProperty( "linkedSkillId" );
            SerializedProperty skillPointGrantAmountProperty = serializedObject.FindProperty( "skillPointGrantAmount" );
            SerializedProperty randomBoxRewardTableProperty = serializedObject.FindProperty( "randomBoxRewardTable" );
            SerializedProperty equipmentStatBonusProperty = serializedObject.FindProperty( "equipmentStatBonus" );
            SerializedProperty equipmentPartsTypeProperty = serializedObject.FindProperty( "equipmentPartsType" );
            SerializedProperty equipmentPartsIndexProperty = serializedObject.FindProperty( "equipmentPartsIndex" );

            EditorGUILayout.LabelField( "Item Settings", EditorStyles.boldLabel );

            if ( itemIdProperty != null )
            {
                EditorGUILayout.PropertyField( itemIdProperty );
            }

            if ( itemNameProperty != null )
            {
                EditorGUILayout.PropertyField( itemNameProperty );
            }

            if ( itemTypeProperty != null )
            {
                EditorGUILayout.PropertyField( itemTypeProperty );
            }

            if ( descriptionProperty != null )
            {
                EditorGUILayout.PropertyField( descriptionProperty );
            }

            if ( iconSpriteProperty != null )
            {
                EditorGUILayout.PropertyField( iconSpriteProperty );
            }

            if ( worldDropPrefabProperty != null )
            {
                EditorGUILayout.PropertyField( worldDropPrefabProperty );
            }

            if ( isStackableProperty != null )
            {
                EditorGUILayout.PropertyField( isStackableProperty );
            }

            if ( maxStackCountProperty != null )
            {
                bool isStackable = isStackableProperty != null && isStackableProperty.boolValue;

                using ( new EditorGUI.DisabledScope( isStackable == false ) )
                {
                    EditorGUILayout.PropertyField( maxStackCountProperty );
                }

                if ( isStackable == false )
                {
                    maxStackCountProperty.longValue = 1L;
                }
            }

            if ( sellPriceItemIdProperty != null )
            {
                EditorGUILayout.PropertyField( sellPriceItemIdProperty );

                if ( string.IsNullOrWhiteSpace( sellPriceItemIdProperty.stringValue ) )
                {
                    sellPriceItemIdProperty.stringValue = DefaultSellPriceItemId;
                }
            }

            if ( sellPriceProperty != null )
            {
                EditorGUILayout.PropertyField( sellPriceProperty );

                if ( sellPriceProperty.longValue < 0L )
                {
                    sellPriceProperty.longValue = 0L;
                }
            }

            bool isEquipmentItem = itemTypeProperty != null && itemTypeProperty.enumValueIndex == ( int )eItemType.EQUIPMENT;
            bool isConsumableItem = itemTypeProperty != null && itemTypeProperty.enumValueIndex == ( int )eItemType.CONSUMABLE;

            if ( isConsumableItem )
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField( "Consumable Settings", EditorStyles.boldLabel );

                if ( consumableTypeProperty != null )
                {
                    EditorGUILayout.PropertyField( consumableTypeProperty );
                }

                bool isSkillBook = consumableTypeProperty != null && consumableTypeProperty.enumValueIndex == ( int )eConsumableType.SKILL_BOOK;
                bool isRandomBox = consumableTypeProperty != null && consumableTypeProperty.enumValueIndex == ( int )eConsumableType.RANDOM_BOX;
                bool isSkillPointBook = consumableTypeProperty != null && consumableTypeProperty.enumValueIndex == ( int )eConsumableType.SKILL_POINT_BOOK;

                if ( linkedSkillIdProperty != null )
                {
                    using ( new EditorGUI.DisabledScope( isSkillBook == false ) )
                    {
                        EditorGUILayout.PropertyField( linkedSkillIdProperty );
                    }

                    if ( isSkillBook == false )
                    {
                        linkedSkillIdProperty.stringValue = string.Empty;
                    }
                }

                if ( skillPointGrantAmountProperty != null )
                {
                    using ( new EditorGUI.DisabledScope( isSkillPointBook == false ) )
                    {
                        EditorGUILayout.PropertyField( skillPointGrantAmountProperty );
                    }

                    skillPointGrantAmountProperty.intValue = isSkillPointBook ? Mathf.Max( 1, skillPointGrantAmountProperty.intValue ) : 0;
                }

                if ( randomBoxRewardTableProperty != null )
                {
                    using ( new EditorGUI.DisabledScope( isRandomBox == false ) )
                    {
                        EditorGUILayout.PropertyField( randomBoxRewardTableProperty );
                    }

                    if ( isRandomBox == false )
                    {
                        randomBoxRewardTableProperty.objectReferenceValue = null;
                    }

                    if ( isRandomBox && GUILayout.Button( "Open Random Box Editor", GUILayout.Height( 26.0f ) ) )
                    {
                        RandomBoxRewardTableEditorWindow.OpenWindow();
                    }
                }
            }

            if ( isEquipmentItem )
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField( "Equipment Settings", EditorStyles.boldLabel );

                if ( equipmentTypeProperty != null )
                {
                    EditorGUILayout.PropertyField( equipmentTypeProperty );
                }

                if ( equipmentStatBonusProperty != null )
                {
                    EditorGUILayout.PropertyField( equipmentStatBonusProperty, true );
                }

                if ( equipmentPartsTypeProperty != null )
                {
                    EditorGUILayout.PropertyField( equipmentPartsTypeProperty );
                }

                if ( equipmentPartsIndexProperty != null )
                {
                    EditorGUILayout.PropertyField( equipmentPartsIndexProperty );
                }

                DrawEquipmentPartsAutoAssignSection( serializedObject, iconSpriteProperty, equipmentPartsTypeProperty, equipmentPartsIndexProperty );
            }

            bool hasModifiedProperties = serializedObject.ApplyModifiedProperties();

            if ( hasModifiedProperties )
            {
                EditorUtility.SetDirty( _itemDefinition );
                hasPendingAssetChanges = true;
            }
        }

        ///<summary>
        /// 장비 외형 자동 할당 영역 렌더링
        ///</summary>
        private void DrawEquipmentPartsAutoAssignSection( SerializedObject _serializedObject, SerializedProperty _iconSpriteProperty, SerializedProperty _equipmentPartsTypeProperty, SerializedProperty _equipmentPartsIndexProperty )
        {
            if ( _serializedObject == null || _iconSpriteProperty == null || _equipmentPartsTypeProperty == null || _equipmentPartsIndexProperty == null )
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Auto Assign Parts", GUILayout.Height( 26.0f ) ) )
            {
                bool didAssign = TryAutoAssignEquipmentParts( _serializedObject, _iconSpriteProperty, _equipmentPartsTypeProperty, _equipmentPartsIndexProperty, out string resultMessage );
                SetStatus( resultMessage, didAssign ? MessageType.Info : MessageType.Warning );

                if ( didAssign )
                {
                    hasPendingAssetChanges = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 액션 버튼 영역 렌더링
        ///</summary>
        private void DrawActionButtonSection( CItemDefinition _itemDefinition )
        {
            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );
            EditorGUILayout.BeginHorizontal();

            using ( new EditorGUI.DisabledScope( hasPendingAssetChanges == false ) )
            {
                if ( GUILayout.Button( "Save", GUILayout.Height( 32.0f ) ) )
                {
                    SaveItemDefinition( _itemDefinition );
                }
            }

            if ( GUILayout.Button( "Duplicate", GUILayout.Height( 32.0f ) ) )
            {
                DuplicateItemDefinition( _itemDefinition );
            }

            if ( GUILayout.Button( "Generate Drop Prefab", GUILayout.Height( 32.0f ) ) )
            {
                GenerateDropPrefabForItem( _itemDefinition );
            }

            if ( GUILayout.Button( "Ping", GUILayout.Height( 32.0f ) ) )
            {
                EditorGUIUtility.PingObject( _itemDefinition );
                Selection.activeObject = _itemDefinition;
            }

            if ( GUILayout.Button( "Delete", GUILayout.Height( 32.0f ) ) )
            {
                DeleteItemDefinition( _itemDefinition );
            }

            EditorGUILayout.EndHorizontal();

            GameObject worldDropPrefab = _itemDefinition.GetWorldDropPrefab();

            if ( worldDropPrefab != null )
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                if ( GUILayout.Button( "Ping Drop Prefab", GUILayout.Height( 28.0f ) ) )
                {
                    EditorGUIUtility.PingObject( worldDropPrefab );
                    Selection.activeObject = worldDropPrefab;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        ///<summary>
        /// 아이템 드랍 프리팹 생성 처리
        ///</summary>
        private void GenerateDropPrefabForItem( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return;
            }

            string result = CItemWorldDropBootstrapper.GenerateWorldDropPrefabForItem( _itemDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus( result, MessageType.Info );
        }

        ///<summary>
        /// 아이템 정의 목록 갱신
        ///</summary>
        private void RefreshItemDefinitionInfos()
        {
            itemDefinitionInfoList.Clear();
            EnsureItemDefinitionFolderExists();
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

                ItemDefinitionInfo itemInfo = new ItemDefinitionInfo();
                itemInfo.itemName = string.IsNullOrWhiteSpace( itemDefinition.GetItemName() ) ? itemDefinition.name : itemDefinition.GetItemName();
                itemInfo.itemId = itemDefinition.GetItemId();
                itemInfo.assetPath = assetPath;
                itemInfo.itemType = itemDefinition.GetItemType();
                itemDefinitionInfoList.Add( itemInfo );
            }

            if ( itemDefinitionInfoList.Count == 0 )
            {
                selectedItemIndex = -1;
                SetStatus( "아이템 정의가 없습니다. Create 또는 Generate Samples를 사용하세요.", MessageType.Warning );
                return;
            }

            if ( selectedItemIndex < 0 || selectedItemIndex >= itemDefinitionInfoList.Count )
            {
                selectedItemIndex = 0;
            }

            SetStatus( $"아이템 정의 {itemDefinitionInfoList.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 새 아이템 정의 생성
        ///</summary>
        private void CreateNewItemDefinition()
        {
            EnsureItemDefinitionFolderExists();
            string nextAssetPath = AssetDatabase.GenerateUniqueAssetPath( $"{ItemDefinitionFolderPath}/Item_NewItem.asset" );
            string assetFileName = Path.GetFileNameWithoutExtension( nextAssetPath );
            CItemDefinition createdItemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
            createdItemDefinition.Configure( assetFileName.ToUpperInvariant(), assetFileName, eItemType.CONSUMABLE, string.Empty, null, true, DefaultMaxStackCount );
            createdItemDefinition.SetSellPriceItemId( DefaultSellPriceItemId );
            AssetDatabase.CreateAsset( createdItemDefinition, nextAssetPath );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshItemDefinitionInfos();
            SelectItemByAssetPath( nextAssetPath );
            Selection.activeObject = createdItemDefinition;
            hasPendingAssetChanges = false;
            SetStatus( $"아이템 정의를 생성했습니다: {nextAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 샘플 아이템 생성 실행
        ///</summary>
        private void GenerateSampleItems()
        {
            string result = CItemAssetBootstrapper.GenerateSampleItemAssets();
            RefreshItemDefinitionInfos();
            hasPendingAssetChanges = false;
            SetStatus( result, MessageType.Info );
        }

        ///<summary>
        /// 아이템 정의 저장 처리
        ///</summary>
        private void SaveItemDefinition( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return;
            }

            EditorUtility.SetDirty( _itemDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshItemDefinitionInfos();
            hasPendingAssetChanges = false;
            string assetPath = AssetDatabase.GetAssetPath( _itemDefinition );
            SetStatus( $"아이템 정의를 저장했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 아이템 정의 복제
        ///</summary>
        private void DuplicateItemDefinition( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( _itemDefinition );
            string duplicatedAssetPath = AssetDatabase.GenerateUniqueAssetPath( sourceAssetPath );
            bool isCopied = AssetDatabase.CopyAsset( sourceAssetPath, duplicatedAssetPath );

            if ( isCopied == false )
            {
                SetStatus( "아이템 정의 복제에 실패했습니다.", MessageType.Error );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshItemDefinitionInfos();
            SelectItemByAssetPath( duplicatedAssetPath );
            CItemDefinition duplicatedItemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( duplicatedAssetPath );

            if ( duplicatedItemDefinition != null )
            {
                string duplicatedName = Path.GetFileNameWithoutExtension( duplicatedAssetPath );
                duplicatedItemDefinition.Configure( $"{duplicatedName.ToUpperInvariant()}_COPY", $"{duplicatedItemDefinition.GetItemName()} Copy", duplicatedItemDefinition.GetItemType(), duplicatedItemDefinition.GetDescription(), duplicatedItemDefinition.GetIconSprite(), duplicatedItemDefinition.IsStackable(), duplicatedItemDefinition.GetMaxStackCount(), duplicatedItemDefinition.GetEquipmentType(), duplicatedItemDefinition.GetConsumableType(), duplicatedItemDefinition.GetLinkedSkillId(), duplicatedItemDefinition.GetEquipmentStatBonus(), duplicatedItemDefinition.GetEquipmentPartsType(), duplicatedItemDefinition.GetEquipmentPartsIndex() );
                duplicatedItemDefinition.SetSellPriceItemId( _itemDefinition.GetSellPriceItemId() );
                duplicatedItemDefinition.SetSellPrice( _itemDefinition.GetSellPrice() );
                EditorUtility.SetDirty( duplicatedItemDefinition );
                AssetDatabase.SaveAssets();
            }

            hasPendingAssetChanges = false;
            SetStatus( $"아이템 정의를 복제했습니다: {duplicatedAssetPath}", MessageType.Info );
        }

        ///<summary>
        /// 아이템 정의 삭제
        ///</summary>
        private void DeleteItemDefinition( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _itemDefinition );
            bool isConfirmed = EditorUtility.DisplayDialog( "Delete Item Definition", $"{assetPath}\n삭제하시겠습니까?", "Delete", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            bool isDeleted = AssetDatabase.DeleteAsset( assetPath );

            if ( isDeleted == false )
            {
                SetStatus( "아이템 정의 삭제에 실패했습니다.", MessageType.Error );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshItemDefinitionInfos();
            hasPendingAssetChanges = false;
            SetStatus( $"아이템 정의를 삭제했습니다: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 아이콘 이름 기반 장비 외형 자동 할당
        ///</summary>
        private bool TryAutoAssignEquipmentParts( SerializedObject _serializedObject, SerializedProperty _iconSpriteProperty, SerializedProperty _equipmentPartsTypeProperty, SerializedProperty _equipmentPartsIndexProperty, out string _resultMessage )
        {
            _resultMessage = "아이콘 스프라이트가 비어 있습니다.";

            if ( _serializedObject == null || _iconSpriteProperty == null || _equipmentPartsTypeProperty == null || _equipmentPartsIndexProperty == null )
            {
                _resultMessage = "자동 할당에 필요한 프로퍼티를 찾지 못했습니다.";
                return false;
            }

            Sprite iconSprite = _iconSpriteProperty.objectReferenceValue as Sprite;

            if ( iconSprite == null )
            {
                return false;
            }

            bool isMatched = TryFindEquipmentPartsBySpriteName( iconSprite.name, out PartsType partsType, out int partsIndex );

            if ( isMatched == false )
            {
                _resultMessage = $"'{iconSprite.name}' 과 일치하는 파츠를 찾지 못했습니다.";
                return false;
            }

            _equipmentPartsTypeProperty.enumValueIndex = ( int )partsType;
            _equipmentPartsIndexProperty.intValue = partsIndex;
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( _serializedObject.targetObject );
            _resultMessage = $"자동 할당 완료: {partsType} / {partsIndex}";
            return true;
        }

        ///<summary>
        /// 스프라이트 이름 기반 파츠 데이터 탐색
        ///</summary>
        private bool TryFindEquipmentPartsBySpriteName( string _spriteName, out PartsType _partsType, out int _partsIndex )
        {
            _partsType = PartsType.Chest;
            _partsIndex = -1;

            if ( string.IsNullOrWhiteSpace( _spriteName ) )
            {
                return false;
            }

            GameObject playerPrefabRoot = PrefabUtility.LoadPrefabContents( PlayerPrefabAssetPath );

            if ( playerPrefabRoot == null )
            {
                return false;
            }

            try
            {
                PartsManager partsManager = playerPrefabRoot.GetComponent<PartsManager>();

                if ( partsManager == null )
                {
                    return false;
                }

                SerializedObject serializedObject = new SerializedObject( partsManager );
                SerializedProperty categoriesProperty = serializedObject.FindProperty( "categories" );

                if ( categoriesProperty == null || categoriesProperty.isArray == false )
                {
                    return false;
                }

                for ( int categoryIndex = 0; categoryIndex < categoriesProperty.arraySize; categoryIndex++ )
                {
                    SerializedProperty categoryProperty = categoriesProperty.GetArrayElementAtIndex( categoryIndex );

                    if ( categoryProperty == null )
                    {
                        continue;
                    }

                    SerializedProperty partsTypeProperty = categoryProperty.FindPropertyRelative( "type" );
                    SerializedProperty renderersProperty = categoryProperty.FindPropertyRelative( "renderers" );

                    if ( partsTypeProperty == null || renderersProperty == null || renderersProperty.isArray == false || renderersProperty.arraySize <= 0 )
                    {
                        continue;
                    }

                    SerializedProperty firstRendererProperty = renderersProperty.GetArrayElementAtIndex( 0 );

                    if ( firstRendererProperty == null )
                    {
                        continue;
                    }

                    SerializedProperty spritesProperty = firstRendererProperty.FindPropertyRelative( "sprites" );

                    if ( spritesProperty == null || spritesProperty.isArray == false )
                    {
                        continue;
                    }

                    for ( int spriteIndex = 0; spriteIndex < spritesProperty.arraySize; spriteIndex++ )
                    {
                        SerializedProperty spriteProperty = spritesProperty.GetArrayElementAtIndex( spriteIndex );
                        Sprite currentSprite = spriteProperty != null ? spriteProperty.objectReferenceValue as Sprite : null;

                        if ( currentSprite == null )
                        {
                            continue;
                        }

                        if ( string.Equals( currentSprite.name, _spriteName, StringComparison.Ordinal ) == false )
                        {
                            continue;
                        }

                        _partsType = ( PartsType )partsTypeProperty.enumValueIndex;
                        _partsIndex = spriteIndex;
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( playerPrefabRoot );
            }
        }

        ///<summary>
        /// 아이템 폴더 생성 보장
        ///</summary>
        private void EnsureItemDefinitionFolderExists()
        {
            if ( AssetDatabase.IsValidFolder( ItemDefinitionFolderPath ) )
            {
                return;
            }

            string[] folderSegmentArray = ItemDefinitionFolderPath.Split( '/' );
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
        /// 필터 적용 아이템 목록 반환
        ///</summary>
        private List<ItemDefinitionInfo> GetFilteredItemDefinitionInfos()
        {
            List<ItemDefinitionInfo> filteredInfoList = new List<ItemDefinitionInfo>();

            for ( int index = 0; index < itemDefinitionInfoList.Count; index++ )
            {
                ItemDefinitionInfo itemInfo = itemDefinitionInfoList[ index ];

                if ( IsMatchedSearch( itemInfo ) == false )
                {
                    continue;
                }

                if ( IsMatchedTypeFilter( itemInfo ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( itemInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 검색 일치 여부 반환
        ///</summary>
        private bool IsMatchedSearch( ItemDefinitionInfo _itemInfo )
        {
            if ( _itemInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();
            bool isItemNameMatched = _itemInfo.itemName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;

            if ( isItemNameMatched )
            {
                return true;
            }

            bool isItemIdMatched = _itemInfo.itemId.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return isItemIdMatched;
        }

        ///<summary>
        /// 타입 필터 일치 여부 반환
        ///</summary>
        private bool IsMatchedTypeFilter( ItemDefinitionInfo _itemInfo )
        {
            if ( _itemInfo == null )
            {
                return false;
            }

            if ( selectedTypeFilterIndex <= 0 )
            {
                return true;
            }

            eItemType targetType = ( eItemType )( selectedTypeFilterIndex - 1 );
            bool result = _itemInfo.itemType == targetType;
            return result;
        }

        ///<summary>
        /// 현재 선택 아이템 정의 반환
        ///</summary>
        private CItemDefinition GetSelectedItemDefinition()
        {
            if ( selectedItemIndex < 0 || selectedItemIndex >= itemDefinitionInfoList.Count )
            {
                return null;
            }

            ItemDefinitionInfo itemInfo = itemDefinitionInfoList[ selectedItemIndex ];

            if ( itemInfo == null )
            {
                return null;
            }

            CItemDefinition result = AssetDatabase.LoadAssetAtPath<CItemDefinition>( itemInfo.assetPath );
            return result;
        }

        ///<summary>
        /// 에셋 경로 기준 선택 처리
        ///</summary>
        private void SelectItemByAssetPath( string _assetPath )
        {
            for ( int index = 0; index < itemDefinitionInfoList.Count; index++ )
            {
                ItemDefinitionInfo itemInfo = itemDefinitionInfoList[ index ];

                if ( itemInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemInfo.assetPath, _assetPath, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                selectedItemIndex = index;
                isPendingFocusToSelection = true;
                Repaint();
                return;
            }
        }

        ///<summary>
        /// 아이템 선택 처리
        ///</summary>
        private void SelectItemByIndex( int _sourceIndex, int _filteredIndex, int _filteredItemCount )
        {
            if ( _sourceIndex < 0 || _sourceIndex >= itemDefinitionInfoList.Count )
            {
                return;
            }

            selectedItemIndex = _sourceIndex;
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

            if ( itemTop < itemListScrollPosition.y )
            {
                itemListScrollPosition.y = itemTop;
            }
            else if ( itemBottom > itemListScrollPosition.y + ListViewHeight )
            {
                itemListScrollPosition.y = itemBottom - ListViewHeight;
            }

            itemListScrollPosition.y = Mathf.Clamp( itemListScrollPosition.y, 0.0f, maxScrollY );
        }

        ///<summary>
        /// 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildItemControlName( int _sourceIndex )
        {
            string result = $"ItemDefinitionItem_{_sourceIndex}";
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
            List<ItemDefinitionInfo> filteredInfoList = GetFilteredItemDefinitionInfos();

            if ( filteredInfoList.Count == 0 )
            {
                return;
            }

            int filteredSelectedIndex = 0;
            ItemDefinitionInfo selectedInfo = null;

            if ( selectedItemIndex >= 0 && selectedItemIndex < itemDefinitionInfoList.Count )
            {
                selectedInfo = itemDefinitionInfoList[ selectedItemIndex ];
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
            ItemDefinitionInfo nextInfo = filteredInfoList[ nextFilteredIndex ];
            int sourceIndex = itemDefinitionInfoList.IndexOf( nextInfo );
            SelectItemByIndex( sourceIndex, nextFilteredIndex, filteredInfoList.Count );
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
