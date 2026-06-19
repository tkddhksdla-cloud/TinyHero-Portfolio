using TinyHero.Player;
using TinyHero.UI;
using UnityEngine;

///<summary>
/// NPC 상호작용 범위 처리 컴포넌트
///</summary>
[DisallowMultipleComponent]
public sealed class CNPCInteractionRange : MonoBehaviour
{
    [SerializeField] private CNPCObject ownerNpcObject;
    [SerializeField] private BoxCollider2D targetTriggerCollider;

    private PlayerController currentPlayerController;

    ///<summary>
    /// 컴포넌트 초기화
    ///</summary>
    private void Awake()
    {
        if ( ownerNpcObject == null )
        {
            CNPCObject resolvedNpcObject = GetComponentInParent<CNPCObject>();
            ownerNpcObject = resolvedNpcObject;
        }

        if ( targetTriggerCollider == null )
        {
            BoxCollider2D resolvedTriggerCollider = GetComponent<BoxCollider2D>();
            targetTriggerCollider = resolvedTriggerCollider;
        }

        if ( targetTriggerCollider != null )
        {
            targetTriggerCollider.isTrigger = true;
        }
    }

    ///<summary>
    /// 비활성화 시 범위 해제 처리
    ///</summary>
    private void OnDisable()
    {
        currentPlayerController = null;

        if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager ) == false )
        {
            return;
        }

        interactionManager.UnregisterInteractionRange( this );
    }

    ///<summary>
    /// 범위 설정
    ///</summary>
    public void ConfigureRange( CNPCObject _ownerNpcObject, BoxCollider2D _targetTriggerCollider )
    {
        ownerNpcObject = _ownerNpcObject;
        targetTriggerCollider = _targetTriggerCollider;

        if ( targetTriggerCollider != null )
        {
            targetTriggerCollider.isTrigger = true;
        }
    }

    ///<summary>
    /// 소유 NPC 반환
    ///</summary>
    public CNPCObject GetOwnerNpcObject()
    {
        CNPCObject result = ownerNpcObject;
        return result;
    }

    ///<summary>
    /// 현재 플레이어 반환
    ///</summary>
    public PlayerController GetCurrentPlayerController()
    {
        PlayerController result = currentPlayerController;
        return result;
    }

    ///<summary>
    /// 플레이어 범위 진입 여부 반환
    ///</summary>
    public bool IsPlayerInRange()
    {
        bool result = currentPlayerController != null;
        return result;
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

        if ( ownerNpcObject != null )
        {
            ownerNpcObject.IgnoreCollisionWithPlayer( detectedPlayerController );
        }

        currentPlayerController = detectedPlayerController;
        CNPCInteractionManager interactionManager = CNPCInteractionManager.Instance;
        interactionManager.RegisterInteractionRange( this );
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

        if ( CNPCInteractionManager.TryGetInstance( out CNPCInteractionManager interactionManager ) == false )
        {
            return;
        }

        interactionManager.UnregisterInteractionRange( this );
    }

    ///<summary>
    /// 플레이어 제어 컴포넌트 결정
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
}
