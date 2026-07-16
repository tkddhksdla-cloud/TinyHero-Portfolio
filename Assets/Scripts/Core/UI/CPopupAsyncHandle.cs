using System;
using TinyHero.UI;

namespace TinyHero.Core
{
    /// <summary>
    /// 팝업의 비동기 생성과 재사용 인스턴스 캐시를 담당하는 합성 객체입니다.
    /// 팝업별 표시 규칙과 도메인 데이터 바인딩은 호출 매니저가 담당합니다.
    /// </summary>
    public sealed class CPopupAsyncHandle<TPopup> where TPopup : CUIPopup
    {
        private readonly eResourceKey resourceKey;
        private readonly bool shouldReuseExistingInstance;

        private TPopup cachedPopup;

        public CPopupAsyncHandle( eResourceKey _resourceKey, bool _shouldReuseExistingInstance )
        {
            resourceKey = _resourceKey;
            shouldReuseExistingInstance = _shouldReuseExistingInstance;
        }

        public TPopup GetCachedPopup()
        {
            TPopup result = cachedPopup;
            return result;
        }

        public bool Request( Action<TPopup> _onCompleted )
        {
            if ( cachedPopup != null )
            {
                InvokeCompletedHandler( _onCompleted, cachedPopup );
                return true;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                InvokeCompletedHandler( _onCompleted, null );
                return false;
            }

            navigationController.AddPopupAsync<TPopup>(
                resourceKey,
                shouldReuseExistingInstance,
                ( TPopup _createdPopup ) =>
                {
                    cachedPopup = _createdPopup;
                    InvokeCompletedHandler( _onCompleted, cachedPopup );
                } );
            return true;
        }

        public void ClearCachedPopup()
        {
            cachedPopup = null;
        }

        private void InvokeCompletedHandler( Action<TPopup> _onCompleted, TPopup _popup )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _popup );
        }
    }
}
