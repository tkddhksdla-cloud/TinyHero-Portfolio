using TinyHero.Core;
using UnityEngine;

using TinyHero.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] private KeyCode skillWindowKey = KeyCode.K;
        [SerializeField] private KeyCode cheatWindowKey = KeyCode.F1;
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
        /// 전역 입력 갱신 처리
        ///</summary>
        private void Update()
        {
            HandleInventoryToggleInput();
            HandleSkillWindowToggleInput();
            HandleQuestJournalToggleInput();
            HandleCheatWindowToggleInput();
#if UNITY_EDITOR
            HandleEditorPauseInput();
#endif
        }

        ///<summary>
        /// 수평 입력 반환
        ///</summary>
        public float GetHorizontalInput()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return 0.0f;
            }

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
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isJumpDown = Input.GetKeyDown( jumpKey );
            return isJumpDown;
        }

        ///<summary>
        /// 점프 유지 입력 반환
        ///</summary>
        public bool GetJumpHeld()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isJumpHeld = Input.GetKey( jumpKey );
            return isJumpHeld;
        }

        ///<summary>
        /// 공격 다운 입력 반환
        ///</summary>
        public bool GetAttackDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isAttackDown = Input.GetKeyDown( attackKey ) || Input.GetKeyDown( alternateAttackKey );
            return isAttackDown;
        }

        ///<summary>
        /// 상호작용 다운 입력 반환
        ///</summary>
        public bool GetInteractionDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isInteractionDown = Input.GetKeyDown( interactionKey );
            return isInteractionDown;
        }

        ///<summary>
        /// 상호작용 유지 입력 반환
        ///</summary>
        public bool GetInteractionHeld()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isInteractionHeld = Input.GetKey( interactionKey );
            return isInteractionHeld;
        }

        ///<summary>
        /// 인벤토리 다운 입력 반환
        ///</summary>
        public bool GetInventoryDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isInventoryDown = Input.GetKeyDown( inventoryKey );
            return isInventoryDown;
        }

        ///<summary>
        /// 퀘스트 창 다운 입력 반환
        ///</summary>
        public bool GetQuestJournalDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isQuestJournalDown = Input.GetKeyDown( questJournalKey );
            return isQuestJournalDown;
        }

        ///<summary>
        /// 스킬 창 다운 입력 반환
        ///</summary>
        public bool GetSkillWindowDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isSkillWindowDown = Input.GetKeyDown( skillWindowKey );
            return isSkillWindowDown;
        }

        ///<summary>
        /// 포탈 다운 입력 반환
        ///</summary>
        public bool GetPortalDown()
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

            bool isPortalDown = Input.GetKeyDown( portalKey );
            return isPortalDown;
        }

        ///<summary>
        /// 스킬 슬롯 다운 입력 반환
        ///</summary>
        public bool GetSkillSlotDown( int _slotIndex )
        {
            if ( IsCheatUiBlockingInput() )
            {
                return false;
            }

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

        ///<summary>
        /// 치트 창 토글 처리
        ///</summary>
        private void HandleCheatWindowToggleInput()
        {
            bool isCheatWindowDown = Input.GetKeyDown( cheatWindowKey );

            if ( isCheatWindowDown == false )
            {
                return;
            }

            CCheatCommandUI cheatCommandUi = CCheatCommandUI.GetOrCreate();

            if ( cheatCommandUi == null )
            {
                return;
            }

            cheatCommandUi.ToggleVisible();
        }

        ///<summary>
        /// 인벤토리 토글 입력 처리
        ///</summary>
        private void HandleInventoryToggleInput()
        {
            bool isInventoryDown = GetInventoryDown();

            if ( isInventoryDown == false )
            {
                return;
            }

            CItemInventoryUiManager itemInventoryUiManager = CItemInventoryUiManager.Instance;

            if ( itemInventoryUiManager == null )
            {
                return;
            }

            if ( itemInventoryUiManager.IsInventoryToggleLocked() )
            {
                return;
            }

            itemInventoryUiManager.ToggleInventoryUi();
        }

        ///<summary>
        /// 스킬 창 토글 입력 처리
        ///</summary>
        private void HandleSkillWindowToggleInput()
        {
            bool isSkillWindowDown = GetSkillWindowDown();

            if ( isSkillWindowDown == false )
            {
                return;
            }

            CSkillUiManager skillUiManager = CSkillUiManager.Instance;

            if ( skillUiManager == null )
            {
                return;
            }

            skillUiManager.ToggleSkillUi();
        }

        ///<summary>
        /// 퀘스트 저널 토글 입력 처리
        ///</summary>
        private void HandleQuestJournalToggleInput()
        {
            bool isQuestJournalDown = GetQuestJournalDown();

            if ( isQuestJournalDown == false )
            {
                return;
            }

            CQuestUiManager questUiManager = CQuestUiManager.Instance;

            if ( questUiManager == null )
            {
                return;
            }

            questUiManager.TogglePlayerQuestListUi();
        }

#if UNITY_EDITOR
        ///<summary>
        /// 에디터 플레이모드 일시정지 입력 처리
        ///</summary>
        private void HandleEditorPauseInput()
        {
            bool isPauseDown = Input.GetKeyDown( KeyCode.Insert );

            if ( isPauseDown == false )
            {
                return;
            }

            EditorApplication.isPaused = true;
        }
#endif

        ///<summary>
        /// 치트 UI 입력 점유 상태 반환
        ///</summary>
        private bool IsCheatUiBlockingInput()
        {
            bool result = CCheatCommandUI.IsAnyVisible();
            return result;
        }
    }
}


