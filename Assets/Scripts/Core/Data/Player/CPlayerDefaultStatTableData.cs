using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 플레이어 기본 스탯 테이블 데이터
    ///</summary>
    [CreateAssetMenu( fileName = "PlayerDefaultStatTableData", menuName = "TinyHero/Data/Player Default Stat Table Data" )]
    public sealed class CPlayerDefaultStatTableData : CExcelTableData<CPlayerDefaultStatRow>
    {
        ///<summary>
        /// 첫 기본 스탯 행 반환
        ///</summary>
        public CPlayerDefaultStatRow GetDefaultRow()
        {
            System.Collections.Generic.List<CPlayerDefaultStatRow> rowList = GetRowList();

            if ( rowList == null || rowList.Count <= 0 )
            {
                return null;
            }

            CPlayerDefaultStatRow result = rowList[ 0 ];
            return result;
        }
    }
}
