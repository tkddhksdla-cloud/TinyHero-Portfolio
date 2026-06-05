using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    /// <summary>
    /// 엑셀 row 목록을 직렬화하는 범용 테이블 클래스이다.
    /// </summary>
    public abstract class CExcelTableData<TRow> : CExcelTableDataBase where TRow : class, new()
    {
        [SerializeField] private string sourceExcelAssetPath = string.Empty;
        [SerializeField] private List<TRow> rowList = new List<TRow>();

        /// <summary>
        /// 현재 테이블이 보관하는 row 목록을 반환한다.
        /// </summary>
        public List<TRow> GetRowList()
        {
            List<TRow> result = rowList;
            return result;
        }

        /// <summary>
        /// 현재 테이블이 보관하는 원본 엑셀 경로를 반환한다.
        /// </summary>
        public string GetSourceExcelAssetPath()
        {
            string result = sourceExcelAssetPath;
            return result;
        }

        /// <summary>
        /// 테이블이 보관하는 row 타입을 반환한다.
        /// </summary>
        public override Type GetRowType()
        {
            Type result = typeof( TRow );
            return result;
        }

        /// <summary>
        /// 외부 row 목록으로 내부 직렬화 목록을 교체한다.
        /// </summary>
        public override void ReplaceRowList( IList newRowList )
        {
            rowList.Clear();

            if ( newRowList == null )
            {
                return;
            }

            for ( int i = 0; i < newRowList.Count; i++ )
            {
                TRow rowData = newRowList[ i ] as TRow;

                if ( rowData == null )
                {
                    continue;
                }

                rowList.Add( rowData );
            }
        }

        /// <summary>
        /// 원본 엑셀 에셋 경로를 갱신한다.
        /// </summary>
        public override void SetSourceExcelAssetPath( string newSourceExcelAssetPath )
        {
            sourceExcelAssetPath = newSourceExcelAssetPath;
        }
    }
}
