using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 스탯 설명 툴팁 트리거 컴포넌트
    ///</summary>
    public sealed class CPlayerStatTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string tooltipTitle = string.Empty;
        [SerializeField] private string tooltipDescription = string.Empty;
        [SerializeField] private CPlayerEquipmentStatusPanelUI ownerPanelUi;

        ///<summary>
        /// 툴팁 대상 구성
        ///</summary>
        public void Configure( CPlayerEquipmentStatusPanelUI _ownerPanelUi, string _tooltipTitle, string _tooltipDescription )
        {
            ownerPanelUi = _ownerPanelUi;
            tooltipTitle = string.IsNullOrWhiteSpace( _tooltipTitle ) ? string.Empty : _tooltipTitle;
            tooltipDescription = string.IsNullOrWhiteSpace( _tooltipDescription ) ? string.Empty : _tooltipDescription;
        }

        ///<summary>
        /// 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null )
            {
                return;
            }

            ownerPanelUi.ShowStatTooltip( tooltipTitle, tooltipDescription );
        }

        ///<summary>
        /// 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null )
            {
                return;
            }

            ownerPanelUi.HideTooltip();
        }
    }
}
