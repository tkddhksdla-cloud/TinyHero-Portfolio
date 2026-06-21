using System;
using System.Collections.Generic;
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
        /// 장착 아이템 ID 설정
        ///</summary>
        public void SetItemId( string _itemId )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
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
            }

            bool didReplaceInventorySlot = _inventoryManager.TryReplaceSlotItem( _slotIndex, replacementEntryData );

            if ( didReplaceInventorySlot == false )
            {
                return false;
            }

            slotEntryData.SetItemId( itemDefinition.GetItemId() );
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
            bool didAddItem = _inventoryManager.TryAddItemById( equippedItemId, 1 );

            if ( didAddItem == false )
            {
                return false;
            }

            slotEntryData.Clear();
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
            }

            if ( targetStatManager != null )
            {
                targetStatManager.ApplyEquipmentStatBonus( aggregatedEquipmentStatBonus );
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
