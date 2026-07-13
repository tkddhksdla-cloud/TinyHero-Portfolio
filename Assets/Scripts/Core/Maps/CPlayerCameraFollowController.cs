using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 메인 카메라 플레이어 추적 제어
    ///</summary>
    [DisallowMultipleComponent]
    [RequireComponent( typeof( Camera ) )]
    public sealed class CPlayerCameraFollowController : MonoBehaviour
    {
        private const float DefaultFollowSmoothTime = 0.42f;
        private const float DefaultCameraZ = -10.0f;
        private const float DefaultHorizontalDeadZone = 0.8f;
        private const float DefaultVerticalDeadZone = 0.45f;
        private const float DefaultSnapDistance = 10.0f;
        private const float MinimumSmoothTime = 0.01f;

        [Header( "추적" )]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector2 followOffset = Vector2.zero;
        [SerializeField] private float smoothTime = DefaultFollowSmoothTime;
        [SerializeField] private Vector2 deadZoneSize = new Vector2( DefaultHorizontalDeadZone, DefaultVerticalDeadZone );
        [SerializeField] private float snapDistance = DefaultSnapDistance;
        [SerializeField] private bool clampToBackgroundBounds = true;

        private Camera targetCamera;
        private Vector3 followVelocity;
        private bool isFollowEnabled = true;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ResolveTargetTransform();
        }

        ///<summary>
        /// 후처리 카메라 위치 갱신
        ///</summary>
        private void LateUpdate()
        {
            if ( isFollowEnabled == false )
            {
                return;
            }

            if ( targetCamera == null )
            {
                targetCamera = GetComponent<Camera>();
            }

            if ( targetTransform == null )
            {
                ResolveTargetTransform();
            }

            if ( targetTransform == null )
            {
                return;
            }

            Vector3 desiredPosition = BuildDesiredCameraPosition();
            Vector3 clampedPosition = ClampPositionToBackgroundBounds( desiredPosition );
            ApplySmoothedCameraPosition( clampedPosition );
        }

        ///<summary>
        /// 추적 대상 직접 설정
        ///</summary>
        public void SetTarget( Transform _targetTransform )
        {
            targetTransform = _targetTransform;
            followVelocity = Vector3.zero;
        }

        ///<summary>
        /// 추적 대상 위치로 카메라 즉시 이동
        ///</summary>
        public void SnapToTargetImmediate()
        {
            if ( targetCamera == null )
            {
                targetCamera = GetComponent<Camera>();
            }

            if ( targetTransform == null )
            {
                ResolveTargetTransform();
            }

            if ( targetTransform == null )
            {
                return;
            }

            Vector3 desiredPosition = BuildDesiredCameraPosition();
            Vector3 clampedPosition = ClampPositionToBackgroundBounds( desiredPosition );
            transform.position = clampedPosition;
            followVelocity = Vector3.zero;
        }

        ///<summary>
        /// 카메라 추적 활성 상태 설정
        ///</summary>
        public void SetFollowEnabled( bool _isEnabled )
        {
            isFollowEnabled = _isEnabled;
            followVelocity = Vector3.zero;
        }

        ///<summary>
        /// 카메라 추적 활성 상태 반환
        ///</summary>
        public bool IsFollowEnabled()
        {
            bool result = isFollowEnabled;
            return result;
        }

        ///<summary>
        /// 추적 대상 결정
        ///</summary>
        private void ResolveTargetTransform()
        {
            CGameManager gameManager = CGameManager.Instance;
            gameManager.TryGetActivePlayerController( out PlayerController playerController );

            if ( playerController == null )
            {
                return;
            }

            targetTransform = playerController.transform;
        }

        ///<summary>
        /// 목표 카메라 위치 구성
        ///</summary>
        private Vector3 BuildDesiredCameraPosition()
        {
            Vector3 targetPosition = targetTransform.position;
            Vector3 desiredPosition = transform.position;
            desiredPosition.x = targetPosition.x + followOffset.x;
            desiredPosition.y = targetPosition.y + followOffset.y;
            desiredPosition.z = Mathf.Approximately( transform.position.z, 0.0f ) ? DefaultCameraZ : transform.position.z;
            return desiredPosition;
        }

        ///<summary>
        /// 데드존과 감속을 적용한 카메라 위치 반영
        ///</summary>
        private void ApplySmoothedCameraPosition( Vector3 _targetPosition )
        {
            Vector3 currentPosition = transform.position;
            Vector3 deadZoneAdjustedPosition = currentPosition;
            float halfDeadZoneWidth = Mathf.Max( 0.0f, deadZoneSize.x * 0.5f );
            float halfDeadZoneHeight = Mathf.Max( 0.0f, deadZoneSize.y * 0.5f );
            float deltaX = _targetPosition.x - currentPosition.x;
            float deltaY = _targetPosition.y - currentPosition.y;

            if ( Mathf.Abs( deltaX ) > halfDeadZoneWidth )
            {
                float signedDeadZoneX = Mathf.Sign( deltaX ) * halfDeadZoneWidth;
                deadZoneAdjustedPosition.x = _targetPosition.x - signedDeadZoneX;
            }

            if ( Mathf.Abs( deltaY ) > halfDeadZoneHeight )
            {
                float signedDeadZoneY = Mathf.Sign( deltaY ) * halfDeadZoneHeight;
                deadZoneAdjustedPosition.y = _targetPosition.y - signedDeadZoneY;
            }

            deadZoneAdjustedPosition.z = _targetPosition.z;
            Vector2 cameraDelta = new Vector2( _targetPosition.x - currentPosition.x, _targetPosition.y - currentPosition.y );

            if ( cameraDelta.sqrMagnitude >= snapDistance * snapDistance )
            {
                transform.position = deadZoneAdjustedPosition;
                followVelocity = Vector3.zero;
                return;
            }

            float resolvedSmoothTime = Mathf.Max( MinimumSmoothTime, smoothTime );
            Vector3 smoothedPosition = Vector3.SmoothDamp( currentPosition, deadZoneAdjustedPosition, ref followVelocity, resolvedSmoothTime );
            transform.position = smoothedPosition;
        }

        ///<summary>
        /// 배경 경계 기준 카메라 위치 제한
        ///</summary>
        private Vector3 ClampPositionToBackgroundBounds( Vector3 _desiredPosition )
        {
            if ( clampToBackgroundBounds == false || targetCamera == null || targetCamera.orthographic == false )
            {
                return _desiredPosition;
            }

            if ( TryResolveBackgroundBounds( out Bounds backgroundBounds ) == false )
            {
                return _desiredPosition;
            }

            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            float minX = backgroundBounds.min.x + halfWidth;
            float maxX = backgroundBounds.max.x - halfWidth;
            float minY = backgroundBounds.min.y + halfHeight;
            float maxY = backgroundBounds.max.y - halfHeight;
            Vector3 clampedPosition = _desiredPosition;

            if ( minX <= maxX )
            {
                clampedPosition.x = Mathf.Clamp( clampedPosition.x, minX, maxX );
            }

            if ( minY <= maxY )
            {
                clampedPosition.y = Mathf.Clamp( clampedPosition.y, minY, maxY );
            }

            return clampedPosition;
        }

        ///<summary>
        /// 배경 월드 경계 결정
        ///</summary>
        private bool TryResolveBackgroundBounds( out Bounds _bounds )
        {
            _bounds = default;
            CMapBackgroundLayoutController backgroundLayoutController = FindFirstObjectByType<CMapBackgroundLayoutController>();

            if ( backgroundLayoutController != null )
            {
                bool hasLayoutBounds = backgroundLayoutController.TryGetCombinedWorldBounds( out _bounds );

                if ( hasLayoutBounds )
                {
                    return true;
                }
            }

            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
            {
                return false;
            }

            bool hasBounds = mapManager.TryGetCurrentBackgroundBounds( out _bounds );
            return hasBounds;
        }
    }
}
