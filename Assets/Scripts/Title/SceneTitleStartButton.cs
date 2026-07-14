using TinyHero.Maps;
using TinyHero.Player;
using TinyHero.UI;
using TinyHero.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyHero.Title
{
    ///<summary>
    /// 타이틀 시작 버튼 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class SceneTitleStartButton : MonoBehaviour
    {
        private const string GameplaySceneName = "SceneMap";
        private const string StarterMapId = "MAP_STARTER_000_VILLAGE";
        private const string FadeImageObjectName = "TitleFadeImage";
        private const float DefaultFadeDuration = 0.35f;
        private const float RemoteDataWaitTimeoutSeconds = 20.0f;
        private const string LoadSaveDescriptionText = "저장된 정보를 불러오시겠습니까?";
        private const string NicknameInputDescriptionText = "닉네임을 입력하세요.";
        private const string NicknameInputPlaceholderText = "닉네임";
        private const string NicknameConfirmButtonText = "확인";
        private const string PositiveButtonText = "예";
        private const string NegativeButtonText = "아니오";
        private const string DownloadConfirmButtonText = "다운로드";
        private const string DownloadCancelButtonText = "취소";

        [SerializeField] private CButtonEx startButton;
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = DefaultFadeDuration;

        private bool isStarting;
        private bool shouldLoadSavedData;
        private bool isDownloadNoticeRequested;
        private bool isDownloadProgressPopupRequested;
        private PopupContentDownload contentDownloadPopup;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            if ( startButton == null )
            {
                CButtonEx resolvedButton = GetComponent<CButtonEx>();
                startButton = resolvedButton;
            }

            EnsureFadeImage();
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            if ( startButton == null )
            {
                return;
            }

            startButton.onClick.AddListener( HandleStartButtonClicked );
        }

        private void Start()
        {
            StartCoroutine( IE_MonitorRemoteContentDownload() );
        }

        ///<summary>
        /// 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            if ( startButton == null )
            {
                return;
            }

            startButton.onClick.RemoveListener( HandleStartButtonClicked );
        }

        private IEnumerator IE_MonitorRemoteContentDownload()
        {
            while ( true )
            {
                CResourceManager resourceManager = CResourceManager.Instance;

                if ( resourceManager == null )
                {
                    yield return null;
                    continue;
                }

                if ( resourceManager.IsRemoteContentDownloadConfirmationRequired() )
                {
                    ShowRemoteContentDownloadNotice( resourceManager );
                }

                if ( resourceManager.IsRemoteContentDownloading() )
                {
                    ShowRemoteContentDownloadProgress( resourceManager );

                    if ( contentDownloadPopup != null )
                    {
                        long downloadedBytes = resourceManager.GetRemoteContentDownloadedBytes();
                        long totalBytes = resourceManager.GetRemoteContentTotalDownloadBytes();
                        contentDownloadPopup.SetProgress( downloadedBytes, totalBytes );
                    }
                }
                else if ( resourceManager.IsRemoteContentVerifying() )
                {
                    if ( contentDownloadPopup != null )
                    {
                        contentDownloadPopup.SetVerifying();
                    }
                }
                else if ( resourceManager.IsRemoteDataReady() && resourceManager.HasRemoteDataLoadFailed() == false )
                {
                    if ( contentDownloadPopup != null )
                    {
                        contentDownloadPopup.SetCompleted();
                    }
                }
                else if ( contentDownloadPopup != null )
                {
                    contentDownloadPopup.Hide();
                    contentDownloadPopup = null;
                    isDownloadProgressPopupRequested = false;
                }

                if ( resourceManager.IsRemoteDataReady() )
                {
                    yield break;
                }

                yield return null;
            }
        }

        private void ShowRemoteContentDownloadNotice( CResourceManager _resourceManager )
        {
            if ( _resourceManager == null || isDownloadNoticeRequested )
            {
                return;
            }

            isDownloadNoticeRequested = true;
            long totalBytes = _resourceManager.GetRemoteContentTotalDownloadBytes();
            string formattedSize = FormatBytes( totalBytes );
            string descriptionText = $"새로운 업데이트 파일이 있습니다.\n다운로드 크기: {formattedSize}\n지금 다운로드하시겠습니까?";
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                isDownloadNoticeRequested = false;
                return;
            }

            navigationController.ShowCommonNotice(
                descriptionText,
                DownloadConfirmButtonText,
                HandleRemoteContentDownloadConfirmed,
                DownloadCancelButtonText,
                HandleRemoteContentDownloadRejected,
                true );
        }

        private void HandleRemoteContentDownloadConfirmed()
        {
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                return;
            }

            resourceManager.ConfirmRemoteContentDownload();
            ShowRemoteContentDownloadProgress( resourceManager );
        }

        private void HandleRemoteContentDownloadRejected()
        {
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager != null )
            {
                resourceManager.RejectRemoteContentDownload();
            }
        }

        private void ShowRemoteContentDownloadProgress( CResourceManager _resourceManager )
        {
            if ( _resourceManager == null || contentDownloadPopup != null || isDownloadProgressPopupRequested )
            {
                return;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return;
            }

            isDownloadProgressPopupRequested = true;
            long totalBytes = _resourceManager.GetRemoteContentTotalDownloadBytes();
            navigationController.ShowContentDownload( totalBytes, HandleContentDownloadPopupShown );
        }

        private void HandleContentDownloadPopupShown( PopupContentDownload _popup )
        {
            contentDownloadPopup = _popup;
            isDownloadProgressPopupRequested = false;
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

        ///<summary>
        /// 시작 버튼 클릭 처리
        ///</summary>
        private void HandleStartButtonClicked()
        {
            if ( isStarting )
            {
                return;
            }

            Debug.Log( "[ SaveDebug ] SceneTitle start button clicked.", this );
            CSaveManager saveManager = CSaveManager.Instance;
            bool hasSaveData = saveManager != null && saveManager.HasSaveData();
            Debug.Log( $"[ SaveDebug ] SceneTitle detected save data. HasSaveData: {hasSaveData}", this );

            if ( hasSaveData )
            {
                CUINavigationController navigationController = CUINavigationController.Instance;
                bool isPopupShown = navigationController != null && navigationController.ShowCommonNotice( LoadSaveDescriptionText, PositiveButtonText, HandlePositiveLoadSelected, NegativeButtonText, HandleNegativeLoadSelected );

                if ( isPopupShown )
                {
                    return;
                }
            }

            HandleNegativeLoadSelected();
        }

        ///<summary>
        /// 저장 데이터 불러오기 선택 처리
        ///</summary>
        private void HandlePositiveLoadSelected()
        {
            Debug.Log( "[ SaveDebug ] SceneTitle selected positive load option.", this );
            shouldLoadSavedData = true;
            StartCoroutine( IE_StartGame() );
        }

        ///<summary>
        /// 새 게임 시작 선택 처리
        ///</summary>
        private void HandleNegativeLoadSelected()
        {
            Debug.Log( "[ SaveDebug ] SceneTitle selected negative load option.", this );
            shouldLoadSavedData = false;
            ShowNicknameInputPopup();
        }

        ///<summary>
        /// 닉네임 입력 팝업 표시
        ///</summary>
        private void ShowNicknameInputPopup()
        {
            isStarting = true;

            if ( startButton != null )
            {
                startButton.interactable = false;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;
            bool isPopupShown = navigationController != null && navigationController.ShowCommonInputField( NicknameInputDescriptionText, string.Empty, NicknameInputPlaceholderText, NicknameConfirmButtonText, HandleNicknameSubmitted, HandleNicknameInputClosed );

            if ( isPopupShown )
            {
                return;
            }

            HandleNicknameSubmitted( string.Empty );
        }

        ///<summary>
        /// 닉네임 입력 완료 처리
        ///</summary>
        private void HandleNicknameSubmitted( string _nickname )
        {
            CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;

            if ( playerProfileManager != null )
            {
                playerProfileManager.SetPlayerName( _nickname );
            }

            StartCoroutine( IE_StartGame() );
        }

        ///<summary>
        /// 닉네임 입력 닫기 처리
        ///</summary>
        private void HandleNicknameInputClosed()
        {
            isStarting = false;

            if ( startButton != null )
            {
                startButton.interactable = true;
            }
        }

        ///<summary>
        /// 시작 시퀀스 코루틴 처리
        ///</summary>
        private IEnumerator IE_StartGame()
        {
            isStarting = true;

            if ( startButton != null )
            {
                startButton.interactable = false;
            }

            yield return IE_WaitForRemoteDataReady();

            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager != null && resourceManager.HasRemoteDataLoadFailed() )
            {
                HandleRemoteDataLoadFailure( resourceManager.GetRemoteDataFailureReason() );
                yield break;
            }

            if ( resourceManager != null && resourceManager.IsRemoteDataReady() == false )
            {
                HandleRemoteDataLoadFailure( "원격 콘텐츠 준비 시간이 초과되었습니다." );
                yield break;
            }

            EnsureFadeImage();
            yield return IE_FadeAlpha( 0.0f, 1.0f );
            PreparePendingGameStart();
            CMapManager mapManager = CMapManager.Instance;

            if ( mapManager == null )
            {
                yield break;
            }

            SceneManager.LoadScene( GameplaySceneName );
        }

        ///<summary>
        /// 원격 정의 데이터 준비 대기
        ///</summary>
        private IEnumerator IE_WaitForRemoteDataReady()
        {
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null || resourceManager.IsRemoteDataReady() )
            {
                yield break;
            }

            float elapsedTime = 0.0f;

            while ( resourceManager.IsRemoteDataReady() == false && elapsedTime < RemoteDataWaitTimeoutSeconds )
            {
                bool isWaitingForDownload = resourceManager.IsRemoteContentDownloadConfirmationRequired() || resourceManager.IsRemoteContentDownloading();

                if ( isWaitingForDownload )
                {
                    elapsedTime = 0.0f;
                }
                else
                {
                    elapsedTime += Time.unscaledDeltaTime;
                }

                yield return null;
            }

            if ( resourceManager.IsRemoteDataReady() == false )
            {
                Debug.LogWarning( "[ Title ] Remote data wait timed out.", this );
            }
        }

        ///<summary>
        /// 원격 데이터 로드 실패 처리
        ///</summary>
        private void HandleRemoteDataLoadFailure( string _failureReason )
        {
            isStarting = false;

            if ( startButton != null )
            {
                startButton.interactable = true;
            }

            string message = string.IsNullOrWhiteSpace( _failureReason ) ? "필수 콘텐츠 업데이트에 실패했습니다. 게임을 다시 실행해 주세요." : _failureReason;
            CToastMessageSystem.Show( message );
            Debug.LogError( $"[ Title ] {message}", this );
        }

        ///<summary>
        /// 시작 직전 로드 대상 구성
        ///</summary>
        private void PreparePendingGameStart()
        {
            Debug.Log( $"[ SaveDebug ] PreparePendingGameStart invoked. ShouldLoadSavedData: {shouldLoadSavedData}", this );
            if ( shouldLoadSavedData )
            {
                CSaveManager saveManager = CSaveManager.Instance;
                bool isPrepared = saveManager != null && saveManager.TryPreparePendingLoad();
                Debug.Log( $"[ SaveDebug ] Pending load preparation result: {isPrepared}", this );

                if ( isPrepared )
                {
                    return;
                }
            }

            CSaveManager fallbackSaveManager = CSaveManager.Instance;

            if ( fallbackSaveManager != null )
            {
                fallbackSaveManager.ClearPendingLoadRequest();
            }

            CMapManager.SetPendingMapLoad( StarterMapId );
        }

        ///<summary>
        /// 타이틀 페이드 알파 코루틴 처리
        ///</summary>
        private IEnumerator IE_FadeAlpha(float _startAlpha, float _endAlpha)
        {
            if ( fadeImage == null )
            {
                yield break;
            }

            float elapsedTime = 0.0f;
            Color fadeColor = fadeImage.color;
            fadeColor.a = _startAlpha;
            fadeImage.color = fadeColor;

            if ( fadeDuration <= 0.0f )
            {
                fadeColor.a = _endAlpha;
                fadeImage.color = fadeColor;
                yield break;
            }

            while ( elapsedTime < fadeDuration )
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01( elapsedTime / fadeDuration );
                float alpha = Mathf.Lerp( _startAlpha, _endAlpha, normalizedTime );
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
                yield return null;
            }

            fadeColor.a = _endAlpha;
            fadeImage.color = fadeColor;
        }

        ///<summary>
        /// 타이틀 페이드 이미지 보장
        ///</summary>
        private void EnsureFadeImage()
        {
            if ( fadeImage != null )
            {
                return;
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();

            if ( parentCanvas == null )
            {
                return;
            }

            Transform foundFadeTransform = parentCanvas.transform.Find( FadeImageObjectName );

            if ( foundFadeTransform != null )
            {
                Image foundFadeImage = foundFadeTransform.GetComponent<Image>();
                fadeImage = foundFadeImage;
                SetFadeImageAlpha( 0.0f );
                return;
            }

            GameObject fadeImageObject = new GameObject( FadeImageObjectName, typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform fadeRectTransform = fadeImageObject.GetComponent<RectTransform>();
            fadeRectTransform.SetParent( parentCanvas.transform, false );
            fadeRectTransform.anchorMin = Vector2.zero;
            fadeRectTransform.anchorMax = Vector2.one;
            fadeRectTransform.offsetMin = Vector2.zero;
            fadeRectTransform.offsetMax = Vector2.zero;
            fadeRectTransform.SetAsLastSibling();
            Image createdFadeImage = fadeImageObject.GetComponent<Image>();
            createdFadeImage.raycastTarget = false;
            createdFadeImage.color = new Color( 0.0f, 0.0f, 0.0f, 0.0f );
            fadeImage = createdFadeImage;
        }

        ///<summary>
        /// 타이틀 페이드 이미지 알파 설정
        ///</summary>
        private void SetFadeImageAlpha(float _alpha)
        {
            if ( fadeImage == null )
            {
                return;
            }

            Color fadeColor = fadeImage.color;
            fadeColor.a = _alpha;
            fadeImage.color = fadeColor;
        }
    }
}
