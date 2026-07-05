using System;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 다국어 텍스트 테이블 행 데이터
    ///</summary>
    [Serializable]
    public sealed class CTextTableRow
    {
        [SerializeField] [CExcelHeader( "TextKey" )] private string textKey = string.Empty;
        [SerializeField] [TextArea] [CExcelHeader( "KR" )] private string krText = string.Empty;
        [SerializeField] [TextArea] [CExcelHeader( "EN" )] private string enText = string.Empty;

        ///<summary>
        /// 텍스트 키 반환
        ///</summary>
        public string GetKey()
        {
            string result = string.IsNullOrWhiteSpace( textKey ) ? string.Empty : textKey.Trim();
            return result;
        }

        ///<summary>
        /// 언어별 텍스트 반환
        ///</summary>
        public string GetText( eTextLanguage _textLanguage )
        {
            string result = string.Empty;

            switch ( _textLanguage )
            {
                case eTextLanguage.EN:
                    result = enText;
                    break;

                case eTextLanguage.KR:
                default:
                    result = krText;
                    break;
            }

            return result;
        }
    }
}
