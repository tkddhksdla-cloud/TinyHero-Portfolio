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

        private RectTransform followRootRectTransform;
        private Canvas followCanvas;
        private Camera followWorldCamera;
        private Vector3 followWorldPosition;

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
        public void SetDisplay( string _damageText, Color _damageColor, Vector3 _worldPosition, RectTransform _rootRectTransform, Canvas _targetCanvas, Camera _worldCamera )
        {
            ResolveReferences();
            followWorldPosition = _worldPosition;
            followRootRectTransform = _rootRectTransform;
            followCanvas = _targetCanvas;
            followWorldCamera = _worldCamera;

            if ( damageValueText != null )
            {
                damageValueText.text = _damageText;
                damageValueText.color = _damageColor;
            }

            UpdateFollowPosition();
        }

        ///<summary>
        /// 후처리 위치 갱신
        ///</summary>
        private void LateUpdate()
        {
            UpdateFollowPosition();
        }

        ///<summary>
        /// 활성화 시 애니메이션 초기화
        ///</summary>
        protected override void OnAutoReturnObjectEnabled()
        {
            RestartAnimator();
        }

        ///<summary>
        /// 비활성화 시 추적 상태 초기화
        ///</summary>
        protected override void OnAutoReturnObjectDisabled()
        {
            followRootRectTransform = null;
            followCanvas = null;
            followWorldCamera = null;
            followWorldPosition = Vector3.zero;
        }

        ///<summary>
        /// 추적 대상 기준 UI 위치 갱신
        ///</summary>
        private void UpdateFollowPosition()
        {
            if ( targetRectTransform == null || followRootRectTransform == null )
            {
                return;
            }

            Camera resolvedWorldCamera = ResolveWorldCamera();
            Vector3 screenPosition = resolvedWorldCamera != null
                ? resolvedWorldCamera.WorldToScreenPoint( followWorldPosition )
                : RectTransformUtility.WorldToScreenPoint( null, followWorldPosition );

            if ( screenPosition.z < 0.0f )
            {
                return;
            }

            Camera canvasCamera = followCanvas != null && followCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? followCanvas.worldCamera
                : null;
            bool wasResolved = RectTransformUtility.ScreenPointToLocalPointInRectangle( followRootRectTransform, screenPosition, canvasCamera, out Vector2 localPoint );

            if ( wasResolved == false )
            {
                return;
            }

            targetRectTransform.anchoredPosition = localPoint;
        }

        ///<summary>
        /// 월드 카메라 결정
        ///</summary>
        private Camera ResolveWorldCamera()
        {
            if ( followWorldCamera != null )
            {
                return followWorldCamera;
            }

            Camera mainCamera = Camera.main;
            followWorldCamera = mainCamera;
            return followWorldCamera;
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
