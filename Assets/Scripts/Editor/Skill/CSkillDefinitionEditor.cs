using TinyHero.Skill;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 스킬 정의 인스펙터 편집기
    ///</summary>
    [CustomEditor( typeof( CSkillDefinition ) )]
    public sealed class CSkillDefinitionEditor : UnityEditor.Editor
    {
        private const float PreviewHeight = 180.0f;

        private SerializedProperty skillIdProperty;
        private SerializedProperty skillNameProperty;
        private SerializedProperty skillIconProperty;
        private SerializedProperty skillTypeProperty;
        private SerializedProperty activeSkillTypeProperty;
        private SerializedProperty quickSlotIndexProperty;
        private SerializedProperty cooldownSecondsProperty;
        private SerializedProperty mpCostProperty;
        private SerializedProperty castLockDurationSecondsProperty;
        private SerializedProperty castAnimationProperty;
        private SerializedProperty castAnimationNameProperty;
        private SerializedProperty castAnimationSpeedProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty castVfxPrefabProperty;
        private SerializedProperty castVfxOffsetProperty;
        private SerializedProperty castVfxReturnDelayProperty;
        private SerializedProperty hitVfxPrefabProperty;
        private SerializedProperty hitVfxOffsetProperty;
        private SerializedProperty hitVfxReturnDelayProperty;
        private SerializedProperty projectileVfxPrefabProperty;
        private SerializedProperty projectileVfxOffsetProperty;
        private SerializedProperty projectileVfxReturnDelayProperty;
        private SerializedProperty loopVfxPrefabProperty;
        private SerializedProperty loopVfxOffsetProperty;
        private SerializedProperty loopVfxReturnDelayProperty;
        private SerializedProperty passiveStatBonusProperty;
        private SerializedProperty activeActionProperty;
        private SerializedProperty activeSkillEffectProperty;
        private SerializedProperty passiveSkillEffectListProperty;
        private SerializedProperty unlockConditionListProperty;

        private bool isSummaryFoldoutOpen = true;
        private bool isExecutionFoldoutOpen = true;
        private bool isRangePreviewFoldoutOpen = true;
        private bool isVfxFoldoutOpen;
        private bool isPassiveFoldoutOpen = true;
        private bool isUnlockFoldoutOpen = true;
        private bool isEffectInspectorFoldoutOpen;

        private UnityEditor.Editor cachedActiveEffectEditor;

        ///<summary>
        /// 인스펙터 초기화 처리
        ///</summary>
        private void OnEnable()
        {
            skillIdProperty = serializedObject.FindProperty( "skillId" );
            skillNameProperty = serializedObject.FindProperty( "skillName" );
            skillIconProperty = serializedObject.FindProperty( "skillIcon" );
            skillTypeProperty = serializedObject.FindProperty( "skillType" );
            activeSkillTypeProperty = serializedObject.FindProperty( "activeSkillType" );
            quickSlotIndexProperty = serializedObject.FindProperty( "quickSlotIndex" );
            cooldownSecondsProperty = serializedObject.FindProperty( "cooldownSeconds" );
            mpCostProperty = serializedObject.FindProperty( "mpCost" );
            castLockDurationSecondsProperty = serializedObject.FindProperty( "castLockDurationSeconds" );
            castAnimationProperty = serializedObject.FindProperty( "castAnimation" );
            castAnimationNameProperty = serializedObject.FindProperty( "castAnimationName" );
            castAnimationSpeedProperty = serializedObject.FindProperty( "castAnimationSpeed" );
            descriptionProperty = serializedObject.FindProperty( "description" );
            castVfxPrefabProperty = serializedObject.FindProperty( "castVfxPrefab" );
            castVfxOffsetProperty = serializedObject.FindProperty( "castVfxOffset" );
            castVfxReturnDelayProperty = serializedObject.FindProperty( "castVfxReturnDelay" );
            hitVfxPrefabProperty = serializedObject.FindProperty( "hitVfxPrefab" );
            hitVfxOffsetProperty = serializedObject.FindProperty( "hitVfxOffset" );
            hitVfxReturnDelayProperty = serializedObject.FindProperty( "hitVfxReturnDelay" );
            projectileVfxPrefabProperty = serializedObject.FindProperty( "projectileVfxPrefab" );
            projectileVfxOffsetProperty = serializedObject.FindProperty( "projectileVfxOffset" );
            projectileVfxReturnDelayProperty = serializedObject.FindProperty( "projectileVfxReturnDelay" );
            loopVfxPrefabProperty = serializedObject.FindProperty( "loopVfxPrefab" );
            loopVfxOffsetProperty = serializedObject.FindProperty( "loopVfxOffset" );
            loopVfxReturnDelayProperty = serializedObject.FindProperty( "loopVfxReturnDelay" );
            passiveStatBonusProperty = serializedObject.FindProperty( "passiveStatBonus" );
            activeActionProperty = serializedObject.FindProperty( "activeAction" );
            activeSkillEffectProperty = serializedObject.FindProperty( "activeSkillEffect" );
            passiveSkillEffectListProperty = serializedObject.FindProperty( "passiveSkillEffectList" );
            unlockConditionListProperty = serializedObject.FindProperty( "unlockConditionList" );
        }

        ///<summary>
        /// 인스펙터 비활성화 정리
        ///</summary>
        private void OnDisable()
        {
            if ( cachedActiveEffectEditor != null )
            {
                DestroyImmediate( cachedActiveEffectEditor );
                cachedActiveEffectEditor = null;
            }
        }

        ///<summary>
        /// 커스텀 인스펙터 렌더링
        ///</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeaderSection();
            DrawSummarySection();
            DrawExecutionSection();
            DrawRangePreviewSection();
            DrawPassiveSection();
            DrawVfxSection();
            DrawUnlockSection();
            DrawEffectInspectorSection();

            serializedObject.ApplyModifiedProperties();
        }

        ///<summary>
        /// 헤더 카드 렌더링
        ///</summary>
        private void DrawHeaderSection()
        {
            CSkillDefinition skillDefinition = target as CSkillDefinition;
            Rect contentRect;
            Sprite skillIcon = skillDefinition != null ? skillDefinition.GetSkillIcon() : null;
            string skillName = skillDefinition != null ? skillDefinition.GetSkillName() : "Skill";
            string skillId = skillDefinition != null ? skillDefinition.GetSkillId() : string.Empty;
            string typeLabel = CSkillEditorPreviewUtility.BuildTypeSummaryText( skillTypeProperty, activeSkillTypeProperty );
            Rect iconRect;
            Rect titleRect;
            Rect subTitleRect;
            Rect infoRect;
            Color headerColor = new Color( 0.18f, 0.22f, 0.28f, 0.45f );

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            contentRect = EditorGUILayout.GetControlRect( false, 68.0f );
            iconRect = new Rect( contentRect.x + 8.0f, contentRect.y + 6.0f, 56.0f, 56.0f );
            titleRect = new Rect( iconRect.xMax + 10.0f, contentRect.y + 6.0f, contentRect.width - 76.0f, 22.0f );
            subTitleRect = new Rect( titleRect.x, titleRect.yMax + 2.0f, titleRect.width, 18.0f );
            infoRect = new Rect( titleRect.x, subTitleRect.yMax + 4.0f, titleRect.width, 18.0f );

            EditorGUI.DrawRect( contentRect, headerColor );

            if ( skillIcon != null )
            {
                Texture iconTexture = AssetPreview.GetAssetPreview( skillIcon );

                if ( iconTexture == null )
                {
                    iconTexture = AssetPreview.GetMiniThumbnail( skillIcon );
                }

                if ( iconTexture != null )
                {
                    GUI.DrawTexture( iconRect, iconTexture, ScaleMode.ScaleToFit, true );
                }
            }
            else
            {
                EditorGUI.DrawRect( iconRect, new Color( 0.0f, 0.0f, 0.0f, 0.18f ) );
                EditorGUI.LabelField( iconRect, "NO ICON", EditorStyles.centeredGreyMiniLabel );
            }

            EditorGUI.LabelField( titleRect, skillName, EditorStyles.boldLabel );
            EditorGUI.LabelField( subTitleRect, skillId, EditorStyles.miniLabel );
            EditorGUI.LabelField( infoRect, typeLabel, EditorStyles.wordWrappedMiniLabel );
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space( 4.0f );
        }

        ///<summary>
        /// 기본 정보 섹션 렌더링
        ///</summary>
        private void DrawSummarySection()
        {
            isSummaryFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isSummaryFoldoutOpen, "기본 정보" );

            if ( isSummaryFoldoutOpen )
            {
                EditorGUILayout.PropertyField( skillIdProperty );
                EditorGUILayout.PropertyField( skillNameProperty );
                EditorGUILayout.PropertyField( skillIconProperty );
                EditorGUILayout.PropertyField( skillTypeProperty );

                using ( new EditorGUI.DisabledScope( true ) )
                {
                    EditorGUILayout.PropertyField( activeSkillTypeProperty );
                }

                EditorGUILayout.PropertyField( descriptionProperty );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// 사용 설정 섹션 렌더링
        ///</summary>
        private void DrawExecutionSection()
        {
            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            isExecutionFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isExecutionFoldoutOpen, "사용 설정" );

            if ( isExecutionFoldoutOpen )
            {
                EditorGUILayout.PropertyField( quickSlotIndexProperty );
                EditorGUILayout.PropertyField( cooldownSecondsProperty );
                EditorGUILayout.PropertyField( mpCostProperty );
                DrawCastPropertyBlock();
                EditorGUILayout.PropertyField( activeSkillEffectProperty );
                EditorGUILayout.PropertyField( activeActionProperty );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// 시전 설정 블록 렌더링
        ///</summary>
        private void DrawCastPropertyBlock()
        {
            string resolvedAnimationName = BuildResolvedCastAnimationName();

            EditorGUILayout.Space( 2.0f );
            EditorGUILayout.LabelField( "시전 설정", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( castLockDurationSecondsProperty );
            EditorGUILayout.PropertyField( castAnimationProperty );
            EditorGUILayout.PropertyField( castAnimationSpeedProperty );

            if ( ( ePlayerSkillCastAnimation ) castAnimationProperty.enumValueIndex == ePlayerSkillCastAnimation.CUSTOM )
            {
                EditorGUILayout.PropertyField( castAnimationNameProperty, new GUIContent( "Cast Animation Name" ) );
                return;
            }

            using ( new EditorGUI.DisabledScope( true ) )
            {
                EditorGUILayout.TextField( "Cast Animation Name", resolvedAnimationName );
            }
        }

        ///<summary>
        /// 시전 애니메이션 실사용 이름 계산
        ///</summary>
        private string BuildResolvedCastAnimationName()
        {
            ePlayerSkillCastAnimation castAnimation = ( ePlayerSkillCastAnimation ) castAnimationProperty.enumValueIndex;

            switch ( castAnimation )
            {
                case ePlayerSkillCastAnimation.ATTACK:
                    return "Attack";

                case ePlayerSkillCastAnimation.IDLE:
                    return "Idle";

                case ePlayerSkillCastAnimation.MOVE:
                    return "Move";

                case ePlayerSkillCastAnimation.HIT:
                    return "Hit";

                case ePlayerSkillCastAnimation.DIE:
                    return "Die";

                case ePlayerSkillCastAnimation.CUSTOM:
                default:
                    return string.IsNullOrWhiteSpace( castAnimationNameProperty.stringValue ) ? "Attack" : castAnimationNameProperty.stringValue.Trim();
            }
        }

        ///<summary>
        /// 범위 미리보기 섹션 렌더링
        ///</summary>
        private void DrawRangePreviewSection()
        {
            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            isRangePreviewFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isRangePreviewFoldoutOpen, "공격 범위 미리보기" );

            if ( isRangePreviewFoldoutOpen )
            {
                CSkillRangePreviewData previewData = CSkillEditorPreviewUtility.BuildRangePreviewData( activeSkillEffectProperty.objectReferenceValue );

                if ( previewData.isValid == false )
                {
                    EditorGUILayout.HelpBox( "현재 액티브 효과는 범위 프리뷰를 제공하지 않습니다.", MessageType.Info );
                }
                else
                {
                    EditorGUILayout.LabelField( previewData.title, EditorStyles.boldLabel );
                    EditorGUILayout.LabelField( previewData.detail, EditorStyles.wordWrappedMiniLabel );
                    GUILayout.Space( 4.0f );

                    Rect previewRect = GUILayoutUtility.GetRect( 10.0f, PreviewHeight, GUILayout.ExpandWidth( true ) );
                    CSkillEditorPreviewUtility.DrawRangePreviewCanvas( previewRect, previewData );
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// 패시브 설정 섹션 렌더링
        ///</summary>
        private void DrawPassiveSection()
        {
            if ( CSkillEditorPreviewUtility.IsPassiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            isPassiveFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isPassiveFoldoutOpen, "패시브 설정" );

            if ( isPassiveFoldoutOpen )
            {
                EditorGUILayout.PropertyField( passiveStatBonusProperty, true );
                EditorGUILayout.PropertyField( passiveSkillEffectListProperty, true );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// VFX 설정 섹션 렌더링
        ///</summary>
        private void DrawVfxSection()
        {
            isVfxFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isVfxFoldoutOpen, "VFX 설정" );

            if ( isVfxFoldoutOpen )
            {
                DrawVfxBlock( "Cast VFX", castVfxPrefabProperty, castVfxOffsetProperty, castVfxReturnDelayProperty );
                DrawVfxBlock( "Hit VFX", hitVfxPrefabProperty, hitVfxOffsetProperty, hitVfxReturnDelayProperty );
                DrawVfxBlock( "Projectile VFX", projectileVfxPrefabProperty, projectileVfxOffsetProperty, projectileVfxReturnDelayProperty );
                DrawVfxBlock( "Loop VFX", loopVfxPrefabProperty, loopVfxOffsetProperty, loopVfxReturnDelayProperty );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// 해금 조건 섹션 렌더링
        ///</summary>
        private void DrawUnlockSection()
        {
            isUnlockFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isUnlockFoldoutOpen, "해금 조건" );

            if ( isUnlockFoldoutOpen )
            {
                EditorGUILayout.PropertyField( unlockConditionListProperty, true );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// 액티브 효과 세부 섹션 렌더링
        ///</summary>
        private void DrawEffectInspectorSection()
        {
            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            Object activeEffectObject = activeSkillEffectProperty.objectReferenceValue;

            if ( activeEffectObject == null )
            {
                return;
            }

            isEffectInspectorFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup( isEffectInspectorFoldoutOpen, "액티브 효과 세부 편집" );

            if ( isEffectInspectorFoldoutOpen )
            {
                UnityEditor.Editor.CreateCachedEditor( activeEffectObject, null, ref cachedActiveEffectEditor );

                if ( cachedActiveEffectEditor != null )
                {
                    cachedActiveEffectEditor.OnInspectorGUI();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        ///<summary>
        /// VFX 블록 렌더링
        ///</summary>
        private void DrawVfxBlock( string _title, SerializedProperty _prefabProperty, SerializedProperty _offsetProperty, SerializedProperty _returnDelayProperty )
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( _title, EditorStyles.boldLabel );
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField( _prefabProperty );

            if ( EditorGUI.EndChangeCheck() )
            {
                GameObject assignedPrefab = _prefabProperty.objectReferenceValue as GameObject;
                CSkillEditorVfxSortingUtility.ApplySkillEffectSortingLayer( assignedPrefab );
            }

            EditorGUILayout.PropertyField( _offsetProperty );
            EditorGUILayout.PropertyField( _returnDelayProperty );
            EditorGUILayout.EndVertical();
        }
    }
}
