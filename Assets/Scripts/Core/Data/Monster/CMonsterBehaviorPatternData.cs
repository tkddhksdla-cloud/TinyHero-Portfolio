using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 몬스터 공격 패턴 데이터
    ///</summary>
    [Serializable]
    public sealed class CMonsterAttackPatternData
    {
        [SerializeField] private bool useAttackPattern;
        [SerializeField] private float attackDistance = 1.0f;
        [SerializeField] private List<CMonsterBehaviorActionEntry> actionEntryList = new List<CMonsterBehaviorActionEntry>();

        ///<summary>
        /// 공격 패턴 사용 여부 반환
        ///</summary>
        public bool GetUseAttackPattern()
        {
            bool result = useAttackPattern;
            return result;
        }

        ///<summary>
        /// 공격 패턴 사용 여부 설정
        ///</summary>
        public void SetUseAttackPattern( bool _useAttackPattern )
        {
            useAttackPattern = _useAttackPattern;
        }

        ///<summary>
        /// 공격 거리 반환
        ///</summary>
        public float GetAttackDistance()
        {
            float result = attackDistance;
            return result;
        }

        ///<summary>
        /// 공격 거리 설정
        ///</summary>
        public void SetAttackDistance( float _attackDistance )
        {
            attackDistance = Mathf.Max( 0.0f, _attackDistance );
        }

        ///<summary>
        /// 공격 행동 목록 반환
        ///</summary>
        public List<CMonsterBehaviorActionEntry> GetActionEntryList()
        {
            List<CMonsterBehaviorActionEntry> result = actionEntryList;
            return result;
        }
    }

    ///<summary>
    /// 몬스터 상시 패턴 데이터
    ///</summary>
    [Serializable]
    public sealed class CMonsterAlwaysPatternData
    {
        [SerializeField] private List<CMonsterBehaviorActionEntry> actionEntryList = new List<CMonsterBehaviorActionEntry>();
        [SerializeField] private CMonsterAttackPatternData attackPatternData = new CMonsterAttackPatternData();

        ///<summary>
        /// 상시 행동 목록 반환
        ///</summary>
        public List<CMonsterBehaviorActionEntry> GetActionEntryList()
        {
            List<CMonsterBehaviorActionEntry> result = actionEntryList;
            return result;
        }

        ///<summary>
        /// 상시 공격 패턴 반환
        ///</summary>
        public CMonsterAttackPatternData GetAttackPatternData()
        {
            CMonsterAttackPatternData result = attackPatternData;
            return result;
        }
    }

    ///<summary>
    /// 몬스터 거리 반응 패턴 데이터
    ///</summary>
    [Serializable]
    public sealed class CMonsterPlayerDistancePatternData
    {
        [SerializeField] private float playerDistance = 2.0f;
        [SerializeField] private List<CMonsterBehaviorActionEntry> actionEntryList = new List<CMonsterBehaviorActionEntry>();
        [SerializeField] private CMonsterAttackPatternData attackPatternData = new CMonsterAttackPatternData();

        ///<summary>
        /// 플레이어 감지 거리 반환
        ///</summary>
        public float GetPlayerDistance()
        {
            float result = playerDistance;
            return result;
        }

        ///<summary>
        /// 플레이어 감지 거리 설정
        ///</summary>
        public void SetPlayerDistance( float _playerDistance )
        {
            playerDistance = Mathf.Max( 0.0f, _playerDistance );
        }

        ///<summary>
        /// 거리 행동 목록 반환
        ///</summary>
        public List<CMonsterBehaviorActionEntry> GetActionEntryList()
        {
            List<CMonsterBehaviorActionEntry> result = actionEntryList;
            return result;
        }

        ///<summary>
        /// 거리 공격 패턴 반환
        ///</summary>
        public CMonsterAttackPatternData GetAttackPatternData()
        {
            CMonsterAttackPatternData result = attackPatternData;
            return result;
        }
    }

    ///<summary>
    /// 몬스터 행동 패턴 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "MonsterBehaviorPatternData", menuName = "TinyHero/Data/Monster Behavior Pattern Data" )]
    public sealed class CMonsterBehaviorPatternData : ScriptableObject
    {
        [SerializeField] private string monsterId = string.Empty;
        [SerializeField] private float respawnDelaySeconds = 4.0f;
        [SerializeField] private CMonsterAlwaysPatternData alwaysPatternData = new CMonsterAlwaysPatternData();
        [SerializeField] private CMonsterPlayerDistancePatternData playerDistancePatternData = new CMonsterPlayerDistancePatternData();

        ///<summary>
        /// 몬스터 아이디 반환
        ///</summary>
        public string GetMonsterId()
        {
            string result = monsterId;
            return result;
        }

        ///<summary>
        /// 몬스터 아이디 설정
        ///</summary>
        public void SetMonsterId( string _monsterId )
        {
            monsterId = string.IsNullOrWhiteSpace( _monsterId ) ? string.Empty : _monsterId.Trim();
        }

        ///<summary>
        /// 몬스터 리스폰 대기 시간 반환
        ///</summary>
        public float GetRespawnDelaySeconds()
        {
            float result = respawnDelaySeconds;
            return result;
        }

        ///<summary>
        /// 몬스터 리스폰 대기 시간 설정
        ///</summary>
        public void SetRespawnDelaySeconds( float _respawnDelaySeconds )
        {
            respawnDelaySeconds = Mathf.Max( 0.0f, _respawnDelaySeconds );
        }

        ///<summary>
        /// 상시 패턴 반환
        ///</summary>
        public CMonsterAlwaysPatternData GetAlwaysPatternData()
        {
            CMonsterAlwaysPatternData result = alwaysPatternData;
            return result;
        }

        ///<summary>
        /// 거리 패턴 반환
        ///</summary>
        public CMonsterPlayerDistancePatternData GetPlayerDistancePatternData()
        {
            CMonsterPlayerDistancePatternData result = playerDistancePatternData;
            return result;
        }
    }
}
