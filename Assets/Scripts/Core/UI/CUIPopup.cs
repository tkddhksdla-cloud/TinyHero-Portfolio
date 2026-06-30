using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 팝업 UI 공용 기반 클래스
    ///</summary>
    public abstract class CUIPopup : CUILayer
    {
        private float closeLockedUntilTime;

        ///<summary>
        /// 팝업 닫기 잠금 여부 반환
        ///</summary>
        public bool IsCloseLocked()
        {
            bool result = Time.unscaledTime < closeLockedUntilTime;
            return result;
        }

        ///<summary>
        /// 지정 시간 동안 팝업 닫기 잠금
        ///</summary>
        public void LockCloseForSeconds( float _lockSeconds )
        {
            if ( _lockSeconds <= 0.0f )
            {
                return;
            }

            float nextLockedUntilTime = Time.unscaledTime + _lockSeconds;
            closeLockedUntilTime = Mathf.Max( closeLockedUntilTime, nextLockedUntilTime );
        }

        ///<summary>
        /// ESC 닫기 허용 여부 반환
        ///</summary>
        public override bool CanCloseByEscape()
        {
            if ( IsCloseLocked() )
            {
                return false;
            }

            bool result = base.CanCloseByEscape();
            return result;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            if ( IsCloseLocked() )
            {
                return;
            }

            base.CloseNavigationLayer();
        }

        ///<summary>
        /// 팝업 창 상호작용 컴포넌트 보장
        ///</summary>
        protected void EnsurePopupWindowInteraction( RectTransform _windowRootRectTransform, RectTransform _windowDragHandleRectTransform, Canvas _targetCanvas, RectTransform _siblingTargetRectTransform )
        {
            EnsurePopupWindowDragHandle( _windowRootRectTransform, _windowDragHandleRectTransform, _targetCanvas );
            EnsurePopupWindowFocusHandlers( _windowRootRectTransform, _siblingTargetRectTransform );
        }

        ///<summary>
        /// 팝업 창 드래그 핸들 보장
        ///</summary>
        protected void EnsurePopupWindowDragHandle( RectTransform _windowRootRectTransform, RectTransform _windowDragHandleRectTransform, Canvas _targetCanvas )
        {
            if ( _windowRootRectTransform == null || _windowDragHandleRectTransform == null || _targetCanvas == null )
            {
                return;
            }

            CItemInventoryWindowDragHandle dragHandle = _windowDragHandleRectTransform.GetComponent<CItemInventoryWindowDragHandle>();

            if ( dragHandle == null )
            {
                dragHandle = _windowDragHandleRectTransform.gameObject.AddComponent<CItemInventoryWindowDragHandle>();
            }

            dragHandle.Configure( _windowRootRectTransform, _targetCanvas );
        }

        ///<summary>
        /// 팝업 창 포커스 핸들 보장
        ///</summary>
        protected void EnsurePopupWindowFocusHandlers( RectTransform _windowRootRectTransform, RectTransform _siblingTargetRectTransform )
        {
            if ( _windowRootRectTransform == null || _siblingTargetRectTransform == null )
            {
                return;
            }

            Graphic[] graphicArray = _windowRootRectTransform.GetComponentsInChildren<Graphic>( true );

            for ( int index = 0; index < graphicArray.Length; index++ )
            {
                Graphic graphic = graphicArray[ index ];

                if ( graphic == null || graphic.raycastTarget == false )
                {
                    continue;
                }

                CWindowDragHandle focusHandler = graphic.GetComponent<CWindowDragHandle>();

                if ( focusHandler == null )
                {
                    focusHandler = graphic.gameObject.AddComponent<CWindowDragHandle>();
                }

                focusHandler.Configure( _siblingTargetRectTransform );
            }
        }

        ///<summary>
        /// 팝업 창 최상단 정렬
        ///</summary>
        protected void BringPopupWindowToFront( RectTransform _siblingTargetRectTransform )
        {
            if ( _siblingTargetRectTransform == null )
            {
                return;
            }

            _siblingTargetRectTransform.SetAsLastSibling();
        }
    }
}
