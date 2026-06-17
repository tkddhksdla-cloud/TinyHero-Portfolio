using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴 스킬 범위 시각화 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class CMapToolSkillRangeVisualizer : MonoBehaviour
    {
        private const int CirclePointCount = 49;
        private const float DefaultLineWidth = 0.08f;
        private const float RainbowCycleSpeed = 0.7f;
        private const float RainbowHueOffset = 0.12f;
        private const int SortingOrder = 500;

        [SerializeField] private LineRenderer targetLineRenderer;
        [SerializeField] private float lineWidth = DefaultLineWidth;

        private float visibleUntilTime = -1.0f;
        private bool isFollowingOwner;
        private Transform followOwnerTransform;
        private CActiveSkillEffectBase followSkillEffect;
        private CSkillToolRangePreviewData currentPreviewData;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        private void Awake()
        {
            EnsureLineRendererExists();
            SetVisualizerVisible( false );
        }

        ///<summary>
        /// 프레임 상태 갱신
        ///</summary>
        private void LateUpdate()
        {
            if ( isFollowingOwner )
            {
                RefreshFollowingPreview();
            }

            if ( targetLineRenderer == null || targetLineRenderer.enabled == false )
            {
                return;
            }

            if ( visibleUntilTime >= 0.0f && Time.time > visibleUntilTime )
            {
                HidePreview();
                return;
            }

            UpdateRainbowGradient();
        }

        ///<summary>
        /// 추적형 미리보기 표시
        ///</summary>
        public void ShowFollowingPreview( CActiveSkillEffectBase _skillEffect, Transform _ownerTransform )
        {
            if ( _skillEffect == null || _ownerTransform == null )
            {
                HidePreview();
                return;
            }

            isFollowingOwner = true;
            followOwnerTransform = _ownerTransform;
            followSkillEffect = _skillEffect;
            visibleUntilTime = -1.0f;
            RefreshFollowingPreview();
        }

        ///<summary>
        /// 고정형 미리보기 표시
        ///</summary>
        public void ShowFixedPreview( CSkillToolRangePreviewData _previewData, float _durationSeconds )
        {
            if ( _previewData.isValid == false )
            {
                HidePreview();
                return;
            }

            isFollowingOwner = false;
            followOwnerTransform = null;
            followSkillEffect = null;
            currentPreviewData = _previewData;
            visibleUntilTime = _durationSeconds > 0.0f ? Time.time + _durationSeconds : -1.0f;
            ApplyPreviewData();
        }

        ///<summary>
        /// 미리보기 숨김
        ///</summary>
        public void HidePreview()
        {
            isFollowingOwner = false;
            followOwnerTransform = null;
            followSkillEffect = null;
            visibleUntilTime = -1.0f;
            currentPreviewData = default;
            SetVisualizerVisible( false );
        }

        ///<summary>
        /// 라인 렌더러 존재 보장
        ///</summary>
        private void EnsureLineRendererExists()
        {
            if ( targetLineRenderer == null )
            {
                LineRenderer resolvedLineRenderer = GetComponent<LineRenderer>();
                targetLineRenderer = resolvedLineRenderer;
            }

            if ( targetLineRenderer == null )
            {
                LineRenderer createdLineRenderer = gameObject.AddComponent<LineRenderer>();
                targetLineRenderer = createdLineRenderer;
            }

            ConfigureLineRenderer();
        }

        ///<summary>
        /// 라인 렌더러 구성
        ///</summary>
        private void ConfigureLineRenderer()
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

            targetLineRenderer.useWorldSpace = true;
            targetLineRenderer.loop = false;
            targetLineRenderer.positionCount = CirclePointCount;
            targetLineRenderer.startWidth = lineWidth;
            targetLineRenderer.endWidth = lineWidth;
            targetLineRenderer.sortingOrder = SortingOrder;
            targetLineRenderer.textureMode = LineTextureMode.Stretch;
            targetLineRenderer.numCapVertices = 4;
            targetLineRenderer.numCornerVertices = 4;
        }

        ///<summary>
        /// 추적형 미리보기 갱신
        ///</summary>
        private void RefreshFollowingPreview()
        {
            if ( followSkillEffect == null || followOwnerTransform == null )
            {
                HidePreview();
                return;
            }

            bool hasPreview = followSkillEffect.TryGetToolRangePreviewData( followOwnerTransform, out CSkillToolRangePreviewData previewData );

            if ( hasPreview == false )
            {
                HidePreview();
                return;
            }

            currentPreviewData = previewData;
            ApplyPreviewData();
        }

        ///<summary>
        /// 현재 미리보기 데이터 적용
        ///</summary>
        private void ApplyPreviewData()
        {
            if ( currentPreviewData.isValid == false )
            {
                SetVisualizerVisible( false );
                return;
            }

            if ( currentPreviewData.shapeType == eSkillToolRangePreviewShape.CIRCLE )
            {
                UpdateCirclePositions();
                SetVisualizerVisible( true );
                UpdateRainbowGradient();
                return;
            }

            SetVisualizerVisible( false );
        }

        ///<summary>
        /// 원형 포인트 위치 갱신
        ///</summary>
        private void UpdateCirclePositions()
        {
            int pointCount = CirclePointCount;
            float radius = Mathf.Max( 0.05f, currentPreviewData.radius );
            Vector3 centerPosition = currentPreviewData.worldCenterPosition;

            for ( int index = 0; index < pointCount; index++ )
            {
                float normalizedValue = ( float ) index / ( pointCount - 1 );
                float angleRadians = normalizedValue * Mathf.PI * 2.0f;
                float xPosition = Mathf.Cos( angleRadians ) * radius;
                float yPosition = Mathf.Sin( angleRadians ) * radius;
                Vector3 pointPosition = new Vector3( centerPosition.x + xPosition, centerPosition.y + yPosition, centerPosition.z );
                targetLineRenderer.SetPosition( index, pointPosition );
            }
        }

        ///<summary>
        /// 무지개 색상 그라데이션 갱신
        ///</summary>
        private void UpdateRainbowGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[ 7 ];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[ 2 ];
            float baseHue = Mathf.Repeat( Time.time * RainbowCycleSpeed, 1.0f );

            for ( int index = 0; index < colorKeys.Length; index++ )
            {
                float hue = Mathf.Repeat( baseHue + ( RainbowHueOffset * index ), 1.0f );
                Color rainbowColor = Color.HSVToRGB( hue, 0.95f, 1.0f );
                float timeValue = ( float ) index / ( colorKeys.Length - 1 );
                colorKeys[ index ] = new GradientColorKey( rainbowColor, timeValue );
            }

            alphaKeys[ 0 ] = new GradientAlphaKey( 1.0f, 0.0f );
            alphaKeys[ 1 ] = new GradientAlphaKey( 1.0f, 1.0f );
            gradient.SetKeys( colorKeys, alphaKeys );
            targetLineRenderer.colorGradient = gradient;
        }

        ///<summary>
        /// 시각화 표시 상태 적용
        ///</summary>
        private void SetVisualizerVisible( bool _isVisible )
        {
            if ( targetLineRenderer == null )
            {
                return;
            }

            targetLineRenderer.enabled = _isVisible;
        }
    }
}
