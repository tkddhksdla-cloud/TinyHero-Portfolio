using TinyHero.Core.Data;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 상점 슬롯 컴포넌트
    ///</summary>
    public sealed class CShopSlot : CItemSlotBase, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private PopupShop ownerPopupShop;
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
        ///<summary>
        /// 현재 아이템 수량 반환
        ///</summary>
        public long GetCurrentQuantity()
        {
            long result = GetCurrentItemCount();
            return result;
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
