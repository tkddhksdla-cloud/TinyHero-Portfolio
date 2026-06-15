using System;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 플레이어 레벨 스탯 행 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerLevelStatRow
    {
        [SerializeField]
        [CExcelHeader( "LV" )]
        private int lv = 1;

        [SerializeField]
        [CExcelHeader( "NeedExp" )]
        private float needExp;

        [SerializeField]
        [CExcelHeader( "HP" )]
        private float hp = 100.0f;

        [SerializeField]
        [CExcelHeader( "MP" )]
        private float mp = 50.0f;

        [SerializeField]
        [CExcelHeader( "ATK" )]
        private float atk = 3.0f;

        [SerializeField]
        [CExcelHeader( "DEF" )]
        private float def;

        ///<summary>
        /// 레벨 반환
        ///</summary>
        public int GetLv()
        {
            int result = lv;
            return result;
        }

        ///<summary>
        /// 누적 필요 경험치 반환
        ///</summary>
        public float GetNeedExp()
        {
            float result = needExp;
            return result;
        }

        ///<summary>
        /// 체력 반환
        ///</summary>
        public float GetHp()
        {
            float result = hp;
            return result;
        }

        ///<summary>
        /// 마나 반환
        ///</summary>
        public float GetMp()
        {
            float result = mp;
            return result;
        }

        ///<summary>
        /// 공격력 반환
        ///</summary>
        public float GetAtk()
        {
            float result = atk;
            return result;
        }

        ///<summary>
        /// 방어력 반환
        ///</summary>
        public float GetDef()
        {
            float result = def;
            return result;
        }
    }
}
