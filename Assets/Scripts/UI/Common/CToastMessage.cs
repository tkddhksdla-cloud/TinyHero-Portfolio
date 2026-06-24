using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 토스트 메시지 표시 컴포넌트
    ///</summary>
    public sealed class CToastMessage : CAutoPoolReturnObject
    {
        private const float ToastReturnDelaySeconds = 5.0f;

        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private LayoutElement layoutElement;

        private RectTransform cachedRectTransform;

        ///<summary>
        /// 토스트 메시지 초기 참조 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            ApplyLayoutSize();
            SetReturnDelay( ToastReturnDelaySeconds );
        }

        ///<summary>
        /// 토스트 메시지 활성 상태 초기화
        ///</summary>
        protected override void OnAutoReturnObjectEnabled()
        {
            ResolveReferences();
            ApplyLayoutSize();
            ResetVisualState();
        }

        ///<summary>
        /// 토스트 메시지 내용 반영
        ///</summary>
        public void ShowMessage( string _message )
        {
            ResolveReferences();
            ResetVisualState();

            if ( messageText == null )
            {
                return;
            }

            string resolvedMessage = string.IsNullOrWhiteSpace( _message ) ? string.Empty : _message;
            messageText.text = resolvedMessage;
        }

        ///<summary>
        /// 토스트 메시지 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( messageText == null )
            {
                TMP_Text resolvedMessageText = GetComponentInChildren<TMP_Text>( true );
                messageText = resolvedMessageText;
            }

            if ( canvasGroup == null )
            {
                CanvasGroup resolvedCanvasGroup = GetComponent<CanvasGroup>();
                canvasGroup = resolvedCanvasGroup;
            }

            if ( targetAnimator == null )
            {
                Animator resolvedAnimator = GetComponent<Animator>();
                targetAnimator = resolvedAnimator;
            }

            if ( layoutElement == null )
            {
                LayoutElement resolvedLayoutElement = GetComponent<LayoutElement>();

                if ( resolvedLayoutElement == null )
                {
                    resolvedLayoutElement = gameObject.AddComponent<LayoutElement>();
                }

                layoutElement = resolvedLayoutElement;
            }

            if ( cachedRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                cachedRectTransform = resolvedRectTransform;
            }
        }

        ///<summary>
        /// 토스트 메시지 표시 상태 초기화
        ///</summary>
        private void ResetVisualState()
        {
            if ( canvasGroup != null )
            {
                canvasGroup.alpha = 1.0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if ( targetAnimator == null )
            {
                return;
            }

            targetAnimator.Rebind();
            targetAnimator.Update( 0.0f );
        }

        ///<summary>
        /// 토스트 메시지 레이아웃 크기 반영
        ///</summary>
        private void ApplyLayoutSize()
        {
            if ( layoutElement == null || cachedRectTransform == null )
            {
                return;
            }

            float preferredWidth = cachedRectTransform.sizeDelta.x;
            float preferredHeight = cachedRectTransform.sizeDelta.y;

            if ( preferredWidth <= 0.0f )
            {
                Rect rect = cachedRectTransform.rect;
                preferredWidth = rect.width;
            }

            if ( preferredHeight <= 0.0f )
            {
                Rect rect = cachedRectTransform.rect;
                preferredHeight = rect.height;
            }

            layoutElement.minWidth = preferredWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = 0.0f;
            layoutElement.minHeight = preferredHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0.0f;
        }
    }
}
