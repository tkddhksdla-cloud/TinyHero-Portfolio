using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// UI 레이어 공용 기반 클래스
    ///</summary>
    public abstract class CUILayer : MonoBehaviour
    {
        [SerializeField] private bool canCloseByEscape = true;

        ///<summary>
        /// ESC 닫기 허용 여부 반환
        ///</summary>
        public virtual bool CanCloseByEscape()
        {
            bool result = canCloseByEscape;
            return result;
        }

        ///<summary>
        /// 네비게이션 표시 상태 반환
        ///</summary>
        public virtual bool IsNavigationVisible()
        {
            bool result = gameObject.activeSelf;
            return result;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public virtual void CloseNavigationLayer()
        {
            SetLayerVisible( false );
        }

        ///<summary>
        /// 네비게이션 레이어 표시 상태 반영
        ///</summary>
        public virtual void SetLayerVisible( bool _isVisible )
        {
            if ( gameObject.activeSelf == _isVisible )
            {
                return;
            }

            gameObject.SetActive( _isVisible );
        }

        ///<summary>
        /// 네비게이션 레이어 최상단 정렬
        ///</summary>
        public virtual void BringLayerToFront()
        {
            RectTransform layerRectTransform = transform as RectTransform;

            if ( layerRectTransform == null )
            {
                return;
            }

            layerRectTransform.SetAsLastSibling();
        }
    }
}
