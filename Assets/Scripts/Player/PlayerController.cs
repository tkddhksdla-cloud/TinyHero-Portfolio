using TinyHero.Core;
using System.Collections;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 제어 컴포넌트
    ///</summary>
    [RequireComponent( typeof( Rigidbody2D ) )]
    public sealed class PlayerController : MonoBehaviour
    {
        ///<summary>
        /// 플레이어 상태 정의
        ///</summary>
        public enum ePlayerState
        {
            Idle,
            Move,
            Attack,
            Jump,
            Hit,
            Die
        }

        private const float DefaultGravityScale = 1.0f;
        private const float GroundDetachVelocityThreshold = 0.01f;
        private const string IdleAnimationStateName = "Idle";
        private const string MoveAnimationStateName = "Move";
        private const string AttackAnimationStateName = "Attack";
        private const string HitAnimationStateName = "Hit";
        private const string DieAnimationStateName = "Die";

        [Header( "References" )]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Collider2D[] targetColliders;

        [Header( "Movement" )]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float jumpPower = 8.5f;
        [SerializeField] private float doubleJumpForwardPower = 8.5f;
        [SerializeField] private float doubleJumpUpwardPower = 8.5f;
        [SerializeField] private float verticalDoubleJumpUpwardPower = 10.5f;
        [SerializeField] private float airHorizontalForce = 0.8f;
        [SerializeField] private float airHorizontalMaxSpeed = 3.0f;
        [SerializeField] private float risingGravityScale = 2.2f;
        [SerializeField] private float fallingGravityScale = 3.8f;
        [SerializeField] private int maxJumpCount = 2;

        [Header( "Ground Check" )]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private float groundCheckRadius = 0.12f;

        [Header( "Combat" )]
        [SerializeField] private float attackDuration = 0.2f;
        [SerializeField] private float hitStateDuration = 0.25f;
        [SerializeField] private float hitKnockbackDistance = 0.45f;
        [SerializeField] private float invincibilityDuration = 1.25f;
        [SerializeField] private float invincibilityBlinkInterval = 0.1f;
        [SerializeField] private Color invincibilityTintColor = new Color( 0.35f, 0.35f, 0.35f, 1.0f );
        [SerializeField] private SpriteRenderer[] targetSpriteRenderers;

        private ePlayerState currentState = ePlayerState.Idle;
        private float horizontalInput;
        private float attackElapsedTime;
        private float defaultScaleX;
        private float hitElapsedTime;
        private float hitReactionDirection = -1.0f;
        private int currentJumpCount;
        private Color[] defaultSpriteColors;
        private Coroutine hitReactionRoutine;
        private Coroutine invincibilityRoutine;
        private readonly Collider2D[] overlapResultBuffer = new Collider2D[ 16 ];
        private bool isGrounded;
        private bool isHitReactionActive;
        private bool isInvincible;
        private bool isJumpHeld;
        private bool isInteractionHeld;
        private bool isPendingJump;
        private bool isPendingAttack;
        private bool wasGrounded;

        ///<summary>
        /// 현재 상태 정보
        ///</summary>
        public ePlayerState CurrentState
        {
            get
            {
                ePlayerState result = currentState;
                return result;
            }
        }

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            defaultScaleX = Mathf.Abs( transform.localScale.x );

            if ( defaultScaleX <= 0.0f )
            {
                defaultScaleX = 1.0f;
            }

            if ( targetAnimator == null )
            {
                Animator resolvedAnimator = GetComponent<Animator>();
                targetAnimator = resolvedAnimator;
            }

            if ( targetRigidbody == null )
            {
                Rigidbody2D resolvedRigidbody = GetComponent<Rigidbody2D>();
                targetRigidbody = resolvedRigidbody;
            }

            if ( targetRigidbody != null )
            {
                targetRigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }

            CacheTargetColliders();
            CacheSpriteRenderers();
            CacheDefaultSpriteColors();
        }

        ///<summary>
        /// 초기 상태 설정
        ///</summary>
        private void Start()
        {
            currentState = ePlayerState.Die;
            ChangeState( ePlayerState.Idle );
        }

        ///<summary>
        /// 프레임 상태 처리
        ///</summary>
        private void Update()
        {
            if ( targetRigidbody == null )
            {
                return;
            }

            CaptureInput();
            UpdateGroundState();
            ProcessStateTransitions();
            UpdateCurrentState();
            EvaluateMonsterContactOverlap();
            ClearOneShotInputs();
        }

        ///<summary>
        /// 물리 상태 처리
        ///</summary>
        private void FixedUpdate()
        {
            if ( targetRigidbody == null )
            {
                return;
            }

            ApplyFacingDirection();
            ApplyHorizontalMovement();
            ApplyJumpGravity();
        }

        ///<summary>
        /// 종료 시각 효과 복원
        ///</summary>
        private void OnDestroy()
        {
            StopHitReaction();
            RestoreSpriteColors();
        }

        ///<summary>
        /// 사망 상태 전환
        ///</summary>
        public void Die()
        {
            ChangeState( ePlayerState.Die );
        }

        ///<summary>
        /// 피격 상태 전환
        ///</summary>
        public void Hit()
        {
            ChangeState( ePlayerState.Hit );
        }

        ///<summary>
        /// 접촉 피격 처리
        ///</summary>
        public bool TryReceiveContactHit()
        {
            if ( currentState == ePlayerState.Die || isInvincible )
            {
                return false;
            }

            hitReactionDirection = -ResolveFacingDirection();
            Hit();
            BeginInvincibility();
            return true;
        }

        ///<summary>
        /// 입력 수집
        ///</summary>
        private void CaptureInput()
        {
            if ( isHitReactionActive )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                return;
            }

            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager == null )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isInteractionHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                return;
            }

            float capturedHorizontalInput = inputManager.GetHorizontalInput();
            bool capturedJumpHeld = inputManager.GetJumpHeld();
            bool capturedInteractionHeld = inputManager.GetInteractionHeld();
            bool capturedJumpDown = inputManager.GetJumpDown();
            bool capturedAttackDown = inputManager.GetAttackDown();

            horizontalInput = capturedHorizontalInput;
            isJumpHeld = capturedJumpHeld;
            isInteractionHeld = capturedInteractionHeld;
            isPendingJump = capturedJumpDown;
            isPendingAttack = capturedAttackDown;
        }

        ///<summary>
        /// 지상 상태 갱신
        ///</summary>
        private void UpdateGroundState()
        {
            Vector2 groundCheckPosition = GetGroundCheckPosition();
            Collider2D hitCollider = Physics2D.OverlapCircle( groundCheckPosition, groundCheckRadius, groundLayerMask );
            bool currentGroundedState = hitCollider != null;
            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > GroundDetachVelocityThreshold )
            {
                currentGroundedState = false;
            }

            wasGrounded = isGrounded;
            isGrounded = currentGroundedState;

            if ( isGrounded && wasGrounded == false )
            {
                currentJumpCount = 0;
            }

            if ( isGrounded == false && wasGrounded && targetRigidbody.linearVelocity.y <= 0.0f )
            {
                currentJumpCount = 1;
            }
        }

        ///<summary>
        /// 상태 전환 조건 처리
        ///</summary>
        private void ProcessStateTransitions()
        {
            if ( currentState == ePlayerState.Die )
            {
                return;
            }

            if ( currentState == ePlayerState.Hit )
            {
                return;
            }

            if ( isPendingAttack && isGrounded )
            {
                ChangeState( ePlayerState.Attack );
                return;
            }

            if ( isPendingJump && CanJump() )
            {
                bool didJump = TryExecuteJump();

                if ( didJump )
                {
                    ChangeState( ePlayerState.Jump );
                }

                return;
            }

            if ( currentState == ePlayerState.Jump && isGrounded )
            {
                ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
                ChangeState( nextGroundedState );
                return;
            }

            if ( currentState == ePlayerState.Attack && isGrounded == false )
            {
                ChangeState( ePlayerState.Jump );
                return;
            }

        }

        ///<summary>
        /// 현재 상태 갱신
        ///</summary>
        private void UpdateCurrentState()
        {
            switch ( currentState )
            {
                case ePlayerState.Idle:
                    UpdateIdleState();
                    break;

                case ePlayerState.Move:
                    UpdateMoveState();
                    break;

                case ePlayerState.Attack:
                    UpdateAttackState();
                    break;

                case ePlayerState.Jump:
                    UpdateJumpState();
                    break;

                case ePlayerState.Hit:
                    UpdateHitState();
                    break;

                case ePlayerState.Die:
                    UpdateDieState();
                    break;
            }
        }

        ///<summary>
        /// 상태 변경
        ///</summary>
        private void ChangeState( ePlayerState _nextState )
        {
            if ( currentState == _nextState )
            {
                ApplyAnimationState();
                return;
            }

            currentState = _nextState;

            switch ( currentState )
            {
                case ePlayerState.Idle:
                    EnterIdleState();
                    break;

                case ePlayerState.Move:
                    EnterMoveState();
                    break;

                case ePlayerState.Attack:
                    EnterAttackState();
                    break;

                case ePlayerState.Jump:
                    EnterJumpState();
                    break;

                case ePlayerState.Hit:
                    EnterHitState();
                    break;

                case ePlayerState.Die:
                    EnterDieState();
                    break;
            }

            ApplyAnimationState();
        }

        ///<summary>
        /// 대기 상태 진입 처리
        ///</summary>
        private void EnterIdleState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 이동 상태 진입 처리
        ///</summary>
        private void EnterMoveState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 공격 상태 진입 처리
        ///</summary>
        private void EnterAttackState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 점프 상태 진입 처리
        ///</summary>
        private void EnterJumpState()
        {
        }

        ///<summary>
        /// 피격 상태 진입 처리
        ///</summary>
        private void EnterHitState()
        {
            attackElapsedTime = 0.0f;
            hitElapsedTime = 0.0f;

            if ( targetRigidbody != null )
            {
                targetRigidbody.linearVelocity = Vector2.zero;
            }

            StartHitReaction();
        }

        ///<summary>
        /// 사망 상태 진입 처리
        ///</summary>
        private void EnterDieState()
        {
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.x = 0.0f;
            targetRigidbody.linearVelocity = currentVelocity;
            targetRigidbody.gravityScale = DefaultGravityScale;
        }

        ///<summary>
        /// 대기 상태 갱신
        ///</summary>
        private void UpdateIdleState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.Jump );
                return;
            }

            if ( HasHorizontalInput() )
            {
                ChangeState( ePlayerState.Move );
            }
        }

        ///<summary>
        /// 이동 상태 갱신
        ///</summary>
        private void UpdateMoveState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.Jump );
                return;
            }

            if ( HasHorizontalInput() == false )
            {
                ChangeState( ePlayerState.Idle );
            }
        }

        ///<summary>
        /// 공격 상태 갱신
        ///</summary>
        private void UpdateAttackState()
        {
            attackElapsedTime += Time.deltaTime;

            if ( attackElapsedTime < attackDuration )
            {
                return;
            }

            ePlayerState nextState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.Jump;
            ChangeState( nextState );
        }

        ///<summary>
        /// 점프 상태 갱신
        ///</summary>
        private void UpdateJumpState()
        {
            ApplyAnimationState();

            if ( isGrounded == false )
            {
                return;
            }

            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > 0.01f )
            {
                return;
            }

            ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
            ChangeState( nextGroundedState );
        }

        ///<summary>
        /// 피격 상태 갱신
        ///</summary>
        private void UpdateHitState()
        {
            hitElapsedTime += Time.deltaTime;

            if ( hitElapsedTime < hitStateDuration || isHitReactionActive )
            {
                return;
            }

            ePlayerState nextGroundedState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.Jump;
            ChangeState( nextGroundedState );
        }

        ///<summary>
        /// 사망 상태 갱신
        ///</summary>
        private void UpdateDieState()
        {
        }

        ///<summary>
        /// 수평 이동 적용
        ///</summary>
        private void ApplyHorizontalMovement()
        {
            if ( currentState == ePlayerState.Die || currentState == ePlayerState.Hit )
            {
                return;
            }

            if ( isGrounded == false )
            {
                ApplyAirHorizontalForce();
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            float horizontalVelocity = horizontalInput * moveSpeed;
            currentVelocity.x = horizontalVelocity;
            targetRigidbody.linearVelocity = currentVelocity;
        }

        ///<summary>
        /// 바라보기 방향 적용
        ///</summary>
        private void ApplyFacingDirection()
        {
            if ( currentState == ePlayerState.Hit )
            {
                return;
            }

            if ( Mathf.Approximately( horizontalInput, 0.0f ) )
            {
                return;
            }

            Vector3 localScale = transform.localScale;
            float facingScaleX = horizontalInput > 0.0f ? defaultScaleX : -defaultScaleX;
            localScale.x = facingScaleX;
            transform.localScale = localScale;
        }

        ///<summary>
        /// 점프 중력 적용
        ///</summary>
        private void ApplyJumpGravity()
        {
            if ( currentState == ePlayerState.Die )
            {
                return;
            }

            if ( isGrounded )
            {
                targetRigidbody.gravityScale = DefaultGravityScale;
                return;
            }

            float verticalVelocity = targetRigidbody.linearVelocity.y;

            if ( verticalVelocity > 0.0f && isJumpHeld )
            {
                targetRigidbody.gravityScale = risingGravityScale;
                return;
            }

            targetRigidbody.gravityScale = fallingGravityScale;
        }

        ///<summary>
        /// 점프 가능 여부
        ///</summary>
        private bool CanJump()
        {
            bool canJump = currentJumpCount < maxJumpCount;
            return canJump;
        }

        ///<summary>
        /// 점프 실행 시도
        ///</summary>
        private bool TryExecuteJump()
        {
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            bool isDoubleJump = currentJumpCount > 0;

            if ( isDoubleJump )
            {
                currentVelocity.y = 0.0f;
                targetRigidbody.linearVelocity = currentVelocity;

                Vector2 doubleJumpForce = ResolveDoubleJumpForce();
                targetRigidbody.AddForce( doubleJumpForce, ForceMode2D.Impulse );
            }
            else
            {
                currentVelocity.y = jumpPower;
                targetRigidbody.linearVelocity = currentVelocity;
            }

            currentJumpCount++;
            isGrounded = false;
            return true;
        }

        ///<summary>
        /// 지상 체크 위치 반환
        ///</summary>
        private Vector2 GetGroundCheckPosition()
        {
            if ( groundCheckPoint != null )
            {
                Vector2 pointPosition = groundCheckPoint.position;
                return pointPosition;
            }

            Vector2 fallbackPosition = transform.position;
            return fallbackPosition;
        }

        ///<summary>
        /// 수평 입력 여부
        ///</summary>
        private bool HasHorizontalInput()
        {
            bool hasHorizontalInput = Mathf.Approximately( horizontalInput, 0.0f ) == false;
            return hasHorizontalInput;
        }

        ///<summary>
        /// 공중 수평 힘 적용
        ///</summary>
        private void ApplyAirHorizontalForce()
        {
            if ( HasHorizontalInput() == false )
            {
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            float inputDirection = Mathf.Sign( horizontalInput );
            float directionalVelocity = currentVelocity.x * inputDirection;

            if ( directionalVelocity >= airHorizontalMaxSpeed )
            {
                return;
            }

            Vector2 airForce = new Vector2( inputDirection * airHorizontalForce, 0.0f );
            targetRigidbody.AddForce( airForce, ForceMode2D.Force );
        }

        ///<summary>
        /// 더블 점프 힘 결정
        ///</summary>
        private Vector2 ResolveDoubleJumpForce()
        {
            if ( HasHorizontalInput() )
            {
                Vector2 inputForwardDoubleJumpForce = GetInputForwardDoubleJumpForce();
                return inputForwardDoubleJumpForce;
            }

            if ( isInteractionHeld )
            {
                Vector2 upwardDoubleJumpForce = GetUpwardDoubleJumpForce();
                return upwardDoubleJumpForce;
            }

            Vector2 facingForwardDoubleJumpForce = GetFacingForwardDoubleJumpForce();
            return facingForwardDoubleJumpForce;
        }

        ///<summary>
        /// 입력 전방 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetInputForwardDoubleJumpForce()
        {
            float inputDirection = Mathf.Sign( horizontalInput );
            float horizontalForce = inputDirection * doubleJumpForwardPower;
            Vector2 result = new Vector2( horizontalForce, doubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 바라보기 전방 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetFacingForwardDoubleJumpForce()
        {
            float facingDirection = ResolveFacingDirection();
            float horizontalForce = facingDirection * doubleJumpForwardPower;
            Vector2 result = new Vector2( horizontalForce, doubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 상향 더블 점프 힘 반환
        ///</summary>
        private Vector2 GetUpwardDoubleJumpForce()
        {
            Vector2 result = new Vector2( 0.0f, verticalDoubleJumpUpwardPower );
            return result;
        }

        ///<summary>
        /// 바라보기 방향 결정
        ///</summary>
        private float ResolveFacingDirection()
        {
            if ( Mathf.Approximately( horizontalInput, 0.0f ) == false )
            {
                float inputDirection = Mathf.Sign( horizontalInput );
                return inputDirection;
            }

            float facingDirection = Mathf.Sign( transform.localScale.x );

            if ( Mathf.Approximately( facingDirection, 0.0f ) )
            {
                facingDirection = 1.0f;
            }

            float result = facingDirection;
            return result;
        }

        ///<summary>
        /// 지상 이동 상태 결정
        ///</summary>
        private ePlayerState ResolveGroundedLocomotionState()
        {
            ePlayerState result = HasHorizontalInput() ? ePlayerState.Move : ePlayerState.Idle;
            return result;
        }

        ///<summary>
        /// 원샷 입력 정리
        ///</summary>
        private void ClearOneShotInputs()
        {
            isPendingJump = false;
            isPendingAttack = false;
        }

        ///<summary>
        /// 애니메이션 상태 적용
        ///</summary>
        private void ApplyAnimationState()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            string animationStateName = ResolveAnimationStateName();
            targetAnimator.Play( animationStateName );
        }

        ///<summary>
        /// 애니메이션 상태 이름 결정
        ///</summary>
        private string ResolveAnimationStateName()
        {
            string animationStateName = IdleAnimationStateName;

            switch ( currentState )
            {
                case ePlayerState.Idle:
                    animationStateName = IdleAnimationStateName;
                    break;

                case ePlayerState.Move:
                    animationStateName = MoveAnimationStateName;
                    break;

                case ePlayerState.Attack:
                    animationStateName = AttackAnimationStateName;
                    break;

                case ePlayerState.Jump:
                    animationStateName = HasHorizontalInput() ? MoveAnimationStateName : IdleAnimationStateName;
                    break;

                case ePlayerState.Hit:
                    animationStateName = HitAnimationStateName;
                    break;

                case ePlayerState.Die:
                    animationStateName = DieAnimationStateName;
                    break;
            }

            return animationStateName;
        }

        ///<summary>
        /// 무적 상태 시작
        ///</summary>
        private void BeginInvincibility()
        {
            if ( invincibilityRoutine != null )
            {
                StopCoroutine( invincibilityRoutine );
                invincibilityRoutine = null;
            }

            isInvincible = true;

            if ( invincibilityDuration <= 0.0f )
            {
                isInvincible = false;
                RestoreSpriteColors();
                return;
            }

            invincibilityRoutine = StartCoroutine( IE_HandleInvincibilityVisual() );
        }

        ///<summary>
        /// 피격 리액션 시작
        ///</summary>
        private void StartHitReaction()
        {
            StopHitReaction();
            isHitReactionActive = true;
            hitReactionRoutine = StartCoroutine( IE_PlayHitReaction() );
        }

        ///<summary>
        /// 피격 리액션 중단
        ///</summary>
        private void StopHitReaction()
        {
            if ( hitReactionRoutine != null )
            {
                StopCoroutine( hitReactionRoutine );
                hitReactionRoutine = null;
            }

            isHitReactionActive = false;
        }

        ///<summary>
        /// 피격 튕김 연출
        ///</summary>
        private IEnumerator IE_PlayHitReaction()
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + new Vector3( hitReactionDirection * hitKnockbackDistance, 0.0f, 0.0f );
            float elapsedTime = 0.0f;

            if ( hitStateDuration <= 0.0f )
            {
                transform.position = endPosition;
                isHitReactionActive = false;
                hitReactionRoutine = null;
                yield break;
            }

            while ( elapsedTime < hitStateDuration )
            {
                elapsedTime += Time.deltaTime;

                float normalizedTime = Mathf.Clamp01( elapsedTime / hitStateDuration );
                float easedTime = 1.0f - Mathf.Pow( 1.0f - normalizedTime, 2.0f );
                transform.position = Vector3.Lerp( startPosition, endPosition, easedTime );
                yield return null;
            }

            transform.position = endPosition;
            isHitReactionActive = false;
            hitReactionRoutine = null;
        }

        ///<summary>
        /// 무적 시각 효과 처리
        ///</summary>
        private IEnumerator IE_HandleInvincibilityVisual()
        {
            float elapsedTime = 0.0f;
            float blinkInterval = Mathf.Max( 0.01f, invincibilityBlinkInterval );

            while ( elapsedTime < invincibilityDuration )
            {
                bool useTint = Mathf.Repeat( elapsedTime, blinkInterval * 2.0f ) < blinkInterval;
                ApplyInvincibilityTint( useTint );
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            isInvincible = false;
            invincibilityRoutine = null;
            RestoreSpriteColors();
            EvaluateMonsterContactOverlap();
        }

        ///<summary>
        /// 플레이어 충돌체 캐시
        ///</summary>
        private void CacheTargetColliders()
        {
            if ( targetColliders != null && targetColliders.Length > 0 )
            {
                return;
            }

            Collider2D[] resolvedColliders = GetComponentsInChildren<Collider2D>( true );
            targetColliders = resolvedColliders;
        }

        ///<summary>
        /// 몬스터 접촉 중첩 검사
        ///</summary>
        private void EvaluateMonsterContactOverlap()
        {
            if ( currentState == ePlayerState.Die || isInvincible )
            {
                return;
            }

            if ( targetColliders == null || targetColliders.Length == 0 )
            {
                return;
            }

            ContactFilter2D contactFilter = BuildMonsterContactFilter();

            for ( int index = 0; index < targetColliders.Length; index++ )
            {
                Collider2D playerCollider = targetColliders[ index ];

                if ( playerCollider == null || playerCollider.enabled == false )
                {
                    continue;
                }

                int overlapCount = playerCollider.Overlap( contactFilter, overlapResultBuffer );

                if ( overlapCount <= 0 )
                {
                    continue;
                }

                bool didReceiveHit = TryHandleMonsterContactOverlap( overlapCount );

                if ( didReceiveHit )
                {
                    return;
                }
            }
        }

        ///<summary>
        /// 몬스터 접촉 필터 구성
        ///</summary>
        private ContactFilter2D BuildMonsterContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = false;
            contactFilter.useTriggers = true;
            return contactFilter;
        }

        ///<summary>
        /// 몬스터 접촉 중첩 처리
        ///</summary>
        private bool TryHandleMonsterContactOverlap( int _overlapCount )
        {
            for ( int overlapIndex = 0; overlapIndex < _overlapCount; overlapIndex++ )
            {
                Collider2D overlapCollider = overlapResultBuffer[ overlapIndex ];

                if ( overlapCollider == null )
                {
                    continue;
                }

                MonsterContactHitbox monsterContactHitbox = overlapCollider.GetComponent<MonsterContactHitbox>();

                if ( monsterContactHitbox == null )
                {
                    monsterContactHitbox = overlapCollider.GetComponentInParent<MonsterContactHitbox>();
                }

                if ( monsterContactHitbox == null )
                {
                    continue;
                }

                bool didReceiveHit = TryReceiveContactHit();
                return didReceiveHit;
            }

            return false;
        }

        ///<summary>
        /// 스프라이트 렌더러 캐시
        ///</summary>
        private void CacheSpriteRenderers()
        {
            if ( targetSpriteRenderers != null && targetSpriteRenderers.Length > 0 )
            {
                return;
            }

            SpriteRenderer[] resolvedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>( true );
            targetSpriteRenderers = resolvedSpriteRenderers;
        }

        ///<summary>
        /// 기본 스프라이트 색상 캐시
        ///</summary>
        private void CacheDefaultSpriteColors()
        {
            if ( targetSpriteRenderers == null || targetSpriteRenderers.Length == 0 )
            {
                defaultSpriteColors = new Color[ 0 ];
                return;
            }

            defaultSpriteColors = new Color[ targetSpriteRenderers.Length ];

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];
                defaultSpriteColors[ index ] = spriteRenderer != null ? spriteRenderer.color : Color.white;
            }
        }

        ///<summary>
        /// 무적 색상 적용
        ///</summary>
        private void ApplyInvincibilityTint( bool _useTint )
        {
            if ( targetSpriteRenderers == null || defaultSpriteColors == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                Color defaultColor = defaultSpriteColors[ index ];
                Color tintColor = Color.Lerp( defaultColor, invincibilityTintColor, 0.65f );
                Color appliedColor = _useTint ? tintColor : defaultColor;
                spriteRenderer.color = appliedColor;
            }
        }

        ///<summary>
        /// 스프라이트 색상 복원
        ///</summary>
        private void RestoreSpriteColors()
        {
            if ( targetSpriteRenderers == null || defaultSpriteColors == null )
            {
                return;
            }

            for ( int index = 0; index < targetSpriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = targetSpriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                spriteRenderer.color = defaultSpriteColors[ index ];
            }
        }

        ///<summary>
        /// 선택 기즈모 표시
        ///</summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 groundCheckPosition = GetGroundCheckPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere( groundCheckPosition, groundCheckRadius );
        }
    }
}



