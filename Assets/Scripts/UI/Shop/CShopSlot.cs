using TinyHero.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 상점 슬롯 컴포넌트
    ///</summary>
    public sealed class CShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text itemCountText;

        private PopupShop ownerPopupShop;
        private CItemDefinition currentItemDefinition;
        private int currentItemCount;
        private int slotIndex = -1;

        ///<summary>
        /// 슬롯 초기화 처리
        ///</summary>
        public void Initialize( PopupShop _ownerPopupShop, int _slotIndex )
        {
            ownerPopupShop = _ownerPopupShop;
            slotIndex = _slotIndex;
        }

        ///<summary>
        /// 슬롯 표시 데이터 반영
        ///</summary>
        public void RefreshSlot( CItemDefinition _itemDefinition, int _itemCount )
        {
            currentItemDefinition = _itemDefinition;
            currentItemCount = Mathf.Max( 0, _itemCount );
            bool hasItem = currentItemDefinition != null && currentItemCount > 0;

            if ( itemImage != null )
            {
                itemImage.sprite = hasItem ? currentItemDefinition.GetIconSprite() : null;
                itemImage.enabled = hasItem && itemImage.sprite != null;
                Color imageColor = itemImage.color;
                imageColor.a = hasItem ? 1.0f : 0.0f;
                itemImage.color = imageColor;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = hasItem && currentItemCount > 1 ? currentItemCount.ToString() : string.Empty;
            }
        }

        ///<summary>
        /// 슬롯 인덱스 반환
        ///</summary>
        public int GetSlotIndex()
        {
            int result = slotIndex;
            return result;
        }

        ///<summary>
        /// 현재 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetCurrentItemDefinition()
        {
            CItemDefinition result = currentItemDefinition;
            return result;
        }

        ///<summary>
        /// 현재 아이템 수량 반환
        ///</summary>
        public int GetCurrentItemCount()
        {
            int result = currentItemCount;
            return result;
        }

        ///<summary>
        /// 아이템 보유 여부 반환
        ///</summary>
        public bool HasItem()
        {
            bool result = currentItemDefinition != null && currentItemCount > 0;
            return result;
        }

        ///<summary>
        /// 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerPopupShop == null )
            {
                return;
            }

            ownerPopupShop.ShowTooltip( this );
        }

        ///<summary>
        /// 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerPopupShop == null )
            {
                return;
            }

            ownerPopupShop.HideTooltip( this );
        }

        ///<summary>
        /// 슬롯 클릭 처리
        ///</summary>
        public void OnPointerClick( PointerEventData _eventData )
        {
            if ( ownerPopupShop == null )
            {
                return;
            }

            ownerPopupShop.HandleSlotPointerClick( this, _eventData );
        }
    }
}
