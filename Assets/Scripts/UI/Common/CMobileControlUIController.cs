using TinyHero.Core;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 모바일 조작 HUD 표시와 안전 영역을 관리하는 컴포넌트
    ///</summary>
    public sealed class CMobileControlUIController : MonoBehaviour
    {
        [Header( "참조" )]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private CMobileJoystickInputView mobileJoystickInputView;
        [SerializeField] private CMobileJumpButtonInputView mobileJumpButtonInputView;

        private Rect cachedSafeArea;
        private Vector2Int cachedScreenSize;

        ///<summary>
        /// 모바일 HUD 초기화
        ///</summary>
        private void Awake()
        {
            ApplyMobileControlVisibility();
        }

        ///<summary>
        /// 모바일 HUD 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            ApplyMobileControlVisibility();
        }

        ///<summary>
        /// 안전 영역 변경 처리
        ///</summary>
        private void Update()
        {
            if ( IsMobileRuntime() == false )
            {
                return;
            }

            Rect currentSafeArea = Screen.safeArea;
            Vector2Int currentScreenSize = new Vector2Int( Screen.width, Screen.height );

            if ( currentSafeArea == cachedSafeArea && currentScreenSize == cachedScreenSize )
            {
                return;
            }

            ApplySafeArea();
        }

        ///<summary>
        /// 비활성화 시 입력 초기화
        ///</summary>
        private void OnDisable()
        {
            ResetMobileInput();
        }

        /// <summary>
        /// 앱 포커스 상실 시 모바일 입력을 즉시 해제합니다.
        /// </summary>
        private void OnApplicationFocus( bool _hasFocus )
        {
            if ( _hasFocus )
            {
                return;
            }

            ResetMobileInput();
        }

        /// <summary>
        /// 앱 일시정지 시 모바일 입력을 즉시 해제합니다.
        /// </summary>
        private void OnApplicationPause( bool _isPaused )
        {
            if ( _isPaused == false )
            {
                return;
            }

            ResetMobileInput();
        }

        ///<summary>
        /// 모바일 조작 UI 표시 상태 반영
        ///</summary>
        private void ApplyMobileControlVisibility()
        {
            bool isMobileRuntime = IsMobileRuntime();

            if ( mobileJoystickInputView != null )
            {
                mobileJoystickInputView.gameObject.SetActive( isMobileRuntime );
            }

            if ( mobileJumpButtonInputView != null )
            {
                mobileJumpButtonInputView.gameObject.SetActive( isMobileRuntime );
            }

            if ( isMobileRuntime )
            {
                ApplySafeArea();
                return;
            }

            ResetMobileInput();
        }

        ///<summary>
        /// 안전 영역 반영
        ///</summary>
        private void ApplySafeArea()
        {
            if ( safeAreaRoot == null )
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float screenWidth = Mathf.Max( 1.0f, Screen.width );
            float screenHeight = Mathf.Max( 1.0f, Screen.height );
            Vector2 anchorMin = new Vector2( safeArea.xMin / screenWidth, safeArea.yMin / screenHeight );
            Vector2 anchorMax = new Vector2( safeArea.xMax / screenWidth, safeArea.yMax / screenHeight );
            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            cachedSafeArea = safeArea;
            cachedScreenSize = new Vector2Int( Screen.width, Screen.height );
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
        /// 모바일 입력 상태 초기화
        ///</summary>
        private void ResetMobileInput()
        {
            if ( mobileJoystickInputView != null )
            {
                mobileJoystickInputView.ResetInput();
            }

            bool hasInputManager = CInputManager.TryGetExistingInstance( out CInputManager inputManager );

            if ( hasInputManager == false || inputManager == null )
            {
                return;
            }

            inputManager.ClearMobileInputState();
        }
    }
}
