namespace TinyHero.HotfixContracts
{
    ///<summary>
    /// Hotfix 실행 결과 상태
    ///</summary>
    public enum eHotfixExecutionStatus
    {
        SUCCESS,
        BLOCKED,
        FAILED,
        FALLBACK
    }
}
