using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 공용 안내 팝업 제어 컴포넌트
    ///</summary>
    public sealed class CPopupCommonNotice : CUIPopup
    {
        [SerializeField] private RectTransform popupRootRectTransform;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text positiveButtonText;
        [SerializeField] private TMP_Text negativeButtonText;
        [SerializeField] private CButtonEx positiveButton;
        [SerializeField] private CButtonEx negativeButton;
        [SerializeField] private CButtonEx closeButton;
        [SerializeField] private Canvas targetCanvas;

        private Action positiveButtonAction;
        private Action negativeButtonAction;
        private bool isCloseBlocked;

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
            BindButtonEvents();
        }

        ///<summary>
        /// 버튼 이벤트 구독 해제 처리
        ///</summary>
        private void OnDisable()
        {
            UnbindButtonEvents();
        }

        ///<summary>
        /// 팝업 내용 표시 처리
        ///</summary>
        public void Show( string _descriptionText, string _positiveButtonText, Action _positiveButtonAction, string _negativeButtonText, Action _negativeButtonAction, bool _isCloseBlocked = false )
        {
            ResolveReferences();
            EnsureWindowDragHandle();
            EnsureWindowFocusHandlers();
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController != null )
            {
                navigationController.RegisterPopup( this );
            }

            positiveButtonAction = _positiveButtonAction;
            negativeButtonAction = _negativeButtonAction;
            isCloseBlocked = _isCloseBlocked;

            if ( descriptionText != null )
            {
                string resolvedDescriptionText = string.IsNullOrWhiteSpace( _descriptionText ) ? string.Empty : _descriptionText;
                descriptionText.text = resolvedDescriptionText;
            }

            bool shouldShowPositiveButton = string.IsNullOrWhiteSpace( _positiveButtonText ) == false;

            if ( positiveButton != null )
            {
                positiveButton.gameObject.SetActive( shouldShowPositiveButton );
            }

            if ( positiveButtonText != null )
            {
                string resolvedPositiveButtonText = string.IsNullOrWhiteSpace( _positiveButtonText ) ? string.Empty : _positiveButtonText;
                positiveButtonText.text = resolvedPositiveButtonText;
            }

            bool shouldShowNegativeButton = string.IsNullOrWhiteSpace( _negativeButtonText ) == false;

            if ( negativeButton != null )
            {
                negativeButton.gameObject.SetActive( shouldShowNegativeButton );
            }

            if ( negativeButtonText != null )
            {
                string resolvedNegativeButtonText = string.IsNullOrWhiteSpace( _negativeButtonText ) ? string.Empty : _negativeButtonText;
                negativeButtonText.text = resolvedNegativeButtonText;
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
            if ( isCloseBlocked )
            {
                return;
            }

            HideInternal();
        }

        public override bool CanCloseByEscape()
        {
            if ( isCloseBlocked )
            {
                return false;
            }

            bool result = base.CanCloseByEscape();
            return result;
        }

        ///<summary>
        /// 긍정 버튼 선택 처리
        ///</summary>
        private void HandlePositiveButtonClicked()
        {
            Action invokedAction = positiveButtonAction;
            HideInternal();

            if ( invokedAction == null )
            {
                return;
            }

            invokedAction();
        }

        ///<summary>
        /// 부정 버튼 선택 처리
        ///</summary>
        private void HandleNegativeButtonClicked()
        {
            Action invokedAction = negativeButtonAction;
            HideInternal();

            if ( invokedAction == null )
            {
                return;
            }

            invokedAction();
        }

        ///<summary>
        /// 닫기 버튼 처리
        ///</summary>
        private void HandleCloseButtonClicked()
        {
            if ( isCloseBlocked )
            {
                return;
            }

            HideInternal();
        }

        ///<summary>
        /// 버튼 이벤트 연결 처리
        ///</summary>
        private void BindButtonEvents()
        {
            UnbindButtonEvents();

            if ( positiveButton != null )
            {
                positiveButton.onClick.AddListener( HandlePositiveButtonClicked );
            }

            if ( negativeButton != null )
            {
                negativeButton.onClick.AddListener( HandleNegativeButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.AddListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 버튼 이벤트 해제 처리
        ///</summary>
        private void UnbindButtonEvents()
        {
            if ( positiveButton != null )
            {
                positiveButton.onClick.RemoveListener( HandlePositiveButtonClicked );
            }

            if ( negativeButton != null )
            {
                negativeButton.onClick.RemoveListener( HandleNegativeButtonClicked );
            }

            if ( closeButton != null )
            {
                closeButton.onClick.RemoveListener( HandleCloseButtonClicked );
            }
        }

        ///<summary>
        /// 팝업 비표시 내부 처리
        ///</summary>
        private void HideInternal()
        {
            positiveButtonAction = null;
            negativeButtonAction = null;
            isCloseBlocked = false;
            SetLayerVisible( false );
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

            if ( targetCanvas == null )
            {
                Canvas resolvedTargetCanvas = GetComponentInParent<Canvas>();
                targetCanvas = resolvedTargetCanvas;
            }
        }
    }
}
