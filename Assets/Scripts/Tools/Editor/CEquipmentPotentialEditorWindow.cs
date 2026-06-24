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
        private const string AssetFolderPath = "Assets/Resources/Data/Item";
        private const string AssetPath = "Assets/Resources/Data/Item/EquipmentPotentialTableData.asset";
        private const float EntrySpacing = 8.0f;

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

        ///<summary>
        /// 잠재 편집 창 열기 메뉴
        ///</summary>
        [MenuItem( "Tools/TinyHero/Equipment Potential Editor" )]
        public static void OpenWindow()
        {
            CEquipmentPotentialEditorWindow window = GetWindow<CEquipmentPotentialEditorWindow>( "Equipment Potential Editor" );
            window.minSize = new Vector2( 1080.0f, 620.0f );
            window.Show();
        }

        ///<summary>
        /// 잠재 편집 창 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            EnsureTableData();
            CacheSerializedProperties();
        }

        ///<summary>
        /// 잠재 편집 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EnsureTableData();

            if ( tableData == null || serializedTableData == null )
            {
                EditorGUILayout.HelpBox( "EquipmentPotentialTableData asset could not be loaded.", MessageType.Error );
                return;
            }

            serializedTableData.Update();
            DrawToolbar();
            EditorGUILayout.Space( 8.0f );
            DrawRuleSection();
            EditorGUILayout.Space( 8.0f );
            DrawSummarySection();
            EditorGUILayout.Space( 8.0f );
            DrawOptionListSection();
            serializedTableData.ApplyModifiedProperties();

            if ( GUI.changed )
            {
                EditorUtility.SetDirty( tableData );
            }
        }

        ///<summary>
        /// 잠재 테이블 데이터 보장
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
        /// 상단 툴바 렌더링
        ///</summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal( EditorStyles.toolbar );
            selectedEquipmentType = ( eEquipmentType )EditorGUILayout.EnumPopup( selectedEquipmentType, EditorStyles.toolbarPopup, GUILayout.Width( 150.0f ) );
            selectedRank = ( eEquipmentPotentialRank )EditorGUILayout.EnumPopup( selectedRank, EditorStyles.toolbarPopup, GUILayout.Width( 150.0f ) );

            if ( GUILayout.Button( "Add Option", EditorStyles.toolbarButton, GUILayout.Width( 110.0f ) ) )
            {
                AddOptionEntry();
            }

            if ( GUILayout.Button( "Generate Defaults", EditorStyles.toolbarButton, GUILayout.Width( 130.0f ) ) )
            {
                GenerateDefaultEntries();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 잠재 규칙 섹션 렌더링
        ///</summary>
        private void DrawRuleSection()
        {
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.LabelField( "Roll Rules", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "1번째 줄은 현재 등급에서만 등장합니다. 2, 3번째 줄은 기본적으로 한 단계 낮은 등급에서 등장하며, 설정한 확률로 현재 등급이 다시 등장합니다.", MessageType.Info );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( commonToRareChanceProperty, new GUIContent( "Common -> Rare" ) );
            EditorGUILayout.PropertyField( rareToUniqueChanceProperty, new GUIContent( "Rare -> Unique" ) );
            EditorGUILayout.PropertyField( uniqueToLegendaryChanceProperty, new GUIContent( "Unique -> Legendary" ) );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space( 4.0f );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( rareAdditionalCurrentRankChanceProperty, new GUIContent( "Rare Line 2/3 Current Rank" ) );
            EditorGUILayout.PropertyField( uniqueAdditionalCurrentRankChanceProperty, new GUIContent( "Unique Line 2/3 Current Rank" ) );
            EditorGUILayout.PropertyField( legendaryAdditionalCurrentRankChanceProperty, new GUIContent( "Legendary Line 2/3 Current Rank" ) );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 잠재 요약 섹션 렌더링
        ///</summary>
        private void DrawSummarySection()
        {
            int filteredEntryCount = GetFilteredEntryIndexList().Count;
            int totalWeight = ResolveFilteredTotalWeight();
            eEquipmentPotentialRank fallbackRank = CEquipmentPotentialUtility.GetPreviousRank( selectedRank );
            float currentRankChance = tableData != null ? tableData.GetAdditionalCurrentRankChance( selectedRank ) * 100.0f : 100.0f;
            float fallbackRankChance = 100.0f - currentRankChance;
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
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.LabelField( "Option Entries", EditorStyles.boldLabel );
            scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition );
            List<int> filteredEntryIndexList = GetFilteredEntryIndexList();

            for ( int index = 0; index < filteredEntryIndexList.Count; index += 2 )
            {
                EditorGUILayout.BeginHorizontal();
                DrawOptionEntry( filteredEntryIndexList[ index ] );
                GUILayout.Space( EntrySpacing );

                if ( index + 1 < filteredEntryIndexList.Count )
                {
                    DrawOptionEntry( filteredEntryIndexList[ index + 1 ] );
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space( EntrySpacing );
            }

            EditorGUILayout.EndScrollView();
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
            eEquipmentPotentialOptionType optionType = ( eEquipmentPotentialOptionType )optionTypeProperty.enumValueIndex;
            bool forcePercentValueType = CEquipmentPotentialUtility.ShouldForcePercentValueType( optionType );
            float chancePercent = ResolveEntryChancePercent( optionEntryProperty );
            EditorGUILayout.BeginVertical( "box", GUILayout.MaxWidth( position.width * 0.48f ) );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"{CEquipmentPotentialUtility.GetOptionLabel( optionType )}", EditorStyles.boldLabel );
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField( $"{chancePercent:0.##}%", GUILayout.Width( 70.0f ) );

            if ( GUILayout.Button( "Delete", GUILayout.Width( 70.0f ) ) )
            {
                optionEntryListProperty.DeleteArrayElementAtIndex( _entryIndex );
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
                valueTypeProperty.enumValueIndex = ( int )eEquipmentPotentialValueType.PERCENT;
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
            EditorGUILayout.LabelField( $"Chance by Weight: {chancePercent:0.##}% (within current filter)" );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 잠재 엔트리 미리보기 문자열 구성
        ///</summary>
        private string BuildEntryPreviewText( SerializedProperty _optionTypeProperty, SerializedProperty _valueTypeProperty, SerializedProperty _valueProperty )
        {
            eEquipmentPotentialOptionType optionType = ( eEquipmentPotentialOptionType )_optionTypeProperty.enumValueIndex;
            eEquipmentPotentialValueType valueType = ( eEquipmentPotentialValueType )_valueTypeProperty.enumValueIndex;
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

                filteredEntryIndexList.Add( index );
            }

            return filteredEntryIndexList;
        }

        ///<summary>
        /// 엔트리 필터 일치 여부 반환
        ///</summary>
        private bool ShouldDrawEntry( SerializedProperty _optionEntryProperty )
        {
            if ( _optionEntryProperty == null )
            {
                return false;
            }

            SerializedProperty equipmentTypeProperty = _optionEntryProperty.FindPropertyRelative( "equipmentType" );
            SerializedProperty rankProperty = _optionEntryProperty.FindPropertyRelative( "rank" );

            if ( equipmentTypeProperty == null || rankProperty == null )
            {
                return false;
            }

            eEquipmentType equipmentType = ( eEquipmentType )equipmentTypeProperty.enumValueIndex;
            eEquipmentPotentialRank rank = ( eEquipmentPotentialRank )rankProperty.enumValueIndex;
            bool result = equipmentType == selectedEquipmentType && rank == selectedRank;
            return result;
        }

        ///<summary>
        /// 현재 필터 총 가중치 반환
        ///</summary>
        private int ResolveFilteredTotalWeight()
        {
            int totalWeight = 0;
            List<int> filteredEntryIndexList = GetFilteredEntryIndexList();

            for ( int index = 0; index < filteredEntryIndexList.Count; index++ )
            {
                int entryIndex = filteredEntryIndexList[ index ];
                SerializedProperty optionEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( entryIndex );
                SerializedProperty weightProperty = optionEntryProperty.FindPropertyRelative( "weight" );
                totalWeight += Mathf.Max( 0, weightProperty.intValue );
            }

            return totalWeight;
        }

        ///<summary>
        /// 엔트리 실제 확률 반환
        ///</summary>
        private float ResolveEntryChancePercent( SerializedProperty _optionEntryProperty )
        {
            if ( _optionEntryProperty == null )
            {
                return 0.0f;
            }

            SerializedProperty weightProperty = _optionEntryProperty.FindPropertyRelative( "weight" );
            int totalWeight = ResolveFilteredTotalWeight();

            if ( weightProperty == null || totalWeight <= 0 )
            {
                return 0.0f;
            }

            float chancePercent = Mathf.Max( 0.0f, weightProperty.intValue ) / totalWeight * 100.0f;
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

            int newIndex = optionEntryListProperty.arraySize;
            optionEntryListProperty.InsertArrayElementAtIndex( newIndex );
            SerializedProperty createdEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( newIndex );
            SerializedProperty optionTypeProperty = createdEntryProperty.FindPropertyRelative( "optionType" );
            eEquipmentPotentialOptionType optionType = eEquipmentPotentialOptionType.ATK;
            createdEntryProperty.FindPropertyRelative( "equipmentType" ).enumValueIndex = ( int )selectedEquipmentType;
            createdEntryProperty.FindPropertyRelative( "rank" ).enumValueIndex = ( int )selectedRank;
            optionTypeProperty.enumValueIndex = ( int )optionType;
            createdEntryProperty.FindPropertyRelative( "valueType" ).enumValueIndex = ( int )CEquipmentPotentialUtility.GetDefaultValueType( optionType );
            createdEntryProperty.FindPropertyRelative( "value" ).floatValue = ResolveDefaultValue( selectedRank, optionType, CEquipmentPotentialUtility.GetDefaultValueType( optionType ) );
            createdEntryProperty.FindPropertyRelative( "weight" ).intValue = 1;
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
                        AddDefaultOptionEntry( equipmentType, rank, optionType );
                    }
                }
            }
        }

        ///<summary>
        /// 기본 잠재 엔트리 추가
        ///</summary>
        private void AddDefaultOptionEntry( eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType )
        {
            if ( optionEntryListProperty == null )
            {
                return;
            }

            int newIndex = optionEntryListProperty.arraySize;
            optionEntryListProperty.InsertArrayElementAtIndex( newIndex );
            SerializedProperty createdEntryProperty = optionEntryListProperty.GetArrayElementAtIndex( newIndex );
            eEquipmentPotentialValueType valueType = CEquipmentPotentialUtility.GetDefaultValueType( _optionType );
            createdEntryProperty.FindPropertyRelative( "equipmentType" ).enumValueIndex = ( int )_equipmentType;
            createdEntryProperty.FindPropertyRelative( "rank" ).enumValueIndex = ( int )_rank;
            createdEntryProperty.FindPropertyRelative( "optionType" ).enumValueIndex = ( int )_optionType;
            createdEntryProperty.FindPropertyRelative( "valueType" ).enumValueIndex = ( int )valueType;
            createdEntryProperty.FindPropertyRelative( "value" ).floatValue = ResolveDefaultValue( _rank, _optionType, valueType );
            createdEntryProperty.FindPropertyRelative( "weight" ).intValue = 1;
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
