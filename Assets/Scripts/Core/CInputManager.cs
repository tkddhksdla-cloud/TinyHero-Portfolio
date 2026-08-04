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
        private const int SkillSlotCount = 8;

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

        private readonly int[] mobileSkillSlotDownRequestFrameArray = new int[ SkillSlotCount ];
        private float mobileHorizontalInput;
        private bool isMobileJumpHeld;
        private int mobileJumpDownRequestFrame = -1;

        ///<summary>
        /// 싱글톤 및 모바일 입력 상태 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();
            ClearMobileInputState();
        }

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
            float keyboardHorizontalInput = 0.0f;

            if ( isLeftPressed == isRightPressed )
            {
                float mobileHorizontalResult = mobileHorizontalInput;
                return mobileHorizontalResult;
            }

            keyboardHorizontalInput = isLeftPressed ? -1.0f : 1.0f;
            return keyboardHorizontalInput;
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

            bool isMobileJumpDown = ConsumeMobileJumpDownRequest();
            bool isJumpDown = Input.GetKeyDown( jumpKey ) || isMobileJumpDown;
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

            bool isJumpHeld = Input.GetKey( jumpKey ) || isMobileJumpHeld;
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

            bool isMobileSkillSlotDown = ConsumeMobileSkillSlotDownRequest( _slotIndex );
            bool isSkillSlotDown = Input.GetKeyDown( resolvedKeyCode ) || isMobileSkillSlotDown;
            return isSkillSlotDown;
        }

        ///<summary>
        /// 모바일 수평 이동 입력 설정
        ///</summary>
        public void SetMobileHorizontalInput( float _horizontalInput )
        {
            float resolvedHorizontalInput = Mathf.Clamp( _horizontalInput, -1.0f, 1.0f );
            mobileHorizontalInput = resolvedHorizontalInput;
        }

        ///<summary>
        /// 모바일 점프 유지 입력 설정
        ///</summary>
        public void SetMobileJumpHeld( bool _isJumpHeld )
        {
            isMobileJumpHeld = _isJumpHeld;
        }

        ///<summary>
        /// 모바일 점프 시작 입력 요청
        ///</summary>
        public void RequestMobileJumpDown()
        {
            mobileJumpDownRequestFrame = Time.frameCount;
        }

        ///<summary>
        /// 모바일 스킬 슬롯 시작 입력 요청
        ///</summary>
        public void RequestMobileSkillSlotDown( int _slotIndex )
        {
            if ( _slotIndex < 0 || _slotIndex >= SkillSlotCount )
            {
                return;
            }

            mobileSkillSlotDownRequestFrameArray[ _slotIndex ] = Time.frameCount;
        }

        ///<summary>
        /// 모바일 입력 상태 초기화
        ///</summary>
        public void ClearMobileInputState()
        {
            mobileHorizontalInput = 0.0f;
            isMobileJumpHeld = false;
            mobileJumpDownRequestFrame = -1;

            for ( int index = 0; index < mobileSkillSlotDownRequestFrameArray.Length; index++ )
            {
                mobileSkillSlotDownRequestFrameArray[ index ] = -1;
            }
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

        ///<summary>
        /// 모바일 스킬 슬롯 시작 입력 소비
        ///</summary>
        private bool ConsumeMobileSkillSlotDownRequest( int _slotIndex )
        {
            if ( _slotIndex < 0 || _slotIndex >= mobileSkillSlotDownRequestFrameArray.Length )
            {
                return false;
            }

            int requestedFrame = mobileSkillSlotDownRequestFrameArray[ _slotIndex ];
            bool isRequested = IsMobileDownRequestValid( requestedFrame );
            mobileSkillSlotDownRequestFrameArray[ _slotIndex ] = -1;
            return isRequested;
        }

        ///<summary>
        /// 모바일 점프 시작 입력 소비
        ///</summary>
        private bool ConsumeMobileJumpDownRequest()
        {
            bool isRequested = IsMobileDownRequestValid( mobileJumpDownRequestFrame );
            mobileJumpDownRequestFrame = -1;
            return isRequested;
        }

        ///<summary>
        /// 모바일 시작 입력 유효 프레임 여부 반환
        ///</summary>
        private bool IsMobileDownRequestValid( int _requestedFrame )
        {
            int previousFrame = Time.frameCount - 1;
            bool result = _requestedFrame >= previousFrame;
            return result;
        }
    }
}


