using System;
using System.Collections;
using UnityEngine;

namespace TinyHero.Core.Data
{
    /// <summary>
    /// 엑셀 테이블 ScriptableObject의 공통 기반 클래스이다.
    /// </summary>
    public abstract class CExcelTableDataBase : ScriptableObject
    {
        /// <summary>
        /// 테이블이 보관하는 row 타입을 반환한다.
        /// </summary>
        public abstract Type GetRowType();

        /// <summary>
        /// 외부에서 파싱한 row 목록으로 테이블 내용을 교체한다.
        /// </summary>
        public abstract void ReplaceRowList( IList rowList );

        /// <summary>
        /// 마지막으로 가져온 원본 엑셀 경로를 저장한다.
        /// </summary>
        public abstract void SetSourceExcelAssetPath( string sourceExcelAssetPath );
    }
}
