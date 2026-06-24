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
        private const string PopupRootPath = "Popup";
        private const string BackgroundPath = "Popup/BG";
        private const string DescriptionPath = "Popup/BG/Desc";
        private const string PositiveButtonPath = "Popup/BG/ButtonArea/ButtonInteraction_Positive";
        private const string PositiveButtonTextPath = "Popup/BG/ButtonArea/ButtonInteraction_Positive/Button/ButtonText";
        private const string NegativeButtonPath = "Popup/BG/ButtonArea/ButtonInteraction_Negative";
        private const string NegativeButtonTextPath = "Popup/BG/ButtonArea/ButtonInteraction_Negative/Button/ButtonText";
        private const string CloseButtonPath = "Popup/ButtonClose";
        private const string DragHandlePath = "Popup/BG/WindowDragHandle";

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
        public void Show( string _descriptionText, string _positiveButtonText, Action _positiveButtonAction, string _negativeButtonText, Action _negativeButtonAction )
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
            HideInternal();
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
            SetLayerVisible( false );
        }

        ///<summary>
        /// 창 드래그 핸들 구성 보장
        ///</summary>
        private void EnsureWindowDragHandle()
        {
            if ( windowDragHandleRectTransform == null )
            {
                return;
            }

            CItemInventoryWindowDragHandle dragHandle = windowDragHandleRectTransform.GetComponent<CItemInventoryWindowDragHandle>();

            if ( dragHandle == null )
            {
                dragHandle = windowDragHandleRectTransform.gameObject.AddComponent<CItemInventoryWindowDragHandle>();
            }

            dragHandle.Configure( windowRootRectTransform, targetCanvas );
        }

        ///<summary>
        /// 창 최상단 핸들 구성 보장
        ///</summary>
        private void EnsureWindowFocusHandlers()
        {
            if ( windowRootRectTransform == null || popupRootRectTransform == null )
            {
                return;
            }

            Graphic[] graphicArray = windowRootRectTransform.GetComponentsInChildren<Graphic>( true );
            int graphicCount = graphicArray.Length;

            for ( int index = 0; index < graphicCount; index++ )
            {
                Graphic targetGraphic = graphicArray[ index ];

                if ( targetGraphic == null || targetGraphic.raycastTarget == false )
                {
                    continue;
                }

                CWindowDragHandle focusHandler = targetGraphic.GetComponent<CWindowDragHandle>();

                if ( focusHandler == null )
                {
                    focusHandler = targetGraphic.gameObject.AddComponent<CWindowDragHandle>();
                }

                focusHandler.Configure( popupRootRectTransform );
            }
        }

        ///<summary>
        /// 네비게이션 레이어 최상단 정렬
        ///</summary>
        public override void BringLayerToFront()
        {
            if ( popupRootRectTransform == null )
            {
                return;
            }

            popupRootRectTransform.SetAsLastSibling();
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

            if ( windowRootRectTransform == null )
            {
                Transform popupTransform = transform.Find( PopupRootPath );
                RectTransform resolvedWindowRootRectTransform = popupTransform as RectTransform;
                windowRootRectTransform = resolvedWindowRootRectTransform;
            }

            if ( descriptionText == null )
            {
                Transform descriptionTransform = transform.Find( DescriptionPath );
                TMP_Text resolvedDescriptionText = descriptionTransform != null ? descriptionTransform.GetComponent<TMP_Text>() : null;
                descriptionText = resolvedDescriptionText;
            }

            if ( positiveButton == null )
            {
                Transform positiveButtonTransform = transform.Find( PositiveButtonPath );
                CButtonEx resolvedPositiveButton = positiveButtonTransform != null ? positiveButtonTransform.GetComponent<CButtonEx>() : null;
                positiveButton = resolvedPositiveButton;
            }

            if ( positiveButtonText == null )
            {
                Transform positiveButtonTextTransform = transform.Find( PositiveButtonTextPath );
                TMP_Text resolvedPositiveButtonText = positiveButtonTextTransform != null ? positiveButtonTextTransform.GetComponent<TMP_Text>() : null;
                positiveButtonText = resolvedPositiveButtonText;
            }

            if ( negativeButton == null )
            {
                Transform negativeButtonTransform = transform.Find( NegativeButtonPath );
                CButtonEx resolvedNegativeButton = negativeButtonTransform != null ? negativeButtonTransform.GetComponent<CButtonEx>() : null;
                negativeButton = resolvedNegativeButton;
            }

            if ( negativeButtonText == null )
            {
                Transform negativeButtonTextTransform = transform.Find( NegativeButtonTextPath );
                TMP_Text resolvedNegativeButtonText = negativeButtonTextTransform != null ? negativeButtonTextTransform.GetComponent<TMP_Text>() : null;
                negativeButtonText = resolvedNegativeButtonText;
            }

            if ( closeButton == null )
            {
                Transform closeButtonTransform = transform.Find( CloseButtonPath );
                CButtonEx resolvedCloseButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<CButtonEx>() : null;
                closeButton = resolvedCloseButton;
            }

            if ( windowDragHandleRectTransform == null )
            {
                Transform dragHandleTransform = transform.Find( DragHandlePath );
                RectTransform resolvedDragHandleRectTransform = dragHandleTransform as RectTransform;
                windowDragHandleRectTransform = resolvedDragHandleRectTransform;
            }

            if ( targetCanvas == null )
            {
                Canvas resolvedTargetCanvas = GetComponentInParent<Canvas>();
                targetCanvas = resolvedTargetCanvas;
            }

            if ( windowRootRectTransform == null )
            {
                Transform backgroundTransform = transform.Find( BackgroundPath );
                RectTransform resolvedBackgroundRectTransform = backgroundTransform as RectTransform;
                windowRootRectTransform = resolvedBackgroundRectTransform;
            }
        }
    }
}
