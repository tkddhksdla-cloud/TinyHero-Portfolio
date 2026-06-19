using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// NPC 상호작용 액션 종류
    ///</summary>
    public enum eNPCInteractionAction
    {
        DIALOGUE,
        QUEST,
        SHOP
    }

    ///<summary>
    /// NPC 대화 프리셋 데이터
    ///</summary>
    [Serializable]
    public sealed class CNPCDialoguePreset
    {
        [SerializeField] private string presetName = "Preset";
        [SerializeField] private List<string> dialogueLineList = new List<string>();

        ///<summary>
        /// 프리셋 이름 반환
        ///</summary>
        public string GetPresetName()
        {
            string result = presetName;
            return result;
        }

        ///<summary>
        /// 프리셋 이름 설정
        ///</summary>
        public void SetPresetName( string _presetName )
        {
            presetName = string.IsNullOrWhiteSpace( _presetName ) ? "Preset" : _presetName.Trim();
        }

        ///<summary>
        /// 대화 라인 목록 반환
        ///</summary>
        public List<string> GetDialogueLineList()
        {
            List<string> result = dialogueLineList;
            return result;
        }
    }

    ///<summary>
    /// NPC 상호작용 엔트리 데이터
    ///</summary>
    [Serializable]
    public sealed class CNPCInteractionActionEntry
    {
        [SerializeField] private eNPCInteractionAction actionType = eNPCInteractionAction.DIALOGUE;
        [SerializeField] private bool useDialogue = true;
        [SerializeField] private List<CNPCDialoguePreset> dialoguePresetList = new List<CNPCDialoguePreset>();
        [SerializeField] private string linkedQuestId = string.Empty;
        [SerializeField] private string linkedShopId = string.Empty;

        ///<summary>
        /// 액션 종류 반환
        ///</summary>
        public eNPCInteractionAction GetActionType()
        {
            eNPCInteractionAction result = actionType;
            return result;
        }

        ///<summary>
        /// 액션 종류 설정
        ///</summary>
        public void SetActionType( eNPCInteractionAction _actionType )
        {
            actionType = _actionType;
        }

        ///<summary>
        /// 대화 사용 여부 반환
        ///</summary>
        public bool GetUseDialogue()
        {
            bool result = useDialogue;
            return result;
        }

        ///<summary>
        /// 대화 사용 여부 설정
        ///</summary>
        public void SetUseDialogue( bool _useDialogue )
        {
            useDialogue = _useDialogue;
        }

        ///<summary>
        /// 대화 프리셋 목록 반환
        ///</summary>
        public List<CNPCDialoguePreset> GetDialoguePresetList()
        {
            List<CNPCDialoguePreset> result = dialoguePresetList;
            return result;
        }

        ///<summary>
        /// 연결 퀘스트 ID 반환
        ///</summary>
        public string GetLinkedQuestId()
        {
            string result = linkedQuestId;
            return result;
        }

        ///<summary>
        /// 연결 퀘스트 ID 설정
        ///</summary>
        public void SetLinkedQuestId( string _linkedQuestId )
        {
            linkedQuestId = string.IsNullOrWhiteSpace( _linkedQuestId ) ? string.Empty : _linkedQuestId.Trim();
        }

        ///<summary>
        /// 연결 상점 ID 반환
        ///</summary>
        public string GetLinkedShopId()
        {
            string result = linkedShopId;
            return result;
        }

        ///<summary>
        /// 연결 상점 ID 설정
        ///</summary>
        public void SetLinkedShopId( string _linkedShopId )
        {
            linkedShopId = string.IsNullOrWhiteSpace( _linkedShopId ) ? string.Empty : _linkedShopId.Trim();
        }
    }

    ///<summary>
    /// NPC 상호작용 데이터 자산
    ///</summary>
    [CreateAssetMenu( fileName = "NPCInteractionData", menuName = "TinyHero/Data/NPC Interaction Data" )]
    public sealed class CNPCInteractionData : ScriptableObject
    {
        [SerializeField] private string npcId = string.Empty;
        [SerializeField] private string npcName = string.Empty;
        [SerializeField] private List<CNPCInteractionActionEntry> actionEntryList = new List<CNPCInteractionActionEntry>();

        ///<summary>
        /// NPC ID 반환
        ///</summary>
        public string GetNpcId()
        {
            string result = npcId;
            return result;
        }

        ///<summary>
        /// NPC ID 설정
        ///</summary>
        public void SetNpcId( string _npcId )
        {
            npcId = string.IsNullOrWhiteSpace( _npcId ) ? string.Empty : _npcId.Trim();
        }

        ///<summary>
        /// NPC 이름 반환
        ///</summary>
        public string GetNpcName()
        {
            string result = npcName;
            return result;
        }

        ///<summary>
        /// NPC 이름 설정
        ///</summary>
        public void SetNpcName( string _npcName )
        {
            npcName = string.IsNullOrWhiteSpace( _npcName ) ? string.Empty : _npcName.Trim();
        }

        ///<summary>
        /// 상호작용 엔트리 목록 반환
        ///</summary>
        public List<CNPCInteractionActionEntry> GetActionEntryList()
        {
            List<CNPCInteractionActionEntry> result = actionEntryList;
            return result;
        }
    }
}
