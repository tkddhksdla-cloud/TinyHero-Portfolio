using System;
using System.Collections.Generic;
using LayerLab.ArtMakerUnity;
using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 아이템 분류
    ///</summary>
    public enum eItemType
    {
        EQUIPMENT,
        CONSUMABLE,
        CURRENCY,
        MATERIAL,
        QUEST_ITEM
    }

    ///<summary>
    /// 장비 슬롯 타입
    ///</summary>
    public enum eEquipmentType
    {
        NONE,
        WEAPON,
        HELMET,
        ARMOR,
        SHIELD
    }

    ///<summary>
    /// 소모품 상세 타입
    ///</summary>
    public enum eConsumableType
    {
        NONE,
        GENERAL,
        SKILL_BOOK,
        CUBE,
        RANDOM_BOX
    }

    ///<summary>
    /// 인벤토리 보관 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CInventoryItemEntryData
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private long quantityValue;
        [SerializeField] private CEquipmentPotentialData equipmentPotentialData = new CEquipmentPotentialData();

        [NonSerialized] private CSecureLong secureQuantityValue;
        [NonSerialized] private bool hasSecureQuantityValue;
        [NonSerialized] private bool didReportQuantityTamper;

        ///<summary>
        /// 아이템 ID 반환
        ///</summary>
        public string GetItemId()
        {
            string result = itemId;
            return result;
        }

        ///<summary>
        /// 아이템 ID 설정
        ///</summary>
        public void SetItemId( string _itemId )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
        }

        ///<summary>
        /// 수량 반환
        ///</summary>
        public long GetQuantity()
        {
            EnsureSecureQuantityValue();

            if ( secureQuantityValue.TryGetValue( out long resolvedQuantity ) == false )
            {
                ReportQuantityTamper();
                return 0L;
            }

            long result = Math.Max( 0L, resolvedQuantity );
            quantityValue = result;
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
        /// 수량 설정
        ///</summary>
        public void SetQuantity( long _quantity )
        {
            long resolvedQuantity = Math.Max( 0L, _quantity );
            quantityValue = resolvedQuantity;
            secureQuantityValue = new CSecureLong( resolvedQuantity );
            hasSecureQuantityValue = true;
            didReportQuantityTamper = false;
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
        /// 빈 슬롯 여부 반환
        ///</summary>
        public bool IsEmpty()
        {
            bool hasItemId = string.IsNullOrWhiteSpace( itemId ) == false;
            bool result = hasItemId == false || GetQuantity() <= 0L;
            return result;
        }

        ///<summary>
        /// 슬롯 데이터 초기화
        ///</summary>
        public void Clear()
        {
            itemId = string.Empty;
            SetQuantity( 0L );
            EnsurePotentialData();
            equipmentPotentialData.Clear();
        }

        ///<summary>
        /// 보안 수량 초기화 보장
        ///</summary>
        private void EnsureSecureQuantityValue()
        {
            if ( hasSecureQuantityValue )
            {
                return;
            }

            long resolvedQuantity = Math.Max( 0L, quantityValue );
            secureQuantityValue = new CSecureLong( resolvedQuantity );
            hasSecureQuantityValue = true;
            didReportQuantityTamper = false;
        }

        ///<summary>
        /// 수량 메모리 변조 경고 출력
        ///</summary>
        private void ReportQuantityTamper()
        {
            if ( didReportQuantityTamper )
            {
                return;
            }

            didReportQuantityTamper = true;
            string displayItemId = string.IsNullOrWhiteSpace( itemId ) ? "EMPTY" : itemId;
            Debug.LogWarning( $"[ Security ] Inventory quantity tamper detected. ItemId: {displayItemId}" );
        }

        ///<summary>
        /// 슬롯 데이터 복사본 생성
        ///</summary>
        public CInventoryItemEntryData CreateCopy()
        {
            CInventoryItemEntryData copiedEntryData = new CInventoryItemEntryData();
            copiedEntryData.SetItemId( itemId );
            copiedEntryData.SetQuantity( GetQuantity() );
            copiedEntryData.SetEquipmentPotentialData( equipmentPotentialData );
            return copiedEntryData;
        }

        ///<summary>
        /// 슬롯 데이터 복사 반영
        ///</summary>
        public void CopyFrom( CInventoryItemEntryData _sourceEntryData )
        {
            if ( _sourceEntryData == null )
            {
                Clear();
                return;
            }

            itemId = _sourceEntryData.GetItemId();
            SetQuantity( _sourceEntryData.GetQuantity() );
            SetEquipmentPotentialData( _sourceEntryData.GetEquipmentPotentialData() );
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
    /// 인벤토리 점유 슬롯 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CInventoryOccupiedSlotSnapshotData
    {
        public eItemType itemType = eItemType.EQUIPMENT;
        public int localSlotIndex = -1;
        public string itemId = string.Empty;
        public long quantityValue;
        public CEquipmentPotentialSnapshotData equipmentPotentialSnapshotData = new CEquipmentPotentialSnapshotData();

        ///<summary>
        /// 저장 슬롯 데이터 유효 여부 반환
        ///</summary>
        public bool IsValid()
        {
            bool result = localSlotIndex >= 0 && string.IsNullOrWhiteSpace( itemId ) == false && quantityValue > 0L;
            return result;
        }
    }

    ///<summary>
    /// 플레이어 인벤토리 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerInventorySnapshotData
    {
        [SerializeField] private List<CInventoryOccupiedSlotSnapshotData> occupiedSlotSnapshotList = new List<CInventoryOccupiedSlotSnapshotData>();
        [SerializeField] private List<CInventoryCategoryEntryData> inventoryCategoryEntryList = new List<CInventoryCategoryEntryData>();
        [SerializeField] private List<CInventoryItemEntryData> itemEntryList = new List<CInventoryItemEntryData>();

        ///<summary>
        /// 점유 슬롯 저장 목록 반환
        ///</summary>
        public List<CInventoryOccupiedSlotSnapshotData> GetOccupiedSlotSnapshotList()
        {
            List<CInventoryOccupiedSlotSnapshotData> result = occupiedSlotSnapshotList;
            return result;
        }

        ///<summary>
        /// 점유 슬롯 저장 목록 설정
        ///</summary>
        public void SetOccupiedSlotSnapshotList( List<CInventoryOccupiedSlotSnapshotData> _occupiedSlotSnapshotList )
        {
            occupiedSlotSnapshotList = _occupiedSlotSnapshotList ?? new List<CInventoryOccupiedSlotSnapshotData>();
        }

        ///<summary>
        /// 저장 카테고리 목록 반환
        ///</summary>
        public List<CInventoryCategoryEntryData> GetInventoryCategoryEntryList()
        {
            List<CInventoryCategoryEntryData> result = inventoryCategoryEntryList;
            return result;
        }

        ///<summary>
        /// 저장 카테고리 목록 설정
        ///</summary>
        public void SetInventoryCategoryEntryList( List<CInventoryCategoryEntryData> _inventoryCategoryEntryList )
        {
            inventoryCategoryEntryList = _inventoryCategoryEntryList ?? new List<CInventoryCategoryEntryData>();
        }

        ///<summary>
        /// 구버전 저장 항목 목록 반환
        ///</summary>
        public List<CInventoryItemEntryData> GetLegacyItemEntryList()
        {
            List<CInventoryItemEntryData> result = itemEntryList;
            return result;
        }
    }

    ///<summary>
    /// 아이템 타입별 인벤토리 카테고리 데이터
    ///</summary>
    [Serializable]
    public sealed class CInventoryCategoryEntryData
    {
        [SerializeField] private eItemType itemType = eItemType.EQUIPMENT;
        [SerializeField] private List<CInventoryItemEntryData> itemEntryList = new List<CInventoryItemEntryData>();

        ///<summary>
        /// 카테고리 아이템 타입 반환
        ///</summary>
        public eItemType GetItemType()
        {
            eItemType result = itemType;
            return result;
        }

        ///<summary>
        /// 카테고리 아이템 타입 설정
        ///</summary>
        public void SetItemType( eItemType _itemType )
        {
            itemType = _itemType;
        }

        ///<summary>
        /// 카테고리 슬롯 목록 반환
        ///</summary>
        public List<CInventoryItemEntryData> GetItemEntryList()
        {
            List<CInventoryItemEntryData> result = itemEntryList;
            return result;
        }

        ///<summary>
        /// 카테고리 슬롯 목록 설정
        ///</summary>
        public void SetItemEntryList( List<CInventoryItemEntryData> _itemEntryList )
        {
            itemEntryList = _itemEntryList ?? new List<CInventoryItemEntryData>();
        }
    }

    ///<summary>
    /// 몬스터 아이템 드랍 엔트리
    ///</summary>
    [Serializable]
    public sealed class CMonsterItemDropEntry
    {
        [SerializeField] private CItemDefinition itemDefinition;
        [SerializeField] [Range( 0.0f, 1.0f )] private float dropChance = 1.0f;
        [SerializeField] private long minDropCountValue = 1L;
        [SerializeField] private long maxDropCountValue = 1L;

        ///<summary>
        /// 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinition()
        {
            CItemDefinition result = itemDefinition;
            return result;
        }

        ///<summary>
        /// 아이템 정의 설정
        ///</summary>
        public void SetItemDefinition( CItemDefinition _itemDefinition )
        {
            itemDefinition = _itemDefinition;
        }

        ///<summary>
        /// 드랍 확률 반환
        ///</summary>
        public float GetDropChance()
        {
            float result = Mathf.Clamp01( dropChance );
            return result;
        }

        ///<summary>
        /// 드랍 확률 설정
        ///</summary>
        public void SetDropChance( float _dropChance )
        {
            dropChance = Mathf.Clamp01( _dropChance );
        }

        ///<summary>
        /// 최소 드랍 수량 반환
        ///</summary>
        public long GetMinDropCount()
        {
            long result = Math.Max( 0L, minDropCountValue );
            return result;
        }

        ///<summary>
        /// 최소 드랍 수량 설정
        ///</summary>
        public void SetMinDropCount( long _minDropCount )
        {
            long resolvedMinDropCount = Math.Max( 0L, _minDropCount );
            minDropCountValue = resolvedMinDropCount;
        }

        ///<summary>
        /// 최대 드랍 수량 반환
        ///</summary>
        public long GetMaxDropCount()
        {
            long normalizedMinDropCount = GetMinDropCount();
            long result = Math.Max( normalizedMinDropCount, maxDropCountValue );
            return result;
        }

        ///<summary>
        /// 최대 드랍 수량 설정
        ///</summary>
        public void SetMaxDropCount( long _maxDropCount )
        {
            long normalizedMinDropCount = GetMinDropCount();
            long resolvedMaxDropCount = Math.Max( normalizedMinDropCount, _maxDropCount );
            maxDropCountValue = resolvedMaxDropCount;
        }
    }

    ///<summary>
    /// 아이템 정의 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "ItemDefinition", menuName = "TinyHero/Data/Item Definition" )]
    public sealed class CItemDefinition : ScriptableObject
    {
        private const string DefaultSellPriceItemId = "GOLD";

        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private string itemName = string.Empty;
        [SerializeField] private eItemType itemType = eItemType.CONSUMABLE;
        [SerializeField] [TextArea] private string description = string.Empty;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private GameObject worldDropPrefab;
        [SerializeField] private bool isStackable = true;
        [SerializeField] private long maxStackCountValue = 99L;
        [SerializeField] private string sellPriceItemId = DefaultSellPriceItemId;
        [SerializeField] private long sellPriceValue;
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private eConsumableType consumableType = eConsumableType.NONE;
        [SerializeField] private string linkedSkillId = string.Empty;
        [SerializeField] private CRandomBoxRewardTable randomBoxRewardTable;
        [SerializeField] private CPlayerStatRuntimeData equipmentStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private PartsType equipmentPartsType = PartsType.Chest;
        [SerializeField] private int equipmentPartsIndex = -1;

        ///<summary>
        /// 아이템 ID 반환
        ///</summary>
        public string GetItemId()
        {
            string result = itemId;
            return result;
        }

        ///<summary>
        /// 아이템 이름 반환
        ///</summary>
        public string GetItemName()
        {
            string result = itemName;
            return result;
        }

        ///<summary>
        /// 아이템 타입 반환
        ///</summary>
        public eItemType GetItemType()
        {
            eItemType result = itemType;
            return result;
        }

        ///<summary>
        /// 아이템 설명 반환
        ///</summary>
        public string GetDescription()
        {
            string result = description;
            return result;
        }

        ///<summary>
        /// 아이콘 스프라이트 반환
        ///</summary>
        public Sprite GetIconSprite()
        {
            Sprite result = iconSprite;
            return result;
        }

        ///<summary>
        /// 아이콘 스프라이트 설정
        ///</summary>
        public void SetIconSprite( Sprite _iconSprite )
        {
            iconSprite = _iconSprite;
        }

        ///<summary>
        /// 월드 드랍 프리팹 반환
        ///</summary>
        public GameObject GetWorldDropPrefab()
        {
            GameObject result = worldDropPrefab;
            return result;
        }

        ///<summary>
        /// 중첩 가능 여부 반환
        ///</summary>
        public bool IsStackable()
        {
            bool result = isStackable;
            return result;
        }

        ///<summary>
        /// 최대 중첩 수량 반환
        ///</summary>
        public long GetMaxStackCount()
        {
            long result = isStackable ? Math.Max( 1L, maxStackCountValue ) : 1L;
            return result;
        }

        ///<summary>
        /// 판매 가격 아이템 ID 반환
        ///</summary>
        public string GetSellPriceItemId()
        {
            string result = string.IsNullOrWhiteSpace( sellPriceItemId ) ? DefaultSellPriceItemId : sellPriceItemId.Trim();
            return result;
        }

        ///<summary>
        /// 판매 가격 수량 반환
        ///</summary>
        public long GetSellPrice()
        {
            long result = Math.Max( 0L, sellPriceValue );
            return result;
        }

        ///<summary>
        /// 판매 가격 보유 여부 반환
        ///</summary>
        public bool HasSellPrice()
        {
            bool result = GetSellPrice() > 0;
            return result;
        }

        ///<summary>
        /// 장비 아이템 여부 반환
        ///</summary>
        public bool IsEquipmentItem()
        {
            bool result = itemType == eItemType.EQUIPMENT;
            return result;
        }

        ///<summary>
        /// 장비 슬롯 타입 반환
        ///</summary>
        public eEquipmentType GetEquipmentType()
        {
            eEquipmentType result = IsEquipmentItem() ? equipmentType : eEquipmentType.NONE;
            return result;
        }

        ///<summary>
        /// 소모품 상세 타입 반환
        ///</summary>
        public eConsumableType GetConsumableType()
        {
            eConsumableType result = itemType == eItemType.CONSUMABLE ? consumableType : eConsumableType.NONE;
            return result;
        }

        ///<summary>
        /// 스킬 북 여부 반환
        ///</summary>
        public bool IsSkillBook()
        {
            bool result = itemType == eItemType.CONSUMABLE && consumableType == eConsumableType.SKILL_BOOK;
            return result;
        }

        ///<summary>
        /// 큐브 소모품 여부 반환
        ///</summary>
        public bool IsCube()
        {
            bool result = itemType == eItemType.CONSUMABLE && consumableType == eConsumableType.CUBE;
            return result;
        }

        ///<summary>
        /// 랜덤상자 여부 반환
        ///</summary>
        public bool IsRandomBox()
        {
            bool result = itemType == eItemType.CONSUMABLE && consumableType == eConsumableType.RANDOM_BOX;
            return result;
        }

        ///<summary>
        /// 연결 스킬 ID 반환
        ///</summary>
        public string GetLinkedSkillId()
        {
            string result = IsSkillBook() ? linkedSkillId : string.Empty;
            return result;
        }

        ///<summary>
        /// 랜덤상자 보상 테이블 반환
        ///</summary>
        public CRandomBoxRewardTable GetRandomBoxRewardTable()
        {
            CRandomBoxRewardTable result = IsRandomBox() ? randomBoxRewardTable : null;
            return result;
        }

        ///<summary>
        /// 장비 슬롯 타입 일치 여부 반환
        ///</summary>
        public bool IsEquipmentTypeMatched( eEquipmentType _equipmentType )
        {
            if ( IsEquipmentItem() == false )
            {
                return false;
            }

            bool result = equipmentType == _equipmentType;
            return result;
        }

        ///<summary>
        /// 장비 스탯 보너스 반환
        ///</summary>
        public CPlayerStatRuntimeData GetEquipmentStatBonus()
        {
            CPlayerStatRuntimeData result = equipmentStatBonus;
            return result;
        }

        ///<summary>
        /// 장비 외형 파츠 타입 반환
        ///</summary>
        public PartsType GetEquipmentPartsType()
        {
            PartsType result = equipmentPartsType;
            return result;
        }

        ///<summary>
        /// 장비 외형 파츠 인덱스 반환
        ///</summary>
        public int GetEquipmentPartsIndex()
        {
            int result = IsEquipmentItem() ? equipmentPartsIndex : -1;
            return result;
        }

        ///<summary>
        /// 장비 외형 데이터 보유 여부 반환
        ///</summary>
        public bool HasEquipmentPartsVisual()
        {
            bool result = IsEquipmentItem() && equipmentPartsIndex >= 0;
            return result;
        }

        ///<summary>
        /// 월드 드랍 프리팹 설정
        ///</summary>
        public void SetWorldDropPrefab( GameObject _worldDropPrefab )
        {
            worldDropPrefab = _worldDropPrefab;
        }

        ///<summary>
        /// 판매 가격 아이템 ID 설정
        ///</summary>
        public void SetSellPriceItemId( string _sellPriceItemId )
        {
            sellPriceItemId = string.IsNullOrWhiteSpace( _sellPriceItemId ) ? DefaultSellPriceItemId : _sellPriceItemId.Trim();
        }

        ///<summary>
        /// 판매 가격 수량 설정
        ///</summary>
        public void SetSellPrice( long _sellPrice )
        {
            long resolvedSellPrice = Math.Max( 0L, _sellPrice );
            sellPriceValue = resolvedSellPrice;
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, long _maxStackCount )
        {
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, eEquipmentType.NONE, null, PartsType.Chest, -1 );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, long _maxStackCount, eEquipmentType _equipmentType, CPlayerStatRuntimeData _equipmentStatBonus )
        {
            PartsType defaultEquipmentPartsType = ResolveDefaultEquipmentPartsType( _equipmentType );
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, _equipmentType, eConsumableType.NONE, string.Empty, _equipmentStatBonus, defaultEquipmentPartsType, -1 );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, long _maxStackCount, eEquipmentType _equipmentType, CPlayerStatRuntimeData _equipmentStatBonus, PartsType _equipmentPartsType, int _equipmentPartsIndex )
        {
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, _equipmentType, eConsumableType.NONE, string.Empty, _equipmentStatBonus, _equipmentPartsType, _equipmentPartsIndex );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, long _maxStackCount, eEquipmentType _equipmentType, eConsumableType _consumableType, string _linkedSkillId, CPlayerStatRuntimeData _equipmentStatBonus, PartsType _equipmentPartsType, int _equipmentPartsIndex )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
            itemName = string.IsNullOrWhiteSpace( _itemName ) ? itemId : _itemName.Trim();
            itemType = _itemType;
            description = string.IsNullOrWhiteSpace( _description ) ? string.Empty : _description.Trim();
            iconSprite = _iconSprite;
            sellPriceItemId = DefaultSellPriceItemId;
            sellPriceValue = 0L;
            bool isEquipmentItem = _itemType == eItemType.EQUIPMENT;
            bool isConsumableItem = _itemType == eItemType.CONSUMABLE;
            equipmentType = isEquipmentItem ? _equipmentType : eEquipmentType.NONE;
            consumableType = isConsumableItem ? _consumableType : eConsumableType.NONE;
            linkedSkillId = isConsumableItem ? ( string.IsNullOrWhiteSpace( _linkedSkillId ) ? string.Empty : _linkedSkillId.Trim() ) : string.Empty;
            randomBoxRewardTable = isConsumableItem && _consumableType == eConsumableType.RANDOM_BOX ? randomBoxRewardTable : null;
            equipmentPartsType = isEquipmentItem ? _equipmentPartsType : PartsType.Chest;
            equipmentPartsIndex = isEquipmentItem ? Mathf.Max( -1, _equipmentPartsIndex ) : -1;
            isStackable = isEquipmentItem == false && _isStackable;
            long resolvedMaxStackCount = isStackable ? Math.Max( 1L, _maxStackCount ) : 1L;
            maxStackCountValue = resolvedMaxStackCount;

            if ( equipmentStatBonus == null )
            {
                equipmentStatBonus = new CPlayerStatRuntimeData();
            }

            if ( isEquipmentItem && _equipmentStatBonus != null )
            {
                equipmentStatBonus.CopyFrom( _equipmentStatBonus );
                return;
            }

            equipmentStatBonus.Clear();
        }

        ///<summary>
        /// 장비 기본 파츠 타입 결정
        ///</summary>
        private static PartsType ResolveDefaultEquipmentPartsType( eEquipmentType _equipmentType )
        {
            switch ( _equipmentType )
            {
                case eEquipmentType.WEAPON:
                    return PartsType.Sword;

                case eEquipmentType.HELMET:
                    return PartsType.Helmet;

                case eEquipmentType.ARMOR:
                    return PartsType.Chest;

                case eEquipmentType.SHIELD:
                    return PartsType.Shield;
            }

            PartsType result = PartsType.Chest;
            return result;
        }
    }
}
