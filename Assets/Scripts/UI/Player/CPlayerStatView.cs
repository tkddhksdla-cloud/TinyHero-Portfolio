using TinyHero.Core;
using TinyHero.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 플레이어 스탯 UI 갱신 컴포넌트
    ///</summary>
    public sealed class CPlayerStatView : MonoBehaviour
    {
        [Header( "Target" )]
        [SerializeField] private CPlayerStatManager targetStatManager;

        [Header( "Resource UI" )]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Image mpFillImage;

        [Header( "Stat UI" )]
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI crtText;
        [SerializeField] private TextMeshProUGUI crdText;
        [SerializeField] private TextMeshProUGUI accText;
        [SerializeField] private TextMeshProUGUI cdrText;
        [SerializeField] private TextMeshProUGUI moveText;
        [SerializeField] private TextMeshProUGUI hrText;
        [SerializeField] private TextMeshProUGUI mrText;
        [SerializeField] private TextMeshProUGUI rangeText;

        ///<summary>
        /// 참조 자동 연결
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            ResolveTargetStatManager();
        }

        ///<summary>
        /// 이벤트 구독과 초기 갱신
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            ResolveTargetStatManager();
            SubscribeEvents();
            RefreshView();
        }

        ///<summary>
        /// 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        ///<summary>
        /// 외부 스탯 매니저 연결
        ///</summary>
        public void Bind( CPlayerStatManager _targetStatManager )
        {
            UnsubscribeEvents();
            targetStatManager = _targetStatManager;
            SubscribeEvents();
            RefreshView();
        }

        ///<summary>
        /// UI 전체 갱신
        ///</summary>
        public void RefreshView()
        {
            ResolveReferences();

            if ( targetStatManager == null )
            {
                return;
            }

            ApplyResourceText( hpText, targetStatManager.GetCurrentHp(), targetStatManager.GetMaxHp() );
            ApplyResourceText( mpText, targetStatManager.GetCurrentMp(), targetStatManager.GetMaxMp() );
            ApplyFillAmount( hpFillImage, targetStatManager.GetCurrentHp(), targetStatManager.GetMaxHp() );
            ApplyFillAmount( mpFillImage, targetStatManager.GetCurrentMp(), targetStatManager.GetMaxMp() );

            ApplyValueText( atkText, targetStatManager.GetFinalStatValue( ePlayerStatType.ATK ) );
            ApplyValueText( defText, targetStatManager.GetFinalStatValue( ePlayerStatType.DEF ) );
            ApplyFlatPercentText( crtText, targetStatManager.GetFinalStatValue( ePlayerStatType.CRT ) );
            ApplyCriticalDamagePercentText( crdText, targetStatManager.GetFinalStatValue( ePlayerStatType.CRD ) );
            ApplyValueText( accText, targetStatManager.GetFinalStatValue( ePlayerStatType.ACC ) );
            ApplyFlatPercentText( cdrText, targetStatManager.GetFinalStatValue( ePlayerStatType.CDR ) );
            ApplyFlatPercentText( moveText, targetStatManager.GetFinalStatValue( ePlayerStatType.MOVE ) );
            ApplyValueText( hrText, targetStatManager.GetFinalStatValue( ePlayerStatType.HR ) );
            ApplyValueText( mrText, targetStatManager.GetFinalStatValue( ePlayerStatType.MR ) );
            ApplyFlatPercentText( rangeText, targetStatManager.GetFinalStatValue( ePlayerStatType.RANGE ) );
        }

        ///<summary>
        /// 대상 스탯 매니저 결정
        ///</summary>
        private void ResolveTargetStatManager()
        {
            if ( targetStatManager != null )
            {
                return;
            }

            CPlayerStatManager resolvedManager = GetComponentInParent<CPlayerStatManager>();

            if ( resolvedManager == null )
            {
                bool hasRuntimeContext = CActivePlayerRuntimeContextResolver.TryGetActivePlayerRuntimeContext( out CPlayerRuntimeContext playerRuntimeContext );
                resolvedManager = hasRuntimeContext ? playerRuntimeContext.GetStatManager() : null;
            }

            targetStatManager = resolvedManager;
        }

        ///<summary>
        /// 하위 UI 참조 자동 결정
        ///</summary>
        private void ResolveReferences()
        {
            hpText = ResolveTextReference( hpText, "HpText" );
            mpText = ResolveTextReference( mpText, "MpText" );
            atkText = ResolveTextReference( atkText, "AtkText" );
            defText = ResolveTextReference( defText, "DefText" );
            crtText = ResolveTextReference( crtText, "CrtText" );
            crdText = ResolveTextReference( crdText, "CrdText" );
            accText = ResolveTextReference( accText, "AccText" );
            cdrText = ResolveTextReference( cdrText, "CdrText" );
            moveText = ResolveTextReference( moveText, "MoveText" );
            hrText = ResolveTextReference( hrText, "HrText" );
            mrText = ResolveTextReference( mrText, "MrText" );
            rangeText = ResolveTextReference( rangeText, "RngText", "StatPointText" );
            hpFillImage = ResolveImageReference( hpFillImage, "HpFillImage" );
            mpFillImage = ResolveImageReference( mpFillImage, "MpFillImage" );
        }

        ///<summary>
        /// 텍스트 참조 결정
        ///</summary>
        private TextMeshProUGUI ResolveTextReference( TextMeshProUGUI _currentReference, params string[] _targetNameArray )
        {
            if ( _currentReference != null )
            {
                return _currentReference;
            }

            TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>( true );

            for ( int index = 0; index < textComponents.Length; index++ )
            {
                TMP_Text textComponent = textComponents[ index ];

                if ( textComponent == null || IsMatchingTextName( textComponent.name, _targetNameArray ) == false )
                {
                    continue;
                }

                TextMeshProUGUI result = textComponent as TextMeshProUGUI;
                return result;
            }

            return null;
        }

        ///<summary>
        /// 대상 텍스트 이름 일치 여부 반환
        ///</summary>
        private bool IsMatchingTextName( string _textName, params string[] _targetNameArray )
        {
            if ( string.IsNullOrWhiteSpace( _textName ) || _targetNameArray == null )
            {
                return false;
            }

            for ( int index = 0; index < _targetNameArray.Length; index++ )
            {
                string targetName = _targetNameArray[ index ];

                if ( string.IsNullOrWhiteSpace( targetName ) )
                {
                    continue;
                }

                if ( string.Equals( _textName, targetName, System.StringComparison.Ordinal ) )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 이미지 참조 결정
        ///</summary>
        private Image ResolveImageReference( Image _currentReference, string _targetName )
        {
            if ( _currentReference != null )
            {
                return _currentReference;
            }

            Image[] imageComponents = GetComponentsInChildren<Image>( true );

            for ( int index = 0; index < imageComponents.Length; index++ )
            {
                Image imageComponent = imageComponents[ index ];

                if ( imageComponent == null || imageComponent.name != _targetName )
                {
                    continue;
                }

                return imageComponent;
            }

            return null;
        }

        ///<summary>
        /// 스탯 이벤트 구독
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetStatManager == null )
            {
                return;
            }

            targetStatManager.OnStatChanged -= HandleStatChanged;
            targetStatManager.OnHpChanged -= HandleHpChanged;
            targetStatManager.OnMpChanged -= HandleMpChanged;

            targetStatManager.OnStatChanged += HandleStatChanged;
            targetStatManager.OnHpChanged += HandleHpChanged;
            targetStatManager.OnMpChanged += HandleMpChanged;
        }

        ///<summary>
        /// 스탯 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetStatManager == null )
            {
                return;
            }

            targetStatManager.OnStatChanged -= HandleStatChanged;
            targetStatManager.OnHpChanged -= HandleHpChanged;
            targetStatManager.OnMpChanged -= HandleMpChanged;
        }

        ///<summary>
        /// 전체 스탯 변경 반영
        ///</summary>
        private void HandleStatChanged( CPlayerStatManager _statManager )
        {
            RefreshView();
        }

        ///<summary>
        /// 체력 변경 반영
        ///</summary>
        private void HandleHpChanged( float _currentHp, float _maxHp )
        {
            ApplyResourceText( hpText, _currentHp, _maxHp );
            ApplyFillAmount( hpFillImage, _currentHp, _maxHp );
        }

        ///<summary>
        /// 마나 변경 반영
        ///</summary>
        private void HandleMpChanged( float _currentMp, float _maxMp )
        {
            ApplyResourceText( mpText, _currentMp, _maxMp );
            ApplyFillAmount( mpFillImage, _currentMp, _maxMp );
        }

        ///<summary>
        /// 자원 수치 텍스트 반영
        ///</summary>
        private void ApplyResourceText( TextMeshProUGUI _targetText, float _currentValue, float _maxValue )
        {
            if ( _targetText == null )
            {
                return;
            }

            int roundedCurrentValue = Mathf.RoundToInt( _currentValue );
            int roundedMaxValue = Mathf.RoundToInt( _maxValue );
            _targetText.text = $"{roundedCurrentValue} / {roundedMaxValue}";
        }

        ///<summary>
        /// 일반 수치 텍스트 반영
        ///</summary>
        private void ApplyValueText( TextMeshProUGUI _targetText, float _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            _targetText.text = _value.ToString( "0.##" );
        }

        ///<summary>
        /// 퍼센트 텍스트 반영
        ///</summary>
        private void ApplyFlatPercentText( TextMeshProUGUI _targetText, float _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            _targetText.text = $"{_value:0.##}%";
        }

        ///<summary>
        /// 배수 텍스트 반영
        ///</summary>
        private void ApplyCriticalDamagePercentText( TextMeshProUGUI _targetText, float _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            float totalPercent = 100.0f + Mathf.Max( 0.0f, _value );
            _targetText.text = $"{totalPercent:0.##}%";
        }

        ///<summary>
        /// 게이지 채움값 반영
        ///</summary>
        private void ApplyFillAmount( Image _targetImage, float _currentValue, float _maxValue )
        {
            if ( _targetImage == null )
            {
                return;
            }

            float fillAmount = 0.0f;

            if ( _maxValue > 0.0f )
            {
                fillAmount = Mathf.Clamp01( _currentValue / _maxValue );
            }

            _targetImage.fillAmount = fillAmount;
        }
    }
}
