using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 다국어 텍스트 엑셀 테이블 데이터
    ///</summary>
    [CreateAssetMenu( fileName = "TextTableData", menuName = "TinyHero/Data/Text Table Data" )]
    public sealed class CTextTableData : CExcelTableData<CTextTableRow>
    {
        ///<summary>
        /// 텍스트 키 기반 행 사전 생성
        ///</summary>
        public Dictionary<string, CTextTableRow> CreateRowDictionary()
        {
            Dictionary<string, CTextTableRow> rowDictionary = new Dictionary<string, CTextTableRow>();
            List<CTextTableRow> rowList = GetRowList();

            if ( rowList == null )
            {
                return rowDictionary;
            }

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CTextTableRow row = rowList[ index ];

                if ( row == null )
                {
                    continue;
                }

                string key = row.GetKey();

                if ( string.IsNullOrWhiteSpace( key ) )
                {
                    continue;
                }

                rowDictionary[ key ] = row;
            }

            return rowDictionary;
        }
    }
}
