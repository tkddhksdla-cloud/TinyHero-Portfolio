using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 월드 공간 배경 맞춤 컴포넌트
    ///</summary>
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

        ///<summary>
        /// 기본 참조 재설정
        ///</summary>
        private void Reset()
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
            baseLocalScale = transform.localScale;
            CacheCurrentSprite();
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            if ( applyOnEnable == false )
            {
                CacheCurrentSprite();
                return;
            }

            ApplyFit();
        }

        ///<summary>
        /// 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            isInspectorRealtimeSyncActive = false;
        }

        ///<summary>
        /// 인스펙터 값 검증
        ///</summary>
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

        ///<summary>
        /// 후처리 갱신
        ///</summary>
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

        ///<summary>
        /// 기준 스케일 저장
        ///</summary>
        [ContextMenu( "Capture Base Scale" )]
        public void CaptureBaseScale()
        {
            baseLocalScale = transform.localScale;
        }

        ///<summary>
        /// 배경 맞춤 적용
        ///</summary>
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

        ///<summary>
        /// 인스펙터 실시간 동기화 활성 상태 설정
        ///</summary>
        public void SetInspectorRealtimeSyncActive(bool _isActive)
        {
            isInspectorRealtimeSyncActive = _isActive;

            if ( _isActive == false )
            {
                return;
            }

            CacheCurrentSprite();
        }

        ///<summary>
        /// 맞춤 조건부 스프라이트 변경 적용
        ///</summary>
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

        ///<summary>
        /// 카메라 결정
        ///</summary>
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

        ///<summary>
        /// 스프라이트 렌더러 결정
        ///</summary>
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

        ///<summary>
        /// 대상 월드 크기 계산
        ///</summary>
        private Vector2 CalculateTargetWorldSize(Camera _cameraToUse)
        {
            float targetHeight = _cameraToUse.orthographicSize * 2.0f;
            float targetWidth = targetHeight * _cameraToUse.aspect;
            Vector2 result = new Vector2( targetWidth, targetHeight );
            return result;
        }

        ///<summary>
        /// 기준 월드 크기 계산
        ///</summary>
        private Vector2 CalculateBaseWorldSize(Sprite _sprite)
        {
            Vector2 spriteSize = _sprite.bounds.size;
            Vector3 parentLossyScale = GetParentLossyScale();
            float width = spriteSize.x * Mathf.Abs( baseLocalScale.x ) * parentLossyScale.x;
            float height = spriteSize.y * Mathf.Abs( baseLocalScale.y ) * parentLossyScale.y;
            Vector2 result = new Vector2( width, height );
            return result;
        }

        ///<summary>
        /// 부모 손실 스케일 반환
        ///</summary>
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

        ///<summary>
        /// 스케일 배율 계산
        ///</summary>
        private float CalculateScaleMultiplier(Vector2 _targetWorldSize, Vector2 _baseWorldSize)
        {
            float widthRatio = _targetWorldSize.x / _baseWorldSize.x;
            float heightRatio = _targetWorldSize.y / _baseWorldSize.y;
            float result = fitMode == eBackgroundFitMode.COVER ? Mathf.Max( widthRatio, heightRatio ) : Mathf.Min( widthRatio, heightRatio );
            return result;
        }

        ///<summary>
        /// 부호 스케일 값 반환
        ///</summary>
        private float GetSignedScaleValue(float _scaleValue)
        {
            bool isZero = Mathf.Approximately( _scaleValue, 0.0f );

            if ( isZero )
            {
                float fallbackScale = 1.0f;
                return fallbackScale;
            }

            float result = _scaleValue;
            return result;
        }

        ///<summary>
        /// 현재 스프라이트 캐시
        ///</summary>
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


