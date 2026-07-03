using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 로딩 진행 상태 표시 컴포넌트
    ///</summary>
    public sealed class CMapLoadingUI : MonoBehaviour
    {
        private const string DefaultLoadingText = "Loading...";

        [Header( "UI 참조" )]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text percentText;
        [SerializeField] private Image gaugeFillImage;

        ///<summary>
        /// 로딩 UI 표시 상태 갱신
        ///</summary>
        public void Show( string _statusText, float _progress )
        {
            gameObject.SetActive( true );
            SetProgress( _statusText, _progress );
        }

        ///<summary>
        /// 로딩 UI 진행 상태 갱신
        ///</summary>
        public void SetProgress( string _statusText, float _progress )
        {
            float clampedProgress = Mathf.Clamp01( _progress );

            if ( statusText != null )
            {
                statusText.text = string.IsNullOrWhiteSpace( _statusText ) ? DefaultLoadingText : _statusText;
            }

            if ( percentText != null )
            {
                float percentValue = clampedProgress * 100.0f;
                int percent = Mathf.RoundToInt( percentValue );
                percentText.text = $"{percent}%";
            }

            if ( gaugeFillImage != null )
            {
                gaugeFillImage.fillAmount = clampedProgress;
            }
        }

        ///<summary>
        /// 로딩 UI 숨김 상태 처리
        ///</summary>
        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
