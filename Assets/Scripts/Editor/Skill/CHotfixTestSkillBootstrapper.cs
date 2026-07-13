using System.Collections.Generic;
using System.IO;
using TinyHero.Player;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// Hotfix 테스트 스킬 에셋 생성 도구
    ///</summary>
    public static class CHotfixTestSkillBootstrapper
    {
        private const string RootFolderPath = "Assets/Resources/Data/Skill";
        private const string DefinitionFolderPath = "Assets/Resources/Data/Skill/Definitions";
        private const string ActiveEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Active";
        private const string ConditionFolderPath = "Assets/Resources/Data/Skill/Conditions";
        private const string IconFolderPath = "Assets/Resources/Data/Skill/Icons";
        private const string GameManagerPrefabAssetPath = "Assets/Resources/Prefabs/Core/CGameManager.prefab";
        private const string HotfixTestSkillId = "skill_hotfix_test";
        private const int IconTextureSize = 64;

        ///<summary>Hotfix 테스트 스킬 생성 메뉴 실행</summary>
        [MenuItem( "TinyHero/Skill/Generate Hotfix Test Skill" )]
        public static void GenerateHotfixTestSkillMenu()
        {
            string result = GenerateAndBind();
            Debug.Log( result );
        }

        ///<summary>Hotfix 테스트 스킬 생성 및 바인딩</summary>
        public static string GenerateAndBind()
        {
            EnsureFolderStructure();
            Sprite iconSprite = CreateColorIcon( $"{IconFolderPath}/Icon_HotfixTest.png", new Color32( 108, 255, 214, 255 ) );
            CLevelUnlockCondition unlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_01_HotfixTest.asset" );
            unlockCondition.Configure( 1 );
            EditorUtility.SetDirty( unlockCondition );

            CInstantActiveSkillEffect activeSkillEffect = CreateOrReplaceAsset<CInstantActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_HotfixTest_Instant.asset" );
            activeSkillEffect.Configure( Vector2.zero, 0.1f, 0.0f, 1, 1 );
            EditorUtility.SetDirty( activeSkillEffect );

            CSkillDefinition skillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_HotfixTest.asset" );
            skillDefinition.ConfigureActiveSkill( HotfixTestSkillId, "Hotfix Test", iconSprite, 5, 1, 0.0f, 0.0f, "HybridCLR Hotfix 연결 확인용 테스트 스킬입니다.", activeSkillEffect );
            skillDefinition.ConfigureCastSetting( 0.0f, ePlayerSkillCastAnimation.IDLE, "Idle", 1.0f );
            skillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { unlockCondition } );
            EditorUtility.SetDirty( skillDefinition );

            string bindResult = BindSkillToPlayer( skillDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return bindResult;
        }

        ///<summary>스킬 폴더 구조 보장</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets", "Resources" );
            EnsureFolder( "Assets/Resources", "Data" );
            EnsureFolder( "Assets/Resources/Data", "Skill" );
            EnsureFolder( RootFolderPath, "Definitions" );
            EnsureFolder( RootFolderPath, "Effects" );
            EnsureFolder( "Assets/Resources/Data/Skill/Effects", "Active" );
            EnsureFolder( RootFolderPath, "Conditions" );
            EnsureFolder( RootFolderPath, "Icons" );
        }

        ///<summary>폴더 생성 보장</summary>
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

        ///<summary>스크립터블 오브젝트 에셋 생성 또는 재사용</summary>
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

        ///<summary>단색 스킬 아이콘 생성</summary>
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

        ///<summary>플레이어 스킬 매니저에 Hotfix 테스트 스킬 연결</summary>
        private static string BindSkillToPlayer( CSkillDefinition _skillDefinition )
        {
            GameObject gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( GameManagerPrefabAssetPath );

            if ( gameManagerPrefab == null )
            {
                return "Hotfix test skill asset created. CGameManager prefab was not found.";
            }

            CSkillManager skillManager = gameManagerPrefab.GetComponentInChildren<CSkillManager>( true );

            if ( skillManager == null )
            {
                return "Hotfix test skill asset created. CSkillManager was not found under CGameManager.";
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
            AssetDatabase.SaveAssets();
            return "Hotfix test skill asset created and bound to CGameManager.";
        }
    }
}
