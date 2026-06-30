using System;
using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 장비 슬롯 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerEquipmentSlotEntryData
    {
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private CEquipmentPotentialData equipmentPotentialData = new CEquipmentPotentialData();

        ///<summary>
        /// 장비 슬롯 타입 반환
        ///</summary>
        public eEquipmentType GetEquipmentType()
        {
            eEquipmentType result = equipmentType;
            return result;
        }

        ///<summary>
        /// 장비 슬롯 타입 설정
        ///</summary>
        public void SetEquipmentType( eEquipmentType _equipmentType )
        {
            equipmentType = _equipmentType;
        }

        ///<summary>
        /// 장착 아이템 ID 반환
        ///</summary>
        public string GetItemId()
        {
            string result = itemId;
            return result;
        }

        ///<summary>
        /// 장비 잠재 데이터 반환
        ///</summary>
        public CEquipmentPotentialData GetEquipmentPotentialData()
        {
            EnsurePotentialData();
            CEquipmentPotentialData result = equipmentPotentialData;
            return result;
        }

        ///<summary>
        /// 장착 아이템 ID 설정
        ///</summary>
        public void SetItemId( string _itemId )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
        }

        ///<summary>
        /// 장비 잠재 데이터 설정
        ///</summary>
        public void SetEquipmentPotentialData( CEquipmentPotentialData _equipmentPotentialData )
        {
            EnsurePotentialData();
            equipmentPotentialData.CopyFrom( _equipmentPotentialData );
        }

        ///<summary>
        /// 장착 아이템 존재 여부 반환
        ///</summary>
        public bool HasItem()
        {
            bool result = string.IsNullOrWhiteSpace( itemId ) == false;
            return result;
        }

        ///<summary>
        /// 장비 슬롯 초기화
        ///</summary>
        public void Clear()
        {
            itemId = string.Empty;
            EnsurePotentialData();
            equipmentPotentialData.Clear();
        }

        ///<summary>
        /// 장비 슬롯 데이터 복사본 생성
        ///</summary>
        public CPlayerEquipmentSlotEntryData CreateCopy()
        {
            CPlayerEquipmentSlotEntryData copiedSlotEntryData = new CPlayerEquipmentSlotEntryData();
            copiedSlotEntryData.SetEquipmentType( equipmentType );
            copiedSlotEntryData.SetItemId( itemId );
            copiedSlotEntryData.SetEquipmentPotentialData( equipmentPotentialData );
            return copiedSlotEntryData;
        }

        ///<summary>
        /// 장비 슬롯 데이터 복사 반영
        ///</summary>
        public void CopyFrom( CPlayerEquipmentSlotEntryData _sourceSlotEntryData )
        {
            if ( _sourceSlotEntryData == null )
            {
                Clear();
                return;
            }

            equipmentType = _sourceSlotEntryData.GetEquipmentType();
            itemId = _sourceSlotEntryData.GetItemId();
            SetEquipmentPotentialData( _sourceSlotEntryData.GetEquipmentPotentialData() );
        }

        ///<summary>
        /// 잠재 데이터 초기화 보장
        ///</summary>
        private void EnsurePotentialData()
        {
            if ( equipmentPotentialData != null )
            {
                return;
            }

            equipmentPotentialData = new CEquipmentPotentialData();
        }
    }

    ///<summary>
    /// 플레이어 장착 장비 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerEquippedItemSnapshotData
    {
        public eEquipmentType equipmentType = eEquipmentType.NONE;
        public string itemId = string.Empty;
        public CEquipmentPotentialSnapshotData equipmentPotentialSnapshotData = new CEquipmentPotentialSnapshotData();

        ///<summary>
        /// 장착 장비 저장 데이터 유효 여부 반환
        ///</summary>
        public bool IsValid()
        {
            bool result = equipmentType != eEquipmentType.NONE && string.IsNullOrWhiteSpace( itemId ) == false;
            return result;
        }
    }

    ///<summary>
    /// 플레이어 장비 상태 관리
    ///</summary>
    public sealed class CPlayerEquipmentManager : MonoBehaviour
    {
        [SerializeField] private CPlayerStatManager targetStatManager;
        [SerializeField] private List<CPlayerEquipmentSlotEntryData> equipmentSlotEntryList = new List<CPlayerEquipmentSlotEntryData>();

        private readonly CPlayerStatRuntimeData aggregatedEquipmentStatBonus = new CPlayerStatRuntimeData();
        private readonly CPlayerStatRuntimeData aggregatedEquipmentPercentStatBonus = new CPlayerStatRuntimeData();
        private readonly CPlayerModifierRuntimeData aggregatedEquipmentModifierBonus = new CPlayerModifierRuntimeData();

        public event Action<CPlayerEquipmentManager> OnEquipmentChanged;

        ///<summary>
        /// 장비 상태 초기화
        ///</summary>
        private void Awake()
        {
            ResolveStatManager();
            EnsureEquipmentSlots();
            RefreshEquipmentStatBonus();
        }

        ///<summary>
        /// 장비 슬롯 목록 반환
        ///</summary>
        public IReadOnlyList<CPlayerEquipmentSlotEntryData> GetEquipmentSlotEntryList()
        {
            EnsureEquipmentSlots();
            IReadOnlyList<CPlayerEquipmentSlotEntryData> result = equipmentSlotEntryList;
            return result;
        }

        ///<summary>
        /// 장비 슬롯 데이터 반환
        ///</summary>
        public CPlayerEquipmentSlotEntryData GetEquipmentSlotEntryData( eEquipmentType _equipmentType )
        {
            EnsureEquipmentSlots();
            CPlayerEquipmentSlotEntryData result = FindEquipmentSlotEntryData( _equipmentType );
            return result;
        }

        ///<summary>
        /// 장비 슬롯 데이터 탐색
        ///</summary>
        private CPlayerEquipmentSlotEntryData FindEquipmentSlotEntryData( eEquipmentType _equipmentType )
        {
            if ( equipmentSlotEntryList == null )
            {
                return null;
            }

            for ( int index = 0; index < equipmentSlotEntryList.Count; index++ )
            {
                CPlayerEquipmentSlotEntryData slotEntryData = equipmentSlotEntryList[ index ];

                if ( slotEntryData == null )
                {
                    continue;
                }

                if ( slotEntryData.GetEquipmentType() != _equipmentType )
                {
                    continue;
                }

                return slotEntryData;
            }

            return null;
        }

        ///<summary>
        /// 장착 아이템 ID 반환
        ///</summary>
        public string GetEquippedItemId( eEquipmentType _equipmentType )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null )
            {
                return string.Empty;
            }

            string result = slotEntryData.GetItemId();
            return result;
        }

        ///<summary>
        /// 장착 잠재 데이터 반환
        ///</summary>
        public CEquipmentPotentialData GetEquippedPotentialData( eEquipmentType _equipmentType )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null || slotEntryData.HasItem() == false )
            {
                return null;
            }

            CEquipmentPotentialData result = slotEntryData.GetEquipmentPotentialData();
            return result;
        }

        ///<summary>
        /// 장착 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetEquippedItemDefinition( eEquipmentType _equipmentType )
        {
            string equippedItemId = GetEquippedItemId( _equipmentType );

            if ( string.IsNullOrWhiteSpace( equippedItemId ) )
            {
                return null;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( equippedItemId, out CItemDefinition itemDefinition );

            if ( hasDefinition == false )
            {
                return null;
            }

            return itemDefinition;
        }

        ///<summary>
        /// 장비 아이템 장착 여부 반환
        ///</summary>
        public bool HasEquippedItem( eEquipmentType _equipmentType )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );
            bool result = slotEntryData != null && slotEntryData.HasItem();
            return result;
        }

        ///<summary>
        /// 아이템 장착 가능 여부 반환
        ///</summary>
        public bool CanEquipItemDefinition( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null )
            {
                return false;
            }

            if ( _itemDefinition.IsEquipmentItem() == false )
            {
                return false;
            }

            eEquipmentType equipmentType = _itemDefinition.GetEquipmentType();

            if ( equipmentType == eEquipmentType.NONE )
            {
                return false;
            }

            bool result = GetEquipmentSlotEntryData( equipmentType ) != null;
            return result;
        }

        ///<summary>
        /// 인벤토리 슬롯 장비 착용 처리
        ///</summary>
        public bool TryEquipFromInventorySlot( CPlayerInventoryManager _inventoryManager, int _slotIndex )
        {
            if ( _inventoryManager == null )
            {
                return false;
            }

            CItemDefinition itemDefinition = _inventoryManager.GetItemDefinitionAtSlot( _slotIndex );

            if ( CanEquipItemDefinition( itemDefinition ) == false )
            {
                return false;
            }

            eEquipmentType equipmentType = itemDefinition.GetEquipmentType();
            bool result = TryEquipFromInventorySlot( _inventoryManager, _slotIndex, equipmentType );
            return result;
        }

        ///<summary>
        /// 인벤토리 슬롯 장비 타입 지정 착용 처리
        ///</summary>
        public bool TryEquipFromInventorySlot( CPlayerInventoryManager _inventoryManager, int _slotIndex, eEquipmentType _equipmentType )
        {
            if ( _inventoryManager == null )
            {
                return false;
            }

            CInventoryItemEntryData sourceEntryData = _inventoryManager.GetItemEntryData( _slotIndex );

            if ( sourceEntryData == null || sourceEntryData.IsEmpty() )
            {
                return false;
            }

            CInventoryItemEntryData copiedSourceEntryData = sourceEntryData.CreateCopy();
            CItemDefinition itemDefinition = _inventoryManager.GetItemDefinitionAtSlot( _slotIndex );

            if ( itemDefinition == null || itemDefinition.IsEquipmentTypeMatched( _equipmentType ) == false )
            {
                return false;
            }

            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null )
            {
                return false;
            }

            CInventoryItemEntryData replacementEntryData = new CInventoryItemEntryData();
            string previouslyEquippedItemId = slotEntryData.GetItemId();

            if ( string.IsNullOrWhiteSpace( previouslyEquippedItemId ) == false )
            {
                replacementEntryData.SetItemId( previouslyEquippedItemId );
                replacementEntryData.SetQuantity( 1 );
                replacementEntryData.SetEquipmentPotentialData( slotEntryData.GetEquipmentPotentialData() );
            }

            bool didReplaceInventorySlot = _inventoryManager.TryReplaceSlotItem( _slotIndex, replacementEntryData );

            if ( didReplaceInventorySlot == false )
            {
                return false;
            }

            slotEntryData.SetItemId( copiedSourceEntryData.GetItemId() );
            slotEntryData.SetEquipmentPotentialData( copiedSourceEntryData.GetEquipmentPotentialData() );
            RefreshEquipmentState();
            return true;
        }

        ///<summary>
        /// 장비 해제 처리
        ///</summary>
        public bool TryUnequipToInventory( CPlayerInventoryManager _inventoryManager, eEquipmentType _equipmentType )
        {
            if ( _inventoryManager == null )
            {
                return false;
            }

            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null || slotEntryData.HasItem() == false )
            {
                return false;
            }

            string equippedItemId = slotEntryData.GetItemId();
            CInventoryItemEntryData copiedEntryData = new CInventoryItemEntryData();
            copiedEntryData.SetItemId( equippedItemId );
            copiedEntryData.SetQuantity( 1 );
            copiedEntryData.SetEquipmentPotentialData( slotEntryData.GetEquipmentPotentialData() );
            bool didAddItem = _inventoryManager.TryAddItemEntry( copiedEntryData );

            if ( didAddItem == false )
            {
                return false;
            }

            slotEntryData.Clear();
            RefreshEquipmentState();
            return true;
        }

        ///<summary>
        /// 장착 장비 잠재 리롤 처리
        ///</summary>
        public bool TryRollEquippedPotential( eEquipmentType _equipmentType )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null || slotEntryData.HasItem() == false )
            {
                return false;
            }

            bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( slotEntryData.GetItemId(), out CItemDefinition itemDefinition );

            if ( hasDefinition == false || itemDefinition == null || itemDefinition.IsEquipmentItem() == false )
            {
                return false;
            }

            CEquipmentPotentialData equipmentPotentialData = slotEntryData.GetEquipmentPotentialData();
            bool didRoll = CEquipmentPotentialRollUtility.TryRollPotential( itemDefinition.GetEquipmentType(), equipmentPotentialData );

            if ( didRoll == false )
            {
                return false;
            }

            RefreshEquipmentState();
            return true;
        }

        ///<summary>
        /// 장착 장비 잠재 데이터 반영
        ///</summary>
        public bool TrySetEquippedPotentialData( eEquipmentType _equipmentType, CEquipmentPotentialData _equipmentPotentialData )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = GetEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData == null || slotEntryData.HasItem() == false )
            {
                return false;
            }

            slotEntryData.SetEquipmentPotentialData( _equipmentPotentialData );
            RefreshEquipmentState();
            return true;
        }

        ///<summary>
        /// 합산 장비 스탯 반환
        ///</summary>
        public CPlayerStatRuntimeData GetAggregatedEquipmentStatBonus()
        {
            CPlayerStatRuntimeData result = aggregatedEquipmentStatBonus;
            return result;
        }

        ///<summary>
        /// 합산 장비 특수 보너스 반환
        ///</summary>
        public CPlayerModifierRuntimeData GetAggregatedEquipmentModifierBonus()
        {
            CPlayerModifierRuntimeData result = aggregatedEquipmentModifierBonus;
            return result;
        }

        ///<summary>
        /// 플레이어 장비 저장 데이터 생성
        ///</summary>
        public CPlayerEquipmentSnapshotData CreateSnapshotData()
        {
            EnsureEquipmentSlots();
            CPlayerEquipmentSnapshotData snapshotData = new CPlayerEquipmentSnapshotData();
            List<CPlayerEquippedItemSnapshotData> equippedItemSnapshotList = new List<CPlayerEquippedItemSnapshotData>();
            int equipmentSlotCount = equipmentSlotEntryList.Count;

            for ( int index = 0; index < equipmentSlotCount; index++ )
            {
                CPlayerEquipmentSlotEntryData sourceSlotEntryData = equipmentSlotEntryList[ index ];

                if ( sourceSlotEntryData == null || sourceSlotEntryData.HasItem() == false )
                {
                    continue;
                }

                CPlayerEquippedItemSnapshotData equippedItemSnapshotData = CreateEquippedItemSnapshotData( sourceSlotEntryData );
                equippedItemSnapshotList.Add( equippedItemSnapshotData );
            }

            snapshotData.equippedItemSnapshotList = equippedItemSnapshotList;
            return snapshotData;
        }

        ///<summary>
        /// 플레이어 장비 저장 데이터 로드
        ///</summary>
        public void LoadSnapshotData( CPlayerEquipmentSnapshotData _snapshotData )
        {
            EnsureEquipmentSlots();
            int equipmentSlotCount = equipmentSlotEntryList.Count;

            for ( int index = 0; index < equipmentSlotCount; index++ )
            {
                CPlayerEquipmentSlotEntryData targetSlotEntryData = equipmentSlotEntryList[ index ];

                if ( targetSlotEntryData == null )
                {
                    continue;
                }

                targetSlotEntryData.Clear();
            }

            if ( _snapshotData == null )
            {
                RefreshEquipmentState();
                return;
            }

            if ( _snapshotData.equippedItemSnapshotList != null && _snapshotData.equippedItemSnapshotList.Count > 0 )
            {
                LoadEquippedItemSnapshotData( _snapshotData.equippedItemSnapshotList );
                RefreshEquipmentState();
                return;
            }

            if ( _snapshotData.equipmentSlotEntryList == null )
            {
                RefreshEquipmentState();
                return;
            }

            int snapshotSlotCount = _snapshotData.equipmentSlotEntryList.Count;

            for ( int index = 0; index < snapshotSlotCount; index++ )
            {
                CPlayerEquipmentSlotEntryData sourceSlotEntryData = _snapshotData.equipmentSlotEntryList[ index ];

                if ( sourceSlotEntryData == null )
                {
                    continue;
                }

                CPlayerEquipmentSlotEntryData targetSlotEntryData = GetEquipmentSlotEntryData( sourceSlotEntryData.GetEquipmentType() );

                if ( targetSlotEntryData == null )
                {
                    continue;
                }

                targetSlotEntryData.CopyFrom( sourceSlotEntryData );
            }

            RefreshEquipmentState();
        }

        ///<summary>
        /// 장착 장비 저장 데이터 생성
        ///</summary>
        private CPlayerEquippedItemSnapshotData CreateEquippedItemSnapshotData( CPlayerEquipmentSlotEntryData _sourceSlotEntryData )
        {
            CPlayerEquippedItemSnapshotData snapshotData = new CPlayerEquippedItemSnapshotData();
            snapshotData.equipmentType = _sourceSlotEntryData.GetEquipmentType();
            snapshotData.itemId = _sourceSlotEntryData.GetItemId();
            CEquipmentPotentialData potentialData = _sourceSlotEntryData.GetEquipmentPotentialData();

            if ( potentialData != null && potentialData.HasPotential() )
            {
                snapshotData.equipmentPotentialSnapshotData = potentialData.CreateSnapshotData();
            }

            return snapshotData;
        }

        ///<summary>
        /// 장착 장비 저장 데이터 로드
        ///</summary>
        private void LoadEquippedItemSnapshotData( List<CPlayerEquippedItemSnapshotData> _equippedItemSnapshotList )
        {
            for ( int index = 0; index < _equippedItemSnapshotList.Count; index++ )
            {
                CPlayerEquippedItemSnapshotData equippedItemSnapshotData = _equippedItemSnapshotList[ index ];

                if ( equippedItemSnapshotData == null || equippedItemSnapshotData.IsValid() == false )
                {
                    continue;
                }

                bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( equippedItemSnapshotData.itemId, out CItemDefinition itemDefinition );

                if ( hasDefinition == false || itemDefinition == null || itemDefinition.IsEquipmentTypeMatched( equippedItemSnapshotData.equipmentType ) == false )
                {
                    continue;
                }

                CPlayerEquipmentSlotEntryData targetSlotEntryData = GetEquipmentSlotEntryData( equippedItemSnapshotData.equipmentType );

                if ( targetSlotEntryData == null )
                {
                    continue;
                }

                targetSlotEntryData.SetItemId( equippedItemSnapshotData.itemId );
                targetSlotEntryData.GetEquipmentPotentialData().LoadSnapshotData( equippedItemSnapshotData.equipmentPotentialSnapshotData );
            }
        }

        ///<summary>
        /// 플레이어 스탯 매니저 결정
        ///</summary>
        private void ResolveStatManager()
        {
            if ( targetStatManager != null )
            {
                return;
            }

            CPlayerStatManager resolvedStatManager = GetComponent<CPlayerStatManager>();

            if ( resolvedStatManager == null )
            {
                resolvedStatManager = gameObject.AddComponent<CPlayerStatManager>();
            }

            targetStatManager = resolvedStatManager;
        }

        ///<summary>
        /// 장비 슬롯 기본 구조 보장
        ///</summary>
        private void EnsureEquipmentSlots()
        {
            if ( equipmentSlotEntryList == null )
            {
                equipmentSlotEntryList = new List<CPlayerEquipmentSlotEntryData>();
            }

            EnsureEquipmentSlot( eEquipmentType.WEAPON );
            EnsureEquipmentSlot( eEquipmentType.HELMET );
            EnsureEquipmentSlot( eEquipmentType.ARMOR );
            EnsureEquipmentSlot( eEquipmentType.SHIELD );
        }

        ///<summary>
        /// 개별 장비 슬롯 보장
        ///</summary>
        private void EnsureEquipmentSlot( eEquipmentType _equipmentType )
        {
            CPlayerEquipmentSlotEntryData slotEntryData = FindEquipmentSlotEntryData( _equipmentType );

            if ( slotEntryData != null )
            {
                return;
            }

            CPlayerEquipmentSlotEntryData createdSlotEntryData = new CPlayerEquipmentSlotEntryData();
            createdSlotEntryData.SetEquipmentType( _equipmentType );
            equipmentSlotEntryList.Add( createdSlotEntryData );
        }

        ///<summary>
        /// 장비 스탯 보너스 재계산
        ///</summary>
        private void RefreshEquipmentStatBonus()
        {
            aggregatedEquipmentStatBonus.Clear();
            aggregatedEquipmentPercentStatBonus.Clear();
            aggregatedEquipmentModifierBonus.Clear();

            for ( int index = 0; index < equipmentSlotEntryList.Count; index++ )
            {
                CPlayerEquipmentSlotEntryData slotEntryData = equipmentSlotEntryList[ index ];

                if ( slotEntryData == null || slotEntryData.HasItem() == false )
                {
                    continue;
                }

                bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( slotEntryData.GetItemId(), out CItemDefinition itemDefinition );

                if ( hasDefinition == false || itemDefinition == null )
                {
                    continue;
                }

                CPlayerStatRuntimeData equipmentStatBonus = itemDefinition.GetEquipmentStatBonus();
                aggregatedEquipmentStatBonus.AddFrom( equipmentStatBonus );
                CEquipmentPotentialData equipmentPotentialData = slotEntryData.GetEquipmentPotentialData();

                if ( equipmentPotentialData != null )
                {
                    equipmentPotentialData.AccumulateStatBonus( aggregatedEquipmentStatBonus, aggregatedEquipmentPercentStatBonus );
                    equipmentPotentialData.AccumulateModifierBonus( aggregatedEquipmentModifierBonus );
                }
            }

            if ( targetStatManager != null )
            {
                targetStatManager.ApplyEquipmentStatBonus( aggregatedEquipmentStatBonus );
                targetStatManager.ApplyEquipmentPercentStatBonus( aggregatedEquipmentPercentStatBonus );
                targetStatManager.ApplyEquipmentModifierBonus( aggregatedEquipmentModifierBonus );
            }
        }

        ///<summary>
        /// 장비 상태 갱신
        ///</summary>
        private void RefreshEquipmentState()
        {
            RefreshEquipmentStatBonus();
            RaiseEquipmentChanged();
        }

        ///<summary>
        /// 장비 변경 이벤트 전파
        ///</summary>
        private void RaiseEquipmentChanged()
        {
            if ( OnEquipmentChanged != null )
            {
                OnEquipmentChanged( this );
            }
        }
    }
}
