namespace TinyHero.HotfixContracts
{
    ///<summary>
    /// Hotfix 실행 모듈 계약
    ///</summary>
    public interface IHotfixModule
    {
        ///<summary>Hotfix 모듈 식별자 반환</summary>
        string GetModuleId();

        ///<summary>Hotfix 모듈 버전 반환</summary>
        int GetVersion();

        ///<summary>Hotfix 실행 가능 여부 반환</summary>
        bool CanExecute( CHotfixExecutionContext _context );

        ///<summary>Hotfix 실행 결과 반환</summary>
        CHotfixExecutionResult Execute( CHotfixExecutionContext _context );
    }
}
