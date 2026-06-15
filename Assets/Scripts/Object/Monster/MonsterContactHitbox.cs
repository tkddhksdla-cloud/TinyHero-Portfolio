using TinyHero.Player;
using UnityEngine;

///<summary>
/// 몬스터 접촉 피격 트리거
///</summary>
[RequireComponent( typeof( Collider2D ) )]
public sealed class MonsterContactHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D targetCollider;

    ///<summary>
    /// 트리거 콜라이더 초기화
    ///</summary>
    private void Awake()
    {
        if ( targetCollider == null )
        {
            Collider2D resolvedCollider = GetComponent<Collider2D>();
            targetCollider = resolvedCollider;
        }

        if ( targetCollider == null )
        {
            return;
        }

        targetCollider.isTrigger = true;
    }

    ///<summary>
    /// 진입 접촉 피격 전달
    ///</summary>
    private void OnTriggerEnter2D( Collider2D _other )
    {
        TryNotifyPlayerHit( _other );
    }

    ///<summary>
    /// 유지 접촉 피격 전달
    ///</summary>
    private void OnTriggerStay2D( Collider2D _other )
    {
        TryNotifyPlayerHit( _other );
    }

    ///<summary>
    /// 플레이어 피격 전달
    ///</summary>
    private void TryNotifyPlayerHit( Collider2D _other )
    {
        if ( _other == null )
        {
            return;
        }

        PlayerController playerController = ResolvePlayerController( _other );

        if ( playerController == null )
        {
            return;
        }

        MonsterObject monsterObject = GetComponentInParent<MonsterObject>();
        playerController.TryReceiveContactHit( monsterObject );
    }

    ///<summary>
    /// 닿은 콜라이더 기준 플레이어 제어 컴포넌트 결정
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
            return detectedPlayerController;
        }

        PlayerController detectedParentPlayerController = _other.GetComponentInParent<PlayerController>();
        return detectedParentPlayerController;
    }
}
