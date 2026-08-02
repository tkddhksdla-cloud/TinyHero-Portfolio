using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 부유 무기 소환 샘플 스킬 에셋 생성 도구
    ///</summary>
    public static class CFloatingWeaponSummonSkillBootstrapper
    {
        private const string RootFolderPath = "Assets/Resources/Data/Skill";
        private const string DefinitionFolderPath = "Assets/Resources/Data/Skill/Definitions";
        private const string ActiveEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Active";
        private const string ConditionFolderPath = "Assets/Resources/Data/Skill/Conditions";
        private const string IconFolderPath = "Assets/Resources/Data/Skill/Icons";
        private const string FloatingWeaponPrefabFolderPath = "Assets/Resources/Prefabs/Skill";
        private const string FloatingWeaponPrefabAssetPath = "Assets/Resources/Prefabs/Skill/FloatingWeaponRetinueProjectile.prefab";
        private const string FloatingWeaponTrailPrefabAssetPath = "Assets/Resources/Prefabs/Skill/FloatingWeaponRetinueTrail.prefab";
        private const string FloatingWeaponTrailMaterialAssetPath = "Assets/Lana Studio/Casual RPG VFX/Materials/AB_01.mat";
        private const string GameManagerPrefabAssetPath = "Assets/Resources/Prefabs/Core/CGameManager.prefab";
        private const string HitVfxPrefabAssetPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_sharp.prefab";
        private const string SkillId = "skill_floating_weapon_retinue";
        private const int IconTextureSize = 64;

        ///<summary>
        /// 부유 무기 소환 샘플 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Skill/Generate Floating Weapon Summon Skill" )]
        public static void GenerateFloatingWeaponSummonSkillMenu()
        {
            string result = GenerateAndBind();
            Debug.Log( result );
        }

        ///<summary>
        /// 샘플 스킬 생성 및 CGameManager 바인딩
        ///</summary>
        public static string GenerateAndBind()
        {
            EnsureFolderStructure();
            Sprite iconSprite = CreateSkillIcon( $"{IconFolderPath}/Icon_FloatingWeaponRetinue.png" );
            GameObject floatingWeaponPrefab = CreateOrUpdateFloatingWeaponPrefab();
            GameObject floatingWeaponTrailPrefab = CreateOrUpdateFloatingWeaponTrailPrefab();
            CLevelUnlockCondition unlockCondition = CreateOrReuseAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_01_FloatingWeaponRetinue.asset" );
            unlockCondition.Configure( 1 );
            EditorUtility.SetDirty( unlockCondition );

            CFloatingWeaponSummonActiveSkillEffect activeSkillEffect = CreateOrReuseAsset<CFloatingWeaponSummonActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_FloatingWeaponRetinue_Summon.asset" );
            activeSkillEffect.Configure(
                3,
                30.0f,
                new Vector2( -0.9f, 0.9f ),
                0.65f,
                0.12f,
                2.4f,
                7.0f,
                1.2f,
                15.0f,
                0.35f,
                1.0f,
                0,
                floatingWeaponPrefab,
                floatingWeaponTrailPrefab,
                0.6f,
                720.0f
            );
            EditorUtility.SetDirty( activeSkillEffect );

            GameObject hitVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( HitVfxPrefabAssetPath );
            CSkillDefinition skillDefinition = CreateOrReuseAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_FloatingWeaponRetinue.asset" );
            skillDefinition.ConfigureActiveSkill(
                SkillId,
                "Floating Weapon Retinue",
                iconSprite,
                6,
                1,
                30.0f,
                30.0f,
                "장착 중인 무기 3개를 30초 동안 소환한다. 무기들은 주변 적이 남아 있는 동안 부메랑처럼 연속 공격하고, 적이 없을 때 플레이어 뒤의 편대로 복귀한다.",
                activeSkillEffect
            );
            skillDefinition.ConfigureCastSetting( 0.15f, ePlayerSkillCastAnimation.IDLE, "Idle", 1.0f );
            skillDefinition.ConfigureHitVfx( hitVfxPrefab, new Vector3( 0.0f, 0.5f, 0.0f ), 3.0f );
            skillDefinition.ConfigureAudioSetting( string.Empty, "SFX_MONSTER_HURT_01", string.Empty );
            skillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { unlockCondition } );
            EditorUtility.SetDirty( skillDefinition );

            string bindResult = BindSkillToGameManager( skillDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return bindResult;
        }

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
            EnsureFolder( "Assets/Resources/Prefabs", "Skill" );
        }

        private static GameObject CreateOrUpdateFloatingWeaponPrefab()
        {
            GameObject floatingWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( FloatingWeaponPrefabAssetPath );

            if ( floatingWeaponPrefab != null )
            {
                TrailRenderer legacyTrailRenderer = floatingWeaponPrefab.GetComponent<TrailRenderer>();

                if ( legacyTrailRenderer != null )
                {
                    Object.DestroyImmediate( legacyTrailRenderer, true );
                    EditorUtility.SetDirty( floatingWeaponPrefab );
                    AssetDatabase.SaveAssets();
                }

                return floatingWeaponPrefab;
            }

            GameObject prefabRoot = new GameObject( "FloatingWeaponRetinueProjectile" );
            SpriteRenderer spriteRenderer = prefabRoot.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "SkillEffect";
            PrefabUtility.SaveAsPrefabAsset( prefabRoot, FloatingWeaponPrefabAssetPath );
            Object.DestroyImmediate( prefabRoot );
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>( FloatingWeaponPrefabAssetPath );
            return result;
        }

        private static GameObject CreateOrUpdateFloatingWeaponTrailPrefab()
        {
            GameObject floatingWeaponTrailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( FloatingWeaponTrailPrefabAssetPath );
            Material trailMaterial = AssetDatabase.LoadAssetAtPath<Material>( FloatingWeaponTrailMaterialAssetPath );

            if ( floatingWeaponTrailPrefab != null )
            {
                TrailRenderer existingTrailRenderer = floatingWeaponTrailPrefab.GetComponent<TrailRenderer>();

                if ( existingTrailRenderer != null && existingTrailRenderer.sharedMaterial != trailMaterial )
                {
                    existingTrailRenderer.sharedMaterial = trailMaterial;
                    EditorUtility.SetDirty( floatingWeaponTrailPrefab );
                    AssetDatabase.SaveAssets();
                }

                return floatingWeaponTrailPrefab;
            }

            GameObject prefabRoot = new GameObject( "FloatingWeaponRetinueTrail" );
            TrailRenderer trailRenderer = prefabRoot.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.28f;
            trailRenderer.startWidth = 0.20f;
            trailRenderer.endWidth = 0.035f;
            trailRenderer.minVertexDistance = 0.03f;
            trailRenderer.sortingLayerName = "SkillEffect";
            trailRenderer.sortingOrder = 9;
            trailRenderer.sharedMaterial = trailMaterial;
            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new[] { new GradientColorKey( Color.white, 0.0f ), new GradientColorKey( Color.white, 1.0f ) },
                new[] { new GradientAlphaKey( 0.9f, 0.0f ), new GradientAlphaKey( 0.0f, 1.0f ) }
            );
            trailRenderer.colorGradient = trailGradient;
            trailRenderer.emitting = false;
            PrefabUtility.SaveAsPrefabAsset( prefabRoot, FloatingWeaponTrailPrefabAssetPath );
            Object.DestroyImmediate( prefabRoot );
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>( FloatingWeaponTrailPrefabAssetPath );
            return result;
        }

        private static string EnsureFolder( string _parentPath, string _folderName )
        {
            string folderPath = $"{_parentPath}/{_folderName}";

            if ( AssetDatabase.IsValidFolder( folderPath ) )
            {
                return folderPath;
            }

            string folderGuid = AssetDatabase.CreateFolder( _parentPath, _folderName );
            return folderGuid;
        }

        private static T CreateOrReuseAsset<T>( string _assetPath ) where T : ScriptableObject
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
        /// 세 자루 부유 무기를 표현하는 샘플 아이콘 생성
        ///</summary>
        private static Sprite CreateSkillIcon( string _assetPath )
        {
            Texture2D texture = new Texture2D( IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false );
            Color32 backgroundColor = new Color32( 16, 24, 48, 255 );
            Color32 glowColor = new Color32( 54, 202, 255, 255 );
            Color32 bladeColor = new Color32( 226, 248, 255, 255 );
            Color32 handleColor = new Color32( 255, 190, 72, 255 );
            Color32[] pixelArray = new Color32[ IconTextureSize * IconTextureSize ];

            for ( int index = 0; index < pixelArray.Length; index++ )
            {
                pixelArray[ index ] = backgroundColor;
            }

            DrawGlowCircle( pixelArray, 32, 32, 24, glowColor );
            DrawBlade( pixelArray, 20, 36, bladeColor, handleColor );
            DrawBlade( pixelArray, 32, 42, bladeColor, handleColor );
            DrawBlade( pixelArray, 44, 36, bladeColor, handleColor );
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

        private static void DrawGlowCircle( Color32[] _pixelArray, int _centerX, int _centerY, int _radius, Color32 _glowColor )
        {
            int radiusSquared = _radius * _radius;

            for ( int y = 0; y < IconTextureSize; y++ )
            {
                for ( int x = 0; x < IconTextureSize; x++ )
                {
                    int deltaX = x - _centerX;
                    int deltaY = y - _centerY;
                    int distanceSquared = deltaX * deltaX + deltaY * deltaY;

                    if ( distanceSquared > radiusSquared || distanceSquared < radiusSquared - 90 )
                    {
                        continue;
                    }

                    _pixelArray[ y * IconTextureSize + x ] = _glowColor;
                }
            }
        }

        private static void DrawBlade( Color32[] _pixelArray, int _centerX, int _centerY, Color32 _bladeColor, Color32 _handleColor )
        {
            for ( int y = -14; y <= 8; y++ )
            {
                int halfWidth = y > 2 ? 2 : 1;
                Color32 drawColor = y > 2 ? _handleColor : _bladeColor;

                for ( int x = -halfWidth; x <= halfWidth; x++ )
                {
                    int pixelX = _centerX + x;
                    int pixelY = _centerY + y;

                    if ( pixelX < 0 || pixelX >= IconTextureSize || pixelY < 0 || pixelY >= IconTextureSize )
                    {
                        continue;
                    }

                    _pixelArray[ pixelY * IconTextureSize + pixelX ] = drawColor;
                }
            }
        }

        private static string BindSkillToGameManager( CSkillDefinition _skillDefinition )
        {
            GameObject gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( GameManagerPrefabAssetPath );

            if ( gameManagerPrefab == null )
            {
                return "Floating weapon skill created. CGameManager prefab was not found.";
            }

            CSkillManager skillManager = gameManagerPrefab.GetComponentInChildren<CSkillManager>( true );

            if ( skillManager == null )
            {
                return "Floating weapon skill created. CSkillManager was not found under CGameManager.";
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
            return "Floating weapon summon skill created and bound to CGameManager.";
        }
    }
}
