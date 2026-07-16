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
    }
}
