using System;
using System.Collections;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 엑셀 테이블 데이터 기준 클래스
    ///</summary>
    public abstract class CExcelTableDataBase : ScriptableObject
    {
        ///<summary>
        /// 행 타입 반환
        ///</summary>
        public abstract Type GetRowType();

        ///<summary>
        /// 행 목록 교체
        ///</summary>
        public abstract void ReplaceRowList(IList _rowList);

        ///<summary>
        /// 원본 엑셀 에셋 경로 설정
        ///</summary>
        public abstract void SetSourceExcelAssetPath(string _sourceExcelAssetPath);
    }
}


