using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 플레이어 입력을 중앙에서 조회하는 매니저이다.
    ///</summary>
    public sealed class CInputManager : CSingleTon<CInputManager>
    {
        [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode alternateLeftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode alternateRightKey = KeyCode.D;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode attackKey = KeyCode.Z;

        ///<summary>
        /// 좌우 이동 입력값을 반환한다.
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
        /// 점프 시작 입력 여부를 반환한다.
        ///</summary>
        public bool GetJumpDown()
        {
            bool isJumpDown = Input.GetKeyDown( jumpKey );
            return isJumpDown;
        }

        ///<summary>
        /// 점프 유지 입력 여부를 반환한다.
        ///</summary>
        public bool GetJumpHeld()
        {
            bool isJumpHeld = Input.GetKey( jumpKey );
            return isJumpHeld;
        }

        ///<summary>
        /// 공격 시작 입력 여부를 반환한다.
        ///</summary>
        public bool GetAttackDown()
        {
            bool isAttackDown = Input.GetKeyDown( attackKey );
            return isAttackDown;
        }
    }
}
