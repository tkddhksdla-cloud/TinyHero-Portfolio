using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 장비 잠재 에셋 생성 도구
    ///</summary>
    public static class CEquipmentPotentialAssetBootstrapper
    {
        private const string ResourceFolderPath = "Assets/Resources/Data/Item";
        private const string AssetPath = "Assets/Resources/Data/Item/EquipmentPotentialTableData.asset";

        ///<summary>
        /// 잠재 테이블 에셋 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Item/Generate Default Equipment Potential Table" )]
        public static void GenerateDefaultEquipmentPotentialTableMenu()
        {
            string result = GenerateDefaultEquipmentPotentialTable();
            Debug.Log( result );
        }

        ///<summary>
        /// 잠재 테이블 에셋 생성
        ///</summary>
        public static string GenerateDefaultEquipmentPotentialTable()
        {
            EnsureFolderStructure();
            CEquipmentPotentialTableData tableData = AssetDatabase.LoadAssetAtPath<CEquipmentPotentialTableData>( AssetPath );

            if ( tableData == null )
            {
                tableData = ScriptableObject.CreateInstance<CEquipmentPotentialTableData>();
                AssetDatabase.CreateAsset( tableData, AssetPath );
            }

            SerializedObject serializedObject = new SerializedObject( tableData );
            SerializedProperty commonToRareChanceProperty = serializedObject.FindProperty( "commonToRareChance" );
            SerializedProperty rareToUniqueChanceProperty = serializedObject.FindProperty( "rareToUniqueChance" );
            SerializedProperty uniqueToLegendaryChanceProperty = serializedObject.FindProperty( "uniqueToLegendaryChance" );
            SerializedProperty rareAdditionalCurrentRankChanceProperty = serializedObject.FindProperty( "rareAdditionalCurrentRankChance" );
            SerializedProperty uniqueAdditionalCurrentRankChanceProperty = serializedObject.FindProperty( "uniqueAdditionalCurrentRankChance" );
            SerializedProperty legendaryAdditionalCurrentRankChanceProperty = serializedObject.FindProperty( "legendaryAdditionalCurrentRankChance" );
            SerializedProperty optionEntryListProperty = serializedObject.FindProperty( "optionEntryList" );
            commonToRareChanceProperty.floatValue = 0.12f;
            rareToUniqueChanceProperty.floatValue = 0.04f;
            uniqueToLegendaryChanceProperty.floatValue = 0.01f;
            rareAdditionalCurrentRankChanceProperty.floatValue = 0.15f;
            uniqueAdditionalCurrentRankChanceProperty.floatValue = 0.10f;
            legendaryAdditionalCurrentRankChanceProperty.floatValue = 0.08f;
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
                        AddEntry( optionEntryListProperty, equipmentType, rank, optionType );
                    }
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( tableData );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CEquipmentPotentialDatabase.Reload();
            return "Default equipment potential table created.";
        }

        ///<summary>
        /// 잠재 엔트리 추가
        ///</summary>
        private static void AddEntry( SerializedProperty _optionEntryListProperty, eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType )
        {
            if ( _optionEntryListProperty == null )
            {
                return;
            }

            int newIndex = _optionEntryListProperty.arraySize;
            _optionEntryListProperty.InsertArrayElementAtIndex( newIndex );
            SerializedProperty entryProperty = _optionEntryListProperty.GetArrayElementAtIndex( newIndex );
            SerializedProperty equipmentTypeProperty = entryProperty.FindPropertyRelative( "equipmentType" );
            SerializedProperty rankProperty = entryProperty.FindPropertyRelative( "rank" );
            SerializedProperty optionTypeProperty = entryProperty.FindPropertyRelative( "optionType" );
            SerializedProperty valueTypeProperty = entryProperty.FindPropertyRelative( "valueType" );
            SerializedProperty valueProperty = entryProperty.FindPropertyRelative( "value" );
            SerializedProperty weightProperty = entryProperty.FindPropertyRelative( "weight" );
            eEquipmentPotentialValueType valueType = ResolveDefaultValueType( _optionType );
            float defaultValue = ResolveDefaultValue( _rank, _optionType, valueType );
            equipmentTypeProperty.enumValueIndex = ( int )_equipmentType;
            rankProperty.enumValueIndex = ( int )_rank;
            optionTypeProperty.enumValueIndex = ( int )_optionType;
            valueTypeProperty.enumValueIndex = ( int )valueType;
            valueProperty.floatValue = defaultValue;
            weightProperty.intValue = 1;
        }

        ///<summary>
        /// 기본 잠재 수치 타입 결정
        ///</summary>
        private static eEquipmentPotentialValueType ResolveDefaultValueType( eEquipmentPotentialOptionType _optionType )
        {
            eEquipmentPotentialValueType result = CEquipmentPotentialUtility.GetDefaultValueType( _optionType );
            return result;
        }

        ///<summary>
        /// 기본 잠재 수치 결정
        ///</summary>
        private static float ResolveDefaultValue( eEquipmentPotentialRank _rank, eEquipmentPotentialOptionType _optionType, eEquipmentPotentialValueType _valueType )
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
        /// 리소스 폴더 구조 보장
        ///</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets", "Resources" );
            EnsureFolder( "Assets/Resources", "Data" );
            EnsureFolder( "Assets/Resources/Data", "Item" );
        }

        ///<summary>
        /// 폴더 생성 보장
        ///</summary>
        private static string EnsureFolder( string _parentPath, string _folderName )
        {
            string folderPath = $"{_parentPath}/{_folderName}";

            if ( AssetDatabase.IsValidFolder( folderPath ) )
            {
                return folderPath;
            }

            AssetDatabase.CreateFolder( _parentPath, _folderName );
            return folderPath;
        }
    }
}
