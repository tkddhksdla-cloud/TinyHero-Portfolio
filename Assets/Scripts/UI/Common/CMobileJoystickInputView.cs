using TinyHero.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 모바일 화면 좌측 터치 기반 조이스틱 입력 뷰
    ///</summary>
    public sealed class CMobileJoystickInputView : MonoBehaviour
    {
        [Header( "참조" )]
        [SerializeField] private RectTransform joystickVisualRoot;
        [SerializeField] private RectTransform joystickKnob;
        [SerializeField] private Canvas targetCanvas;

        [Header( "설정값" )]
        [SerializeField] private float joystickRadiusPixels = 110.0f;

        private int activeFingerId = -1;
        private Vector2 touchStartScreenPosition;

        ///<summary>
        /// 초기 표시 상태 설정
        ///</summary>
        private void Awake()
        {
            ResetInput();
        }

        ///<summary>
        /// 모바일 조이스틱 입력 갱신
        ///</summary>
        private void Update()
        {
            if ( Application.isMobilePlatform == false )
            {
                return;
            }

            if ( activeFingerId < 0 )
            {
                TryBeginJoystickTouch();
                return;
            }

            UpdateActiveJoystickTouch();
        }

        ///<summary>
        /// 비활성화 시 입력 초기화
        ///</summary>
        private void OnDisable()
        {
            ResetInput();
        }

        ///<summary>
        /// 조이스틱 입력 초기화
        ///</summary>
        public void ResetInput()
        {
            activeFingerId = -1;
            touchStartScreenPosition = Vector2.zero;
            SetJoystickVisualActive( false );
            SetJoystickKnobPosition( Vector2.zero );
            ApplyHorizontalInput( 0.0f );
        }

        ///<summary>
        /// 좌측 비 UI 터치에서 조이스틱 시작 시도
        ///</summary>
        private void TryBeginJoystickTouch()
        {
            for ( int index = 0; index < Input.touchCount; index++ )
            {
                Touch touch = Input.GetTouch( index );

                if ( touch.phase != TouchPhase.Began || touch.position.x >= Screen.width * 0.5f )
                {
                    continue;
                }

                bool isPointerOverUi = IsPointerOverUi( touch.fingerId );

                if ( isPointerOverUi )
                {
                    continue;
                }

                activeFingerId = touch.fingerId;
                touchStartScreenPosition = touch.position;
                SetJoystickVisualPosition( touchStartScreenPosition );
                SetJoystickVisualActive( true );
                ApplyHorizontalInput( 0.0f );
                return;
            }
        }

        ///<summary>
        /// 활성 조이스틱 터치 갱신
        ///</summary>
        private void UpdateActiveJoystickTouch()
        {
            bool isTouchFound = false;

            for ( int index = 0; index < Input.touchCount; index++ )
            {
                Touch touch = Input.GetTouch( index );

                if ( touch.fingerId != activeFingerId )
                {
                    continue;
                }

                isTouchFound = true;

                if ( touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled )
                {
                    ResetInput();
                    return;
                }

                Vector2 touchDelta = touch.position - touchStartScreenPosition;
                float resolvedRadius = Mathf.Max( 1.0f, joystickRadiusPixels );
                Vector2 clampedDelta = Vector2.ClampMagnitude( touchDelta, resolvedRadius );
                float horizontalInput = Mathf.Clamp( clampedDelta.x / resolvedRadius, -1.0f, 1.0f );
                SetJoystickKnobPosition( clampedDelta );
                ApplyHorizontalInput( horizontalInput );
                return;
            }

            if ( isTouchFound == false )
            {
                ResetInput();
            }
        }

        ///<summary>
        /// UI 터치 점유 여부 반환
        ///</summary>
        private bool IsPointerOverUi( int _fingerId )
        {
            EventSystem eventSystem = EventSystem.current;
            bool result = eventSystem != null && eventSystem.IsPointerOverGameObject( _fingerId );
            return result;
        }

        ///<summary>
        /// 조이스틱 시각 위치 설정
        ///</summary>
        private void SetJoystickVisualPosition( Vector2 _screenPosition )
        {
            if ( joystickVisualRoot == null || targetCanvas == null )
            {
                return;
            }

            RectTransform parentRectTransform = joystickVisualRoot.parent as RectTransform;

            if ( parentRectTransform == null )
            {
                return;
            }

            Camera eventCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle( parentRectTransform, _screenPosition, eventCamera, out Vector2 localPosition );

            if ( isConverted == false )
            {
                return;
            }

            joystickVisualRoot.anchoredPosition = localPosition;
        }

        ///<summary>
        /// 조이스틱 시각 표시 여부 설정
        ///</summary>
        private void SetJoystickVisualActive( bool _isActive )
        {
            if ( joystickVisualRoot == null )
            {
                return;
            }

            joystickVisualRoot.gameObject.SetActive( _isActive );
        }

        ///<summary>
        /// 조이스틱 노브 위치 설정
        ///</summary>
        private void SetJoystickKnobPosition( Vector2 _anchoredPosition )
        {
            if ( joystickKnob == null )
            {
                return;
            }

            joystickKnob.anchoredPosition = _anchoredPosition;
        }

        ///<summary>
        /// 수평 입력 적용
        ///</summary>
        private void ApplyHorizontalInput( float _horizontalInput )
        {
            bool hasInputManager = CInputManager.TryGetExistingInstance( out CInputManager inputManager );

            if ( hasInputManager == false || inputManager == null )
            {
                return;
            }

            inputManager.SetMobileHorizontalInput( _horizontalInput );
        }
    }
}
