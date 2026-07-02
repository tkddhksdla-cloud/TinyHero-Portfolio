using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TinyHero.Tools.Editor
{
    public static class CTinyHeroEditModeTestRunner
    {
        private static readonly string[] CoreEditModeTestGroups =
        {
            "TinyHero.Tests.DataValidationRulesTests",
            "TinyHero.Tests.InventoryDataTests",
            "TinyHero.Tests.SaveProtectionTests",
            "TinyHero.Tests.SecureNumberTests",
            "TinyHero.Tests.SkillDataTests",
            "TinyHero.Tests.SkillManagerTests"
        };

        ///<summary>핵심 EditMode 테스트 묶음 실행 메뉴</summary>
        [MenuItem( "TinyHero/Tests/Run Core EditMode Tests" )]
        public static void RunCoreEditModeTests()
        {
            TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            Filter coreTestFilter = new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = CoreEditModeTestGroups
            };
            ExecutionSettings executionSettings = new ExecutionSettings( coreTestFilter );
            string executionId = testRunnerApi.Execute( executionSettings );

            Debug.Log( $"[TinyHero Tests] Core EditMode test run requested. ExecutionId: {executionId}" );
        }
    }
}
