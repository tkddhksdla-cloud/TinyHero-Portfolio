using TMPro;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 데미지 폰트 뷰 컴포넌트
    ///</summary>
    public sealed class CDamageFontObject : CAutoPoolReturnObject
    {
        private const string DamageValueObjectName = "DamageValue";

        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private TextMeshProUGUI damageValueText;
        [SerializeField] private Animator targetAnimator;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 데미지 폰트 표시 내용 설정
        ///</summary>
        public void SetDisplay( string _damageText, Color _damageColor, Vector2 _anchoredPosition )
        {
            ResolveReferences();

            if ( damageValueText != null )
            {
                damageValueText.text = _damageText;
                damageValueText.color = _damageColor;
            }

            if ( targetRectTransform != null )
            {
                targetRectTransform.anchoredPosition = _anchoredPosition;
            }
        }

        ///<summary>
        /// 활성화 시 애니메이션 초기화
        ///</summary>
        protected override void OnAutoReturnObjectEnabled()
        {
            RestartAnimator();
        }

        ///<summary>
        /// 참조 컴포넌트 자동 연결
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                targetRectTransform = resolvedRectTransform;
            }

            if ( targetAnimator == null )
            {
                Animator resolvedAnimator = GetComponent<Animator>();
                targetAnimator = resolvedAnimator;
            }

            if ( damageValueText != null )
            {
                return;
            }

            Transform damageValueTransform = transform.Find( DamageValueObjectName );

            if ( damageValueTransform == null )
            {
                return;
            }

            TextMeshProUGUI resolvedDamageValueText = damageValueTransform.GetComponent<TextMeshProUGUI>();
            damageValueText = resolvedDamageValueText;
        }

        ///<summary>
        /// 데미지 폰트 애니메이션 재시작
        ///</summary>
        private void RestartAnimator()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            if ( targetAnimator.gameObject.activeInHierarchy == false || targetAnimator.isActiveAndEnabled == false )
            {
                return;
            }

            targetAnimator.Rebind();
            targetAnimator.Update( 0.0f );
        }
    }
}
