using System;
using System.Collections.Generic;
using LayerLab.ArtMakerUnity;
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
        SKILL_BOOK
    }

    ///<summary>
    /// 인벤토리 보관 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CInventoryItemEntryData
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private int quantity;

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
        public int GetQuantity()
        {
            int result = quantity;
            return result;
        }

        ///<summary>
        /// 수량 설정
        ///</summary>
        public void SetQuantity( int _quantity )
        {
            quantity = Mathf.Max( 0, _quantity );
        }

        ///<summary>
        /// 빈 슬롯 여부 반환
        ///</summary>
        public bool IsEmpty()
        {
            bool hasItemId = string.IsNullOrWhiteSpace( itemId ) == false;
            bool result = hasItemId == false || quantity <= 0;
            return result;
        }

        ///<summary>
        /// 슬롯 데이터 초기화
        ///</summary>
        public void Clear()
        {
            itemId = string.Empty;
            quantity = 0;
        }

        ///<summary>
        /// 슬롯 데이터 복사본 생성
        ///</summary>
        public CInventoryItemEntryData CreateCopy()
        {
            CInventoryItemEntryData copiedEntryData = new CInventoryItemEntryData();
            copiedEntryData.SetItemId( itemId );
            copiedEntryData.SetQuantity( quantity );
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
            quantity = _sourceEntryData.GetQuantity();
        }
    }

    ///<summary>
    /// 플레이어 인벤토리 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerInventorySnapshotData
    {
        [SerializeField] private List<CInventoryItemEntryData> itemEntryList = new List<CInventoryItemEntryData>();

        ///<summary>
        /// 저장 항목 목록 반환
        ///</summary>
        public List<CInventoryItemEntryData> GetItemEntryList()
        {
            List<CInventoryItemEntryData> result = itemEntryList;
            return result;
        }

        ///<summary>
        /// 저장 항목 목록 설정
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
        [SerializeField] private int minDropCount = 1;
        [SerializeField] private int maxDropCount = 1;

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
        public int GetMinDropCount()
        {
            int result = Mathf.Max( 0, minDropCount );
            return result;
        }

        ///<summary>
        /// 최소 드랍 수량 설정
        ///</summary>
        public void SetMinDropCount( int _minDropCount )
        {
            minDropCount = Mathf.Max( 0, _minDropCount );
        }

        ///<summary>
        /// 최대 드랍 수량 반환
        ///</summary>
        public int GetMaxDropCount()
        {
            int normalizedMinDropCount = Mathf.Max( 0, minDropCount );
            int result = Mathf.Max( normalizedMinDropCount, maxDropCount );
            return result;
        }

        ///<summary>
        /// 최대 드랍 수량 설정
        ///</summary>
        public void SetMaxDropCount( int _maxDropCount )
        {
            int normalizedMinDropCount = Mathf.Max( 0, minDropCount );
            maxDropCount = Mathf.Max( normalizedMinDropCount, _maxDropCount );
        }
    }

    ///<summary>
    /// 아이템 정의 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "ItemDefinition", menuName = "TinyHero/Data/Item Definition" )]
    public sealed class CItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private string itemName = string.Empty;
        [SerializeField] private eItemType itemType = eItemType.CONSUMABLE;
        [SerializeField] [TextArea] private string description = string.Empty;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private GameObject worldDropPrefab;
        [SerializeField] private bool isStackable = true;
        [SerializeField] private int maxStackCount = 99;
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private eConsumableType consumableType = eConsumableType.NONE;
        [SerializeField] private string linkedSkillId = string.Empty;
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
        public int GetMaxStackCount()
        {
            int result = isStackable ? Mathf.Max( 1, maxStackCount ) : 1;
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
        /// 연결 스킬 ID 반환
        ///</summary>
        public string GetLinkedSkillId()
        {
            string result = IsSkillBook() ? linkedSkillId : string.Empty;
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
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount )
        {
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, eEquipmentType.NONE, null, PartsType.Chest, -1 );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount, eEquipmentType _equipmentType, CPlayerStatRuntimeData _equipmentStatBonus )
        {
            PartsType defaultEquipmentPartsType = ResolveDefaultEquipmentPartsType( _equipmentType );
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, _equipmentType, eConsumableType.NONE, string.Empty, _equipmentStatBonus, defaultEquipmentPartsType, -1 );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount, eEquipmentType _equipmentType, CPlayerStatRuntimeData _equipmentStatBonus, PartsType _equipmentPartsType, int _equipmentPartsIndex )
        {
            Configure( _itemId, _itemName, _itemType, _description, _iconSprite, _isStackable, _maxStackCount, _equipmentType, eConsumableType.NONE, string.Empty, _equipmentStatBonus, _equipmentPartsType, _equipmentPartsIndex );
        }

        ///<summary>
        /// 아이템 정의 구성
        ///</summary>
        public void Configure( string _itemId, string _itemName, eItemType _itemType, string _description, Sprite _iconSprite, bool _isStackable, int _maxStackCount, eEquipmentType _equipmentType, eConsumableType _consumableType, string _linkedSkillId, CPlayerStatRuntimeData _equipmentStatBonus, PartsType _equipmentPartsType, int _equipmentPartsIndex )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
            itemName = string.IsNullOrWhiteSpace( _itemName ) ? itemId : _itemName.Trim();
            itemType = _itemType;
            description = string.IsNullOrWhiteSpace( _description ) ? string.Empty : _description.Trim();
            iconSprite = _iconSprite;
            bool isEquipmentItem = _itemType == eItemType.EQUIPMENT;
            bool isConsumableItem = _itemType == eItemType.CONSUMABLE;
            equipmentType = isEquipmentItem ? _equipmentType : eEquipmentType.NONE;
            consumableType = isConsumableItem ? _consumableType : eConsumableType.NONE;
            linkedSkillId = isConsumableItem ? ( string.IsNullOrWhiteSpace( _linkedSkillId ) ? string.Empty : _linkedSkillId.Trim() ) : string.Empty;
            equipmentPartsType = isEquipmentItem ? _equipmentPartsType : PartsType.Chest;
            equipmentPartsIndex = isEquipmentItem ? Mathf.Max( -1, _equipmentPartsIndex ) : -1;
            isStackable = isEquipmentItem == false && _isStackable;
            maxStackCount = isStackable ? Mathf.Max( 1, _maxStackCount ) : 1;

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
