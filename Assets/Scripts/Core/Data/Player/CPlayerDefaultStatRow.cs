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
        private float ats = 1.0f;

        [SerializeField]
        [CExcelHeader( "MOV" )]
        private float mov = 4.5f;

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
    }
}
