using TinyHero.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 아이템 표시 슬롯 공용 기반 클래스
    ///</summary>
    public abstract class CItemSlotBase : MonoBehaviour
    {
        [Header( "참조" )]
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text itemCountText;

        private CItemDefinition currentItemDefinition;
        private long currentItemCount;

        ///<summary>
        /// 슬롯 표시 데이터 반영
        ///</summary>
        public virtual void RefreshSlot( CItemDefinition _itemDefinition, long _itemCount )
        {
            currentItemDefinition = _itemDefinition;
            currentItemCount = System.Math.Max( 0L, _itemCount );
            bool hasItem = currentItemDefinition != null && currentItemCount > 0L;

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
                bool useCountText = hasItem && currentItemCount > 1L;
                itemCountText.text = useCountText ? currentItemCount.ToString() : string.Empty;
            }
        }

        ///<summary>
        /// 슬롯 아이템 존재 여부 반환
        ///</summary>
        public bool HasItem()
        {
            bool result = currentItemDefinition != null && currentItemCount > 0L;
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
        public long GetCurrentItemCount()
        {
            long result = currentItemCount;
            return result;
        }

    }
}
