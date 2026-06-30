using System.Collections;
using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 아이템 보상 획득 팝업
    ///</summary>
    public sealed class PopupReward : CUIPopup
    {
        private const string RewardOnEventName = "RewardON";
        private const string RewardOnTypoEventName = "RerwardON";
        private const float DefaultCloseLockSeconds = 0.5f;

        [Header( "참조" )]
        [SerializeField] private CButtonEx dimmedButton;
        [SerializeField] private Transform rewardList;
        [SerializeField] private CAnimationEventController animationEventController;
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private CRewardSlot rewardSlotPrefab;

        [Header( "설정값" )]
        [SerializeField] private float rewardSlotRevealInterval = 0.25f;

        private readonly List<CRewardSlot> rewardSlotList = new List<CRewardSlot>();
        private Coroutine revealCoroutine;
        private int activeRewardSlotCount;
        private bool hasSkippedAnimation;

        ///<summary>
        /// 팝업 참조 초기화
        ///</summary>
        private void Awake()
        {
            RegisterAnimationEvents();
            BindButtonEvents();
        }

        ///<summary>
        /// 팝업 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RegisterAnimationEvents();
            BindButtonEvents();
            LockCloseForSeconds( DefaultCloseLockSeconds );
        }

        ///<summary>
        /// 팝업 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            UnbindButtonEvents();
            StopRevealCoroutine();
        }

        ///<summary>
        /// 스페이스 입력 처리
        ///</summary>
        private void Update()
        {
            if ( Input.GetKeyDown( KeyCode.Space ) == false )
            {
                return;
            }

            HandleAdvanceInput();
        }

        ///<summary>
        /// 보상 목록 표시 요청
        ///</summary>
        public void ShowRewardList( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            StopRevealCoroutine();
            hasSkippedAnimation = false;
            LockCloseForSeconds( DefaultCloseLockSeconds );
            EnsureRewardSlotCount( _rewardItemDataList );
            ApplyRewardSlotData( _rewardItemDataList );
            SetAllRewardSlotsVisible( false );
            RestartAnimator();
        }

        ///<summary>
        /// 애니메이션 이벤트 처리
        ///</summary>
        private void HandleRewardOnAnimationEvent()
        {
            StopRevealCoroutine();
            revealCoroutine = StartCoroutine( IE_RevealRewardSlots() );
        }

        ///<summary>
        /// 보상 슬롯 순차 표시
        ///</summary>
        private IEnumerator IE_RevealRewardSlots()
        {
            for ( int index = 0; index < activeRewardSlotCount; index++ )
            {
                CRewardSlot rewardSlot = rewardSlotList[ index ];

                if ( rewardSlot == null )
                {
                    continue;
                }

                rewardSlot.gameObject.SetActive( true );
                yield return new WaitForSeconds( rewardSlotRevealInterval );
            }

            revealCoroutine = null;
        }

        ///<summary>
        /// 진행 입력 처리
        ///</summary>
        private void HandleAdvanceInput()
        {
            if ( hasSkippedAnimation == false )
            {
                SkipToAnimationLastFrame();
                RevealAllRewardSlots();
                hasSkippedAnimation = true;
                return;
            }

            if ( IsCloseLocked() )
            {
                return;
            }

            CloseNavigationLayer();
        }

        ///<summary>
        /// 애니메이션 마지막 프레임 이동
        ///</summary>
        private void SkipToAnimationLastFrame()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo( 0 );
            int shortNameHash = stateInfo.shortNameHash;
            targetAnimator.Play( shortNameHash, 0, 1.0f );
            targetAnimator.Update( 0.0f );
        }

        ///<summary>
        /// 전체 보상 슬롯 표시
        ///</summary>
        private void RevealAllRewardSlots()
        {
            StopRevealCoroutine();
            SetAllRewardSlotsVisible( true );
        }

        ///<summary>
        /// 애니메이터 재시작 처리
        ///</summary>
        private void RestartAnimator()
        {
            if ( targetAnimator == null )
            {
                return;
            }

            targetAnimator.Rebind();
            targetAnimator.Update( 0.0f );
        }

        ///<summary>
        /// 보상 슬롯 개수 보장
        ///</summary>
        private void EnsureRewardSlotCount( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            int targetCount = CountValidRewardItems( _rewardItemDataList );
            activeRewardSlotCount = targetCount;

            while ( rewardSlotList.Count < targetCount )
            {
                if ( rewardSlotPrefab == null || rewardList == null )
                {
                    return;
                }

                CRewardSlot createdRewardSlot = Instantiate( rewardSlotPrefab, rewardList );
                createdRewardSlot.name = rewardSlotPrefab.name;
                rewardSlotList.Add( createdRewardSlot );
            }

            for ( int index = targetCount; index < rewardSlotList.Count; index++ )
            {
                CRewardSlot rewardSlot = rewardSlotList[ index ];

                if ( rewardSlot == null )
                {
                    continue;
                }

                rewardSlot.gameObject.SetActive( false );
            }
        }

        ///<summary>
        /// 보상 슬롯 데이터 반영
        ///</summary>
        private void ApplyRewardSlotData( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            int slotIndex = 0;

            if ( _rewardItemDataList == null )
            {
                return;
            }

            for ( int index = 0; index < _rewardItemDataList.Count; index++ )
            {
                CRewardItemData rewardItemData = _rewardItemDataList[ index ];

                if ( rewardItemData == null || rewardItemData.IsValid() == false )
                {
                    continue;
                }

                if ( slotIndex >= rewardSlotList.Count )
                {
                    break;
                }

                CRewardSlot rewardSlot = rewardSlotList[ slotIndex ];

                if ( rewardSlot != null )
                {
                    rewardSlot.SetReward( rewardItemData.GetItemDefinition(), rewardItemData.GetItemCount() );
                }

                slotIndex++;
            }
        }

        ///<summary>
        /// 유효 보상 아이템 개수 반환
        ///</summary>
        private int CountValidRewardItems( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            if ( _rewardItemDataList == null )
            {
                return 0;
            }

            int count = 0;

            for ( int index = 0; index < _rewardItemDataList.Count; index++ )
            {
                CRewardItemData rewardItemData = _rewardItemDataList[ index ];

                if ( rewardItemData == null || rewardItemData.IsValid() == false )
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        ///<summary>
        /// 전체 보상 슬롯 활성 상태 반영
        ///</summary>
        private void SetAllRewardSlotsVisible( bool _isVisible )
        {
            for ( int index = 0; index < rewardSlotList.Count; index++ )
            {
                CRewardSlot rewardSlot = rewardSlotList[ index ];

                if ( rewardSlot == null )
                {
                    continue;
                }

                bool shouldShowSlot = _isVisible && index < activeRewardSlotCount;
                rewardSlot.gameObject.SetActive( shouldShowSlot );
            }
        }

        ///<summary>
        /// 보상 표시 코루틴 정지
        ///</summary>
        private void StopRevealCoroutine()
        {
            if ( revealCoroutine == null )
            {
                return;
            }

            StopCoroutine( revealCoroutine );
            revealCoroutine = null;
        }

        ///<summary>
        /// 애니메이션 이벤트 등록
        ///</summary>
        private void RegisterAnimationEvents()
        {
            if ( animationEventController == null )
            {
                return;
            }

            animationEventController.RegisterEventAction( RewardOnEventName, HandleRewardOnAnimationEvent );
            animationEventController.RegisterEventAction( RewardOnTypoEventName, HandleRewardOnAnimationEvent );
        }

        ///<summary>
        /// 버튼 이벤트 연결
        ///</summary>
        private void BindButtonEvents()
        {
            if ( dimmedButton == null )
            {
                return;
            }

            dimmedButton.onClick.RemoveListener( HandleAdvanceInput );
            dimmedButton.onClick.AddListener( HandleAdvanceInput );
        }

        ///<summary>
        /// 버튼 이벤트 해제
        ///</summary>
        private void UnbindButtonEvents()
        {
            if ( dimmedButton == null )
            {
                return;
            }

            dimmedButton.onClick.RemoveListener( HandleAdvanceInput );
        }
    }
}
