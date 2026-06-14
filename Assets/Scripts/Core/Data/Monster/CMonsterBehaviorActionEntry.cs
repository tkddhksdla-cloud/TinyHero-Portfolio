using System;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 몬스터 행동 가중치 엔트리
    ///</summary>
    [Serializable]
    public sealed class CMonsterBehaviorActionEntry
    {
        [SerializeField] private eMonsterBehaviorAction actionType = eMonsterBehaviorAction.IDLE;
        [SerializeField] private float weight = 1.0f;
        [SerializeField] private float durationSeconds = 1.0f;
        [SerializeField] private float cooldownSeconds = 0.25f;

        ///<summary>
        /// 행동 종류 반환
        ///</summary>
        public eMonsterBehaviorAction GetActionType()
        {
            eMonsterBehaviorAction result = actionType;
            return result;
        }

        ///<summary>
        /// 행동 종류 설정
        ///</summary>
        public void SetActionType( eMonsterBehaviorAction _actionType )
        {
            actionType = _actionType;
        }

        ///<summary>
        /// 가중치 반환
        ///</summary>
        public float GetWeight()
        {
            float result = weight;
            return result;
        }

        ///<summary>
        /// 가중치 설정
        ///</summary>
        public void SetWeight( float _weight )
        {
            weight = Mathf.Max( 0.0f, _weight );
        }

        ///<summary>
        /// 수행 시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            float result = durationSeconds;
            return result;
        }

        ///<summary>
        /// 수행 시간 설정
        ///</summary>
        public void SetDurationSeconds( float _durationSeconds )
        {
            durationSeconds = Mathf.Max( 0.0f, _durationSeconds );
        }

        ///<summary>
        /// 다음 행동 쿨타임 반환
        ///</summary>
        public float GetCooldownSeconds()
        {
            float result = cooldownSeconds;
            return result;
        }

        ///<summary>
        /// 다음 행동 쿨타임 설정
        ///</summary>
        public void SetCooldownSeconds( float _cooldownSeconds )
        {
            cooldownSeconds = Mathf.Max( 0.0f, _cooldownSeconds );
        }
    }
}
