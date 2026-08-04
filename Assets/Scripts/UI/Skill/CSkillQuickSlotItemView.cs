using TinyHero.Core;
using TinyHero.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 퀵슬롯 단일 슬롯 뷰
    ///</summary>
    public sealed class CSkillQuickSlotItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static readonly string[] FixedQuickKeyLabelArray = { "Q", "W", "E", "R", "A", "S", "D", "F" };

        [SerializeField] private int quickSlotIndex;
        [SerializeField] private Image skillIcon;
        [SerializeField] private Image skillCooltimeFillImage;
        [SerializeField] private TMP_Text skillCooltimeValue;
        [SerializeField] private TMP_Text skillQuickKeyText;

        private CSkillQuickSlotUI ownerSkillQuickSlotUi;
        private CSkillManager targetSkillManager;

        ///<summary>
        /// 슬롯 참조 자동 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 스킬 매니저 및 슬롯 인덱스 바인딩
        ///</summary>
        public void Bind( CSkillQuickSlotUI _ownerSkillQuickSlotUi, CSkillManager _skillManager, int _quickSlotIndex )
        {
            ResolveReferences();
            ownerSkillQuickSlotUi = _ownerSkillQuickSlotUi;
            targetSkillManager = _skillManager;
            quickSlotIndex = _quickSlotIndex;
            ApplyFixedQuickKeyLabel();
        }

        ///<summary>
        /// 퀵슬롯 인덱스 반환
        ///</summary>
        public int GetQuickSlotIndex()
        {
            int result = quickSlotIndex;
            return result;
        }

        ///<summary>
        /// 현재 배정 스킬 식별자 반환
        ///</summary>
        public string GetAssignedSkillId()
        {
            if ( targetSkillManager == null )
            {
                return string.Empty;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinitionByQuickSlotIndex( quickSlotIndex );

            if ( skillDefinition == null )
            {
                return string.Empty;
            }

            string result = skillDefinition.GetSkillId();
            return result;
        }

        ///<summary>
        /// 퀵슬롯 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.ShowQuickSlotSkillTooltip( quickSlotIndex );
        }

        ///<summary>
        /// 퀵슬롯 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.HideQuickSlotSkillTooltip();
        }

        ///<summary>
        /// 퀵슬롯 마우스 다운 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.HideQuickSlotSkillTooltip();
        }

        ///<summary>
        /// 모바일 퀵슬롯 탭 처리
        ///</summary>
        public void OnPointerClick( PointerEventData _eventData )
        {
            if ( Application.isMobilePlatform == false || _eventData == null || _eventData.button != PointerEventData.InputButton.Left )
            {
                return;
            }

            bool hasInputManager = CInputManager.TryGetExistingInstance( out CInputManager inputManager );

            if ( hasInputManager == false || inputManager == null )
            {
                return;
            }

            inputManager.RequestMobileSkillSlotDown( quickSlotIndex );
        }

        ///<summary>
        /// 슬롯 입력 요청 여부 반환
        ///</summary>
        public bool IsTriggerRequested()
        {
            KeyCode keyCode = ResolveQuickKeyCode();

            if ( keyCode == KeyCode.None )
            {
                return false;
            }

            bool isTriggerRequested = Input.GetKeyDown( keyCode );
            return isTriggerRequested;
        }

        ///<summary>
        /// 슬롯 시각 상태 갱신
        ///</summary>
        public void RefreshView()
        {
            ResolveReferences();

            if ( targetSkillManager == null )
            {
                ApplyEmptyState();
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinitionByQuickSlotIndex( quickSlotIndex );

            if ( skillDefinition == null )
            {
                ApplyEmptyState();
                return;
            }

            if ( skillIcon != null )
            {
                skillIcon.sprite = skillDefinition.GetSkillIcon();
                skillIcon.enabled = skillDefinition.GetSkillIcon() != null;
                bool isUnlocked = targetSkillManager.IsSkillUnlocked( skillDefinition.GetSkillId() );
                Color iconColor = skillIcon.color;
                iconColor.a = isUnlocked ? 1.0f : 0.35f;
                skillIcon.color = iconColor;
            }

            float remainingCooldown = targetSkillManager.GetSkillCooldownRemaining( skillDefinition.GetSkillId() );
            float cooldownSeconds = targetSkillManager.GetSkillCooldownDuration( skillDefinition.GetSkillId() );
            ApplyCooldownState( remainingCooldown, cooldownSeconds );
        }

        ///<summary>
        /// 퀵슬롯 드래그 시작 처리
        ///</summary>
        public void OnBeginDrag( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.TryBeginDragFromQuickSlot( quickSlotIndex, _eventData );
        }

        ///<summary>
        /// 퀵슬롯 드래그 진행 처리
        ///</summary>
        public void OnDrag( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.UpdateSkillDrag( _eventData );
        }

        ///<summary>
        /// 퀵슬롯 드래그 종료 처리
        ///</summary>
        public void OnEndDrag( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.EndSkillDrag( _eventData );
        }

        ///<summary>
        /// 퀵슬롯 드롭 처리
        ///</summary>
        public void OnDrop( PointerEventData _eventData )
        {
            if ( ownerSkillQuickSlotUi == null )
            {
                return;
            }

            ownerSkillQuickSlotUi.HandleSlotDrop( quickSlotIndex );
        }

        ///<summary>
        /// 슬롯 참조 자동 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( skillIcon == null )
            {
                Transform childTransform = transform.Find( "SkillIcon" );
                skillIcon = childTransform != null ? childTransform.GetComponent<Image>() : null;
            }

            if ( skillIcon != null )
            {
                skillIcon.raycastTarget = true;
            }

            if ( skillCooltimeFillImage == null )
            {
                Transform childTransform = transform.Find( "SkillCooltimeFillImage" );
                skillCooltimeFillImage = childTransform != null ? childTransform.GetComponent<Image>() : null;
            }

            if ( skillCooltimeValue == null )
            {
                Transform childTransform = transform.Find( "SkillCooltimeValue" );
                skillCooltimeValue = childTransform != null ? childTransform.GetComponent<TMP_Text>() : null;
            }

            if ( skillQuickKeyText == null )
            {
                Transform childTransform = transform.Find( "SkillQuickKeyText" );
                skillQuickKeyText = childTransform != null ? childTransform.GetComponent<TMP_Text>() : null;
            }

            ApplyFixedQuickKeyLabel();
        }

        ///<summary>
        /// 고정 퀵슬롯 키 라벨 반영
        ///</summary>
        private void ApplyFixedQuickKeyLabel()
        {
            if ( skillQuickKeyText == null )
            {
                return;
            }

            if ( Application.isMobilePlatform )
            {
                skillQuickKeyText.text = string.Empty;
                return;
            }

            if ( quickSlotIndex < 0 || quickSlotIndex >= FixedQuickKeyLabelArray.Length )
            {
                return;
            }

            string quickKeyLabel = FixedQuickKeyLabelArray[ quickSlotIndex ];
            skillQuickKeyText.text = quickKeyLabel;
        }

        ///<summary>
        /// 빈 슬롯 상태 반영
        ///</summary>
        private void ApplyEmptyState()
        {
            if ( skillIcon != null )
            {
                skillIcon.sprite = null;
                skillIcon.enabled = false;
            }

            ApplyCooldownState( 0.0f, 0.0f );
        }

        ///<summary>
        /// 쿨타임 표시 상태 반영
        ///</summary>
        private void ApplyCooldownState( float _remainingCooldown, float _cooldownSeconds )
        {
            if ( skillCooltimeFillImage != null )
            {
                bool isCooldownActive = _remainingCooldown > 0.0f && _cooldownSeconds > 0.0f;
                skillCooltimeFillImage.gameObject.SetActive( isCooldownActive );

                if ( isCooldownActive )
                {
                    float fillAmount = Mathf.Clamp01( _remainingCooldown / _cooldownSeconds );
                    skillCooltimeFillImage.fillAmount = fillAmount;
                }
                else
                {
                    skillCooltimeFillImage.fillAmount = 0.0f;
                }
            }

            if ( skillCooltimeValue != null )
            {
                if ( _remainingCooldown > 0.0f )
                {
                    float ceilSeconds = Mathf.Ceil( _remainingCooldown );
                    skillCooltimeValue.text = ceilSeconds.ToString( "0" );
                }
                else
                {
                    skillCooltimeValue.text = string.Empty;
                }
            }
        }

        ///<summary>
        /// 퀵슬롯 키 코드 결정
        ///</summary>
        private KeyCode ResolveQuickKeyCode()
        {
            CInputManager inputManager = CInputManager.Instance;

            if ( inputManager != null )
            {
                KeyCode inputManagerKeyCode = inputManager.GetSkillSlotKeyCode( quickSlotIndex );

                if ( inputManagerKeyCode != KeyCode.None )
                {
                    return inputManagerKeyCode;
                }
            }

            if ( skillQuickKeyText == null )
            {
                return KeyCode.None;
            }

            string quickKeyText = skillQuickKeyText.text;

            if ( string.IsNullOrWhiteSpace( quickKeyText ) )
            {
                return KeyCode.None;
            }

            bool isParsed = System.Enum.TryParse( quickKeyText.Trim(), true, out KeyCode keyCode );

            if ( isParsed == false )
            {
                return KeyCode.None;
            }

            return keyCode;
        }
    }
}
