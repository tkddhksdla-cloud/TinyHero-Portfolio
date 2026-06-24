using TinyHero.Core.Data;
using TinyHero.Maps;
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
    private string mapRuntimePoolKey = string.Empty;
    private bool isPickupInProgress;

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
        PrepareForSpawn();
        cachedItemDefinition = _itemDefinition;
        itemId = _itemDefinition != null ? _itemDefinition.GetItemId() : string.Empty;
        itemCount = Mathf.Max( 1, _itemCount );
        RefreshVisual();
    }

    ///<summary>
    /// 드랍 오브젝트 스폰 준비
    ///</summary>
    public void PrepareForSpawn()
    {
        isPickupInProgress = false;
        ResolveReferences();
        ConfigureCollider();
    }

    ///<summary>
    /// 맵 런타임 풀 키 설정
    ///</summary>
    public void SetMapRuntimePoolKey( string _poolKey )
    {
        string resolvedPoolKey = string.IsNullOrWhiteSpace( _poolKey ) ? string.Empty : _poolKey.Trim();
        mapRuntimePoolKey = resolvedPoolKey;
    }

    ///<summary>
    /// 맵 런타임 풀 키 반환
    ///</summary>
    public string GetMapRuntimePoolKey()
    {
        string result = mapRuntimePoolKey;
        return result;
    }

    ///<summary>
    /// 충돌 기반 아이템 획득 처리
    ///</summary>
    private void OnTriggerEnter2D( Collider2D _other )
    {
        if ( isPickupInProgress )
        {
            return;
        }

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
        CPlayerStatManager statManager = playerController.GetPlayerStatManager();

        if ( inventoryManager == null )
        {
            return;
        }

        CItemDefinition itemDefinition = ResolveItemDefinition();

        if ( itemDefinition == null )
        {
            return;
        }

        isPickupInProgress = true;

        if ( pickupTriggerCollider != null )
        {
            pickupTriggerCollider.enabled = false;
        }

        int resolvedItemCount = ResolvePickupItemCount( itemDefinition, statManager );
        bool wasAdded = inventoryManager.TryAddItem( itemDefinition, resolvedItemCount );

        if ( wasAdded == false )
        {
            isPickupInProgress = false;

            if ( pickupTriggerCollider != null )
            {
                pickupTriggerCollider.enabled = true;
            }

            return;
        }

        ReleaseToPoolOrDeactivate();
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

    ///<summary>
    /// 드랍 충돌 활성 상태 설정
    ///</summary>
    public void SetPickupTriggerEnabled( bool _isEnabled )
    {
        ResolveReferences();

        if ( pickupTriggerCollider == null )
        {
            return;
        }

        pickupTriggerCollider.enabled = _isEnabled;
    }

    ///<summary>
    /// 드랍 오브젝트 반환 상태 정리
    ///</summary>
    public void PrepareForRelease()
    {
        SetPickupTriggerEnabled( false );
    }

    ///<summary>
    /// 드랍 오브젝트 풀 반환 또는 비활성화
    ///</summary>
    private void ReleaseToPoolOrDeactivate()
    {
        bool hasMapManager = CMapManager.TryGetInstance( out CMapManager mapManager );

        if ( hasMapManager && mapManager != null )
        {
            bool wasReleased = mapManager.ReleasePooledWorldItemDrop( this, mapRuntimePoolKey );

            if ( wasReleased )
            {
                return;
            }
        }

        gameObject.SetActive( false );
    }

    ///<summary>
    /// 획득 수량 보정 처리
    ///</summary>
    private int ResolvePickupItemCount( CItemDefinition _itemDefinition, CPlayerStatManager _playerStatManager )
    {
        int resolvedItemCount = Mathf.Max( 1, itemCount );

        if ( _itemDefinition == null || _playerStatManager == null )
        {
            return resolvedItemCount;
        }

        if ( _itemDefinition.GetItemType() != eItemType.CURRENCY )
        {
            return resolvedItemCount;
        }

        float goldGainMultiplier = _playerStatManager.GetGoldGainMultiplier();
        int scaledItemCount = Mathf.Max( 1, Mathf.RoundToInt( resolvedItemCount * goldGainMultiplier ) );
        return scaledItemCount;
    }
}
