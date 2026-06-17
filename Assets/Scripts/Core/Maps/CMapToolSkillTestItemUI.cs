using TinyHero.Skill;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴 스킬 테스트 항목 UI 컴포넌트
    ///</summary>
    public sealed class CMapToolSkillTestItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CMapToolRuntimeController targetController;
        private CSkillDefinition targetSkillDefinition;

        ///<summary>
        /// 테스트 항목 초기화 처리
        ///</summary>
        public void Initialize( CMapToolRuntimeController _targetController, CSkillDefinition _targetSkillDefinition )
        {
            targetController = _targetController;
            targetSkillDefinition = _targetSkillDefinition;
        }

        ///<summary>
        /// 포인터 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( targetController == null || targetSkillDefinition == null )
            {
                return;
            }

            targetController.HandleSkillTestItemPointerEnter( targetSkillDefinition );
        }

        ///<summary>
        /// 포인터 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( targetController == null || targetSkillDefinition == null )
            {
                return;
            }

            targetController.HandleSkillTestItemPointerExit( targetSkillDefinition );
        }
    }
}
