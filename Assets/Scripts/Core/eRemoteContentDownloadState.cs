namespace TinyHero.Core
{
    ///<summary>
    /// 원격 콘텐츠 다운로드 진행 상태
    ///</summary>
    public enum eRemoteContentDownloadState
    {
        CHECKING,
        AWAITING_CONFIRMATION,
        DOWNLOADING,
        COMPLETED,
        FAILED
    }
}
