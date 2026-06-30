using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 인벤토리 슬롯 관리
    ///</summary>
    public sealed class CPlayerInventoryManager : MonoBehaviour
    {
        private const int SlotCountPerItemType = 48;

        [SerializeField] private List<CInventoryCategoryEntryData> inventoryCategoryEntryList = new List<CInventoryCategoryEntryData>();

        private readonly Dictionary<eItemType, List<CInventoryItemEntryData>> itemEntryListByType = new Dictionary<eItemType, List<CInventoryItemEntryData>>();

        public event Action<CPlayerInventoryManager> OnInventoryChanged;

        ///<summary>
        /// 인벤토리 슬롯 초기화
        ///</summary>
        private void Awake()
        {
            EnsureCategorySlots();
        }

        ///<summary>
        /// 전체 인벤토리 카테고리 목록 반환
        ///</summary>
        public IReadOnlyList<CInventoryCategoryEntryData> GetInventoryCategoryEntryList()
        {
            EnsureCategorySlots();
            IReadOnlyList<CInventoryCategoryEntryData> result = inventoryCategoryEntryList;
            return result;
        }

        ///<summary>
        /// 전역 순서 인벤토리 슬롯 목록 반환
        ///</summary>
        public IReadOnlyList<CInventoryItemEntryData> GetItemEntryList()
        {
            EnsureCategorySlots();
            List<CInventoryItemEntryData> flattenedEntryList = new List<CInventoryItemEntryData>();
            eItemType[] itemTypeArray = GetSupportedItemTypeArray();

            for ( int typeIndex = 0; typeIndex < itemTypeArray.Length; typeIndex++ )
            {
                List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemTypeArray[ typeIndex ] );

                for ( int slotIndex = 0; slotIndex < itemEntryList.Count; slotIndex++ )
                {
                    flattenedEntryList.Add( itemEntryList[ slotIndex ] );
                }
            }

            IReadOnlyList<CInventoryItemEntryData> result = flattenedEntryList;
            return result;
        }

        ///<summary>
        /// 아이템 타입별 슬롯 개수 반환
        ///</summary>
        public int GetSlotCountPerItemType()
        {
            int result = SlotCountPerItemType;
            return result;
        }

        ///<summary>
        /// 전체 슬롯 개수 반환
        ///</summary>
        public int GetSlotCount()
        {
            int result = SlotCountPerItemType * GetSupportedItemTypeCount();
            return result;
        }

        ///<summary>
        /// 아이템 타입과 로컬 슬롯 기준 엔트리 반환
        ///</summary>
        public CInventoryItemEntryData GetItemEntryData( eItemType _itemType, int _localSlotIndex )
        {
            EnsureCategorySlots();

            if ( IsValidLocalSlotIndex( _localSlotIndex ) == false )
            {
                return null;
            }

            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( _itemType );
            CInventoryItemEntryData result = itemEntryList[ _localSlotIndex ];
            return result;
        }

        ///<summary>
        /// 전역 슬롯 기준 엔트리 반환
        ///</summary>
        public CInventoryItemEntryData GetItemEntryData( int _slotIndex )
        {
            bool isResolved = TryConvertGlobalSlotIndex( _slotIndex, out eItemType itemType, out int localSlotIndex );

            if ( isResolved == false )
            {
                return null;
            }

            CInventoryItemEntryData result = GetItemEntryData( itemType, localSlotIndex );
            return result;
        }

        ///<summary>
        /// 아이템 타입과 로컬 슬롯 기준 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinitionAtSlot( eItemType _itemType, int _localSlotIndex )
        {
            CInventoryItemEntryData itemEntryData = GetItemEntryData( _itemType, _localSlotIndex );

            if ( itemEntryData == null || itemEntryData.IsEmpty() )
            {
                return null;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( itemEntryData.GetItemId(), out CItemDefinition itemDefinition );

            if ( hasDefinition == false )
            {
                return null;
            }

            return itemDefinition;
        }

        ///<summary>
        /// 전역 슬롯 기준 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinitionAtSlot( int _slotIndex )
        {
            bool isResolved = TryConvertGlobalSlotIndex( _slotIndex, out eItemType itemType, out int localSlotIndex );

            if ( isResolved == false )
            {
                return null;
            }

            CItemDefinition result = GetItemDefinitionAtSlot( itemType, localSlotIndex );
            return result;
        }

        ///<summary>
        /// 아이템 타입과 로컬 슬롯 기준 전역 슬롯 인덱스 반환
        ///</summary>
        public int GetSlotIndex( eItemType _itemType, int _localSlotIndex )
        {
            bool isResolved = TryResolveGlobalSlotIndex( _itemType, _localSlotIndex, out int resolvedSlotIndex );

            if ( isResolved == false )
            {
                return -1;
            }

            return resolvedSlotIndex;
        }

        ///<summary>
        /// 전역 슬롯 기준 아이템 타입과 로컬 슬롯 반환 시도
        ///</summary>
        public bool TryGetSlotLocation( int _slotIndex, out eItemType _itemType, out int _localSlotIndex )
        {
            bool result = TryConvertGlobalSlotIndex( _slotIndex, out _itemType, out _localSlotIndex );
            return result;
        }

        ///<summary>
        /// 아이템 정의 기준 아이템 추가 시도
        ///</summary>
        public bool TryAddItem( CItemDefinition _itemDefinition, long _count )
        {
            if ( _itemDefinition == null || _count <= 0 )
            {
                return false;
            }

            EnsureCategorySlots();
            string itemId = _itemDefinition.GetItemId();
            eItemType itemType = _itemDefinition.GetItemType();
            long remainingCount = _count;

            if ( _itemDefinition.IsStackable() )
            {
                remainingCount = TryStackToExistingSlots( itemType, itemId, remainingCount, _itemDefinition.GetMaxStackCount() );
            }

            remainingCount = TryFillEmptySlots( itemType, itemId, remainingCount, _itemDefinition.GetMaxStackCount() );

            if ( remainingCount == _count )
            {
                return false;
            }

            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 아이템 전체 수량 추가 가능 여부 반환
        ///</summary>
        public bool CanAddItem( CItemDefinition _itemDefinition, long _count )
        {
            if ( _itemDefinition == null || _count <= 0 )
            {
                return false;
            }

            EnsureCategorySlots();
            eItemType itemType = _itemDefinition.GetItemType();
            string itemId = _itemDefinition.GetItemId();
            long remainingCount = _count;
            long maxStackCount = _itemDefinition.GetMaxStackCount();
            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemType );

            if ( _itemDefinition.IsStackable() )
            {
                remainingCount = CalculateRemainingCountAfterExistingStackCapacity( itemEntryList, itemId, remainingCount, maxStackCount );
            }

            remainingCount = CalculateRemainingCountAfterEmptySlotCapacity( itemEntryList, remainingCount, maxStackCount );
            bool result = remainingCount <= 0L;
            return result;
        }

        ///<summary>
        /// 인벤토리 엔트리 직접 추가 시도
        ///</summary>
        public bool TryAddItemEntry( CInventoryItemEntryData _itemEntryData )
        {
            if ( _itemEntryData == null || _itemEntryData.IsEmpty() )
            {
                return false;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( _itemEntryData.GetItemId(), out CItemDefinition itemDefinition );

            if ( hasDefinition == false || itemDefinition == null )
            {
                return false;
            }

            if ( itemDefinition.IsStackable() )
            {
                bool didAddStackableItem = TryAddItem( itemDefinition, _itemEntryData.GetQuantity() );
                return didAddStackableItem;
            }

            EnsureCategorySlots();
            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemDefinition.GetItemType() );

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                CInventoryItemEntryData targetEntryData = itemEntryList[ index ];

                if ( targetEntryData == null )
                {
                    targetEntryData = new CInventoryItemEntryData();
                    itemEntryList[ index ] = targetEntryData;
                }

                if ( targetEntryData.IsEmpty() == false )
                {
                    continue;
                }

                targetEntryData.CopyFrom( _itemEntryData );
                RaiseInventoryChanged();
                return true;
            }

            return false;
        }

        ///<summary>
        /// 아이템 ID 기준 아이템 추가 시도
        ///</summary>
        public bool TryAddItemById( string _itemId, long _count )
        {
            if ( string.IsNullOrWhiteSpace( _itemId ) || _count <= 0 )
            {
                return false;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( _itemId, out CItemDefinition itemDefinition );

            if ( hasDefinition == false || itemDefinition == null )
            {
                return false;
            }

            bool result = TryAddItem( itemDefinition, _count );
            return result;
        }

        ///<summary>
        /// 타입별 로컬 슬롯 교환 시도
        ///</summary>
        public bool TrySwapSlotItems( eItemType _fromItemType, int _fromLocalSlotIndex, eItemType _toItemType, int _toLocalSlotIndex )
        {
            EnsureCategorySlots();

            if ( IsValidLocalSlotIndex( _fromLocalSlotIndex ) == false || IsValidLocalSlotIndex( _toLocalSlotIndex ) == false )
            {
                return false;
            }

            List<CInventoryItemEntryData> fromItemEntryList = GetOrCreateItemEntryList( _fromItemType );
            List<CInventoryItemEntryData> toItemEntryList = GetOrCreateItemEntryList( _toItemType );
            CInventoryItemEntryData fromEntryData = fromItemEntryList[ _fromLocalSlotIndex ];
            CInventoryItemEntryData toEntryData = toItemEntryList[ _toLocalSlotIndex ];
            CInventoryItemEntryData copiedFromEntryData = fromEntryData != null ? fromEntryData.CreateCopy() : new CInventoryItemEntryData();
            CInventoryItemEntryData copiedToEntryData = toEntryData != null ? toEntryData.CreateCopy() : new CInventoryItemEntryData();

            if ( IsCompatibleWithItemType( _toItemType, copiedFromEntryData ) == false || IsCompatibleWithItemType( _fromItemType, copiedToEntryData ) == false )
            {
                return false;
            }

            if ( fromEntryData == null )
            {
                fromEntryData = new CInventoryItemEntryData();
                fromItemEntryList[ _fromLocalSlotIndex ] = fromEntryData;
            }

            if ( toEntryData == null )
            {
                toEntryData = new CInventoryItemEntryData();
                toItemEntryList[ _toLocalSlotIndex ] = toEntryData;
            }

            fromEntryData.CopyFrom( copiedToEntryData );
            toEntryData.CopyFrom( copiedFromEntryData );
            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 전역 슬롯 기준 슬롯 교환 시도
        ///</summary>
        public bool TrySwapSlotItems( int _fromSlotIndex, int _toSlotIndex )
        {
            bool hasFromLocation = TryConvertGlobalSlotIndex( _fromSlotIndex, out eItemType fromItemType, out int fromLocalSlotIndex );
            bool hasToLocation = TryConvertGlobalSlotIndex( _toSlotIndex, out eItemType toItemType, out int toLocalSlotIndex );

            if ( hasFromLocation == false || hasToLocation == false )
            {
                return false;
            }

            bool result = TrySwapSlotItems( fromItemType, fromLocalSlotIndex, toItemType, toLocalSlotIndex );
            return result;
        }

        ///<summary>
        /// 타입별 로컬 슬롯 아이템 교체 처리
        ///</summary>
        public bool TryReplaceSlotItem( eItemType _itemType, int _localSlotIndex, CInventoryItemEntryData _itemEntryData )
        {
            EnsureCategorySlots();

            if ( IsValidLocalSlotIndex( _localSlotIndex ) == false )
            {
                return false;
            }

            if ( IsCompatibleWithItemType( _itemType, _itemEntryData ) == false )
            {
                return false;
            }

            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( _itemType );
            CInventoryItemEntryData targetEntryData = itemEntryList[ _localSlotIndex ];

            if ( targetEntryData == null )
            {
                targetEntryData = new CInventoryItemEntryData();
                itemEntryList[ _localSlotIndex ] = targetEntryData;
            }

            if ( _itemEntryData == null || _itemEntryData.IsEmpty() )
            {
                targetEntryData.Clear();
            }
            else
            {
                targetEntryData.CopyFrom( _itemEntryData );
            }

            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 전역 슬롯 기준 슬롯 아이템 교체 처리
        ///</summary>
        public bool TryReplaceSlotItem( int _slotIndex, CInventoryItemEntryData _itemEntryData )
        {
            bool isResolved = TryConvertGlobalSlotIndex( _slotIndex, out eItemType itemType, out int localSlotIndex );

            if ( isResolved == false )
            {
                return false;
            }

            bool result = TryReplaceSlotItem( itemType, localSlotIndex, _itemEntryData );
            return result;
        }

        ///<summary>
        /// 타입별 로컬 슬롯 잠재 데이터 반영 처리
        ///</summary>
        public bool TrySetItemEntryPotentialData( eItemType _itemType, int _localSlotIndex, CEquipmentPotentialData _equipmentPotentialData )
        {
            EnsureCategorySlots();

            if ( IsValidLocalSlotIndex( _localSlotIndex ) == false )
            {
                return false;
            }

            CInventoryItemEntryData targetEntryData = GetItemEntryData( _itemType, _localSlotIndex );

            if ( targetEntryData == null || targetEntryData.IsEmpty() )
            {
                return false;
            }

            CItemDefinition itemDefinition = GetItemDefinitionAtSlot( _itemType, _localSlotIndex );

            if ( itemDefinition == null || itemDefinition.IsEquipmentItem() == false )
            {
                return false;
            }

            targetEntryData.SetEquipmentPotentialData( _equipmentPotentialData );
            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 전역 슬롯 기준 잠재 데이터 반영 처리
        ///</summary>
        public bool TrySetItemEntryPotentialData( int _slotIndex, CEquipmentPotentialData _equipmentPotentialData )
        {
            bool isResolved = TryConvertGlobalSlotIndex( _slotIndex, out eItemType itemType, out int localSlotIndex );

            if ( isResolved == false )
            {
                return false;
            }

            bool result = TrySetItemEntryPotentialData( itemType, localSlotIndex, _equipmentPotentialData );
            return result;
        }

        ///<summary>
        /// 아이템 제거 시도
        ///</summary>
        public bool TryRemoveItem( string _itemId, long _count )
        {
            EnsureCategorySlots();

            if ( string.IsNullOrWhiteSpace( _itemId ) || _count <= 0 )
            {
                return false;
            }

            string normalizedItemId = _itemId.Trim();
            long currentItemCount = GetItemCount( normalizedItemId );

            if ( currentItemCount < _count )
            {
                return false;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( normalizedItemId, out CItemDefinition itemDefinition );

            if ( hasDefinition == false || itemDefinition == null )
            {
                return false;
            }

            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemDefinition.GetItemType() );
            long remainingCount = _count;

            for ( int index = itemEntryList.Count - 1; index >= 0; index-- )
            {
                CInventoryItemEntryData itemEntryData = itemEntryList[ index ];

                if ( itemEntryData == null || itemEntryData.IsEmpty() )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemEntryData.GetItemId(), normalizedItemId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                long quantity = itemEntryData.GetQuantity();
                long removedCount = Math.Min( quantity, remainingCount );
                long nextQuantity = quantity - removedCount;

                if ( nextQuantity <= 0 )
                {
                    itemEntryData.Clear();
                }
                else
                {
                    itemEntryData.SetQuantity( nextQuantity );
                }

                remainingCount -= removedCount;

                if ( remainingCount <= 0 )
                {
                    RaiseInventoryChanged();
                    return true;
                }
            }

            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 지정 슬롯 아이템 수량 제거 시도
        ///</summary>
        public bool TryRemoveItemAtSlot( eItemType _itemType, int _localSlotIndex, long _count )
        {
            EnsureCategorySlots();

            if ( IsValidLocalSlotIndex( _localSlotIndex ) == false || _count <= 0 )
            {
                return false;
            }

            CInventoryItemEntryData targetEntryData = GetItemEntryData( _itemType, _localSlotIndex );

            if ( targetEntryData == null || targetEntryData.IsEmpty() )
            {
                return false;
            }

            long currentQuantity = targetEntryData.GetQuantity();

            if ( currentQuantity < _count )
            {
                return false;
            }

            long nextQuantity = currentQuantity - _count;

            if ( nextQuantity <= 0 )
            {
                targetEntryData.Clear();
            }
            else
            {
                targetEntryData.SetQuantity( nextQuantity );
            }

            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 지정 전역 슬롯 아이템 수량 제거 시도
        ///</summary>
        public bool TryRemoveItemAtSlot( int _slotIndex, long _count )
        {
            bool isResolved = TryConvertGlobalSlotIndex( _slotIndex, out eItemType itemType, out int localSlotIndex );

            if ( isResolved == false )
            {
                return false;
            }

            bool result = TryRemoveItemAtSlot( itemType, localSlotIndex, _count );
            return result;
        }

        ///<summary>
        /// 아이템 보유 여부 반환
        ///</summary>
        public bool HasItem( string _itemId, long _requiredCount = 1L )
        {
            long itemCount = GetItemCount( _itemId );
            bool result = itemCount >= Math.Max( 1L, _requiredCount );
            return result;
        }

        ///<summary>
        /// 아이템 총 수량 반환
        ///</summary>
        public long GetItemCount( string _itemId )
        {
            EnsureCategorySlots();

            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                return 0L;
            }

            string normalizedItemId = _itemId.Trim();
            long totalQuantity = 0L;
            eItemType[] itemTypeArray = GetSupportedItemTypeArray();

            for ( int typeIndex = 0; typeIndex < itemTypeArray.Length; typeIndex++ )
            {
                eItemType itemType = itemTypeArray[ typeIndex ];
                List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemType );

                for ( int slotIndex = 0; slotIndex < itemEntryList.Count; slotIndex++ )
                {
                    CInventoryItemEntryData itemEntryData = itemEntryList[ slotIndex ];

                    if ( itemEntryData == null || itemEntryData.IsEmpty() )
                    {
                        continue;
                    }

                    bool isMatched = string.Equals( itemEntryData.GetItemId(), normalizedItemId, StringComparison.Ordinal );

                    if ( isMatched == false )
                    {
                        continue;
                    }

                    totalQuantity += itemEntryData.GetQuantity();
                }
            }

            return totalQuantity;
        }

        ///<summary>
        /// 인벤토리 저장 데이터 생성
        ///</summary>
        public CPlayerInventorySnapshotData CreateSnapshotData()
        {
            EnsureCategorySlots();
            CPlayerInventorySnapshotData snapshotData = new CPlayerInventorySnapshotData();
            List<CInventoryOccupiedSlotSnapshotData> occupiedSlotSnapshotList = new List<CInventoryOccupiedSlotSnapshotData>();
            eItemType[] itemTypeArray = GetSupportedItemTypeArray();

            for ( int typeIndex = 0; typeIndex < itemTypeArray.Length; typeIndex++ )
            {
                eItemType itemType = itemTypeArray[ typeIndex ];
                List<CInventoryItemEntryData> sourceEntryList = GetOrCreateItemEntryList( itemType );

                for ( int slotIndex = 0; slotIndex < sourceEntryList.Count; slotIndex++ )
                {
                    CInventoryItemEntryData sourceEntryData = sourceEntryList[ slotIndex ];

                    if ( sourceEntryData == null || sourceEntryData.IsEmpty() )
                    {
                        continue;
                    }

                    CInventoryOccupiedSlotSnapshotData occupiedSlotSnapshotData = CreateOccupiedSlotSnapshotData( itemType, slotIndex, sourceEntryData );
                    occupiedSlotSnapshotList.Add( occupiedSlotSnapshotData );
                }
            }

            snapshotData.SetOccupiedSlotSnapshotList( occupiedSlotSnapshotList );
            return snapshotData;
        }

        ///<summary>
        /// 인벤토리 저장 데이터 로드
        ///</summary>
        public void LoadSnapshotData( CPlayerInventorySnapshotData _snapshotData )
        {
            EnsureCategorySlots();
            ClearAllEntries();

            if ( _snapshotData == null )
            {
                RaiseInventoryChanged();
                return;
            }

            List<CInventoryOccupiedSlotSnapshotData> occupiedSlotSnapshotList = _snapshotData.GetOccupiedSlotSnapshotList();

            if ( occupiedSlotSnapshotList != null && occupiedSlotSnapshotList.Count > 0 )
            {
                LoadOccupiedSlotSnapshotData( occupiedSlotSnapshotList );
                return;
            }

            List<CInventoryCategoryEntryData> snapshotCategoryEntryList = _snapshotData.GetInventoryCategoryEntryList();

            if ( snapshotCategoryEntryList == null || snapshotCategoryEntryList.Count == 0 )
            {
                LoadLegacySnapshotData( _snapshotData.GetLegacyItemEntryList() );
                return;
            }

            for ( int categoryIndex = 0; categoryIndex < snapshotCategoryEntryList.Count; categoryIndex++ )
            {
                CInventoryCategoryEntryData sourceCategoryEntryData = snapshotCategoryEntryList[ categoryIndex ];

                if ( sourceCategoryEntryData == null )
                {
                    continue;
                }

                eItemType itemType = sourceCategoryEntryData.GetItemType();
                List<CInventoryItemEntryData> sourceEntryList = sourceCategoryEntryData.GetItemEntryList();

                if ( sourceEntryList == null )
                {
                    continue;
                }

                List<CInventoryItemEntryData> targetEntryList = GetOrCreateItemEntryList( itemType );
                int copyCount = Mathf.Min( sourceEntryList.Count, targetEntryList.Count );

                for ( int slotIndex = 0; slotIndex < copyCount; slotIndex++ )
                {
                    CInventoryItemEntryData sourceEntryData = sourceEntryList[ slotIndex ];

                    if ( sourceEntryData == null )
                    {
                        continue;
                    }

                    CInventoryItemEntryData targetEntryData = targetEntryList[ slotIndex ];

                    if ( targetEntryData == null )
                    {
                        targetEntryData = new CInventoryItemEntryData();
                        targetEntryList[ slotIndex ] = targetEntryData;
                    }

                    targetEntryData.CopyFrom( sourceEntryData );
                }
            }

            RaiseInventoryChanged();
        }

        ///<summary>
        /// 점유 인벤토리 슬롯 저장 데이터 생성
        ///</summary>
        private CInventoryOccupiedSlotSnapshotData CreateOccupiedSlotSnapshotData( eItemType _itemType, int _localSlotIndex, CInventoryItemEntryData _sourceEntryData )
        {
            CInventoryOccupiedSlotSnapshotData snapshotData = new CInventoryOccupiedSlotSnapshotData();
            snapshotData.itemType = _itemType;
            snapshotData.localSlotIndex = _localSlotIndex;
            snapshotData.itemId = _sourceEntryData.GetItemId();
            snapshotData.quantityValue = _sourceEntryData.GetQuantity();
            CEquipmentPotentialData potentialData = _sourceEntryData.GetEquipmentPotentialData();

            if ( potentialData != null && potentialData.HasPotential() )
            {
                snapshotData.equipmentPotentialSnapshotData = potentialData.CreateSnapshotData();
            }

            return snapshotData;
        }

        ///<summary>
        /// 점유 인벤토리 슬롯 저장 데이터 로드
        ///</summary>
        private void LoadOccupiedSlotSnapshotData( List<CInventoryOccupiedSlotSnapshotData> _occupiedSlotSnapshotList )
        {
            for ( int index = 0; index < _occupiedSlotSnapshotList.Count; index++ )
            {
                CInventoryOccupiedSlotSnapshotData sourceSlotSnapshotData = _occupiedSlotSnapshotList[ index ];

                if ( sourceSlotSnapshotData == null || sourceSlotSnapshotData.IsValid() == false )
                {
                    continue;
                }

                if ( IsValidLocalSlotIndex( sourceSlotSnapshotData.localSlotIndex ) == false )
                {
                    continue;
                }

                bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( sourceSlotSnapshotData.itemId, out CItemDefinition itemDefinition );

                if ( hasDefinition == false || itemDefinition == null || itemDefinition.GetItemType() != sourceSlotSnapshotData.itemType )
                {
                    continue;
                }

                List<CInventoryItemEntryData> targetEntryList = GetOrCreateItemEntryList( sourceSlotSnapshotData.itemType );
                CInventoryItemEntryData targetEntryData = targetEntryList[ sourceSlotSnapshotData.localSlotIndex ];

                if ( targetEntryData == null )
                {
                    targetEntryData = new CInventoryItemEntryData();
                    targetEntryList[ sourceSlotSnapshotData.localSlotIndex ] = targetEntryData;
                }

                targetEntryData.SetItemId( sourceSlotSnapshotData.itemId );
                targetEntryData.SetQuantity( sourceSlotSnapshotData.quantityValue );

                if ( itemDefinition.IsEquipmentItem() )
                {
                    targetEntryData.GetEquipmentPotentialData().LoadSnapshotData( sourceSlotSnapshotData.equipmentPotentialSnapshotData );
                }
            }

            RaiseInventoryChanged();
        }

        ///<summary>
        /// 인벤토리 카테고리와 슬롯 보장
        ///</summary>
        private void EnsureCategorySlots()
        {
            if ( inventoryCategoryEntryList == null )
            {
                inventoryCategoryEntryList = new List<CInventoryCategoryEntryData>();
            }

            itemEntryListByType.Clear();
            eItemType[] itemTypeArray = GetSupportedItemTypeArray();

            for ( int index = 0; index < itemTypeArray.Length; index++ )
            {
                eItemType itemType = itemTypeArray[ index ];
                CInventoryCategoryEntryData categoryEntryData = GetOrCreateCategoryEntryData( itemType );
                List<CInventoryItemEntryData> itemEntryList = categoryEntryData.GetItemEntryList();

                if ( itemEntryList == null )
                {
                    itemEntryList = new List<CInventoryItemEntryData>();
                    categoryEntryData.SetItemEntryList( itemEntryList );
                }

                while ( itemEntryList.Count < SlotCountPerItemType )
                {
                    itemEntryList.Add( new CInventoryItemEntryData() );
                }

                while ( itemEntryList.Count > SlotCountPerItemType )
                {
                    itemEntryList.RemoveAt( itemEntryList.Count - 1 );
                }

                itemEntryListByType[ itemType ] = itemEntryList;
            }
        }

        ///<summary>
        /// 아이템 타입별 카테고리 엔트리 반환
        ///</summary>
        private CInventoryCategoryEntryData GetOrCreateCategoryEntryData( eItemType _itemType )
        {
            for ( int index = 0; index < inventoryCategoryEntryList.Count; index++ )
            {
                CInventoryCategoryEntryData categoryEntryData = inventoryCategoryEntryList[ index ];

                if ( categoryEntryData == null )
                {
                    continue;
                }

                if ( categoryEntryData.GetItemType() != _itemType )
                {
                    continue;
                }

                return categoryEntryData;
            }

            CInventoryCategoryEntryData createdCategoryEntryData = new CInventoryCategoryEntryData();
            createdCategoryEntryData.SetItemType( _itemType );
            inventoryCategoryEntryList.Add( createdCategoryEntryData );
            return createdCategoryEntryData;
        }

        ///<summary>
        /// 아이템 타입별 슬롯 목록 반환
        ///</summary>
        private List<CInventoryItemEntryData> GetOrCreateItemEntryList( eItemType _itemType )
        {
            if ( itemEntryListByType.TryGetValue( _itemType, out List<CInventoryItemEntryData> itemEntryList ) )
            {
                return itemEntryList;
            }

            EnsureCategorySlots();
            List<CInventoryItemEntryData> resolvedEntryList = itemEntryListByType[ _itemType ];
            return resolvedEntryList;
        }

        ///<summary>
        /// 로컬 슬롯 인덱스 유효성 반환
        ///</summary>
        private bool IsValidLocalSlotIndex( int _localSlotIndex )
        {
            bool result = _localSlotIndex >= 0 && _localSlotIndex < SlotCountPerItemType;
            return result;
        }

        ///<summary>
        /// 전역 슬롯 인덱스 변환 시도
        ///</summary>
        private bool TryConvertGlobalSlotIndex( int _slotIndex, out eItemType _itemType, out int _localSlotIndex )
        {
            _itemType = eItemType.EQUIPMENT;
            _localSlotIndex = -1;

            if ( _slotIndex < 0 || _slotIndex >= GetSlotCount() )
            {
                return false;
            }

            int typeIndex = _slotIndex / SlotCountPerItemType;
            _localSlotIndex = _slotIndex % SlotCountPerItemType;
            bool hasType = TryConvertTypeIndexToItemType( typeIndex, out eItemType resolvedItemType );

            if ( hasType == false )
            {
                return false;
            }

            _itemType = resolvedItemType;
            return true;
        }

        ///<summary>
        /// 타입과 로컬 슬롯 기준 전역 슬롯 인덱스 계산 시도
        ///</summary>
        private bool TryResolveGlobalSlotIndex( eItemType _itemType, int _localSlotIndex, out int _resolvedSlotIndex )
        {
            _resolvedSlotIndex = -1;

            if ( IsValidLocalSlotIndex( _localSlotIndex ) == false )
            {
                return false;
            }

            int typeIndex = ConvertItemTypeToTypeIndex( _itemType );
            _resolvedSlotIndex = typeIndex * SlotCountPerItemType + _localSlotIndex;
            return true;
        }

        ///<summary>
        /// 아이템 타입 인덱스 변환
        ///</summary>
        private int ConvertItemTypeToTypeIndex( eItemType _itemType )
        {
            switch ( _itemType )
            {
                case eItemType.EQUIPMENT:
                    return 0;

                case eItemType.CONSUMABLE:
                    return 1;

                case eItemType.CURRENCY:
                    return 2;

                case eItemType.MATERIAL:
                    return 3;

                case eItemType.QUEST_ITEM:
                    return 4;
            }

            return 0;
        }

        ///<summary>
        /// 인덱스 기준 아이템 타입 변환 시도
        ///</summary>
        private bool TryConvertTypeIndexToItemType( int _typeIndex, out eItemType _itemType )
        {
            _itemType = eItemType.EQUIPMENT;

            switch ( _typeIndex )
            {
                case 0:
                    _itemType = eItemType.EQUIPMENT;
                    return true;

                case 1:
                    _itemType = eItemType.CONSUMABLE;
                    return true;

                case 2:
                    _itemType = eItemType.CURRENCY;
                    return true;

                case 3:
                    _itemType = eItemType.MATERIAL;
                    return true;

                case 4:
                    _itemType = eItemType.QUEST_ITEM;
                    return true;
            }

            return false;
        }

        ///<summary>
        /// 지원 아이템 타입 배열 반환
        ///</summary>
        private eItemType[] GetSupportedItemTypeArray()
        {
            eItemType[] result =
            {
                eItemType.EQUIPMENT,
                eItemType.CONSUMABLE,
                eItemType.CURRENCY,
                eItemType.MATERIAL,
                eItemType.QUEST_ITEM
            };
            return result;
        }

        ///<summary>
        /// 지원 아이템 타입 개수 반환
        ///</summary>
        private int GetSupportedItemTypeCount()
        {
            int result = GetSupportedItemTypeArray().Length;
            return result;
        }

        ///<summary>
        /// 타입별 기존 슬롯 중첩 처리
        ///</summary>
        private long TryStackToExistingSlots( eItemType _itemType, string _itemId, long _remainingCount, long _maxStackCount )
        {
            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( _itemType );
            long remainingCount = _remainingCount;

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0 )
                {
                    return 0L;
                }

                CInventoryItemEntryData itemEntryData = itemEntryList[ index ];

                if ( itemEntryData == null || itemEntryData.IsEmpty() )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemEntryData.GetItemId(), _itemId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                long currentQuantity = itemEntryData.GetQuantity();
                long availableCapacity = Math.Max( 0L, _maxStackCount - currentQuantity );

                if ( availableCapacity <= 0 )
                {
                    continue;
                }

                long addedCount = Math.Min( availableCapacity, remainingCount );
                itemEntryData.SetQuantity( currentQuantity + addedCount );
                remainingCount -= addedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 기존 중첩 슬롯 반영 후 잔여 수량 계산
        ///</summary>
        private long CalculateRemainingCountAfterExistingStackCapacity( List<CInventoryItemEntryData> _itemEntryList, string _itemId, long _remainingCount, long _maxStackCount )
        {
            if ( _itemEntryList == null )
            {
                return _remainingCount;
            }

            long remainingCount = _remainingCount;

            for ( int index = 0; index < _itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0L )
                {
                    return 0L;
                }

                CInventoryItemEntryData itemEntryData = _itemEntryList[ index ];

                if ( itemEntryData == null || itemEntryData.IsEmpty() )
                {
                    continue;
                }

                bool isMatched = string.Equals( itemEntryData.GetItemId(), _itemId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                long currentQuantity = itemEntryData.GetQuantity();
                long availableCapacity = Math.Max( 0L, _maxStackCount - currentQuantity );

                if ( availableCapacity <= 0L )
                {
                    continue;
                }

                long resolvedCount = Math.Min( availableCapacity, remainingCount );
                remainingCount -= resolvedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 빈 슬롯 반영 후 잔여 수량 계산
        ///</summary>
        private long CalculateRemainingCountAfterEmptySlotCapacity( List<CInventoryItemEntryData> _itemEntryList, long _remainingCount, long _maxStackCount )
        {
            if ( _itemEntryList == null )
            {
                return _remainingCount;
            }

            long remainingCount = _remainingCount;
            long slotCapacity = Math.Max( 1L, _maxStackCount );

            for ( int index = 0; index < _itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0L )
                {
                    return 0L;
                }

                CInventoryItemEntryData itemEntryData = _itemEntryList[ index ];

                if ( itemEntryData != null && itemEntryData.IsEmpty() == false )
                {
                    continue;
                }

                long resolvedCount = Math.Min( slotCapacity, remainingCount );
                remainingCount -= resolvedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 타입별 빈 슬롯 채우기 처리
        ///</summary>
        private long TryFillEmptySlots( eItemType _itemType, string _itemId, long _remainingCount, long _maxStackCount )
        {
            List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( _itemType );
            long remainingCount = _remainingCount;
            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( _itemId, out CItemDefinition itemDefinition );
            bool isEquipmentItem = hasDefinition && itemDefinition != null && itemDefinition.IsEquipmentItem();

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0 )
                {
                    return 0L;
                }

                CInventoryItemEntryData itemEntryData = itemEntryList[ index ];

                if ( itemEntryData == null )
                {
                    itemEntryData = new CInventoryItemEntryData();
                    itemEntryList[ index ] = itemEntryData;
                }

                if ( itemEntryData.IsEmpty() == false )
                {
                    continue;
                }

                long addedCount = Math.Min( Math.Max( 1L, _maxStackCount ), remainingCount );
                itemEntryData.SetItemId( _itemId );
                itemEntryData.SetQuantity( addedCount );

                if ( isEquipmentItem )
                {
                    CEquipmentPotentialData generatedPotentialData = new CEquipmentPotentialData();
                    itemEntryData.SetEquipmentPotentialData( generatedPotentialData );
                }
                else
                {
                    itemEntryData.GetEquipmentPotentialData().Clear();
                }

                remainingCount -= addedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 슬롯 타입 호환성 검증
        ///</summary>
        private bool IsCompatibleWithItemType( eItemType _itemType, CInventoryItemEntryData _itemEntryData )
        {
            if ( _itemEntryData == null || _itemEntryData.IsEmpty() )
            {
                return true;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( _itemEntryData.GetItemId(), out CItemDefinition itemDefinition );

            if ( hasDefinition == false || itemDefinition == null )
            {
                return false;
            }

            bool result = itemDefinition.GetItemType() == _itemType;
            return result;
        }

        ///<summary>
        /// 전체 슬롯 엔트리 초기화
        ///</summary>
        private void ClearAllEntries()
        {
            eItemType[] itemTypeArray = GetSupportedItemTypeArray();

            for ( int typeIndex = 0; typeIndex < itemTypeArray.Length; typeIndex++ )
            {
                List<CInventoryItemEntryData> itemEntryList = GetOrCreateItemEntryList( itemTypeArray[ typeIndex ] );

                for ( int slotIndex = 0; slotIndex < itemEntryList.Count; slotIndex++ )
                {
                    CInventoryItemEntryData itemEntryData = itemEntryList[ slotIndex ];

                    if ( itemEntryData == null )
                    {
                        itemEntryData = new CInventoryItemEntryData();
                        itemEntryList[ slotIndex ] = itemEntryData;
                    }

                    itemEntryData.Clear();
                }
            }
        }

        ///<summary>
        /// 구버전 인벤토리 저장 데이터 로드
        ///</summary>
        private void LoadLegacySnapshotData( List<CInventoryItemEntryData> _legacyItemEntryList )
        {
            if ( _legacyItemEntryList == null )
            {
                RaiseInventoryChanged();
                return;
            }

            int entryCount = _legacyItemEntryList.Count;

            for ( int index = 0; index < entryCount; index++ )
            {
                CInventoryItemEntryData sourceEntryData = _legacyItemEntryList[ index ];

                if ( sourceEntryData == null || sourceEntryData.IsEmpty() )
                {
                    continue;
                }

                bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( sourceEntryData.GetItemId(), out CItemDefinition itemDefinition );

                if ( hasDefinition == false || itemDefinition == null )
                {
                    continue;
                }

                TryAddItemEntry( sourceEntryData.CreateCopy() );
            }

            RaiseInventoryChanged();
        }

        ///<summary>
        /// 인벤토리 변경 이벤트 전파
        ///</summary>
        private void RaiseInventoryChanged()
        {
            if ( OnInventoryChanged != null )
            {
                OnInventoryChanged( this );
            }
        }
    }
}
