using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 상점 판매 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CShopEntryData
    {
        private const string DefaultPriceItemId = "GOLD";

        [SerializeField] private string itemId = string.Empty;
        [SerializeField] [HideInInspector] private int itemCount = 1;
        [SerializeField] private long itemCountValue = 1L;
        [SerializeField] private string priceItemId = DefaultPriceItemId;
        [SerializeField] [HideInInspector] private int priceAmount = 1;
        [SerializeField] private long priceAmountValue = 1L;

        ///<summary>
        /// 판매 아이템 ID 반환
        ///</summary>
        public string GetItemId()
        {
            string result = itemId;
            return result;
        }

        ///<summary>
        /// 판매 아이템 수량 반환
        ///</summary>
        public long GetItemCount()
        {
            long resolvedItemCount = itemCountValue > 0L ? itemCountValue : itemCount;
            long result = System.Math.Max( 1L, resolvedItemCount );
            return result;
        }

        ///<summary>
        /// 구매 가격 아이템 ID 반환
        ///</summary>
        public string GetPriceItemId()
        {
            string result = string.IsNullOrWhiteSpace( priceItemId ) ? DefaultPriceItemId : priceItemId.Trim();
            return result;
        }

        ///<summary>
        /// 구매 가격 수량 반환
        ///</summary>
        public long GetPriceAmount()
        {
            long resolvedPriceAmount = priceAmountValue > 0L ? priceAmountValue : priceAmount;
            long result = System.Math.Max( 0L, resolvedPriceAmount );
            return result;
        }

        ///<summary>
        /// 판매 아이템 ID 설정
        ///</summary>
        public void SetItemId( string _itemId )
        {
            itemId = string.IsNullOrWhiteSpace( _itemId ) ? string.Empty : _itemId.Trim();
        }

        ///<summary>
        /// 판매 아이템 수량 설정
        ///</summary>
        public void SetItemCount( long _itemCount )
        {
            long resolvedItemCount = System.Math.Max( 1L, _itemCount );
            itemCountValue = resolvedItemCount;
            itemCount = resolvedItemCount > int.MaxValue ? int.MaxValue : ( int )resolvedItemCount;
        }

        ///<summary>
        /// 구매 가격 아이템 ID 설정
        ///</summary>
        public void SetPriceItemId( string _priceItemId )
        {
            priceItemId = string.IsNullOrWhiteSpace( _priceItemId ) ? DefaultPriceItemId : _priceItemId.Trim();
        }

        ///<summary>
        /// 구매 가격 수량 설정
        ///</summary>
        public void SetPriceAmount( long _priceAmount )
        {
            long resolvedPriceAmount = System.Math.Max( 0L, _priceAmount );
            priceAmountValue = resolvedPriceAmount;
            priceAmount = resolvedPriceAmount > int.MaxValue ? int.MaxValue : ( int )resolvedPriceAmount;
        }
    }

    ///<summary>
    /// 상점 정의 데이터 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "ShopDefinition", menuName = "TinyHero/Data/Shop Definition" )]
    public sealed class CShopDefinition : ScriptableObject
    {
        [SerializeField] private string shopId = string.Empty;
        [SerializeField] private string shopName = string.Empty;
        [SerializeField] private List<CShopEntryData> shopEntryDataList = new List<CShopEntryData>();

        ///<summary>
        /// 상점 ID 반환
        ///</summary>
        public string GetShopId()
        {
            string result = shopId;
            return result;
        }

        ///<summary>
        /// 상점 이름 반환
        ///</summary>
        public string GetShopName()
        {
            string result = CDataManager.GetText( shopName );
            return result;
        }

        ///<summary>
        /// 상점 판매 목록 반환
        ///</summary>
        public List<CShopEntryData> GetShopEntryDataList()
        {
            List<CShopEntryData> result = shopEntryDataList;
            return result;
        }

        ///<summary>
        /// 상점 ID 설정
        ///</summary>
        public void SetShopId( string _shopId )
        {
            shopId = string.IsNullOrWhiteSpace( _shopId ) ? string.Empty : _shopId.Trim();
        }

        ///<summary>
        /// 상점 이름 설정
        ///</summary>
        public void SetShopName( string _shopName )
        {
            shopName = string.IsNullOrWhiteSpace( _shopName ) ? string.Empty : _shopName.Trim();
        }
    }
}
