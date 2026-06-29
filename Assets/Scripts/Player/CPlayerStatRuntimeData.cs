using System;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 추가 스탯 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerStatRuntimeData
    {
        [SerializeField] private float hp;
        [SerializeField] private float hr;
        [SerializeField] private float mp;
        [SerializeField] private float mr;
        [SerializeField] private float atk;
        [SerializeField] private float def;
        [SerializeField] private float crt;
        [SerializeField] private float crd;
        [SerializeField] private float acc;
        [SerializeField] private float ats;
        [SerializeField] private float move;
        [SerializeField] private float range;

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
            }
        }

        ///<summary>
        /// 스탯 값 누적
        ///</summary>
        public void AddStatValue( ePlayerStatType _statType, float _value )
        {
            float currentValue = GetStatValue( _statType );
            float nextValue = currentValue + _value;
            SetStatValue( _statType, nextValue );
        }

        ///<summary>
        /// 다른 스탯 데이터 누적
        ///</summary>
        public void AddFrom( CPlayerStatRuntimeData _sourceData )
        {
            if ( _sourceData == null )
            {
                return;
            }

            Array statTypeArray = Enum.GetValues( typeof( ePlayerStatType ) );

            for ( int index = 0; index < statTypeArray.Length; index++ )
            {
                object statTypeObject = statTypeArray.GetValue( index );

                if ( statTypeObject == null )
                {
                    continue;
                }

                ePlayerStatType statType = ( ePlayerStatType )statTypeObject;
                float bonusValue = _sourceData.GetStatValue( statType );

                if ( Mathf.Approximately( bonusValue, 0.0f ) )
                {
                    continue;
                }

                AddStatValue( statType, bonusValue );
            }
        }

        ///<summary>
        /// 데이터 전체 복사
        ///</summary>
        public void CopyFrom( CPlayerStatRuntimeData _sourceData )
        {
            if ( _sourceData == null )
            {
                Clear();
                return;
            }

            hp = _sourceData.hp;
            hr = _sourceData.hr;
            mp = _sourceData.mp;
            mr = _sourceData.mr;
            atk = _sourceData.atk;
            def = _sourceData.def;
            crt = _sourceData.crt;
            crd = _sourceData.crd;
            acc = _sourceData.acc;
            ats = _sourceData.ats;
            move = _sourceData.move;
            range = _sourceData.range;
        }

        ///<summary>
        /// 데이터 초기화
        ///</summary>
        public void Clear()
        {
            hp = 0.0f;
            hr = 0.0f;
            mp = 0.0f;
            mr = 0.0f;
            atk = 0.0f;
            def = 0.0f;
            crt = 0.0f;
            crd = 0.0f;
            acc = 0.0f;
            ats = 0.0f;
            move = 0.0f;
            range = 0.0f;
        }
    }
}
