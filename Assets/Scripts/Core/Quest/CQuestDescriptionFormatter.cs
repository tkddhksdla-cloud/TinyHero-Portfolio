using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Quest
{
    ///<summary>
    /// 퀘스트 설명 심볼 치환 도구
    ///</summary>
    public static class CQuestDescriptionFormatter
    {
        private const string GiverTokenName = "GIVER";
        private const string CompleterTokenName = "COMPLETER";
        private const string NpcTokenName = "NPC";
        private const string PlayerTokenName = "PLAYER";
        private const string NpcPrefabResourceFolderPath = "Prefabs/Character/NPC";
        private const string ResolvedTokenColorHex = "#FFA500";
        private const string ResolvedPlayerTokenColorHex = "#4AA3FF";

        private static readonly Regex TokenRegex = new Regex( "\\{([A-Za-z_]+)\\}", RegexOptions.Compiled );
        private static readonly string[] SupportedTokenArray =
        {
            "{GIVER}",
            "{COMPLETER}",
            "{NPC}",
            "{PLAYER}"
        };

        private static readonly string[] QuestTokenArray =
        {
            "{GIVER}",
            "{COMPLETER}",
            "{PLAYER}"
        };

        private static readonly Dictionary<string, string> npcNameById = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
        private static bool isNpcNameCacheBuilt;

        ///<summary>
        /// 지원 심볼 목록 반환
        ///</summary>
        public static IReadOnlyList<string> GetSupportedTokenList()
        {
            IReadOnlyList<string> result = SupportedTokenArray;
            return result;
        }

        ///<summary>
        /// 퀘스트 정의용 지원 심볼 목록 반환
        ///</summary>
        public static IReadOnlyList<string> GetQuestTokenList()
        {
            IReadOnlyList<string> result = QuestTokenArray;
            return result;
        }

        ///<summary>
        /// 퀘스트 설명 심볼 치환
        ///</summary>
        public static string Format( CQuestDefinition _questDefinition, string _template )
        {
            string result = Format( _questDefinition, _template, string.Empty );
            return result;
        }

        ///<summary>
        /// 퀘스트 설명 심볼 치환
        ///</summary>
        public static string Format( CQuestDefinition _questDefinition, string _template, string _npcName )
        {
            if ( string.IsNullOrWhiteSpace( _template ) )
            {
                return string.Empty;
            }

            string result = TokenRegex.Replace( _template, _match =>
            {
                string tokenName = _match.Groups[ 1 ].Value;
                bool isResolved = TryResolveTokenValue( _questDefinition, tokenName, _npcName, out string resolvedValue );

                if ( isResolved == false )
                {
                    return _match.Value;
                }

                string richTextValue = ApplyResolvedTokenColor( tokenName, resolvedValue );
                return richTextValue;
            } );

            return result;
        }

        ///<summary>
        /// 퀘스트 심볼 값 조회 시도
        ///</summary>
        public static bool TryResolveTokenValue( CQuestDefinition _questDefinition, string _tokenName, out string _resolvedValue )
        {
            bool result = TryResolveTokenValue( _questDefinition, _tokenName, string.Empty, out _resolvedValue );
            return result;
        }

        ///<summary>
        /// 퀘스트 심볼 값 조회 시도
        ///</summary>
        public static bool TryResolveTokenValue( CQuestDefinition _questDefinition, string _tokenName, string _npcName, out string _resolvedValue )
        {
            _resolvedValue = string.Empty;

            if ( string.IsNullOrWhiteSpace( _tokenName ) )
            {
                return false;
            }

            string normalizedTokenName = _tokenName.Trim().ToUpperInvariant();
            string npcId = string.Empty;

            switch ( normalizedTokenName )
            {
                case NpcTokenName:
                {
                    string normalizedNpcName = string.IsNullOrWhiteSpace( _npcName ) ? string.Empty : CDataManager.GetText( _npcName.Trim() );

                    if ( string.IsNullOrWhiteSpace( normalizedNpcName ) )
                    {
                        return false;
                    }

                    _resolvedValue = normalizedNpcName;
                    return true;
                }

                case PlayerTokenName:
                {
                    CPlayerProfileManager playerProfileManager = CPlayerProfileManager.Instance;

                    if ( playerProfileManager == null )
                    {
                        return false;
                    }

                    string playerName = playerProfileManager.GetPlayerName();

                    if ( string.IsNullOrWhiteSpace( playerName ) )
                    {
                        return false;
                    }

                    _resolvedValue = playerName;
                    return true;
                }

                case GiverTokenName:
                    if ( _questDefinition == null )
                    {
                        return false;
                    }

                    npcId = _questDefinition.GetGiverNpcId();
                    break;

                case CompleterTokenName:
                    if ( _questDefinition == null )
                    {
                        return false;
                    }

                    npcId = _questDefinition.GetCompleterNpcId();
                    break;

                default:
                    return false;
            }

            _resolvedValue = ResolveNpcName( npcId );
            return true;
        }

        ///<summary>
        /// NPC ID 기반 표시 이름 반환
        ///</summary>
        public static string ResolveNpcName( string _npcId )
        {
            string normalizedNpcId = string.IsNullOrWhiteSpace( _npcId ) ? string.Empty : _npcId.Trim();

            if ( string.IsNullOrWhiteSpace( normalizedNpcId ) )
            {
                return string.Empty;
            }

            EnsureNpcNameCache();
            bool hasNpcName = npcNameById.TryGetValue( normalizedNpcId, out string npcName );

            if ( hasNpcName && string.IsNullOrWhiteSpace( npcName ) == false )
            {
                return npcName;
            }

            string result = normalizedNpcId;
            return result;
        }

        ///<summary>
        /// 치환된 심볼 표시 색상 적용
        ///</summary>
        private static string ApplyResolvedTokenColor( string _tokenName, string _resolvedValue )
        {
            if ( string.IsNullOrWhiteSpace( _resolvedValue ) )
            {
                return string.Empty;
            }

            string normalizedTokenName = string.IsNullOrWhiteSpace( _tokenName ) ? string.Empty : _tokenName.Trim().ToUpperInvariant();
            string colorHex = string.Equals( normalizedTokenName, PlayerTokenName, StringComparison.Ordinal ) ? ResolvedPlayerTokenColorHex : ResolvedTokenColorHex;
            string result = $"<color={colorHex}>{_resolvedValue}</color>";
            return result;
        }

        ///<summary>
        /// NPC 이름 캐시 강제 갱신
        ///</summary>
        public static void ReloadNpcNameCache()
        {
            isNpcNameCacheBuilt = false;
            npcNameById.Clear();
            EnsureNpcNameCache();
        }

        ///<summary>
        /// NPC 이름 캐시 구성 보장
        ///</summary>
        private static void EnsureNpcNameCache()
        {
            if ( isNpcNameCacheBuilt )
            {
                return;
            }

            isNpcNameCacheBuilt = true;
            npcNameById.Clear();
            RegisterSceneNpcNames();
            RegisterResourceNpcPrefabNames();
        }

        ///<summary>
        /// 현재 씬 NPC 이름 등록
        ///</summary>
        private static void RegisterSceneNpcNames()
        {
            CNPCObject[] npcObjectArray = UnityEngine.Object.FindObjectsByType<CNPCObject>( FindObjectsInactive.Include, FindObjectsSortMode.None );

            if ( npcObjectArray == null )
            {
                return;
            }

            for ( int index = 0; index < npcObjectArray.Length; index++ )
            {
                CNPCObject npcObject = npcObjectArray[ index ];
                RegisterNpcObjectName( npcObject );
            }
        }

        ///<summary>
        /// Resources NPC 프리팹 이름 등록
        ///</summary>
        private static void RegisterResourceNpcPrefabNames()
        {
            GameObject[] npcPrefabArray = Resources.LoadAll<GameObject>( NpcPrefabResourceFolderPath );

            if ( npcPrefabArray == null )
            {
                return;
            }

            for ( int index = 0; index < npcPrefabArray.Length; index++ )
            {
                GameObject npcPrefab = npcPrefabArray[ index ];

                if ( npcPrefab == null )
                {
                    continue;
                }

                CNPCObject npcObject = npcPrefab.GetComponentInChildren<CNPCObject>( true );
                RegisterNpcObjectName( npcObject );
            }
        }

        ///<summary>
        /// NPC 오브젝트 이름 등록
        ///</summary>
        private static void RegisterNpcObjectName( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return;
            }

            string npcId = _npcObject.GetNpcId();

            if ( string.IsNullOrWhiteSpace( npcId ) )
            {
                return;
            }

            string npcName = ResolveNpcObjectDisplayName( _npcObject );
            npcNameById[ npcId.Trim() ] = npcName;
        }

        ///<summary>
        /// NPC 오브젝트 표시 이름 반환
        ///</summary>
        private static string ResolveNpcObjectDisplayName( CNPCObject _npcObject )
        {
            CNPCInteractionData interactionData = _npcObject.GetInteractionData();

            if ( interactionData != null && string.IsNullOrWhiteSpace( interactionData.GetNpcName() ) == false )
            {
                string interactionNpcName = interactionData.GetNpcName();
                return interactionNpcName;
            }

            string npcName = _npcObject.GetNpcName();
            string result = CDataManager.GetText( npcName );
            return result;
        }
    }
}
