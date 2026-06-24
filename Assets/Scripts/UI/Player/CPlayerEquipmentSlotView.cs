using TinyHero.Core.Data;
using TinyHero.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 장비 슬롯 표시 컴포넌트
    ///</summary>
    public sealed class CPlayerEquipmentSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private CPlayerEquipmentStatusPanelUI ownerPanelUi;

        private CItemDefinition currentItemDefinition;

        ///<summary>
        /// 슬롯 참조 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 슬롯 구성
        ///</summary>
        public void Initialize( CPlayerEquipmentStatusPanelUI _ownerPanelUi, eEquipmentType _equipmentType, string _slotLabelText, string _placeholderText )
        {
            ResolveReferences();
            ownerPanelUi = _ownerPanelUi;
            equipmentType = _equipmentType;

            if ( slotLabelText != null )
            {
                slotLabelText.text = _slotLabelText;
            }

            if ( placeholderText != null )
            {
                placeholderText.text = _placeholderText;
            }
        }

        ///<summary>
        /// 장비 타입 반환
        ///</summary>
        public eEquipmentType GetEquipmentType()
        {
            eEquipmentType result = equipmentType;
            return result;
        }

        ///<summary>
        /// 슬롯 아이템 갱신
        ///</summary>
        public void RefreshSlot( CItemDefinition _itemDefinition )
        {
            ResolveReferences();
            currentItemDefinition = _itemDefinition;
            bool hasItem = currentItemDefinition != null;

            if ( iconImage != null )
            {
                Sprite iconSprite = hasItem ? currentItemDefinition.GetIconSprite() : null;
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
            }

            if ( placeholderText != null )
            {
                placeholderText.gameObject.SetActive( hasItem == false );
            }
        }

        ///<summary>
        /// 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null || currentItemDefinition == null )
            {
                return;
            }

            ownerPanelUi.ShowEquipmentTooltip( equipmentType, currentItemDefinition );
        }

        ///<summary>
        /// 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null )
            {
                return;
            }

            ownerPanelUi.HideTooltip();
        }

        ///<summary>
        /// 마우스 클릭 처리
        ///</summary>
        public void OnPointerClick( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null || _eventData == null )
            {
                return;
            }

            if ( _eventData.button != PointerEventData.InputButton.Right )
            {
                return;
            }

            ownerPanelUi.TryUnequip( equipmentType );
        }

        ///<summary>
        /// 마우스 다운 처리
        ///</summary>
        public void OnPointerDown( PointerEventData _eventData )
        {
            if ( ownerPanelUi == null )
            {
                return;
            }

            ownerPanelUi.HideTooltip();
        }

        ///<summary>
        /// 장비 드래그 시작 처리
        ///</summary>
        public void OnBeginDrag( PointerEventData _eventData )
        {
            if ( currentItemDefinition == null )
            {
                return;
            }

            CEquipmentUiDragState.BeginDrag( equipmentType );
        }

        ///<summary>
        /// 장비 드래그 진행 처리
        ///</summary>
        public void OnDrag( PointerEventData _eventData )
        {
        }

        ///<summary>
        /// 장비 드래그 종료 처리
        ///</summary>
        public void OnEndDrag( PointerEventData _eventData )
        {
            CEquipmentUiDragState.EndDrag();
        }

        ///<summary>
        /// 하위 UI 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( iconImage == null )
            {
                Transform iconTransform = transform.Find( "IconImage" );
                iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            }

            if ( placeholderText == null )
            {
                Transform placeholderTransform = transform.Find( "PlaceholderText" );
                placeholderText = placeholderTransform != null ? placeholderTransform.GetComponent<TMP_Text>() : null;
            }

            if ( slotLabelText == null )
            {
                Transform labelTransform = transform.Find( "SlotLabelText" );
                slotLabelText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
            }
        }
    }
}
