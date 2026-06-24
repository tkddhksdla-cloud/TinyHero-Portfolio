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

            CEquipmentPotentialTableData loadedTableData = Resources.Load<CEquipmentPotentialTableData>( ResourcePath );
            cachedTableData = loadedTableData;
            return cachedTableData;
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
