using System;
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
        private const string ItemDefinitionFolderPath = "Assets/Resources/Data/Item/Definitions";
        private const string ItemIconFolderPath = "Assets/Images/Icons";
        private const string DefaultSampleIconAssetPath = "Assets/Images/Icons/Icon_Item_Sample.png";
        private const string PrefabFolderPath = "Assets/Resources/Prefabs/Item";
        private const string ItemDropPrefabFolderPath = "Assets/Resources/Prefabs/Item/Drops";
        private const string WorldDropPrefabPath = "Assets/Resources/Prefabs/Item/WorldItemDropObject.prefab";
        private const string SampleItemAssetPath = "Assets/Resources/Data/Item/Definitions/Item_Consumable_Apple.asset";
        private const string SampleMonsterPrefabPath = "Assets/Resources/Prefabs/Character/Monster/Monster_0001.prefab";
        private const float WorldDropTargetMaxSize = 0.64f;
        private const float WorldDropScaleMin = 0.05f;
        private const float WorldDropScaleMax = 1.0f;
        private const float WorldDropColliderWorldRadius = 0.35f;

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
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CItemDefinition", new string[] { ItemDefinitionFolderPath } );

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
        /// 아이템 자동 할당 처리
        ///</summary>
        public static bool AutoAssignItemAssets( CItemDefinition _itemDefinition, out string _resultMessage )
        {
            _resultMessage = string.Empty;

            if ( _itemDefinition == null )
            {
                _resultMessage = "Item definition was not provided.";
                return false;
            }

            string itemId = NormalizeItemId( _itemDefinition.GetItemId() );

            if ( string.IsNullOrWhiteSpace( itemId ) )
            {
                _resultMessage = "ItemId is required before auto assign.";
                return false;
            }

            eItemType itemType = _itemDefinition.GetItemType();
            bool hasResolvedIcon = TryResolveAutoAssignIconSprite( itemId, itemType, out Sprite resolvedIconSprite, out bool isFallbackIcon );

            if ( hasResolvedIcon == false || resolvedIconSprite == null )
            {
                _resultMessage = $"Fallback sample icon was not found: {DefaultSampleIconAssetPath}";
                return false;
            }

            if ( _itemDefinition.GetIconSprite() != resolvedIconSprite )
            {
                _itemDefinition.SetIconSprite( resolvedIconSprite );
                EditorUtility.SetDirty( _itemDefinition );
            }

            string prefabResult = GenerateWorldDropPrefabForItem( _itemDefinition );
            string iconAssetPath = AssetDatabase.GetAssetPath( resolvedIconSprite );
            string iconResult = isFallbackIcon ? $"Fallback sample icon assigned: {iconAssetPath}" : $"Item icon assigned: {iconAssetPath}";
            _resultMessage = $"{iconResult}\n{prefabResult}";
            return true;
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

            EnsureItemIconAssigned( _itemDefinition );
            string itemPrefabPath = GetItemWorldDropPrefabPath( _itemDefinition );
            string dropPrefabName = ResolveDropPrefabName( _itemDefinition );
            string prefabResult = CreateOrUpdateWorldDropPrefabAsset( itemPrefabPath, _itemDefinition, dropPrefabName );
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
                CircleCollider2D groundCollisionCollider = ResolveOrCreateGroundCollisionCollider( prefabRoot, circleCollider );
                Rigidbody2D targetRigidbody = ResolveOrCreateDropRigidbody( prefabRoot );
                CWorldItemDropObject worldItemDropObject = prefabRoot.GetComponent<CWorldItemDropObject>();

                if ( worldItemDropObject == null )
                {
                    worldItemDropObject = prefabRoot.AddComponent<CWorldItemDropObject>();
                }

                worldItemDropObject.ConfigureDrop( _sampleItemDefinition, 1 );
                SerializedObject serializedObject = new SerializedObject( worldItemDropObject );
                SerializedProperty spriteRendererProperty = serializedObject.FindProperty( "targetSpriteRenderer" );
                SerializedProperty pickupTriggerColliderProperty = serializedObject.FindProperty( "pickupTriggerCollider" );
                SerializedProperty groundCollisionColliderProperty = serializedObject.FindProperty( "groundCollisionCollider" );
                SerializedProperty targetRigidbodyProperty = serializedObject.FindProperty( "targetRigidbody" );

                if ( spriteRendererProperty != null )
                {
                    spriteRendererProperty.objectReferenceValue = spriteRenderer;
                }

                if ( pickupTriggerColliderProperty != null )
                {
                    pickupTriggerColliderProperty.objectReferenceValue = circleCollider;
                }

                if ( groundCollisionColliderProperty != null )
                {
                    groundCollisionColliderProperty.objectReferenceValue = groundCollisionCollider;
                }

                if ( targetRigidbodyProperty != null )
                {
                    targetRigidbodyProperty.objectReferenceValue = targetRigidbody;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                ApplyWorldDropVisualScale( prefabRoot.transform, spriteRenderer, circleCollider, groundCollisionCollider );

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
                    UnityEngine.Object.DestroyImmediate( prefabRoot );
                }
            }
        }

        ///<summary>
        /// 아이템 전용 드랍 프리팹 경로 반환
        ///</summary>
        private static string GetItemWorldDropPrefabPath( CItemDefinition _itemDefinition )
        {
            string dropPrefabName = ResolveDropPrefabName( _itemDefinition );
            string result = $"{ItemDropPrefabFolderPath}/{dropPrefabName}.prefab";
            return result;
        }

        ///<summary>
        /// 드랍 프리팹 이름 결정
        ///</summary>
        private static string ResolveDropPrefabName( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return "DROP_ITEM_UNKNOWN";
            }

            string normalizedItemId = NormalizeItemId( _itemDefinition.GetItemId() );
            string itemKey = string.IsNullOrWhiteSpace( normalizedItemId ) ? _itemDefinition.name : normalizedItemId;
            string result = $"DROP_{itemKey}";
            return result;
        }

        ///<summary>
        /// 아이템 타입 아이콘 폴더 경로 반환
        ///</summary>
        private static string GetItemTypeIconFolderPath( eItemType _itemType )
        {
            string result = $"{ItemIconFolderPath}/{_itemType}";
            return result;
        }

        ///<summary>
        /// 아이템 아이콘 자동 할당 보장
        ///</summary>
        private static void EnsureItemIconAssigned( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return;
            }

            if ( _itemDefinition.GetIconSprite() != null )
            {
                return;
            }

            string itemId = NormalizeItemId( _itemDefinition.GetItemId() );
            eItemType itemType = _itemDefinition.GetItemType();
            bool hasResolvedIcon = TryResolveAutoAssignIconSprite( itemId, itemType, out Sprite resolvedIconSprite, out _ );

            if ( hasResolvedIcon == false || resolvedIconSprite == null )
            {
                return;
            }

            _itemDefinition.SetIconSprite( resolvedIconSprite );
            EditorUtility.SetDirty( _itemDefinition );
        }

        ///<summary>
        /// 자동 할당 아이콘 탐색 시도
        ///</summary>
        private static bool TryResolveAutoAssignIconSprite( string _itemId, eItemType _itemType, out Sprite _resolvedIconSprite, out bool _isFallbackIcon )
        {
            _resolvedIconSprite = null;
            _isFallbackIcon = false;
            string normalizedItemId = NormalizeItemId( _itemId );
            string itemTypeIconFolderPath = GetItemTypeIconFolderPath( _itemType );

            if ( string.IsNullOrWhiteSpace( normalizedItemId ) == false )
            {
                _resolvedIconSprite = FindSpriteByItemId( normalizedItemId, itemTypeIconFolderPath );
            }

            if ( _resolvedIconSprite != null )
            {
                return true;
            }

            _resolvedIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>( DefaultSampleIconAssetPath );
            _isFallbackIcon = _resolvedIconSprite != null;
            return _resolvedIconSprite != null;
        }

        ///<summary>
        /// 아이템 ID 기반 스프라이트 탐색
        ///</summary>
        private static Sprite FindSpriteByItemId( string _itemId, string _iconFolderPath )
        {
            if ( string.IsNullOrWhiteSpace( _itemId ) || string.IsNullOrWhiteSpace( _iconFolderPath ) )
            {
                return null;
            }

            string[] assetGuidArray = AssetDatabase.FindAssets( _itemId, new string[] { _iconFolderPath } );
            Sprite fallbackSprite = null;

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetGuid = assetGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                Sprite matchedSprite = LoadBestMatchedSpriteFromPath( assetPath, _itemId );

                if ( matchedSprite == null )
                {
                    continue;
                }

                bool isExactMatched = string.Equals( matchedSprite.name, _itemId, StringComparison.OrdinalIgnoreCase );

                if ( isExactMatched )
                {
                    return matchedSprite;
                }

                if ( fallbackSprite == null )
                {
                    fallbackSprite = matchedSprite;
                }
            }

            return fallbackSprite;
        }

        ///<summary>
        /// 경로 기준 최적 스프라이트 탐색
        ///</summary>
        private static Sprite LoadBestMatchedSpriteFromPath( string _assetPath, string _itemId )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) || string.IsNullOrWhiteSpace( _itemId ) )
            {
                return null;
            }

            bool isExactFileName = string.Equals( Path.GetFileNameWithoutExtension( _assetPath ), _itemId, StringComparison.OrdinalIgnoreCase );
            UnityEngine.Object[] assetArray = AssetDatabase.LoadAllAssetsAtPath( _assetPath );
            Sprite bestMatchedSprite = null;
            float bestMatchedArea = -1.0f;

            for ( int index = 0; index < assetArray.Length; index++ )
            {
                Sprite sprite = assetArray[ index ] as Sprite;

                if ( sprite == null )
                {
                    continue;
                }

                bool isExactSpriteName = string.Equals( sprite.name, _itemId, StringComparison.OrdinalIgnoreCase );

                if ( isExactSpriteName )
                {
                    return sprite;
                }

                bool isPrefixMatched = sprite.name.StartsWith( _itemId, StringComparison.OrdinalIgnoreCase );

                if ( isPrefixMatched == false && isExactFileName == false )
                {
                    continue;
                }

                float spriteArea = sprite.rect.width * sprite.rect.height;

                if ( spriteArea <= bestMatchedArea )
                {
                    continue;
                }

                bestMatchedSprite = sprite;
                bestMatchedArea = spriteArea;
            }

            if ( bestMatchedSprite != null )
            {
                return bestMatchedSprite;
            }

            if ( isExactFileName == false )
            {
                return null;
            }

            Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>( _assetPath );
            return loadedSprite;
        }

        ///<summary>
        /// 월드 드랍 비주얼 크기 보정
        ///</summary>
        private static void ApplyWorldDropVisualScale( Transform _rootTransform, SpriteRenderer _spriteRenderer, CircleCollider2D _circleCollider, CircleCollider2D _groundCollisionCollider )
        {
            if ( _rootTransform == null )
            {
                return;
            }

            float resolvedScale = 1.0f;
            Sprite sprite = _spriteRenderer != null ? _spriteRenderer.sprite : null;

            if ( sprite != null )
            {
                Vector3 spriteSize = sprite.bounds.size;
                float maxSize = Mathf.Max( spriteSize.x, spriteSize.y );

                if ( maxSize > Mathf.Epsilon )
                {
                    resolvedScale = Mathf.Clamp( WorldDropTargetMaxSize / maxSize, WorldDropScaleMin, WorldDropScaleMax );
                }
            }

            _rootTransform.localScale = new Vector3( resolvedScale, resolvedScale, 1.0f );

            if ( _circleCollider == null )
            {
                return;
            }

            float colliderRadius = WorldDropColliderWorldRadius;

            if ( resolvedScale > Mathf.Epsilon )
            {
                colliderRadius = WorldDropColliderWorldRadius / resolvedScale;
            }

            _circleCollider.radius = colliderRadius;

            if ( _groundCollisionCollider == null )
            {
                return;
            }

            _groundCollisionCollider.offset = _circleCollider.offset;
            _groundCollisionCollider.radius = _circleCollider.radius;
        }

        ///<summary>
        /// 드랍용 리지드바디 보장
        ///</summary>
        private static Rigidbody2D ResolveOrCreateDropRigidbody( GameObject _prefabRoot )
        {
            if ( _prefabRoot == null )
            {
                return null;
            }

            Rigidbody2D targetRigidbody = _prefabRoot.GetComponent<Rigidbody2D>();

            if ( targetRigidbody == null )
            {
                targetRigidbody = _prefabRoot.AddComponent<Rigidbody2D>();
            }

            targetRigidbody.bodyType = RigidbodyType2D.Dynamic;
            targetRigidbody.gravityScale = 3.0f;
            targetRigidbody.linearDamping = 1.5f;
            targetRigidbody.angularDamping = 10.0f;
            targetRigidbody.freezeRotation = true;
            targetRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            targetRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return targetRigidbody;
        }

        ///<summary>
        /// 드랍용 바닥 충돌 콜라이더 보장
        ///</summary>
        private static CircleCollider2D ResolveOrCreateGroundCollisionCollider( GameObject _prefabRoot, CircleCollider2D _pickupTriggerCollider )
        {
            if ( _prefabRoot == null )
            {
                return null;
            }

            CircleCollider2D[] colliderArray = _prefabRoot.GetComponents<CircleCollider2D>();
            int colliderCount = colliderArray.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                CircleCollider2D currentCollider = colliderArray[ index ];

                if ( currentCollider == null || currentCollider == _pickupTriggerCollider )
                {
                    continue;
                }

                currentCollider.isTrigger = false;
                return currentCollider;
            }

            CircleCollider2D createdCollider = _prefabRoot.AddComponent<CircleCollider2D>();
            createdCollider.isTrigger = false;

            if ( _pickupTriggerCollider != null )
            {
                createdCollider.offset = _pickupTriggerCollider.offset;
                createdCollider.radius = _pickupTriggerCollider.radius;
            }

            return createdCollider;
        }

        ///<summary>
        /// 아이템 ID 정규화
        ///</summary>
        private static string NormalizeItemId( string _itemId )
        {
            string result = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
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
