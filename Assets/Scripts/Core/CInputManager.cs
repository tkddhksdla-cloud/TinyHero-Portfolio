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
        [SerializeField] private KeyCode alternateLeftKey = KeyCode.None;
        [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode alternateRightKey = KeyCode.None;
        [SerializeField] private KeyCode jumpKey = KeyCode.C;
        [SerializeField] private KeyCode attackKey = KeyCode.Z;
        [SerializeField] private KeyCode alternateAttackKey = KeyCode.None;
        [SerializeField] private KeyCode interactionKey = KeyCode.Space;
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private KeyCode questJournalKey = KeyCode.J;
        [SerializeField] private KeyCode portalKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode skillSlot1Key = KeyCode.Q;
        [SerializeField] private KeyCode skillSlot2Key = KeyCode.W;
        [SerializeField] private KeyCode skillSlot3Key = KeyCode.E;
        [SerializeField] private KeyCode skillSlot4Key = KeyCode.R;
        [SerializeField] private KeyCode skillSlot5Key = KeyCode.A;
        [SerializeField] private KeyCode skillSlot6Key = KeyCode.S;
        [SerializeField] private KeyCode skillSlot7Key = KeyCode.D;
        [SerializeField] private KeyCode skillSlot8Key = KeyCode.F;

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
            bool isAttackDown = Input.GetKeyDown( attackKey ) || Input.GetKeyDown( alternateAttackKey );
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

        ///<summary>
        /// 인벤토리 다운 입력 반환
        ///</summary>
        public bool GetInventoryDown()
        {
            bool isInventoryDown = Input.GetKeyDown( inventoryKey );
            return isInventoryDown;
        }

        ///<summary>
        /// 퀘스트 창 다운 입력 반환
        ///</summary>
        public bool GetQuestJournalDown()
        {
            bool isQuestJournalDown = Input.GetKeyDown( questJournalKey );
            return isQuestJournalDown;
        }

        ///<summary>
        /// 포탈 다운 입력 반환
        ///</summary>
        public bool GetPortalDown()
        {
            bool isPortalDown = Input.GetKeyDown( portalKey );
            return isPortalDown;
        }

        ///<summary>
        /// 스킬 슬롯 다운 입력 반환
        ///</summary>
        public bool GetSkillSlotDown( int _slotIndex )
        {
            KeyCode resolvedKeyCode = GetSkillSlotKeyCode( _slotIndex );

            if ( resolvedKeyCode == KeyCode.None )
            {
                return false;
            }

            bool isSkillSlotDown = Input.GetKeyDown( resolvedKeyCode );
            return isSkillSlotDown;
        }

        ///<summary>
        /// 스킬 슬롯 키 코드 반환
        ///</summary>
        public KeyCode GetSkillSlotKeyCode( int _slotIndex )
        {
            switch ( _slotIndex )
            {
                case 0:
                    return skillSlot1Key;

                case 1:
                    return skillSlot2Key;

                case 2:
                    return skillSlot3Key;

                case 3:
                    return skillSlot4Key;

                case 4:
                    return skillSlot5Key;

                case 5:
                    return skillSlot6Key;

                case 6:
                    return skillSlot7Key;

                case 7:
                    return skillSlot8Key;
            }

            return KeyCode.None;
        }
    }
}


