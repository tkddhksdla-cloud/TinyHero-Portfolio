using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 몬스터 스탯 테이블 데이터 클래스
    ///</summary>
    [CreateAssetMenu( fileName = "MonsterStatTableData", menuName = "TinyHero/Data/Monster Stat Table Data" )]
    public sealed class CMonsterStatTableData : CExcelTableData<CMonsterStatRow>
    {
        ///<summary>
        /// 몬스터 식별자 기준 행 조회 시도
        ///</summary>
        public bool TryGetRow(string _id, out CMonsterStatRow _rowData)
        {
            List<CMonsterStatRow> rowList = GetRowList();

            for ( int i = 0; i < rowList.Count; i++ )
            {
                CMonsterStatRow rowData = rowList[ i ];

                if ( rowData == null )
                {
                    continue;
                }

                string rowId = rowData.GetId();
                bool isMatched = string.Equals( rowId, _id, System.StringComparison.OrdinalIgnoreCase );

                if ( isMatched == false )
                {
                    continue;
                }

                _rowData = rowData;
                return true;
            }

            _rowData = null;
            return false;
        }
    }
}
