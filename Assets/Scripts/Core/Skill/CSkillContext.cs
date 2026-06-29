using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 실행 문맥 데이터
    ///</summary>
    public sealed class CSkillContext
    {
        private static int nextExecutionId = 1;

        private readonly CSkillManager skillManager;
        private readonly PlayerController playerController;
        private readonly CPlayerStatManager playerStatManager;
        private readonly CSkillDefinition skillDefinition;
        private readonly CSkillRuntimeData skillRuntimeData;
        private readonly Transform ownerTransform;
        private readonly float attackStatOverride;
        private readonly float skillAttackPowerMultiplierOverride;
        private readonly int executionId;

        ///<summary>
        /// 스킬 실행 문맥 생성자
        ///</summary>
        public CSkillContext( CSkillManager _skillManager, PlayerController _playerController, CPlayerStatManager _playerStatManager, CSkillDefinition _skillDefinition, CSkillRuntimeData _skillRuntimeData, Transform _ownerTransform )
            : this( _skillManager, _playerController, _playerStatManager, _skillDefinition, _skillRuntimeData, _ownerTransform, -1.0f, -1.0f )
        {
        }

        ///<summary>
        /// 스킬 실행 문맥 생성
        ///</summary>
        public CSkillContext( CSkillManager _skillManager, PlayerController _playerController, CPlayerStatManager _playerStatManager, CSkillDefinition _skillDefinition, CSkillRuntimeData _skillRuntimeData, Transform _ownerTransform, float _attackStatOverride, float _skillAttackPowerMultiplierOverride )
        {
            skillManager = _skillManager;
            playerController = _playerController;
            playerStatManager = _playerStatManager;
            skillDefinition = _skillDefinition;
            skillRuntimeData = _skillRuntimeData;
            ownerTransform = _ownerTransform;
            attackStatOverride = _attackStatOverride;
            skillAttackPowerMultiplierOverride = _skillAttackPowerMultiplierOverride;
            executionId = AllocateExecutionId();
        }

        ///<summary>
        /// 스킬 매니저 반환
        ///</summary>
        public CSkillManager GetSkillManager()
        {
            CSkillManager result = skillManager;
            return result;
        }

        ///<summary>
        /// 플레이어 컨트롤러 반환
        ///</summary>
        public PlayerController GetPlayerController()
        {
            PlayerController result = playerController;
            return result;
        }

        ///<summary>
        /// 플레이어 스탯 매니저 반환
        ///</summary>
        public CPlayerStatManager GetPlayerStatManager()
        {
            CPlayerStatManager result = playerStatManager;
            return result;
        }

        ///<summary>
        /// 스킬 정의 반환
        ///</summary>
        public CSkillDefinition GetSkillDefinition()
        {
            CSkillDefinition result = skillDefinition;
            return result;
        }

        ///<summary>
        /// 스킬 런타임 데이터 반환
        ///</summary>
        public CSkillRuntimeData GetSkillRuntimeData()
        {
            CSkillRuntimeData result = skillRuntimeData;
            return result;
        }

        ///<summary>
        /// 소유자 트랜스폼 반환
        ///</summary>
        public Transform GetOwnerTransform()
        {
            Transform result = ownerTransform;
            return result;
        }

        ///<summary>
        /// 공격력 오버라이드 반환
        ///</summary>
        public float GetAttackStatOverride()
        {
            float result = attackStatOverride;
            return result;
        }

        ///<summary>
        /// 스킬 공격 배수 오버라이드 반환
        ///</summary>
        public float GetSkillAttackPowerMultiplierOverride()
        {
            float result = skillAttackPowerMultiplierOverride;
            return result;
        }

        ///<summary>
        /// 스킬 실행 식별자 반환
        ///</summary>
        public int GetExecutionId()
        {
            int result = executionId;
            return result;
        }

        ///<summary>
        /// 스킬 범위 배율 반환
        ///</summary>
        public float GetRangeMultiplier()
        {
            if ( playerStatManager == null )
            {
                return 1.0f;
            }

            float result = playerStatManager.GetRangeMultiplier();
            return result;
        }

        ///<summary>
        /// 스킬 거리 값 배율 적용
        ///</summary>
        public float ScaleRangeValue( float _value )
        {
            float rangeMultiplier = GetRangeMultiplier();
            float result = _value * rangeMultiplier;
            return result;
        }

        ///<summary>
        /// 스킬 이차원 오프셋 배율 적용
        ///</summary>
        public Vector2 ScaleRangeOffset( Vector2 _offset )
        {
            float rangeMultiplier = GetRangeMultiplier();
            Vector2 result = _offset * rangeMultiplier;
            return result;
        }

        ///<summary>
        /// 스킬 삼차원 오프셋 배율 적용
        ///</summary>
        public Vector3 ScaleRangeOffset( Vector3 _offset )
        {
            float rangeMultiplier = GetRangeMultiplier();
            Vector3 result = _offset * rangeMultiplier;
            return result;
        }

        ///<summary>
        /// 스킬 실행 식별자 발급
        ///</summary>
        private static int AllocateExecutionId()
        {
            int issuedExecutionId = nextExecutionId;
            nextExecutionId++;

            if ( nextExecutionId <= 0 )
            {
                nextExecutionId = 1;
            }

            int result = issuedExecutionId;
            return result;
        }
    }
}
