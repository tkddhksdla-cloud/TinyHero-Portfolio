using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 엑셀 테이블 데이터 데이터
    ///</summary>
    public abstract class CExcelTableData<TRow> : CExcelTableDataBase where TRow : class, new()
    {
        [SerializeField] private string sourceExcelAssetPath = string.Empty;
        [SerializeField] private List<TRow> rowList = new List<TRow>();

        ///<summary>
        /// 행 목록 반환
        ///</summary>
        public List<TRow> GetRowList()
        {
            List<TRow> result = rowList;
            return result;
        }

        ///<summary>
        /// 원본 엑셀 에셋 경로 반환
        ///</summary>
        public string GetSourceExcelAssetPath()
        {
            string result = sourceExcelAssetPath;
            return result;
        }

        ///<summary>
        /// 행 타입 반환
        ///</summary>
        public override Type GetRowType()
        {
            Type result = typeof( TRow );
            return result;
        }

        ///<summary>
        /// 행 목록 교체
        ///</summary>
        public override void ReplaceRowList(IList _newRowList)
        {
            rowList.Clear();

            if ( _newRowList == null )
            {
                return;
            }

            for ( int i = 0; i < _newRowList.Count; i++ )
            {
                TRow rowData = _newRowList[ i ] as TRow;

                if ( rowData == null )
                {
                    continue;
                }

                rowList.Add( rowData );
            }
        }

        ///<summary>
        /// 원본 엑셀 에셋 경로 설정
        ///</summary>
        public override void SetSourceExcelAssetPath(string _newSourceExcelAssetPath)
        {
            sourceExcelAssetPath = _newSourceExcelAssetPath;
        }
    }
}


