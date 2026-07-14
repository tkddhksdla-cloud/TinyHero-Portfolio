using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 장비 잠재 테이블 데이터베이스
    ///</summary>
    public static class CEquipmentPotentialDatabase
    {
        private const string ResourcePath = "Data/Item/EquipmentPotentialTableData";

        private static CEquipmentPotentialTableData cachedTableData;

        ///<summary>
        /// 잠재 테이블 데이터 반환
        ///</summary>
        public static CEquipmentPotentialTableData GetTableData()
        {
            if ( cachedTableData != null )
            {
                return cachedTableData;
            }

            CResourceManager resourceManager = CResourceManager.Instance;
            CEquipmentPotentialTableData loadedTableData = resourceManager != null ? resourceManager.GetEquipmentPotentialTableData() : Resources.Load<CEquipmentPotentialTableData>( ResourcePath );
            cachedTableData = loadedTableData;
            return cachedTableData;
        }

        ///<summary>
        /// 안정 키 기준 잠재 옵션 엔트리 반환 시도
        ///</summary>
        public static bool TryGetOptionEntryByKey( string _optionKey, out CEquipmentPotentialOptionEntry _optionEntry )
        {
            _optionEntry = null;

            if ( string.IsNullOrWhiteSpace( _optionKey ) )
            {
                return false;
            }

            CEquipmentPotentialTableData tableData = GetTableData();

            if ( tableData == null )
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<CEquipmentPotentialOptionEntry> optionEntryList = tableData.GetOptionEntryList();

            for ( int index = 0; index < optionEntryList.Count; index++ )
            {
                CEquipmentPotentialOptionEntry optionEntry = optionEntryList[ index ];

                if ( optionEntry == null || optionEntry.IsMatchedOptionKey( _optionKey ) == false )
                {
                    continue;
                }

                _optionEntry = optionEntry;
                return true;
            }

            return false;
        }

        ///<summary>
        /// 잠재 라인 기준 안정 키 반환 시도
        ///</summary>
        public static bool TryResolveOptionKey( CEquipmentPotentialLineData _lineData, out string _optionKey )
        {
            _optionKey = string.Empty;

            if ( _lineData == null || _lineData.HasValue() == false )
            {
                return false;
            }

            string savedOptionKey = _lineData.GetOptionKey();

            if ( string.IsNullOrWhiteSpace( savedOptionKey ) == false )
            {
                _optionKey = savedOptionKey;
                return true;
            }

            CEquipmentPotentialTableData tableData = GetTableData();

            if ( tableData == null )
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<CEquipmentPotentialOptionEntry> optionEntryList = tableData.GetOptionEntryList();

            for ( int index = 0; index < optionEntryList.Count; index++ )
            {
                CEquipmentPotentialOptionEntry optionEntry = optionEntryList[ index ];

                if ( optionEntry == null || optionEntry.IsMatchedLineData( _lineData ) == false )
                {
                    continue;
                }

                _optionKey = optionEntry.GetOptionKey();
                return true;
            }

            return false;
        }

        ///<summary>
        /// 잠재 테이블 데이터 재로드
        ///</summary>
        public static void Reload()
        {
            cachedTableData = null;
        }
    }
}
