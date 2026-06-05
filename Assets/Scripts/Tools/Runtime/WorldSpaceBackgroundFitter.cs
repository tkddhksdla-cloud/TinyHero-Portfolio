using UnityEngine;

namespace TinyHero.Tools
{
    /// <summary>
    /// 월드 스페이스 배경 스프라이트를 카메라 화면 크기에 맞춰 조정한다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WorldSpaceBackgroundFitter : MonoBehaviour
    {
        private enum eBackgroundFitMode
        {
            COVER,
            CONTAIN
        }

        private const float MinimumParentScale = 0.0001f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [SerializeField] private eBackgroundFitMode fitMode = eBackgroundFitMode.COVER;
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool updateContinuously;
        [SerializeField] private Vector3 baseLocalScale = Vector3.one;

        private Sprite previousSprite;
        private bool isInspectorRealtimeSyncActive;

        /// <summary>
        /// 기본 참조를 자동으로 연결한다.
        /// </summary>
        private void Reset()
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
            baseLocalScale = transform.localScale;
            CacheCurrentSprite();
        }

        /// <summary>
        /// 활성화 시 배경 크기를 즉시 갱신한다.
        /// </summary>
        private void OnEnable()
        {
            if ( applyOnEnable == false )
            {
                CacheCurrentSprite();
                return;
            }

            ApplyFit();
        }

        /// <summary>
        /// 비활성화 시 인스펙터 실시간 동기화를 해제한다.
        /// </summary>
        private void OnDisable()
        {
            isInspectorRealtimeSyncActive = false;
        }

        /// <summary>
        /// 인스펙터 값 변경 시 참조와 스케일 값을 정리한다.
        /// </summary>
        private void OnValidate()
        {
            if ( targetSpriteRenderer == null )
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if ( Mathf.Approximately( baseLocalScale.x, 0.0f ) && Mathf.Approximately( baseLocalScale.y, 0.0f ) )
            {
                baseLocalScale = transform.localScale;
            }

            if ( isActiveAndEnabled == false )
            {
                return;
            }

            ApplyFit();
        }

        /// <summary>
        /// 지속 갱신 옵션이 켜져 있으면 매 프레임 배경 크기를 맞춘다.
        /// </summary>
        private void LateUpdate()
        {
            if ( updateContinuously )
            {
                ApplyFit();
            }

            if ( isInspectorRealtimeSyncActive == false )
            {
                return;
            }

            ApplyFitIfSpriteChanged();
        }

        /// <summary>
        /// 현재 로컬 스케일을 기준 스케일로 저장한다.
        /// </summary>
        [ContextMenu( "Capture Base Scale" )]
        public void CaptureBaseScale()
        {
            baseLocalScale = transform.localScale;
        }

        /// <summary>
        /// 배경 스프라이트를 카메라 화면에 맞게 스케일링한다.
        /// </summary>
        [ContextMenu( "Apply Background Fit" )]
        public void ApplyFit()
        {
            Camera resolvedCamera = ResolveCamera();
            SpriteRenderer resolvedSpriteRenderer = ResolveSpriteRenderer();

            if ( resolvedCamera == null || resolvedSpriteRenderer == null )
            {
                return;
            }

            Sprite sprite = resolvedSpriteRenderer.sprite;

            if ( sprite == null )
            {
                previousSprite = null;
                return;
            }

            Vector2 targetWorldSize = CalculateTargetWorldSize( resolvedCamera );
            Vector2 baseWorldSize = CalculateBaseWorldSize( sprite );

            if ( baseWorldSize.x <= 0.0f || baseWorldSize.y <= 0.0f )
            {
                return;
            }

            float scaleMultiplier = CalculateScaleMultiplier( targetWorldSize, baseWorldSize );
            Vector3 localScale = transform.localScale;
            float scaleX = Mathf.Abs( baseLocalScale.x ) * scaleMultiplier;
            float scaleY = Mathf.Abs( baseLocalScale.y ) * scaleMultiplier;
            localScale.x = Mathf.Sign( GetSignedScaleValue( baseLocalScale.x ) ) * scaleX;
            localScale.y = Mathf.Sign( GetSignedScaleValue( baseLocalScale.y ) ) * scaleY;
            transform.localScale = localScale;
            previousSprite = sprite;
        }

        /// <summary>
        /// 인스펙터 실시간 동기화 상태를 설정한다.
        /// </summary>
        public void SetInspectorRealtimeSyncActive( bool isActive )
        {
            isInspectorRealtimeSyncActive = isActive;

            if ( isActive == false )
            {
                return;
            }

            CacheCurrentSprite();
        }

        /// <summary>
        /// 스프라이트 변경 시에만 배경 크기를 다시 맞춘다.
        /// </summary>
        public void ApplyFitIfSpriteChanged()
        {
            SpriteRenderer resolvedSpriteRenderer = ResolveSpriteRenderer();

            if ( resolvedSpriteRenderer == null )
            {
                previousSprite = null;
                return;
            }

            Sprite currentSprite = resolvedSpriteRenderer.sprite;

            if ( ReferenceEquals( currentSprite, previousSprite ) )
            {
                return;
            }

            ApplyFit();
        }

        /// <summary>
        /// 사용할 카메라를 결정한다.
        /// </summary>
        private Camera ResolveCamera()
        {
            if ( targetCamera != null )
            {
                Camera explicitCamera = targetCamera;
                return explicitCamera;
            }

            Camera mainCamera = Camera.main;
            return mainCamera;
        }

        /// <summary>
        /// 사용할 스프라이트 렌더러를 결정한다.
        /// </summary>
        private SpriteRenderer ResolveSpriteRenderer()
        {
            if ( targetSpriteRenderer != null )
            {
                SpriteRenderer explicitSpriteRenderer = targetSpriteRenderer;
                return explicitSpriteRenderer;
            }

            SpriteRenderer localSpriteRenderer = GetComponent<SpriteRenderer>();
            return localSpriteRenderer;
        }

        /// <summary>
        /// 카메라 시야의 월드 기준 너비와 높이를 계산한다.
        /// </summary>
        private Vector2 CalculateTargetWorldSize( Camera cameraToUse )
        {
            float targetHeight = cameraToUse.orthographicSize * 2.0f;
            float targetWidth = targetHeight * cameraToUse.aspect;
            Vector2 result = new Vector2( targetWidth, targetHeight );
            return result;
        }

        /// <summary>
        /// 기준 스케일에서의 배경 월드 크기를 계산한다.
        /// </summary>
        private Vector2 CalculateBaseWorldSize( Sprite sprite )
        {
            Vector2 spriteSize = sprite.bounds.size;
            Vector3 parentLossyScale = GetParentLossyScale();
            float width = spriteSize.x * Mathf.Abs( baseLocalScale.x ) * parentLossyScale.x;
            float height = spriteSize.y * Mathf.Abs( baseLocalScale.y ) * parentLossyScale.y;
            Vector2 result = new Vector2( width, height );
            return result;
        }

        /// <summary>
        /// 부모 트랜스폼의 손실 스케일을 안전하게 가져온다.
        /// </summary>
        private Vector3 GetParentLossyScale()
        {
            Transform parentTransform = transform.parent;

            if ( parentTransform == null )
            {
                Vector3 noParentScale = Vector3.one;
                return noParentScale;
            }

            Vector3 lossyScale = parentTransform.lossyScale;
            lossyScale.x = Mathf.Max( MinimumParentScale, Mathf.Abs( lossyScale.x ) );
            lossyScale.y = Mathf.Max( MinimumParentScale, Mathf.Abs( lossyScale.y ) );
            lossyScale.z = Mathf.Max( MinimumParentScale, Mathf.Abs( lossyScale.z ) );
            return lossyScale;
        }

        /// <summary>
        /// 화면 맞춤에 필요한 배율을 계산한다.
        /// </summary>
        private float CalculateScaleMultiplier( Vector2 targetWorldSize, Vector2 baseWorldSize )
        {
            float widthRatio = targetWorldSize.x / baseWorldSize.x;
            float heightRatio = targetWorldSize.y / baseWorldSize.y;
            float result = fitMode == eBackgroundFitMode.COVER ? Mathf.Max( widthRatio, heightRatio ) : Mathf.Min( widthRatio, heightRatio );
            return result;
        }

        /// <summary>
        /// 부호 보존을 위한 기본 스케일 값을 정리한다.
        /// </summary>
        private float GetSignedScaleValue( float scaleValue )
        {
            bool isZero = Mathf.Approximately( scaleValue, 0.0f );

            if ( isZero )
            {
                float fallbackScale = 1.0f;
                return fallbackScale;
            }

            float result = scaleValue;
            return result;
        }

        /// <summary>
        /// 현재 스프라이트 상태를 감시 기준값으로 저장한다.
        /// </summary>
        private void CacheCurrentSprite()
        {
            SpriteRenderer resolvedSpriteRenderer = ResolveSpriteRenderer();

            if ( resolvedSpriteRenderer == null )
            {
                previousSprite = null;
                return;
            }

            previousSprite = resolvedSpriteRenderer.sprite;
        }
    }
}
