using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 게임 공용 설정 관리 컴포넌트
    ///</summary>
    public sealed class CGameSettingManager : CSingleTon<CGameSettingManager>
    {
        private const string TextLanguagePlayerPrefsKey = "TinyHero.Settings.TextLanguage";
        private const eTextLanguage DefaultTextLanguage = eTextLanguage.KR;

        [SerializeField] private eTextLanguage currentTextLanguage = DefaultTextLanguage;

        ///<summary>
        /// 설정 매니저 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            LoadSettings();
            ApplyTextLanguageToDataManager();
        }

        ///<summary>
        /// 현재 텍스트 언어 반환
        ///</summary>
        public eTextLanguage GetCurrentTextLanguage()
        {
            eTextLanguage result = currentTextLanguage;
            return result;
        }

        ///<summary>
        /// 현재 텍스트 언어 설정
        ///</summary>
        public void SetCurrentTextLanguage( eTextLanguage _textLanguage )
        {
            currentTextLanguage = _textLanguage;
            SaveSettings();
            ApplyTextLanguageToDataManager();
        }

        ///<summary>
        /// 저장된 게임 설정 로드
        ///</summary>
        public void LoadSettings()
        {
            string savedLanguageText = PlayerPrefs.GetString( TextLanguagePlayerPrefsKey, DefaultTextLanguage.ToString() );
            bool isParsed = System.Enum.TryParse( savedLanguageText, true, out eTextLanguage parsedLanguage );
            currentTextLanguage = isParsed ? parsedLanguage : DefaultTextLanguage;
        }

        ///<summary>
        /// 현재 게임 설정 저장
        ///</summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetString( TextLanguagePlayerPrefsKey, currentTextLanguage.ToString() );
            PlayerPrefs.Save();
        }

        ///<summary>
        /// 데이터 매니저 텍스트 언어 반영
        ///</summary>
        private void ApplyTextLanguageToDataManager()
        {
            CDataManager dataManager = CDataManager.Instance;

            if ( dataManager == null )
            {
                return;
            }

            dataManager.SetCurrentTextLanguage( currentTextLanguage );
        }
    }
}
