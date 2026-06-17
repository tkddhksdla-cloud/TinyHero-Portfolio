using System.Collections.Generic;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 퀵슬롯 영역 UI 제어 컴포넌트
    ///</summary>
    public sealed class CSkillQuickSlotUI : MonoBehaviour
    {
        [SerializeField] private CSkillManager targetSkillManager;
        [SerializeField] private List<CSkillQuickSlotItemView> slotViewList = new List<CSkillQuickSlotItemView>();

        ///<summary>
        /// 퀵슬롯 UI 초기화
        ///</summary>
        private void Awake()
        {
            ResolveSkillManager();
            CollectSlotViews();
            BindSlotViews();
        }

        ///<summary>
        /// 이벤트 구독 및 초기 갱신
        ///</summary>
        private void OnEnable()
        {
            ResolveSkillManager();
            CollectSlotViews();
            SubscribeEvents();
            BindSlotViews();
            RefreshView();
        }

        ///<summary>
        /// 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        ///<summary>
        /// 키 입력 및 쿨타임 표시 갱신
        ///</summary>
        private void Update()
        {
            ProcessSlotInput();
            RefreshView();
        }

        ///<summary>
        /// 스킬 상태 변경 반영
        ///</summary>
        private void HandleSkillStateChanged()
        {
            RefreshView();
        }

        ///<summary>
        /// 스킬 매니저 참조 결정
        ///</summary>
        private void ResolveSkillManager()
        {
            if ( targetSkillManager != null )
            {
                return;
            }

            CSkillManager resolvedSkillManager = FindFirstObjectByType<CSkillManager>();
            targetSkillManager = resolvedSkillManager;
        }

        ///<summary>
        /// 슬롯 뷰 바인딩 처리
        ///</summary>
        private void BindSlotViews()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.Bind( targetSkillManager, index );
            }
        }

        ///<summary>
        /// 자식 슬롯 뷰 수집 처리
        ///</summary>
        private void CollectSlotViews()
        {
            if ( slotViewList == null )
            {
                slotViewList = new List<CSkillQuickSlotItemView>();
            }

            if ( slotViewList.Count > 0 )
            {
                return;
            }

            CSkillQuickSlotItemView[] collectedSlotViewArray = GetComponentsInChildren<CSkillQuickSlotItemView>( true );

            for ( int index = 0; index < collectedSlotViewArray.Length; index++ )
            {
                CSkillQuickSlotItemView slotView = collectedSlotViewArray[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotViewList.Add( slotView );
            }
        }

        ///<summary>
        /// 스킬 이벤트 구독
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
            targetSkillManager.OnSkillStateChanged += HandleSkillStateChanged;
        }

        ///<summary>
        /// 스킬 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
        }

        ///<summary>
        /// 퀵슬롯 입력 처리
        ///</summary>
        private void ProcessSlotInput()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                bool shouldTrigger = slotView.IsTriggerRequested();

                if ( shouldTrigger == false )
                {
                    continue;
                }

                targetSkillManager.TryUseSkillByQuickSlotIndex( index );
            }
        }

        ///<summary>
        /// 퀵슬롯 전체 시각 상태 갱신
        ///</summary>
        public void RefreshView()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.RefreshView();
            }
        }
    }
}
