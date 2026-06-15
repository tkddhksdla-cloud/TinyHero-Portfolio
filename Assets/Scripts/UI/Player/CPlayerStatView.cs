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
        [SerializeField] private TextMeshProUGUI atsText;
        [SerializeField] private TextMeshProUGUI moveText;
        [SerializeField] private TextMeshProUGUI hrText;
        [SerializeField] private TextMeshProUGUI mrText;
        [SerializeField] private TextMeshProUGUI statPointText;

        ///<summary>
        /// 참조 자동 연결
        ///</summary>
        private void Awake()
        {
            ResolveTargetStatManager();
        }

        ///<summary>
        /// 이벤트 구독과 초기 갱신
        ///</summary>
        private void OnEnable()
        {
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
            ApplyPercentText( crtText, targetStatManager.GetFinalStatValue( ePlayerStatType.CRT ) );
            ApplyMultiplierText( crdText, targetStatManager.GetFinalStatValue( ePlayerStatType.CRD ) );
            ApplyValueText( atsText, targetStatManager.GetFinalStatValue( ePlayerStatType.ATS ) );
            ApplyValueText( moveText, targetStatManager.GetFinalStatValue( ePlayerStatType.MOVE ) );
            ApplyValueText( hrText, targetStatManager.GetFinalStatValue( ePlayerStatType.HR ) );
            ApplyValueText( mrText, targetStatManager.GetFinalStatValue( ePlayerStatType.MR ) );
            ApplyStatPointText( statPointText, targetStatManager.GetUnspentStatPoint() );
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
            targetStatManager = resolvedManager;
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
            targetStatManager.OnStatPointChanged -= HandleStatPointChanged;

            targetStatManager.OnStatChanged += HandleStatChanged;
            targetStatManager.OnHpChanged += HandleHpChanged;
            targetStatManager.OnMpChanged += HandleMpChanged;
            targetStatManager.OnStatPointChanged += HandleStatPointChanged;
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
            targetStatManager.OnStatPointChanged -= HandleStatPointChanged;
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
        /// 스탯 포인트 변경 반영
        ///</summary>
        private void HandleStatPointChanged( int _statPoint )
        {
            ApplyStatPointText( statPointText, _statPoint );
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
        /// 확률 텍스트 반영
        ///</summary>
        private void ApplyPercentText( TextMeshProUGUI _targetText, float _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            float percentValue = _value * 100.0f;
            _targetText.text = $"{percentValue:0.##}%";
        }

        ///<summary>
        /// 배율 텍스트 반영
        ///</summary>
        private void ApplyMultiplierText( TextMeshProUGUI _targetText, float _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            _targetText.text = $"{_value:0.##}x";
        }

        ///<summary>
        /// 스탯 포인트 텍스트 반영
        ///</summary>
        private void ApplyStatPointText( TextMeshProUGUI _targetText, int _value )
        {
            if ( _targetText == null )
            {
                return;
            }

            _targetText.text = _value.ToString();
        }

        ///<summary>
        /// 게이지 채움량 반영
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
