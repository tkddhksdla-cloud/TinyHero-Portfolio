using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 슬롯 아이콘 툴팁 트리거 컴포넌트
    ///</summary>
    public sealed class CSkillSlotTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private CSkillListUIController ownerSkillListUiController;
        [SerializeField] private string targetSkillId = string.Empty;

        ///<summary>
        /// 툴팁 트리거 대상 구성
        ///</summary>
        public void Configure( CSkillListUIController _ownerSkillListUiController, string _targetSkillId )
        {
            ownerSkillListUiController = _ownerSkillListUiController;
            targetSkillId = string.IsNullOrWhiteSpace( _targetSkillId ) ? string.Empty : _targetSkillId.Trim();
        }

        ///<summary>
        /// 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null || string.IsNullOrWhiteSpace( targetSkillId ) )
            {
                return;
            }

            ownerSkillListUiController.ShowSkillTooltip( targetSkillId );
        }

        ///<summary>
        /// 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null )
            {
                return;
            }

            ownerSkillListUiController.HideSkillTooltip();
        }
        ///<summary>
        /// 스킬 아이콘 마우스 다운 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null )
            {
                return;
            }

            ownerSkillListUiController.HideSkillTooltip();
        }
    }
}
