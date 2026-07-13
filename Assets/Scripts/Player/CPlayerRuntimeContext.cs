using TinyHero.Quest;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 런타임 상태와 기능 참조 컨텍스트
    ///</summary>
    public sealed class CPlayerRuntimeContext : MonoBehaviour
    {
        [Header( "플레이어 런타임" )]
        [SerializeField] private CPlayerStatManager playerStatManager;
        [SerializeField] private CPlayerInventoryManager playerInventoryManager;
        [SerializeField] private CPlayerEquipmentManager playerEquipmentManager;
        [SerializeField] private CQuestStateProvider questStateProvider;
        [SerializeField] private CQuestManager questManager;
        [SerializeField] private CSkillManager skillManager;

        private PlayerController playerController;

        ///<summary>
        /// 플레이어 아바타와 런타임 기능 연결
        ///</summary>
        public bool BindPlayerController( PlayerController _playerController )
        {
            if ( _playerController == null || HasRequiredManagers() == false )
            {
                return false;
            }

            playerController = _playerController;
            playerEquipmentManager.BindStatManager( playerStatManager );
            questManager.BindRuntimeReferences( playerController, playerStatManager, playerInventoryManager, questStateProvider );
            skillManager.BindRuntimeReferences( playerController, playerStatManager, questStateProvider );
            playerController.BindRuntimeContext( this );
            return true;
        }

        ///<summary>
        /// 현재 플레이어 아바타 연결 해제
        ///</summary>
        public void UnbindPlayerController( PlayerController _playerController )
        {
            if ( playerController != _playerController )
            {
                return;
            }

            questManager.BindRuntimeReferences( null, playerStatManager, playerInventoryManager, questStateProvider );
            skillManager.BindRuntimeReferences( null, playerStatManager, questStateProvider );
            playerController = null;
        }

        ///<summary>
        /// 플레이어 제어 컴포넌트 반환
        ///</summary>
        public PlayerController GetPlayerController()
        {
            PlayerController result = playerController;
            return result;
        }

        ///<summary>
        /// 플레이어 스탯 매니저 반환
        ///</summary>
        public CPlayerStatManager GetStatManager()
        {
            CPlayerStatManager result = playerStatManager;
            return result;
        }

        ///<summary>
        /// 플레이어 인벤토리 매니저 반환
        ///</summary>
        public CPlayerInventoryManager GetInventoryManager()
        {
            CPlayerInventoryManager result = playerInventoryManager;
            return result;
        }

        ///<summary>
        /// 플레이어 장비 매니저 반환
        ///</summary>
        public CPlayerEquipmentManager GetEquipmentManager()
        {
            CPlayerEquipmentManager result = playerEquipmentManager;
            return result;
        }

        ///<summary>
        /// 퀘스트 상태 제공자 반환
        ///</summary>
        public CQuestStateProvider GetQuestStateProvider()
        {
            CQuestStateProvider result = questStateProvider;
            return result;
        }

        ///<summary>
        /// 플레이어 퀘스트 매니저 반환
        ///</summary>
        public CQuestManager GetQuestManager()
        {
            CQuestManager result = questManager;
            return result;
        }

        ///<summary>
        /// 플레이어 스킬 매니저 반환
        ///</summary>
        public CSkillManager GetSkillManager()
        {
            CSkillManager result = skillManager;
            return result;
        }

        ///<summary>
        /// 필수 런타임 매니저 참조 유효성 확인
        ///</summary>
        private bool HasRequiredManagers()
        {
            bool result = playerStatManager != null
                && playerInventoryManager != null
                && playerEquipmentManager != null
                && questStateProvider != null
                && questManager != null
                && skillManager != null;

            if ( result == false )
            {
                Debug.LogError( "[ PlayerRuntime ] One or more player runtime manager references are missing.", this );
            }

            return result;
        }
    }
}
