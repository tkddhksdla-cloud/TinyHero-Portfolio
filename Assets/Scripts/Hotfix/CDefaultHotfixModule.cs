using TinyHero.HotfixContracts;

namespace TinyHero.Hotfix
{
    ///<summary>
    /// 기본 Hotfix 모듈
    ///</summary>
    public sealed class CDefaultHotfixModule : IHotfixModule
    {
        private const string ModuleId = "default";
        private const int ModuleVersion = 1;

        ///<summary>Hotfix 모듈 식별자 반환</summary>
        public string GetModuleId()
        {
            string result = ModuleId;
            return result;
        }

        ///<summary>Hotfix 모듈 버전 반환</summary>
        public int GetVersion()
        {
            int result = ModuleVersion;
            return result;
        }

        ///<summary>Hotfix 실행 가능 여부 반환</summary>
        public bool CanExecute( CHotfixExecutionContext _context )
        {
            bool result = _context != null && string.Equals( _context.GetModuleId(), ModuleId, System.StringComparison.Ordinal );
            return result;
        }

        ///<summary>Hotfix 기본 실행 결과 반환</summary>
        public CHotfixExecutionResult Execute( CHotfixExecutionContext _context )
        {
            if ( CanExecute( _context ) == false )
            {
                CHotfixExecutionResult blockedResult = CHotfixExecutionResult.CreateBlocked( "Invalid hotfix context." );
                return blockedResult;
            }

            CHotfixExecutionResult result = CHotfixExecutionResult.CreateFallback( "Default hotfix module has no override." );
            return result;
        }
    }
}
