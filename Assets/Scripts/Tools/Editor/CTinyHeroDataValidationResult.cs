using System;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 데이터 검증 결과 심각도
    ///</summary>
    public enum eTinyHeroDataValidationSeverity
    {
        ERROR,
        WARNING,
        INFO
    }

    ///<summary>
    /// TinyHero 데이터 검증 결과 데이터
    ///</summary>
    [Serializable]
    public sealed class CTinyHeroDataValidationResult
    {
        public eTinyHeroDataValidationSeverity severity;
        public string category;
        public string title;
        public string message;
        public string assetPath;

        ///<summary>
        /// 데이터 검증 결과 초기화
        ///</summary>
        public CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity _severity, string _category, string _title, string _message, string _assetPath )
        {
            severity = _severity;
            category = string.IsNullOrWhiteSpace( _category ) ? string.Empty : _category.Trim();
            title = string.IsNullOrWhiteSpace( _title ) ? string.Empty : _title.Trim();
            message = string.IsNullOrWhiteSpace( _message ) ? string.Empty : _message.Trim();
            assetPath = string.IsNullOrWhiteSpace( _assetPath ) ? string.Empty : _assetPath.Trim();
        }
    }
}
