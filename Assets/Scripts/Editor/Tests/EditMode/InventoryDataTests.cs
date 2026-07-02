using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 인벤토리 데이터와 매니저 EditMode 검증
    ///</summary>
    public sealed class InventoryDataTests
    {
        private GameObject inventoryManagerObject;
        private CPlayerInventoryManager inventoryManager;
        private CItemDefinition materialItemDefinition;
        private CItemDefinition questItemDefinition;
        private CItemDefinition consumableItemDefinition;

        ///<summary>
        /// 테스트용 인벤토리 매니저 생성
        ///</summary>
        [SetUp]
        public void SetUp()
        {
            inventoryManagerObject = new GameObject( "InventoryDataTests_CPlayerInventoryManager" );
            inventoryManager = inventoryManagerObject.AddComponent<CPlayerInventoryManager>();
            materialItemDefinition = CreateItemDefinition( "ITEM_MATERIAL_TEST", eItemType.MATERIAL, true, 99L );
            questItemDefinition = CreateItemDefinition( "ITEM_QUEST_TEST", eItemType.QUEST_ITEM, true, 99L );
            consumableItemDefinition = CreateItemDefinition( "ITEM_CONSUMABLE_TEST", eItemType.CONSUMABLE, true, 99L );
            InjectItemDefinitionDatabase( new List<CItemDefinition> { materialItemDefinition, questItemDefinition, consumableItemDefinition } );
        }

        ///<summary>
        /// 테스트용 인벤토리 매니저 정리
        ///</summary>
        [TearDown]
        public void TearDown()
        {
            if ( inventoryManagerObject != null )
            {
                Object.DestroyImmediate( inventoryManagerObject );
            }

            if ( materialItemDefinition != null )
            {
                Object.DestroyImmediate( materialItemDefinition );
            }

            if ( questItemDefinition != null )
            {
                Object.DestroyImmediate( questItemDefinition );
            }

            if ( consumableItemDefinition != null )
            {
                Object.DestroyImmediate( consumableItemDefinition );
            }

            ClearItemDefinitionDatabase();
        }

        ///<summary>
        /// 인벤토리 슬롯 초기 구성 검증
        ///</summary>
        [Test]
        public void InventoryManager_InitializesCategorySlots()
        {
            Assert.AreEqual( 48, inventoryManager.GetSlotCountPerItemType() );
            Assert.AreEqual( 240, inventoryManager.GetSlotCount() );
            Assert.AreEqual( 5, inventoryManager.GetInventoryCategoryEntryList().Count );
            Assert.IsNotNull( inventoryManager.GetItemEntryData( eItemType.MATERIAL, 0 ) );
        }

        ///<summary>
        /// 슬롯 교체와 수량 제거 검증
        ///</summary>
        [Test]
        public void InventoryManager_ReplacesAndRemovesSlotItem()
        {
            CInventoryItemEntryData itemEntryData = CreateItemEntry( materialItemDefinition.GetItemId(), 7L );

            bool didReplace = inventoryManager.TryReplaceSlotItem( eItemType.MATERIAL, 0, itemEntryData );
            bool didRemove = inventoryManager.TryRemoveItemAtSlot( eItemType.MATERIAL, 0, 3L );
            CInventoryItemEntryData slotEntryData = inventoryManager.GetItemEntryData( eItemType.MATERIAL, 0 );

            Assert.IsTrue( didReplace );
            Assert.IsTrue( didRemove );
            Assert.AreEqual( materialItemDefinition.GetItemId(), slotEntryData.GetItemId() );
            Assert.AreEqual( 4L, slotEntryData.GetQuantity() );
        }

        ///<summary>
        /// 슬롯 수량 전체 제거 시 빈 슬롯 처리 검증
        ///</summary>
        [Test]
        public void InventoryManager_RemoveAllQuantityClearsSlot()
        {
            CInventoryItemEntryData itemEntryData = CreateItemEntry( materialItemDefinition.GetItemId(), 2L );
            inventoryManager.TryReplaceSlotItem( eItemType.MATERIAL, 0, itemEntryData );

            bool didRemove = inventoryManager.TryRemoveItemAtSlot( eItemType.MATERIAL, 0, 2L );
            CInventoryItemEntryData slotEntryData = inventoryManager.GetItemEntryData( eItemType.MATERIAL, 0 );

            Assert.IsTrue( didRemove );
            Assert.IsTrue( slotEntryData.IsEmpty() );
        }

        ///<summary>
        /// 서로 다른 타입 슬롯 교체 거부 검증
        ///</summary>
        [Test]
        public void InventoryManager_RejectsIncompatibleSlotReplacement()
        {
            CInventoryItemEntryData itemEntryData = CreateItemEntry( consumableItemDefinition.GetItemId(), 1L );

            bool didReplace = inventoryManager.TryReplaceSlotItem( eItemType.MATERIAL, 0, itemEntryData );

            Assert.IsFalse( didReplace );
            Assert.IsTrue( inventoryManager.GetItemEntryData( eItemType.MATERIAL, 0 ).IsEmpty() );
        }

        ///<summary>
        /// 점유 슬롯 스냅샷 생성 검증
        ///</summary>
        [Test]
        public void InventoryManager_CreateSnapshotData_CapturesOccupiedSlots()
        {
            CInventoryItemEntryData firstEntryData = CreateItemEntry( materialItemDefinition.GetItemId(), 4L );
            CInventoryItemEntryData secondEntryData = CreateItemEntry( questItemDefinition.GetItemId(), 1L );
            inventoryManager.TryReplaceSlotItem( eItemType.MATERIAL, 2, firstEntryData );
            inventoryManager.TryReplaceSlotItem( eItemType.QUEST_ITEM, 3, secondEntryData );

            CPlayerInventorySnapshotData snapshotData = inventoryManager.CreateSnapshotData();
            List<CInventoryOccupiedSlotSnapshotData> occupiedSlotSnapshotList = snapshotData.GetOccupiedSlotSnapshotList();

            Assert.AreEqual( 2, occupiedSlotSnapshotList.Count );
            Assert.IsTrue( ContainsOccupiedSlot( occupiedSlotSnapshotList, eItemType.MATERIAL, 2, materialItemDefinition.GetItemId(), 4L ) );
            Assert.IsTrue( ContainsOccupiedSlot( occupiedSlotSnapshotList, eItemType.QUEST_ITEM, 3, questItemDefinition.GetItemId(), 1L ) );
        }

        ///<summary>
        /// 인벤토리 엔트리 복사와 초기화 검증
        ///</summary>
        [Test]
        public void InventoryItemEntry_CopyAndClearPreservesExpectedState()
        {
            CInventoryItemEntryData sourceEntryData = CreateItemEntry( "ITEM_COPY", 9L );
            CInventoryItemEntryData copiedEntryData = sourceEntryData.CreateCopy();

            sourceEntryData.Clear();

            Assert.IsTrue( sourceEntryData.IsEmpty() );
            Assert.AreEqual( "ITEM_COPY", copiedEntryData.GetItemId() );
            Assert.AreEqual( 9L, copiedEntryData.GetQuantity() );
        }

        ///<summary>
        /// 테스트용 인벤토리 항목 생성
        ///</summary>
        private static CInventoryItemEntryData CreateItemEntry( string _itemId, long _quantity )
        {
            CInventoryItemEntryData itemEntryData = new CInventoryItemEntryData();
            itemEntryData.SetItemId( _itemId );
            itemEntryData.SetQuantity( _quantity );
            return itemEntryData;
        }

        ///<summary>
        /// 테스트용 아이템 정의 생성
        ///</summary>
        private static CItemDefinition CreateItemDefinition( string _itemId, eItemType _itemType, bool _isStackable, long _maxStackCount )
        {
            CItemDefinition itemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
            itemDefinition.Configure( _itemId, _itemId, _itemType, string.Empty, null, _isStackable, _maxStackCount );
            return itemDefinition;
        }

        ///<summary>
        /// 테스트용 아이템 정의 DB 주입
        ///</summary>
        private static void InjectItemDefinitionDatabase( List<CItemDefinition> _itemDefinitionList )
        {
            Dictionary<string, CItemDefinition> dictionary = GetItemDefinitionDictionary();
            List<CItemDefinition> list = GetItemDefinitionList();
            dictionary.Clear();
            list.Clear();

            for ( int index = 0; index < _itemDefinitionList.Count; index++ )
            {
                CItemDefinition itemDefinition = _itemDefinitionList[ index ];

                if ( itemDefinition == null )
                {
                    continue;
                }

                dictionary[ itemDefinition.GetItemId() ] = itemDefinition;
                list.Add( itemDefinition );
            }

            SetItemDefinitionDatabaseInitialized( true );
        }

        ///<summary>
        /// 테스트용 아이템 정의 DB 초기화
        ///</summary>
        private static void ClearItemDefinitionDatabase()
        {
            GetItemDefinitionDictionary().Clear();
            GetItemDefinitionList().Clear();
            SetItemDefinitionDatabaseInitialized( false );
        }

        ///<summary>
        /// 아이템 정의 DB 사전 반환
        ///</summary>
        private static Dictionary<string, CItemDefinition> GetItemDefinitionDictionary()
        {
            FieldInfo fieldInfo = typeof( CItemDefinitionDatabase ).GetField( "itemDefinitionDictionary", BindingFlags.NonPublic | BindingFlags.Static );
            Assert.IsNotNull( fieldInfo );
            Dictionary<string, CItemDefinition> result = fieldInfo.GetValue( null ) as Dictionary<string, CItemDefinition>;
            Assert.IsNotNull( result );
            return result;
        }

        ///<summary>
        /// 아이템 정의 DB 목록 반환
        ///</summary>
        private static List<CItemDefinition> GetItemDefinitionList()
        {
            FieldInfo fieldInfo = typeof( CItemDefinitionDatabase ).GetField( "itemDefinitionList", BindingFlags.NonPublic | BindingFlags.Static );
            Assert.IsNotNull( fieldInfo );
            List<CItemDefinition> result = fieldInfo.GetValue( null ) as List<CItemDefinition>;
            Assert.IsNotNull( result );
            return result;
        }

        ///<summary>
        /// 아이템 정의 DB 초기화 상태 설정
        ///</summary>
        private static void SetItemDefinitionDatabaseInitialized( bool _isInitialized )
        {
            FieldInfo fieldInfo = typeof( CItemDefinitionDatabase ).GetField( "isInitialized", BindingFlags.NonPublic | BindingFlags.Static );
            Assert.IsNotNull( fieldInfo );
            fieldInfo.SetValue( null, _isInitialized );
        }

        ///<summary>
        /// 특정 점유 슬롯 포함 여부 반환
        ///</summary>
        private static bool ContainsOccupiedSlot( List<CInventoryOccupiedSlotSnapshotData> _snapshotList, eItemType _itemType, int _localSlotIndex, string _itemId, long _quantity )
        {
            for ( int index = 0; index < _snapshotList.Count; index++ )
            {
                CInventoryOccupiedSlotSnapshotData snapshotData = _snapshotList[ index ];

                if ( snapshotData == null )
                {
                    continue;
                }

                bool isMatched = snapshotData.itemType == _itemType
                    && snapshotData.localSlotIndex == _localSlotIndex
                    && snapshotData.itemId == _itemId
                    && snapshotData.quantityValue == _quantity;

                if ( isMatched )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
