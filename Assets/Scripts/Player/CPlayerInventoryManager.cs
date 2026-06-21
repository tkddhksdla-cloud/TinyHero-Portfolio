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
        private const int DefaultSlotCount = 35;

        [SerializeField] private int slotCount = DefaultSlotCount;
        [SerializeField] private List<CInventoryItemEntryData> itemEntryList = new List<CInventoryItemEntryData>();

        public event Action<CPlayerInventoryManager> OnInventoryChanged;

        ///<summary>
        /// 인벤토리 슬롯 초기화
        ///</summary>
        private void Awake()
        {
            EnsureSlotCapacity();
        }

        ///<summary>
        /// 전체 인벤토리 슬롯 목록 반환
        ///</summary>
        public IReadOnlyList<CInventoryItemEntryData> GetItemEntryList()
        {
            EnsureSlotCapacity();
            IReadOnlyList<CInventoryItemEntryData> result = itemEntryList;
            return result;
        }

        ///<summary>
        /// 인벤토리 슬롯 개수 반환
        ///</summary>
        public int GetSlotCount()
        {
            EnsureSlotCapacity();
            int result = slotCount;
            return result;
        }

        ///<summary>
        /// 슬롯 데이터 반환
        ///</summary>
        public CInventoryItemEntryData GetItemEntryData( int _slotIndex )
        {
            EnsureSlotCapacity();

            if ( IsValidSlotIndex( _slotIndex ) == false )
            {
                return null;
            }

            CInventoryItemEntryData result = itemEntryList[ _slotIndex ];
            return result;
        }

        ///<summary>
        /// 슬롯 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinitionAtSlot( int _slotIndex )
        {
            CInventoryItemEntryData itemEntryData = GetItemEntryData( _slotIndex );

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
        /// 아이템 정의 기준 아이템 추가 시도
        ///</summary>
        public bool TryAddItem( CItemDefinition _itemDefinition, int _count )
        {
            if ( _itemDefinition == null || _count <= 0 )
            {
                return false;
            }

            EnsureSlotCapacity();
            string itemId = _itemDefinition.GetItemId();
            int remainingCount = _count;

            if ( _itemDefinition.IsStackable() )
            {
                remainingCount = TryStackToExistingSlots( itemId, remainingCount, _itemDefinition.GetMaxStackCount() );
            }

            remainingCount = TryFillEmptySlots( itemId, remainingCount, _itemDefinition.GetMaxStackCount() );

            if ( remainingCount == _count )
            {
                return false;
            }

            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 아이템 ID 기준 아이템 추가 시도
        ///</summary>
        public bool TryAddItemById( string _itemId, int _count )
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
        /// 슬롯 교환 시도
        ///</summary>
        public bool TrySwapSlotItems( int _fromSlotIndex, int _toSlotIndex )
        {
            EnsureSlotCapacity();

            if ( IsValidSlotIndex( _fromSlotIndex ) == false || IsValidSlotIndex( _toSlotIndex ) == false )
            {
                return false;
            }

            if ( _fromSlotIndex == _toSlotIndex )
            {
                return false;
            }

            CInventoryItemEntryData fromEntryData = itemEntryList[ _fromSlotIndex ];
            CInventoryItemEntryData toEntryData = itemEntryList[ _toSlotIndex ];
            CInventoryItemEntryData copiedFromEntryData = fromEntryData != null ? fromEntryData.CreateCopy() : new CInventoryItemEntryData();
            CInventoryItemEntryData copiedToEntryData = toEntryData != null ? toEntryData.CreateCopy() : new CInventoryItemEntryData();

            if ( fromEntryData == null )
            {
                fromEntryData = new CInventoryItemEntryData();
                itemEntryList[ _fromSlotIndex ] = fromEntryData;
            }

            if ( toEntryData == null )
            {
                toEntryData = new CInventoryItemEntryData();
                itemEntryList[ _toSlotIndex ] = toEntryData;
            }

            fromEntryData.CopyFrom( copiedToEntryData );
            toEntryData.CopyFrom( copiedFromEntryData );
            RaiseInventoryChanged();
            return true;
        }

        ///<summary>
        /// 아이템 제거 시도
        ///</summary>
        ///<summary>
        /// 슬롯 아이템 직접 교체 처리
        ///</summary>
        public bool TryReplaceSlotItem( int _slotIndex, CInventoryItemEntryData _itemEntryData )
        {
            EnsureSlotCapacity();

            if ( IsValidSlotIndex( _slotIndex ) == false )
            {
                return false;
            }

            CInventoryItemEntryData targetEntryData = itemEntryList[ _slotIndex ];

            if ( targetEntryData == null )
            {
                targetEntryData = new CInventoryItemEntryData();
                itemEntryList[ _slotIndex ] = targetEntryData;
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
        /// ?꾩씠???쒓굅 ?쒕룄
        ///</summary>
        public bool TryRemoveItem( string _itemId, int _count )
        {
            EnsureSlotCapacity();

            if ( string.IsNullOrWhiteSpace( _itemId ) || _count <= 0 )
            {
                return false;
            }

            string normalizedItemId = _itemId.Trim();
            int currentItemCount = GetItemCount( normalizedItemId );

            if ( currentItemCount < _count )
            {
                return false;
            }

            int remainingCount = _count;

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

                int quantity = itemEntryData.GetQuantity();
                int removedCount = Mathf.Min( quantity, remainingCount );
                int nextQuantity = quantity - removedCount;

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
        /// 아이템 보유 여부 반환
        ///</summary>
        public bool HasItem( string _itemId, int _requiredCount = 1 )
        {
            int itemCount = GetItemCount( _itemId );
            bool result = itemCount >= Mathf.Max( 1, _requiredCount );
            return result;
        }

        ///<summary>
        /// 아이템 수량 반환
        ///</summary>
        public int GetItemCount( string _itemId )
        {
            EnsureSlotCapacity();

            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                return 0;
            }

            string normalizedItemId = _itemId.Trim();
            int totalQuantity = 0;

            for ( int index = 0; index < itemEntryList.Count; index++ )
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

                totalQuantity += itemEntryData.GetQuantity();
            }

            return totalQuantity;
        }

        ///<summary>
        /// 인벤토리 저장 데이터 생성
        ///</summary>
        public CPlayerInventorySnapshotData CreateSnapshotData()
        {
            EnsureSlotCapacity();
            CPlayerInventorySnapshotData snapshotData = new CPlayerInventorySnapshotData();
            List<CInventoryItemEntryData> snapshotEntryList = new List<CInventoryItemEntryData>();

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                CInventoryItemEntryData sourceEntryData = itemEntryList[ index ];
                CInventoryItemEntryData copiedEntryData = sourceEntryData != null ? sourceEntryData.CreateCopy() : new CInventoryItemEntryData();
                snapshotEntryList.Add( copiedEntryData );
            }

            snapshotData.SetItemEntryList( snapshotEntryList );
            return snapshotData;
        }

        ///<summary>
        /// 인벤토리 저장 데이터 로드
        ///</summary>
        public void LoadSnapshotData( CPlayerInventorySnapshotData _snapshotData )
        {
            EnsureSlotCapacity();

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                CInventoryItemEntryData itemEntryData = itemEntryList[ index ];

                if ( itemEntryData == null )
                {
                    itemEntryData = new CInventoryItemEntryData();
                    itemEntryList[ index ] = itemEntryData;
                }

                itemEntryData.Clear();
            }

            if ( _snapshotData == null )
            {
                RaiseInventoryChanged();
                return;
            }

            List<CInventoryItemEntryData> snapshotEntryList = _snapshotData.GetItemEntryList();

            if ( snapshotEntryList == null )
            {
                RaiseInventoryChanged();
                return;
            }

            int copyCount = Mathf.Min( snapshotEntryList.Count, itemEntryList.Count );

            for ( int index = 0; index < copyCount; index++ )
            {
                CInventoryItemEntryData sourceEntryData = snapshotEntryList[ index ];

                if ( sourceEntryData == null )
                {
                    continue;
                }

                CInventoryItemEntryData targetEntryData = itemEntryList[ index ];
                targetEntryData.CopyFrom( sourceEntryData );
            }

            RaiseInventoryChanged();
        }

        ///<summary>
        /// 슬롯 유효성 반환
        ///</summary>
        private bool IsValidSlotIndex( int _slotIndex )
        {
            bool result = _slotIndex >= 0 && _slotIndex < itemEntryList.Count;
            return result;
        }

        ///<summary>
        /// 기존 슬롯 중첩 처리
        ///</summary>
        private int TryStackToExistingSlots( string _itemId, int _remainingCount, int _maxStackCount )
        {
            int remainingCount = _remainingCount;

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0 )
                {
                    return 0;
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

                int currentQuantity = itemEntryData.GetQuantity();
                int availableCapacity = Mathf.Max( 0, _maxStackCount - currentQuantity );

                if ( availableCapacity <= 0 )
                {
                    continue;
                }

                int addedCount = Mathf.Min( availableCapacity, remainingCount );
                itemEntryData.SetQuantity( currentQuantity + addedCount );
                remainingCount -= addedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 빈 슬롯 채우기 처리
        ///</summary>
        private int TryFillEmptySlots( string _itemId, int _remainingCount, int _maxStackCount )
        {
            int remainingCount = _remainingCount;

            for ( int index = 0; index < itemEntryList.Count; index++ )
            {
                if ( remainingCount <= 0 )
                {
                    return 0;
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

                int addedCount = Mathf.Min( Mathf.Max( 1, _maxStackCount ), remainingCount );
                itemEntryData.SetItemId( _itemId );
                itemEntryData.SetQuantity( addedCount );
                remainingCount -= addedCount;
            }

            return remainingCount;
        }

        ///<summary>
        /// 슬롯 개수 보장
        ///</summary>
        private void EnsureSlotCapacity()
        {
            slotCount = Mathf.Max( 1, slotCount );

            if ( itemEntryList == null )
            {
                itemEntryList = new List<CInventoryItemEntryData>();
            }

            while ( itemEntryList.Count < slotCount )
            {
                itemEntryList.Add( new CInventoryItemEntryData() );
            }

            while ( itemEntryList.Count > slotCount )
            {
                itemEntryList.RemoveAt( itemEntryList.Count - 1 );
            }
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
