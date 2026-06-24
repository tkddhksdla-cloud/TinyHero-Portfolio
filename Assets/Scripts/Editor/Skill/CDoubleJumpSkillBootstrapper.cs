using System.Collections.Generic;
using System.IO;
using TinyHero.Player;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 더블 점프 스킬 에셋 생성 도구
    ///</summary>
    public static class CDoubleJumpSkillBootstrapper
    {
        private const string RootFolderPath = "Assets/Data/Skill";
        private const string DefinitionFolderPath = "Assets/Data/Skill/Definitions";
        private const string ConditionFolderPath = "Assets/Data/Skill/Conditions";
        private const string IconFolderPath = "Assets/Data/Skill/Icons";
        private const string PlayerObjectName = "PlayerObject";
        private const int IconTextureSize = 64;

        ///<summary>
        /// 더블 점프 스킬 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Skill/Generate Double Jump Skill" )]
        public static void GenerateDoubleJumpSkillMenu()
        {
            string result = GenerateAndBind();
            Debug.Log( result );
        }

        ///<summary>
        /// 더블 점프 스킬 생성 및 바인딩
        ///</summary>
        public static string GenerateAndBind()
        {
            EnsureFolderStructure();
            Sprite iconSprite = CreateColorIcon( $"{IconFolderPath}/Icon_DoubleJump.png", new Color32( 255, 236, 105, 255 ) );
            CLevelUnlockCondition unlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_01_DoubleJump.asset" );
            unlockCondition.Configure( 1 );
            EditorUtility.SetDirty( unlockCondition );

            CSkillDefinition skillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_DoubleJump.asset" );
            skillDefinition.ConfigureActiveSkill( "skill_double_jump", "Double Jump", iconSprite, 0, 1, 0.0f, 12.0f, "공중에서 한 번 더 도약한다. 퀵슬롯에 등록되지 않으며 C키 연속 입력으로 발동한다. 스킬 레벨이 오를수록 MP 소모가 {mpCost}까지 감소한다.", null );
            skillDefinition.ConfigureMpScaling( 2.0f );
            skillDefinition.SetAssignableToQuickSlot( false );
            skillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { unlockCondition } );
            EditorUtility.SetDirty( skillDefinition );

            string bindResult = BindSkillToPlayer( skillDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return bindResult;
        }

        ///<summary>
        /// 스킬 폴더 구조 보장
        ///</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets", "Data" );
            EnsureFolder( "Assets/Data", "Skill" );
            EnsureFolder( RootFolderPath, "Definitions" );
            EnsureFolder( RootFolderPath, "Conditions" );
            EnsureFolder( RootFolderPath, "Icons" );
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

        ///<summary>
        /// 스크립터블 오브젝트 에셋 생성 또는 재사용
        ///</summary>
        private static T CreateOrReplaceAsset<T>( string _assetPath ) where T : ScriptableObject
        {
            T existingAsset = AssetDatabase.LoadAssetAtPath<T>( _assetPath );

            if ( existingAsset != null )
            {
                return existingAsset;
            }

            T createdAsset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset( createdAsset, _assetPath );
            return createdAsset;
        }

        ///<summary>
        /// 단색 스킬 아이콘 생성
        ///</summary>
        private static Sprite CreateColorIcon( string _assetPath, Color32 _color )
        {
            Texture2D texture = new Texture2D( IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false );
            Color32[] pixelArray = new Color32[ IconTextureSize * IconTextureSize ];

            for ( int index = 0; index < pixelArray.Length; index++ )
            {
                pixelArray[ index ] = _color;
            }

            texture.SetPixels32( pixelArray );
            texture.Apply();
            byte[] pngBytes = texture.EncodeToPNG();
            Object.DestroyImmediate( texture );
            File.WriteAllBytes( _assetPath, pngBytes );
            AssetDatabase.ImportAsset( _assetPath, ImportAssetOptions.ForceUpdate );
            TextureImporter textureImporter = AssetImporter.GetAtPath( _assetPath ) as TextureImporter;

            if ( textureImporter != null )
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.mipmapEnabled = false;
                textureImporter.alphaIsTransparency = true;
                textureImporter.SaveAndReimport();
            }

            Sprite result = AssetDatabase.LoadAssetAtPath<Sprite>( _assetPath );
            return result;
        }

        ///<summary>
        /// 플레이어 스킬 매니저에 더블 점프 스킬 연결
        ///</summary>
        private static string BindSkillToPlayer( CSkillDefinition _skillDefinition )
        {
            GameObject playerObject = GameObject.Find( PlayerObjectName );

            if ( playerObject == null )
            {
                return "PlayerObject was not found.";
            }

            CSkillManager skillManager = playerObject.GetComponent<CSkillManager>();

            if ( skillManager == null )
            {
                return "CSkillManager was not found on PlayerObject.";
            }

            SerializedObject serializedSkillManager = new SerializedObject( skillManager );
            SerializedProperty useDefaultSampleSkillsProperty = serializedSkillManager.FindProperty( "useDefaultSampleSkills" );
            SerializedProperty skillDefinitionListProperty = serializedSkillManager.FindProperty( "skillDefinitionList" );
            useDefaultSampleSkillsProperty.boolValue = false;
            bool hasExistingSkill = false;

            for ( int index = 0; index < skillDefinitionListProperty.arraySize; index++ )
            {
                SerializedProperty skillProperty = skillDefinitionListProperty.GetArrayElementAtIndex( index );
                CSkillDefinition existingSkillDefinition = skillProperty.objectReferenceValue as CSkillDefinition;

                if ( existingSkillDefinition == _skillDefinition )
                {
                    hasExistingSkill = true;
                    break;
                }
            }

            if ( hasExistingSkill == false )
            {
                int newIndex = skillDefinitionListProperty.arraySize;
                skillDefinitionListProperty.InsertArrayElementAtIndex( newIndex );
                skillDefinitionListProperty.GetArrayElementAtIndex( newIndex ).objectReferenceValue = _skillDefinition;
            }

            serializedSkillManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( skillManager );
            return "Double Jump skill asset created and bound to PlayerObject.";
        }
    }
}
