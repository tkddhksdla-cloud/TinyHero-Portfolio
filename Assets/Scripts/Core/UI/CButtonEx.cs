using TinyHero.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 확장 버튼 컴포넌트
    ///</summary>
    public sealed class CButtonEx : Button
    {
        private const string DefaultClickSfxClipName = "SFX_CLICK_00";
        private const float DefaultSfxVolumeScale = 1.0f;

        [Header( "효과음" )]
        [SerializeField] private bool useClickSfx = true;
        [SerializeField] private string clickSfxClipName = DefaultClickSfxClipName;
        [SerializeField] private float clickSfxVolumeScale = DefaultSfxVolumeScale;

        ///<summary>
        /// 활성화 시 클릭 효과음 선로딩 처리
        ///</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            PreloadClickSfx();
        }

        ///<summary>
        /// 버튼 클릭 이벤트 처리
        ///</summary>
        public override void OnPointerClick( PointerEventData _eventData )
        {
            bool canPlayClickSfx = CanPlayClickSfx( _eventData );

            if ( canPlayClickSfx )
            {
                PlayClickSfx();
            }

            base.OnPointerClick( _eventData );
        }

        ///<summary>
        /// 클릭 효과음 재생 가능 여부 반환
        ///</summary>
        private bool CanPlayClickSfx( PointerEventData _eventData )
        {
            if ( _eventData == null )
            {
                return false;
            }

            bool result = _eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable();
            return result;
        }

        ///<summary>
        /// 클릭 효과음 재생 처리
        ///</summary>
        private void PlayClickSfx()
        {
            if ( useClickSfx == false || string.IsNullOrWhiteSpace( clickSfxClipName ) )
            {
                return;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            string normalizedClipName = clickSfxClipName.Trim();
            float normalizedVolumeScale = Mathf.Clamp01( clickSfxVolumeScale );
            audioManager.PlaySfx( normalizedClipName, normalizedVolumeScale );
        }

        ///<summary>
        /// 클릭 효과음 선로딩 요청
        ///</summary>
        private void PreloadClickSfx()
        {
            if ( Application.isPlaying == false )
            {
                return;
            }

            if ( useClickSfx == false || string.IsNullOrWhiteSpace( clickSfxClipName ) )
            {
                return;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            string normalizedClipName = clickSfxClipName.Trim();
            audioManager.PreloadSfx( normalizedClipName );
        }
    }
}
