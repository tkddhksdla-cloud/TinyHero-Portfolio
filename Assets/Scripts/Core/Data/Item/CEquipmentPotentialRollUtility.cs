using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 장비 잠재 리롤 유틸리티
    ///</summary>
    public static class CEquipmentPotentialRollUtility
    {
        ///<summary>
        /// 장비 잠재 리롤 처리
        ///</summary>
        public static bool TryRollPotential( eEquipmentType _equipmentType, CEquipmentPotentialData _targetPotentialData )
        {
            if ( _targetPotentialData == null || _equipmentType == eEquipmentType.NONE )
            {
                return false;
            }

            CEquipmentPotentialTableData tableData = CEquipmentPotentialDatabase.GetTableData();

            if ( tableData == null )
            {
                return false;
            }

            eEquipmentPotentialRank currentRank = _targetPotentialData.GetRank();
            eEquipmentPotentialRank nextRank = ResolveNextRank( tableData, currentRank );
            _targetPotentialData.SetRank( nextRank );

            for ( int index = 0; index < CEquipmentPotentialData.FixedLineCount; index++ )
            {
                eEquipmentPotentialRank lineRank = ResolveLineRank( tableData, nextRank, index );
                bool isRolled = TryRollPotentialLine( tableData, _equipmentType, lineRank, out CEquipmentPotentialLineData rolledLineData );

                if ( isRolled == false )
                {
                    return false;
                }

                rolledLineData.SetLineRank( lineRank );
                _targetPotentialData.SetLineData( index, rolledLineData );
            }

            return true;
        }

        ///<summary>
        /// 다음 잠재 등급 결정
        ///</summary>
        private static eEquipmentPotentialRank ResolveNextRank( CEquipmentPotentialTableData _tableData, eEquipmentPotentialRank _currentRank )
        {
            if ( _tableData == null )
            {
                return _currentRank;
            }

            float chance = 0.0f;
            eEquipmentPotentialRank upgradedRank = _currentRank;

            switch ( _currentRank )
            {
                case eEquipmentPotentialRank.COMMON:
                    chance = _tableData.GetCommonToRareChance();
                    upgradedRank = eEquipmentPotentialRank.RARE;
                    break;

                case eEquipmentPotentialRank.RARE:
                    chance = _tableData.GetRareToUniqueChance();
                    upgradedRank = eEquipmentPotentialRank.UNIQUE;
                    break;

                case eEquipmentPotentialRank.UNIQUE:
                    chance = _tableData.GetUniqueToLegendaryChance();
                    upgradedRank = eEquipmentPotentialRank.LEGENDARY;
                    break;
            }

            bool isUpgraded = chance > 0.0f && Random.value <= chance;
            eEquipmentPotentialRank result = isUpgraded ? upgradedRank : _currentRank;
            return result;
        }

        ///<summary>
        /// 잠재 줄 등급 결정
        ///</summary>
        private static eEquipmentPotentialRank ResolveLineRank( CEquipmentPotentialTableData _tableData, eEquipmentPotentialRank _targetRank, int _lineIndex )
        {
            if ( _lineIndex <= 0 )
            {
                return _targetRank;
            }

            eEquipmentPotentialRank previousRank = CEquipmentPotentialUtility.GetPreviousRank( _targetRank );

            if ( previousRank == _targetRank )
            {
                return _targetRank;
            }

            if ( _tableData == null )
            {
                return previousRank;
            }

            float sameRankChance = _tableData.GetAdditionalCurrentRankChance( _targetRank );
            bool useCurrentRank = sameRankChance > 0.0f && Random.value <= sameRankChance;
            eEquipmentPotentialRank result = useCurrentRank ? _targetRank : previousRank;
            return result;
        }

        ///<summary>
        /// 잠재 한 줄 리롤 처리
        ///</summary>
        private static bool TryRollPotentialLine( CEquipmentPotentialTableData _tableData, eEquipmentType _equipmentType, eEquipmentPotentialRank _rank, out CEquipmentPotentialLineData _rolledLineData )
        {
            _rolledLineData = null;

            if ( _tableData == null )
            {
                return false;
            }

            IReadOnlyList<CEquipmentPotentialOptionEntry> optionEntryList = _tableData.GetOptionEntryList();
            List<CEquipmentPotentialOptionEntry> filteredEntryList = new List<CEquipmentPotentialOptionEntry>();
            int totalWeight = 0;

            for ( int index = 0; index < optionEntryList.Count; index++ )
            {
                CEquipmentPotentialOptionEntry optionEntry = optionEntryList[ index ];

                if ( optionEntry == null )
                {
                    continue;
                }

                if ( optionEntry.GetEquipmentType() != _equipmentType || optionEntry.GetRank() != _rank )
                {
                    continue;
                }

                int weight = optionEntry.GetWeight();

                if ( weight <= 0 )
                {
                    continue;
                }

                filteredEntryList.Add( optionEntry );
                totalWeight += weight;
            }

            if ( filteredEntryList.Count == 0 || totalWeight <= 0 )
            {
                return false;
            }

            bool wasSelected = CWeightedRandomSelector.TrySelect( filteredEntryList, GetOptionEntryWeight, out CEquipmentPotentialOptionEntry selectedOptionEntry );

            if ( wasSelected == false )
            {
                return false;
            }

            CEquipmentPotentialLineData rolledLineData = new CEquipmentPotentialLineData();
            rolledLineData.CopyFromOptionEntry( selectedOptionEntry, _rank );
            _rolledLineData = rolledLineData;
            return true;
        }

        private static float GetOptionEntryWeight( CEquipmentPotentialOptionEntry _optionEntry )
        {
            if ( _optionEntry == null )
            {
                return 0.0f;
            }

            float result = _optionEntry.GetWeight();
            return result;
        }
    }
}
