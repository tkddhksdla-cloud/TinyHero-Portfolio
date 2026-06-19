using System.IO;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 월드 아이템 드랍 샘플 구성 도구
    ///</summary>
    public static class CItemWorldDropBootstrapper
    {
        private const string PrefabFolderPath = "Assets/Resources/Prefabs/Item";
        private const string ItemDropPrefabFolderPath = "Assets/Resources/Prefabs/Item/Drops";
        private const string WorldDropPrefabPath = "Assets/Resources/Prefabs/Item/WorldItemDropObject.prefab";
        private const string SampleItemAssetPath = "Assets/Resources/Data/Item/Definitions/Item_Consumable_Apple.asset";
        private const string SampleMonsterPrefabPath = "Assets/Resources/Prefabs/Character/Monster/Monster_0001.prefab";

        ///<summary>
        /// 샘플 월드 드랍 구성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Item/Generate Sample World Drop Setup" )]
        public static void GenerateSampleWorldDropSetupMenu()
        {
            string result = GenerateSampleWorldDropSetup();
            Debug.Log( result );
        }

        ///<summary>
        /// 전체 아이템 드랍 프리팹 일괄 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Item/Generate World Drop Prefabs For All Items" )]
        public static void GenerateWorldDropPrefabsForAllItemsMenu()
        {
            string result = GenerateWorldDropPrefabsForAllItems();
            Debug.Log( result );
        }

        ///<summary>
        /// 샘플 월드 드랍 구성 실행
        ///</summary>
        public static string GenerateSampleWorldDropSetup()
        {
            EnsurePrefabFolderExists();
            CItemDefinition sampleItemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( SampleItemAssetPath );

            if ( sampleItemDefinition == null )
            {
                return $"Sample item definition was not found: {SampleItemAssetPath}";
            }

            string sharedPrefabResult = CreateOrUpdateSharedWorldDropPrefab( sampleItemDefinition );
            string itemPrefabResult = GenerateWorldDropPrefabForItem( sampleItemDefinition );
            string monsterResult = AssignSampleDropToMonsterPrefab( sampleItemDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string result = $"{sharedPrefabResult}\n{itemPrefabResult}\n{monsterResult}";
            return result;
        }

        ///<summary>
        /// 전체 아이템 드랍 프리팹 일괄 생성
        ///</summary>
        public static string GenerateWorldDropPrefabsForAllItems()
        {
            EnsurePrefabFolderExists();
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CItemDefinition", new string[] { "Assets/Resources/Data/Item/Definitions" } );

            if ( assetGuidArray == null || assetGuidArray.Length == 0 )
            {
                return "No item definitions were found.";
            }

            int generatedCount = 0;

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetGuid = assetGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                CItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( assetPath );

                if ( itemDefinition == null )
                {
                    continue;
                }

                GenerateWorldDropPrefabForItem( itemDefinition );
                generatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string result = $"Generated world drop prefabs for {generatedCount} item definitions.";
            return result;
        }

        ///<summary>
        /// 아이템 전용 드랍 프리팹 생성
        ///</summary>
        public static string GenerateWorldDropPrefabForItem( CItemDefinition _itemDefinition )
        {
            EnsurePrefabFolderExists();

            if ( _itemDefinition == null )
            {
                return "Item definition was not provided.";
            }

            string itemPrefabPath = GetItemWorldDropPrefabPath( _itemDefinition );
            string prefabResult = CreateOrUpdateWorldDropPrefabAsset( itemPrefabPath, _itemDefinition, $"Drop_{_itemDefinition.name}" );
            GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( itemPrefabPath );

            if ( createdPrefab != null && _itemDefinition.GetWorldDropPrefab() != createdPrefab )
            {
                _itemDefinition.SetWorldDropPrefab( createdPrefab );
                EditorUtility.SetDirty( _itemDefinition );
            }

            AssetDatabase.SaveAssets();
            return prefabResult;
        }

        ///<summary>
        /// 공용 드랍 프리팹 생성
        ///</summary>
        private static string CreateOrUpdateSharedWorldDropPrefab( CItemDefinition _sampleItemDefinition )
        {
            string result = CreateOrUpdateWorldDropPrefabAsset( WorldDropPrefabPath, _sampleItemDefinition, "WorldItemDropObject" );
            return result;
        }

        ///<summary>
        /// 드랍 프리팹 에셋 생성 또는 갱신
        ///</summary>
        private static string CreateOrUpdateWorldDropPrefabAsset( string _prefabAssetPath, CItemDefinition _sampleItemDefinition, string _prefabName )
        {
            bool hasExistingPrefab = File.Exists( _prefabAssetPath );
            GameObject prefabRoot = hasExistingPrefab ? PrefabUtility.LoadPrefabContents( _prefabAssetPath ) : null;
            bool shouldUnloadPrefabContents = prefabRoot != null;

            if ( prefabRoot == null )
            {
                prefabRoot = new GameObject( _prefabName );
            }
            else
            {
                prefabRoot.name = _prefabName;
            }

            try
            {
                SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();

                if ( spriteRenderer == null )
                {
                    spriteRenderer = prefabRoot.AddComponent<SpriteRenderer>();
                }

                spriteRenderer.sprite = _sampleItemDefinition.GetIconSprite();
                spriteRenderer.sortingOrder = 10;
                CircleCollider2D circleCollider = prefabRoot.GetComponent<CircleCollider2D>();

                if ( circleCollider == null )
                {
                    circleCollider = prefabRoot.AddComponent<CircleCollider2D>();
                }

                circleCollider.isTrigger = true;
                circleCollider.radius = 0.35f;
                CWorldItemDropObject worldItemDropObject = prefabRoot.GetComponent<CWorldItemDropObject>();

                if ( worldItemDropObject == null )
                {
                    worldItemDropObject = prefabRoot.AddComponent<CWorldItemDropObject>();
                }

                worldItemDropObject.ConfigureDrop( _sampleItemDefinition, 1 );
                SerializedObject serializedObject = new SerializedObject( worldItemDropObject );
                SerializedProperty spriteRendererProperty = serializedObject.FindProperty( "targetSpriteRenderer" );
                SerializedProperty pickupTriggerColliderProperty = serializedObject.FindProperty( "pickupTriggerCollider" );

                if ( spriteRendererProperty != null )
                {
                    spriteRendererProperty.objectReferenceValue = spriteRenderer;
                }

                if ( pickupTriggerColliderProperty != null )
                {
                    pickupTriggerColliderProperty.objectReferenceValue = circleCollider;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                if ( shouldUnloadPrefabContents )
                {
                    PrefabUtility.SaveAsPrefabAsset( prefabRoot, _prefabAssetPath );
                    return $"Updated world drop prefab: {_prefabAssetPath}";
                }

                PrefabUtility.SaveAsPrefabAsset( prefabRoot, _prefabAssetPath );
                return $"Created world drop prefab: {_prefabAssetPath}";
            }
            finally
            {
                if ( shouldUnloadPrefabContents )
                {
                    PrefabUtility.UnloadPrefabContents( prefabRoot );
                }
                else
                {
                    Object.DestroyImmediate( prefabRoot );
                }
            }
        }

        ///<summary>
        /// 아이템 전용 드랍 프리팹 경로 반환
        ///</summary>
        private static string GetItemWorldDropPrefabPath( CItemDefinition _itemDefinition )
        {
            string itemFileName = _itemDefinition.name;
            string result = $"{ItemDropPrefabFolderPath}/Drop_{itemFileName}.prefab";
            return result;
        }

        ///<summary>
        /// 샘플 몬스터 드랍 설정 반영
        ///</summary>
        private static string AssignSampleDropToMonsterPrefab( CItemDefinition _sampleItemDefinition )
        {
            if ( File.Exists( SampleMonsterPrefabPath ) == false )
            {
                return $"Sample monster prefab was not found: {SampleMonsterPrefabPath}";
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( SampleMonsterPrefabPath );

            try
            {
                MonsterObject monsterObject = prefabRoot.GetComponent<MonsterObject>();

                if ( monsterObject == null )
                {
                    return $"MonsterObject was not found on prefab: {SampleMonsterPrefabPath}";
                }

                SerializedObject serializedObject = new SerializedObject( monsterObject );
                SerializedProperty useItemDropProperty = serializedObject.FindProperty( "useItemDrop" );
                SerializedProperty itemDropEntryListProperty = serializedObject.FindProperty( "itemDropEntryList" );

                if ( useItemDropProperty != null )
                {
                    useItemDropProperty.boolValue = true;
                }

                if ( itemDropEntryListProperty != null )
                {
                    itemDropEntryListProperty.arraySize = 1;
                    SerializedProperty entryProperty = itemDropEntryListProperty.GetArrayElementAtIndex( 0 );
                    SerializedProperty itemDefinitionProperty = entryProperty.FindPropertyRelative( "itemDefinition" );
                    SerializedProperty dropChanceProperty = entryProperty.FindPropertyRelative( "dropChance" );
                    SerializedProperty minDropCountProperty = entryProperty.FindPropertyRelative( "minDropCount" );
                    SerializedProperty maxDropCountProperty = entryProperty.FindPropertyRelative( "maxDropCount" );

                    if ( itemDefinitionProperty != null )
                    {
                        itemDefinitionProperty.objectReferenceValue = _sampleItemDefinition;
                    }

                    if ( dropChanceProperty != null )
                    {
                        dropChanceProperty.floatValue = 1.0f;
                    }

                    if ( minDropCountProperty != null )
                    {
                        minDropCountProperty.intValue = 1;
                    }

                    if ( maxDropCountProperty != null )
                    {
                        maxDropCountProperty.intValue = 1;
                    }
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset( prefabRoot, SampleMonsterPrefabPath );
                return $"Assigned sample drop to monster prefab: {SampleMonsterPrefabPath}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( prefabRoot );
            }
        }

        ///<summary>
        /// 드랍 프리팹 폴더 생성 보장
        ///</summary>
        private static void EnsurePrefabFolderExists()
        {
            EnsureFolderExists( PrefabFolderPath );
            EnsureFolderExists( ItemDropPrefabFolderPath );
        }

        ///<summary>
        /// 대상 폴더 경로 생성 보장
        ///</summary>
        private static void EnsureFolderExists( string _folderPath )
        {
            if ( AssetDatabase.IsValidFolder( _folderPath ) )
            {
                return;
            }

            string[] folderSegmentArray = _folderPath.Split( '/' );
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
    }
}
