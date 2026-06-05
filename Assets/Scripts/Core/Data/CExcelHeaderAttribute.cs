using System;

namespace TinyHero.Core.Data
{
    /// <summary>
    /// 엑셀 헤더명과 필드명을 연결하는 속성이다.
    /// </summary>
    [AttributeUsage( AttributeTargets.Field )]
    public sealed class CExcelHeaderAttribute : Attribute
    {
        private readonly string headerName;

        /// <summary>
        /// 엑셀 헤더명을 저장한다.
        /// </summary>
        public CExcelHeaderAttribute( string headerName )
        {
            this.headerName = headerName;
        }

        /// <summary>
        /// 지정된 엑셀 헤더명을 반환한다.
        /// </summary>
        public string GetHeaderName()
        {
            string result = headerName;
            return result;
        }
    }
}
