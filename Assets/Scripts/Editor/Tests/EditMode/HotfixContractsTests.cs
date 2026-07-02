using NUnit.Framework;
using TinyHero.Core;
using TinyHero.Hotfix;
using TinyHero.HotfixContracts;

namespace TinyHero.Tests
{
    public sealed class HotfixContractsTests
    {
        ///<summary>Hotfix 실행 문맥 값 보존 검증</summary>
        [Test]
        public void ExecutionContext_StoresTypedValues()
        {
            CHotfixExecutionContext context = new CHotfixExecutionContext( "skill", "damage", 2 );
            context.SetStringValue( "skillId", "skill_double_jump" );
            context.SetIntValue( "level", 3 );
            context.SetFloatValue( "multiplier", 1.5f );

            bool hasSkillId = context.TryGetStringValue( "skillId", out string skillId );
            bool hasLevel = context.TryGetIntValue( "level", out int level );
            bool hasMultiplier = context.TryGetFloatValue( "multiplier", out float multiplier );

            Assert.IsTrue( hasSkillId );
            Assert.IsTrue( hasLevel );
            Assert.IsTrue( hasMultiplier );
            Assert.AreEqual( "skill", context.GetModuleId() );
            Assert.AreEqual( "damage", context.GetCommandId() );
            Assert.AreEqual( 2, context.GetVersion() );
            Assert.AreEqual( "skill_double_jump", skillId );
            Assert.AreEqual( 3, level );
            Assert.AreEqual( 1.5f, multiplier );
        }

        ///<summary>Hotfix 실행 결과 상태 검증</summary>
        [Test]
        public void ExecutionResult_ReportsStatus()
        {
            CHotfixExecutionResult result = CHotfixExecutionResult.CreateFallback( "fallback" );

            Assert.AreEqual( eHotfixExecutionStatus.FALLBACK, result.GetStatus() );
            Assert.AreEqual( "fallback", result.GetMessage() );
            Assert.IsFalse( result.IsSuccess() );
        }

        ///<summary>기본 Hotfix 모듈 fallback 검증</summary>
        [Test]
        public void DefaultHotfixModule_ReturnsFallback()
        {
            CDefaultHotfixModule module = new CDefaultHotfixModule();
            CHotfixExecutionContext context = new CHotfixExecutionContext( module.GetModuleId(), "noop", module.GetVersion() );

            CHotfixExecutionResult result = module.Execute( context );

            Assert.AreEqual( "default", module.GetModuleId() );
            Assert.AreEqual( 1, module.GetVersion() );
            Assert.AreEqual( eHotfixExecutionStatus.FALLBACK, result.GetStatus() );
        }

        ///<summary>기본 Hotfix 모듈 매칭 범위 검증</summary>
        [Test]
        public void DefaultHotfixModule_RejectsDifferentModule()
        {
            CDefaultHotfixModule module = new CDefaultHotfixModule();
            CHotfixExecutionContext context = new CHotfixExecutionContext( "skill", "try_use_skill", 1 );

            bool canExecute = module.CanExecute( context );

            Assert.IsFalse( canExecute );
        }

        ///<summary>Hotfix 레지스트리 미매칭 fallback 검증</summary>
        [Test]
        public void HotfixModuleRegistry_ReturnsFallbackWhenModuleMissing()
        {
            CHotfixModuleRegistry.ClearCache();
            CHotfixExecutionContext context = new CHotfixExecutionContext( "skill", "try_use_skill", 1 );

            CHotfixExecutionResult result = CHotfixModuleRegistry.ExecuteOrFallback( context );

            Assert.AreEqual( eHotfixExecutionStatus.FALLBACK, result.GetStatus() );
        }

        ///<summary>스킬 테스트 Hotfix 모듈 성공 검증</summary>
        [Test]
        public void SkillUseTestHotfixModule_ReturnsSuccessForTestSkill()
        {
            CSkillUseTestHotfixModule module = new CSkillUseTestHotfixModule();
            CHotfixExecutionContext context = new CHotfixExecutionContext( "skill", "try_use_skill", 1 );
            context.SetStringValue( "skillId", "skill_hotfix_test" );

            CHotfixExecutionResult result = module.Execute( context );

            Assert.AreEqual( eHotfixExecutionStatus.SUCCESS, result.GetStatus() );
            Assert.IsTrue( result.IsSuccess() );
        }

        ///<summary>Hotfix 레지스트리 성공 모듈 검색 검증</summary>
        [Test]
        public void HotfixModuleRegistry_ReturnsSuccessForTestSkill()
        {
            CHotfixModuleRegistry.ClearCache();
            CHotfixExecutionContext context = new CHotfixExecutionContext( "skill", "try_use_skill", 1 );
            context.SetStringValue( "skillId", "skill_hotfix_test" );

            CHotfixExecutionResult result = CHotfixModuleRegistry.ExecuteOrFallback( context );

            Assert.AreEqual( eHotfixExecutionStatus.SUCCESS, result.GetStatus() );
        }
    }
}
