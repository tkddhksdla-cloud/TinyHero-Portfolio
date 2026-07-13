using System.Collections.Generic;
using System.IO;
using TinyHero.Player;
using TinyHero.Skill;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 스킬 샘플 에셋 생성 유틸리티
    ///</summary>
    public static class CSkillAssetBootstrapper
    {
        private const string RootFolderPath = "Assets/Resources/Data/Skill";
        private const string DefinitionFolderPath = "Assets/Resources/Data/Skill/Definitions";
        private const string EffectFolderPath = "Assets/Resources/Data/Skill/Effects";
        private const string ActiveEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Active";
        private const string BuffEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Buff";
        private const string DebuffEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Debuff";
        private const string PassiveEffectFolderPath = "Assets/Resources/Data/Skill/Effects/Passive";
        private const string ConditionFolderPath = "Assets/Resources/Data/Skill/Conditions";
        private const string IconFolderPath = "Assets/Resources/Data/Skill/Icons";
        private const string GameManagerPrefabAssetPath = "Assets/Resources/Prefabs/Core/CGameManager.prefab";
        private const int IconTextureSize = 64;

        ///<summary>
        /// 스킬 샘플 에셋 생성 실행
        ///</summary>
        public static string GenerateSampleSkillAssets()
        {
            EnsureFolderStructure();

            Sprite flameIcon = CreateColorIcon( $"{IconFolderPath}/Icon_FlameSlash.png", new Color32( 232, 103, 58, 255 ) );
            Sprite frostIcon = CreateColorIcon( $"{IconFolderPath}/Icon_FrostField.png", new Color32( 88, 178, 255, 255 ) );
            Sprite arcBoltIcon = CreateColorIcon( $"{IconFolderPath}/Icon_ArcBolt.png", new Color32( 104, 255, 181, 255 ) );
            Sprite phaseStrikeIcon = CreateColorIcon( $"{IconFolderPath}/Icon_PhaseStrike.png", new Color32( 255, 92, 156, 255 ) );
            Sprite echoCloneIcon = CreateColorIcon( $"{IconFolderPath}/Icon_EchoClone.png", new Color32( 173, 139, 255, 255 ) );
            Sprite warCryIcon = CreateColorIcon( $"{IconFolderPath}/Icon_WarCry.png", new Color32( 255, 189, 64, 255 ) );
            Sprite ironSkinIcon = CreateColorIcon( $"{IconFolderPath}/Icon_IronSkin.png", new Color32( 144, 144, 144, 255 ) );

            CLevelUnlockCondition flameUnlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_01.asset" );
            flameUnlockCondition.Configure( 1 );
            EditorUtility.SetDirty( flameUnlockCondition );

            CLevelUnlockCondition frostUnlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_02.asset" );
            frostUnlockCondition.Configure( 2 );
            EditorUtility.SetDirty( frostUnlockCondition );

            CQuestUnlockCondition warCryUnlockCondition = CreateOrReplaceAsset<CQuestUnlockCondition>( $"{ConditionFolderPath}/Cond_Quest_WarCry.asset" );
            warCryUnlockCondition.Configure( "quest_unlock_war_cry" );
            EditorUtility.SetDirty( warCryUnlockCondition );

            CLevelUnlockCondition ironSkinUnlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_03.asset" );
            ironSkinUnlockCondition.Configure( 3 );
            EditorUtility.SetDirty( ironSkinUnlockCondition );

            CLevelUnlockCondition arcBoltUnlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_04.asset" );
            arcBoltUnlockCondition.Configure( 4 );
            EditorUtility.SetDirty( arcBoltUnlockCondition );

            CLevelUnlockCondition echoCloneUnlockCondition = CreateOrReplaceAsset<CLevelUnlockCondition>( $"{ConditionFolderPath}/Cond_Level_05.asset" );
            echoCloneUnlockCondition.Configure( 5 );
            EditorUtility.SetDirty( echoCloneUnlockCondition );

            CDefReductionDebuffEffect flameDebuffEffect = CreateOrReplaceAsset<CDefReductionDebuffEffect>( $"{DebuffEffectFolderPath}/Debuff_FlameSlash_DefReduction.asset" );
            flameDebuffEffect.Configure( 4.0f, 0.2f, 0.35f );
            EditorUtility.SetDirty( flameDebuffEffect );

            CAtkReductionDebuffEffect frostDebuffEffect = CreateOrReplaceAsset<CAtkReductionDebuffEffect>( $"{DebuffEffectFolderPath}/Debuff_FrostField_AtkReduction.asset" );
            frostDebuffEffect.Configure( 3.0f, 2, 0.4f );
            EditorUtility.SetDirty( frostDebuffEffect );

            CFinalAttackPercentBuffEffect warCryAttackBuffEffect = CreateOrReplaceAsset<CFinalAttackPercentBuffEffect>( $"{BuffEffectFolderPath}/Buff_WarCry_FinalAttack.asset" );
            warCryAttackBuffEffect.Configure( 6.0f, 0.3f );
            EditorUtility.SetDirty( warCryAttackBuffEffect );

            CInvincibleBuffEffect warCryInvincibleBuffEffect = CreateOrReplaceAsset<CInvincibleBuffEffect>( $"{BuffEffectFolderPath}/Buff_WarCry_Invincible.asset" );
            warCryInvincibleBuffEffect.Configure( 1.5f );
            EditorUtility.SetDirty( warCryInvincibleBuffEffect );

            CPassiveStatSkillEffect ironSkinPassiveEffect = CreateOrReplaceAsset<CPassiveStatSkillEffect>( $"{PassiveEffectFolderPath}/Passive_IronSkin_Defense.asset" );
            ironSkinPassiveEffect.Configure( ePlayerStatType.DEF, 8.0f );
            EditorUtility.SetDirty( ironSkinPassiveEffect );

            CInstantActiveSkillEffect flameActiveEffect = CreateOrReplaceAsset<CInstantActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_FlameSlash_Instant.asset" );
            flameActiveEffect.Configure( new Vector2( 1.25f, 0.0f ), 1.35f, 1.8f, 4, 16 );
            flameActiveEffect.SetDebuffEffects( new List<CEnemyDebuffEffectBase> { flameDebuffEffect } );
            EditorUtility.SetDirty( flameActiveEffect );

            CPlaceActiveSkillEffect frostActiveEffect = CreateOrReplaceAsset<CPlaceActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_FrostField_Place.asset" );
            frostActiveEffect.Configure( new Vector2( 1.0f, 0.0f ), 2.0f, 5.0f, 1.0f, 1.25f, 2, 16 );
            frostActiveEffect.SetDebuffEffects( new List<CEnemyDebuffEffectBase> { frostDebuffEffect } );
            EditorUtility.SetDirty( frostActiveEffect );

            CProjectileActiveSkillEffect arcBoltActiveEffect = CreateOrReplaceAsset<CProjectileActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_ArcBolt_Projectile.asset" );
            arcBoltActiveEffect.Configure( new Vector2( 0.7f, 0.2f ), 0.45f, 6.0f, 10.5f, 1.35f, 3, 1 );
            EditorUtility.SetDirty( arcBoltActiveEffect );

            CPhaseStrikeActiveSkillEffect phaseStrikeActiveEffect = CreateOrReplaceAsset<CPhaseStrikeActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_PhaseStrike_Phase.asset" );
            phaseStrikeActiveEffect.Configure( 10, 0.15f, 1.15f, 2 );
            EditorUtility.SetDirty( phaseStrikeActiveEffect );

            CCloneReplayActiveSkillEffect echoCloneActiveEffect = CreateOrReplaceAsset<CCloneReplayActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_EchoClone_Replay.asset" );
            echoCloneActiveEffect.Configure( 6.0f, 0.45f, 0.65f, new Vector3( -0.35f, 0.0f, 0.0f ), 0.85f );
            EditorUtility.SetDirty( echoCloneActiveEffect );

            CBuffActiveSkillEffect warCryActiveEffect = CreateOrReplaceAsset<CBuffActiveSkillEffect>( $"{ActiveEffectFolderPath}/Effect_WarCry_Buff.asset" );
            warCryActiveEffect.SetBuffEffects( new List<CPlayerBuffEffectBase> { warCryAttackBuffEffect, warCryInvincibleBuffEffect } );
            EditorUtility.SetDirty( warCryActiveEffect );

            CSkillDefinition flameSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_FlameSlash.asset" );
            flameSkillDefinition.ConfigureActiveSkill( "skill_flame_slash", "Flame Slash", flameIcon, 0, 1, 2.0f, 10.0f, "Instant attack with a defense reduction debuff.", flameActiveEffect );
            flameSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { flameUnlockCondition } );
            EditorUtility.SetDirty( flameSkillDefinition );

            CSkillDefinition frostSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_FrostField.asset" );
            frostSkillDefinition.ConfigureActiveSkill( "skill_frost_field", "Frost Field", frostIcon, 1, 2, 5.0f, 18.0f, "Placed field that deals periodic damage and reduces attack.", frostActiveEffect );
            frostSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { frostUnlockCondition } );
            EditorUtility.SetDirty( frostSkillDefinition );

            CSkillDefinition arcBoltSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_ArcBolt.asset" );
            arcBoltSkillDefinition.ConfigureActiveSkill( "skill_arc_bolt", "Arc Bolt", arcBoltIcon, 2, 4, 3.0f, 12.0f, "Projectile skill that travels forward and damages the first target hit.", arcBoltActiveEffect );
            arcBoltSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { arcBoltUnlockCondition } );
            EditorUtility.SetDirty( arcBoltSkillDefinition );

            CSkillDefinition phaseStrikeSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_PhaseStrike.asset" );
            phaseStrikeSkillDefinition.ConfigureActiveSkill( "skill_phase_strike", "Phase Strike", phaseStrikeIcon, 4, 1, 14.0f, 22.0f, "Become invisible and invincible instantly. Strike visible enemies up to {hitCount} times every {hitInterval}s, dealing {damage}% damage per hit, then return to the cast position after {duration}s.", phaseStrikeActiveEffect );
            phaseStrikeSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { flameUnlockCondition } );
            EditorUtility.SetDirty( phaseStrikeSkillDefinition );

            CSkillDefinition echoCloneSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_EchoClone.asset" );
            echoCloneSkillDefinition.ConfigureActiveSkill( "skill_echo_clone", "Echo Clone", echoCloneIcon, 4, 5, 12.0f, 24.0f, "Summons a delayed replay clone that mimics movement, attacks, and skills.", echoCloneActiveEffect );
            echoCloneSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { echoCloneUnlockCondition } );
            EditorUtility.SetDirty( echoCloneSkillDefinition );

            CSkillDefinition warCrySkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_WarCry.asset" );
            warCrySkillDefinition.ConfigureActiveSkill( "skill_war_cry", "War Cry", warCryIcon, 3, 1, 8.0f, 20.0f, "Buff skill that grants final attack increase and brief invincibility.", warCryActiveEffect );
            warCrySkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { warCryUnlockCondition } );
            EditorUtility.SetDirty( warCrySkillDefinition );

            CSkillDefinition ironSkinSkillDefinition = CreateOrReplaceAsset<CSkillDefinition>( $"{DefinitionFolderPath}/Skill_IronSkin.asset" );
            ironSkinSkillDefinition.ConfigurePassiveSkill( "skill_iron_skin", "Iron Skin", ironSkinIcon, "Passive skill that increases defense.", new List<CPassiveSkillEffectBase> { ironSkinPassiveEffect } );
            ironSkinSkillDefinition.SetUnlockConditions( new List<CSkillUnlockConditionBase> { ironSkinUnlockCondition } );
            EditorUtility.SetDirty( ironSkinSkillDefinition );

            string bindingResult = BindSampleSkillsToPlayer( flameSkillDefinition, frostSkillDefinition, arcBoltSkillDefinition, phaseStrikeSkillDefinition, echoCloneSkillDefinition, warCrySkillDefinition, ironSkinSkillDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return bindingResult;
        }

        ///<summary>
        /// 스킬 샘플 에셋 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Skill/Generate Sample Skill Assets" )]
        public static void GenerateSampleSkillAssetsMenu()
        {
            string result = GenerateSampleSkillAssets();
            Debug.Log( result );
        }

        ///<summary>
        /// 스킬 데이터 폴더 구조 보장
        ///</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets/Data", "Skill" );
            EnsureFolder( RootFolderPath, "Definitions" );
            EnsureFolder( RootFolderPath, "Effects" );
            EnsureFolder( EffectFolderPath, "Active" );
            EnsureFolder( EffectFolderPath, "Buff" );
            EnsureFolder( EffectFolderPath, "Debuff" );
            EnsureFolder( EffectFolderPath, "Passive" );
            EnsureFolder( RootFolderPath, "Conditions" );
            EnsureFolder( RootFolderPath, "Icons" );
        }

        ///<summary>
        /// 에셋 폴더 생성 보장
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
        /// 스크립터블 오브젝트 에셋 교체 생성
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
        /// 단색 임시 아이콘 스프라이트 생성
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

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>( _assetPath );
            return sprite;
        }

        ///<summary>
        /// 샘플 스킬을 플레이어 스킬 매니저에 연결
        ///</summary>
        private static string BindSampleSkillsToPlayer( CSkillDefinition _flameSkillDefinition, CSkillDefinition _frostSkillDefinition, CSkillDefinition _arcBoltSkillDefinition, CSkillDefinition _phaseStrikeSkillDefinition, CSkillDefinition _echoCloneSkillDefinition, CSkillDefinition _warCrySkillDefinition, CSkillDefinition _ironSkinSkillDefinition )
        {
            GameObject gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( GameManagerPrefabAssetPath );

            if ( gameManagerPrefab == null )
            {
                return "CGameManager prefab was not found.";
            }

            CSkillManager skillManager = gameManagerPrefab.GetComponentInChildren<CSkillManager>( true );

            if ( skillManager == null )
            {
                return "CSkillManager was not found under CGameManager.";
            }

            SerializedObject serializedSkillManager = new SerializedObject( skillManager );
            SerializedProperty useDefaultSampleSkillsProperty = serializedSkillManager.FindProperty( "useDefaultSampleSkills" );
            SerializedProperty skillDefinitionListProperty = serializedSkillManager.FindProperty( "skillDefinitionList" );
            useDefaultSampleSkillsProperty.boolValue = false;
            skillDefinitionListProperty.arraySize = 7;
            skillDefinitionListProperty.GetArrayElementAtIndex( 0 ).objectReferenceValue = _flameSkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 1 ).objectReferenceValue = _frostSkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 2 ).objectReferenceValue = _arcBoltSkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 3 ).objectReferenceValue = _warCrySkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 4 ).objectReferenceValue = _phaseStrikeSkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 5 ).objectReferenceValue = _echoCloneSkillDefinition;
            skillDefinitionListProperty.GetArrayElementAtIndex( 6 ).objectReferenceValue = _ironSkinSkillDefinition;
            serializedSkillManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( skillManager );

            CQuestStateProvider questStateProvider = gameManagerPrefab.GetComponentInChildren<CQuestStateProvider>( true );

            if ( questStateProvider == null )
            {
                return "CQuestStateProvider was not found under CGameManager.";
            }

            AssetDatabase.SaveAssets();
            return "Sample skill assets created and bound to CGameManager.";
        }
    }
}
