using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 상점 판매 드롭 타겟 컴포넌트
    ///</summary>
    public sealed class CShopSellDropTarget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private PopupShop ownerPopupShop;

        ///<summary>
        /// 드롭 타겟 구성
        ///</summary>
        public void Configure( PopupShop _ownerPopupShop )
        {
            ownerPopupShop = _ownerPopupShop;
        }

        ///<summary>
        /// 슬롯 드롭 처리
        ///</summary>
        public void OnDrop( PointerEventData _eventData )
        {
            if ( ownerPopupShop == null )
            {
                return;
            }

            ownerPopupShop.TryPromptSellDraggedInventoryItem();
        }
    }
}
