using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 장비 잠재 옵션 엔트리
    ///</summary>
    [Serializable]
    public sealed class CEquipmentPotentialOptionEntry
    {
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private eEquipmentPotentialRank rank = eEquipmentPotentialRank.COMMON;
        [SerializeField] private eEquipmentPotentialOptionType optionType = eEquipmentPotentialOptionType.NONE;
        [SerializeField] private eEquipmentPotentialValueType valueType = eEquipmentPotentialValueType.VALUE;
        [SerializeField] private float value = 1.0f;
        [SerializeField] private int weight = 1;

        ///<summary>
        /// 장비 타입 반환
        ///</summary>
        public eEquipmentType GetEquipmentType()
        {
            eEquipmentType result = equipmentType;
            return result;
        }

        ///<summary>
        /// 잠재 등급 반환
        ///</summary>
        public eEquipmentPotentialRank GetRank()
        {
            eEquipmentPotentialRank result = rank;
            return result;
        }

        ///<summary>
        /// 잠재 옵션 종류 반환
        ///</summary>
        public eEquipmentPotentialOptionType GetOptionType()
        {
            eEquipmentPotentialOptionType result = optionType;
            return result;
        }

        ///<summary>
        /// 잠재 수치 타입 반환
        ///</summary>
        public eEquipmentPotentialValueType GetValueType()
        {
            eEquipmentPotentialValueType result = CEquipmentPotentialUtility.ShouldForcePercentValueType( optionType ) ? eEquipmentPotentialValueType.PERCENT : valueType;
            return result;
        }

        ///<summary>
        /// 잠재 수치 반환
        ///</summary>
        public float GetValue()
        {
            float result = value;
            return result;
        }

        ///<summary>
        /// 등장 가중치 반환
        ///</summary>
        public int GetWeight()
        {
            int result = Mathf.Max( 0, weight );
            return result;
        }
    }

    ///<summary>
    /// 장비 잠재 테이블 데이터
    ///</summary>
    [CreateAssetMenu( fileName = "EquipmentPotentialTableData", menuName = "TinyHero/Data/Equipment Potential Table Data" )]
    public sealed class CEquipmentPotentialTableData : ScriptableObject
    {
        [SerializeField] [Range( 0.0f, 1.0f )] private float commonToRareChance = 0.12f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float rareToUniqueChance = 0.04f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float uniqueToLegendaryChance = 0.01f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float rareAdditionalCurrentRankChance = 0.10f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float uniqueAdditionalCurrentRankChance = 0.08f;
        [SerializeField] [Range( 0.0f, 1.0f )] private float legendaryAdditionalCurrentRankChance = 0.05f;
        [SerializeField] private List<CEquipmentPotentialOptionEntry> optionEntryList = new List<CEquipmentPotentialOptionEntry>();

        ///<summary>
        /// 커먼 등급업 확률 반환
        ///</summary>
        public float GetCommonToRareChance()
        {
            float result = Mathf.Clamp01( commonToRareChance );
            return result;
        }

        ///<summary>
        /// 레어 등급업 확률 반환
        ///</summary>
        public float GetRareToUniqueChance()
        {
            float result = Mathf.Clamp01( rareToUniqueChance );
            return result;
        }

        ///<summary>
        /// 유니크 등급업 확률 반환
        ///</summary>
        public float GetUniqueToLegendaryChance()
        {
            float result = Mathf.Clamp01( uniqueToLegendaryChance );
            return result;
        }

        ///<summary>
        /// 추가 줄 현재 등급 유지 확률 반환
        ///</summary>
        public float GetAdditionalCurrentRankChance( eEquipmentPotentialRank _rank )
        {
            switch ( _rank )
            {
                case eEquipmentPotentialRank.RARE:
                    return Mathf.Clamp01( rareAdditionalCurrentRankChance );

                case eEquipmentPotentialRank.UNIQUE:
                    return Mathf.Clamp01( uniqueAdditionalCurrentRankChance );

                case eEquipmentPotentialRank.LEGENDARY:
                    return Mathf.Clamp01( legendaryAdditionalCurrentRankChance );
            }

            return 1.0f;
        }

        ///<summary>
        /// 잠재 옵션 엔트리 목록 반환
        ///</summary>
        public IReadOnlyList<CEquipmentPotentialOptionEntry> GetOptionEntryList()
        {
            IReadOnlyList<CEquipmentPotentialOptionEntry> result = optionEntryList;
            return result;
        }
    }
}
