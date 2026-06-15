using System;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 몬스터 스탯 행 클래스
    ///</summary>
    [Serializable]
    public sealed class CMonsterStatRow
    {
        [SerializeField]
        [CExcelHeader( "Id" )]
        private string id = string.Empty;

        [SerializeField]
        [CExcelHeader( "NAME" )]
        private string name = string.Empty;

        [SerializeField]
        [CExcelHeader( "HP" )]
        private long hp;

        [SerializeField]
        [CExcelHeader( "LV" )]
        private long lv;

        [SerializeField]
        [CExcelHeader( "ATK" )]
        private long atk;

        [SerializeField]
        [CExcelHeader( "DEF" )]
        private long def;

        [SerializeField]
        [CExcelHeader( "ATS" )]
        private long ats;

        [SerializeField]
        [CExcelHeader( "MVS" )]
        private long mvs;

        [SerializeField]
        [CExcelHeader( "EXP" )]
        private long exp;

        [SerializeField]
        [CExcelHeader( "AT_Available" )]
        private bool atAvailable;

        ///<summary>
        /// 몬스터 식별자 반환
        ///</summary>
        public string GetId()
        {
            string result = id;
            return result;
        }

        ///<summary>
        /// 몬스터 이름 반환
        ///</summary>
        public string GetName()
        {
            string result = name;
            return result;
        }

        ///<summary>
        /// 체력 반환
        ///</summary>
        public long GetHp()
        {
            long result = hp;
            return result;
        }

        ///<summary>
        /// 레벨 반환
        ///</summary>
        public long GetLv()
        {
            long result = lv;
            return result;
        }

        ///<summary>
        /// 공격력 반환
        ///</summary>
        public long GetAtk()
        {
            long result = atk;
            return result;
        }

        ///<summary>
        /// 방어력 반환
        ///</summary>
        public long GetDef()
        {
            long result = def;
            return result;
        }

        ///<summary>
        /// 공격 속도 반환
        ///</summary>
        public long GetAts()
        {
            long result = ats;
            return result;
        }

        ///<summary>
        /// 이동 속도 반환
        ///</summary>
        public long GetMvs()
        {
            long result = mvs;
            return result;
        }

        ///<summary>
        /// 경험치 보상 반환
        ///</summary>
        public long GetExp()
        {
            long result = exp;
            return result;
        }

        ///<summary>
        /// 직접 공격 가능 여부 반환
        ///</summary>
        public bool GetAtAvailable()
        {
            bool result = atAvailable;
            return result;
        }
    }
}
