using System;
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
        [Header( "참조" )]
        [SerializeField] private RectTransform popupRootRectTransform;
        [SerializeField] private RectTransform windowRootRectTransform;
        [SerializeField] private RectTransform windowDragHandleRectTransform;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Canvas targetCanvas;

        private void Awake()
        {
            ResolveReferences();
            EnsurePopupWindowInteraction( windowRootRectTransform, windowDragHandleRectTransform, targetCanvas, popupRootRectTransform );
            SetProgress( 0L, 0L );
            SetLayerVisible( false );
        }

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
                descriptionText.text = "업데이트 파일을 다운로드하고 있습니다.";
            }

            SetProgress( 0L, _totalBytes );
            SetLayerVisible( true );
            BringLayerToFront();
        }

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

        public void Hide()
        {
            SetLayerVisible( false );
        }

        public override bool CanCloseByEscape()
        {
            return false;
        }

        public override void CloseNavigationLayer()
        {
        }

        public override void SetLayerVisible( bool _isVisible )
        {
            if ( gameObject.activeSelf == _isVisible )
            {
                return;
            }

            gameObject.SetActive( _isVisible );
        }

        public override void BringLayerToFront()
        {
            BringPopupWindowToFront( popupRootRectTransform );
        }

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
