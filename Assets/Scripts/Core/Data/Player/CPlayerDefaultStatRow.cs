using System;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 플레이어 기본 스탯 행 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerDefaultStatRow
    {
        [SerializeField]
        [CExcelHeader( "ATS" )]
        private float ats = 2.0f;

        [SerializeField]
        [CExcelHeader( "MOV" )]
        private float mov = 0.0f;

        [SerializeField]
        [CExcelHeader( "CRT" )]
        private float crt = 5.0f;

        [SerializeField]
        [CExcelHeader( "CRD" )]
        private float crd = 50.0f;

        [SerializeField]
        [CExcelHeader( "ACC" )]
        private float acc = 0.0f;

        [SerializeField]
        [CExcelHeader( "RNG" )]
        private float range = 0.0f;

        ///<summary>
        /// 기본 공격 주기 반환
        ///</summary>
        public float GetAts()
        {
            float result = ats;
            return result;
        }

        ///<summary>
        /// 기본 이동 속도 반환
        ///</summary>
        public float GetMov()
        {
            float result = mov;
            return result;
        }

        ///<summary>
        /// 기본 크리티컬 확률 반환
        ///</summary>
        public float GetCrt()
        {
            float result = crt;
            return result;
        }

        ///<summary>
        /// 기본 크리티컬 피해율 반환
        ///</summary>
        public float GetCrd()
        {
            float result = crd;
            return result;
        }

        ///<summary>
        /// 기본 정확도 반환
        ///</summary>
        public float GetAcc()
        {
            float result = acc;
            return result;
        }

        ///<summary>
        /// 기본 공격 범위 배율 반환
        ///</summary>
        public float GetRange()
        {
            float result = range;
            return result;
        }
    }
}
