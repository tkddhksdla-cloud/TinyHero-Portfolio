using System;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 엑셀 헤더 특성
    ///</summary>
    [AttributeUsage( AttributeTargets.Field )]
    public sealed class CExcelHeaderAttribute : Attribute
    {
        private readonly string headerName;

        ///<summary>
        /// 헤더 이름 초기화
        ///</summary>
        public CExcelHeaderAttribute( string _headerName )
        {
            headerName = _headerName;
        }

        ///<summary>
        /// 헤더 이름 반환
        ///</summary>
        public string GetHeaderName()
        {
            string result = headerName;
            return result;
        }
    }
}


