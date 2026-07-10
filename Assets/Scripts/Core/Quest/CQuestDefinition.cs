using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Quest
{
    ///<summary>
    /// 퀘스트 유형
    ///</summary>
    public enum eQuestType
    {
        NORMAL,
        REPEATABLE
    }

    ///<summary>
    /// 퀘스트 진행 상태
    ///</summary>
    public enum eQuestStatus
    {
        ACCEPTABLE,
        IN_PROGRESS,
        COMPLETE_WAIT,
        COMPLETE
    }

    ///<summary>
    /// 퀘스트 조건 유형
    ///</summary>
    public enum eQuestConditionType
    {
        KILL_MONSTER,
        REACH_LEVEL,
        TURN_IN_ITEM
    }

    ///<summary>
    /// 퀘스트 보상 유형
    ///</summary>
    public enum eQuestRewardType
    {
        EXP,
        ITEM
    }

    ///<summary>
    /// NPC 퀘스트 상호작용 유형
    ///</summary>
    public enum eQuestNpcInteractionType
    {
        NONE,
        ACCEPT,
        PROGRESS,
        SUBMIT,
        CLAIM,
        COMPLETE
    }

    ///<summary>
    /// 퀘스트 조건 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestConditionEntry
    {
        [SerializeField] private string conditionId = string.Empty;
        [SerializeField] private eQuestConditionType conditionType = eQuestConditionType.KILL_MONSTER;
        [SerializeField] private string targetMonsterId = string.Empty;
        [SerializeField] private int requiredKillCount = 1;
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private CItemDefinition targetItemDefinition;
        [SerializeField] [HideInInspector] private int requiredItemCount = 1;
        [SerializeField] private long requiredItemCountValue = 1L;

        ///<summary>
        /// 조건 ID 반환
        ///</summary>
        public string GetConditionId()
        {
            string result = conditionId;
            return result;
        }

        ///<summary>
        /// 조건 ID 설정
        ///</summary>
        public void SetConditionId( string _conditionId )
        {
            conditionId = string.IsNullOrWhiteSpace( _conditionId ) ? string.Empty : _conditionId.Trim();
        }

        ///<summary>
        /// 조건 유형 반환
        ///</summary>
        public eQuestConditionType GetConditionType()
        {
            eQuestConditionType result = conditionType;
            return result;
        }

        ///<summary>
        /// 조건 유형 설정
        ///</summary>
        public void SetConditionType( eQuestConditionType _conditionType )
        {
            conditionType = _conditionType;
        }

        ///<summary>
        /// 대상 몬스터 ID 반환
        ///</summary>
        public string GetTargetMonsterId()
        {
            string result = targetMonsterId;
            return result;
        }

        ///<summary>
        /// 대상 몬스터 ID 설정
        ///</summary>
        public void SetTargetMonsterId( string _targetMonsterId )
        {
            targetMonsterId = string.IsNullOrWhiteSpace( _targetMonsterId ) ? string.Empty : _targetMonsterId.Trim();
        }

        ///<summary>
        /// 필요 처치 횟수 반환
        ///</summary>
        public int GetRequiredKillCount()
        {
            int result = Mathf.Max( 1, requiredKillCount );
            return result;
        }

        ///<summary>
        /// 필요 처치 횟수 설정
        ///</summary>
        public void SetRequiredKillCount( int _requiredKillCount )
        {
            requiredKillCount = Mathf.Max( 1, _requiredKillCount );
        }

        ///<summary>
        /// 필요 레벨 반환
        ///</summary>
        public int GetRequiredLevel()
        {
            int result = Mathf.Max( 1, requiredLevel );
            return result;
        }

        ///<summary>
        /// 필요 레벨 설정
        ///</summary>
        public void SetRequiredLevel( int _requiredLevel )
        {
            requiredLevel = Mathf.Max( 1, _requiredLevel );
        }

        ///<summary>
        /// 대상 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetTargetItemDefinition()
        {
            CItemDefinition result = targetItemDefinition;
            return result;
        }

        ///<summary>
        /// 대상 아이템 정의 설정
        ///</summary>
        public void SetTargetItemDefinition( CItemDefinition _targetItemDefinition )
        {
            targetItemDefinition = _targetItemDefinition;
        }

        ///<summary>
        /// 필요 아이템 개수 반환
        ///</summary>
        public long GetRequiredItemCount()
        {
            long resolvedRequiredItemCount = requiredItemCountValue > 0L ? requiredItemCountValue : requiredItemCount;
            long result = Math.Max( 1L, resolvedRequiredItemCount );
            return result;
        }

        ///<summary>
        /// 필요 아이템 개수 설정
        ///</summary>
        public void SetRequiredItemCount( long _requiredItemCount )
        {
            long resolvedRequiredItemCount = Math.Max( 1L, _requiredItemCount );
            requiredItemCountValue = resolvedRequiredItemCount;
            requiredItemCount = resolvedRequiredItemCount > int.MaxValue ? int.MaxValue : ( int )resolvedRequiredItemCount;
        }
    }

    ///<summary>
    /// 퀘스트 보상 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestRewardEntry
    {
        [SerializeField] private eQuestRewardType rewardType = eQuestRewardType.EXP;
        [SerializeField] private int expAmount = 10;
        [SerializeField] private CItemDefinition itemDefinition;
        [SerializeField] [HideInInspector] private int itemCount = 1;
        [SerializeField] private long itemCountValue = 1L;

        ///<summary>
        /// 보상 유형 반환
        ///</summary>
        public eQuestRewardType GetRewardType()
        {
            eQuestRewardType result = rewardType;
            return result;
        }

        ///<summary>
        /// 보상 유형 설정
        ///</summary>
        public void SetRewardType( eQuestRewardType _rewardType )
        {
            rewardType = _rewardType;
        }

        ///<summary>
        /// 경험치 보상량 반환
        ///</summary>
        public int GetExpAmount()
        {
            int result = Mathf.Max( 0, expAmount );
            return result;
        }

        ///<summary>
        /// 경험치 보상량 설정
        ///</summary>
        public void SetExpAmount( int _expAmount )
        {
            expAmount = Mathf.Max( 0, _expAmount );
        }

        ///<summary>
        /// 아이템 보상 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinition()
        {
            CItemDefinition result = itemDefinition;
            return result;
        }

        ///<summary>
        /// 아이템 보상 정의 설정
        ///</summary>
        public void SetItemDefinition( CItemDefinition _itemDefinition )
        {
            itemDefinition = _itemDefinition;
        }

        ///<summary>
        /// 아이템 보상 개수 반환
        ///</summary>
        public long GetItemCount()
        {
            long resolvedItemCount = itemCountValue > 0L ? itemCountValue : itemCount;
            long result = Math.Max( 1L, resolvedItemCount );
            return result;
        }

        ///<summary>
        /// 아이템 보상 개수 설정
        ///</summary>
        public void SetItemCount( long _itemCount )
        {
            long resolvedItemCount = Math.Max( 1L, _itemCount );
            itemCountValue = resolvedItemCount;
            itemCount = resolvedItemCount > int.MaxValue ? int.MaxValue : ( int )resolvedItemCount;
        }
    }

    ///<summary>
    /// 퀘스트 조건 진행 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestConditionProgressData
    {
        [SerializeField] private string conditionId = string.Empty;
        [SerializeField] private int currentValue;
        [SerializeField] private bool isCompleted;

        ///<summary>
        /// 조건 ID 반환
        ///</summary>
        public string GetConditionId()
        {
            string result = conditionId;
            return result;
        }

        ///<summary>
        /// 조건 ID 설정
        ///</summary>
        public void SetConditionId( string _conditionId )
        {
            conditionId = string.IsNullOrWhiteSpace( _conditionId ) ? string.Empty : _conditionId.Trim();
        }

        ///<summary>
        /// 현재 진행값 반환
        ///</summary>
        public int GetCurrentValue()
        {
            int result = Mathf.Max( 0, currentValue );
            return result;
        }

        ///<summary>
        /// 현재 진행값 설정
        ///</summary>
        public void SetCurrentValue( int _currentValue )
        {
            currentValue = Mathf.Max( 0, _currentValue );
        }

        ///<summary>
        /// 완료 여부 반환
        ///</summary>
        public bool GetIsCompleted()
        {
            bool result = isCompleted;
            return result;
        }

        ///<summary>
        /// 완료 여부 설정
        ///</summary>
        public void SetIsCompleted( bool _isCompleted )
        {
            isCompleted = _isCompleted;
        }

        ///<summary>
        /// 조건 진행도 초기화
        ///</summary>
        public void ResetProgress()
        {
            currentValue = 0;
            isCompleted = false;
        }
    }

    ///<summary>
    /// 퀘스트 런타임 엔트리 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestRuntimeEntryData
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private eQuestStatus questStatus = eQuestStatus.ACCEPTABLE;
        [SerializeField] private string acceptedNpcId = string.Empty;
        [SerializeField] private int acceptCount;
        [SerializeField] private int completeCount;
        [SerializeField] private List<CQuestConditionProgressData> conditionProgressList = new List<CQuestConditionProgressData>();

        ///<summary>
        /// 퀘스트 ID 반환
        ///</summary>
        public string GetQuestId()
        {
            string result = questId;
            return result;
        }

        ///<summary>
        /// 퀘스트 ID 설정
        ///</summary>
        public void SetQuestId( string _questId )
        {
            questId = string.IsNullOrWhiteSpace( _questId ) ? string.Empty : _questId.Trim();
        }

        ///<summary>
        /// 퀘스트 상태 반환
        ///</summary>
        public eQuestStatus GetQuestStatus()
        {
            eQuestStatus result = questStatus;
            return result;
        }

        ///<summary>
        /// 퀘스트 상태 설정
        ///</summary>
        public void SetQuestStatus( eQuestStatus _questStatus )
        {
            questStatus = _questStatus;
        }

        ///<summary>
        /// 수락 NPC ID 반환
        ///</summary>
        public string GetAcceptedNpcId()
        {
            string result = acceptedNpcId;
            return result;
        }

        ///<summary>
        /// 수락 NPC ID 설정
        ///</summary>
        public void SetAcceptedNpcId( string _acceptedNpcId )
        {
            acceptedNpcId = string.IsNullOrWhiteSpace( _acceptedNpcId ) ? string.Empty : _acceptedNpcId.Trim();
        }

        ///<summary>
        /// 수락 횟수 반환
        ///</summary>
        public int GetAcceptCount()
        {
            int result = Mathf.Max( 0, acceptCount );
            return result;
        }

        ///<summary>
        /// 수락 횟수 증가
        ///</summary>
        public void IncreaseAcceptCount()
        {
            acceptCount = Mathf.Max( 0, acceptCount ) + 1;
        }

        ///<summary>
        /// 완료 횟수 반환
        ///</summary>
        public int GetCompleteCount()
        {
            int result = Mathf.Max( 0, completeCount );
            return result;
        }

        ///<summary>
        /// 완료 횟수 증가
        ///</summary>
        public void IncreaseCompleteCount()
        {
            completeCount = Mathf.Max( 0, completeCount ) + 1;
        }

        ///<summary>
        /// 조건 진행 목록 반환
        ///</summary>
        public List<CQuestConditionProgressData> GetConditionProgressList()
        {
            List<CQuestConditionProgressData> result = conditionProgressList;
            return result;
        }
    }

    ///<summary>
    /// 퀘스트 스냅샷 데이터
    ///</summary>
    [Serializable]
    public sealed class CQuestRuntimeSnapshotData
    {
        [SerializeField] private List<string> completedQuestIdList = new List<string>();
        [SerializeField] private List<CQuestRuntimeEntryData> runtimeEntryList = new List<CQuestRuntimeEntryData>();

        ///<summary>
        /// 완료 퀘스트 ID 목록 반환
        ///</summary>
        public List<string> GetCompletedQuestIdList()
        {
            List<string> result = completedQuestIdList;
            return result;
        }

        ///<summary>
        /// 완료 퀘스트 ID 목록 설정
        ///</summary>
        public void SetCompletedQuestIdList( List<string> _completedQuestIdList )
        {
            completedQuestIdList = _completedQuestIdList ?? new List<string>();
        }

        ///<summary>
        /// 런타임 엔트리 목록 반환
        ///</summary>
        public List<CQuestRuntimeEntryData> GetRuntimeEntryList()
        {
            List<CQuestRuntimeEntryData> result = runtimeEntryList;
            return result;
        }

        ///<summary>
        /// 런타임 엔트리 목록 설정
        ///</summary>
        public void SetRuntimeEntryList( List<CQuestRuntimeEntryData> _runtimeEntryList )
        {
            runtimeEntryList = _runtimeEntryList ?? new List<CQuestRuntimeEntryData>();
        }
    }

    ///<summary>
    /// 퀘스트 정의 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "QuestDefinition", menuName = "TinyHero/Quest/Quest Definition" )]
    public sealed class CQuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private string questName = string.Empty;
        [SerializeField] [TextArea] private string description = string.Empty;
        [SerializeField] private eQuestType questType = eQuestType.NORMAL;
        [SerializeField] private string giverNpcId = string.Empty;
        [SerializeField] private string completerNpcId = string.Empty;
        [SerializeField] private bool useAcceptDialogue;
        [SerializeField] private CNPCDialoguePreset acceptDialoguePreset = new CNPCDialoguePreset();
        [SerializeField] private bool useCompleteDialogue;
        [SerializeField] private CNPCDialoguePreset completeDialoguePreset = new CNPCDialoguePreset();
        [SerializeField] private List<CQuestConditionEntry> conditionEntryList = new List<CQuestConditionEntry>();
        [SerializeField] private List<CQuestRewardEntry> rewardEntryList = new List<CQuestRewardEntry>();

        ///<summary>
        /// 퀘스트 ID 반환
        ///</summary>
        public string GetQuestId()
        {
            string result = questId;
            return result;
        }

        ///<summary>
        /// 퀘스트 ID 설정
        ///</summary>
        public void SetQuestId( string _questId )
        {
            questId = string.IsNullOrWhiteSpace( _questId ) ? string.Empty : _questId.Trim();
        }

        ///<summary>
        /// 퀘스트 이름 반환
        ///</summary>
        public string GetQuestName()
        {
            string resolvedQuestName = string.IsNullOrWhiteSpace( questName ) ? questId : questName;
            string result = CDataManager.GetText( resolvedQuestName );
            return result;
        }

        ///<summary>
        /// 퀘스트 이름 설정
        ///</summary>
        public void SetQuestName( string _questName )
        {
            questName = string.IsNullOrWhiteSpace( _questName ) ? string.Empty : _questName.Trim();
        }

        ///<summary>
        /// 퀘스트 설명 반환
        ///</summary>
        public string GetDescription()
        {
            string descriptionTemplate = CDataManager.GetText( description );
            string result = CQuestDescriptionFormatter.Format( this, descriptionTemplate );
            return result;
        }

        ///<summary>
        /// 퀘스트 설명 원문 반환
        ///</summary>
        public string GetDescriptionTemplate()
        {
            string result = description;
            return result;
        }

        ///<summary>
        /// 퀘스트 설명 설정
        ///</summary>
        public void SetDescription( string _description )
        {
            description = string.IsNullOrWhiteSpace( _description ) ? string.Empty : _description.Trim();
        }

        ///<summary>
        /// 퀘스트 유형 반환
        ///</summary>
        public eQuestType GetQuestType()
        {
            eQuestType result = questType;
            return result;
        }

        ///<summary>
        /// 퀘스트 유형 설정
        ///</summary>
        public void SetQuestType( eQuestType _questType )
        {
            questType = _questType;
        }

        ///<summary>
        /// 의뢰 NPC ID 반환
        ///</summary>
        public string GetGiverNpcId()
        {
            string result = giverNpcId;
            return result;
        }

        ///<summary>
        /// 의뢰 NPC ID 설정
        ///</summary>
        public void SetGiverNpcId( string _giverNpcId )
        {
            giverNpcId = string.IsNullOrWhiteSpace( _giverNpcId ) ? string.Empty : _giverNpcId.Trim();
        }

        ///<summary>
        /// 완료 NPC ID 반환
        ///</summary>
        public string GetCompleterNpcId()
        {
            string resolvedNpcId = string.IsNullOrWhiteSpace( completerNpcId ) ? giverNpcId : completerNpcId;
            string result = resolvedNpcId;
            return result;
        }

        ///<summary>
        /// 완료 NPC ID 설정
        ///</summary>
        public void SetCompleterNpcId( string _completerNpcId )
        {
            completerNpcId = string.IsNullOrWhiteSpace( _completerNpcId ) ? string.Empty : _completerNpcId.Trim();
        }

        ///<summary>
        /// 수락 대화 사용 여부 반환
        ///</summary>
        public bool GetUseAcceptDialogue()
        {
            bool result = useAcceptDialogue;
            return result;
        }

        ///<summary>
        /// 수락 대화 프리셋 반환
        ///</summary>
        public CNPCDialoguePreset GetAcceptDialoguePreset()
        {
            CNPCDialoguePreset result = acceptDialoguePreset;
            return result;
        }

        ///<summary>
        /// 완료 대화 사용 여부 반환
        ///</summary>
        public bool GetUseCompleteDialogue()
        {
            bool result = useCompleteDialogue;
            return result;
        }

        ///<summary>
        /// 완료 대화 프리셋 반환
        ///</summary>
        public CNPCDialoguePreset GetCompleteDialoguePreset()
        {
            CNPCDialoguePreset result = completeDialoguePreset;
            return result;
        }

        ///<summary>
        /// 조건 목록 반환
        ///</summary>
        public List<CQuestConditionEntry> GetConditionEntryList()
        {
            List<CQuestConditionEntry> result = conditionEntryList;
            return result;
        }

        ///<summary>
        /// 보상 목록 반환
        ///</summary>
        public List<CQuestRewardEntry> GetRewardEntryList()
        {
            List<CQuestRewardEntry> result = rewardEntryList;
            return result;
        }
    }
}
