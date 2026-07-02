using TinyHero.HotfixContracts;

namespace TinyHero.Hotfix
{
    ///<summary>
    /// 테스트 전용 스킬 사용 Hotfix 모듈
    ///</summary>
    public sealed class CSkillUseTestHotfixModule : IHotfixModule
    {
        private const string ModuleId = "skill";
        private const string CommandId = "try_use_skill";
        private const string TestSkillId = "skill_hotfix_test";
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

        ///<summary>스킬 테스트 Hotfix 실행 가능 여부 반환</summary>
        public bool CanExecute( CHotfixExecutionContext _context )
        {
            if ( _context == null )
            {
                return false;
            }

            if ( string.Equals( _context.GetModuleId(), ModuleId, System.StringComparison.Ordinal ) == false )
            {
                return false;
            }

            if ( string.Equals( _context.GetCommandId(), CommandId, System.StringComparison.Ordinal ) == false )
            {
                return false;
            }

            bool hasSkillId = _context.TryGetStringValue( "skillId", out string skillId );
            bool result = hasSkillId && string.Equals( skillId, TestSkillId, System.StringComparison.Ordinal );
            return result;
        }

        ///<summary>스킬 테스트 Hotfix 실행 결과 반환</summary>
        public CHotfixExecutionResult Execute( CHotfixExecutionContext _context )
        {
            if ( CanExecute( _context ) == false )
            {
                CHotfixExecutionResult blockedResult = CHotfixExecutionResult.CreateBlocked( "Skill hotfix context was not matched." );
                return blockedResult;
            }

            CHotfixExecutionResult result = CHotfixExecutionResult.CreateSuccess( "Skill hotfix test module executed." );
            return result;
        }
    }
}
