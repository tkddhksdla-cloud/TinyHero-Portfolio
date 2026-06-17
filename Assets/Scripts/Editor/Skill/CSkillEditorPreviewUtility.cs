using TinyHero.Skill;
using UnityEditor;
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
        public string title;
        public string detail;
    }

    ///<summary>
    /// 스킬 편집기 미리보기 유틸리티
    ///</summary>
    public static class CSkillEditorPreviewUtility
    {
        private const float PreviewPadding = 12.0f;
        private const float PreviewAxisThickness = 2.0f;
        private const float PreviewOwnerRadius = 6.0f;
        private const float PreviewMinRangeSize = 8.0f;

        ///<summary>
        /// 스킬 타입 요약 문구 생성
        ///</summary>
        public static string BuildTypeSummaryText( SerializedProperty _skillTypeProperty, SerializedProperty _activeSkillTypeProperty )
        {
            string skillTypeLabel = _skillTypeProperty.enumDisplayNames[ _skillTypeProperty.enumValueIndex ];
            string activeTypeLabel = _activeSkillTypeProperty.enumDisplayNames[ _activeSkillTypeProperty.enumValueIndex ];
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

            if ( effectTypeName == nameof( CBuffActiveSkillEffect ) )
            {
                previewData.isValid = true;
                previewData.shapeType = eSkillRangePreviewShape.NONE;
                previewData.title = "Self Target";
                previewData.detail = "Buff skills are applied to the caster instead of an attack area.";
                return previewData;
            }

            return previewData;
        }

        ///<summary>
        /// 범위 미리보기 캔버스 렌더링
        ///</summary>
        public static void DrawRangePreviewCanvas( Rect _previewRect, CSkillRangePreviewData _previewData )
        {
            EditorGUI.DrawRect( _previewRect, new Color( 0.11f, 0.12f, 0.14f, 1.0f ) );

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
            float maxExtent = Mathf.Max(
                Mathf.Abs( _previewData.offset.x ) + _previewData.radius,
                Mathf.Abs( _previewData.offset.y ) + _previewData.radius,
                1.0f
            );
            float scaleX = contentRect.width * 0.5f / maxExtent;
            float scaleY = contentRect.height * 0.5f / maxExtent;
            float pixelPerUnit = Mathf.Min( scaleX, scaleY );
            Vector2 ownerPosition = contentRect.center;
            Vector2 targetCenter = ownerPosition + new Vector2( _previewData.offset.x * pixelPerUnit, -_previewData.offset.y * pixelPerUnit );
            float radius = Mathf.Max( PreviewMinRangeSize, _previewData.radius * pixelPerUnit );

            EditorGUI.DrawRect(
                new Rect( contentRect.x, ownerPosition.y - PreviewAxisThickness * 0.5f, contentRect.width, PreviewAxisThickness ),
                new Color( 1.0f, 1.0f, 1.0f, 0.08f )
            );
            EditorGUI.DrawRect(
                new Rect( ownerPosition.x - PreviewAxisThickness * 0.5f, contentRect.y, PreviewAxisThickness, contentRect.height ),
                new Color( 1.0f, 1.0f, 1.0f, 0.08f )
            );
            EditorGUI.DrawRect(
                new Rect( ownerPosition.x - PreviewOwnerRadius, ownerPosition.y - PreviewOwnerRadius, PreviewOwnerRadius * 2.0f, PreviewOwnerRadius * 2.0f ),
                new Color( 0.97f, 0.84f, 0.36f, 1.0f )
            );
            Handles.BeginGUI();
            Handles.color = new Color( 0.30f, 0.78f, 1.0f, 0.28f );
            Handles.DrawSolidDisc( targetCenter, Vector3.forward, radius );
            Handles.color = new Color( 0.30f, 0.78f, 1.0f, 1.0f );
            Handles.DrawWireDisc( targetCenter, Vector3.forward, radius );
            Handles.color = new Color( 1.0f, 1.0f, 1.0f, 0.45f );
            Handles.DrawLine( ownerPosition, targetCenter );
            Handles.EndGUI();
            EditorGUI.LabelField(
                new Rect( _previewRect.x + 8.0f, _previewRect.y + 6.0f, _previewRect.width - 16.0f, 18.0f ),
                "Yellow: player origin / Blue: attack area",
                EditorStyles.miniLabel
            );
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
    }
}
