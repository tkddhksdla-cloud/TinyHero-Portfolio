namespace TinyHero.Hotfix
{
    ///<summary>
    /// Hotfix 어셈블리 존재 확인용 마커 타입
    ///</summary>
    public static class CHotfixAssemblyMarker
    {
        ///<summary>Hotfix 어셈블리 이름 반환</summary>
        public static string GetAssemblyName()
        {
            string result = "TinyHero.Hotfix";
            return result;
        }
    }
}
