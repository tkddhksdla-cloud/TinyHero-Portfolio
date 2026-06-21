using UnityEditor;

namespace TinyHero.Tools
{
    ///<summary>
    /// 자주 사용하는 에디터 창 일괄 실행 메뉴
    ///</summary>
    public static class CToolWindowShortcutMenu
    {
        ///<summary>
        /// 자주 사용하는 에디터 창 일괄 실행
        ///</summary>
        [MenuItem( "TinyHero/Open M.B / N.I / Q.D / I.D" )]
        private static void OpenCommonEditorWindows()
        {
            MonsterBehaviorPatternEditorWindow.OpenWindow();
            NPCInteractionDataEditorWindow.OpenWindow();
            CQuestDefinitionEditorWindow.OpenWindow();
            ItemDefinitionEditorWindow.OpenWindow();
        }
    }
}
