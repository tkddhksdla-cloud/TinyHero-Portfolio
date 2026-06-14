using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 입력 관리 컴포넌트
    ///</summary>
    public sealed class CInputManager : CSingleTon<CInputManager>
    {
        [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode alternateLeftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode alternateRightKey = KeyCode.D;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode attackKey = KeyCode.Z;
        [SerializeField] private KeyCode interactionKey = KeyCode.UpArrow;

        ///<summary>
        /// 수평 입력 반환
        ///</summary>
        public float GetHorizontalInput()
        {
            bool isLeftPressed = Input.GetKey( leftKey ) || Input.GetKey( alternateLeftKey );
            bool isRightPressed = Input.GetKey( rightKey ) || Input.GetKey( alternateRightKey );
            float horizontalInput = 0.0f;

            if ( isLeftPressed == isRightPressed )
            {
                return horizontalInput;
            }

            horizontalInput = isLeftPressed ? -1.0f : 1.0f;
            return horizontalInput;
        }

        ///<summary>
        /// 점프 다운 입력 반환
        ///</summary>
        public bool GetJumpDown()
        {
            bool isJumpDown = Input.GetKeyDown( jumpKey );
            return isJumpDown;
        }

        ///<summary>
        /// 점프 유지 입력 반환
        ///</summary>
        public bool GetJumpHeld()
        {
            bool isJumpHeld = Input.GetKey( jumpKey );
            return isJumpHeld;
        }

        ///<summary>
        /// 공격 다운 입력 반환
        ///</summary>
        public bool GetAttackDown()
        {
            bool isAttackDown = Input.GetKeyDown( attackKey );
            return isAttackDown;
        }

        ///<summary>
        /// 상호작용 다운 입력 반환
        ///</summary>
        public bool GetInteractionDown()
        {
            bool isInteractionDown = Input.GetKeyDown( interactionKey );
            return isInteractionDown;
        }

        ///<summary>
        /// 상호작용 유지 입력 반환
        ///</summary>
        public bool GetInteractionHeld()
        {
            bool isInteractionHeld = Input.GetKey( interactionKey );
            return isInteractionHeld;
        }
    }
}


