using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools.Editor
{
    ///<summary>
    /// 장비 잠재 옵션 편집 창
    ///</summary>
    public sealed class CEquipmentPotentialEditorWindow : EditorWindow
    {
        private struct COptionEntryViewData
        {
            public int entryIndex;
            public eEquipmentPotentialOptionType optionType;
            public eEquipmentPotentialValueType valueType;
            public float value;
            public int weight;
        }

        private const string AssetFolderPath = "Assets/Resources/Data/Item";
        private const string AssetPath = "Assets/Resources/Data/Item/EquipmentPotentialTableData.asset";
        private const float EntrySpacing = 8.0f;
        private const float PercentScale = 100.0f;
        private const float BottomSaveSectionHeight = 76.0f;
        private const float TopSectionEstimateHeight = 220.0f;
        private const float DefaultValueGapMultiplier = 1.3f;
        private const int EntryCountPerRow = 4;

        private Vector2 scrollPosition;
        private eEquipmentType selectedEquipmentType = eEquipmentType.WEAPON;
        private eEquipmentPotentialRank selectedRank = eEquipmentPotentialRank.COMMON;
        private CEquipmentPotentialTableData tableData;
        private SerializedObject serializedTableData;
        private SerializedProperty commonToRareChanceProperty;
        private SerializedProperty rareToUniqueChanceProperty;
        private SerializedProperty uniqueToLegendaryChanceProperty;
        private SerializedProperty rareAdditionalCurrentRankChanceProperty;
        private SerializedProperty uniqueAdditionalCurrentRankChanceProperty;
        private SerializedProperty legendaryAdditionalCurrentRankChanceProperty;
        private SerializedProperty optionEntryListProperty;
        private List<int> cachedFilteredEntryIndexList = new List<int>();
        private List<List<int>> cachedGroupedEntryIndexList = new List<List<int>>();
        private Dictionary<int, COptionEntryViewData> cachedEntryViewDataMap = new Dictionary<int, COptionEntryViewData>();
        private int cachedFilteredTotalWeight;
        private bool hasPendingChanges;
        private bool isViewCacheDirty = true;

        ///<summary>
        /// 장비 잠재 옵션 편집 창 열기 메뉴
        ///</summary>
        [MenuItem( "Tools/TinyHero/Equipment Potential Editor" )]
        public static void OpenWindow()
        {
            CEquipmentPotentialEditorWindow window = GetWindow<CEquipmentPotentialEditorWindow>( "Equipment Potential Editor" );
            window.minSize = new Vector2( 1400.0f, 720.0f );
            window.Show();
        }

        ///<summary>
        /// 장비 잠재 옵션 편집 창 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            EnsureTableData();
            CacheSerializedProperties();
        }

        ///<summary>
        /// 장비 잠재 옵션 편집 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EnsureTableData();

            if ( tableData == null || serializedTableData == null )
            {
                EditorGUILayout.HelpBox( "EquipmentPotentialTableData asset could not be loaded.", MessageType.Error );
                return;
            }

            serializedTableData.UpdateIfRequiredOrScript();
            RebuildViewCacheIfNeeded();
            DrawToolbar();
            EditorGUILayout.Space( 8.0f );
            DrawRuleSection();
            EditorGUILayout.Space( 8.0f );
            DrawSummarySection();
            EditorGUILayout.Space( 8.0f );
            DrawOptionListSection();
            EditorGUILayout.Space( 8.0f );
            DrawBottomSaveSection();

            bool hasModifiedProperties = serializedTableData.hasModifiedProperties;

            if ( hasModifiedProperties )
            {
                serializedTableData.ApplyModifiedPropertiesWithoutUndo();
                hasPendingChanges = true;
                RebuildViewCache();
                Repaint();
            }
        }

        ///<summary>
        /// 장비 잠재 테이블 에셋 보장
        ///</summary>
        private void EnsureTableData()
        {
            if ( tableData != null && serializedTableData != null )
            {
                return;
            }

            EnsureAssetFolder();
            tableData = AssetDatabase.LoadAssetAtPath<CEquipmentPotentialTableData>( AssetPath );

            if ( tableData == null )
            {
                tableData = CreateInstance<CEquipmentPotentialTableData>();
                AssetDatabase.CreateAsset( tableData, AssetPath );
                AssetDatabase.SaveAssets();
            }

            serializedTableData = new SerializedObject( tableData );
            CacheSerializedProperties();
        }

        ///<summary>
        /// 직렬화 프로퍼티 캐시
        ///</summary>
        private void CacheSerializedProperties()
        {
            if ( serializedTableData == null )
            {
                return;
            }

            commonToRareChanceProperty = serializedTableData.FindProperty( "commonToRareChance" );
            rareToUniqueChanceProperty = serializedTableData.FindProperty( "rareToUniqueChance" );
            uniqueToLegendaryChanceProperty = serializedTableData.FindProperty( "uniqueToLegendaryChance" );
            rareAdditionalCurrentRankChanceProperty = serializedTableData.FindProperty( "rareAdditionalCurrentRankChance" );
            uniqueAdditionalCurrentRankChanceProperty = serializedTableData.FindProperty( "uniqueAdditionalCurrentRankChance" );
            legendaryAdditionalCurrentRankChanceProperty = serializedTableData.FindProperty( "legendaryAdditionalCurrentRankChance" );
            optionEntryListProperty = serializedTableData.FindProperty( "optionEntryList" );
        }

        ///<summary>
        /// 화면 표시용 캐시 재구성
        ///</summary>
        private void RebuildViewCache()
        {
            cachedEntryViewDataMap.Clear();
            cachedFilteredEntryIndexList = GetFilteredEntryIndexList();
            cachedGroupedEntryIndexList = BuildGroupedFilteredEntryIndexList( cachedFilteredEntryIndexList );
            cachedFilteredTotalWeight = ResolveFilteredTotalWeight( cachedFilteredEntryIndexList );
            isViewCacheDirty = false;
        }

        ///<summary>
        /// 화면 표시용 캐시 재구성 보장
        ///</summary>
        private void RebuildViewCacheIfNeeded()
        {
            if ( isViewCacheDirty == false )
            {
                return;
            }

            RebuildViewCache();
        }

        ///<summary>
        /// 화면 표시용 캐시 갱신 요청
        ///</summary>
        private void MarkViewCacheDirty()
        {
            isViewCacheDirty = true;
        }

        ///<summary>
        /// 상단 툴바 렌더링
        ///</summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            eEquipmentType changedEquipmentType = ( eEquipmentType )EditorGUILayout.EnumPopup( selectedEquipmentType, EditorStyles.toolbarPopup, GUILayout.Width( 150.0f ) );
            eEquipmentPotentialRank changedRank = ( eEquipmentPotentialRank )EditorGUILayout.EnumPopup( selectedRank, EditorStyles.toolbarPopup, GUILayout.Width( 150.0f ) );

            if ( changedEquipmentType != selectedEquipmentType || changedRank != selectedRank )
            {
                selectedEquipmentType = changedEquipmentType;
                selectedRank = changedRank;
                MarkViewCacheDirty();
                RebuildViewCacheIfNeeded();
            }

            if ( GUILayout.Button( "Add Option", EditorStyles.toolbarButton, GUILayout.Width( 110.0f ) ) )
            {
                AddOptionEntry();
                MarkViewCacheDirty();
            }

            if ( GUILayout.Button( "Generate Defaults", EditorStyles.toolbarButton, GUILayout.Width( 130.0f ) ) )
            {
                GenerateDefaultEntries();
                MarkViewCacheDirty();
            }

            GUILayout.FlexibleSpace();
            GUI.enabled = hasPendingChanges;

            if ( GUILayout.Button( "Save", EditorStyles.toolbarButton, GUILayout.Width( 80.0f ) ) )
            {
                SaveTableData();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 잠재 규칙 섹션 렌더링
        ///</summary>
        private void DrawRuleSection()
        {
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.LabelField( "Roll Rules", EditorStyles.boldLabel );
            EditorGUILayout.Space( 2.0f );
            EditorGUILayout.BeginHorizontal();
            DrawPercentField( commonToRareChanceProperty, "Common -> Rare (%)" );
            DrawPercentField( rareToUniqueChanceProperty, "Rare -> Unique (%)" );
            DrawPercentField( uniqueToLegendaryChanceProperty, "Unique -> Legendary (%)" );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space( 4.0f );
            EditorGUILayout.BeginHorizontal();
            DrawPercentField( rareAdditionalCurrentRankChanceProperty, "Rare Line 2/3 (%)" );
            DrawPercentField( uniqueAdditionalCurrentRankChanceProperty, "Unique Line 2/3 (%)" );
            DrawPercentField( legendaryAdditionalCurrentRankChanceProperty, "Legendary Line 2/3 (%)" );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 퍼센트 규칙 입력 필드 렌더링
        ///</summary>
        private void DrawPercentField( SerializedProperty _property, string _label )
        {
            if ( _property == null )
            {
                return;
            }

            float percentValue = _property.floatValue * PercentScale;
            float changedPercentValue = EditorGUILayout.DelayedFloatField( new GUIContent( _label ), percentValue );
            float clampedPercentValue = Mathf.Clamp( changedPercentValue, 0.0f, PercentScale );
            _property.floatValue = clampedPercentValue / PercentScale;
        }

        ///<summary>
        /// 잠재 요약 섹션 렌더링
        ///</summary>
        private void DrawSummarySection()
        {
            int filteredEntryCount = cachedFilteredEntryIndexList.Count;
            int totalWeight = cachedFilteredTotalWeight;
            eEquipmentPotentialRank fallbackRank = CEquipmentPotentialUtility.GetPreviousRank( selectedRank );
            float currentRankChance = tableData != null ? tableData.GetAdditionalCurrentRankChance( selectedRank ) * PercentScale : PercentScale;
            float fallbackRankChance = PercentScale - currentRankChance;
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.LabelField( "Summary", EditorStyles.boldLabel );
            EditorGUILayout.LabelField( $"Filtered Entries: {filteredEntryCount}" );
            EditorGUILayout.LabelField( $"Total Weight: {totalWeight}" );

            if ( selectedRank == eEquipmentPotentialRank.COMMON )
            {
                EditorGUILayout.LabelField( "Line 2 / 3 Rank Rule: Always COMMON" );
            }
            else
            {
                EditorGUILayout.LabelField( $"Line 2 / 3 Rank Rule: {fallbackRankChance:0.##}% {fallbackRank}, {currentRankChance:0.##}% {selectedRank}" );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 잠재 엔트리 섹션 렌더링
        ///</summary>
        private void DrawOptionListSection()
        {
            float optionListHeight = ResolveOptionListHeight();
            EditorGUILayout.BeginVertical( "box", GUILayout.Height( optionListHeight ) );
            EditorGUILayout.LabelField( "Option Entries", EditorStyles.boldLabel );
            scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition, GUILayout.ExpandHeight( true ) );

            for ( int groupIndex = 0; groupIndex < cachedGroupedEntryIndexList.Count; groupIndex++ )
            {
                List<int> optionGroupList = cachedGroupedEntryIndexList[ groupIndex ];
                DrawOptionGroupRows( optionGroupList );
                GUILayout.Space( EntrySpacing );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 옵션 그룹 행 묶음 렌더링
        ///</summary>
        private void DrawOptionGroupRows( List<int> _optionGroupList )
        {
            if ( _optionGroupList == null || _optionGroupList.Count == 0 )
            {
                return;
            }

            for ( int index = 0; index < _optionGroupList.Count; index += EntryCountPerRow )
            {
                EditorGUILayout.BeginHorizontal();

                for ( int columnIndex = 0; columnIndex < EntryCountPerRow; columnIndex++ )
                {
                    int entryListIndex = index + columnIndex;

                    if ( entryListIndex < _optionGroupList.Count )
                    {
                        int entryIndex = _optionGroupList[ entryListIndex ];
                        DrawOptionEntry( entryIndex );
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }

                    if ( columnIndex < EntryCountPerRow - 1 )
                    {
                        GUILayout.Space( EntrySpacing );
                    }
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space( EntrySpacing );
            }
        }

        ///<summary>
        /// 하단 저장 영역 렌더링
        ///</summary>
        private void DrawBottomSaveSection()
        {
            EditorGUILayout.BeginVertical( "box", GUILayout.Height( BottomSaveSectionHeight ) );
            string statusText = hasPendingChanges ? "Unsaved changes" : "All changes saved";
            EditorGUILayout.LabelField( statusText, EditorStyles.boldLabel );
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = hasPendingChanges;

            if ( GUILayout.Button( "Save Changes", GUILayout.Width( 180.0f ), GUILayout.Height( 30.0f ) ) )
            {
                SaveTableData();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 개별 잠재 엔트리 렌더링
        ///</summary>
        private void DrawOptionEntry( int _entryIndex )
        {
            if ( optionEntryListProperty == null || _entryIndex < 0 || _entryIndex >= optionEntryListProperty.arraySize )
            {
                return;
            }

            SerializedProperty optionEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( _entryIndex );
            SerializedProperty equipmentTypeProperty = optionEntryProperty.FindPropertyRelative( "equipmentType" );
            SerializedProperty rankProperty = optionEntryProperty.FindPropertyRelative( "rank" );
            SerializedProperty optionTypeProperty = optionEntryProperty.FindPropertyRelative( "optionType" );
            SerializedProperty valueTypeProperty = optionEntryProperty.FindPropertyRelative( "valueType" );
            SerializedProperty valueProperty = optionEntryProperty.FindPropertyRelative( "value" );
            SerializedProperty weightProperty = optionEntryProperty.FindPropertyRelative( "weight" );
            COptionEntryViewData viewData = cachedEntryViewDataMap[ _entryIndex ];
            bool forcePercentValueType = CEquipmentPotentialUtility.ShouldForcePercentValueType( viewData.optionType );
            float chancePercent = ResolveEntryChancePercent( weightProperty );
            float entryWidth = ResolveEntryWidth();
            EditorGUILayout.BeginVertical( "box", GUILayout.Width( entryWidth ) );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"{CEquipmentPotentialUtility.GetOptionLabel( viewData.optionType )}", EditorStyles.boldLabel );
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField( $"{chancePercent:0.##}%", GUILayout.Width( 58.0f ) );

            if ( GUILayout.Button( "Delete", GUILayout.Width( 58.0f ) ) )
            {
                optionEntryListProperty.DeleteArrayElementAtIndex( _entryIndex );
                MarkViewCacheDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( equipmentTypeProperty );
            EditorGUILayout.PropertyField( rankProperty );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField( optionTypeProperty );

            if ( forcePercentValueType )
            {
                if ( valueTypeProperty.enumValueIndex != ( int )eEquipmentPotentialValueType.PERCENT )
                {
                    valueTypeProperty.enumValueIndex = ( int )eEquipmentPotentialValueType.PERCENT;
                }

                EditorGUI.BeginDisabledGroup( true );
                EditorGUILayout.PropertyField( valueTypeProperty, new GUIContent( "Value Type" ) );
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.PropertyField( valueTypeProperty, new GUIContent( "Value Type" ) );
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( valueProperty, new GUIContent( "Value" ) );
            EditorGUILayout.PropertyField( weightProperty, new GUIContent( "Weight" ) );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField( $"Applied: {BuildEntryPreviewText( optionTypeProperty, valueTypeProperty, valueProperty )}" );
            EditorGUILayout.LabelField( $"Chance by Weight: {chancePercent:0.##}%" );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 잠재 엔트리 미리보기 문자열 구성
        ///</summary>
        private string BuildEntryPreviewText( SerializedProperty _optionTypeProperty, SerializedProperty _valueTypeProperty, SerializedProperty _valueProperty )
        {
            eEquipmentPotentialOptionType optionType = ( eEquipmentPotentialOptionType )_optionTypeProperty.enumValueIndex;
            eEquipmentPotentialValueType valueType = ResolveDisplayValueType( optionType, _valueTypeProperty );
            string optionLabel = CEquipmentPotentialUtility.GetOptionLabel( optionType );
            string valueText = CEquipmentPotentialUtility.FormatOptionValue( valueType, _valueProperty.floatValue );
            string result = $"{optionLabel} {valueText}";
            return result;
        }

        ///<summary>
        /// 현재 필터 엔트리 목록 반환
        ///</summary>
        private List<int> GetFilteredEntryIndexList()
        {
            List<int> filteredEntryIndexList = new List<int>();

            if ( optionEntryListProperty == null )
            {
                return filteredEntryIndexList;
            }

            for ( int index = 0; index < optionEntryListProperty.arraySize; index++ )
            {
                SerializedProperty optionEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( index );

                if ( ShouldDrawEntry( optionEntryProperty ) == false )
                {
                    continue;
                }

                CacheEntryViewData( index, optionEntryProperty );
                filteredEntryIndexList.Add( index );
            }

            filteredEntryIndexList.Sort( CompareEntryDisplayOrder );
            return filteredEntryIndexList;
        }

        ///<summary>
        /// 엔트리 표시 데이터 캐시
        ///</summary>
        private void CacheEntryViewData( int _entryIndex, SerializedProperty _optionEntryProperty )
        {
            SerializedProperty optionTypeProperty = _optionEntryProperty.FindPropertyRelative( "optionType" );
            SerializedProperty valueTypeProperty = _optionEntryProperty.FindPropertyRelative( "valueType" );
            SerializedProperty valueProperty = _optionEntryProperty.FindPropertyRelative( "value" );
            SerializedProperty weightProperty = _optionEntryProperty.FindPropertyRelative( "weight" );
            eEquipmentPotentialOptionType optionType = ( eEquipmentPotentialOptionType )optionTypeProperty.enumValueIndex;
            eEquipmentPotentialValueType valueType = ResolveDisplayValueType( optionType, valueTypeProperty );
            COptionEntryViewData viewData = new COptionEntryViewData();
            viewData.entryIndex = _entryIndex;
            viewData.optionType = optionType;
            viewData.valueType = valueType;
            viewData.value = valueProperty.floatValue;
            viewData.weight = Mathf.Max( 0, weightProperty.intValue );
            cachedEntryViewDataMap[ _entryIndex ] = viewData;
        }

        ///<summary>
        /// 현재 필터 엔트리 그룹 목록 반환
        ///</summary>
        private List<List<int>> BuildGroupedFilteredEntryIndexList( List<int> _filteredEntryIndexList )
        {
            List<List<int>> groupedEntryIndexList = new List<List<int>>();
            eEquipmentPotentialOptionType currentOptionType = eEquipmentPotentialOptionType.NONE;
            List<int> currentGroupList = null;

            if ( _filteredEntryIndexList == null )
            {
                return groupedEntryIndexList;
            }

            for ( int index = 0; index < _filteredEntryIndexList.Count; index++ )
            {
                int entryIndex = _filteredEntryIndexList[ index ];
                COptionEntryViewData viewData = cachedEntryViewDataMap[ entryIndex ];

                if ( currentGroupList == null || currentOptionType != viewData.optionType )
                {
                    currentGroupList = new List<int>();
                    groupedEntryIndexList.Add( currentGroupList );
                    currentOptionType = viewData.optionType;
                }

                currentGroupList.Add( entryIndex );
            }

            return groupedEntryIndexList;
        }

        ///<summary>
        /// 엔트리 표시 순서 비교
        ///</summary>
        private int CompareEntryDisplayOrder( int _leftIndex, int _rightIndex )
        {
            COptionEntryViewData leftViewData = cachedEntryViewDataMap[ _leftIndex ];
            COptionEntryViewData rightViewData = cachedEntryViewDataMap[ _rightIndex ];
            int optionCompareResult = CompareEntryOptionType( leftViewData, rightViewData );

            if ( optionCompareResult != 0 )
            {
                return optionCompareResult;
            }

            int valueTypeCompareResult = CompareEntryValueType( leftViewData, rightViewData );

            if ( valueTypeCompareResult != 0 )
            {
                return valueTypeCompareResult;
            }

            int result = _leftIndex.CompareTo( _rightIndex );
            return result;
        }

        ///<summary>
        /// 옵션 타입 기준 비교
        ///</summary>
        private int CompareEntryOptionType( COptionEntryViewData _leftViewData, COptionEntryViewData _rightViewData )
        {
            int result = _leftViewData.optionType.CompareTo( _rightViewData.optionType );
            return result;
        }

        ///<summary>
        /// 값 타입 기준 비교
        ///</summary>
        private int CompareEntryValueType( COptionEntryViewData _leftViewData, COptionEntryViewData _rightViewData )
        {
            int leftSortOrder = ResolveValueTypeSortOrder( _leftViewData.valueType );
            int rightSortOrder = ResolveValueTypeSortOrder( _rightViewData.valueType );
            int result = leftSortOrder.CompareTo( rightSortOrder );
            return result;
        }

        ///<summary>
        /// 값 타입 정렬 우선순위 결정
        ///</summary>
        private int ResolveValueTypeSortOrder( eEquipmentPotentialValueType _valueType )
        {
            if ( _valueType == eEquipmentPotentialValueType.PERCENT )
            {
                return 0;
            }

            return 1;
        }

        ///<summary>
        /// 표시용 값 타입 결정
        ///</summary>
        private eEquipmentPotentialValueType ResolveDisplayValueType( eEquipmentPotentialOptionType _optionType, SerializedProperty _valueTypeProperty )
        {
            if ( CEquipmentPotentialUtility.ShouldForcePercentValueType( _optionType ) )
            {
                return eEquipmentPotentialValueType.PERCENT;
            }

            eEquipmentPotentialValueType result = ( eEquipmentPotentialValueType )_valueTypeProperty.enumValueIndex;
            return result;
        }

        ///<summary>
        /// 엔트리 필터 일치 여부 반환
        ///</summary>
        private bool ShouldDrawEntry( SerializedProperty _optionEntryProperty )
        {
            SerializedProperty equipmentTypeProperty = _optionEntryProperty.FindPropertyRelative( "equipmentType" );
            SerializedProperty rankProperty = _optionEntryProperty.FindPropertyRelative( "rank" );
            eEquipmentType equipmentType = ( eEquipmentType )equipmentTypeProperty.enumValueIndex;
            eEquipmentPotentialRank rank = ( eEquipmentPotentialRank )rankProperty.enumValueIndex;
            bool result = equipmentType == selectedEquipmentType && rank == selectedRank;
            return result;
        }

        ///<summary>
        /// 현재 필터 총 가중치 반환
        ///</summary>
        private int ResolveFilteredTotalWeight( List<int> _filteredEntryIndexList )
        {
            int totalWeight = 0;

            if ( _filteredEntryIndexList == null )
            {
                return totalWeight;
            }

            for ( int index = 0; index < _filteredEntryIndexList.Count; index++ )
            {
                int entryIndex = _filteredEntryIndexList[ index ];
                COptionEntryViewData viewData = cachedEntryViewDataMap[ entryIndex ];
                totalWeight += viewData.weight;
            }

            return totalWeight;
        }

        ///<summary>
        /// 엔트리 실제 확률 반환
        ///</summary>
        private float ResolveEntryChancePercent( SerializedProperty _weightProperty )
        {
            if ( _weightProperty == null || cachedFilteredTotalWeight <= 0 )
            {
                return 0.0f;
            }

            float chancePercent = Mathf.Max( 0.0f, _weightProperty.intValue ) / cachedFilteredTotalWeight * PercentScale;
            return chancePercent;
        }

        ///<summary>
        /// 잠재 옵션 엔트리 추가
        ///</summary>
        private void AddOptionEntry()
        {
            if ( optionEntryListProperty == null )
            {
                return;
            }

            eEquipmentPotentialOptionType optionType = eEquipmentPotentialOptionType.ATK;
            eEquipmentPotentialValueType valueType = CEquipmentPotentialUtility.GetDefaultValueType( optionType );
            int variantIndex = ResolveNextVariantIndex( selectedEquipmentType, selectedRank, optionType, valueType );
            int newIndex = optionEntryListProperty.arraySize;
            optionEntryListProperty.InsertArrayElementAtIndex( newIndex );
            SerializedProperty createdEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( newIndex );
            SerializedProperty optionTypeProperty = createdEntryProperty.FindPropertyRelative( "optionType" );
            createdEntryProperty.FindPropertyRelative( "optionKey" ).stringValue = BuildOptionKey( selectedEquipmentType, selectedRank, optionType, valueType, variantIndex );
            createdEntryProperty.FindPropertyRelative( "equipmentType" ).enumValueIndex = ( int )selectedEquipmentType;
            createdEntryProperty.FindPropertyRelative( "rank" ).enumValueIndex = ( int )selectedRank;
            optionTypeProperty.enumValueIndex = ( int )optionType;
            createdEntryProperty.FindPropertyRelative( "valueType" ).enumValueIndex = ( int )valueType;
            createdEntryProperty.FindPropertyRelative( "value" ).floatValue = ResolveDefaultValue( selectedRank, optionType, valueType );
            createdEntryProperty.FindPropertyRelative( "weight" ).intValue = 1;
            MarkViewCacheDirty();
        }

        ///<summary>
        /// 장비 잠재 옵션 테이블 저장
        ///</summary>
        private void SaveTableData()
        {
            if ( tableData == null )
            {
                return;
            }

            if ( serializedTableData != null )
            {
                NormalizeOptionKeys();
                serializedTableData.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty( tableData );
            AssetDatabase.SaveAssets();
            hasPendingChanges = false;
            MarkViewCacheDirty();
        }

        ///<summary>
        /// 기본 잠재 엔트리 생성
        ///</summary>
        private void GenerateDefaultEntries()
        {
            if ( optionEntryListProperty == null )
            {
                return;
            }

            optionEntryListProperty.ClearArray();
            eEquipmentType[] equipmentTypeArray =
            {
                eEquipmentType.WEAPON,
                eEquipmentType.HELMET,
                eEquipmentType.ARMOR,
                eEquipmentType.SHIELD
            };

            eEquipmentPotentialRank[] rankArray =
            {
                eEquipmentPotentialRank.COMMON,
                eEquipmentPotentialRank.RARE,
                eEquipmentPotentialRank.UNIQUE,
                eEquipmentPotentialRank.LEGENDARY
            };

            eEquipmentPotentialOptionType[] optionTypeArray =
            {
                eEquipmentPotentialOptionType.HP,
                eEquipmentPotentialOptionType.HR,
                eEquipmentPotentialOptionType.MP,
                eEquipmentPotentialOptionType.MR,
                eEquipmentPotentialOptionType.ATK,
                eEquipmentPotentialOptionType.DEF,
                eEquipmentPotentialOptionType.CRT,
                eEquipmentPotentialOptionType.CRD,
                eEquipmentPotentialOptionType.ACC,
                eEquipmentPotentialOptionType.ATS,
                eEquipmentPotentialOptionType.MOVE,
                eEquipmentPotentialOptionType.RANGE,
                eEquipmentPotentialOptionType.EXP_GAIN_PERCENT,
                eEquipmentPotentialOptionType.GOLD_GAIN_PERCENT,
                eEquipmentPotentialOptionType.FINAL_ATTACK_PERCENT
            };

            for ( int equipmentIndex = 0; equipmentIndex < equipmentTypeArray.Length; equipmentIndex++ )
            {
                eEquipmentType equipmentType = equipmentTypeArray[ equipmentIndex ];

                for ( int rankIndex = 0; rankIndex < rankArray.Length; rankIndex++ )
                {
                    eEquipmentPotentialRank rank = rankArray[ rankIndex ];

                    for ( int optionIndex = 0; optionIndex < optionTypeArray.Length; optionIndex++ )
                    {
                        eEquipmentPotentialOptionType optionType = optionTypeArray[ optionIndex ];
                        AddDefaultOptionEntries( equipmentType, rank, optionType );
                    }
                }
            }
        }

        ///<summary>
        /// 기본 잠재 엔트리 묶음 추가
        ///</summary>
        private void AddDefaultOptionEntries( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType )
        {
            if ( CEquipmentPotentialUtility.ShouldForcePercentValueType( _optionType ) )
            {
                float forcedFirstPercentValue = ResolveDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.PERCENT );
                float forcedSecondPercentValue = ResolveSecondaryDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.PERCENT, forcedFirstPercentValue );
                AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.PERCENT, forcedFirstPercentValue );
                AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.PERCENT, forcedSecondPercentValue );
                return;
            }

            float firstPercentValue = ResolveDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.PERCENT );
            float secondPercentValue = ResolveSecondaryDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.PERCENT, firstPercentValue );
            float firstValueValue = ResolveDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.VALUE );
            float secondValueValue = ResolveSecondaryDefaultValue( _rank, _optionType, eEquipmentPotentialValueType.VALUE, firstValueValue );
            AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.PERCENT, firstPercentValue );
            AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.PERCENT, secondPercentValue );
            AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.VALUE, firstValueValue );
            AddDefaultOptionEntry( _equipmentType, _rank, _optionType, eEquipmentPotentialValueType.VALUE, secondValueValue );
        }

        ///<summary>
        /// 기본 잠재 엔트리 추가
        ///</summary>
        private void AddDefaultOptionEntry( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType, float _value )
        {
            if ( optionEntryListProperty == null )
            {
                return;
            }

            int variantIndex = ResolveNextVariantIndex( _equipmentType, _rank, _optionType, _valueType );
            int newIndex = optionEntryListProperty.arraySize;
            optionEntryListProperty.InsertArrayElementAtIndex( newIndex );
            SerializedProperty createdEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( newIndex );
            createdEntryProperty.FindPropertyRelative( "equipmentType" ).enumValueIndex = ( int )_equipmentType;
            createdEntryProperty.FindPropertyRelative( "rank" ).enumValueIndex = ( int )_rank;
            createdEntryProperty.FindPropertyRelative( "optionType" ).enumValueIndex = ( int )_optionType;
            createdEntryProperty.FindPropertyRelative( "valueType" ).enumValueIndex = ( int )_valueType;
            createdEntryProperty.FindPropertyRelative( "value" ).floatValue = _value;
            createdEntryProperty.FindPropertyRelative( "weight" ).intValue = 1;
            createdEntryProperty.FindPropertyRelative( "optionKey" ).stringValue = BuildOptionKey( _equipmentType, _rank, _optionType, _valueType, variantIndex );
        }

        ///<summary>
        /// 잠재 엔트리 안정 키 자동 보정
        ///</summary>
        private void NormalizeOptionKeys()
        {
            if ( optionEntryListProperty == null )
            {
                return;
            }

            Dictionary<string, int> variantCountByBaseKey = new Dictionary<string, int>();

            for ( int index = 0; index < optionEntryListProperty.arraySize; index++ )
            {
                SerializedProperty entryProperty = optionEntryListProperty.GetArrayElementAtIndex( index );

                if ( entryProperty == null )
                {
                    continue;
                }

                SerializedProperty optionKeyProperty = entryProperty.FindPropertyRelative( "optionKey" );
                SerializedProperty equipmentTypeProperty = entryProperty.FindPropertyRelative( "equipmentType" );
                SerializedProperty rankProperty = entryProperty.FindPropertyRelative( "rank" );
                SerializedProperty optionTypeProperty = entryProperty.FindPropertyRelative( "optionType" );
                SerializedProperty valueTypeProperty = entryProperty.FindPropertyRelative( "valueType" );
                eEquipmentType equipmentType = ( eEquipmentType )equipmentTypeProperty.enumValueIndex;
                eEquipmentPotentialRank rank = ( eEquipmentPotentialRank )rankProperty.enumValueIndex;
                eEquipmentPotentialOptionType optionType = ( eEquipmentPotentialOptionType )optionTypeProperty.enumValueIndex;
                eEquipmentPotentialValueType valueType = ResolveDisplayValueType( optionType, valueTypeProperty );
                string baseKey = BuildOptionBaseKey( equipmentType, rank, optionType, valueType );
                int variantIndex = 1;

                if ( variantCountByBaseKey.TryGetValue( baseKey, out int currentVariantCount ) )
                {
                    variantIndex = currentVariantCount + 1;
                }

                variantCountByBaseKey[ baseKey ] = variantIndex;
                optionKeyProperty.stringValue = BuildOptionKey( equipmentType, rank, optionType, valueType, variantIndex );
            }
        }

        ///<summary>
        /// 잠재 엔트리 다음 변형 인덱스 반환
        ///</summary>
        private int ResolveNextVariantIndex( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType )
        {
            int nextVariantIndex = 1;

            if ( optionEntryListProperty == null )
            {
                return nextVariantIndex;
            }

            for ( int index = 0; index < optionEntryListProperty.arraySize; index++ )
            {
                SerializedProperty entryProperty = optionEntryListProperty.GetArrayElementAtIndex( index );

                if ( entryProperty == null )
                {
                    continue;
                }

                SerializedProperty equipmentTypeProperty = entryProperty.FindPropertyRelative( "equipmentType" );
                SerializedProperty rankProperty = entryProperty.FindPropertyRelative( "rank" );
                SerializedProperty optionTypeProperty = entryProperty.FindPropertyRelative( "optionType" );
                SerializedProperty valueTypeProperty = entryProperty.FindPropertyRelative( "valueType" );

                if ( equipmentTypeProperty.enumValueIndex != ( int )_equipmentType )
                {
                    continue;
                }

                if ( rankProperty.enumValueIndex != ( int )_rank )
                {
                    continue;
                }

                if ( optionTypeProperty.enumValueIndex != ( int )_optionType )
                {
                    continue;
                }

                if ( ResolveDisplayValueType( _optionType, valueTypeProperty ) != _valueType )
                {
                    continue;
                }

                nextVariantIndex++;
            }

            return nextVariantIndex;
        }

        ///<summary>
        /// 잠재 엔트리 안정 키 구성
        ///</summary>
        private string BuildOptionKey( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType, int _variantIndex )
        {
            string baseKey = BuildOptionBaseKey( _equipmentType, _rank, _optionType, _valueType );
            string result = $"{baseKey}_{_variantIndex:00}";
            return result;
        }

        ///<summary>
        /// 잠재 엔트리 안정 키 기본값 구성
        ///</summary>
        private string BuildOptionBaseKey( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType )
        {
            string result = $"{_equipmentType}_{_rank}_{_optionType}_{_valueType}";
            return result;
        }

        ///<summary>
        /// 기본 잠재 수치 결정
        ///</summary>
        private float ResolveDefaultValue( eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType )
        {
            float baseValue = _valueType == eEquipmentPotentialValueType.PERCENT ? 1.0f : 3.0f;

            if ( _optionType == eEquipmentPotentialOptionType.HP || _optionType == eEquipmentPotentialOptionType.MP )
            {
                baseValue = _valueType == eEquipmentPotentialValueType.PERCENT ? 2.0f : 25.0f;
            }

            switch ( _rank )
            {
                case eEquipmentPotentialRank.RARE:
                    baseValue += _valueType == eEquipmentPotentialValueType.PERCENT ? 1.0f : 3.0f;
                    break;

                case eEquipmentPotentialRank.UNIQUE:
                    baseValue += _valueType == eEquipmentPotentialValueType.PERCENT ? 3.0f : 8.0f;
                    break;

                case eEquipmentPotentialRank.LEGENDARY:
                    baseValue += _valueType == eEquipmentPotentialValueType.PERCENT ? 6.0f : 15.0f;
                    break;
            }

            return baseValue;
        }

        ///<summary>
        /// 기본 잠재 보조 수치 결정
        ///</summary>
        private float ResolveSecondaryDefaultValue( eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType, float _baseValue )
        {
            float result = _baseValue / DefaultValueGapMultiplier;
            return result;
        }

        ///<summary>
        /// 옵션 리스트 영역 높이 계산
        ///</summary>
        private float ResolveOptionListHeight()
        {
            float estimatedHeight = position.height - TopSectionEstimateHeight - BottomSaveSectionHeight;
            float clampedHeight = Mathf.Max( 180.0f, estimatedHeight );
            return clampedHeight;
        }

        ///<summary>
        /// 옵션 엔트리 너비 계산
        ///</summary>
        private float ResolveEntryWidth()
        {
            float availableWidth = position.width - 48.0f - EntrySpacing * ( EntryCountPerRow - 1 );
            float entryWidth = Mathf.Max( 240.0f, availableWidth / EntryCountPerRow );
            return entryWidth;
        }

        ///<summary>
        /// 잠재 에셋 폴더 보장
        ///</summary>
        private void EnsureAssetFolder()
        {
            string[] folderPathPartArray = AssetFolderPath.Split( '/' );
            string currentPath = folderPathPartArray[ 0 ];

            for ( int index = 1; index < folderPathPartArray.Length; index++ )
            {
                string folderName = folderPathPartArray[ index ];
                string nextPath = $"{currentPath}/{folderName}";

                if ( AssetDatabase.IsValidFolder( nextPath ) == false )
                {
                    AssetDatabase.CreateFolder( currentPath, folderName );
                }

                currentPath = nextPath;
            }
        }
    }
}
