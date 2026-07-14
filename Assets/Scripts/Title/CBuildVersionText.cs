using TMPro;
using UnityEngine;

namespace TinyHero.Title
{
    ///<summary>
    /// 현재 Player 빌드 버전 표시 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class CBuildVersionText : MonoBehaviour
    {
        private const string VersionTextFormat = "version : {0}";

        [Header( "참조" )]
        [SerializeField] private TMP_Text versionText;

        ///<summary>
        /// 현재 빌드 버전 표시 초기화
        ///</summary>
        private void Awake()
        {
            RefreshVersionText();
        }

        ///<summary>
        /// 현재 빌드 버전 표시 갱신
        ///</summary>
        private void RefreshVersionText()
        {
            if ( versionText == null )
            {
                Debug.LogWarning( "[ BuildVersionText ] Version TMP_Text reference is missing.", this );
                return;
            }

            string resolvedVersionText = string.Format( VersionTextFormat, Application.version );
            versionText.text = resolvedVersionText;
        }
    }
}
