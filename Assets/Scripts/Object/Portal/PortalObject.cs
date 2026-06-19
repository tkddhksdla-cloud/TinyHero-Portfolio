using TinyHero.Core;
using TinyHero.Player;
using TinyHero.UI;
using UnityEngine;

public class PortalObject : MonoBehaviour
{
    [SerializeField] private string portalId;
    [SerializeField] private string targetSceneID;
    [SerializeField] private string targetPortalId;
    [SerializeField] private Collider2D targetTriggerCollider;

    private PlayerController currentPlayerController;

    ///<summary>
    /// 컴포넌트 초기화
    ///</summary>
    private void Awake()
    {
        if ( targetTriggerCollider == null )
        {
            Collider2D resolvedCollider = GetComponent<Collider2D>();

            if ( resolvedCollider == null )
            {
                resolvedCollider = GetComponentInChildren<Collider2D>( true );
            }

            targetTriggerCollider = resolvedCollider;
        }

        if ( targetTriggerCollider != null )
        {
            targetTriggerCollider.isTrigger = true;
        }
    }

    ///<summary>
    /// 프레임 상태 처리
    ///</summary>
    private void Update()
    {
        if ( currentPlayerController == null )
        {
            return;
        }

        if ( string.IsNullOrWhiteSpace( targetSceneID ) )
        {
            return;
        }

        CInputManager inputManager = CInputManager.Instance;

        if ( inputManager == null )
        {
            return;
        }

        if ( inputManager.GetPortalDown() == false )
        {
            return;
        }

        if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager ) && interactionManager != null && interactionManager.IsInteractionInProgress() )
        {
            return;
        }

        TinyHero.Maps.CMapManager mapManager = TinyHero.Maps.CMapManager.Instance;

        if ( mapManager == null )
        {
            return;
        }

        mapManager.TransitionToMap( targetSceneID, targetPortalId );
    }

    ///<summary>
    /// 트리거 진입 처리
    ///</summary>
    private void OnTriggerEnter2D( Collider2D _other )
    {
        PlayerController detectedPlayerController = ResolvePlayerController( _other );

        if ( detectedPlayerController == null )
        {
            return;
        }

        currentPlayerController = detectedPlayerController;
    }

    ///<summary>
    /// 트리거 이탈 처리
    ///</summary>
    private void OnTriggerExit2D( Collider2D _other )
    {
        PlayerController detectedPlayerController = ResolvePlayerController( _other );

        if ( detectedPlayerController == null )
        {
            return;
        }

        if ( currentPlayerController != detectedPlayerController )
        {
            return;
        }

        currentPlayerController = null;
    }

    ///<summary>
    /// 플레이어 제어 결정
    ///</summary>
    private PlayerController ResolvePlayerController( Collider2D _other )
    {
        if ( _other == null )
        {
            return null;
        }

        PlayerController detectedPlayerController = _other.GetComponent<PlayerController>();

        if ( detectedPlayerController != null )
        {
            if ( detectedPlayerController.enabled == false || detectedPlayerController.gameObject.activeInHierarchy == false )
            {
                return null;
            }

            return detectedPlayerController;
        }

        PlayerController detectedParentPlayerController = _other.GetComponentInParent<PlayerController>();

        if ( detectedParentPlayerController == null )
        {
            return null;
        }

        if ( detectedParentPlayerController.enabled == false || detectedParentPlayerController.gameObject.activeInHierarchy == false )
        {
            return null;
        }

        return detectedParentPlayerController;
    }

    ///<summary>
    /// 포탈 설정
    ///</summary>
    public void ConfigurePortal( string _assignedPortalId, string _assignedTargetSceneID, string _assignedTargetPortalId )
    {
        portalId = _assignedPortalId;
        targetSceneID = _assignedTargetSceneID;
        targetPortalId = _assignedTargetPortalId;
    }

    ///<summary>
    /// 포탈 ID 반환
    ///</summary>
    public string GetPortalId()
    {
        string result = portalId;
        return result;
    }

    ///<summary>
    /// 대상 씬 ID 설정
    ///</summary>
    public void SetTargetSceneID( string _assignedTargetSceneID )
    {
        targetSceneID = _assignedTargetSceneID;
    }

    ///<summary>
    /// 대상 씬 ID 반환
    ///</summary>
    public string GetTargetSceneID()
    {
        string result = targetSceneID;
        return result;
    }

    ///<summary>
    /// 대상 포탈 ID 설정
    ///</summary>
    public void SetTargetPortalId( string _assignedTargetPortalId )
    {
        targetPortalId = _assignedTargetPortalId;
    }

    ///<summary>
    /// 대상 포탈 ID 반환
    ///</summary>
    public string GetTargetPortalId()
    {
        string result = targetPortalId;
        return result;
    }
}
