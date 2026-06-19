using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;

///<summary>
/// NPC 상호작용 루트 컴포넌트
///</summary>
[DisallowMultipleComponent]
public sealed class CNPCObject : MonoBehaviour
{
    private const string InteractionRangeObjectName = "InteractionRange";
    private const float InteractionRangeOffsetY = 0.4f;
    private const float MinimumInteractionRangeWidth = 1.6f;
    private const float MinimumInteractionRangeHeight = 1.6f;
    private const float InteractionRangeExtraWidth = 0.8f;
    private const float InteractionRangeExtraHeight = 0.8f;
    private const float DefaultFacingScaleX = 1.0f;

    [SerializeField] private string npcId = string.Empty;
    [SerializeField] private string npcName = string.Empty;
    [SerializeField] private CNPCInteractionData interactionData;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private BoxCollider2D interactionRangeCollider;

    private readonly Dictionary<int, int> lastDialoguePresetIndexByActionIndex = new Dictionary<int, int>();

    ///<summary>
    /// 컴포넌트 초기화
    ///</summary>
    private void Awake()
    {
        EnsureBodyCollider();
        EnsureInteractionRangeCollider();
    }

    ///<summary>
    /// 활성화 시 이름표 등록
    ///</summary>
    private void OnEnable()
    {
        ApplyCollisionIgnoreToActivePlayers();
        CNPCNameTagManager nameTagManager = CNPCNameTagManager.Instance;

        if ( nameTagManager == null )
        {
            return;
        }

        nameTagManager.RegisterNpc( this );
    }

    ///<summary>
    /// 비활성화 시 이름표 해제
    ///</summary>
    private void OnDisable()
    {
        if ( CNPCNameTagManager.TryGetInstance( out CNPCNameTagManager nameTagManager ) == false )
        {
            return;
        }

        nameTagManager.UnregisterNpc( this );
    }

    ///<summary>
    /// NPC ID 반환
    ///</summary>
    public string GetNpcId()
    {
        string resolvedNpcId = string.IsNullOrWhiteSpace( npcId ) == false ? npcId : gameObject.name;
        string result = resolvedNpcId;
        return result;
    }

    ///<summary>
    /// NPC 이름 반환
    ///</summary>
    public string GetNpcName()
    {
        string resolvedNpcName = string.IsNullOrWhiteSpace( npcName ) == false ? npcName : gameObject.name;
        string result = resolvedNpcName;
        return result;
    }

    ///<summary>
    /// NPC 표시 이름 반환
    ///</summary>
    public string GetDisplayName()
    {
        if ( interactionData != null && string.IsNullOrWhiteSpace( interactionData.GetNpcName() ) == false )
        {
            string dataNpcName = interactionData.GetNpcName();
            return dataNpcName;
        }

        string result = GetNpcName();
        return result;
    }

    ///<summary>
    /// 이름표 월드 위치 반환
    ///</summary>
    public Vector3 GetNameTagWorldPosition()
    {
        if ( bodyCollider == null )
        {
            Vector3 fallbackPosition = transform.position + new Vector3( 0.0f, 1.4f, 0.0f );
            return fallbackPosition;
        }

        Bounds colliderBounds = bodyCollider.bounds;
        Vector3 result = new Vector3( colliderBounds.center.x, colliderBounds.max.y, colliderBounds.center.z );
        return result;
    }

    ///<summary>
    /// NPC 상호작용 데이터 반환
    ///</summary>
    public CNPCInteractionData GetInteractionData()
    {
        CNPCInteractionData result = interactionData;
        return result;
    }

    ///<summary>
    /// 상호작용 범위 콜라이더 반환
    ///</summary>
    public BoxCollider2D GetInteractionRangeCollider()
    {
        BoxCollider2D result = interactionRangeCollider;
        return result;
    }

    ///<summary>
    /// 대화 프리셋 인덱스 결정
    ///</summary>
    public int ResolveNextDialoguePresetIndex( int _actionEntryIndex, int _presetCount )
    {
        if ( _presetCount <= 0 )
        {
            return -1;
        }

        if ( _presetCount == 1 )
        {
            lastDialoguePresetIndexByActionIndex[ _actionEntryIndex ] = 0;
            return 0;
        }

        int lastPresetIndex = -1;
        bool hasLastPresetIndex = lastDialoguePresetIndexByActionIndex.TryGetValue( _actionEntryIndex, out lastPresetIndex );

        if ( hasLastPresetIndex == false || lastPresetIndex < 0 || lastPresetIndex >= _presetCount )
        {
            int initialRandomIndex = Random.Range( 0, _presetCount );
            lastDialoguePresetIndexByActionIndex[ _actionEntryIndex ] = initialRandomIndex;
            return initialRandomIndex;
        }

        int randomOffset = Random.Range( 1, _presetCount );
        int nextPresetIndex = ( lastPresetIndex + randomOffset ) % _presetCount;
        lastDialoguePresetIndexByActionIndex[ _actionEntryIndex ] = nextPresetIndex;
        return nextPresetIndex;
    }

    ///<summary>
    /// NPC ID 설정
    ///</summary>
    public void SetNpcId( string _npcId )
    {
        npcId = string.IsNullOrWhiteSpace( _npcId ) ? string.Empty : _npcId.Trim();
    }

    ///<summary>
    /// NPC 이름 설정
    ///</summary>
    public void SetNpcName( string _npcName )
    {
        npcName = string.IsNullOrWhiteSpace( _npcName ) ? string.Empty : _npcName.Trim();
    }

    ///<summary>
    /// NPC 상호작용 데이터 설정
    ///</summary>
    public void SetInteractionData( CNPCInteractionData _interactionData )
    {
        interactionData = _interactionData;
    }

    ///<summary>
    /// 대상 방향 바라보기 적용
    ///</summary>
    public void FaceTarget( Transform _targetTransform )
    {
        if ( _targetTransform == null )
        {
            return;
        }

        Vector3 npcPosition = transform.position;
        Vector3 targetPosition = _targetTransform.position;
        float deltaX = targetPosition.x - npcPosition.x;

        if ( Mathf.Approximately( deltaX, 0.0f ) )
        {
            return;
        }

        Vector3 localScale = transform.localScale;
        float scaleMagnitudeX = Mathf.Abs( localScale.x );

        if ( scaleMagnitudeX <= 0.0f )
        {
            scaleMagnitudeX = DefaultFacingScaleX;
        }

        float facingSignX = deltaX > 0.0f ? 1.0f : -1.0f;
        localScale.x = scaleMagnitudeX * facingSignX;
        transform.localScale = localScale;
    }

    ///<summary>
    /// 플레이어 충돌 무시 적용
    ///</summary>
    public void IgnoreCollisionWithPlayer( PlayerController _playerController )
    {
        if ( _playerController == null )
        {
            return;
        }

        Collider2D[] npcColliderArray = GetComponentsInChildren<Collider2D>( true );
        Collider2D[] playerColliderArray = _playerController.GetComponentsInChildren<Collider2D>( true );
        int npcColliderCount = npcColliderArray.Length;
        int playerColliderCount = playerColliderArray.Length;

        for ( int npcColliderIndex = 0; npcColliderIndex < npcColliderCount; npcColliderIndex++ )
        {
            Collider2D npcCollider = npcColliderArray[ npcColliderIndex ];

            if ( npcCollider == null || npcCollider == interactionRangeCollider || npcCollider.isTrigger )
            {
                continue;
            }

            for ( int playerColliderIndex = 0; playerColliderIndex < playerColliderCount; playerColliderIndex++ )
            {
                Collider2D playerCollider = playerColliderArray[ playerColliderIndex ];

                if ( playerCollider == null )
                {
                    continue;
                }

                Physics2D.IgnoreCollision( npcCollider, playerCollider, true );
            }
        }
    }

    ///<summary>
    /// 본체 콜라이더 보장
    ///</summary>
    public void EnsureBodyCollider()
    {
        if ( bodyCollider != null )
        {
            return;
        }

        BoxCollider2D resolvedBodyCollider = GetComponent<BoxCollider2D>();
        bodyCollider = resolvedBodyCollider;
    }

    ///<summary>
    /// 상호작용 범위 콜라이더 보장
    ///</summary>
    public void EnsureInteractionRangeCollider()
    {
        Transform interactionRangeTransform = transform.Find( InteractionRangeObjectName );

        if ( interactionRangeTransform == null )
        {
            GameObject interactionRangeObject = new GameObject( InteractionRangeObjectName );
            Transform createdTransform = interactionRangeObject.transform;
            createdTransform.SetParent( transform, false );
            interactionRangeTransform = createdTransform;
        }

        if ( interactionRangeCollider == null )
        {
            BoxCollider2D resolvedInteractionRangeCollider = interactionRangeTransform.GetComponent<BoxCollider2D>();

            if ( resolvedInteractionRangeCollider == null )
            {
                resolvedInteractionRangeCollider = interactionRangeTransform.gameObject.AddComponent<BoxCollider2D>();
            }

            interactionRangeCollider = resolvedInteractionRangeCollider;
        }

        interactionRangeCollider.isTrigger = true;
        interactionRangeCollider.offset = new Vector2( 0.0f, InteractionRangeOffsetY );
        interactionRangeCollider.size = ResolveInteractionRangeSize();

        CNPCInteractionRange interactionRange = interactionRangeTransform.GetComponent<CNPCInteractionRange>();

        if ( interactionRange == null )
        {
            interactionRange = interactionRangeTransform.gameObject.AddComponent<CNPCInteractionRange>();
        }

        interactionRange.ConfigureRange( this, interactionRangeCollider );
    }

    ///<summary>
    /// 상호작용 범위 크기 결정
    ///</summary>
    private Vector2 ResolveInteractionRangeSize()
    {
        if ( bodyCollider == null )
        {
            Vector2 fallbackSize = new Vector2( MinimumInteractionRangeWidth, MinimumInteractionRangeHeight );
            return fallbackSize;
        }

        Vector2 bodySize = bodyCollider.size;
        float width = Mathf.Max( MinimumInteractionRangeWidth, bodySize.x + InteractionRangeExtraWidth );
        float height = Mathf.Max( MinimumInteractionRangeHeight, bodySize.y + InteractionRangeExtraHeight );
        Vector2 result = new Vector2( width, height );
        return result;
    }

    ///<summary>
    /// 활성 플레이어 충돌 무시 일괄 적용
    ///</summary>
    private void ApplyCollisionIgnoreToActivePlayers()
    {
        PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
        int playerControllerCount = playerControllerArray.Length;

        for ( int index = 0; index < playerControllerCount; index++ )
        {
            PlayerController playerController = playerControllerArray[ index ];

            if ( playerController == null || playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
            {
                continue;
            }

            IgnoreCollisionWithPlayer( playerController );
        }
    }
}
