using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Quest
{
    ///<summary>
    /// 퀘스트 정의 조회 데이터베이스
    ///</summary>
    public static class CQuestDefinitionDatabase
    {
        private const string QuestDefinitionResourcePath = "Data/Quest/Definitions";

        private static readonly Dictionary<string, CQuestDefinition> questDefinitionDictionary = new Dictionary<string, CQuestDefinition>();
        private static readonly List<CQuestDefinition> questDefinitionList = new List<CQuestDefinition>();
        private static bool isInitialized;

        ///<summary>
        /// 퀘스트 정의 목록 반환
        ///</summary>
        public static IReadOnlyList<CQuestDefinition> GetQuestDefinitionList()
        {
            EnsureInitialized();
            IReadOnlyList<CQuestDefinition> result = questDefinitionList;
            return result;
        }

        ///<summary>
        /// 퀘스트 정의 조회 시도
        ///</summary>
        public static bool TryGetQuestDefinition( string _questId, out CQuestDefinition _questDefinition )
        {
            EnsureInitialized();

            if ( string.IsNullOrWhiteSpace( _questId ) )
            {
                _questDefinition = null;
                return false;
            }

            string normalizedQuestId = _questId.Trim();
            bool isFound = questDefinitionDictionary.TryGetValue( normalizedQuestId, out CQuestDefinition resolvedQuestDefinition );
            _questDefinition = resolvedQuestDefinition;
            return isFound;
        }

        ///<summary>
        /// 퀘스트 데이터베이스 강제 갱신
        ///</summary>
        public static void Reload()
        {
            isInitialized = false;
            questDefinitionDictionary.Clear();
            questDefinitionList.Clear();
            EnsureInitialized();
        }

        ///<summary>
        /// 퀘스트 데이터베이스 초기화 보장
        ///</summary>
        private static void EnsureInitialized()
        {
            if ( isInitialized )
            {
                return;
            }

            isInitialized = true;
            questDefinitionDictionary.Clear();
            questDefinitionList.Clear();
            CQuestDefinition[] loadedQuestDefinitionArray = Resources.LoadAll<CQuestDefinition>( QuestDefinitionResourcePath );

            for ( int index = 0; index < loadedQuestDefinitionArray.Length; index++ )
            {
                CQuestDefinition questDefinition = loadedQuestDefinitionArray[ index ];

                if ( questDefinition == null )
                {
                    continue;
                }

                string questId = questDefinition.GetQuestId();

                if ( string.IsNullOrWhiteSpace( questId ) )
                {
                    continue;
                }

                if ( questDefinitionDictionary.ContainsKey( questId ) )
                {
                    continue;
                }

                questDefinitionDictionary.Add( questId, questDefinition );
                questDefinitionList.Add( questDefinition );
            }
        }
    }
}
