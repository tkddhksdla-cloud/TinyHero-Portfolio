using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

///<summary>
/// 월드 아이템 드랍 오브젝트
///</summary>
[RequireComponent( typeof( Collider2D ) )]
public sealed class CWorldItemDropObject : MonoBehaviour
{
    [SerializeField] private string itemId = string.Empty;
    [SerializeField] private int itemCount = 1;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private Collider2D pickupTriggerCollider;

    private CItemDefinition cachedItemDefinition;

    ///<summary>
    /// 드랍 오브젝트 초기화
    ///</summary>
    private void Awake()
    {
        ResolveReferences();
        ConfigureCollider();
        RefreshVisual();
    }

    ///<summary>
    /// 드랍 오브젝트 데이터 구성
    ///</summary>
    public void ConfigureDrop( CItemDefinition _itemDefinition, int _itemCount )
    {
        cachedItemDefinition = _itemDefinition;
        itemId = _itemDefinition != null ? _itemDefinition.GetItemId() : string.Empty;
        itemCount = Mathf.Max( 1, _itemCount );
        RefreshVisual();
    }

    ///<summary>
    /// 충돌 기반 아이템 획득 처리
    ///</summary>
    private void OnTriggerEnter2D( Collider2D _other )
    {
        if ( _other == null )
        {
            return;
        }

        PlayerController playerController = _other.GetComponent<PlayerController>();

        if ( playerController == null )
        {
            playerController = _other.GetComponentInParent<PlayerController>();
        }

        if ( playerController == null )
        {
            return;
        }

        CPlayerInventoryManager inventoryManager = playerController.GetInventoryManager();

        if ( inventoryManager == null )
        {
            return;
        }

        CItemDefinition itemDefinition = ResolveItemDefinition();

        if ( itemDefinition == null )
        {
            return;
        }

        bool wasAdded = inventoryManager.TryAddItem( itemDefinition, itemCount );

        if ( wasAdded == false )
        {
            return;
        }

        Destroy( gameObject );
    }

    ///<summary>
    /// 드랍 아이템 정의 결정
    ///</summary>
    private CItemDefinition ResolveItemDefinition()
    {
        if ( cachedItemDefinition != null )
        {
            return cachedItemDefinition;
        }

        bool hasDefinition = CItemDefinitionDatabase.TryGetItemDefinition( itemId, out CItemDefinition itemDefinition );

        if ( hasDefinition == false )
        {
            return null;
        }

        cachedItemDefinition = itemDefinition;
        return cachedItemDefinition;
    }

    ///<summary>
    /// 드랍 오브젝트 시각 요소 갱신
    ///</summary>
    private void RefreshVisual()
    {
        ResolveReferences();
        CItemDefinition itemDefinition = ResolveItemDefinition();

        if ( targetSpriteRenderer == null )
        {
            return;
        }

        if ( itemDefinition == null )
        {
            targetSpriteRenderer.sprite = null;
            return;
        }

        targetSpriteRenderer.sprite = itemDefinition.GetIconSprite();
    }

    ///<summary>
    /// 드랍 오브젝트 참조 결정
    ///</summary>
    private void ResolveReferences()
    {
        if ( targetSpriteRenderer == null )
        {
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>( true );
        }

        if ( pickupTriggerCollider == null )
        {
            pickupTriggerCollider = GetComponent<Collider2D>();
        }
    }

    ///<summary>
    /// 드랍 충돌체 설정
    ///</summary>
    private void ConfigureCollider()
    {
        if ( pickupTriggerCollider == null )
        {
            return;
        }

        pickupTriggerCollider.isTrigger = true;
    }
}
