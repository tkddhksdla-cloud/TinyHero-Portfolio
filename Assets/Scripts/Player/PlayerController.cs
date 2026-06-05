using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어의 상태와 이동을 통합 관리한다.
    ///</summary>
    [RequireComponent( typeof( Rigidbody2D ) )]
    public sealed class PlayerController : MonoBehaviour
    {
        ///<summary>
        /// 플레이어의 행동 상태를 정의한다.
        ///</summary>
        public enum ePlayerState
        {
            IDLE,
            MOVE,
            ATTACK,
            JUMP,
            HIT,
            DIE
        }

        private const float DefaultGravityScale = 1.0f;
        private const float GroundDetachVelocityThreshold = 0.01f;
        private const string IdleAnimationParameterName = "IDLE";
        private const string MoveAnimationParameterName = "MOVE";
        private const string AttackAnimationParameterName = "ATTACK";
        private const string JumpAnimationParameterName = "JUMP";
        private const string HitAnimationParameterName = "HIT";
        private const string DieAnimationParameterName = "DIE";

        [Header( "References" )]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private Transform groundCheckPoint;

        [Header( "Movement" )]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float jumpPower = 8.5f;
        [SerializeField] private float risingGravityScale = 2.2f;
        [SerializeField] private float fallingGravityScale = 3.8f;
        [SerializeField] private int maxJumpCount = 2;

        [Header( "Ground Check" )]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private float groundCheckRadius = 0.12f;

        [Header( "Combat" )]
        [SerializeField] private float attackDuration = 0.2f;

        private ePlayerState currentState = ePlayerState.IDLE;
        private float horizontalInput;
        private float attackElapsedTime;
        private float defaultScaleX;
        private int currentJumpCount;
        private bool isGrounded;
        private bool isJumpHeld;
        private bool isPendingJump;
        private bool isPendingAttack;
        private bool wasGrounded;

        ///<summary>
        /// 현재 플레이어 상태를 반환한다.
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
        /// 컴포넌트 참조를 초기화한다.
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
        }

        ///<summary>
        /// 시작 상태를 설정한다.
        ///</summary>
        private void Start()
        {
            currentState = ePlayerState.DIE;
            ChangeState( ePlayerState.IDLE );
        }

        ///<summary>
        /// 입력과 상태 전환을 갱신한다.
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
            ClearOneShotInputs();
        }

        ///<summary>
        /// 물리 이동과 점프 가속을 적용한다.
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
        /// 플레이어를 사망 상태로 전환한다.
        ///</summary>
        public void Die()
        {
            ChangeState( ePlayerState.DIE );
        }

        ///<summary>
        /// 플레이어를 피격 상태로 전환한다.
        ///</summary>
        public void Hit()
        {
            ChangeState( ePlayerState.HIT );
        }

        ///<summary>
        /// 입력 매니저로부터 현재 프레임 입력을 가져온다.
        ///</summary>
        private void CaptureInput()
        {
            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager == null )
            {
                horizontalInput = 0.0f;
                isJumpHeld = false;
                isPendingJump = false;
                isPendingAttack = false;
                return;
            }

            float capturedHorizontalInput = inputManager.GetHorizontalInput();
            bool capturedJumpHeld = inputManager.GetJumpHeld();
            bool capturedJumpDown = inputManager.GetJumpDown();
            bool capturedAttackDown = inputManager.GetAttackDown();

            horizontalInput = capturedHorizontalInput;
            isJumpHeld = capturedJumpHeld;
            isPendingJump = capturedJumpDown;
            isPendingAttack = capturedAttackDown;
        }

        ///<summary>
        /// 바닥 접촉 상태와 점프 횟수를 갱신한다.
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
        /// 상태 전환 조건을 평가한다.
        ///</summary>
        private void ProcessStateTransitions()
        {
            if ( currentState == ePlayerState.DIE )
            {
                return;
            }

            if ( currentState == ePlayerState.HIT && isGrounded == false )
            {
                ChangeState( ePlayerState.JUMP );
                return;
            }

            if ( isPendingAttack && isGrounded )
            {
                ChangeState( ePlayerState.ATTACK );
                return;
            }

            if ( isPendingJump && CanJump() )
            {
                ExecuteJump();
                ChangeState( ePlayerState.JUMP );
                return;
            }

            if ( currentState == ePlayerState.JUMP && isGrounded )
            {
                ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
                ChangeState( nextGroundedState );
                return;
            }

            if ( currentState == ePlayerState.ATTACK && isGrounded == false )
            {
                ChangeState( ePlayerState.JUMP );
                return;
            }

            if ( currentState == ePlayerState.HIT && isGrounded )
            {
                ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
                ChangeState( nextGroundedState );
            }
        }

        ///<summary>
        /// 현재 상태에 맞는 프레임 로직을 실행한다.
        ///</summary>
        private void UpdateCurrentState()
        {
            switch ( currentState )
            {
                case ePlayerState.IDLE:
                    UpdateIdleState();
                    break;

                case ePlayerState.MOVE:
                    UpdateMoveState();
                    break;

                case ePlayerState.ATTACK:
                    UpdateAttackState();
                    break;

                case ePlayerState.JUMP:
                    UpdateJumpState();
                    break;

                case ePlayerState.HIT:
                    UpdateHitState();
                    break;

                case ePlayerState.DIE:
                    UpdateDieState();
                    break;
            }
        }

        ///<summary>
        /// 상태 전환 후 초기화를 수행한다.
        ///</summary>
        private void ChangeState( ePlayerState nextState )
        {
            if ( currentState == nextState )
            {
                return;
            }

            currentState = nextState;

            switch ( currentState )
            {
                case ePlayerState.IDLE:
                    EnterIdleState();
                    break;

                case ePlayerState.MOVE:
                    EnterMoveState();
                    break;

                case ePlayerState.ATTACK:
                    EnterAttackState();
                    break;

                case ePlayerState.JUMP:
                    EnterJumpState();
                    break;

                case ePlayerState.HIT:
                    EnterHitState();
                    break;

                case ePlayerState.DIE:
                    EnterDieState();
                    break;
            }

            ApplyAnimationState();
        }

        ///<summary>
        /// 기본 대기 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterIdleState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 이동 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterMoveState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 공격 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterAttackState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 점프 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterJumpState()
        {
        }

        ///<summary>
        /// 피격 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterHitState()
        {
            attackElapsedTime = 0.0f;
        }

        ///<summary>
        /// 사망 상태 진입 처리를 수행한다.
        ///</summary>
        private void EnterDieState()
        {
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.x = 0.0f;
            targetRigidbody.linearVelocity = currentVelocity;
            targetRigidbody.gravityScale = DefaultGravityScale;
        }

        ///<summary>
        /// 기본 대기 상태를 갱신한다.
        ///</summary>
        private void UpdateIdleState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.JUMP );
                return;
            }

            if ( HasHorizontalInput() )
            {
                ChangeState( ePlayerState.MOVE );
            }
        }

        ///<summary>
        /// 이동 상태를 갱신한다.
        ///</summary>
        private void UpdateMoveState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.JUMP );
                return;
            }

            if ( HasHorizontalInput() == false )
            {
                ChangeState( ePlayerState.IDLE );
            }
        }

        ///<summary>
        /// 공격 상태를 갱신한다.
        ///</summary>
        private void UpdateAttackState()
        {
            attackElapsedTime += Time.deltaTime;

            if ( attackElapsedTime < attackDuration )
            {
                return;
            }

            ePlayerState nextState = isGrounded ? ResolveGroundedLocomotionState() : ePlayerState.JUMP;
            ChangeState( nextState );
        }

        ///<summary>
        /// 점프 상태를 갱신한다.
        ///</summary>
        private void UpdateJumpState()
        {
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
        /// 피격 상태를 갱신한다.
        ///</summary>
        private void UpdateHitState()
        {
            if ( isGrounded == false )
            {
                ChangeState( ePlayerState.JUMP );
                return;
            }

            ePlayerState nextGroundedState = ResolveGroundedLocomotionState();
            ChangeState( nextGroundedState );
        }

        ///<summary>
        /// 사망 상태를 유지한다.
        ///</summary>
        private void UpdateDieState()
        {
        }

        ///<summary>
        /// 좌우 이동 속도를 적용한다.
        ///</summary>
        private void ApplyHorizontalMovement()
        {
            if ( currentState == ePlayerState.DIE )
            {
                return;
            }

            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.x = horizontalInput * moveSpeed;
            targetRigidbody.linearVelocity = currentVelocity;
        }

        ///<summary>
        /// 현재 입력 방향에 맞춰 플레이어의 좌우 방향을 전환한다.
        ///</summary>
        private void ApplyFacingDirection()
        {
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
        /// 점프 중력 가속을 상태에 맞게 조절한다.
        ///</summary>
        private void ApplyJumpGravity()
        {
            if ( currentState == ePlayerState.DIE )
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
        /// 점프 가능 여부를 판정한다.
        ///</summary>
        private bool CanJump()
        {
            bool canJump = currentJumpCount < maxJumpCount;
            return canJump;
        }

        ///<summary>
        /// 점프 속도와 횟수를 갱신한다.
        ///</summary>
        private void ExecuteJump()
        {
            bool shouldRestartJumpAnimation = currentState == ePlayerState.JUMP;
            Vector2 currentVelocity = targetRigidbody.linearVelocity;
            currentVelocity.y = jumpPower;
            targetRigidbody.linearVelocity = currentVelocity;
            currentJumpCount++;
            isGrounded = false;

            if ( shouldRestartJumpAnimation )
            {
                RestartJumpAnimation();
            }
        }

        ///<summary>
        /// 바닥 체크 기준 좌표를 반환한다.
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
        /// 수평 이동 입력 여부를 판정한다.
        ///</summary>
        private bool HasHorizontalInput()
        {
            bool hasHorizontalInput = Mathf.Approximately( horizontalInput, 0.0f ) == false;
            return hasHorizontalInput;
        }

        ///<summary>
        /// 지상 상태에서 사용할 기본 이동 상태를 결정한다.
        ///</summary>
        private ePlayerState ResolveGroundedLocomotionState()
        {
            ePlayerState result = HasHorizontalInput() ? ePlayerState.MOVE : ePlayerState.IDLE;
            return result;
        }

        ///<summary>
        /// 일회성 입력 플래그를 정리한다.
        ///</summary>
        private void ClearOneShotInputs()
        {
            isPendingJump = false;
            isPendingAttack = false;
        }

        ///<summary>
        /// 현재 상태를 애니메이터 bool 파라미터에 반영한다.
        ///</summary>
        private void ApplyAnimationState()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            targetAnimator.SetBool( IdleAnimationParameterName, currentState == ePlayerState.IDLE );
            targetAnimator.SetBool( MoveAnimationParameterName, currentState == ePlayerState.MOVE );
            targetAnimator.SetBool( AttackAnimationParameterName, currentState == ePlayerState.ATTACK );
            targetAnimator.SetBool( JumpAnimationParameterName, currentState == ePlayerState.JUMP );
            targetAnimator.SetBool( HitAnimationParameterName, currentState == ePlayerState.HIT );
            targetAnimator.SetBool( DieAnimationParameterName, currentState == ePlayerState.DIE );
        }

        ///<summary>
        /// 점프 애니메이션을 처음부터 다시 재생한다.
        ///</summary>
        private void RestartJumpAnimation()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            AnimatorStateInfo currentAnimatorStateInfo = targetAnimator.GetCurrentAnimatorStateInfo( 0 );
            int currentFullPathHash = currentAnimatorStateInfo.fullPathHash;
            targetAnimator.Play( currentFullPathHash, 0, 0.0f );
        }

        ///<summary>
        /// 바닥 체크 범위를 에디터에서 표시한다.
        ///</summary>
        private void OnDrawGizmosSelected()
        {
            Vector2 groundCheckPosition = GetGroundCheckPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere( groundCheckPosition, groundCheckRadius );
        }
    }
}
