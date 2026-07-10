using System;
using TMPro;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 공용 입력 팝업 제어 컴포넌트
    ///</summary>
    public sealed class CPopupCommonInputField : CUIPopup
    {
        [Header( "참조" )]
        [SerializeField] private RectTransform popupRootRectTransform;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text positiveButtonText;
        [SerializeField] private CButtonEx positiveButton;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Canvas targetCanvas;

        private Action<string> submitAction;
        private Action closeAction;

        ///<summary>
        /// 팝업 초기 참조 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            SetLayerVisible( false );
        }

        ///<summary>
        /// 버튼 이벤트 구독 처리
        ///</summary>
        private void OnEnable()
        {
            BindEvents();
        }

        ///<summary>
        /// 버튼 이벤트 구독 해제 처리
        ///</summary>
        private void OnDisable()
        {
            UnbindEvents();
        }

        ///<summary>
        /// 팝업 내용 표시 처리
        ///</summary>
        public void Show( string _descriptionText, string _initialText, string _placeholderText, string _positiveButtonText, Action<string> _submitAction, Action _closeAction )
        {
            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController != null )
            {
                navigationController.RegisterPopup( this );
            }

            submitAction = _submitAction;
            closeAction = _closeAction;

            if ( descriptionText != null )
            {
                string resolvedDescriptionText = string.IsNullOrWhiteSpace( _descriptionText ) ? string.Empty : _descriptionText;
                descriptionText.text = resolvedDescriptionText;
            }

            if ( positiveButtonText != null )
            {
                string resolvedPositiveButtonText = string.IsNullOrWhiteSpace( _positiveButtonText ) ? "확인" : _positiveButtonText;
                positiveButtonText.text = resolvedPositiveButtonText;
            }

            if ( inputField != null )
            {
                inputField.text = string.IsNullOrWhiteSpace( _initialText ) ? string.Empty : _initialText.Trim();
                inputField.ActivateInputField();
                inputField.Select();
                ApplyPlaceholderText( _placeholderText );
            }

            SetLayerVisible( true );
            BringLayerToFront();
        }

        ///<summary>
        /// 네비게이션 레이어 표시 상태 반영
        ///</summary>
        public override void SetLayerVisible( bool _isVisible )
        {
            if ( gameObject.activeSelf == _isVisible )
            {
                return;
            }

            gameObject.SetActive( _isVisible );
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
            HideInternal( false );
        }

        ///<summary>
        /// 입력값 확정 처리
        ///</summary>
        private void HandlePositiveButtonClicked()
        {
            string submittedText = inputField != null ? inputField.text : string.Empty;
            Action<string> invokedSubmitAction = submitAction;
            HideInternal( true );

            if ( invokedSubmitAction == null )
            {
                return;
            }

            invokedSubmitAction( submittedText );
        }

        ///<summary>
        /// 닫기 버튼 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            HideInternal( false );
        }

        ///<summary>
        /// 입력 완료 이벤트 처리
        ///</summary>
        private void HandleInputSubmitted( string _submittedText )
        {
            HandlePositiveButtonClicked();
        }

        ///<summary>
        /// 이벤트 연결 처리
        ///</summary>
        private void BindEvents()
        {
            UnbindEvents();

            if ( positiveButton != null )
            {
                positiveButton.onClick.AddListener( HandlePositiveButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }

            if ( inputField != null )
            {
                inputField.onSubmit.AddListener( HandleInputSubmitted );
            }
        }

        ///<summary>
        /// 이벤트 해제 처리
        ///</summary>
        private void UnbindEvents()
        {
            if ( positiveButton != null )
            {
                positiveButton.onClick.RemoveListener( HandlePositiveButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }

            if ( inputField != null )
            {
                inputField.onSubmit.RemoveListener( HandleInputSubmitted );
            }
        }

        ///<summary>
        /// 팝업 비표시 내부 처리
        ///</summary>
        private void HideInternal( bool _isSubmitted )
        {
            Action invokedCloseAction = closeAction;
            submitAction = null;
            closeAction = null;
            SetLayerVisible( false );

            if ( _isSubmitted || invokedCloseAction == null )
            {
                return;
            }

            invokedCloseAction();
        }

        ///<summary>
        /// 플레이스홀더 텍스트 반영
        ///</summary>
        private void ApplyPlaceholderText( string _placeholderText )
        {
            if ( inputField == null )
            {
                return;
            }

            TMP_Text placeholderText = inputField.placeholder as TMP_Text;

            if ( placeholderText == null )
            {
                return;
            }

            string resolvedPlaceholderText = string.IsNullOrWhiteSpace( _placeholderText ) ? string.Empty : _placeholderText;
            placeholderText.text = resolvedPlaceholderText;
        }

        ///<summary>
        /// 창 드래그 핸들 구성 보장
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            EnsurePopupWindowDragHandle( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 최상단 핸들 구성 보장
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            EnsurePopupWindowFocusHandlers( windowRootRectTransform, popupRootRectTransform );
        }

        ///<summary>
        /// 네비게이션 레이어 최상단 정렬
        ///</summary>
        public override void BringLayerToFront()
        {
            BringPopupWindowToFront( popupRootRectTransform );
        }

        ///<summary>
        /// 팝업 참조 요소 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( popupRootRectTransform == null )
            {
                RectTransform resolvedPopupRootRectTransform = transform as RectTransform;
                popupRootRectTransform = resolvedPopupRootRectTransform;
            }

            if ( inputField == null )
            {
                TMP_InputField resolvedInputField = GetComponentInChildren<TMP_InputField>( true );
                inputField = resolvedInputField;
            }

            if ( targetCanvas == null )
            {
                Canvas resolvedTargetCanvas = GetComponentInParent<Canvas>();
                targetCanvas = resolvedTargetCanvas;
            }
        }
    }
}
