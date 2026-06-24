using TinyHero.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 저장 버튼 호출 브리지 컴포넌트
    ///</summary>
    public sealed class CSaveButtonBridge : MonoBehaviour
    {
        private const float ClickDebounceDuration = 0.1f;

        [SerializeField] private CButtonEx targetButton;
        [SerializeField] private Image raycastImage;

        private RectTransform targetRectTransform;
        private bool isPointerPressedInside;
        private float lastClickTime = -10.0f;

        ///<summary>
        /// 버튼 참조 초기 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureRaycastGraphic();
        }

        ///<summary>
        /// 버튼 이벤트 구독 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            EnsureRaycastGraphic();

            if ( targetButton == null )
            {
                Debug.LogWarning( "[ SaveDebug ] Save button bridge could not resolve target button.", this );
                return;
            }

            targetButton.onClick.RemoveListener( HandleSaveButtonClicked );
            targetButton.onClick.AddListener( HandleSaveButtonClicked );
            Debug.Log( $"[ SaveDebug ] Save button bridge bound. Object: {gameObject.name}", this );
        }

        ///<summary>
        /// 버튼 이벤트 구독 해제 처리
        ///</summary>
        private void OnDisable()
        {
            if ( targetButton == null )
            {
                return;
            }

            targetButton.onClick.RemoveListener( HandleSaveButtonClicked );
        }

        ///<summary>
        /// 저장 버튼 직접 입력 폴백 처리
        ///</summary>
        private void Update()
        {
            if ( targetButton == null || targetButton.interactable == false || targetButton.gameObject.activeInHierarchy == false )
            {
                isPointerPressedInside = false;
                return;
            }

            bool isMouseButtonDown = Input.GetMouseButtonDown( 0 );

            if ( isMouseButtonDown )
            {
                bool isPointerInsideOnDown = IsPointerInsideButton( Input.mousePosition );
                isPointerPressedInside = isPointerInsideOnDown;
            }

            bool isMouseButtonUp = Input.GetMouseButtonUp( 0 );

            if ( isMouseButtonUp == false )
            {
                return;
            }

            bool wasPointerPressedInside = isPointerPressedInside;
            isPointerPressedInside = false;

            if ( wasPointerPressedInside == false )
            {
                return;
            }

            bool isPointerInsideOnUp = IsPointerInsideButton( Input.mousePosition );

            if ( isPointerInsideOnUp == false )
            {
                return;
            }

            TryInvokeSave();
        }

        ///<summary>
        /// 저장 버튼 입력 처리
        ///</summary>
        private void HandleSaveButtonClicked()
        {
            TryInvokeSave();
        }

        ///<summary>
        /// 저장 버튼 클릭 처리 단일 진입점
        ///</summary>
        private void TryInvokeSave()
        {
            float currentTime = Time.unscaledTime;

            if ( currentTime - lastClickTime < ClickDebounceDuration )
            {
                return;
            }

            lastClickTime = currentTime;
            Debug.Log( $"[ SaveDebug ] Save button bridge clicked. Object: {gameObject.name}", this );
            CSaveManager saveManager = CSaveManager.Instance;

            if ( saveManager == null )
            {
                Debug.LogError( "[ SaveDebug ] Save manager instance was null on save button click.", this );
                return;
            }

            saveManager.RequestSaveWithPopup();
        }

        ///<summary>
        /// 버튼 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetButton == null )
            {
                CButtonEx resolvedButton = GetComponent<CButtonEx>();
                targetButton = resolvedButton;
            }

            if ( targetRectTransform == null )
            {
                RectTransform resolvedRectTransform = GetComponent<RectTransform>();
                targetRectTransform = resolvedRectTransform;
            }
        }

        ///<summary>
        /// 저장 버튼 레이캐스트 그래픽 보장
        ///</summary>
        private void EnsureRaycastGraphic()
        {
            if ( raycastImage == null )
            {
                Image resolvedRaycastImage = GetComponent<Image>();
                raycastImage = resolvedRaycastImage;
            }

            if ( raycastImage == null )
            {
                Image createdRaycastImage = gameObject.AddComponent<Image>();
                raycastImage = createdRaycastImage;
            }

            if ( raycastImage == null )
            {
                return;
            }

            raycastImage.raycastTarget = true;
            raycastImage.maskable = false;
            raycastImage.color = new Color( 1.0f, 1.0f, 1.0f, 0.0f );
        }

        ///<summary>
        /// 저장 버튼 영역 포함 여부 판정
        ///</summary>
        private bool IsPointerInsideButton( Vector2 _screenPosition )
        {
            if ( targetRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                targetRectTransform = resolvedRectTransform;
            }

            if ( targetRectTransform == null )
            {
                return false;
            }

            Canvas rootCanvas = targetRectTransform.GetComponentInParent<Canvas>();
            Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            bool isInside = RectTransformUtility.RectangleContainsScreenPoint( targetRectTransform, _screenPosition, eventCamera );
            return isInside;
        }
    }
}
