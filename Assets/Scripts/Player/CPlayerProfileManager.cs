using System;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 프로필 런타임 상태 관리
    ///</summary>
    public sealed class CPlayerProfileManager : CSingleTon<CPlayerProfileManager>
    {
        private const string DefaultPlayerName = "Hero";
        private const int MaxPlayerNameLength = 12;

        [SerializeField] private string playerName = DefaultPlayerName;

        public event Action<string> OnPlayerNameChanged;

        ///<summary>
        /// 플레이어 이름 반환
        ///</summary>
        public string GetPlayerName()
        {
            string result = ResolveValidPlayerName( playerName );
            return result;
        }

        ///<summary>
        /// 플레이어 이름 설정
        ///</summary>
        public void SetPlayerName( string _playerName )
        {
            string resolvedPlayerName = ResolveValidPlayerName( _playerName );

            if ( string.Equals( playerName, resolvedPlayerName, StringComparison.Ordinal ) )
            {
                return;
            }

            playerName = resolvedPlayerName;
            OnPlayerNameChanged?.Invoke( playerName );
        }

        ///<summary>
        /// 플레이어 프로필 스냅샷 생성
        ///</summary>
        public CPlayerProfileSnapshotData CreateSnapshotData()
        {
            CPlayerProfileSnapshotData snapshotData = new CPlayerProfileSnapshotData();
            snapshotData.playerName = GetPlayerName();
            return snapshotData;
        }

        ///<summary>
        /// 플레이어 프로필 스냅샷 적용
        ///</summary>
        public void LoadSnapshotData( CPlayerProfileSnapshotData _snapshotData )
        {
            if ( _snapshotData == null )
            {
                SetPlayerName( DefaultPlayerName );
                return;
            }

            SetPlayerName( _snapshotData.playerName );
        }

        ///<summary>
        /// 플레이어 이름 유효값 반환
        ///</summary>
        private string ResolveValidPlayerName( string _playerName )
        {
            string normalizedPlayerName = string.IsNullOrWhiteSpace( _playerName ) ? DefaultPlayerName : _playerName.Trim();

            if ( normalizedPlayerName.Length > MaxPlayerNameLength )
            {
                normalizedPlayerName = normalizedPlayerName.Substring( 0, MaxPlayerNameLength );
            }

            string result = normalizedPlayerName;
            return result;
        }
    }
}
