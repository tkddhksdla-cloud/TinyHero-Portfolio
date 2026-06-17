using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 해금용 퀘스트 완료 상태 제공 컴포넌트
    ///</summary>
    public sealed class CQuestStateProvider : MonoBehaviour
    {
        [SerializeField] private List<string> completedQuestIdList = new List<string>();

        public event Action OnQuestStateChanged;

        ///<summary>
        /// 퀘스트 완료 여부 반환
        ///</summary>
        public bool IsQuestCompleted( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return false;
            }

            bool isCompleted = completedQuestIdList.Contains( _questId );
            return isCompleted;
        }

        ///<summary>
        /// 퀘스트 완료 상태 등록
        ///</summary>
        public void CompleteQuest( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return;
            }

            if ( completedQuestIdList.Contains( _questId ) )
            {
                return;
            }

            completedQuestIdList.Add( _questId );

            if ( OnQuestStateChanged != null )
            {
                OnQuestStateChanged();
            }
        }

        ///<summary>
        /// 퀘스트 완료 상태 해제
        ///</summary>
        public void ResetQuest( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                return;
            }

            bool wasRemoved = completedQuestIdList.Remove( _questId );

            if ( wasRemoved == false )
            {
                return;
            }

            if ( OnQuestStateChanged != null )
            {
                OnQuestStateChanged();
            }
        }
    }
}
