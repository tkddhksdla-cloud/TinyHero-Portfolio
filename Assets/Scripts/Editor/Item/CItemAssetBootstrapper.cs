using System.Collections.Generic;
using System.IO;
using LayerLab.ArtMakerUnity;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 샘플 아이템 에셋 생성 도구
    ///</summary>
    public static class CItemAssetBootstrapper
    {
        private const string RootFolderPath = "Assets/Resources/Data/Item";
        private const string DefinitionFolderPath = "Assets/Resources/Data/Item/Definitions";
        private const string IconFolderPath = "Assets/Images/Icons";
        private const string EquipmentIconFolderPath = "Assets/Images/Icons/EQUIPMENT";
        private const string ConsumableIconFolderPath = "Assets/Images/Icons/CONSUMABLE";
        private const string CurrencyIconFolderPath = "Assets/Images/Icons/CURRENCY";
        private const string MaterialIconFolderPath = "Assets/Images/Icons/MATERIAL";
        private const string QuestItemIconFolderPath = "Assets/Images/Icons/QUEST_ITEM";
        private const string FallbackIconAssetPath = "Assets/Images/Icons/Icon_Item_Sample.png";
        private const int IconTextureSize = 128;
        private const int CubeMaxStackCount = 99999;

        ///<summary>
        /// 샘플 아이템 에셋 생성
        ///</summary>
        public static string GenerateSampleItemAssets()
        {
            EnsureFolderStructure();
            Sprite equipmentIcon = CreateColorIcon( $"{EquipmentIconFolderPath}/ITEM_EQUIPMENT_BRONZE_SWORD.png", new Color32( 120, 176, 255, 255 ) );
            Sprite consumableIcon = CreateColorIcon( $"{ConsumableIconFolderPath}/ITEM_CONSUMABLE_APPLE.png", new Color32( 118, 214, 124, 255 ) );
            Sprite cubeIcon = CreateColorIcon( $"{ConsumableIconFolderPath}/ITEM_CONSUMABLE_CUBE.png", new Color32( 124, 213, 255, 255 ) );
            Sprite currencyIcon = CreateColorIcon( $"{CurrencyIconFolderPath}/GOLD.png", new Color32( 255, 210, 90, 255 ) );
            Sprite materialIcon = CreateColorIcon( $"{MaterialIconFolderPath}/ITEM_MATERIAL_SLIME_GEL.png", new Color32( 172, 133, 98, 255 ) );
            Sprite questItemIcon = CreateColorIcon( $"{QuestItemIconFolderPath}/ITEM_QUEST_ANCIENT_SEAL.png", new Color32( 235, 123, 123, 255 ) );
            CreateColorIcon( FallbackIconAssetPath, new Color32( 118, 214, 124, 255 ) );

            List<CItemDefinition> createdItemDefinitionList = new List<CItemDefinition>();
            CPlayerStatRuntimeData bronzeSwordStatBonus = new CPlayerStatRuntimeData();
            bronzeSwordStatBonus.SetStatValue( ePlayerStatType.ATK, 5.0f );
            createdItemDefinitionList.Add( CreateOrUpdateItemDefinition( $"{DefinitionFolderPath}/Item_Equipment_BronzeSword.asset", "ITEM_EQUIPMENT_BRONZE_SWORD", "Bronze Sword", eItemType.EQUIPMENT, "기본 장비 샘플 검.", equipmentIcon, false, 1, eEquipmentType.WEAPON, bronzeSwordStatBonus, PartsType.Sword, 0 ) );
            createdItemDefinitionList.Add( CreateOrUpdateItemDefinition( $"{DefinitionFolderPath}/Item_Consumable_Apple.asset", "ITEM_CONSUMABLE_APPLE", "Apple", eItemType.CONSUMABLE, "기본 소비 아이템 샘플.", consumableIcon, true, 99 ) );
            createdItemDefinitionList.Add( CreateOrUpdateConsumableItemDefinition( $"{DefinitionFolderPath}/Item_Consumable_Cube.asset", "ITEM_CONSUMABLE_CUBE", "Mystic Cube", "장착 장비의 잠재능력을 재설정하는 샘플 큐브.", cubeIcon, eConsumableType.CUBE, string.Empty, CubeMaxStackCount ) );
            createdItemDefinitionList.Add( CreateOrUpdateItemDefinition( $"{DefinitionFolderPath}/Item_Currency_Gold.asset", "GOLD", "Gold", eItemType.CURRENCY, "기본 골드 샘플.", currencyIcon, true, 999999 ) );
            createdItemDefinitionList.Add( CreateOrUpdateItemDefinition( $"{DefinitionFolderPath}/Item_Material_SlimeGel.asset", "ITEM_MATERIAL_SLIME_GEL", "Slime Gel", eItemType.MATERIAL, "기본 재료 샘플.", materialIcon, true, 999 ) );
            createdItemDefinitionList.Add( CreateOrUpdateItemDefinition( $"{DefinitionFolderPath}/Item_Quest_AncientSeal.asset", "ITEM_QUEST_ANCIENT_SEAL", "Ancient Seal", eItemType.QUEST_ITEM, "기본 퀘스트 아이템 샘플.", questItemIcon, true, 99 ) );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CItemDefinitionDatabase.Reload();
            string result = $"Sample item assets created: {createdItemDefinitionList.Count}";
            return result;
        }

        ///<summary>
        /// 샘플 아이템 에셋 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Item/Generate Sample Item Assets" )]
        public static void GenerateSampleItemAssetsMenu()
        {
            string result = GenerateSampleItemAssets();
            Debug.Log( result );
        }

        ///<summary>
        /// 아이템 데이터 폴더 구조 보장
        ///</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets", "Images" );
            EnsureFolder( "Assets/Images", "Icons" );
            EnsureFolder( IconFolderPath, "EQUIPMENT" );
            EnsureFolder( IconFolderPath, "CONSUMABLE" );
            EnsureFolder( IconFolderPath, "CURRENCY" );
            EnsureFolder( IconFolderPath, "MATERIAL" );
            EnsureFolder( IconFolderPath, "QUEST_ITEM" );
            EnsureFolder( "Assets", "Resources" );
            EnsureFolder( "Assets/Resources", "Data" );
            EnsureFolder( "Assets/Resources/Data", "Item" );
            EnsureFolder( RootFolderPath, "Definitions" );
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
        /// 아이템 정의 생성 또는 갱신
        ///</summary>
        private static CItemDefinition CreateOrUpdateItemDefinition( string _assetPath, string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount )
        {
            CItemDefinition result = CreateOrUpdateItemDefinition( _assetPath, _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, eEquipmentType.NONE, null, PartsType.Chest, -1 );
            return result;
        }

        ///<summary>
        /// 장비 아이템 정의 생성 또는 갱신
        ///</summary>
        private static CItemDefinition CreateOrUpdateItemDefinition( string _assetPath, string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount, eEquipmentType _equipmentType, CPlayerStatRuntimeData _equipmentStatBonus, PartsType _equipmentPartsType, int _equipmentPartsIndex )
        {
            CItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( _assetPath );

            if ( itemDefinition == null )
            {
                itemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
                AssetDatabase.CreateAsset( itemDefinition, _assetPath );
            }

            itemDefinition.Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, _equipmentType, _equipmentStatBonus, _equipmentPartsType, _equipmentPartsIndex );
            EditorUtility.SetDirty( itemDefinition );
            return itemDefinition;
        }

        ///<summary>
        /// 소비 아이템 정의 생성 또는 갱신
        ///</summary>
        private static CItemDefinition CreateOrUpdateConsumableItemDefinition( string _assetPath, string _itemId, string _itemName, string _description, Sprite _iconSprite, eConsumableType _consumableType, string _linkedSkillId, int _maxStackCount )
        {
            CItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<CItemDefinition>( _assetPath );

            if ( itemDefinition == null )
            {
                itemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
                AssetDatabase.CreateAsset( itemDefinition, _assetPath );
            }

            itemDefinition.Configure( _itemId, _itemName, eItemType.CONSUMABLE, _description, _iconSprite, true, _maxStackCount, eEquipmentType.NONE, _consumableType, _linkedSkillId, null, PartsType.Chest, -1 );
            EditorUtility.SetDirty( itemDefinition );
            return itemDefinition;
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
                textureImporter.spritePixelsPerUnit = IconTextureSize;
                textureImporter.SaveAndReimport();
            }

            Sprite result = AssetDatabase.LoadAssetAtPath<Sprite>( _assetPath );
            return result;
        }
    }
}
