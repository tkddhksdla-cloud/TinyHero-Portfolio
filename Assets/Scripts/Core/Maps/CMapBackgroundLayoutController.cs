using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 배경 반복 렌더러와 경계 콜라이더 자동 정렬
    ///</summary>
    [DisallowMultipleComponent]
    [RequireComponent( typeof( SpriteRenderer ) )]
    public sealed class CMapBackgroundLayoutController : MonoBehaviour
    {
        private const string MirroredBackgroundObjectName = "BackgroundObject_FlipX";
        private const string BottomColliderObjectName = "Collider_Bottom";
        private const string LeftColliderObjectName = "Collider_Left";
        private const string RightColliderObjectName = "Collider_Right";
        private const float DefaultSideColliderWidth = 0.24f;
        private const float MinimumColliderSize = 0.01f;

        [Header( "참조" )]
        [SerializeField] private SpriteRenderer primaryRenderer;
        [SerializeField] private SpriteRenderer mirroredRenderer;

        [Header( "콜라이더" )]
        [SerializeField] private BoxCollider2D bottomCollider;
        [SerializeField] private BoxCollider2D leftCollider;
        [SerializeField] private BoxCollider2D rightCollider;

        [Header( "경계" )]
        [SerializeField] private bool useCustomRightBoundary;
        [SerializeField] private float customRightBoundaryX;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            RefreshLayout();
        }

        ///<summary>
        /// 인스펙터 값 검증
        ///</summary>
        private void OnValidate()
        {
            ResolveReferences( false );

            if ( primaryRenderer == null || primaryRenderer.sprite == null || mirroredRenderer == null )
            {
                return;
            }

            ConfigureMirroredRenderer();
            RefreshColliderLayout();
        }

        ///<summary>
        /// 배경 렌더러와 콜라이더 배치 갱신
        ///</summary>
        public void RefreshLayout()
        {
            ResolveReferences( true );

            if ( primaryRenderer == null || primaryRenderer.sprite == null )
            {
                SetMirroredRendererSprite( null );
                return;
            }

            ConfigureMirroredRenderer();
            RefreshColliderLayout();
        }

        ///<summary>
        /// 우측 경계 월드 좌표 설정
        ///</summary>
        public void SetCustomRightBoundaryX( float _worldX )
        {
            useCustomRightBoundary = true;
            customRightBoundaryX = _worldX;
            RefreshLayout();
        }

        ///<summary>
        /// 우측 경계 기본값 복원
        ///</summary>
        public void ClearCustomRightBoundary()
        {
            useCustomRightBoundary = false;
            customRightBoundaryX = 0.0f;
            RefreshLayout();
        }

        ///<summary>
        /// 우측 경계 월드 좌표 조회 시도
        ///</summary>
        public bool TryGetCustomRightBoundaryX( out float _worldX )
        {
            _worldX = customRightBoundaryX;
            bool result = useCustomRightBoundary;
            return result;
        }

        ///<summary>
        /// 전체 배경 월드 경계 조회 시도
        ///</summary>
        public bool TryGetCombinedWorldBounds( out Bounds _bounds )
        {
            _bounds = default;
            RefreshLayout();

            if ( primaryRenderer == null || primaryRenderer.sprite == null )
            {
                return false;
            }

            Bounds combinedBounds = primaryRenderer.bounds;

            if ( mirroredRenderer != null && mirroredRenderer.sprite != null )
            {
                combinedBounds.Encapsulate( mirroredRenderer.bounds );
            }

            if ( useCustomRightBoundary )
            {
                float clampedMaxX = Mathf.Clamp( customRightBoundaryX, combinedBounds.min.x + MinimumColliderSize, combinedBounds.max.x );
                Vector3 min = combinedBounds.min;
                Vector3 max = combinedBounds.max;
                max.x = clampedMaxX;
                combinedBounds.SetMinMax( min, max );
            }

            _bounds = combinedBounds;
            return true;
        }

        ///<summary>
        /// 필요한 참조 결정
        ///</summary>
        private void ResolveReferences( bool _canCreateMissingObjects )
        {
            if ( primaryRenderer == null )
            {
                SpriteRenderer resolvedPrimaryRenderer = GetComponent<SpriteRenderer>();
                primaryRenderer = resolvedPrimaryRenderer;
            }

            if ( mirroredRenderer == null && _canCreateMissingObjects )
            {
                mirroredRenderer = ResolveOrCreateMirroredRenderer();
            }

            if ( mirroredRenderer == null && _canCreateMissingObjects == false )
            {
                Transform mirroredTransform = transform.Find( MirroredBackgroundObjectName );

                if ( mirroredTransform != null )
                {
                    mirroredRenderer = mirroredTransform.GetComponent<SpriteRenderer>();
                }
            }

            bottomCollider = ResolveCollider( bottomCollider, BottomColliderObjectName );
            leftCollider = ResolveCollider( leftCollider, LeftColliderObjectName );
            rightCollider = ResolveCollider( rightCollider, RightColliderObjectName );
        }

        ///<summary>
        /// 반전 배경 렌더러 결정 또는 생성
        ///</summary>
        private SpriteRenderer ResolveOrCreateMirroredRenderer()
        {
            Transform existingTransform = transform.Find( MirroredBackgroundObjectName );

            if ( existingTransform != null )
            {
                SpriteRenderer existingRenderer = existingTransform.GetComponent<SpriteRenderer>();

                if ( existingRenderer != null )
                {
                    return existingRenderer;
                }
            }

            GameObject mirroredObject = new GameObject( MirroredBackgroundObjectName );
            mirroredObject.layer = gameObject.layer;
            mirroredObject.transform.SetParent( transform, false );
            SpriteRenderer createdRenderer = mirroredObject.AddComponent<SpriteRenderer>();
            return createdRenderer;
        }

        ///<summary>
        /// 이름 기준 콜라이더 결정
        ///</summary>
        private BoxCollider2D ResolveCollider( BoxCollider2D _currentCollider, string _objectName )
        {
            if ( _currentCollider != null )
            {
                return _currentCollider;
            }

            Transform colliderTransform = transform.Find( _objectName );

            if ( colliderTransform == null )
            {
                return null;
            }

            BoxCollider2D resolvedCollider = colliderTransform.GetComponent<BoxCollider2D>();
            return resolvedCollider;
        }

        ///<summary>
        /// 반전 배경 렌더러 설정
        ///</summary>
        private void ConfigureMirroredRenderer()
        {
            if ( mirroredRenderer == null || primaryRenderer == null )
            {
                return;
            }

            SetMirroredRendererSprite( primaryRenderer.sprite );
            mirroredRenderer.gameObject.layer = gameObject.layer;
            mirroredRenderer.sharedMaterial = primaryRenderer.sharedMaterial;
            mirroredRenderer.color = primaryRenderer.color;
            mirroredRenderer.sortingLayerID = primaryRenderer.sortingLayerID;
            mirroredRenderer.sortingOrder = primaryRenderer.sortingOrder;
            mirroredRenderer.flipX = true;
            mirroredRenderer.flipY = primaryRenderer.flipY;
            mirroredRenderer.maskInteraction = primaryRenderer.maskInteraction;

            Bounds spriteBounds = primaryRenderer.sprite.bounds;
            Vector3 mirroredLocalPosition = Vector3.zero;
            mirroredLocalPosition.x = spriteBounds.size.x;
            mirroredRenderer.transform.localPosition = mirroredLocalPosition;
            mirroredRenderer.transform.localRotation = Quaternion.identity;
            mirroredRenderer.transform.localScale = Vector3.one;
        }

        ///<summary>
        /// 반전 배경 스프라이트 설정
        ///</summary>
        private void SetMirroredRendererSprite( Sprite _sprite )
        {
            if ( mirroredRenderer == null )
            {
                return;
            }

            mirroredRenderer.sprite = _sprite;
            mirroredRenderer.enabled = _sprite != null;
        }

        ///<summary>
        /// 배경 크기 기준 콜라이더 정렬
        ///</summary>
        private void RefreshColliderLayout()
        {
            if ( primaryRenderer == null || primaryRenderer.sprite == null )
            {
                return;
            }

            Bounds spriteBounds = primaryRenderer.sprite.bounds;
            float singleWidth = Mathf.Max( MinimumColliderSize, spriteBounds.size.x );
            float combinedWidth = singleWidth * 2.0f;
            float combinedMinX = spriteBounds.min.x;
            float combinedMaxX = combinedMinX + combinedWidth;
            float resolvedMaxX = ResolveRightBoundaryLocalX( combinedMinX, combinedMaxX );
            float resolvedWidth = Mathf.Max( MinimumColliderSize, resolvedMaxX - combinedMinX );
            float combinedCenterX = combinedMinX + ( resolvedWidth * 0.5f );

            RefreshBottomCollider( combinedCenterX, resolvedWidth );
            RefreshSideCollider( leftCollider, combinedMinX );
            RefreshSideCollider( rightCollider, resolvedMaxX );
        }

        ///<summary>
        /// 우측 경계 로컬 좌표 결정
        ///</summary>
        private float ResolveRightBoundaryLocalX( float _combinedMinX, float _defaultCombinedMaxX )
        {
            if ( useCustomRightBoundary == false )
            {
                return _defaultCombinedMaxX;
            }

            Vector3 customWorldPosition = new Vector3( customRightBoundaryX, transform.position.y, transform.position.z );
            Vector3 customLocalPosition = transform.InverseTransformPoint( customWorldPosition );
            float minimumMaxX = _combinedMinX + MinimumColliderSize;
            float result = Mathf.Clamp( customLocalPosition.x, minimumMaxX, _defaultCombinedMaxX );
            return result;
        }

        ///<summary>
        /// 하단 콜라이더 크기 갱신
        ///</summary>
        private void RefreshBottomCollider( float _combinedCenterX, float _combinedWidth )
        {
            if ( bottomCollider == null )
            {
                return;
            }

            Transform bottomTransform = bottomCollider.transform;
            Vector3 localPosition = bottomTransform.localPosition;
            localPosition.x = _combinedCenterX;
            bottomTransform.localPosition = localPosition;

            Vector2 colliderOffset = bottomCollider.offset;
            colliderOffset.x = 0.0f;
            bottomCollider.offset = colliderOffset;

            Vector2 colliderSize = bottomCollider.size;
            colliderSize.x = Mathf.Max( MinimumColliderSize, _combinedWidth );
            bottomCollider.size = colliderSize;
        }

        ///<summary>
        /// 측면 콜라이더 위치 갱신
        ///</summary>
        private void RefreshSideCollider( BoxCollider2D _sideCollider, float _edgeX )
        {
            if ( _sideCollider == null )
            {
                return;
            }

            Transform sideTransform = _sideCollider.transform;
            Vector3 localPosition = sideTransform.localPosition;
            localPosition.x = _edgeX;
            sideTransform.localPosition = localPosition;

            Vector2 colliderOffset = _sideCollider.offset;
            colliderOffset.x = 0.0f;
            _sideCollider.offset = colliderOffset;

            Vector2 colliderSize = _sideCollider.size;
            colliderSize.x = Mathf.Max( DefaultSideColliderWidth, colliderSize.x );
            _sideCollider.size = colliderSize;
        }
    }
}
