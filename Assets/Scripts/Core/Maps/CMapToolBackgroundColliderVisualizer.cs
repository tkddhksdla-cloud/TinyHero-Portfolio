using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴 배경 콜라이더 시각화 클래스
    ///</summary>
    [DisallowMultipleComponent]
    [RequireComponent( typeof( SpriteRenderer ) )]
    public sealed class CMapToolBackgroundColliderVisualizer : MonoBehaviour
    {
        private const string ColliderChildNamePrefix = "Collider";
        private const float DefaultLineWidth = 0.05f;
        private const int OutlinePointCount = 5;
        private const int SortingOrderOffset = 10;

        private sealed class ColliderVisualEntry
        {
            public Transform colliderRoot;
            public BoxCollider2D collider;
            public LineRenderer lineRenderer;
            public Vector2 previousOffset;
            public Vector2 previousSize;
            public Vector3 previousLocalPosition;
            public Vector3 previousLocalScale;
        }

        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [SerializeField] private Color outlineColor = new Color( 0.85f, 0.10f, 0.10f, 1.0f );

        private readonly List<ColliderVisualEntry> colliderVisualEntries = new List<ColliderVisualEntry>();
        private Sprite previousSprite;
        private Vector3 previousLocalScale;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            RefreshColliderVisual();
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshColliderVisual();
        }

        ///<summary>
        /// 후처리 갱신
        ///</summary>
        private void LateUpdate()
        {
            if ( ShouldRefreshVisual() == false )
            {
                return;
            }

            RefreshColliderVisual();
        }

        ///<summary>
        /// 인스펙터 값 검증
        ///</summary>
        private void OnValidate()
        {
            ResolveReferences();

            if ( isActiveAndEnabled == false )
            {
                return;
            }

            RefreshColliderVisual();
        }

        ///<summary>
        /// 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetSpriteRenderer == null )
            {
                SpriteRenderer resolvedSpriteRenderer = GetComponent<SpriteRenderer>();
                targetSpriteRenderer = resolvedSpriteRenderer;
            }

            SyncColliderVisualEntries();
        }

        ///<summary>
        /// 콜라이더 시각화 항목 목록 동기화
        ///</summary>
        private void SyncColliderVisualEntries()
        {
            colliderVisualEntries.Clear();
            BoxCollider2D[] childColliders = GetComponentsInChildren<BoxCollider2D>( true );
            int colliderCount = childColliders.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                BoxCollider2D childCollider = childColliders[ index ];

                if ( childCollider == null )
                {
                    continue;
                }

                Transform colliderTransform = childCollider.transform;

                if ( colliderTransform == transform )
                {
                    continue;
                }

                if ( colliderTransform.name.StartsWith( ColliderChildNamePrefix ) == false )
                {
                    continue;
                }

                LineRenderer lineRenderer = colliderTransform.GetComponent<LineRenderer>();

                if ( lineRenderer == null )
                {
                    lineRenderer = colliderTransform.gameObject.AddComponent<LineRenderer>();
                }

                ColliderVisualEntry visualEntry = new ColliderVisualEntry();
                visualEntry.colliderRoot = colliderTransform;
                visualEntry.collider = childCollider;
                visualEntry.lineRenderer = lineRenderer;
                ConfigureLineRenderer( lineRenderer );
                colliderVisualEntries.Add( visualEntry );
            }
        }

        ///<summary>
        /// 콜라이더 시각화 갱신
        ///</summary>
        public void RefreshColliderVisual()
        {
            ResolveReferences();

            int colliderVisualCount = colliderVisualEntries.Count;

            for ( int index = 0; index < colliderVisualCount; index++ )
            {
                ColliderVisualEntry visualEntry = colliderVisualEntries[ index ];
                UpdateOutlinePositions( visualEntry );
                CacheVisualEntryState( visualEntry );
            }

            previousSprite = targetSpriteRenderer != null ? targetSpriteRenderer.sprite : null;
            previousLocalScale = transform.localScale;
        }

        ///<summary>
        /// 갱신 시각화 필요 여부
        ///</summary>
        private bool ShouldRefreshVisual()
        {
            Sprite currentSprite = targetSpriteRenderer != null ? targetSpriteRenderer.sprite : null;

            if ( currentSprite != previousSprite )
            {
                return true;
            }

            if ( previousLocalScale != transform.localScale )
            {
                return true;
            }

            int colliderVisualCount = colliderVisualEntries.Count;

            for ( int index = 0; index < colliderVisualCount; index++ )
            {
                ColliderVisualEntry visualEntry = colliderVisualEntries[ index ];

                if ( HasVisualEntryChanged( visualEntry ) )
                {
                    return true;
                }
            }

            BoxCollider2D[] childColliders = GetComponentsInChildren<BoxCollider2D>( true );

            if ( childColliders.Length != colliderVisualEntries.Count )
            {
                return true;
            }

            return false;
        }

        ///<summary>
        /// 시각화 항목 변경 여부
        ///</summary>
        private bool HasVisualEntryChanged(ColliderVisualEntry _visualEntry)
        {
            if ( _visualEntry == null || _visualEntry.collider == null || _visualEntry.colliderRoot == null )
            {
                return true;
            }

            if ( _visualEntry.previousOffset != _visualEntry.collider.offset )
            {
                return true;
            }

            if ( _visualEntry.previousSize != _visualEntry.collider.size )
            {
                return true;
            }

            if ( _visualEntry.previousLocalPosition != _visualEntry.colliderRoot.localPosition )
            {
                return true;
            }

            if ( _visualEntry.previousLocalScale != _visualEntry.colliderRoot.localScale )
            {
                return true;
            }

            return false;
        }

        ///<summary>
        /// 라인 렌더러 설정
        ///</summary>
        private void ConfigureLineRenderer(LineRenderer _targetLineRenderer)
        {
            if ( _targetLineRenderer == null )
            {
                return;
            }

            Shader spriteShader = Shader.Find( "Sprites/Default" );

            if ( spriteShader != null )
            {
                Material lineMaterial = new Material( spriteShader );
                _targetLineRenderer.material = lineMaterial;
            }

            _targetLineRenderer.useWorldSpace = false;
            _targetLineRenderer.loop = false;
            _targetLineRenderer.positionCount = OutlinePointCount;
            _targetLineRenderer.startWidth = DefaultLineWidth;
            _targetLineRenderer.endWidth = DefaultLineWidth;
            _targetLineRenderer.startColor = outlineColor;
            _targetLineRenderer.endColor = outlineColor;
            _targetLineRenderer.sortingLayerID = targetSpriteRenderer != null ? targetSpriteRenderer.sortingLayerID : 0;
            _targetLineRenderer.sortingOrder = targetSpriteRenderer != null ? targetSpriteRenderer.sortingOrder + SortingOrderOffset : SortingOrderOffset;
        }

        ///<summary>
        /// 외곽선 위치 갱신
        ///</summary>
        private void UpdateOutlinePositions(ColliderVisualEntry _visualEntry)
        {
            if ( _visualEntry == null || _visualEntry.collider == null || _visualEntry.lineRenderer == null )
            {
                return;
            }

            Vector2 colliderOffset = _visualEntry.collider.offset;
            Vector2 colliderSize = _visualEntry.collider.size;
            float halfWidth = colliderSize.x * 0.5f;
            float halfHeight = colliderSize.y * 0.5f;

            Vector3 bottomLeft = new Vector3( colliderOffset.x - halfWidth, colliderOffset.y - halfHeight, 0.0f );
            Vector3 topLeft = new Vector3( colliderOffset.x - halfWidth, colliderOffset.y + halfHeight, 0.0f );
            Vector3 topRight = new Vector3( colliderOffset.x + halfWidth, colliderOffset.y + halfHeight, 0.0f );
            Vector3 bottomRight = new Vector3( colliderOffset.x + halfWidth, colliderOffset.y - halfHeight, 0.0f );

            _visualEntry.lineRenderer.SetPosition( 0, bottomLeft );
            _visualEntry.lineRenderer.SetPosition( 1, topLeft );
            _visualEntry.lineRenderer.SetPosition( 2, topRight );
            _visualEntry.lineRenderer.SetPosition( 3, bottomRight );
            _visualEntry.lineRenderer.SetPosition( 4, bottomLeft );
        }

        ///<summary>
        /// 시각화 항목 상태 캐시
        ///</summary>
        private void CacheVisualEntryState(ColliderVisualEntry _visualEntry)
        {
            _visualEntry.previousOffset = _visualEntry.collider.offset;
            _visualEntry.previousSize = _visualEntry.collider.size;
            _visualEntry.previousLocalPosition = _visualEntry.colliderRoot.localPosition;
            _visualEntry.previousLocalScale = _visualEntry.colliderRoot.localScale;
        }
    }
}


