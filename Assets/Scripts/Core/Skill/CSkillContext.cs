using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 실행 문맥 데이터
    ///</summary>
    public sealed class CSkillContext
    {
        private readonly CSkillManager skillManager;
        private readonly PlayerController playerController;
        private readonly CPlayerStatManager playerStatManager;
        private readonly CSkillDefinition skillDefinition;
        private readonly CSkillRuntimeData skillRuntimeData;
        private readonly Transform ownerTransform;

        ///<summary>
        /// 스킬 실행 문맥 생성자
        ///</summary>
        public CSkillContext( CSkillManager _skillManager, PlayerController _playerController, CPlayerStatManager _playerStatManager, CSkillDefinition _skillDefinition, CSkillRuntimeData _skillRuntimeData, Transform _ownerTransform )
        {
            skillManager = _skillManager;
            playerController = _playerController;
            playerStatManager = _playerStatManager;
            skillDefinition = _skillDefinition;
            skillRuntimeData = _skillRuntimeData;
            ownerTransform = _ownerTransform;
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
    }
}
