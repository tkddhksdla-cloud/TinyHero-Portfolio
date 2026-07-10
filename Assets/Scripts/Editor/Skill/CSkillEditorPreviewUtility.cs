using TinyHero.Skill;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 스킬 범위 미리보기 도형 분류
    ///</summary>
    public enum eSkillRangePreviewShape
    {
        NONE,
        CIRCLE
    }

    ///<summary>
    /// 스킬 범위 미리보기 데이터
    ///</summary>
    public struct CSkillRangePreviewData
    {
        public bool isValid;
        public eSkillRangePreviewShape shapeType;
        public Vector2 offset;
        public float radius;
        public Vector2[] trajectoryPointArray;
        public string title;
        public string detail;
    }

    ///<summary>
    /// 스킬 편집기 미리보기 유틸리티
    ///</summary>
    public static class CSkillEditorPreviewUtility
    {
        private const string PlayerPrefabAssetPath = "Assets/Resources/Prefabs/Character/Player/PlayerObject.prefab";
        private const float PreviewPadding = 12.0f;
        private const float PreviewAxisThickness = 2.0f;
        private const float PreviewOwnerRadius = 6.0f;
        private const float PreviewMinRangeSize = 8.0f;
        private const float PreviewWorldPaddingUnits = 0.35f;
        private const float PlayerFallbackWidth = 0.9f;
        private const float PlayerFallbackHeight = 1.6f;
        private const float PlayerFallbackCenterY = 0.8f;
        private const float PlayerPreviewTextureAlpha = 0.96f;
        private const float PlayerFallbackFillAlpha = 0.20f;
        private const float PlayerFallbackOutlineAlpha = 0.80f;
        private static readonly Color PreviewBackgroundColor = new Color32( 0x51, 0x51, 0x51, 0xFF );
        private static readonly Color PreviewAxisColor = new Color( 1.0f, 1.0f, 1.0f, 0.16f );
        private static readonly Color PreviewRangeFillColor = new Color( 0.16f, 0.78f, 1.0f, 0.38f );
        private static readonly Color PreviewRangeWireColor = new Color( 0.52f, 0.90f, 1.0f, 1.0f );
        private static readonly Color PreviewLinkLineColor = new Color( 1.0f, 0.98f, 0.88f, 0.78f );
        private static readonly Color PreviewPivotColor = new Color( 1.0f, 0.82f, 0.24f, 1.0f );

        private static CPlayerPreviewRenderData cachedPlayerPreviewRenderData;

        private struct CPreviewWorldBounds
        {
            public float minX;
            public float maxX;
            public float minY;
            public float maxY;
        }

        private sealed class CPlayerPreviewRenderData
        {
            public bool isInitialized;
            public bool isPreviewReady;
            public Texture2D previewTexture;
            public Rect bounds;
        }

        ///<summary>
        /// 스킬 타입 요약 문구 생성
        ///</summary>
        public static string BuildTypeSummaryText( SerializedProperty _skillTypeProperty, SerializedProperty _activeSkillTypeProperty )
        {
            string skillTypeLabel = _skillTypeProperty.enumDisplayNames[ _skillTypeProperty.enumValueIndex ];
            eActiveSkillType activeSkillType = ( eActiveSkillType ) _activeSkillTypeProperty.enumValueIndex;
            string activeTypeLabel = ResolveActiveSkillTypeLabel( activeSkillType );
            string result = $"Type: {skillTypeLabel} / Active: {activeTypeLabel}";
            return result;
        }

        ///<summary>
        /// 계산된 액티브 스킬 타입 기준 요약 문자 생성
        ///</summary>
        public static string BuildTypeSummaryText( SerializedProperty _skillTypeProperty, eActiveSkillType _activeSkillType )
        {
            string skillTypeLabel = _skillTypeProperty.enumDisplayNames[ _skillTypeProperty.enumValueIndex ];
            string activeTypeLabel = ResolveActiveSkillTypeLabel( _activeSkillType );
            string result = $"Type: {skillTypeLabel} / Active: {activeTypeLabel}";
            return result;
        }

        ///<summary>
        /// 액티브 스킬 여부 판정
        ///</summary>
        public static bool IsActiveSkill( SerializedProperty _skillTypeProperty )
        {
            bool result = _skillTypeProperty.enumValueIndex == ( int ) eSkillType.ACTIVE;
            return result;
        }

        ///<summary>
        /// 패시브 스킬 여부 판정
        ///</summary>
        public static bool IsPassiveSkill( SerializedProperty _skillTypeProperty )
        {
            bool result = _skillTypeProperty.enumValueIndex == ( int ) eSkillType.PASSIVE;
            return result;
        }

        ///<summary>
        /// 범위 미리보기 데이터 생성
        ///</summary>
        public static CSkillRangePreviewData BuildRangePreviewData( Object _activeEffectObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();

            if ( _activeEffectObject == null )
            {
                return previewData;
            }

            SerializedObject activeEffectSerializedObject = new SerializedObject( _activeEffectObject );
            string effectTypeName = _activeEffectObject.GetType().Name;

            if ( effectTypeName == nameof( CInstantActiveSkillEffect ) )
            {
                previewData = BuildInstantRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            if ( effectTypeName == nameof( CPlaceActiveSkillEffect ) )
            {
                previewData = BuildPlaceRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            if ( effectTypeName == nameof( CProjectileActiveSkillEffect ) )
            {
                previewData = BuildProjectileRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            if ( effectTypeName == nameof( CBuffActiveSkillEffect ) )
            {
                previewData.isValid = true;
                previewData.shapeType = eSkillRangePreviewShape.NONE;
                previewData.title = "Self Target";
                previewData.detail = "Buff skills are applied to the caster instead of an attack area.";
                return previewData;
            }

            if ( effectTypeName == nameof( CCloneReplayActiveSkillEffect ) )
            {
                previewData = BuildCloneRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            if ( effectTypeName == nameof( CPhaseStrikeActiveSkillEffect ) )
            {
                previewData = BuildPhaseStrikeRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            if ( effectTypeName == nameof( CSkyfallStrikeActiveSkillEffect ) )
            {
                previewData = BuildSkyfallStrikeRangePreviewData( activeEffectSerializedObject );
                return previewData;
            }

            return previewData;
        }

        ///<summary>
        /// 범위 미리보기 캔버스 렌더링
        ///</summary>
        public static void DrawRangePreviewCanvas( Rect _previewRect, CSkillRangePreviewData _previewData )
        {
            EditorGUI.DrawRect( _previewRect, PreviewBackgroundColor );

            if ( _previewData.shapeType == eSkillRangePreviewShape.NONE )
            {
                Rect centeredLabelRect = new Rect( _previewRect.x, _previewRect.center.y - 8.0f, _previewRect.width, 18.0f );
                EditorGUI.LabelField( centeredLabelRect, "SELF", EditorStyles.centeredGreyMiniLabel );
                return;
            }

            Rect contentRect = new Rect(
                _previewRect.x + PreviewPadding,
                _previewRect.y + PreviewPadding,
                _previewRect.width - PreviewPadding * 2.0f,
                _previewRect.height - PreviewPadding * 2.0f
            );
            CPreviewWorldBounds worldBounds = BuildPreviewWorldBounds( _previewData );
            float pixelPerUnit = ResolvePreviewPixelPerUnit( contentRect, worldBounds );
            Rect mappedBoundsRect = BuildMappedBoundsRect( contentRect, worldBounds, pixelPerUnit );
            Vector2 ownerPosition = ConvertWorldToPreviewPosition( Vector2.zero, worldBounds, mappedBoundsRect, pixelPerUnit );
            Vector2 targetCenter = ConvertWorldToPreviewPosition( _previewData.offset, worldBounds, mappedBoundsRect, pixelPerUnit );
            float radius = Mathf.Max( PreviewMinRangeSize, _previewData.radius * pixelPerUnit );
            CPlayerPreviewRenderData playerPreviewRenderData = GetPlayerPreviewRenderData();

            if ( playerPreviewRenderData != null && playerPreviewRenderData.isPreviewReady == false )
            {
                RequestPreviewRepaint();
            }

            DrawPlayerPreview( playerPreviewRenderData, worldBounds, mappedBoundsRect, pixelPerUnit );

            EditorGUI.DrawRect(
                new Rect( mappedBoundsRect.x, ownerPosition.y - PreviewAxisThickness * 0.5f, mappedBoundsRect.width, PreviewAxisThickness ),
                PreviewAxisColor
            );
            EditorGUI.DrawRect(
                new Rect( ownerPosition.x - PreviewAxisThickness * 0.5f, mappedBoundsRect.y, PreviewAxisThickness, mappedBoundsRect.height ),
                PreviewAxisColor
            );
            Handles.BeginGUI();
            DrawTrajectoryPreview( _previewData, worldBounds, mappedBoundsRect, pixelPerUnit );
            Handles.color = PreviewRangeFillColor;
            Handles.DrawSolidDisc( targetCenter, Vector3.forward, radius );
            Handles.color = PreviewRangeWireColor;
            Handles.DrawWireDisc( targetCenter, Vector3.forward, radius );
            Handles.color = PreviewLinkLineColor;
            Handles.DrawLine( ownerPosition, targetCenter );
            Handles.EndGUI();
            EditorGUI.DrawRect(
                new Rect( ownerPosition.x - PreviewOwnerRadius, ownerPosition.y - PreviewOwnerRadius, PreviewOwnerRadius * 2.0f, PreviewOwnerRadius * 2.0f ),
                PreviewPivotColor
            );
            EditorGUI.LabelField(
                new Rect( _previewRect.x + 8.0f, _previewRect.y + 6.0f, _previewRect.width - 16.0f, 18.0f ),
                "Yellow: pivot / White: trajectory / Blue: landing damage area",
                EditorStyles.miniLabel
            );
        }

        ///<summary>
        /// 프리뷰 월드 경계 계산
        ///</summary>
        private static CPreviewWorldBounds BuildPreviewWorldBounds( CSkillRangePreviewData _previewData )
        {
            CPreviewWorldBounds worldBounds = new CPreviewWorldBounds();
            float radius = Mathf.Max( 0.0f, _previewData.radius );
            Rect playerBounds = GetPlayerPreviewWorldBounds();
            float minX = Mathf.Min( 0.0f, _previewData.offset.x - radius );
            float maxX = Mathf.Max( 0.0f, _previewData.offset.x + radius );
            float minY = Mathf.Min( 0.0f, _previewData.offset.y - radius );
            float maxY = Mathf.Max( 0.0f, _previewData.offset.y + radius );

            if ( _previewData.trajectoryPointArray != null )
            {
                for ( int index = 0; index < _previewData.trajectoryPointArray.Length; index++ )
                {
                    Vector2 trajectoryPoint = _previewData.trajectoryPointArray[ index ];
                    minX = Mathf.Min( minX, trajectoryPoint.x );
                    maxX = Mathf.Max( maxX, trajectoryPoint.x );
                    minY = Mathf.Min( minY, trajectoryPoint.y );
                    maxY = Mathf.Max( maxY, trajectoryPoint.y );
                }
            }
            minX = Mathf.Min( minX, playerBounds.xMin );
            maxX = Mathf.Max( maxX, playerBounds.xMax );
            minY = Mathf.Min( minY, playerBounds.yMin );
            maxY = Mathf.Max( maxY, playerBounds.yMax );
            worldBounds.minX = minX - PreviewWorldPaddingUnits;
            worldBounds.maxX = maxX + PreviewWorldPaddingUnits;
            worldBounds.minY = minY - PreviewWorldPaddingUnits;
            worldBounds.maxY = maxY + PreviewWorldPaddingUnits;
            return worldBounds;
        }

        ///<summary>
        /// 플레이어 프리뷰 렌더 데이터 반환
        ///</summary>
        private static CPlayerPreviewRenderData GetPlayerPreviewRenderData()
        {
            if ( cachedPlayerPreviewRenderData != null && cachedPlayerPreviewRenderData.isInitialized && cachedPlayerPreviewRenderData.isPreviewReady )
            {
                return cachedPlayerPreviewRenderData;
            }

            CPlayerPreviewRenderData previewRenderData = BuildPlayerPreviewRenderData();
            cachedPlayerPreviewRenderData = previewRenderData;
            return previewRenderData;
        }

        ///<summary>
        /// 플레이어 프리뷰 월드 경계 반환
        ///</summary>
        private static Rect GetPlayerPreviewWorldBounds()
        {
            CPlayerPreviewRenderData previewRenderData = GetPlayerPreviewRenderData();
            Rect result = previewRenderData.bounds;
            return result;
        }

        ///<summary>
        /// 플레이어 프리뷰 렌더 처리
        ///</summary>
        private static void DrawPlayerPreview( CPlayerPreviewRenderData _previewRenderData, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            if ( _previewRenderData == null )
            {
                return;
            }

            if ( _previewRenderData.previewTexture != null )
            {
                DrawPlayerPreviewTexture( _previewRenderData, _worldBounds, _mappedBoundsRect, _pixelPerUnit );
                return;
            }

            DrawPlayerFallbackSilhouette( _previewRenderData.bounds, _worldBounds, _mappedBoundsRect, _pixelPerUnit );
        }

        ///<summary>
        /// 플레이어 프리뷰 텍스처 렌더 처리
        ///</summary>
        private static void DrawPlayerPreviewTexture( CPlayerPreviewRenderData _previewRenderData, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            Rect previewRect = BuildMappedBoundsRectFromWorldRect(
                _previewRenderData.bounds.center,
                _previewRenderData.bounds.size,
                _worldBounds,
                _mappedBoundsRect,
                _pixelPerUnit
            );
            Color previousColor = GUI.color;
            GUI.color = new Color( 1.0f, 1.0f, 1.0f, PlayerPreviewTextureAlpha );
            GUI.DrawTexture( previewRect, _previewRenderData.previewTexture, ScaleMode.ScaleToFit, true );
            GUI.color = previousColor;
        }

        ///<summary>
        /// 플레이어 대체 실루엣 렌더 처리
        ///</summary>
        private static void DrawPlayerFallbackSilhouette( Rect _playerBounds, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            Rect previewRect = BuildMappedBoundsRectFromWorldRect(
                _playerBounds.center,
                _playerBounds.size,
                _worldBounds,
                _mappedBoundsRect,
                _pixelPerUnit
            );
            Color fillColor = new Color( 0.97f, 0.84f, 0.36f, PlayerFallbackFillAlpha );
            Color outlineColor = new Color( 0.97f, 0.84f, 0.36f, PlayerFallbackOutlineAlpha );
            EditorGUI.DrawRect( previewRect, fillColor );
            Handles.BeginGUI();
            Handles.color = outlineColor;
            Handles.DrawAAPolyLine(
                2.0f,
                new Vector3( previewRect.xMin, previewRect.yMin ),
                new Vector3( previewRect.xMax, previewRect.yMin ),
                new Vector3( previewRect.xMax, previewRect.yMax ),
                new Vector3( previewRect.xMin, previewRect.yMax ),
                new Vector3( previewRect.xMin, previewRect.yMin )
            );
            Handles.EndGUI();
        }

        ///<summary>
        /// 플레이어 프리뷰 렌더 데이터 생성
        ///</summary>
        private static CPlayerPreviewRenderData BuildPlayerPreviewRenderData()
        {
            CPlayerPreviewRenderData previewRenderData = new CPlayerPreviewRenderData();
            previewRenderData.isInitialized = true;
            previewRenderData.bounds = BuildFallbackPlayerBounds();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>( PlayerPrefabAssetPath );

            if ( playerPrefab == null )
            {
                return previewRenderData;
            }

            Texture2D assetPreviewTexture = AssetPreview.GetAssetPreview( playerPrefab );
            bool isLoadingPreview = AssetPreview.IsLoadingAssetPreview( playerPrefab.GetInstanceID() );

            if ( assetPreviewTexture == null )
            {
                Texture2D miniThumbnailTexture = AssetPreview.GetMiniThumbnail( playerPrefab );
                assetPreviewTexture = miniThumbnailTexture;
            }

            previewRenderData.isPreviewReady = assetPreviewTexture != null && isLoadingPreview == false;
            previewRenderData.previewTexture = assetPreviewTexture;
            SpriteRenderer[] spriteRendererArray = playerPrefab.GetComponentsInChildren<SpriteRenderer>( true );
            bool hasAnySprite = false;
            Bounds combinedBounds = new Bounds( Vector3.zero, Vector3.zero );

            for ( int index = 0; index < spriteRendererArray.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRendererArray[ index ];
                Bounds spriteBounds;
                bool hasSpriteBounds = TryBuildPlayerPreviewBounds( playerPrefab.transform, spriteRenderer, out spriteBounds );

                if ( hasSpriteBounds == false )
                {
                    continue;
                }

                if ( hasAnySprite == false )
                {
                    combinedBounds = spriteBounds;
                    hasAnySprite = true;
                }
                else
                {
                    combinedBounds.Encapsulate( spriteBounds.min );
                    combinedBounds.Encapsulate( spriteBounds.max );
                }
            }

            if ( hasAnySprite )
            {
                previewRenderData.bounds = new Rect(
                    combinedBounds.min.x,
                    combinedBounds.min.y,
                    combinedBounds.size.x,
                    combinedBounds.size.y
                );
            }

            return previewRenderData;
        }

        ///<summary>
        /// 플레이어 프리뷰 경계 생성 여부 반환
        ///</summary>
        private static bool TryBuildPlayerPreviewBounds( Transform _rootTransform, SpriteRenderer _spriteRenderer, out Bounds _spriteBounds )
        {
            _spriteBounds = default;

            if ( _rootTransform == null || _spriteRenderer == null || _spriteRenderer.sprite == null )
            {
                return false;
            }

            Sprite sprite = _spriteRenderer.sprite;
            Vector3 localPosition = _rootTransform.InverseTransformPoint( _spriteRenderer.transform.position );
            Vector3 lossyScale = _spriteRenderer.transform.lossyScale;
            Vector3 rootLossyScale = _rootTransform.lossyScale;
            float scaleX = rootLossyScale.x != 0.0f ? lossyScale.x / rootLossyScale.x : lossyScale.x;
            float scaleY = rootLossyScale.y != 0.0f ? lossyScale.y / rootLossyScale.y : lossyScale.y;
            Vector2 spriteSize = sprite.bounds.size;
            Vector2 worldSize = new Vector2( Mathf.Abs( spriteSize.x * scaleX ), Mathf.Abs( spriteSize.y * scaleY ) );
            Vector2 worldCenter = new Vector2( localPosition.x, localPosition.y );
            _spriteBounds = new Bounds( worldCenter, new Vector3( worldSize.x, worldSize.y, 0.0f ) );
            return true;
        }

        ///<summary>
        /// 플레이어 대체 경계 생성
        ///</summary>
        private static Rect BuildFallbackPlayerBounds()
        {
            Rect result = new Rect(
                -PlayerFallbackWidth * 0.5f,
                PlayerFallbackCenterY - PlayerFallbackHeight * 0.5f,
                PlayerFallbackWidth,
                PlayerFallbackHeight
            );
            return result;
        }

        ///<summary>
        /// 프리뷰 갱신 요청
        ///</summary>
        private static void RequestPreviewRepaint()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        ///<summary>
        /// 프리뷰 픽셀 스케일 계산
        ///</summary>
        private static float ResolvePreviewPixelPerUnit( Rect _contentRect, CPreviewWorldBounds _worldBounds )
        {
            float worldWidth = Mathf.Max( 1.0f, _worldBounds.maxX - _worldBounds.minX );
            float worldHeight = Mathf.Max( 1.0f, _worldBounds.maxY - _worldBounds.minY );
            float scaleX = _contentRect.width / worldWidth;
            float scaleY = _contentRect.height / worldHeight;
            float pixelPerUnit = Mathf.Min( scaleX, scaleY );
            return pixelPerUnit;
        }

        ///<summary>
        /// 프리뷰 경계 사각형 계산
        ///</summary>
        private static Rect BuildMappedBoundsRect( Rect _contentRect, CPreviewWorldBounds _worldBounds, float _pixelPerUnit )
        {
            float worldWidth = Mathf.Max( 1.0f, _worldBounds.maxX - _worldBounds.minX );
            float worldHeight = Mathf.Max( 1.0f, _worldBounds.maxY - _worldBounds.minY );
            float mappedWidth = worldWidth * _pixelPerUnit;
            float mappedHeight = worldHeight * _pixelPerUnit;
            float offsetX = _contentRect.x + ( _contentRect.width - mappedWidth ) * 0.5f;
            float offsetY = _contentRect.y + ( _contentRect.height - mappedHeight ) * 0.5f;
            Rect result = new Rect( offsetX, offsetY, mappedWidth, mappedHeight );
            return result;
        }

        ///<summary>
        /// 월드 좌표의 프리뷰 위치 변환
        ///</summary>
        private static Vector2 ConvertWorldToPreviewPosition( Vector2 _worldPosition, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            float normalizedX = ( _worldPosition.x - _worldBounds.minX ) * _pixelPerUnit;
            float normalizedY = ( _worldBounds.maxY - _worldPosition.y ) * _pixelPerUnit;
            Vector2 result = new Vector2( _mappedBoundsRect.x + normalizedX, _mappedBoundsRect.y + normalizedY );
            return result;
        }

        ///<summary>
        /// 월드 사각형의 프리뷰 사각형 계산
        ///</summary>
        private static Rect BuildMappedBoundsRectFromWorldRect( Vector2 _worldCenter, Vector2 _worldSize, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            Vector2 minWorldPosition = _worldCenter - _worldSize * 0.5f;
            Vector2 maxWorldPosition = _worldCenter + _worldSize * 0.5f;
            Vector2 topLeft = ConvertWorldToPreviewPosition( new Vector2( minWorldPosition.x, maxWorldPosition.y ), _worldBounds, _mappedBoundsRect, _pixelPerUnit );
            Vector2 bottomRight = ConvertWorldToPreviewPosition( new Vector2( maxWorldPosition.x, minWorldPosition.y ), _worldBounds, _mappedBoundsRect, _pixelPerUnit );
            Rect result = Rect.MinMaxRect( topLeft.x, topLeft.y, bottomRight.x, bottomRight.y );
            return result;
        }

        ///<summary>
        /// 즉발 스킬 범위 데이터 생성
        ///</summary>
        private static CSkillRangePreviewData BuildInstantRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty areaOffsetProperty = _activeEffectSerializedObject.FindProperty( "areaOffset" );
            SerializedProperty areaRadiusProperty = _activeEffectSerializedObject.FindProperty( "areaRadius" );

            if ( areaOffsetProperty == null || areaRadiusProperty == null )
            {
                return previewData;
            }

            Vector2 offset = areaOffsetProperty.vector2Value;
            float radius = Mathf.Max( 0.0f, areaRadiusProperty.floatValue );
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.CIRCLE;
            previewData.offset = offset;
            previewData.radius = radius;
            previewData.title = "Instant Circle";
            previewData.detail = $"Offset {offset}, Radius {radius:0.##}";
            return previewData;
        }

        ///<summary>
        /// 설치 스킬 범위 데이터 생성
        ///</summary>
        private static CSkillRangePreviewData BuildPlaceRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty placementOffsetProperty = _activeEffectSerializedObject.FindProperty( "placementOffset" );
            SerializedProperty areaRadiusProperty = _activeEffectSerializedObject.FindProperty( "areaRadius" );
            SerializedProperty durationSecondsProperty = _activeEffectSerializedObject.FindProperty( "durationSeconds" );
            SerializedProperty tickIntervalSecondsProperty = _activeEffectSerializedObject.FindProperty( "tickIntervalSeconds" );

            if ( placementOffsetProperty == null || areaRadiusProperty == null )
            {
                return previewData;
            }

            Vector2 offset = placementOffsetProperty.vector2Value;
            float radius = Mathf.Max( 0.0f, areaRadiusProperty.floatValue );
            float durationSeconds = durationSecondsProperty != null ? Mathf.Max( 0.0f, durationSecondsProperty.floatValue ) : 0.0f;
            float tickIntervalSeconds = tickIntervalSecondsProperty != null ? Mathf.Max( 0.0f, tickIntervalSecondsProperty.floatValue ) : 0.0f;
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.CIRCLE;
            previewData.offset = offset;
            previewData.radius = radius;
            previewData.title = "Placed Circle";
            previewData.detail = $"Offset {offset}, Radius {radius:0.##}, Duration {durationSeconds:0.##}s, Tick {tickIntervalSeconds:0.##}s";
            return previewData;
        }

        ///<summary>
        /// 발사체 스킬 범위 데이터 생성
        ///</summary>
        private static CSkillRangePreviewData BuildProjectileRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty spawnOffsetProperty = _activeEffectSerializedObject.FindProperty( "spawnOffset" );
            SerializedProperty collisionRadiusProperty = _activeEffectSerializedObject.FindProperty( "collisionRadius" );
            SerializedProperty travelDistanceProperty = _activeEffectSerializedObject.FindProperty( "travelDistance" );
            SerializedProperty travelSpeedProperty = _activeEffectSerializedObject.FindProperty( "travelSpeed" );
            SerializedProperty destroyOnFirstHitProperty = _activeEffectSerializedObject.FindProperty( "destroyOnFirstHit" );

            if ( spawnOffsetProperty == null || collisionRadiusProperty == null || travelDistanceProperty == null )
            {
                return previewData;
            }

            Vector2 spawnOffset = spawnOffsetProperty.vector2Value;
            float collisionRadius = Mathf.Max( 0.0f, collisionRadiusProperty.floatValue );
            float travelDistance = Mathf.Max( 0.0f, travelDistanceProperty.floatValue );
            float travelSpeed = travelSpeedProperty != null ? Mathf.Max( 0.0f, travelSpeedProperty.floatValue ) : 0.0f;
            bool destroyOnFirstHit = destroyOnFirstHitProperty == null || destroyOnFirstHitProperty.boolValue;
            Vector2 impactOffset = spawnOffset + new Vector2( travelDistance, 0.0f );
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.CIRCLE;
            previewData.offset = impactOffset;
            previewData.radius = collisionRadius;
            previewData.title = destroyOnFirstHit ? "Projectile Impact" : "Piercing Projectile Impact";
            previewData.detail = $"Spawn {spawnOffset}, Travel {travelDistance:0.##}, Radius {collisionRadius:0.##}, Speed {travelSpeed:0.##}, Mode {( destroyOnFirstHit ? "Stop On Hit" : "Piercing" )}";
            return previewData;
        }

        ///<summary>
        /// 액티브 스킬 타입 라벨 문자열 반환
        ///</summary>
        private static string ResolveActiveSkillTypeLabel( eActiveSkillType _activeSkillType )
        {
            string sourceText = _activeSkillType.ToString();
            string result = ObjectNames.NicifyVariableName( sourceText );
            return result;
        }

        ///<summary>
        /// 분신 스킬 범위 데이터 생성
        ///</summary>
        private static CSkillRangePreviewData BuildCloneRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty durationSecondsProperty = _activeEffectSerializedObject.FindProperty( "durationSeconds" );
            SerializedProperty followDelaySecondsProperty = _activeEffectSerializedObject.FindProperty( "followDelaySeconds" );
            SerializedProperty replayOffsetProperty = _activeEffectSerializedObject.FindProperty( "replayOffset" );
            SerializedProperty previewRadiusProperty = _activeEffectSerializedObject.FindProperty( "previewRadius" );
            Vector3 replayOffset = replayOffsetProperty != null ? replayOffsetProperty.vector3Value : Vector3.zero;
            float previewRadius = previewRadiusProperty != null ? Mathf.Max( 0.0f, previewRadiusProperty.floatValue ) : 0.0f;
            float durationSeconds = durationSecondsProperty != null ? Mathf.Max( 0.0f, durationSecondsProperty.floatValue ) : 0.0f;
            float followDelaySeconds = followDelaySecondsProperty != null ? Mathf.Max( 0.0f, followDelaySecondsProperty.floatValue ) : 0.0f;
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.CIRCLE;
            previewData.offset = new Vector2( replayOffset.x, replayOffset.y );
            previewData.radius = previewRadius;
            previewData.title = "Replay Clone";
            previewData.detail = $"Offset {previewData.offset}, Duration {durationSeconds:0.##}s, Delay {followDelaySeconds:0.##}s";
            return previewData;
        }

        ///<summary>
        /// 페이즈 스트라이크 범위 데이터 생성
        ///</summary>
        private static CSkillRangePreviewData BuildPhaseStrikeRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty hitCountProperty = _activeEffectSerializedObject.FindProperty( "hitCount" );
            SerializedProperty hitIntervalSecondsProperty = _activeEffectSerializedObject.FindProperty( "hitIntervalSeconds" );
            SerializedProperty damageMultiplierProperty = _activeEffectSerializedObject.FindProperty( "damageMultiplier" );
            int hitCount = hitCountProperty != null ? Mathf.Max( 1, hitCountProperty.intValue ) : 1;
            float hitIntervalSeconds = hitIntervalSecondsProperty != null ? Mathf.Max( 0.01f, hitIntervalSecondsProperty.floatValue ) : 0.01f;
            float damageMultiplier = damageMultiplierProperty != null ? Mathf.Max( 0.0f, damageMultiplierProperty.floatValue ) : 0.0f;
            float totalDurationSeconds = Mathf.Max( 0, hitCount - 1 ) * hitIntervalSeconds;
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.NONE;
            previewData.offset = Vector2.zero;
            previewData.radius = 0.0f;
            previewData.title = "Phase Strike";
            previewData.detail = $"Hits {hitCount}, Interval {hitIntervalSeconds:0.##}s, Duration {totalDurationSeconds:0.##}s, Damage {damageMultiplier * 100.0f:0.##}%";
            return previewData;
        }

        private static CSkillRangePreviewData BuildSkyfallStrikeRangePreviewData( SerializedObject _activeEffectSerializedObject )
        {
            CSkillRangePreviewData previewData = new CSkillRangePreviewData();
            SerializedProperty launchDistanceProperty = _activeEffectSerializedObject.FindProperty( "launchDistance" );
            SerializedProperty launchHeightProperty = _activeEffectSerializedObject.FindProperty( "launchHeight" );
            SerializedProperty landingDistanceProperty = _activeEffectSerializedObject.FindProperty( "landingDistance" );
            SerializedProperty areaRadiusProperty = _activeEffectSerializedObject.FindProperty( "areaRadius" );
            SerializedProperty launchDurationProperty = _activeEffectSerializedObject.FindProperty( "launchDurationSeconds" );
            SerializedProperty plungeDurationProperty = _activeEffectSerializedObject.FindProperty( "plungeDurationSeconds" );

            if ( launchDistanceProperty == null || launchHeightProperty == null || landingDistanceProperty == null || areaRadiusProperty == null )
            {
                return previewData;
            }

            float launchDistance = Mathf.Max( 0.0f, launchDistanceProperty.floatValue );
            float launchHeight = Mathf.Max( 0.0f, launchHeightProperty.floatValue );
            float landingDistance = Mathf.Max( launchDistance, landingDistanceProperty.floatValue );
            float radius = Mathf.Max( 0.0f, areaRadiusProperty.floatValue );
            float launchDuration = launchDurationProperty != null ? Mathf.Max( 0.0f, launchDurationProperty.floatValue ) : 0.0f;
            float plungeDuration = plungeDurationProperty != null ? Mathf.Max( 0.0f, plungeDurationProperty.floatValue ) : 0.0f;
            previewData.isValid = true;
            previewData.shapeType = eSkillRangePreviewShape.CIRCLE;
            previewData.offset = new Vector2( landingDistance, 0.0f );
            previewData.radius = radius;
            previewData.trajectoryPointArray = new[]
            {
                Vector2.zero,
                new Vector2( launchDistance, launchHeight ),
                new Vector2( landingDistance, 0.0f )
            };
            previewData.title = "Skyfall Trajectory";
            previewData.detail = $"Apex ({launchDistance:0.##}, {launchHeight:0.##}) in {launchDuration:0.##}s, plunge to ({landingDistance:0.##}, 0) in {plungeDuration:0.##}s, impact radius {radius:0.##}";
            return previewData;
        }

        private static void DrawTrajectoryPreview( CSkillRangePreviewData _previewData, CPreviewWorldBounds _worldBounds, Rect _mappedBoundsRect, float _pixelPerUnit )
        {
            if ( _previewData.trajectoryPointArray == null || _previewData.trajectoryPointArray.Length < 2 )
            {
                return;
            }

            Handles.color = PreviewLinkLineColor;
            Vector2 previousPoint = ConvertWorldToPreviewPosition( _previewData.trajectoryPointArray[ 0 ], _worldBounds, _mappedBoundsRect, _pixelPerUnit );

            for ( int index = 1; index < _previewData.trajectoryPointArray.Length; index++ )
            {
                Vector2 currentPoint = ConvertWorldToPreviewPosition( _previewData.trajectoryPointArray[ index ], _worldBounds, _mappedBoundsRect, _pixelPerUnit );
                Handles.DrawAAPolyLine( 3.0f, previousPoint, currentPoint );
                previousPoint = currentPoint;
            }

            Vector2 apexPoint = ConvertWorldToPreviewPosition( _previewData.trajectoryPointArray[ 1 ], _worldBounds, _mappedBoundsRect, _pixelPerUnit );
            Handles.DrawSolidDisc( apexPoint, Vector3.forward, PreviewOwnerRadius * 0.75f );
        }
    }
}
