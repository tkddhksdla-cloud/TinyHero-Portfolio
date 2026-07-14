using System;
using TinyHero.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 원격 콘텐츠 다운로드 진행 팝업
    ///</summary>
    public sealed class PopupContentDownload : CUIPopup
    {
        private const string DownloadInProgressTextKey = "KEY_TEXT_UI_CONTENT_DOWNLOAD_IN_PROGRESS";
        private const string ContentVerifyingTextKey = "KEY_TEXT_UI_CONTENT_DOWNLOAD_VERIFYING";
        private const string ContentVerifyingStatusTextKey = "KEY_TEXT_UI_CONTENT_DOWNLOAD_VERIFYING_STATUS";
        private const string DownloadCompletedTextKey = "KEY_TEXT_UI_CONTENT_DOWNLOAD_COMPLETED";
        private const string DownloadCompletedStatusTextKey = "KEY_TEXT_UI_CONTENT_DOWNLOAD_COMPLETED_STATUS";
        private const string ConfirmButtonTextKey = "KEY_TEXT_UI_COMMON_CONFIRM";

        [Header( "참조" )]
        [SerializeField] private RectTransform popupRootRectTransform;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private GameObject buttonAreaObject;
        [SerializeField] private CButtonEx interactionButton;
        [SerializeField] private TMP_Text interactionButtonText;
        [SerializeField] private Canvas targetCanvas;

        ///<summary>
        /// 팝업 초기 참조 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            EnsurePopupWindowInteraction( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas, popupRootRectTransform );
            SetProgress( 0L, 0L );
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
        /// 다운로드 진행 팝업 표시
        ///</summary>
        public void Show( long _totalBytes )
        {
            ResolveReferences();
            EnsurePopupWindowInteraction( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas, popupRootRectTransform );
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController != null )
            {
                navigationController.RegisterPopup( this );
            }

            if ( descriptionText != null )
            {
                descriptionText.text = CDataManager.GetText( DownloadInProgressTextKey );
            }

            if ( buttonAreaObject != null )
            {
                buttonAreaObject.SetActive( false );
            }

            SetProgress( 0L, _totalBytes );
            SetLayerVisible( true );
            BringLayerToFront();
        }

        ///<summary>
        /// 다운로드 진행률 표시 갱신
        ///</summary>
        public void SetProgress( long _downloadedBytes, long _totalBytes )
        {
            long totalBytes = Math.Max( 0L, _totalBytes );
            long downloadedBytes = Math.Min( Math.Max( 0L, _downloadedBytes ), totalBytes );
            float progress = totalBytes > 0L ? ( float ) downloadedBytes / totalBytes : 0.0f;

            if ( progressFillImage != null )
            {
                progressFillImage.fillAmount = Mathf.Clamp01( progress );
            }

            if ( progressText != null )
            {
                int progressPercent = Mathf.RoundToInt( progress * 100.0f );
                progressText.text = $"{FormatBytes( downloadedBytes )} / {FormatBytes( totalBytes )}  ({progressPercent}%)";
            }
        }

        ///<summary>
        /// 다운로드 콘텐츠 검증 상태 표시
        ///</summary>
        public void SetVerifying()
        {
            if ( descriptionText != null )
            {
                descriptionText.text = CDataManager.GetText( ContentVerifyingTextKey );
            }

            if ( progressFillImage != null )
            {
                progressFillImage.fillAmount = 1.0f;
            }

            if ( progressText != null )
            {
                progressText.text = CDataManager.GetText( ContentVerifyingStatusTextKey );
            }
        }

        ///<summary>
        /// 다운로드 완료 상태 표시
        ///</summary>
        public void SetCompleted()
        {
            if ( descriptionText != null )
            {
                descriptionText.text = CDataManager.GetText( DownloadCompletedTextKey );
            }

            if ( progressFillImage != null )
            {
                progressFillImage.fillAmount = 1.0f;
            }

            if ( progressText != null )
            {
                progressText.text = CDataManager.GetText( DownloadCompletedStatusTextKey );
            }

            if ( interactionButtonText != null )
            {
                interactionButtonText.text = CDataManager.GetText( ConfirmButtonTextKey );
            }

            if ( buttonAreaObject != null )
            {
                buttonAreaObject.SetActive( true );
            }
        }

        ///<summary>
        /// 다운로드 팝업 비표시 처리
        ///</summary>
        public void Hide()
        {
            SetLayerVisible( false );
        }

        ///<summary>
        /// ESC 닫기 허용 여부 반환
        ///</summary>
        public override bool CanCloseByEscape()
        {
            return false;
        }

        ///<summary>
        /// 네비게이션 레이어 닫기 차단 처리
        ///</summary>
        public override void CloseNavigationLayer()
        {
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
        /// 네비게이션 레이어 최상단 정렬
        ///</summary>
        public override void BringLayerToFront()
        {
            BringPopupWindowToFront( popupRootRectTransform );
        }

        ///<summary>
        /// 확인 버튼 선택 처리
        ///</summary>
        private void HandleInteractionButtonClicked()
        {
            Hide();
        }

        ///<summary>
        /// 버튼 이벤트 연결 처리
        ///</summary>
        private void BindButtonEvents()
        {
            UnbindButtonEvents();

            if ( interactionButton != null )
            {
                interactionButton.onClick.AddListener( HandleInteractionButtonClicked );
            }
        }

        ///<summary>
        /// 버튼 이벤트 해제 처리
        ///</summary>
        private void UnbindButtonEvents()
        {
            if ( interactionButton != null )
            {
                interactionButton.onClick.RemoveListener( HandleInteractionButtonClicked );
            }
        }

        ///<summary>
        /// 팝업 참조 요소 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( popupRootRectTransform == null )
            {
                popupRootRectTransform = transform as RectTransform;
            }

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        ///<summary>
        /// 바이트 크기 표시 문자열 변환
        ///</summary>
        private static string FormatBytes( long _byteCount )
        {
            const float Kilobyte = 1024.0f;
            const float Megabyte = Kilobyte * 1024.0f;
            const float Gigabyte = Megabyte * 1024.0f;
            float byteCount = Mathf.Max( 0.0f, _byteCount );

            if ( byteCount >= Gigabyte )
            {
                return $"{byteCount / Gigabyte:0.00} GB";
            }

            if ( byteCount >= Megabyte )
            {
                return $"{byteCount / Megabyte:0.00} MB";
            }

            if ( byteCount >= Kilobyte )
            {
                return $"{byteCount / Kilobyte:0.00} KB";
            }

            return $"{_byteCount} B";
        }
    }
}
