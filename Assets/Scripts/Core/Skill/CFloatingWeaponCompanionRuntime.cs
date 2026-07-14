using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 개별 부유 무기의 대기, 추적 공격, 복귀 상태 처리
    ///</summary>
    public sealed class CFloatingWeaponCompanionRuntime : MonoBehaviour
    {
        private enum eFloatingWeaponState
        {
            HOVERING,
            ATTACKING,
            REBOUNDING
        }

        private const int TrajectoryHitBufferSize = 32;
        private const float MinimumTrajectoryDuration = 0.05f;
        private const float MinimumReboundDistance = 2.25f;
        private const float ReboundDistanceRatio = 0.65f;
        private const float ReboundArcHeightRatio = 0.55f;

        private readonly RaycastHit2D[] trajectoryHitBuffer = new RaycastHit2D[ TrajectoryHitBufferSize ];
        private readonly HashSet<int> damagedMonsterIdSet = new HashSet<int>();
        private CFloatingWeaponSummonRuntime ownerRuntime;
        private SpriteRenderer weaponSpriteRenderer;
        private MonsterObject targetMonster;
        private eFloatingWeaponState currentState = eFloatingWeaponState.HOVERING;
        private int companionIndex;
        private int companionCount;
        private float hoverPhase;
        private float nextAttackTime;
        private Vector3 attackTravelDirection = Vector3.right;
        private Vector3 trajectoryStartPosition;
        private Vector3 trajectoryFirstControlPosition;
        private Vector3 trajectorySecondControlPosition;
        private Vector3 trajectoryEndPosition;
        private float trajectoryElapsedSeconds;
        private float trajectoryDurationSeconds;
        private int ignoredDepartureMonsterInstanceId;
        private bool isReturningOnCurrentTrajectory;

        ///<summary>
        /// 개별 부유 무기 초기화
        ///</summary>
        public void Initialize( CFloatingWeaponSummonRuntime _ownerRuntime, int _companionIndex, int _companionCount, float _hoverPhase, SpriteRenderer _weaponSpriteRenderer )
        {
            ownerRuntime = _ownerRuntime;
            companionIndex = _companionIndex;
            companionCount = Mathf.Max( 1, _companionCount );
            hoverPhase = _hoverPhase;
            weaponSpriteRenderer = _weaponSpriteRenderer;
            float staggerRatio = companionCount > 0 ? companionIndex / ( float ) companionCount : 0.0f;
            nextAttackTime = Time.time + ownerRuntime.GetAttackIntervalSeconds() * staggerRatio;
            ApplyFacingVisual();
        }

        ///<summary>
        /// 현재 공격 대상 반환
        ///</summary>
        public MonsterObject GetTarget()
        {
            MonsterObject result = targetMonster;
            return result;
        }

        ///<summary>
        /// 장착 무기 외형 반영
        ///</summary>
        public void SetWeaponSprite( Sprite _weaponSprite )
        {
            if ( weaponSpriteRenderer == null )
            {
                return;
            }

            weaponSpriteRenderer.sprite = _weaponSprite;
            weaponSpriteRenderer.enabled = _weaponSprite != null;
        }

        private void Update()
        {
            if ( ownerRuntime == null )
            {
                Destroy( gameObject );
                return;
            }

            switch ( currentState )
            {
                case eFloatingWeaponState.HOVERING:
                    UpdateHovering();
                    break;

                case eFloatingWeaponState.ATTACKING:
                    UpdateAttacking();
                    break;

                case eFloatingWeaponState.REBOUNDING:
                    UpdateRebounding();
                    break;
            }
        }

        private void UpdateHovering()
        {
            Vector3 formationPosition = ownerRuntime.GetFormationWorldPosition( companionIndex, companionCount, hoverPhase );
            transform.position = Vector3.MoveTowards( transform.position, formationPosition, ownerRuntime.GetFlightSpeed() * Time.deltaTime );
            transform.rotation = Quaternion.identity;
            ApplyFacingVisual();

            if ( Time.time < nextAttackTime || weaponSpriteRenderer == null || weaponSpriteRenderer.sprite == null )
            {
                return;
            }

            targetMonster = ownerRuntime.AcquireTarget( this );

            if ( targetMonster == null )
            {
                nextAttackTime = Time.time + ownerRuntime.GetTargetRetrySeconds();
                return;
            }

            BeginDamageTrajectory();
            currentState = eFloatingWeaponState.ATTACKING;
        }

        private void UpdateAttacking()
        {
            if ( ownerRuntime.IsValidTarget( targetMonster ) == false )
            {
                targetMonster = ownerRuntime.AcquireTarget( this );

                if ( targetMonster == null )
                {
                    BeginRebounding();
                    return;
                }
            }

            Vector3 targetPosition = targetMonster.transform.position;
            float flightDistance = ownerRuntime.GetFlightSpeed() * Time.deltaTime;
            Vector3 previousPosition = transform.position;
            Vector3 targetDirection = targetPosition - transform.position;

            if ( targetDirection.sqrMagnitude > 0.0001f )
            {
                attackTravelDirection = targetDirection.normalized;
            }

            transform.position = Vector3.MoveTowards( transform.position, targetPosition, flightDistance );
            ApplyTrajectoryDamage( previousPosition, transform.position );
            float rotationAmount = ownerRuntime.GetAttackRotationSpeed() * Time.deltaTime;
            transform.Rotate( 0.0f, 0.0f, rotationAmount );
            float hitRadius = ownerRuntime.GetHitRadius();

            if ( ( transform.position - targetPosition ).sqrMagnitude > hitRadius * hitRadius )
            {
                return;
            }

            BeginRebounding();
        }

        ///<summary>
        /// 관통 방향으로 뻗은 뒤 다음 대상 또는 편대로 휘어지는 부메랑 이동
        ///</summary>
        private void UpdateRebounding()
        {
            if ( isReturningOnCurrentTrajectory == false && ownerRuntime.IsValidTarget( targetMonster ) == false )
            {
                BeginRebounding();
                return;
            }

            trajectoryEndPosition = ResolveTrajectoryEndPosition();
            trajectoryElapsedSeconds += Time.deltaTime;
            float trajectoryRatio = Mathf.Clamp01( trajectoryElapsedSeconds / trajectoryDurationSeconds );
            Vector3 previousPosition = transform.position;
            Vector3 nextPosition = EvaluateCubicBezier( trajectoryStartPosition, trajectoryFirstControlPosition, trajectorySecondControlPosition, trajectoryEndPosition, trajectoryRatio );
            Vector3 movementDirection = nextPosition - previousPosition;

            if ( movementDirection.sqrMagnitude > 0.0001f )
            {
                attackTravelDirection = movementDirection.normalized;
            }

            transform.position = nextPosition;
            ApplyTrajectoryDamage( previousPosition, nextPosition );
            float rotationAmount = ownerRuntime.GetAttackRotationSpeed() * Time.deltaTime;
            transform.Rotate( 0.0f, 0.0f, rotationAmount );

            if ( trajectoryRatio < 1.0f )
            {
                return;
            }

            if ( isReturningOnCurrentTrajectory )
            {
                CompleteReturnToFormation();
                return;
            }

            BeginRebounding();
        }

        ///<summary>
        /// 대상 타격과 타격 FX 처리
        ///</summary>
        private void ApplyHit( MonsterObject _monsterObject )
        {
            if ( ownerRuntime.IsValidTarget( _monsterObject ) == false )
            {
                return;
            }

            CSkillContext skillContext = ownerRuntime.GetSkillContext();
            bool wasAliveBeforeHit = _monsterObject.GetCurrentHp() > 0;
            long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( skillContext, _monsterObject, ownerRuntime.GetDamageMultiplier(), ownerRuntime.GetFlatDamageBonus(), out bool isCritical );
            _monsterObject.TakeDamage( damage, isCritical );
            CSkillVfxUtility.PlayHitVfxAtWorldPosition( skillContext, transform.position );
            CSkillAudioUtility.PlayHitSfx( skillContext );
            CSkillDamageUtility.TryAwardMonsterExp( skillContext, _monsterObject, wasAliveBeforeHit );
        }

        ///<summary>
        /// 다음 공격을 위한 부메랑 반동 이동 시작
        ///</summary>
        private void BeginRebounding()
        {
            Vector3 formationPosition = ownerRuntime.GetFormationWorldPosition( companionIndex, companionCount, hoverPhase );
            float distanceToFormation = Vector3.Distance( transform.position, formationPosition );
            float reboundDistance = Mathf.Max( MinimumReboundDistance, distanceToFormation * ReboundDistanceRatio );
            int departureMonsterInstanceId = ownerRuntime.IsValidTarget( targetMonster ) ? targetMonster.GetInstanceID() : 0;
            trajectoryStartPosition = transform.position;
            trajectoryFirstControlPosition = trajectoryStartPosition + attackTravelDirection * reboundDistance;
            targetMonster = ownerRuntime.AcquireTarget( this );
            isReturningOnCurrentTrajectory = targetMonster == null;
            trajectoryEndPosition = ResolveTrajectoryEndPosition();
            float arcSide = companionIndex % 2 == 0 ? 1.0f : -1.0f;
            Vector3 arcDirection = new Vector3( -attackTravelDirection.y, attackTravelDirection.x, 0.0f ) * arcSide;
            trajectorySecondControlPosition = trajectoryFirstControlPosition + arcDirection * reboundDistance * ReboundArcHeightRatio;
            float firstLegDistance = Vector3.Distance( trajectoryStartPosition, trajectoryFirstControlPosition );
            float secondLegDistance = Vector3.Distance( trajectoryFirstControlPosition, trajectorySecondControlPosition );
            float thirdLegDistance = Vector3.Distance( trajectorySecondControlPosition, trajectoryEndPosition );
            float trajectoryDistance = firstLegDistance + secondLegDistance + thirdLegDistance;
            trajectoryDurationSeconds = Mathf.Max( MinimumTrajectoryDuration, trajectoryDistance / ownerRuntime.GetFlightSpeed() );
            trajectoryElapsedSeconds = 0.0f;
            BeginDamageTrajectory( departureMonsterInstanceId );
            currentState = eFloatingWeaponState.REBOUNDING;
        }

        private void ApplyTrajectoryDamage( Vector3 _previousPosition, Vector3 _currentPosition )
        {
            Vector2 castOrigin = _previousPosition;
            Vector2 movement = _currentPosition - _previousPosition;
            float movementDistance = movement.magnitude;

            if ( movementDistance <= 0.0001f )
            {
                return;
            }

            Vector2 castDirection = movement / movementDistance;
            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            int hitCount = Physics2D.CircleCast( castOrigin, ownerRuntime.GetHitRadius(), castDirection, contactFilter, trajectoryHitBuffer, movementDistance );
            float trajectoryRatio = trajectoryDurationSeconds > 0.0f ? trajectoryElapsedSeconds / trajectoryDurationSeconds : 1.0f;

            for ( int index = 0; index < hitCount; index++ )
            {
                RaycastHit2D trajectoryHit = trajectoryHitBuffer[ index ];
                MonsterObject monsterObject = ResolveMonsterObject( trajectoryHit.collider );

                if ( ownerRuntime.IsValidTarget( monsterObject ) == false )
                {
                    continue;
                }

                int monsterInstanceId = monsterObject.GetInstanceID();

                if ( monsterInstanceId == ignoredDepartureMonsterInstanceId && trajectoryRatio < 0.5f )
                {
                    continue;
                }

                if ( damagedMonsterIdSet.Add( monsterInstanceId ) == false )
                {
                    continue;
                }

                ApplyHit( monsterObject );
            }
        }

        private void BeginDamageTrajectory( int _ignoredDepartureMonsterInstanceId = 0 )
        {
            damagedMonsterIdSet.Clear();
            ignoredDepartureMonsterInstanceId = _ignoredDepartureMonsterInstanceId;
        }

        private Vector3 ResolveTrajectoryEndPosition()
        {
            if ( isReturningOnCurrentTrajectory == false && ownerRuntime.IsValidTarget( targetMonster ) )
            {
                Vector3 targetPosition = targetMonster.transform.position;
                return targetPosition;
            }

            Vector3 result = ownerRuntime.GetFormationWorldPosition( companionIndex, companionCount, hoverPhase );
            return result;
        }

        private void CompleteReturnToFormation()
        {
            Vector3 formationPosition = ownerRuntime.GetFormationWorldPosition( companionIndex, companionCount, hoverPhase );
            transform.position = formationPosition;
            transform.rotation = Quaternion.identity;
            targetMonster = null;
            ApplyFacingVisual();
            currentState = eFloatingWeaponState.HOVERING;
            nextAttackTime = Time.time + ownerRuntime.GetTargetRetrySeconds();
        }

        private static Vector3 EvaluateCubicBezier( Vector3 _startPosition, Vector3 _firstControlPosition, Vector3 _secondControlPosition, Vector3 _endPosition, float _ratio )
        {
            float inverseRatio = 1.0f - _ratio;
            float inverseRatioSquared = inverseRatio * inverseRatio;
            float ratioSquared = _ratio * _ratio;
            Vector3 result = inverseRatioSquared * inverseRatio * _startPosition
                + 3.0f * inverseRatioSquared * _ratio * _firstControlPosition
                + 3.0f * inverseRatio * ratioSquared * _secondControlPosition
                + ratioSquared * _ratio * _endPosition;
            return result;
        }

        private static ContactFilter2D CreateMonsterContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.useTriggers = true;
            contactFilter.layerMask = LayerMask.GetMask( "Monster" );
            return contactFilter;
        }

        private static MonsterObject ResolveMonsterObject( Collider2D _targetCollider )
        {
            if ( _targetCollider == null )
            {
                return null;
            }

            MonsterObject monsterObject = _targetCollider.GetComponent<MonsterObject>();

            if ( monsterObject != null )
            {
                return monsterObject;
            }

            MonsterObject result = _targetCollider.GetComponentInParent<MonsterObject>();
            return result;
        }

        private void ApplyFacingVisual()
        {
            if ( weaponSpriteRenderer == null || ownerRuntime == null )
            {
                return;
            }

            weaponSpriteRenderer.flipX = ownerRuntime.ResolveFacingDirection() < 0.0f;
        }
    }
}
