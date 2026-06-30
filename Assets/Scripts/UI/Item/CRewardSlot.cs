using TinyHero.Core.Data;
using TinyHero.UI;
using UnityEngine.EventSystems;

///<summary>
/// 보상 아이템 슬롯 컴포넌트
///</summary>
public sealed class CRewardSlot : CItemSlotBase, IPointerEnterHandler, IPointerExitHandler
{
    ///<summary>
    /// 보상 슬롯 표시 데이터 반영
    ///</summary>
    public void SetReward( CItemDefinition _itemDefinition, long _itemCount )
    {
        RefreshSlot( _itemDefinition, _itemCount );
    }

    ///<summary>
    /// 보상 슬롯 마우스 진입 처리
    ///</summary>
    public void OnPointerEnter( PointerEventData _eventData )
    {
        if ( HasItem() == false )
        {
            return;
        }

        CUITooltipManager.ShowItemTooltip( GetCurrentItemDefinition() );
    }

    ///<summary>
    /// 보상 슬롯 마우스 이탈 처리
    ///</summary>
    public void OnPointerExit( PointerEventData _eventData )
    {
        CUITooltipManager.HideItemTooltip();
    }
}
