using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Title
{
    ///<summary>
    /// 타이틀 씬 BGM 재생 루트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class SceneTitleBgmRoot : MonoBehaviour
    {
        private const float DefaultFadeDuration = 0.75f;

        [Header( "BGM" )]
        [SerializeField] private string bgmClipName = string.Empty;
        [SerializeField] private float fadeDuration = DefaultFadeDuration;
        [SerializeField] private bool playOnStart = true;

        ///<summary>
        /// 시작 시 BGM 재생 처리
        ///</summary>
        private void Start()
        {
            if ( playOnStart == false )
            {
                return;
            }

            PlayTitleBgm();
        }

        ///<summary>
        /// 타이틀 BGM 재생 요청
        ///</summary>
        public void PlayTitleBgm()
        {
            if ( string.IsNullOrWhiteSpace( bgmClipName ) )
            {
                return;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            string normalizedClipName = bgmClipName.Trim();
            float resolvedFadeDuration = Mathf.Max( 0.0f, fadeDuration );
            audioManager.PlayBgm( normalizedClipName, resolvedFadeDuration );
        }
    }
}
