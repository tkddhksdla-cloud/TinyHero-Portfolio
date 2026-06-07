using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 배경의 자식 콜라이더 범위를 모두 보이도록 유지한다.
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
        /// 필수 참조와 콜라이더 목록을 초기화한다.
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            RefreshColliderVisual();
        }

        ///<summary>
        /// 활성화 시 모든 콜라이더 외곽선을 즉시 갱신한다.
        ///</summary>
        private void OnEnable()
        {
            RefreshColliderVisual();
        }

        ///<summary>
        /// 배경이나 자식 콜라이더 변경에 맞춰 외곽선을 갱신한다.
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
        /// 인스펙터 변경 직후 외곽선을 다시 맞춘다.
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
        /// 스프라이트와 자식 콜라이더 참조를 다시 수집한다.
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
        /// 현재 자식 콜라이더 목록에 맞춰 시각화 엔트리를 동기화한다.
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
        /// 모든 자식 콜라이더의 외곽선을 현재 값에 맞춰 갱신한다.
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
        /// 외곽선 갱신이 필요한지 검사한다.
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
        /// 개별 콜라이더 엔트리의 변경 여부를 검사한다.
        ///</summary>
        private bool HasVisualEntryChanged( ColliderVisualEntry visualEntry )
        {
            if ( visualEntry == null || visualEntry.collider == null || visualEntry.colliderRoot == null )
            {
                return true;
            }

            if ( visualEntry.previousOffset != visualEntry.collider.offset )
            {
                return true;
            }

            if ( visualEntry.previousSize != visualEntry.collider.size )
            {
                return true;
            }

            if ( visualEntry.previousLocalPosition != visualEntry.colliderRoot.localPosition )
            {
                return true;
            }

            if ( visualEntry.previousLocalScale != visualEntry.colliderRoot.localScale )
            {
                return true;
            }

            return false;
        }

        ///<summary>
        /// 라인 렌더러 공통 설정을 적용한다.
        ///</summary>
        private void ConfigureLineRenderer( LineRenderer targetLineRenderer )
        {
            if ( targetLineRenderer == null )
            {
                return;
            }

            Shader spriteShader = Shader.Find( "Sprites/Default" );

            if ( spriteShader != null )
            {
                Material lineMaterial = new Material( spriteShader );
                targetLineRenderer.material = lineMaterial;
            }

            targetLineRenderer.useWorldSpace = false;
            targetLineRenderer.loop = false;
            targetLineRenderer.positionCount = OutlinePointCount;
            targetLineRenderer.startWidth = DefaultLineWidth;
            targetLineRenderer.endWidth = DefaultLineWidth;
            targetLineRenderer.startColor = outlineColor;
            targetLineRenderer.endColor = outlineColor;
            targetLineRenderer.sortingLayerID = targetSpriteRenderer != null ? targetSpriteRenderer.sortingLayerID : 0;
            targetLineRenderer.sortingOrder = targetSpriteRenderer != null ? targetSpriteRenderer.sortingOrder + SortingOrderOffset : SortingOrderOffset;
        }

        ///<summary>
        /// 개별 콜라이더 범위에 맞는 외곽선 점들을 설정한다.
        ///</summary>
        private void UpdateOutlinePositions( ColliderVisualEntry visualEntry )
        {
            if ( visualEntry == null || visualEntry.collider == null || visualEntry.lineRenderer == null )
            {
                return;
            }

            Vector2 colliderOffset = visualEntry.collider.offset;
            Vector2 colliderSize = visualEntry.collider.size;
            float halfWidth = colliderSize.x * 0.5f;
            float halfHeight = colliderSize.y * 0.5f;

            Vector3 bottomLeft = new Vector3( colliderOffset.x - halfWidth, colliderOffset.y - halfHeight, 0.0f );
            Vector3 topLeft = new Vector3( colliderOffset.x - halfWidth, colliderOffset.y + halfHeight, 0.0f );
            Vector3 topRight = new Vector3( colliderOffset.x + halfWidth, colliderOffset.y + halfHeight, 0.0f );
            Vector3 bottomRight = new Vector3( colliderOffset.x + halfWidth, colliderOffset.y - halfHeight, 0.0f );

            visualEntry.lineRenderer.SetPosition( 0, bottomLeft );
            visualEntry.lineRenderer.SetPosition( 1, topLeft );
            visualEntry.lineRenderer.SetPosition( 2, topRight );
            visualEntry.lineRenderer.SetPosition( 3, bottomRight );
            visualEntry.lineRenderer.SetPosition( 4, bottomLeft );
        }

        ///<summary>
        /// 개별 콜라이더 엔트리의 현재 상태를 캐시한다.
        ///</summary>
        private void CacheVisualEntryState( ColliderVisualEntry visualEntry )
        {
            visualEntry.previousOffset = visualEntry.collider.offset;
            visualEntry.previousSize = visualEntry.collider.size;
            visualEntry.previousLocalPosition = visualEntry.colliderRoot.localPosition;
            visualEntry.previousLocalScale = visualEntry.colliderRoot.localScale;
        }
    }
}
