using System;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 특수 보정 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerModifierRuntimeData
    {
        [SerializeField] private float expGainPercent;
        [SerializeField] private float goldGainPercent;
        [SerializeField] private float finalAttackPercent;

        ///<summary>
        /// 경험치 획득량 증가 수치 반환
        ///</summary>
        public float GetExpGainPercent()
        {
            float result = expGainPercent;
            return result;
        }

        ///<summary>
        /// 골드 획득량 증가 수치 반환
        ///</summary>
        public float GetGoldGainPercent()
        {
            float result = goldGainPercent;
            return result;
        }

        ///<summary>
        /// 최종 공격력 증가 수치 반환
        ///</summary>
        public float GetFinalAttackPercent()
        {
            float result = finalAttackPercent;
            return result;
        }

        ///<summary>
        /// 경험치 획득량 증가 수치 설정
        ///</summary>
        public void SetExpGainPercent( float _value )
        {
            expGainPercent = _value;
        }

        ///<summary>
        /// 골드 획득량 증가 수치 설정
        ///</summary>
        public void SetGoldGainPercent( float _value )
        {
            goldGainPercent = _value;
        }

        ///<summary>
        /// 최종 공격력 증가 수치 설정
        ///</summary>
        public void SetFinalAttackPercent( float _value )
        {
            finalAttackPercent = _value;
        }

        ///<summary>
        /// 경험치 획득량 증가 수치 누적
        ///</summary>
        public void AddExpGainPercent( float _value )
        {
            expGainPercent += _value;
        }

        ///<summary>
        /// 골드 획득량 증가 수치 누적
        ///</summary>
        public void AddGoldGainPercent( float _value )
        {
            goldGainPercent += _value;
        }

        ///<summary>
        /// 최종 공격력 증가 수치 누적
        ///</summary>
        public void AddFinalAttackPercent( float _value )
        {
            finalAttackPercent += _value;
        }

        ///<summary>
        /// 다른 특수 보정 데이터 누적
        ///</summary>
        public void AddFrom( CPlayerModifierRuntimeData _sourceData )
        {
            if ( _sourceData == null )
            {
                return;
            }

            expGainPercent += _sourceData.expGainPercent;
            goldGainPercent += _sourceData.goldGainPercent;
            finalAttackPercent += _sourceData.finalAttackPercent;
        }

        ///<summary>
        /// 다른 특수 보정 데이터 복사
        ///</summary>
        public void CopyFrom( CPlayerModifierRuntimeData _sourceData )
        {
            if ( _sourceData == null )
            {
                Clear();
                return;
            }

            expGainPercent = _sourceData.expGainPercent;
            goldGainPercent = _sourceData.goldGainPercent;
            finalAttackPercent = _sourceData.finalAttackPercent;
        }

        ///<summary>
        /// 특수 보정 데이터 초기화
        ///</summary>
        public void Clear()
        {
            expGainPercent = 0.0f;
            goldGainPercent = 0.0f;
            finalAttackPercent = 0.0f;
        }
    }
}
