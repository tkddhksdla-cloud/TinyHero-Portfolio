namespace TinyHero.Core
{
    /// <summary>
    /// 여러 런타임 도메인에서 재사용하는 상태 없는 순수 공통 로직의 진입점입니다.
    /// </summary>
    public static class CGameUtils
    {
        public static string NormalizeId( string _value )
        {
            string result = string.IsNullOrWhiteSpace( _value ) ? string.Empty : _value.Trim();
            return result;
        }

        /// <summary>
        /// 필수 원격 콘텐츠 상태에서 Resources fallback을 차단해야 하는지 반환합니다.
        /// </summary>
        public static bool ShouldBlockRequiredRemoteContentFallback( bool _isRequiredRemoteUpdateDetected, bool _isRemoteContentRequired )
        {
            bool result = _isRequiredRemoteUpdateDetected || _isRemoteContentRequired;
            return result;
        }
    }
}
