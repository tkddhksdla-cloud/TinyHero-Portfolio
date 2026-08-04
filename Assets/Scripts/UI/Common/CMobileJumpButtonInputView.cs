using TinyHero.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 모바일 점프 버튼 입력 뷰
    ///</summary>
    public sealed class CMobileJumpButtonInputView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        ///<summary>
        /// 점프 버튼 누름 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            if ( IsMobileRuntime() == false )
            {
                return;
            }

            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager == null )
            {
                return;
            }

            inputManager.RequestMobileJumpDown();
            inputManager.SetMobileJumpHeld( true );
        }

        ///<summary>
        /// 점프 버튼 해제 처리
        ///</summary>
        public void OnPointerUp( PointerEventData _eventData )
        {
            ReleaseJumpInput();
        }

        ///<summary>
        /// 점프 버튼 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            ReleaseJumpInput();
        }

        ///<summary>
        /// 비활성화 시 점프 입력 해제
        ///</summary>
        private void OnDisable()
        {
            ReleaseJumpInput();
        }

        ///<summary>
        /// 모바일 런타임 여부 반환
        ///</summary>
        private bool IsMobileRuntime()
        {
            bool result = Application.isMobilePlatform;
            return result;
        }

        ///<summary>
        /// 점프 유지 입력 해제
        ///</summary>
        private void ReleaseJumpInput()
        {
            bool hasInputManager = CInputManager.TryGetExistingInstance( out CInputManager inputManager );

            if ( hasInputManager == false || inputManager == null )
            {
                return;
            }

            inputManager.SetMobileJumpHeld( false );
        }
    }
}
