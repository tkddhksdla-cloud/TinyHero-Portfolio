using TinyHero.Core.Data;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.EventSystems;

///<summary>
/// 인벤토리 아이템 슬롯 컴포넌트
///</summary>
public sealed class CItemSlot : CItemSlotBase, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private int slotIndex = -1;
    private int boundInventorySlotIndex = -1;
    private PopupItemInventory ownerInventoryUiController;

    ///<summary>
    /// 슬롯 초기화 설정
    ///</summary>
    public void Initialize( PopupItemInventory _ownerInventoryUiController, int _slotIndex )
    {
        ownerInventoryUiController = _ownerInventoryUiController;
        slotIndex = _slotIndex;
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
    /// 실제 인벤토리 슬롯 인덱스 설정
    ///</summary>
    public void SetBoundInventorySlotIndex( int _boundInventorySlotIndex )
    {
        boundInventorySlotIndex = _boundInventorySlotIndex;
    }

    ///<summary>
    /// 실제 인벤토리 슬롯 인덱스 반환
    ///</summary>
    public int GetBoundInventorySlotIndex()
    {
        int result = boundInventorySlotIndex;
        return result;
    }

    ///<summary>
    /// 슬롯 아이템 데이터 반영
    ///</summary>
    ///<summary>
    /// 슬롯 아이템 수량 반환
    ///</summary>
    public long GetCurrentQuantity()
    {
        long result = GetCurrentItemCount();
        return result;
    }

    ///<summary>
    /// 슬롯 마우스 진입 처리
    ///</summary>
    public void OnPointerEnter( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.ShowTooltip( this );
    }

    ///<summary>
    /// 슬롯 마우스 이탈 처리
    ///</summary>
    public void OnPointerExit( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.HideTooltip( this );
    }

    ///<summary>
    /// 슬롯 마우스 다운 처리
    ///</summary>
    public void OnPointerDown( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.HideTooltip( this );
    }

    ///<summary>
    /// 슬롯 클릭 처리
    ///</summary>
    public void OnPointerClick( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.HandleSlotPointerClick( this, _eventData );
    }

    ///<summary>
    /// 슬롯 드래그 시작 처리
    ///</summary>
    public void OnBeginDrag( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.TryBeginSlotDrag( this, _eventData );
    }

    ///<summary>
    /// 슬롯 드래그 진행 처리
    ///</summary>
    public void OnDrag( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.UpdateSlotDrag( _eventData );
    }

    ///<summary>
    /// 슬롯 드래그 종료 처리
    ///</summary>
    public void OnEndDrag( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.EndSlotDrag( _eventData );
    }

    ///<summary>
    /// 슬롯 드롭 처리
    ///</summary>
    public void OnDrop( PointerEventData _eventData )
    {
        if ( ownerInventoryUiController == null )
        {
            return;
        }

        ownerInventoryUiController.HandleSlotDrop( this );
    }

}
