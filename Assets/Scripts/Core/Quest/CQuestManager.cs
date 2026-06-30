using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Skill;
using TinyHero.UI;
using UnityEngine;

namespace TinyHero.Quest
{
    ///<summary>
    /// 플레이어 퀘스트 진행 관리 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    [RequireComponent( typeof( CQuestStateProvider ) )]
    [RequireComponent( typeof( CPlayerInventoryManager ) )]
    public sealed class CQuestManager : MonoBehaviour
    {
        private const string MonsterStatTableResourcePath = "Data/Monster/MonsterStatTableData";
        private const string QuestAcceptedToastMessagePrefix = "퀘스트 수락";
        private const string QuestAbandonedToastMessagePrefix = "퀘스트 포기";
        private const string QuestToastMessageFormat = "{0}: {1}";

        [SerializeField] private PlayerController targetPlayerController;
        [SerializeField] private CPlayerStatManager targetPlayerStatManager;
        [SerializeField] private CPlayerInventoryManager targetPlayerInventoryManager;
        [SerializeField] private CQuestStateProvider targetQuestStateProvider;

        private static CMonsterStatTableData cachedMonsterStatTableData;

        public event Action<string> OnQuestUpdated;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 활성화 구독 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            RefreshLevelConditionProgress();
            RefreshTurnInConditionProgress();
        }

        ///<summary>
        /// 비활성화 구독 해제 처리
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        ///<summary>
        /// 퀘스트 상태 제공자 반환
        ///</summary>
        public CQuestStateProvider GetQuestStateProvider()
        {
            CQuestStateProvider result = targetQuestStateProvider;
            return result;
        }

        ///<summary>
        /// 퀘스트 진행 상태 강제 동기화
        ///</summary>
        public void RefreshQuestProgressState()
        {
            ResolveReferences();
            RefreshLevelConditionProgress();
            RefreshTurnInConditionProgress();
        }

        ///<summary>
        /// 퀘스트 정의 데이터 조회 시도
        ///</summary>
        public bool TryGetQuestDefinition( string _questId, out CQuestDefinition _questDefinition )
        {
            bool result = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out _questDefinition );
            return result;
        }

        ///<summary>
        /// 퀘스트 상태 반환
        ///</summary>
        public eQuestStatus GetQuestStatus( string _questId )
        {
            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                eQuestStatus missingStatus = eQuestStatus.COMPLETE;
                return missingStatus;
            }

            if ( targetQuestStateProvider != null && targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData ) )
            {
                eQuestStatus runtimeStatus = runtimeEntryData.GetQuestStatus();
                return runtimeStatus;
            }

            if ( targetQuestStateProvider != null && targetQuestStateProvider.IsQuestCompleted( _questId ) && questDefinition.GetQuestType() == eQuestType.NORMAL )
            {
                eQuestStatus completeStatus = eQuestStatus.COMPLETE;
                return completeStatus;
            }

            eQuestStatus acceptableStatus = eQuestStatus.ACCEPTABLE;
            return acceptableStatus;
        }

        ///<summary>
        /// NPC 상호작용 유형 평가
        ///</summary>
        public eQuestNpcInteractionType EvaluateNpcInteractionType( CNPCObject _npcObject, string _questId )
        {
            if ( _npcObject == null || string.IsNullOrWhiteSpace( _questId ) )
            {
                eQuestNpcInteractionType noneType = eQuestNpcInteractionType.NONE;
                return noneType;
            }

            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                eQuestNpcInteractionType noneType = eQuestNpcInteractionType.NONE;
                return noneType;
            }

            string npcId = _npcObject.GetNpcId();
            eQuestStatus questStatus = GetQuestStatus( _questId );
            bool isGiverNpc = IsMatchedNpcId( npcId, questDefinition.GetGiverNpcId() );
            bool isCompleterNpc = IsMatchedNpcId( npcId, questDefinition.GetCompleterNpcId() );

            switch ( questStatus )
            {
                case eQuestStatus.ACCEPTABLE:
                {
                    eQuestNpcInteractionType acceptableType = isGiverNpc ? eQuestNpcInteractionType.ACCEPT : eQuestNpcInteractionType.NONE;
                    return acceptableType;
                }

                case eQuestStatus.IN_PROGRESS:
                {
                    CQuestRuntimeEntryData runtimeEntryData = null;
                    bool hasRuntimeEntry = targetQuestStateProvider != null && targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out runtimeEntryData );

                    if ( hasRuntimeEntry == false || runtimeEntryData == null )
                    {
                        eQuestNpcInteractionType progressType = eQuestNpcInteractionType.PROGRESS;
                        return progressType;
                    }

                    bool canSubmitTurnInQuest = isCompleterNpc && CanSubmitTurnInQuest( questDefinition, runtimeEntryData );
                    eQuestNpcInteractionType interactionType = canSubmitTurnInQuest ? eQuestNpcInteractionType.SUBMIT : eQuestNpcInteractionType.PROGRESS;
                    return interactionType;
                }

                case eQuestStatus.COMPLETE_WAIT:
                {
                    eQuestNpcInteractionType waitType = isCompleterNpc ? eQuestNpcInteractionType.CLAIM : eQuestNpcInteractionType.PROGRESS;
                    return waitType;
                }

                case eQuestStatus.COMPLETE:
                {
                    eQuestNpcInteractionType completeType = eQuestNpcInteractionType.COMPLETE;
                    return completeType;
                }
            }

            eQuestNpcInteractionType fallbackType = eQuestNpcInteractionType.NONE;
            return fallbackType;
        }

        ///<summary>
        /// NPC 퀘스트 상호작용 처리
        ///</summary>
        public bool ProcessNpcQuestInteraction( CNPCObject _npcObject, string _questId )
        {
            ResolveReferences();
            eQuestNpcInteractionType interactionType = EvaluateNpcInteractionType( _npcObject, _questId );

            switch ( interactionType )
            {
                case eQuestNpcInteractionType.ACCEPT:
                {
                    bool isAccepted = AcceptQuest( _questId, _npcObject.GetNpcId() );

                    if ( isAccepted )
                    {
                        Debug.Log( $"Quest accepted. QuestId: {_questId}" );
                    }

                    return isAccepted;
                }

                case eQuestNpcInteractionType.SUBMIT:
                {
                    bool isSubmitted = SubmitTurnInQuest( _questId, _npcObject.GetNpcId() );

                    if ( isSubmitted )
                    {
                        Debug.Log( $"Quest condition submitted. QuestId: {_questId}" );
                    }

                    return isSubmitted;
                }

                case eQuestNpcInteractionType.CLAIM:
                {
                    bool isClaimed = ClaimQuestReward( _questId, _npcObject.GetNpcId() );

                    if ( isClaimed )
                    {
                        Debug.Log( $"Quest reward claimed. QuestId: {_questId}" );
                    }

                    return isClaimed;
                }

                case eQuestNpcInteractionType.PROGRESS:
                {
                    string progressSummary = BuildQuestProgressSummary( _questId );
                    Debug.Log( progressSummary );
                    return false;
                }

                case eQuestNpcInteractionType.COMPLETE:
                {
                    Debug.Log( $"Quest already completed. QuestId: {_questId}" );
                    return false;
                }
            }

            return false;
        }

        ///<summary>
        /// 퀘스트 상호작용 대화 프리셋 조회 시도
        ///</summary>
        public bool TryGetQuestDialoguePreset( CNPCObject _npcObject, string _questId, out CNPCDialoguePreset _dialoguePreset, out eQuestNpcInteractionType _interactionType )
        {
            _dialoguePreset = null;
            _interactionType = EvaluateNpcInteractionType( _npcObject, _questId );

            if ( _interactionType != eQuestNpcInteractionType.ACCEPT && _interactionType != eQuestNpcInteractionType.CLAIM )
            {
                return false;
            }

            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                return false;
            }

            if ( _interactionType == eQuestNpcInteractionType.ACCEPT && questDefinition.GetUseAcceptDialogue() )
            {
                CNPCDialoguePreset dialoguePreset = questDefinition.GetAcceptDialoguePreset();

                if ( HasDialoguePresetContent( dialoguePreset ) )
                {
                    _dialoguePreset = dialoguePreset;
                    return true;
                }
            }

            if ( _interactionType == eQuestNpcInteractionType.CLAIM && questDefinition.GetUseCompleteDialogue() )
            {
                CNPCDialoguePreset dialoguePreset = questDefinition.GetCompleteDialoguePreset();

                if ( HasDialoguePresetContent( dialoguePreset ) )
                {
                    _dialoguePreset = dialoguePreset;
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 퀘스트 수락 처리
        ///</summary>
        public bool AcceptQuest( string _questId, string _npcId )
        {
            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                return false;
            }

            bool isGiverNpc = IsMatchedNpcId( _npcId, questDefinition.GetGiverNpcId() );

            if ( isGiverNpc == false )
            {
                return false;
            }

            eQuestStatus currentStatus = GetQuestStatus( _questId );

            if ( currentStatus != eQuestStatus.ACCEPTABLE )
            {
                return false;
            }

            CQuestRuntimeEntryData runtimeEntryData = targetQuestStateProvider.GetOrCreateRuntimeEntryData( _questId );
            runtimeEntryData.SetQuestStatus( eQuestStatus.IN_PROGRESS );
            runtimeEntryData.SetAcceptedNpcId( _npcId );
            runtimeEntryData.IncreaseAcceptCount();
            InitializeConditionProgressList( questDefinition, runtimeEntryData );
            RefreshConditionProgressForDefinition( questDefinition, runtimeEntryData );
            EvaluateQuestCompletionState( questDefinition, runtimeEntryData );
            string toastMessage = BuildQuestToastMessage( questDefinition, QuestAcceptedToastMessagePrefix );
            CToastMessageSystem.Show( toastMessage );
            NotifyQuestUpdated( _questId );
            return true;
        }

        ///<summary>
        /// 퀘스트 토스트 메시지 구성
        ///</summary>
        private string BuildQuestToastMessage( CQuestDefinition _questDefinition, string _prefix )
        {
            if ( string.IsNullOrWhiteSpace( _prefix ) )
            {
                return string.Empty;
            }

            if ( _questDefinition == null )
            {
                return _prefix;
            }

            string questName = _questDefinition.GetQuestName();

            if ( string.IsNullOrWhiteSpace( questName ) )
            {
                return _prefix;
            }

            string result = string.Format( QuestToastMessageFormat, _prefix, questName );
            return result;
        }

        ///<summary>
        /// 몬스터 처치 반영 처리
        ///</summary>
        public void NotifyMonsterKilled( string _monsterId )
        {
            if ( string.IsNullOrWhiteSpace( _monsterId ) || targetQuestStateProvider == null )
            {
                return;
            }

            List<CQuestRuntimeEntryData> runtimeEntryList = targetQuestStateProvider.GetRuntimeEntryList();
            bool hasUpdatedQuest = false;

            for ( int entryIndex = 0; entryIndex < runtimeEntryList.Count; entryIndex++ )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ entryIndex ];

                if ( runtimeEntryData == null || runtimeEntryData.GetQuestStatus() != eQuestStatus.IN_PROGRESS )
                {
                    continue;
                }

                bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( runtimeEntryData.GetQuestId(), out CQuestDefinition questDefinition );

                if ( hasDefinition == false || questDefinition == null )
                {
                    continue;
                }

                bool wasChanged = UpdateKillMonsterConditionProgress( questDefinition, runtimeEntryData, _monsterId );

                if ( wasChanged == false )
                {
                    continue;
                }

                EvaluateQuestCompletionState( questDefinition, runtimeEntryData );
                NotifyQuestUpdated( questDefinition.GetQuestId() );
                hasUpdatedQuest = true;
            }

            if ( hasUpdatedQuest == false )
            {
                return;
            }
        }

        ///<summary>
        /// 퀘스트 진행 요약 반환
        ///</summary>
        public string BuildQuestProgressSummary( string _questId )
        {
            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                string invalidMessage = $"Quest was not found. QuestId: {_questId}";
                return invalidMessage;
            }

            eQuestStatus questStatus = GetQuestStatus( _questId );
            string summary = $"Quest [{questDefinition.GetQuestName()}] Status: {questStatus}";

            if ( targetQuestStateProvider == null || targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData ) == false || runtimeEntryData == null )
            {
                return summary;
            }

            List<CQuestConditionEntry> conditionEntryList = questDefinition.GetConditionEntryList();
            string detailText = string.Empty;

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveConditionProgressData( runtimeEntryData, conditionEntry );
                string conditionLabel = BuildConditionLabel( conditionEntry );
                int currentValue = progressData != null ? progressData.GetCurrentValue() : 0;
                int targetValue = ResolveConditionTargetValue( conditionEntry );
                detailText += $"\n - {conditionLabel} : {currentValue}/{targetValue}";
            }

            string result = summary + detailText;
            return result;
        }

        ///<summary>
        /// 퀘스트 진행도 표시 문자열 구성 시도
        ///</summary>
        public bool TryBuildQuestProgressText( string _questId, out string _progressText )
        {
            _progressText = string.Empty;
            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                return false;
            }

            List<CQuestConditionEntry> conditionEntryList = questDefinition.GetConditionEntryList();

            if ( conditionEntryList == null || conditionEntryList.Count == 0 )
            {
                return false;
            }

            CQuestRuntimeEntryData runtimeEntryData = null;

            if ( targetQuestStateProvider != null )
            {
                targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out runtimeEntryData );
            }

            List<string> progressLineList = new List<string>();

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = runtimeEntryData != null ? ResolveConditionProgressData( runtimeEntryData, conditionEntry ) : null;
                int currentValue = ResolveQuestDisplayProgressValue( conditionEntry, progressData );
                int targetValue = ResolveConditionTargetValue( conditionEntry );
                string conditionLabel = BuildQuestConditionProgressLabel( conditionEntry );
                string progressLine = $"{conditionLabel} ( {currentValue} / {targetValue} )";
                progressLineList.Add( progressLine );
            }

            if ( progressLineList.Count == 0 )
            {
                return false;
            }

            _progressText = string.Join( "\n", progressLineList );
            return true;
        }

        ///<summary>
        /// 진행 중 퀘스트 포기 처리
        ///</summary>
        public bool AbandonQuest( string _questId )
        {
            if ( targetQuestStateProvider == null || string.IsNullOrWhiteSpace( _questId ) )
            {
                return false;
            }

            bool hasRuntimeEntry = targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData );

            if ( hasRuntimeEntry == false || runtimeEntryData == null )
            {
                return false;
            }

            eQuestStatus questStatus = runtimeEntryData.GetQuestStatus();

            if ( questStatus != eQuestStatus.IN_PROGRESS && questStatus != eQuestStatus.COMPLETE_WAIT )
            {
                return false;
            }

            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );
            runtimeEntryData.SetQuestStatus( eQuestStatus.ACCEPTABLE );
            runtimeEntryData.SetAcceptedNpcId( string.Empty );
            ResetConditionProgressList( runtimeEntryData );
            string toastMessage = BuildQuestToastMessage( questDefinition, QuestAbandonedToastMessagePrefix );
            CToastMessageSystem.Show( toastMessage );
            NotifyQuestUpdated( _questId );
            return true;
        }

        ///<summary>
        /// 의뢰품 제출 처리
        ///</summary>
        private bool SubmitTurnInQuest( string _questId, string _npcId )
        {
            ResolveReferences();

            if ( targetQuestStateProvider == null || targetPlayerInventoryManager == null )
            {
                return false;
            }

            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                return false;
            }

            bool isCompleterNpc = IsMatchedNpcId( _npcId, questDefinition.GetCompleterNpcId() );

            if ( isCompleterNpc == false )
            {
                return false;
            }

            bool hasRuntimeEntry = targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData );

            if ( hasRuntimeEntry == false || runtimeEntryData == null )
            {
                return false;
            }

            if ( runtimeEntryData.GetQuestStatus() != eQuestStatus.IN_PROGRESS )
            {
                return false;
            }

            bool canSubmit = CanSubmitTurnInQuest( questDefinition, runtimeEntryData );

            if ( canSubmit == false )
            {
                return false;
            }

            Dictionary<string, long> requiredItemCountByIdDictionary = new Dictionary<string, long>();
            List<CQuestConditionEntry> conditionEntryList = questDefinition.GetConditionEntryList();

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.TURN_IN_ITEM )
                {
                    continue;
                }

                CItemDefinition itemDefinition = conditionEntry.GetTargetItemDefinition();

                if ( itemDefinition == null )
                {
                    return false;
                }

                string itemId = itemDefinition.GetItemId();
                long requiredItemCount = conditionEntry.GetRequiredItemCount();

                if ( requiredItemCountByIdDictionary.ContainsKey( itemId ) )
                {
                    requiredItemCountByIdDictionary[ itemId ] += requiredItemCount;
                }
                else
                {
                    requiredItemCountByIdDictionary.Add( itemId, requiredItemCount );
                }
            }

            foreach ( KeyValuePair<string, long> pairData in requiredItemCountByIdDictionary )
            {
                bool isRemoved = targetPlayerInventoryManager.TryRemoveItem( pairData.Key, pairData.Value );

                if ( isRemoved == false )
                {
                    return false;
                }
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.TURN_IN_ITEM )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveOrCreateConditionProgressData( runtimeEntryData, conditionEntry );
                long requiredItemCount = conditionEntry.GetRequiredItemCount();
                int progressValue = requiredItemCount > int.MaxValue ? int.MaxValue : ( int )requiredItemCount;
                progressData.SetCurrentValue( progressValue );
                progressData.SetIsCompleted( true );
            }

            EvaluateQuestCompletionState( questDefinition, runtimeEntryData );
            NotifyQuestUpdated( _questId );
            return true;
        }

        ///<summary>
        /// 퀘스트 보상 지급 처리
        ///</summary>
        private bool ClaimQuestReward( string _questId, string _npcId )
        {
            ResolveReferences();

            if ( targetQuestStateProvider == null || targetPlayerInventoryManager == null )
            {
                return false;
            }

            bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( _questId, out CQuestDefinition questDefinition );

            if ( hasDefinition == false || questDefinition == null )
            {
                return false;
            }

            bool isCompleterNpc = IsMatchedNpcId( _npcId, questDefinition.GetCompleterNpcId() );

            if ( isCompleterNpc == false )
            {
                return false;
            }

            bool hasRuntimeEntry = targetQuestStateProvider.TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData );

            if ( hasRuntimeEntry == false || runtimeEntryData == null )
            {
                return false;
            }

            if ( runtimeEntryData.GetQuestStatus() != eQuestStatus.COMPLETE_WAIT )
            {
                return false;
            }

            bool canConsumeTurnInItems = CanConsumeTurnInItems( questDefinition );

            if ( canConsumeTurnInItems == false )
            {
                return false;
            }

            bool canGrantReward = CanGrantRewardList( questDefinition.GetRewardEntryList() );

            if ( canGrantReward == false )
            {
                Debug.LogWarning( $"Quest reward could not be granted because of missing inventory capacity. QuestId: {_questId}", this );
                return false;
            }

            bool wasConsumed = ConsumeTurnInItems( questDefinition );

            if ( wasConsumed == false )
            {
                return false;
            }

            List<CRewardItemData> grantedItemRewardList = new List<CRewardItemData>();
            bool wasGranted = GrantRewardList( questDefinition.GetRewardEntryList(), grantedItemRewardList );

            if ( wasGranted == false )
            {
                return false;
            }

            runtimeEntryData.IncreaseCompleteCount();

            if ( questDefinition.GetQuestType() == eQuestType.REPEATABLE )
            {
                runtimeEntryData.SetQuestStatus( eQuestStatus.ACCEPTABLE );
                runtimeEntryData.SetAcceptedNpcId( string.Empty );
                ResetConditionProgressList( runtimeEntryData );
                targetQuestStateProvider.UnmarkQuestCompleted( _questId );
            }
            else
            {
                targetQuestStateProvider.MarkQuestCompleted( _questId );
                runtimeEntryData.SetQuestStatus( eQuestStatus.COMPLETE );
            }

            NotifyQuestUpdated( _questId );
            ShowGrantedItemRewardPopup( grantedItemRewardList );
            return true;
        }

        ///<summary>
        /// 건네주기 아이템 소모 가능 여부 반환
        ///</summary>
        private bool CanConsumeTurnInItems( CQuestDefinition _questDefinition )
        {
            if ( _questDefinition == null || targetPlayerInventoryManager == null )
            {
                return false;
            }

            Dictionary<string, long> requiredItemCountByIdDictionary = BuildTurnInRequirementDictionary( _questDefinition );

            foreach ( KeyValuePair<string, long> pairData in requiredItemCountByIdDictionary )
            {
                bool hasItem = targetPlayerInventoryManager.HasItem( pairData.Key, pairData.Value );

                if ( hasItem == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 건네주기 아이템 소모 처리
        ///</summary>
        private bool ConsumeTurnInItems( CQuestDefinition _questDefinition )
        {
            if ( _questDefinition == null || targetPlayerInventoryManager == null )
            {
                return false;
            }

            Dictionary<string, long> requiredItemCountByIdDictionary = BuildTurnInRequirementDictionary( _questDefinition );

            foreach ( KeyValuePair<string, long> pairData in requiredItemCountByIdDictionary )
            {
                bool isRemoved = targetPlayerInventoryManager.TryRemoveItem( pairData.Key, pairData.Value );

                if ( isRemoved == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 레벨 조건 진행도 갱신
        ///</summary>
        private void RefreshLevelConditionProgress()
        {
            if ( targetQuestStateProvider == null || targetPlayerStatManager == null )
            {
                return;
            }

            List<CQuestRuntimeEntryData> runtimeEntryList = targetQuestStateProvider.GetRuntimeEntryList();

            for ( int entryIndex = 0; entryIndex < runtimeEntryList.Count; entryIndex++ )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ entryIndex ];

                if ( runtimeEntryData == null || runtimeEntryData.GetQuestStatus() != eQuestStatus.IN_PROGRESS )
                {
                    continue;
                }

                bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( runtimeEntryData.GetQuestId(), out CQuestDefinition questDefinition );

                if ( hasDefinition == false || questDefinition == null )
                {
                    continue;
                }

                bool wasChanged = UpdateReachLevelConditionProgress( questDefinition, runtimeEntryData, targetPlayerStatManager.GetCurrentLevel() );

                if ( wasChanged == false )
                {
                    continue;
                }

                EvaluateQuestCompletionState( questDefinition, runtimeEntryData );
                NotifyQuestUpdated( questDefinition.GetQuestId() );
            }
        }

        ///<summary>
        /// 의뢰품 조건 진행도 갱신
        ///</summary>
        private void RefreshTurnInConditionProgress()
        {
            ResolveReferences();

            if ( targetQuestStateProvider == null || targetPlayerInventoryManager == null )
            {
                return;
            }

            List<CQuestRuntimeEntryData> runtimeEntryList = targetQuestStateProvider.GetRuntimeEntryList();

            for ( int entryIndex = 0; entryIndex < runtimeEntryList.Count; entryIndex++ )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ entryIndex ];

                if ( runtimeEntryData == null || runtimeEntryData.GetQuestStatus() != eQuestStatus.IN_PROGRESS )
                {
                    continue;
                }

                bool hasDefinition = CQuestDefinitionDatabase.TryGetQuestDefinition( runtimeEntryData.GetQuestId(), out CQuestDefinition questDefinition );

                if ( hasDefinition == false || questDefinition == null )
                {
                    continue;
                }

                bool wasChanged = UpdateTurnInConditionProgress( questDefinition, runtimeEntryData );

                if ( wasChanged == false )
                {
                    continue;
                }

                EvaluateQuestCompletionState( questDefinition, runtimeEntryData );
                NotifyQuestUpdated( questDefinition.GetQuestId() );
            }
        }

        ///<summary>
        /// 조건 진행 목록 초기화
        ///</summary>
        private void InitializeConditionProgressList( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData )
        {
            List<CQuestConditionProgressData> conditionProgressList = _runtimeEntryData.GetConditionProgressList();
            conditionProgressList.Clear();
            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = new CQuestConditionProgressData();
                progressData.SetConditionId( ResolveConditionId( conditionEntry, index ) );
                conditionProgressList.Add( progressData );
            }
        }

        ///<summary>
        /// 조건 진행 초기값 반영
        ///</summary>
        private void RefreshConditionProgressForDefinition( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData )
        {
            if ( _questDefinition == null || _runtimeEntryData == null )
            {
                return;
            }

            UpdateReachLevelConditionProgress( _questDefinition, _runtimeEntryData, targetPlayerStatManager != null ? targetPlayerStatManager.GetCurrentLevel() : 1 );
            UpdateTurnInConditionProgress( _questDefinition, _runtimeEntryData );
        }

        ///<summary>
        /// 몬스터 처치 조건 진행도 갱신
        ///</summary>
        private bool UpdateKillMonsterConditionProgress( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData, string _monsterId )
        {
            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();
            bool wasChanged = false;
            string normalizedMonsterKey = NormalizeQuestMonsterKey( _monsterId );
            string resolvedMonsterName = ResolveQuestMonsterDisplayName( _monsterId );
            string normalizedMonsterName = NormalizeQuestMonsterKey( resolvedMonsterName );

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.KILL_MONSTER )
                {
                    continue;
                }

                string targetMonsterId = conditionEntry.GetTargetMonsterId();
                string normalizedTargetMonsterKey = NormalizeQuestMonsterKey( targetMonsterId );
                bool isMatchedMonster = string.Equals( normalizedTargetMonsterKey, normalizedMonsterKey, StringComparison.Ordinal );

                if ( isMatchedMonster == false && string.IsNullOrWhiteSpace( normalizedMonsterName ) == false )
                {
                    isMatchedMonster = string.Equals( normalizedTargetMonsterKey, normalizedMonsterName, StringComparison.Ordinal );
                }

                if ( isMatchedMonster == false )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveOrCreateConditionProgressData( _runtimeEntryData, conditionEntry );
                int currentValue = progressData.GetCurrentValue();
                int nextValue = Mathf.Min( conditionEntry.GetRequiredKillCount(), currentValue + 1 );

                if ( nextValue == currentValue && progressData.GetIsCompleted() )
                {
                    continue;
                }

                progressData.SetCurrentValue( nextValue );
                progressData.SetIsCompleted( nextValue >= conditionEntry.GetRequiredKillCount() );
                wasChanged = true;
            }

            return wasChanged;
        }

        ///<summary>
        /// 레벨 조건 진행도 갱신
        ///</summary>
        private bool UpdateReachLevelConditionProgress( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData, int _currentLevel )
        {
            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();
            bool wasChanged = false;

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.REACH_LEVEL )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveOrCreateConditionProgressData( _runtimeEntryData, conditionEntry );
                int currentValue = progressData.GetCurrentValue();
                bool wasCompleted = progressData.GetIsCompleted();
                int nextValue = Mathf.Max( currentValue, _currentLevel );
                bool isCompleted = nextValue >= conditionEntry.GetRequiredLevel();

                if ( nextValue == currentValue && wasCompleted == isCompleted )
                {
                    continue;
                }

                progressData.SetCurrentValue( nextValue );
                progressData.SetIsCompleted( isCompleted );
                wasChanged = true;
            }

            return wasChanged;
        }

        ///<summary>
        /// 의뢰품 조건 진행도 갱신
        ///</summary>
        private bool UpdateTurnInConditionProgress( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData )
        {
            if ( targetPlayerInventoryManager == null )
            {
                return false;
            }

            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();
            bool wasChanged = false;

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.TURN_IN_ITEM )
                {
                    continue;
                }

                CItemDefinition itemDefinition = conditionEntry.GetTargetItemDefinition();
                string itemId = itemDefinition != null ? itemDefinition.GetItemId() : string.Empty;
                long ownedCount = string.IsNullOrWhiteSpace( itemId ) ? 0L : targetPlayerInventoryManager.GetItemCount( itemId );
                CQuestConditionProgressData progressData = ResolveOrCreateConditionProgressData( _runtimeEntryData, conditionEntry );
                int currentValue = progressData.GetCurrentValue();
                bool wasCompleted = progressData.GetIsCompleted();
                bool isCompleted = ownedCount >= conditionEntry.GetRequiredItemCount();

                if ( currentValue == ownedCount && wasCompleted == isCompleted )
                {
                    continue;
                }

                int progressValue = ownedCount > int.MaxValue ? int.MaxValue : ( int )ownedCount;
                progressData.SetCurrentValue( progressValue );
                progressData.SetIsCompleted( isCompleted );
                wasChanged = true;
            }

            return wasChanged;
        }

        ///<summary>
        /// 퀘스트 진행도 표시값 결정
        ///</summary>
        private int ResolveQuestDisplayProgressValue( CQuestConditionEntry _conditionEntry, CQuestConditionProgressData _progressData )
        {
            if ( _conditionEntry == null )
            {
                return 0;
            }

            if ( _progressData != null )
            {
                int progressValue = _progressData.GetCurrentValue();
                return progressValue;
            }

            switch ( _conditionEntry.GetConditionType() )
            {
                case eQuestConditionType.KILL_MONSTER:
                    return 0;

                case eQuestConditionType.REACH_LEVEL:
                {
                    int currentLevel = targetPlayerStatManager != null ? targetPlayerStatManager.GetCurrentLevel() : 0;
                    return currentLevel;
                }

                case eQuestConditionType.TURN_IN_ITEM:
                {
                    CItemDefinition itemDefinition = _conditionEntry.GetTargetItemDefinition();
                    string itemId = itemDefinition != null ? itemDefinition.GetItemId() : string.Empty;
                    long ownedCount = string.IsNullOrWhiteSpace( itemId ) == false && targetPlayerInventoryManager != null ? targetPlayerInventoryManager.GetItemCount( itemId ) : 0L;
                    int progressValue = ownedCount > int.MaxValue ? int.MaxValue : ( int )ownedCount;
                    return progressValue;
                }
            }

            return 0;
        }

        ///<summary>
        /// 퀘스트 완료 대기 상태 평가
        ///</summary>
        private void EvaluateQuestCompletionState( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData )
        {
            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();

            if ( conditionEntryList.Count == 0 )
            {
                _runtimeEntryData.SetQuestStatus( eQuestStatus.COMPLETE_WAIT );
                return;
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveConditionProgressData( _runtimeEntryData, conditionEntry );
                bool isCompleted = progressData != null && progressData.GetIsCompleted();

                if ( isCompleted == false )
                {
                    return;
                }
            }

            _runtimeEntryData.SetQuestStatus( eQuestStatus.COMPLETE_WAIT );
        }

        ///<summary>
        /// 의뢰품 제출 가능 여부 반환
        ///</summary>
        private bool CanSubmitTurnInQuest( CQuestDefinition _questDefinition, CQuestRuntimeEntryData _runtimeEntryData )
        {
            if ( targetPlayerInventoryManager == null )
            {
                return false;
            }

            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();
            bool hasTurnInCondition = false;

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                if ( conditionEntry.GetConditionType() == eQuestConditionType.TURN_IN_ITEM )
                {
                    hasTurnInCondition = true;
                    continue;
                }

                CQuestConditionProgressData progressData = ResolveConditionProgressData( _runtimeEntryData, conditionEntry );

                if ( progressData == null || progressData.GetIsCompleted() == false )
                {
                    return false;
                }
            }

            if ( hasTurnInCondition == false )
            {
                return false;
            }

            Dictionary<string, long> requiredItemCountByIdDictionary = BuildTurnInRequirementDictionary( _questDefinition );

            foreach ( KeyValuePair<string, long> pairData in requiredItemCountByIdDictionary )
            {
                bool hasItem = targetPlayerInventoryManager.HasItem( pairData.Key, pairData.Value );

                if ( hasItem == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 건네주기 요구 아이템 집계 반환
        ///</summary>
        private Dictionary<string, long> BuildTurnInRequirementDictionary( CQuestDefinition _questDefinition )
        {
            Dictionary<string, long> requiredItemCountByIdDictionary = new Dictionary<string, long>();

            if ( _questDefinition == null )
            {
                return requiredItemCountByIdDictionary;
            }

            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();

            if ( conditionEntryList == null )
            {
                return requiredItemCountByIdDictionary;
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null || conditionEntry.GetConditionType() != eQuestConditionType.TURN_IN_ITEM )
                {
                    continue;
                }

                CItemDefinition itemDefinition = conditionEntry.GetTargetItemDefinition();

                if ( itemDefinition == null || string.IsNullOrWhiteSpace( itemDefinition.GetItemId() ) )
                {
                    continue;
                }

                string itemId = itemDefinition.GetItemId();
                long requiredItemCount = conditionEntry.GetRequiredItemCount();

                if ( requiredItemCountByIdDictionary.ContainsKey( itemId ) )
                {
                    requiredItemCountByIdDictionary[ itemId ] += requiredItemCount;
                }
                else
                {
                    requiredItemCountByIdDictionary.Add( itemId, requiredItemCount );
                }
            }

            return requiredItemCountByIdDictionary;
        }

        ///<summary>
        /// 보상 지급 가능 여부 반환
        ///</summary>
        private bool CanGrantRewardList( List<CQuestRewardEntry> _rewardEntryList )
        {
            if ( _rewardEntryList == null )
            {
                return true;
            }

            if ( _rewardEntryList.Count == 0 )
            {
                return true;
            }

            if ( targetPlayerInventoryManager == null )
            {
                for ( int index = 0; index < _rewardEntryList.Count; index++ )
                {
                    CQuestRewardEntry rewardEntry = _rewardEntryList[ index ];

                    if ( rewardEntry == null )
                    {
                        continue;
                    }

                    if ( rewardEntry.GetRewardType() == eQuestRewardType.ITEM )
                    {
                        return false;
                    }
                }

                return true;
            }

            List<CInventoryItemEntryData> simulatedEntryList = CreateInventorySimulationList();

            for ( int index = 0; index < _rewardEntryList.Count; index++ )
            {
                CQuestRewardEntry rewardEntry = _rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    continue;
                }

                if ( rewardEntry.GetRewardType() != eQuestRewardType.ITEM )
                {
                    continue;
                }

                CItemDefinition itemDefinition = rewardEntry.GetItemDefinition();

                if ( itemDefinition == null )
                {
                    return false;
                }

                bool canAdd = TrySimulateAddItem( simulatedEntryList, itemDefinition, rewardEntry.GetItemCount() );

                if ( canAdd == false )
                {
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 보상 지급 처리
        ///</summary>
        private bool GrantRewardList( List<CQuestRewardEntry> _rewardEntryList, List<CRewardItemData> _grantedItemRewardList )
        {
            if ( _rewardEntryList == null )
            {
                return true;
            }

            if ( _rewardEntryList.Count == 0 )
            {
                return true;
            }

            for ( int index = 0; index < _rewardEntryList.Count; index++ )
            {
                CQuestRewardEntry rewardEntry = _rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    continue;
                }

                switch ( rewardEntry.GetRewardType() )
                {
                    case eQuestRewardType.EXP:
                    {
                        if ( targetPlayerStatManager != null )
                        {
                            targetPlayerStatManager.AddExp( rewardEntry.GetExpAmount() );
                        }

                        break;
                    }

                    case eQuestRewardType.ITEM:
                    {
                        if ( targetPlayerInventoryManager == null )
                        {
                            return false;
                        }

                        CItemDefinition itemDefinition = rewardEntry.GetItemDefinition();

                        if ( itemDefinition == null )
                        {
                            return false;
                        }

                        bool isAdded = targetPlayerInventoryManager.TryAddItem( itemDefinition, rewardEntry.GetItemCount() );

                        if ( isAdded == false )
                        {
                            return false;
                        }

                        if ( _grantedItemRewardList != null )
                        {
                            CRewardItemData rewardItemData = new CRewardItemData( itemDefinition, rewardEntry.GetItemCount() );
                            _grantedItemRewardList.Add( rewardItemData );
                        }

                        break;
                    }
                }
            }

            return true;
        }

        ///<summary>
        /// 지급 아이템 보상 팝업 표시
        ///</summary>
        private void ShowGrantedItemRewardPopup( IReadOnlyList<CRewardItemData> _grantedItemRewardList )
        {
            if ( _grantedItemRewardList == null || _grantedItemRewardList.Count == 0 )
            {
                return;
            }

            CRewardUiManager rewardUiManager = CRewardUiManager.Instance;

            if ( rewardUiManager == null )
            {
                return;
            }

            rewardUiManager.ShowItemRewardList( _grantedItemRewardList );
        }

        ///<summary>
        /// 인벤토리 시뮬레이션 목록 생성
        ///</summary>
        private List<CInventoryItemEntryData> CreateInventorySimulationList()
        {
            IReadOnlyList<CInventoryItemEntryData> sourceEntryList = targetPlayerInventoryManager.GetItemEntryList();
            List<CInventoryItemEntryData> simulatedEntryList = new List<CInventoryItemEntryData>();

            for ( int index = 0; index < sourceEntryList.Count; index++ )
            {
                CInventoryItemEntryData sourceEntryData = sourceEntryList[ index ];
                CInventoryItemEntryData copiedEntryData = sourceEntryData != null ? sourceEntryData.CreateCopy() : new CInventoryItemEntryData();
                simulatedEntryList.Add( copiedEntryData );
            }

            return simulatedEntryList;
        }

        ///<summary>
        /// 아이템 추가 시뮬레이션 처리
        ///</summary>
        private bool TrySimulateAddItem( List<CInventoryItemEntryData> _simulatedEntryList, CItemDefinition _itemDefinition, long _count )
        {
            if ( _simulatedEntryList == null || _itemDefinition == null || _count <= 0 )
            {
                return false;
            }

            string itemId = _itemDefinition.GetItemId();
            long remainingCount = _count;

            if ( _itemDefinition.IsStackable() )
            {
                for ( int index = 0; index < _simulatedEntryList.Count; index++ )
                {
                    if ( remainingCount <= 0 )
                    {
                        break;
                    }

                    CInventoryItemEntryData entryData = _simulatedEntryList[ index ];

                    if ( entryData == null || entryData.IsEmpty() )
                    {
                        continue;
                    }

                    bool isMatched = string.Equals( entryData.GetItemId(), itemId, StringComparison.Ordinal );

                    if ( isMatched == false )
                    {
                        continue;
                    }

                    long availableCapacity = _itemDefinition.GetMaxStackCount() - entryData.GetQuantity();

                    if ( availableCapacity <= 0 )
                    {
                        continue;
                    }

                    long addedCount = System.Math.Min( availableCapacity, remainingCount );
                    entryData.SetQuantity( entryData.GetQuantity() + addedCount );
                    remainingCount -= addedCount;
                }
            }

            for ( int index = 0; index < _simulatedEntryList.Count; index++ )
            {
                if ( remainingCount <= 0 )
                {
                    break;
                }

                CInventoryItemEntryData entryData = _simulatedEntryList[ index ];

                if ( entryData == null )
                {
                    entryData = new CInventoryItemEntryData();
                    _simulatedEntryList[ index ] = entryData;
                }

                if ( entryData.IsEmpty() == false )
                {
                    continue;
                }

                long maxAddCount = _itemDefinition.IsStackable() ? _itemDefinition.GetMaxStackCount() : 1L;
                long addedCount = System.Math.Min( maxAddCount, remainingCount );
                entryData.SetItemId( itemId );
                entryData.SetQuantity( addedCount );
                remainingCount -= addedCount;
            }

            bool result = remainingCount <= 0;
            return result;
        }

        ///<summary>
        /// 조건 진행 목록 초기화
        ///</summary>
        private void ResetConditionProgressList( CQuestRuntimeEntryData _runtimeEntryData )
        {
            List<CQuestConditionProgressData> conditionProgressList = _runtimeEntryData.GetConditionProgressList();

            for ( int index = 0; index < conditionProgressList.Count; index++ )
            {
                CQuestConditionProgressData progressData = conditionProgressList[ index ];

                if ( progressData == null )
                {
                    continue;
                }

                progressData.ResetProgress();
            }
        }

        ///<summary>
        /// 조건 진행 데이터 조회
        ///</summary>
        private CQuestConditionProgressData ResolveConditionProgressData( CQuestRuntimeEntryData _runtimeEntryData, CQuestConditionEntry _conditionEntry )
        {
            if ( _runtimeEntryData == null || _conditionEntry == null )
            {
                return null;
            }

            string conditionId = ResolveConditionId( _conditionEntry, -1 );
            List<CQuestConditionProgressData> conditionProgressList = _runtimeEntryData.GetConditionProgressList();

            for ( int index = 0; index < conditionProgressList.Count; index++ )
            {
                CQuestConditionProgressData progressData = conditionProgressList[ index ];

                if ( progressData == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( progressData.GetConditionId(), conditionId, StringComparison.Ordinal );

                if ( isMatched )
                {
                    return progressData;
                }
            }

            return null;
        }

        ///<summary>
        /// 조건 진행 데이터 생성 보장
        ///</summary>
        private CQuestConditionProgressData ResolveOrCreateConditionProgressData( CQuestRuntimeEntryData _runtimeEntryData, CQuestConditionEntry _conditionEntry )
        {
            CQuestConditionProgressData progressData = ResolveConditionProgressData( _runtimeEntryData, _conditionEntry );

            if ( progressData != null )
            {
                return progressData;
            }

            CQuestConditionProgressData createdProgressData = new CQuestConditionProgressData();
            createdProgressData.SetConditionId( ResolveConditionId( _conditionEntry, _runtimeEntryData.GetConditionProgressList().Count ) );
            _runtimeEntryData.GetConditionProgressList().Add( createdProgressData );
            return createdProgressData;
        }

        ///<summary>
        /// 조건 ID 결정
        ///</summary>
        private string ResolveConditionId( CQuestConditionEntry _conditionEntry, int _fallbackIndex )
        {
            string conditionId = _conditionEntry.GetConditionId();

            if ( string.IsNullOrWhiteSpace( conditionId ) == false )
            {
                return conditionId;
            }

            int indexValue = Mathf.Max( 0, _fallbackIndex );
            string result = $"COND_{indexValue + 1:00}";
            return result;
        }

        ///<summary>
        /// 조건 목표값 반환
        ///</summary>
        private int ResolveConditionTargetValue( CQuestConditionEntry _conditionEntry )
        {
            switch ( _conditionEntry.GetConditionType() )
            {
                case eQuestConditionType.KILL_MONSTER:
                    return _conditionEntry.GetRequiredKillCount();

                case eQuestConditionType.REACH_LEVEL:
                    return _conditionEntry.GetRequiredLevel();

                case eQuestConditionType.TURN_IN_ITEM:
                {
                    long requiredItemCount = _conditionEntry.GetRequiredItemCount();
                    int displayValue = requiredItemCount > int.MaxValue ? int.MaxValue : ( int )requiredItemCount;
                    return displayValue;
                }
            }

            return 0;
        }

        ///<summary>
        /// 조건 표시 문자열 구성
        ///</summary>
        private string BuildConditionLabel( CQuestConditionEntry _conditionEntry )
        {
            switch ( _conditionEntry.GetConditionType() )
            {
                case eQuestConditionType.KILL_MONSTER:
                {
                    string result = $"Kill Monster ({_conditionEntry.GetTargetMonsterId()})";
                    return result;
                }

                case eQuestConditionType.REACH_LEVEL:
                {
                    string result = "Reach Level";
                    return result;
                }

                case eQuestConditionType.TURN_IN_ITEM:
                {
                    CItemDefinition itemDefinition = _conditionEntry.GetTargetItemDefinition();
                    string itemName = itemDefinition != null ? itemDefinition.GetItemName() : "Missing Item";
                    string result = $"Turn In Item ({itemName})";
                    return result;
                }
            }

            return "Unknown Condition";
        }

        ///<summary>
        /// 퀘스트 UI 진행도 조건 이름 구성
        ///</summary>
        private string BuildQuestConditionProgressLabel( CQuestConditionEntry _conditionEntry )
        {
            if ( _conditionEntry == null )
            {
                return "조건";
            }

            switch ( _conditionEntry.GetConditionType() )
            {
                case eQuestConditionType.KILL_MONSTER:
                {
                    string monsterName = ResolveQuestMonsterDisplayName( _conditionEntry.GetTargetMonsterId() );
                    string result = $"{monsterName} 처치";
                    return result;
                }

                case eQuestConditionType.REACH_LEVEL:
                    return "레벨 달성";

                case eQuestConditionType.TURN_IN_ITEM:
                {
                    CItemDefinition itemDefinition = _conditionEntry.GetTargetItemDefinition();
                    string itemName = itemDefinition != null ? itemDefinition.GetItemName() : "아이템";
                    string result = $"{itemName} 건네주기";
                    return result;
                }
            }

            return "조건";
        }

        ///<summary>
        /// 퀘스트 목표 몬스터 이름 결정
        ///</summary>
        private string ResolveQuestMonsterDisplayName( string _monsterId )
        {
            if ( string.IsNullOrWhiteSpace( _monsterId ) )
            {
                return "몬스터";
            }

            CMonsterStatTableData monsterStatTableData = ResolveMonsterStatTableData();

            if ( monsterStatTableData == null )
            {
                return _monsterId.Trim();
            }

            bool isFound = monsterStatTableData.TryGetRow( _monsterId.Trim(), out CMonsterStatRow rowData );

            if ( isFound == false || rowData == null || string.IsNullOrWhiteSpace( rowData.GetName() ) )
            {
                return _monsterId.Trim();
            }

            string result = rowData.GetName().Trim();
            return result;
        }

        ///<summary>
        /// 몬스터 스탯 테이블 데이터 결정
        ///</summary>
        ///<summary>
        /// 퀘스트 몬스터 키 정규화
        ///</summary>
        ///<summary>
        /// 퀘스트 몬스터 키 정규화
        ///</summary>
        private string NormalizeQuestMonsterKey( string _monsterKey )
        {
            if ( string.IsNullOrWhiteSpace( _monsterKey ) )
            {
                return string.Empty;
            }

            string trimmedMonsterKey = _monsterKey.Trim();
            string normalizedMonsterKey = trimmedMonsterKey.Replace( " ", string.Empty ).ToUpperInvariant();
            return normalizedMonsterKey;
        }

        ///<summary>
        /// 몬스터 스탯 테이블 데이터 결정
        ///</summary>
        private CMonsterStatTableData ResolveMonsterStatTableData()
        {
            if ( cachedMonsterStatTableData != null )
            {
                return cachedMonsterStatTableData;
            }

            CMonsterStatTableData loadedTableData = Resources.Load<CMonsterStatTableData>( MonsterStatTableResourcePath );
            cachedMonsterStatTableData = loadedTableData;
            return cachedMonsterStatTableData;
        }

        ///<summary>
        /// NPC ID 일치 여부 반환
        ///</summary>
        private bool IsMatchedNpcId( string _sourceNpcId, string _targetNpcId )
        {
            if ( string.IsNullOrWhiteSpace( _sourceNpcId ) || string.IsNullOrWhiteSpace( _targetNpcId ) )
            {
                return false;
            }

            bool result = string.Equals( _sourceNpcId.Trim(), _targetNpcId.Trim(), StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 참조 컴포넌트 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetPlayerController == null )
            {
                PlayerController resolvedPlayerController = GetComponent<PlayerController>();
                targetPlayerController = resolvedPlayerController;
            }

            if ( targetPlayerStatManager == null )
            {
                CPlayerStatManager resolvedPlayerStatManager = GetComponent<CPlayerStatManager>();
                targetPlayerStatManager = resolvedPlayerStatManager;
            }

            if ( targetPlayerInventoryManager == null )
            {
                CPlayerInventoryManager resolvedPlayerInventoryManager = GetComponent<CPlayerInventoryManager>();

                if ( resolvedPlayerInventoryManager == null )
                {
                    resolvedPlayerInventoryManager = gameObject.AddComponent<CPlayerInventoryManager>();
                }

                targetPlayerInventoryManager = resolvedPlayerInventoryManager;
            }

            if ( targetQuestStateProvider == null )
            {
                CQuestStateProvider resolvedQuestStateProvider = GetComponent<CQuestStateProvider>();
                targetQuestStateProvider = resolvedQuestStateProvider;
            }
        }

        ///<summary>
        /// 이벤트 구독 처리
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetPlayerStatManager != null )
            {
                targetPlayerStatManager.OnLevelExpChanged -= HandleLevelExpChanged;
                targetPlayerStatManager.OnLevelExpChanged += HandleLevelExpChanged;
            }

            if ( targetPlayerInventoryManager != null )
            {
                targetPlayerInventoryManager.OnInventoryChanged -= HandleInventoryChanged;
                targetPlayerInventoryManager.OnInventoryChanged += HandleInventoryChanged;
            }
        }

        ///<summary>
        /// 이벤트 구독 해제 처리
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetPlayerStatManager != null )
            {
                targetPlayerStatManager.OnLevelExpChanged -= HandleLevelExpChanged;
            }

            if ( targetPlayerInventoryManager != null )
            {
                targetPlayerInventoryManager.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        ///<summary>
        /// 레벨 변경 이벤트 처리
        ///</summary>
        private void HandleLevelExpChanged( int _currentLevel, float _currentExp, float _maxExp )
        {
            RefreshLevelConditionProgress();
        }

        ///<summary>
        /// 인벤토리 변경 이벤트 처리
        ///</summary>
        private void HandleInventoryChanged( CPlayerInventoryManager _inventoryManager )
        {
            RefreshTurnInConditionProgress();
        }

        ///<summary>
        /// 퀘스트 갱신 이벤트 발행
        ///</summary>
        private void NotifyQuestUpdated( string _questId )
        {
            if ( targetQuestStateProvider != null )
            {
                targetQuestStateProvider.NotifyQuestStateChanged();
            }

            if ( OnQuestUpdated != null )
            {
                OnQuestUpdated( _questId );
            }
        }

        ///<summary>
        /// 대화 프리셋 유효성 반환
        ///</summary>
        private bool HasDialoguePresetContent( CNPCDialoguePreset _dialoguePreset )
        {
            if ( _dialoguePreset == null )
            {
                return false;
            }

            List<string> dialogueLineList = _dialoguePreset.GetDialogueLineList();

            if ( dialogueLineList == null || dialogueLineList.Count == 0 )
            {
                return false;
            }

            for ( int index = 0; index < dialogueLineList.Count; index++ )
            {
                string dialogueLine = dialogueLineList[ index ];

                if ( string.IsNullOrWhiteSpace( dialogueLine ) )
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
