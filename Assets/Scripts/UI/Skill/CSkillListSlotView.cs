using TinyHero.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 슬롯 참조 보관 컴포넌트
    ///</summary>
    public sealed class CSkillListSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private GameObject slotRootObject;
        [SerializeField] private CButtonEx slotButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject selectHighlightObject;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private TMP_Text skillLevelText;
        [SerializeField] private TMP_Text skillInfoText;
        [SerializeField] private TMP_Text skillCostText;
        [SerializeField] private CButtonEx actionButton;
        [SerializeField] private TMP_Text actionButtonText;

        private PopupSkillList ownerSkillListUiController;
        private CSkillManager targetSkillManager;
        private string currentSkillId = string.Empty;

        ///<summary>
        /// 슬롯 참조 자동 연결
        ///</summary>
        public void AutoAssignReferences()
        {
            if ( slotRootObject == null )
            {
                slotRootObject = gameObject;
            }

            if ( slotButton == null )
            {
                slotButton = GetComponent<CButtonEx>();
            }

            if ( backgroundImage == null )
            {
                Transform backgroundTransform = transform.Find( "BG" );
                backgroundImage = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
            }

            if ( selectHighlightObject == null )
            {
                Transform highlightTransform = transform.Find( "BG/SelectObject" );
                selectHighlightObject = highlightTransform != null ? highlightTransform.gameObject : null;
            }

            if ( iconImage == null )
            {
                Transform iconTransform = transform.Find( "IconImage" );
                iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            }

            if ( skillNameText == null )
            {
                Transform nameTransform = transform.Find( "SkillName" );
                skillNameText = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;
            }

            if ( skillLevelText == null )
            {
                Transform levelTransform = transform.Find( "SkillLevelText" );
                skillLevelText = levelTransform != null ? levelTransform.GetComponent<TMP_Text>() : null;
            }

            if ( skillInfoText == null )
            {
                Transform infoTransform = transform.Find( "SkillInfoText" );
                skillInfoText = infoTransform != null ? infoTransform.GetComponent<TMP_Text>() : null;
            }

            if ( skillCostText == null )
            {
                Transform costTransform = transform.Find( "SkillCostText" );
                skillCostText = costTransform != null ? costTransform.GetComponent<TMP_Text>() : null;
            }

            if ( actionButton == null )
            {
                Transform actionButtonTransform = transform.Find( "ButtonLevelUp" );
                actionButton = actionButtonTransform != null ? actionButtonTransform.GetComponent<CButtonEx>() : null;
            }

            if ( actionButtonText == null )
            {
                Transform actionButtonTextTransform = transform.Find( "ButtonLevelUp/Text" );
                actionButtonText = actionButtonTextTransform != null ? actionButtonTextTransform.GetComponent<TMP_Text>() : null;
            }

            EnsureTooltipTrigger();
        }

        ///<summary>
        /// 슬롯 데이터 바인딩
        ///</summary>
        public void Bind( PopupSkillList _ownerSkillListUiController, CSkillManager _skillManager, string _skillId )
        {
            AutoAssignReferences();
            ownerSkillListUiController = _ownerSkillListUiController;
            targetSkillManager = _skillManager;
            currentSkillId = string.IsNullOrWhiteSpace( _skillId ) ? string.Empty : _skillId.Trim();

            if ( actionButton != null )
            {
                actionButton.onClick.RemoveListener( HandleActionButtonClicked );
                actionButton.onClick.AddListener( HandleActionButtonClicked );
            }

            EnsureTooltipTrigger();
        }

        ///<summary>
        /// 슬롯 시각 상태 갱신
        ///</summary>
        public void RefreshView()
        {
            AutoAssignReferences();

            if ( targetSkillManager == null || string.IsNullOrWhiteSpace( currentSkillId ) )
            {
                ApplyEmptyState();
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( currentSkillId );

            if ( skillDefinition == null )
            {
                ApplyEmptyState();
                return;
            }

            bool isUnlocked = targetSkillManager.IsSkillUnlocked( currentSkillId );
            int currentLevel = targetSkillManager.GetSkillLevel( currentSkillId );
            int maxSkillLevel = skillDefinition.GetMaxSkillLevel();
            string skillInfoSummary = ownerSkillListUiController != null ? ownerSkillListUiController.BuildSkillSummaryText( skillDefinition, currentLevel, isUnlocked ) : string.Empty;
            string costText = ownerSkillListUiController != null ? ownerSkillListUiController.BuildSkillCostText( skillDefinition, currentLevel, isUnlocked ) : string.Empty;

            if ( skillNameText != null )
            {
                skillNameText.text = skillDefinition.GetSkillName();
            }

            if ( iconImage != null )
            {
                Sprite skillIconSprite = skillDefinition.GetSkillIcon();
                iconImage.sprite = skillIconSprite;
                iconImage.enabled = skillIconSprite != null;
                Color iconColor = iconImage.color;
                iconColor.a = isUnlocked ? 1.0f : 0.45f;
                iconImage.color = iconColor;
            }

            if ( skillLevelText != null )
            {
                skillLevelText.text = $"Lv.{currentLevel}/{maxSkillLevel}";
            }

            if ( skillInfoText != null )
            {
                skillInfoText.text = skillInfoSummary;
            }

            if ( skillCostText != null )
            {
                skillCostText.text = costText;
            }

            RefreshActionButton( skillDefinition, currentLevel, isUnlocked );
        }

        ///<summary>
        /// 슬롯 유효성 반환
        ///</summary>
        public bool IsValid()
        {
            bool hasRootObject = slotRootObject != null;
            bool hasSkillNameText = skillNameText != null;
            bool hasSkillLevelText = skillLevelText != null;
            bool hasSkillInfoText = skillInfoText != null;
            bool hasSkillCostText = skillCostText != null;
            bool hasActionButton = actionButton != null;
            bool hasActionButtonText = actionButtonText != null;
            bool hasIconImage = iconImage != null;
            bool result = hasRootObject && hasSkillNameText && hasSkillLevelText && hasSkillInfoText && hasSkillCostText && hasActionButton && hasActionButtonText && hasIconImage;
            return result;
        }

        ///<summary>
        /// 슬롯 루트 오브젝트 반환
        ///</summary>
        public GameObject GetSlotRootObject()
        {
            GameObject result = slotRootObject != null ? slotRootObject : gameObject;
            return result;
        }

        ///<summary>
        /// 현재 바인딩 스킬 식별자 반환
        ///</summary>
        public string GetCurrentSkillId()
        {
            string result = currentSkillId;
            return result;
        }

        ///<summary>
        /// 액션 버튼 클릭 처리
        ///</summary>
        private void HandleActionButtonClicked()
        {
            if ( ownerSkillListUiController == null || string.IsNullOrWhiteSpace( currentSkillId ) )
            {
                return;
            }

            ownerSkillListUiController.TryProcessSkillAction( currentSkillId );
        }

        ///<summary>
        /// 스킬 슬롯 드래그 시작 처리
        ///</summary>
        public void OnBeginDrag( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null )
            {
                return;
            }

            ownerSkillListUiController.TryBeginSkillDrag( this, _eventData );
        }

        ///<summary>
        /// 스킬 슬롯 드래그 진행 처리
        ///</summary>
        public void OnDrag( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null )
            {
                return;
            }

            ownerSkillListUiController.UpdateSkillDrag( _eventData );
        }

        ///<summary>
        /// 스킬 슬롯 드래그 종료 처리
        ///</summary>
        public void OnEndDrag( PointerEventData _eventData )
        {
            if ( ownerSkillListUiController == null )
            {
                return;
            }

            ownerSkillListUiController.EndSkillDrag( _eventData );
        }

        ///<summary>
        /// 액션 버튼 표시 상태 갱신
        ///</summary>
        private void RefreshActionButton( CSkillDefinition _skillDefinition, int _currentLevel, bool _isUnlocked )
        {
            if ( actionButton == null || actionButtonText == null || _skillDefinition == null || targetSkillManager == null )
            {
                return;
            }

            bool isMaxLevel = _currentLevel >= _skillDefinition.GetMaxSkillLevel();
            bool canLearnSkill = targetSkillManager.CanLearnSkill( currentSkillId );
            bool canLevelUpSkill = targetSkillManager.CanLevelUpSkill( currentSkillId );
            bool isInteractable = false;
            string buttonTextValue = string.Empty;

            if ( _isUnlocked == false )
            {
                buttonTextValue = canLearnSkill ? "배우기" : "조건 부족";
                isInteractable = canLearnSkill;
            }
            else if ( isMaxLevel )
            {
                buttonTextValue = "최대";
            }
            else
            {
                buttonTextValue = canLevelUpSkill ? "강화" : "SP 부족";
                isInteractable = canLevelUpSkill;
            }

            actionButton.interactable = isInteractable;
            actionButtonText.text = buttonTextValue;
        }

        ///<summary>
        /// 빈 슬롯 상태 반영
        ///</summary>
        private void ApplyEmptyState()
        {
            if ( skillNameText != null )
            {
                skillNameText.text = string.Empty;
            }

            if ( iconImage != null )
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if ( skillLevelText != null )
            {
                skillLevelText.text = string.Empty;
            }

            if ( skillInfoText != null )
            {
                skillInfoText.text = string.Empty;
            }

            if ( skillCostText != null )
            {
                skillCostText.text = string.Empty;
            }

            if ( actionButton != null )
            {
                actionButton.interactable = false;
            }

            if ( actionButtonText != null )
            {
                actionButtonText.text = string.Empty;
            }
        }

        ///<summary>
        /// 아이콘 툴팁 트리거 보정
        ///</summary>
        private void EnsureTooltipTrigger()
        {
            if ( iconImage == null )
            {
                return;
            }

            CSkillSlotTooltipTrigger tooltipTrigger = iconImage.GetComponent<CSkillSlotTooltipTrigger>();

            if ( tooltipTrigger == null )
            {
                tooltipTrigger = iconImage.gameObject.AddComponent<CSkillSlotTooltipTrigger>();
            }

            tooltipTrigger.Configure( ownerSkillListUiController, currentSkillId );
            iconImage.raycastTarget = true;
        }
    }
}
