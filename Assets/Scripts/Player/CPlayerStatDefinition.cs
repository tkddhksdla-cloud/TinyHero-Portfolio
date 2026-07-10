using System;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 기본 스탯 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerStatDefinition
    {
        [SerializeField] private float hp = 100.0f;
        [SerializeField] private float hr = 0.0f;
        [SerializeField] private float mp = 50.0f;
        [SerializeField] private float mr = 0.0f;
        [SerializeField] private float atk = 3.0f;
        [SerializeField] private float def = 0.0f;
        [SerializeField] private float crt = 5.0f;
        [SerializeField] private float crd = 50.0f;
        [SerializeField] private float acc = 0.0f;
        [HideInInspector] [SerializeField] private float ats = 2.0f;
        [SerializeField] private float move = 0.0f;
        [SerializeField] private float range = 0.0f;
        [SerializeField] private float cdr = 0.0f;

        ///<summary>
        /// 스탯 값 반환
        ///</summary>
        public float GetStatValue( ePlayerStatType _statType )
        {
            switch ( _statType )
            {
                case ePlayerStatType.HP:
                    return hp;

                case ePlayerStatType.HR:
                    return hr;

                case ePlayerStatType.MP:
                    return mp;

                case ePlayerStatType.MR:
                    return mr;

                case ePlayerStatType.ATK:
                    return atk;

                case ePlayerStatType.DEF:
                    return def;

                case ePlayerStatType.CRT:
                    return crt;

                case ePlayerStatType.CRD:
                    return crd;

                case ePlayerStatType.ACC:
                    return acc;

                case ePlayerStatType.ATS:
                    return ats;

                case ePlayerStatType.MOVE:
                    return move;

                case ePlayerStatType.RANGE:
                    return range;

                case ePlayerStatType.CDR:
                    return cdr;
            }

            return 0.0f;
        }

        ///<summary>
        /// 스탯 값 설정
        ///</summary>
        public void SetStatValue( ePlayerStatType _statType, float _value )
        {
            switch ( _statType )
            {
                case ePlayerStatType.HP:
                    hp = _value;
                    break;

                case ePlayerStatType.HR:
                    hr = _value;
                    break;

                case ePlayerStatType.MP:
                    mp = _value;
                    break;

                case ePlayerStatType.MR:
                    mr = _value;
                    break;

                case ePlayerStatType.ATK:
                    atk = _value;
                    break;

                case ePlayerStatType.DEF:
                    def = _value;
                    break;

                case ePlayerStatType.CRT:
                    crt = _value;
                    break;

                case ePlayerStatType.CRD:
                    crd = _value;
                    break;

                case ePlayerStatType.ACC:
                    acc = _value;
                    break;

                case ePlayerStatType.ATS:
                    ats = _value;
                    break;

                case ePlayerStatType.MOVE:
                    move = _value;
                    break;

                case ePlayerStatType.RANGE:
                    range = _value;
                    break;

                case ePlayerStatType.CDR:
                    cdr = _value;
                    break;
            }
        }
    }
}
