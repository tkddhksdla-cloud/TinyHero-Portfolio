using TinyHero.Player;

namespace TinyHero.Core
{
    /// <summary>
    /// 씬 전환을 고려해 현재 활성 플레이어를 안전하게 조회합니다.
    /// </summary>
    public static class CActivePlayerResolver
    {
        public static bool TryGetActivePlayerController( out PlayerController _playerController )
        {
            _playerController = null;
            bool hasGameManager = CGameManager.TryGetExistingInstance( out CGameManager gameManager );

            if ( hasGameManager == false || gameManager == null )
            {
                return false;
            }

            bool hasPlayerController = gameManager.TryGetActivePlayerController( out PlayerController playerController );

            if ( hasPlayerController == false || playerController == null || playerController.gameObject.activeInHierarchy == false )
            {
                return false;
            }

            _playerController = playerController;
            return true;
        }
    }
}
