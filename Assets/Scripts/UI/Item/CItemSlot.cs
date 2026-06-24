using TinyHero.Core.Data;
using TinyHero.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

///<summary>
/// 인벤토리 아이템 슬롯 컴포넌트
///</summary>
public sealed class CItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemCountText;

    private int slotIndex = -1;
    private int currentQuantity;
    private CItemDefinition currentItemDefinition;
    private PopupItemInventory ownerInventoryUiController;

    ///<summary>
    /// 슬롯 참조 초기화
    ///</summary>
    private void Awake()
    {
        ResolveReferences();
    }

    ///<summary>
    /// 슬롯 초기화 설정
    ///</summary>
    public void Initialize( PopupItemInventory _ownerInventoryUiController, int _slotIndex )
    {
        ResolveReferences();
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
    /// 슬롯 아이템 데이터 반영
    ///</summary>
    public void RefreshSlot( CItemDefinition _itemDefinition, int _quantity )
    {
        ResolveReferences();
        currentItemDefinition = _itemDefinition;
        currentQuantity = Mathf.Max( 0, _quantity );
        bool hasItem = currentItemDefinition != null && currentQuantity > 0;

        if ( itemImage != null )
        {
            itemImage.sprite = hasItem ? currentItemDefinition.GetIconSprite() : null;
            itemImage.enabled = hasItem && itemImage.sprite != null;
            Color itemColor = itemImage.color;
            itemColor.a = hasItem ? 1.0f : 0.0f;
            itemImage.color = itemColor;
        }

        if ( itemCountText != null )
        {
            bool useCountText = hasItem && currentQuantity > 1;
            itemCountText.text = useCountText ? currentQuantity.ToString() : string.Empty;
        }
    }

    ///<summary>
    /// 슬롯 아이템 존재 여부 반환
    ///</summary>
    public bool HasItem()
    {
        bool result = currentItemDefinition != null && currentQuantity > 0;
        return result;
    }

    ///<summary>
    /// 슬롯 아이템 정의 반환
    ///</summary>
    public CItemDefinition GetCurrentItemDefinition()
    {
        CItemDefinition result = currentItemDefinition;
        return result;
    }

    ///<summary>
    /// 슬롯 아이템 수량 반환
    ///</summary>
    public int GetCurrentQuantity()
    {
        int result = currentQuantity;
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
    /// 슬롯 클릭 처리
    ///</summary>
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
    /// ?щ’ ?대┃ 泥섎━
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

    ///<summary>
    /// 슬롯 하위 참조 결정
    ///</summary>
    private void ResolveReferences()
    {
        if ( itemImage == null )
        {
            Transform itemImageTransform = transform.Find( "ItemImage" );
            itemImage = itemImageTransform != null ? itemImageTransform.GetComponent<Image>() : null;
        }

        if ( itemCountText == null )
        {
            Transform itemCountTransform = transform.Find( "ItemCount" );
            itemCountText = itemCountTransform != null ? itemCountTransform.GetComponent<TMP_Text>() : null;
        }
    }
}
