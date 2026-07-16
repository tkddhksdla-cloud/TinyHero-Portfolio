using TinyHero.Player;

namespace TinyHero.Core
{
    /// <summary>
    /// 씬 전환 중에도 활성 플레이어와 연결된 런타임 컨텍스트를 안전하게 조회합니다.
    /// </summary>
    public static class CActivePlayerRuntimeContextResolver
    {
        public static bool TryGetActivePlayerRuntimeContext( out CPlayerRuntimeContext _playerRuntimeContext )
        {
            _playerRuntimeContext = null;
            bool hasActivePlayerController = CActivePlayerResolver.TryGetActivePlayerController( out PlayerController activePlayerController );

            if ( hasActivePlayerController == false )
            {
                return false;
            }

            bool hasGameManager = CGameManager.TryGetExistingInstance( out CGameManager gameManager );

            if ( hasGameManager == false || gameManager == null )
            {
                return false;
            }

            bool hasRuntimeContext = gameManager.TryGetPlayerRuntimeContext( out CPlayerRuntimeContext playerRuntimeContext );

            if ( hasRuntimeContext == false || playerRuntimeContext == null || playerRuntimeContext.gameObject.activeInHierarchy == false )
            {
                return false;
            }

            PlayerController contextPlayerController = playerRuntimeContext.GetPlayerController();

            if ( contextPlayerController != activePlayerController )
            {
                return false;
            }

            _playerRuntimeContext = playerRuntimeContext;
            return true;
        }
    }
}
