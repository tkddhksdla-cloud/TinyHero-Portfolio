using System;
using System.Collections.Generic;
using TinyHero.Quest;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 퀘스트 상태 스냅샷 보관 컴포넌트
    ///</summary>
    public sealed class CQuestStateProvider : MonoBehaviour
    {
        [SerializeField] private List<string> completedQuestIdList = new List<string>();
        [SerializeField] private List<CQuestRuntimeEntryData> runtimeEntryList = new List<CQuestRuntimeEntryData>();

        public event Action OnQuestStateChanged;

        ///<summary>
        /// 완료 퀘스트 여부 반환
        ///</summary>
        public bool IsQuestCompleted( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return false;
            }

            string normalizedQuestId = _questId.Trim();
            bool isCompleted = completedQuestIdList.Contains( normalizedQuestId );
            return isCompleted;
        }

        ///<summary>
        /// 완료 퀘스트 ID 목록 반환
        ///</summary>
        public IReadOnlyList<string> GetCompletedQuestIdList()
        {
            IReadOnlyList<string> result = completedQuestIdList;
            return result;
        }

        ///<summary>
        /// 퀘스트 런타임 엔트리 목록 반환
        ///</summary>
        public List<CQuestRuntimeEntryData> GetRuntimeEntryList()
        {
            List<CQuestRuntimeEntryData> result = runtimeEntryList;
            return result;
        }

        ///<summary>
        /// 퀘스트 런타임 엔트리 조회 시도
        ///</summary>
        public bool TryGetRuntimeEntryData( string _questId, out CQuestRuntimeEntryData _runtimeEntryData )
        {
            _runtimeEntryData = null;

            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return false;
            }

            string normalizedQuestId = _questId.Trim();

            for ( int index = 0; index < runtimeEntryList.Count; index++ )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ index ];

                if ( runtimeEntryData == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( runtimeEntryData.GetQuestId(), normalizedQuestId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                _runtimeEntryData = runtimeEntryData;
                return true;
            }

            return false;
        }

        ///<summary>
        /// 퀘스트 런타임 엔트리 생성 보장
        ///</summary>
        public CQuestRuntimeEntryData GetOrCreateRuntimeEntryData( string _questId )
        {
            bool hasRuntimeEntry = TryGetRuntimeEntryData( _questId, out CQuestRuntimeEntryData runtimeEntryData );

            if ( hasRuntimeEntry )
            {
                return runtimeEntryData;
            }

            CQuestRuntimeEntryData createdRuntimeEntryData = new CQuestRuntimeEntryData();
            createdRuntimeEntryData.SetQuestId( _questId );
            runtimeEntryList.Add( createdRuntimeEntryData );
            return createdRuntimeEntryData;
        }

        ///<summary>
        /// 퀘스트 완료 이력 등록
        ///</summary>
        public void MarkQuestCompleted( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return;
            }

            string normalizedQuestId = _questId.Trim();

            if ( completedQuestIdList.Contains( normalizedQuestId ) )
            {
                return;
            }

            completedQuestIdList.Add( normalizedQuestId );
            NotifyQuestStateChanged();
        }

        ///<summary>
        /// 퀘스트 완료 이력 해제
        ///</summary>
        public void UnmarkQuestCompleted( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return;
            }

            string normalizedQuestId = _questId.Trim();
            bool wasRemoved = completedQuestIdList.Remove( normalizedQuestId );

            if ( wasRemoved == false )
            {
                return;
            }

            NotifyQuestStateChanged();
        }

        ///<summary>
        /// 퀘스트 상태 전체 초기화
        ///</summary>
        public void ResetQuest( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return;
            }

            string normalizedQuestId = _questId.Trim();
            completedQuestIdList.Remove( normalizedQuestId );

            for ( int index = runtimeEntryList.Count - 1; index >= 0; index-- )
            {
                CQuestRuntimeEntryData runtimeEntryData = runtimeEntryList[ index ];

                if ( runtimeEntryData == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( runtimeEntryData.GetQuestId(), normalizedQuestId, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                runtimeEntryList.RemoveAt( index );
            }

            NotifyQuestStateChanged();
        }

        ///<summary>
        /// 퀘스트 상태 변경 이벤트 발행
        ///</summary>
        public void NotifyQuestStateChanged()
        {
            if ( OnQuestStateChanged != null )
            {
                OnQuestStateChanged();
            }
        }

        ///<summary>
        /// 퀘스트 스냅샷 생성
        ///</summary>
        public CQuestRuntimeSnapshotData CreateSnapshotData()
        {
            CQuestRuntimeSnapshotData snapshotData = new CQuestRuntimeSnapshotData();
            List<string> completedQuestIdCopyList = new List<string>( completedQuestIdList );
            List<CQuestRuntimeEntryData> runtimeEntryCopyList = new List<CQuestRuntimeEntryData>();

            for ( int index = 0; index < runtimeEntryList.Count; index++ )
            {
                CQuestRuntimeEntryData sourceRuntimeEntryData = runtimeEntryList[ index ];

                if ( sourceRuntimeEntryData == null )
                {
                    continue;
                }

                CQuestRuntimeEntryData copiedRuntimeEntryData = CreateRuntimeEntryCopy( sourceRuntimeEntryData );
                runtimeEntryCopyList.Add( copiedRuntimeEntryData );
            }

            snapshotData.SetCompletedQuestIdList( completedQuestIdCopyList );
            snapshotData.SetRuntimeEntryList( runtimeEntryCopyList );
            return snapshotData;
        }

        ///<summary>
        /// 퀘스트 스냅샷 로드
        ///</summary>
        public void LoadSnapshotData( CQuestRuntimeSnapshotData _snapshotData )
        {
            completedQuestIdList.Clear();
            runtimeEntryList.Clear();

            if ( _snapshotData == null )
            {
                NotifyQuestStateChanged();
                return;
            }

            List<string> loadedCompletedQuestIdList = _snapshotData.GetCompletedQuestIdList();

            if ( loadedCompletedQuestIdList != null )
            {
                for ( int index = 0; index < loadedCompletedQuestIdList.Count; index++ )
                {
                    string questId = loadedCompletedQuestIdList[ index ];

                    if ( string.IsNullOrWhiteSpace( questId ) )
                    {
                        continue;
                    }

                    completedQuestIdList.Add( questId.Trim() );
                }
            }

            List<CQuestRuntimeEntryData> loadedRuntimeEntryList = _snapshotData.GetRuntimeEntryList();

            if ( loadedRuntimeEntryList != null )
            {
                for ( int index = 0; index < loadedRuntimeEntryList.Count; index++ )
                {
                    CQuestRuntimeEntryData sourceRuntimeEntryData = loadedRuntimeEntryList[ index ];

                    if ( sourceRuntimeEntryData == null )
                    {
                        continue;
                    }

                    CQuestRuntimeEntryData copiedRuntimeEntryData = CreateRuntimeEntryCopy( sourceRuntimeEntryData );
                    runtimeEntryList.Add( copiedRuntimeEntryData );
                }
            }

            NotifyQuestStateChanged();
        }

        ///<summary>
        /// 런타임 엔트리 복사본 생성
        ///</summary>
        private CQuestRuntimeEntryData CreateRuntimeEntryCopy( CQuestRuntimeEntryData _sourceRuntimeEntryData )
        {
            CQuestRuntimeEntryData copiedRuntimeEntryData = new CQuestRuntimeEntryData();
            copiedRuntimeEntryData.SetQuestId( _sourceRuntimeEntryData.GetQuestId() );
            copiedRuntimeEntryData.SetQuestStatus( _sourceRuntimeEntryData.GetQuestStatus() );
            copiedRuntimeEntryData.SetAcceptedNpcId( _sourceRuntimeEntryData.GetAcceptedNpcId() );

            int sourceAcceptCount = _sourceRuntimeEntryData.GetAcceptCount();
            int sourceCompleteCount = _sourceRuntimeEntryData.GetCompleteCount();

            for ( int index = 0; index < sourceAcceptCount; index++ )
            {
                copiedRuntimeEntryData.IncreaseAcceptCount();
            }

            for ( int index = 0; index < sourceCompleteCount; index++ )
            {
                copiedRuntimeEntryData.IncreaseCompleteCount();
            }

            List<CQuestConditionProgressData> sourceConditionProgressList = _sourceRuntimeEntryData.GetConditionProgressList();
            List<CQuestConditionProgressData> copiedConditionProgressList = copiedRuntimeEntryData.GetConditionProgressList();
            copiedConditionProgressList.Clear();

            for ( int index = 0; index < sourceConditionProgressList.Count; index++ )
            {
                CQuestConditionProgressData sourceConditionProgressData = sourceConditionProgressList[ index ];

                if ( sourceConditionProgressData == null )
                {
                    continue;
                }

                CQuestConditionProgressData copiedConditionProgressData = new CQuestConditionProgressData();
                copiedConditionProgressData.SetConditionId( sourceConditionProgressData.GetConditionId() );
                copiedConditionProgressData.SetCurrentValue( sourceConditionProgressData.GetCurrentValue() );
                copiedConditionProgressData.SetIsCompleted( sourceConditionProgressData.GetIsCompleted() );
                copiedConditionProgressList.Add( copiedConditionProgressData );
            }

            return copiedRuntimeEntryData;
        }
    }
}
