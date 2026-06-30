using TinyHero.Core.Data;

namespace TinyHero.UI
{
    ///<summary>
    /// 아이템 보상 표시 데이터
    ///</summary>
    public sealed class CRewardItemData
    {
        private readonly CItemDefinition itemDefinition;
        private readonly long itemCount;

        ///<summary>
        /// 아이템 보상 표시 데이터 생성
        ///</summary>
        public CRewardItemData( CItemDefinition _itemDefinition, long _itemCount )
        {
            itemDefinition = _itemDefinition;
            itemCount = System.Math.Max( 0L, _itemCount );
        }

        ///<summary>
        /// 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinition()
        {
            CItemDefinition result = itemDefinition;
            return result;
        }

        ///<summary>
        /// 아이템 수량 반환
        ///</summary>
        public long GetItemCount()
        {
            long result = itemCount;
            return result;
        }

        ///<summary>
        /// 표시 가능 여부 반환
        ///</summary>
        public bool IsValid()
        {
            bool result = itemDefinition != null && itemCount > 0L;
            return result;
        }
    }
}
