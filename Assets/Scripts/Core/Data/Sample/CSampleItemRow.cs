using System;
using UnityEngine;

namespace TinyHero.Core.Data.Sample
{
    ///<summary>
    /// 샘플 아이템 행 클래스
    ///</summary>
    [Serializable]
    public sealed class CSampleItemRow
    {
        [SerializeField] private int id;
        [SerializeField] private string itemName = string.Empty;
        [SerializeField] private int price;

        ///<summary>
        /// ID 반환
        ///</summary>
        public int GetId()
        {
            int result = id;
            return result;
        }

        ///<summary>
        /// 아이템 이름 반환
        ///</summary>
        public string GetItemName()
        {
            string result = itemName;
            return result;
        }

        ///<summary>
        /// 가격 반환
        ///</summary>
        public int GetPrice()
        {
            int result = price;
            return result;
        }
    }
}


