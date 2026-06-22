using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// UI 창 클릭 시 최상단 정렬 처리 컴포넌트
    ///</summary>
    public sealed class CWindowDragHandle : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private RectTransform siblingTargetRectTransform;

        ///<summary>
        /// 클릭 시 최상단 정렬 대상 설정
        ///</summary>
        public void Configure( RectTransform _siblingTargetRectTransform )
        {
            siblingTargetRectTransform = _siblingTargetRectTransform;
        }

        ///<summary>
        /// 창 클릭 입력 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            BringWindowToFront();
        }

        ///<summary>
        /// 대상 창 최상단 정렬
        ///</summary>
        private void BringWindowToFront()
        {
            if ( siblingTargetRectTransform == null )
            {
                return;
            }

            siblingTargetRectTransform.SetAsLastSibling();
        }
    }
}
