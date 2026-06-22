using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 창 드래그 핸들
    ///</summary>
    public sealed class CItemInventoryWindowDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform targetWindowRectTransform;
        [SerializeField] private Canvas targetCanvas;

        private Vector2 dragOffset;

        ///<summary>
        /// 드래그 대상 설정
        ///</summary>
        public void Configure( RectTransform _targetWindowRectTransform, Canvas _targetCanvas )
        {
            targetWindowRectTransform = _targetWindowRectTransform;
            targetCanvas = _targetCanvas;
        }

        ///<summary>
        /// 창 전면 정렬 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            BringWindowToFront();
        }

        ///<summary>
        /// 창 드래그 시작 처리
        ///</summary>
        public void OnBeginDrag( PointerEventData _eventData )
        {
            BringWindowToFront();

            RectTransform canvasRectTransform = ResolveCanvasRectTransform();

            if ( canvasRectTransform == null || targetWindowRectTransform == null )
            {
                return;
            }

            Vector2 localPointerPosition;
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRectTransform, _eventData.position, _eventData.pressEventCamera, out localPointerPosition );

            if ( isConverted == false )
            {
                return;
            }

            dragOffset = targetWindowRectTransform.anchoredPosition - localPointerPosition;
        }

        ///<summary>
        /// 창 드래그 진행 처리
        ///</summary>
        public void OnDrag( PointerEventData _eventData )
        {
            RectTransform canvasRectTransform = ResolveCanvasRectTransform();

            if ( canvasRectTransform == null || targetWindowRectTransform == null )
            {
                return;
            }

            Vector2 localPointerPosition;
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRectTransform, _eventData.position, _eventData.pressEventCamera, out localPointerPosition );

            if ( isConverted == false )
            {
                return;
            }

            targetWindowRectTransform.anchoredPosition = localPointerPosition + dragOffset;
        }

        ///<summary>
        /// 대상 창 최상단 정렬
        ///</summary>
        private void BringWindowToFront()
        {
            RectTransform siblingTargetRectTransform = ResolveSiblingTargetRectTransform();

            if ( siblingTargetRectTransform == null )
            {
                return;
            }

            siblingTargetRectTransform.SetAsLastSibling();
        }

        ///<summary>
        /// 캔버스 RectTransform 결정
        ///</summary>
        private RectTransform ResolveCanvasRectTransform()
        {
            if ( targetCanvas == null )
            {
                return null;
            }

            RectTransform result = targetCanvas.transform as RectTransform;
            return result;
        }

        ///<summary>
        /// 창 최상위 RectTransform 결정
        ///</summary>
        private RectTransform ResolveSiblingTargetRectTransform()
        {
            if ( targetWindowRectTransform == null )
            {
                return null;
            }

            RectTransform canvasRectTransform = ResolveCanvasRectTransform();
            RectTransform currentRectTransform = targetWindowRectTransform;

            while ( currentRectTransform != null )
            {
                RectTransform parentRectTransform = currentRectTransform.parent as RectTransform;

                if ( parentRectTransform == null )
                {
                    break;
                }

                if ( parentRectTransform.parent == canvasRectTransform )
                {
                    break;
                }

                currentRectTransform = parentRectTransform;
            }

            RectTransform result = currentRectTransform;
            return result;
        }
    }
}
