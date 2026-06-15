using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 몬스터 정보 UI 뷰 컴포넌트
    ///</summary>
    public sealed class CMonsterInfoView : MonoBehaviour
    {
        private const string LevelTextObjectName = "LevelText";
        private const string NameTextObjectName = "NameText";
        private const string HpGaugeObjectName = "HPGauge";
        private const float DefaultGaugeAnimationDuration = 0.15f;

        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image hpGaugeImage;
        [SerializeField] private RectTransform cachedRectTransform;
        [SerializeField] private float gaugeAnimationDuration = DefaultGaugeAnimationDuration;

        private float displayedHpFillAmount;
        private float hpFillAmountStartValue;
        private float hpFillAmountTargetValue;
        private float hpFillAnimationElapsedTime;
        private bool isGaugeStateInitialized;
        private Coroutine hpGaugeAnimationRoutine;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 추적용 RectTransform 기준 정렬
        ///</summary>
        public void PrepareTrackingLayout()
        {
            ResolveReferences();

            if ( cachedRectTransform == null )
            {
                return;
            }

            cachedRectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            cachedRectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            cachedRectTransform.pivot = new Vector2( 0.5f, 0.5f );
            cachedRectTransform.localScale = Vector3.one;
            cachedRectTransform.localRotation = Quaternion.identity;
        }

        ///<summary>
        /// 표시 정보 반영
        ///</summary>
        public void ApplyMonsterInfo(string _monsterName, long _level, long _currentHp, long _maxHp)
        {
            ResolveReferences();

            if ( levelText != null )
            {
                levelText.text = $"Lv.{_level}";
            }

            if ( nameText != null )
            {
                nameText.text = _monsterName;
            }

            if ( hpGaugeImage != null )
            {
                float previousFillAmount = hpGaugeImage.fillAmount;
                float fillAmount = ResolveHpFillAmount( _currentHp, _maxHp );
                SetHpGaugeAnimationTarget( fillAmount );

            }
        }

        ///<summary>
        /// 앵커 위치 반영
        ///</summary>
        public void SetAnchoredPosition(Vector2 _anchoredPosition)
        {
            ResolveReferences();

            if ( cachedRectTransform == null )
            {
                return;
            }

            cachedRectTransform.anchoredPosition = _anchoredPosition;
        }

        ///<summary>
        /// 뷰 상태 초기화
        ///</summary>
        public void ResetView()
        {
            ResolveReferences();

            if ( levelText != null )
            {
                levelText.text = string.Empty;
            }

            if ( nameText != null )
            {
                nameText.text = string.Empty;
            }

            if ( hpGaugeImage != null )
            {
                hpGaugeImage.fillAmount = 0.0f;
            }

            displayedHpFillAmount = 0.0f;
            hpFillAmountStartValue = 0.0f;
            hpFillAmountTargetValue = 0.0f;
            hpFillAnimationElapsedTime = 0.0f;
            isGaugeStateInitialized = false;
            StopHpGaugeAnimationRoutine();
        }

        ///<summary>
        /// 체력 게이지 애니메이션 목표 설정
        ///</summary>
        private void SetHpGaugeAnimationTarget( float _fillAmount )
        {
            if ( isGaugeStateInitialized == false )
            {
                ApplyImmediateHpGaugeState( _fillAmount );
                return;
            }

            hpFillAmountStartValue = displayedHpFillAmount;
            hpFillAmountTargetValue = _fillAmount;
            hpFillAnimationElapsedTime = 0.0f;
            RestartHpGaugeAnimationRoutine();
        }

        ///<summary>
        /// 체력 게이지 즉시 반영
        ///</summary>
        private void ApplyImmediateHpGaugeState( float _fillAmount )
        {
            displayedHpFillAmount = _fillAmount;
            hpFillAmountStartValue = _fillAmount;
            hpFillAmountTargetValue = _fillAmount;
            hpFillAnimationElapsedTime = gaugeAnimationDuration;
            isGaugeStateInitialized = true;

            if ( hpGaugeImage != null )
            {
                hpGaugeImage.fillAmount = _fillAmount;
            }

            StopHpGaugeAnimationRoutine();
        }

    ///<summary>
        /// 체력 게이지 애니메이션 재시작
        ///</summary>
        private void RestartHpGaugeAnimationRoutine()
        {
            StopHpGaugeAnimationRoutine();
            hpGaugeAnimationRoutine = StartCoroutine( IE_AnimateHpGauge() );
        }

        ///<summary>
        /// 체력 게이지 애니메이션 중단
        ///</summary>
        private void StopHpGaugeAnimationRoutine()
        {
            if ( hpGaugeAnimationRoutine == null )
            {
                return;
            }

            StopCoroutine( hpGaugeAnimationRoutine );
            hpGaugeAnimationRoutine = null;
        }

        ///<summary>
        /// 체력 비율 계산
        ///</summary>
        private float ResolveHpFillAmount( long _currentHp, long _maxHp )
        {
            float fillAmount = 0.0f;

            if ( _maxHp > 0 )
            {
                fillAmount = Mathf.Clamp01( (float)_currentHp / _maxHp );
            }

            return fillAmount;
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
        /// 체력 게이지 보간 처리
        ///</summary>
        private IEnumerator IE_AnimateHpGauge()
        {
            if ( isGaugeStateInitialized == false || hpGaugeImage == null )
            {
                hpGaugeAnimationRoutine = null;
                yield break;
            }

            while ( hpFillAnimationElapsedTime < gaugeAnimationDuration )
            {
                hpFillAnimationElapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = ResolveAnimationNormalizedTime( hpFillAnimationElapsedTime );
                displayedHpFillAmount = Mathf.Lerp( hpFillAmountStartValue, hpFillAmountTargetValue, normalizedTime );
                hpGaugeImage.fillAmount = displayedHpFillAmount;
                yield return null;
            }

            displayedHpFillAmount = hpFillAmountTargetValue;
            hpGaugeImage.fillAmount = displayedHpFillAmount;
            hpGaugeAnimationRoutine = null;
        }

        ///<summary>
        /// 참조 컴포넌트 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( cachedRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                cachedRectTransform = resolvedRectTransform;
            }

            if ( levelText == null )
            {
                levelText = FindTextComponent( LevelTextObjectName );
            }

            if ( nameText == null )
            {
                nameText = FindTextComponent( NameTextObjectName );
            }

            if ( hpGaugeImage == null )
            {
                hpGaugeImage = FindImageComponent( HpGaugeObjectName );
            }
        }

        ///<summary>
        /// 이름 기준 TMP 컴포넌트 탐색
        ///</summary>
        private TextMeshProUGUI FindTextComponent(string _targetName)
        {
            TextMeshProUGUI[] textComponentArray = GetComponentsInChildren<TextMeshProUGUI>( true );

            for ( int i = 0; i < textComponentArray.Length; i++ )
            {
                TextMeshProUGUI textComponent = textComponentArray[ i ];

                if ( textComponent == null )
                {
                    continue;
                }

                if ( string.Equals( textComponent.gameObject.name, _targetName, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                return textComponent;
            }

            return null;
        }

        ///<summary>
        /// 이름 기준 이미지 컴포넌트 탐색
        ///</summary>
        private Image FindImageComponent(string _targetName)
        {
            Image[] imageComponentArray = GetComponentsInChildren<Image>( true );

            for ( int i = 0; i < imageComponentArray.Length; i++ )
            {
                Image imageComponent = imageComponentArray[ i ];

                if ( imageComponent == null )
                {
                    continue;
                }

                if ( string.Equals( imageComponent.gameObject.name, _targetName, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                return imageComponent;
            }

            return null;
        }
    }
}
