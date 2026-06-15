using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TinyHero.Player;
using System.Collections;

///<summary>
/// 플레이어 상태 UI 갱신 컴포넌트
///</summary>
public class CPlayerStatusUI : MonoBehaviour
{
    private const float DefaultBarWidth = 245.0f;
    private const float DefaultGaugeAnimationDuration = 0.15f;

    [SerializeField] private CPlayerStatManager targetStatManager;
    [SerializeField] private TextMeshProUGUI textHpValue;   // format : "{currentHp}/{maxHp}"
    [SerializeField] private RectTransform rectHpBarFillImage;  // width 0 ~ 245

    [Space]
    [SerializeField] private TextMeshProUGUI textMpValue;   // format : "{currentMp}/{maxMp}"
    [SerializeField] private RectTransform rectMpBarFillImage;  // width 0 ~ 245

    [Space]
    [SerializeField] private TextMeshProUGUI textLevelValue;    // format : "n"
    [SerializeField] private TextMeshProUGUI textExpValue;  // format : "n% <size=80%>{currentExp}/{maxExp}</size>"
    [SerializeField] private Image expFillImage;  // fillAmount 0 ~ 1

    [SerializeField] private float gaugeAnimationDuration = DefaultGaugeAnimationDuration;

    private float hpBarMaxWidth = DefaultBarWidth;
    private float mpBarMaxWidth = DefaultBarWidth;
    private float displayedHpCurrent;
    private float displayedHpMax;
    private float displayedMpCurrent;
    private float displayedMpMax;
    private float displayedExpCurrent;
    private float displayedExpMax;
    private float hpStartCurrent;
    private float hpStartMax;
    private float hpTargetCurrent;
    private float hpTargetMax;
    private float mpStartCurrent;
    private float mpStartMax;
    private float mpTargetCurrent;
    private float mpTargetMax;
    private float expStartCurrent;
    private float expStartMax;
    private float expTargetCurrent;
    private float expTargetMax;
    private float hpAnimationElapsedTime;
    private float mpAnimationElapsedTime;
    private float expAnimationElapsedTime;
    private int displayedLevelValue;
    private int targetLevelValue;
    private bool isUiStateInitialized;
    private Coroutine hpAnimationRoutine;
    private Coroutine mpAnimationRoutine;
    private Coroutine expAnimationRoutine;

    ///<summary>
    /// 상태 UI 초기 구성
    ///</summary>
    private void Awake()
    {
        ResolveTargetStatManager();
        CacheBarWidth();
    }

    ///<summary>
    /// 초기 바인딩 재시도
    ///</summary>
    private void Start()
    {
        InitializeStatusUi();
    }

    ///<summary>
    /// 이벤트 구독과 초기 갱신
    ///</summary>
    private void OnEnable()
    {
        ResolveTargetStatManager();
        SubscribeEvents();
        RefreshAll();
    }

    ///<summary>
    /// 이벤트 구독 해제
    ///</summary>
    private void OnDisable()
    {
        StopGaugeAnimationRoutines();
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
        RefreshAllImmediate();
    }

    ///<summary>
    /// 상태 UI 전체 갱신
    ///</summary>
    public void RefreshAll()
    {
        if ( targetStatManager == null )
        {
            return;
        }

        SetHpAnimationTarget( targetStatManager.GetCurrentHp(), targetStatManager.GetMaxHp() );
        SetMpAnimationTarget( targetStatManager.GetCurrentMp(), targetStatManager.GetMaxMp() );
        SetLevelExpAnimationTarget( targetStatManager.GetCurrentLevel(), targetStatManager.GetCurrentExp(), targetStatManager.GetMaxExp() );
    }

    ///<summary>
    /// 상태 UI 초기화 처리
    ///</summary>
    public void InitializeStatusUi()
    {
        ResolveTargetStatManager();
        SubscribeEvents();
        RefreshAllImmediate();
    }

    ///<summary>
    /// 상태 UI 즉시 초기화 처리
    ///</summary>
    public void RefreshAllImmediate()
    {
        if ( targetStatManager == null )
        {
            return;
        }

        int currentLevel = targetStatManager.GetCurrentLevel();
        float currentExp = targetStatManager.GetCurrentExp();
        float maxExp = targetStatManager.GetMaxExp();
        ResolveDisplayedLevelExpState( currentLevel, currentExp, maxExp, out float displayedCurrentExp, out float displayedMaxExp );
        ApplyImmediateHpState( targetStatManager.GetCurrentHp(), targetStatManager.GetMaxHp() );
        ApplyImmediateMpState( targetStatManager.GetCurrentMp(), targetStatManager.GetMaxMp() );
        ApplyImmediateLevelExpState( currentLevel, displayedCurrentExp, displayedMaxExp );
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

        CPlayerStatManager resolvedStatManager = GetComponentInParent<CPlayerStatManager>();

        if ( resolvedStatManager == null )
        {
            resolvedStatManager = FindFirstObjectByType<CPlayerStatManager>();
        }

        targetStatManager = resolvedStatManager;
    }

    ///<summary>
    /// 바 길이 기준값 캐시
    ///</summary>
    private void CacheBarWidth()
    {
        if ( rectHpBarFillImage != null )
        {
            hpBarMaxWidth = rectHpBarFillImage.sizeDelta.x;
        }

        if ( rectMpBarFillImage != null )
        {
            mpBarMaxWidth = rectMpBarFillImage.sizeDelta.x;
        }
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

        targetStatManager.OnHpChanged -= HandleHpChanged;
        targetStatManager.OnMpChanged -= HandleMpChanged;
        targetStatManager.OnLevelExpChanged -= HandleLevelExpChanged;

        targetStatManager.OnHpChanged += HandleHpChanged;
        targetStatManager.OnMpChanged += HandleMpChanged;
        targetStatManager.OnLevelExpChanged += HandleLevelExpChanged;
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

        targetStatManager.OnHpChanged -= HandleHpChanged;
        targetStatManager.OnMpChanged -= HandleMpChanged;
        targetStatManager.OnLevelExpChanged -= HandleLevelExpChanged;
    }

    ///<summary>
    /// 체력 변경 반영
    ///</summary>
    private void HandleHpChanged( float _currentHp, float _maxHp )
    {
        SetHpAnimationTarget( _currentHp, _maxHp );
    }

    ///<summary>
    /// 마나 변경 반영
    ///</summary>
    private void HandleMpChanged( float _currentMp, float _maxMp )
    {
        SetMpAnimationTarget( _currentMp, _maxMp );
    }

    ///<summary>
    /// 레벨과 경험치 변경 반영
    ///</summary>
    private void HandleLevelExpChanged( int _level, float _currentExp, float _maxExp )
    {
        SetLevelExpAnimationTarget( _level, _currentExp, _maxExp );
    }

    ///<summary>
    /// 체력 애니메이션 목표 설정
    ///</summary>
    private void SetHpAnimationTarget( float _currentHp, float _maxHp )
    {
        if ( isUiStateInitialized == false )
        {
            ApplyImmediateHpState( _currentHp, _maxHp );
            return;
        }

        hpStartCurrent = displayedHpCurrent;
        hpStartMax = displayedHpMax;
        hpTargetCurrent = _currentHp;
        hpTargetMax = _maxHp;
        hpAnimationElapsedTime = 0.0f;
        RestartHpAnimationRoutine();
    }

    ///<summary>
    /// 마나 애니메이션 목표 설정
    ///</summary>
    private void SetMpAnimationTarget( float _currentMp, float _maxMp )
    {
        if ( isUiStateInitialized == false )
        {
            ApplyImmediateMpState( _currentMp, _maxMp );
            return;
        }

        mpStartCurrent = displayedMpCurrent;
        mpStartMax = displayedMpMax;
        mpTargetCurrent = _currentMp;
        mpTargetMax = _maxMp;
        mpAnimationElapsedTime = 0.0f;
        RestartMpAnimationRoutine();
    }

    ///<summary>
    /// 레벨 경험치 애니메이션 목표 설정
    ///</summary>
    private void SetLevelExpAnimationTarget( int _level, float _currentExp, float _maxExp )
    {
        ResolveDisplayedLevelExpState( _level, _currentExp, _maxExp, out float displayedCurrentExp, out float displayedMaxExp );

        if ( isUiStateInitialized == false )
        {
            ApplyImmediateLevelExpState( _level, displayedCurrentExp, displayedMaxExp );
            return;
        }

        targetLevelValue = _level;
        expStartCurrent = displayedExpCurrent;
        expStartMax = displayedExpMax;
        expTargetCurrent = displayedCurrentExp;
        expTargetMax = displayedMaxExp;
        expAnimationElapsedTime = 0.0f;
        RestartExpAnimationRoutine();
    }

    ///<summary>
    /// 체력 상태 즉시 반영
    ///</summary>
    private void ApplyImmediateHpState( float _currentHp, float _maxHp )
    {
        displayedHpCurrent = _currentHp;
        displayedHpMax = _maxHp;
        hpStartCurrent = _currentHp;
        hpStartMax = _maxHp;
        hpTargetCurrent = _currentHp;
        hpTargetMax = _maxHp;
        hpAnimationElapsedTime = gaugeAnimationDuration;
        RefreshHp( displayedHpCurrent, displayedHpMax );
        isUiStateInitialized = true;
        StopHpAnimationRoutine();
    }

    ///<summary>
    /// 마나 상태 즉시 반영
    ///</summary>
    private void ApplyImmediateMpState( float _currentMp, float _maxMp )
    {
        displayedMpCurrent = _currentMp;
        displayedMpMax = _maxMp;
        mpStartCurrent = _currentMp;
        mpStartMax = _maxMp;
        mpTargetCurrent = _currentMp;
        mpTargetMax = _maxMp;
        mpAnimationElapsedTime = gaugeAnimationDuration;
        RefreshMp( displayedMpCurrent, displayedMpMax );
        isUiStateInitialized = true;
        StopMpAnimationRoutine();
    }

    ///<summary>
    /// 레벨 경험치 상태 즉시 반영
    ///</summary>
    private void ApplyImmediateLevelExpState( int _level, float _currentExp, float _maxExp )
    {
        displayedLevelValue = _level;
        targetLevelValue = _level;
        displayedExpCurrent = _currentExp;
        displayedExpMax = _maxExp;
        expStartCurrent = _currentExp;
        expStartMax = _maxExp;
        expTargetCurrent = _currentExp;
        expTargetMax = _maxExp;
        expAnimationElapsedTime = gaugeAnimationDuration;
        RefreshLevelExp( displayedLevelValue, displayedExpCurrent, displayedExpMax );
        isUiStateInitialized = true;
        StopExpAnimationRoutine();
    }

    ///<summary>
    /// 레벨 구간 기준 경험치 표시값 결정
    ///</summary>
    private void ResolveDisplayedLevelExpState( int _level, float _currentExp, float _maxExp, out float _displayedCurrentExp, out float _displayedMaxExp )
    {
        _displayedCurrentExp = _currentExp;
        _displayedMaxExp = _maxExp;

        if ( targetStatManager == null )
        {
            return;
        }

        _displayedCurrentExp = targetStatManager.GetLevelExpProgress( _level, _currentExp );
        _displayedMaxExp = targetStatManager.GetLevelExpRequirement( _level, _maxExp );
    }

    ///<summary>
    /// 체력 UI 애니메이션 재시작
    ///</summary>
    private void RestartHpAnimationRoutine()
    {
        StopHpAnimationRoutine();
        hpAnimationRoutine = StartCoroutine( IE_AnimateHpDisplay() );
    }

    ///<summary>
    /// 마나 UI 애니메이션 재시작
    ///</summary>
    private void RestartMpAnimationRoutine()
    {
        StopMpAnimationRoutine();
        mpAnimationRoutine = StartCoroutine( IE_AnimateMpDisplay() );
    }

    ///<summary>
    /// 경험치 UI 애니메이션 재시작
    ///</summary>
    private void RestartExpAnimationRoutine()
    {
        StopExpAnimationRoutine();
        expAnimationRoutine = StartCoroutine( IE_AnimateExpDisplay() );
    }

    ///<summary>
    /// 체력 UI 애니메이션 중단
    ///</summary>
    private void StopHpAnimationRoutine()
    {
        if ( hpAnimationRoutine == null )
        {
            return;
        }

        StopCoroutine( hpAnimationRoutine );
        hpAnimationRoutine = null;
    }

    ///<summary>
    /// 마나 UI 애니메이션 중단
    ///</summary>
    private void StopMpAnimationRoutine()
    {
        if ( mpAnimationRoutine == null )
        {
            return;
        }

        StopCoroutine( mpAnimationRoutine );
        mpAnimationRoutine = null;
    }

    ///<summary>
    /// 경험치 UI 애니메이션 중단
    ///</summary>
    private void StopExpAnimationRoutine()
    {
        if ( expAnimationRoutine == null )
        {
            return;
        }

        StopCoroutine( expAnimationRoutine );
        expAnimationRoutine = null;
    }

    ///<summary>
    /// 상태 UI 애니메이션 전체 중단
    ///</summary>
    private void StopGaugeAnimationRoutines()
    {
        StopHpAnimationRoutine();
        StopMpAnimationRoutine();
        StopExpAnimationRoutine();
    }

    ///<summary>
    /// 게이지 애니메이션 진행률 계산
    ///</summary>
    private float ResolveAnimationNormalizedTime( float _elapsedTime )
    {
        float duration = Mathf.Max( 0.001f, gaugeAnimationDuration );
        float normalizedTime = Mathf.Clamp01( _elapsedTime / duration );
        return normalizedTime;
    }

    ///<summary>
    /// 체력 UI 보간 처리
    ///</summary>
    private IEnumerator IE_AnimateHpDisplay()
    {
        while ( hpAnimationElapsedTime < gaugeAnimationDuration )
        {
            hpAnimationElapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = ResolveAnimationNormalizedTime( hpAnimationElapsedTime );
            displayedHpCurrent = Mathf.Lerp( hpStartCurrent, hpTargetCurrent, normalizedTime );
            displayedHpMax = Mathf.Lerp( hpStartMax, hpTargetMax, normalizedTime );
            RefreshHp( displayedHpCurrent, displayedHpMax );
            yield return null;
        }

        displayedHpCurrent = hpTargetCurrent;
        displayedHpMax = hpTargetMax;
        RefreshHp( displayedHpCurrent, displayedHpMax );
        hpAnimationRoutine = null;
    }

    ///<summary>
    /// 마나 UI 보간 처리
    ///</summary>
    private IEnumerator IE_AnimateMpDisplay()
    {
        while ( mpAnimationElapsedTime < gaugeAnimationDuration )
        {
            mpAnimationElapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = ResolveAnimationNormalizedTime( mpAnimationElapsedTime );
            displayedMpCurrent = Mathf.Lerp( mpStartCurrent, mpTargetCurrent, normalizedTime );
            displayedMpMax = Mathf.Lerp( mpStartMax, mpTargetMax, normalizedTime );
            RefreshMp( displayedMpCurrent, displayedMpMax );
            yield return null;
        }

        displayedMpCurrent = mpTargetCurrent;
        displayedMpMax = mpTargetMax;
        RefreshMp( displayedMpCurrent, displayedMpMax );
        mpAnimationRoutine = null;
    }

    ///<summary>
    /// 경험치 UI 보간 처리
    ///</summary>
    private IEnumerator IE_AnimateExpDisplay()
    {
        while ( expAnimationElapsedTime < gaugeAnimationDuration )
        {
            expAnimationElapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = ResolveAnimationNormalizedTime( expAnimationElapsedTime );
            displayedLevelValue = normalizedTime >= 1.0f ? targetLevelValue : displayedLevelValue;
            displayedExpCurrent = Mathf.Lerp( expStartCurrent, expTargetCurrent, normalizedTime );
            displayedExpMax = Mathf.Lerp( expStartMax, expTargetMax, normalizedTime );
            RefreshLevelExp( displayedLevelValue, displayedExpCurrent, displayedExpMax );
            yield return null;
        }

        displayedLevelValue = targetLevelValue;
        displayedExpCurrent = expTargetCurrent;
        displayedExpMax = expTargetMax;
        RefreshLevelExp( displayedLevelValue, displayedExpCurrent, displayedExpMax );
        expAnimationRoutine = null;
    }

    ///<summary>
    /// 체력 UI 갱신
    ///</summary>
    private void RefreshHp( float _currentHp, float _maxHp )
    {
        if ( textHpValue != null )
        {
            int roundedCurrentHp = Mathf.RoundToInt( _currentHp );
            int roundedMaxHp = Mathf.RoundToInt( _maxHp );
            textHpValue.text = $"{roundedCurrentHp}/{roundedMaxHp}";
        }

        ApplyBarWidth( rectHpBarFillImage, hpBarMaxWidth, _currentHp, _maxHp );
    }

    ///<summary>
    /// 마나 UI 갱신
    ///</summary>
    private void RefreshMp( float _currentMp, float _maxMp )
    {
        if ( textMpValue != null )
        {
            int roundedCurrentMp = Mathf.RoundToInt( _currentMp );
            int roundedMaxMp = Mathf.RoundToInt( _maxMp );
            textMpValue.text = $"{roundedCurrentMp}/{roundedMaxMp}";
        }

        ApplyBarWidth( rectMpBarFillImage, mpBarMaxWidth, _currentMp, _maxMp );
    }

    ///<summary>
    /// 레벨과 경험치 UI 갱신
    ///</summary>
    private void RefreshLevelExp( int _level, float _currentExp, float _maxExp )
    {
        if ( textLevelValue != null )
        {
            textLevelValue.text = _level.ToString();
        }

        if ( textExpValue != null )
        {
            float normalizedExp = 0.0f;

            if ( _maxExp > 0.0f )
            {
                normalizedExp = Mathf.Clamp01( _currentExp / _maxExp );
            }

            float expPercent = normalizedExp * 100.0f;
            int roundedCurrentExp = Mathf.RoundToInt( _currentExp );
            int roundedMaxExp = Mathf.RoundToInt( _maxExp );
            textExpValue.text = $"{expPercent:0.#}% <size=80%>{roundedCurrentExp}/{roundedMaxExp}</size>";
        }

        if ( expFillImage != null )
        {
            float fillAmount = 0.0f;

            if ( _maxExp > 0.0f )
            {
                fillAmount = Mathf.Clamp01( _currentExp / _maxExp );
            }

            expFillImage.fillAmount = fillAmount;
        }
    }

    ///<summary>
    /// 바 너비 반영
    ///</summary>
    private void ApplyBarWidth( RectTransform _targetRectTransform, float _maxWidth, float _currentValue, float _maxValue )
    {
        if ( _targetRectTransform == null )
        {
            return;
        }

        float normalizedValue = 0.0f;

        if ( _maxValue > 0.0f )
        {
            normalizedValue = Mathf.Clamp01( _currentValue / _maxValue );
        }

        Vector2 sizeDelta = _targetRectTransform.sizeDelta;
        sizeDelta.x = _maxWidth * normalizedValue;
        _targetRectTransform.sizeDelta = sizeDelta;
    }




}
