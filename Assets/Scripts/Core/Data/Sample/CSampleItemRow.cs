using System;
using UnityEngine;

namespace TinyHero.Core.Data.Sample
{
    /// <summary>
    /// 샘플 아이템 테이블의 한 row 데이터를 나타낸다.
    /// </summary>
    [Serializable]
    public sealed class CSampleItemRow
    {
        [SerializeField] private int id;
        [SerializeField] private string itemName = string.Empty;
        [SerializeField] private int price;

        /// <summary>
        /// 아이템 고유 식별자를 반환한다.
        /// </summary>
        public int GetId()
        {
            int result = id;
            return result;
        }

        /// <summary>
        /// 아이템 이름을 반환한다.
        /// </summary>
        public string GetItemName()
        {
            string result = itemName;
            return result;
        }

        /// <summary>
        /// 아이템 가격을 반환한다.
        /// </summary>
        public int GetPrice()
        {
            int result = price;
            return result;
        }
    }
}
