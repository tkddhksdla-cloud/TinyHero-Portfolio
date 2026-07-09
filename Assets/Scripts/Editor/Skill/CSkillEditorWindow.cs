using System;
using System.Collections.Generic;
using TinyHero.Skill;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Skill.Editor
{
    ///<summary>
    /// 스킬 에셋 목록 항목 정보
    ///</summary>
    [Serializable]
    public sealed class CSkillDefinitionListItem
    {
        public string assetPath;
        public string displayName;
        public string skillId;
        public eSkillType skillType;
    }

    ///<summary>
    /// 스킬 편집기 윈도우
    ///</summary>
    public sealed class CSkillEditorWindow : EditorWindow
    {
        private const float SkillListWidth = 280.0f;
        private const float PreviewPanelWidth = 340.0f;
        private const float PreviewHeight = 240.0f;
        private const float ListItemHeight = 46.0f;

        [SerializeField] private List<CSkillDefinitionListItem> skillDefinitionItemList = new List<CSkillDefinitionListItem>();
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string selectedSkillAssetPath = string.Empty;

        private Vector2 skillListScrollPosition;
        private Vector2 editorScrollPosition;
        private Vector2 previewScrollPosition;
        private CSkillDefinition selectedSkillDefinition;
        private SerializedObject selectedSkillSerializedObject;
        private UnityEditor.Editor cachedActiveEffectEditor;
        private string lastLoadedSkillAssetPath = string.Empty;

        private SerializedProperty skillIdProperty;
        private SerializedProperty skillNameProperty;
        private SerializedProperty skillIconProperty;
        private SerializedProperty skillTypeProperty;
        private SerializedProperty activeSkillTypeProperty;
        private SerializedProperty quickSlotIndexProperty;
        private SerializedProperty cooldownSecondsProperty;
        private SerializedProperty mpCostProperty;
        private SerializedProperty learnSpCostProperty;
        private SerializedProperty levelUpSpCostProperty;
        private SerializedProperty maxSkillLevelProperty;
        private SerializedProperty cooldownReductionPerLevelProperty;
        private SerializedProperty damageMultiplierBonusPerLevelProperty;
        private SerializedProperty flatDamageBonusPerLevelProperty;
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
        private SerializedProperty castSfxClipNameProperty;
        private SerializedProperty hitSfxClipNameProperty;
        private SerializedProperty loopSfxClipNameProperty;
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

        ///<summary>
        /// 스킬 편집기 윈도우 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Skill Editor" )]
        private static void ShowWindow()
        {
            CSkillEditorWindow window = GetWindow<CSkillEditorWindow>();
            window.titleContent = new GUIContent( "Skill Editor" );
            window.minSize = new Vector2( 1320.0f, 760.0f );
            window.Show();
        }

        ///<summary>
        /// 윈도우 초기화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshSkillDefinitionItemList();
            RestoreSelectedSkillDefinition();
        }

        ///<summary>
        /// 윈도우 비활성화 정리
        ///</summary>
        private void OnDisable()
        {
            ReleaseCachedEditors();
        }

        ///<summary>
        /// 프로젝트 변경 반영
        ///</summary>
        private void OnProjectChange()
        {
            RefreshSkillDefinitionItemList();
            RestoreSelectedSkillDefinition();
            Repaint();
        }

        ///<summary>
        /// 윈도우 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawSkillListSection();
            DrawEditorSection();
            DrawPreviewSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 툴바 섹션 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Skill Editor", EditorStyles.boldLabel, GUILayout.Width( 120.0f ) );
            DrawSearchField();

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 100.0f ) ) )
            {
                RefreshSkillDefinitionItemList();
                RestoreSelectedSkillDefinition();
            }

            if ( GUILayout.Button( "Ping Asset", GUILayout.Width( 100.0f ) ) )
            {
                PingSelectedSkillDefinition();
            }

            if ( GUILayout.Button( "Open Inspector", GUILayout.Width( 120.0f ) ) )
            {
                OpenSelectedSkillDefinitionInInspector();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 검색 필드 렌더링
        ///</summary>
        private void DrawSearchField()
        {
            string newSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( newSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = newSearchText;
            }
        }

        ///<summary>
        /// 스킬 목록 섹션 렌더링
        ///</summary>
        private void DrawSkillListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( SkillListWidth ) );
            EditorGUILayout.LabelField( "Skill Definitions", EditorStyles.boldLabel );
            List<CSkillDefinitionListItem> filteredItemList = GetFilteredSkillDefinitionItemList();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredItemList.Count}개", MessageType.None );
            skillListScrollPosition = EditorGUILayout.BeginScrollView( skillListScrollPosition );

            for ( int index = 0; index < filteredItemList.Count; index++ )
            {
                CSkillDefinitionListItem skillDefinitionItem = filteredItemList[ index ];
                DrawSkillListItem( skillDefinitionItem );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 스킬 목록 항목 렌더링
        ///</summary>
        private void DrawSkillListItem( CSkillDefinitionListItem _skillDefinitionItem )
        {
            if ( _skillDefinitionItem == null )
            {
                return;
            }

            bool isSelected = string.Equals( selectedSkillAssetPath, _skillDefinitionItem.assetPath, StringComparison.Ordinal );
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string buttonLabel = $"[ {_skillDefinitionItem.skillType} ] {_skillDefinitionItem.displayName}\n{_skillDefinitionItem.skillId}";
            bool isClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( isClicked )
            {
                SelectSkillDefinitionByAssetPath( _skillDefinitionItem.assetPath );
            }

            if ( isSelected )
            {
                Rect itemRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect( itemRect, new Color( 0.2f, 0.5f, 0.85f, 0.18f ) );
            }
        }

        ///<summary>
        /// 편집 섹션 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField( "Skill Properties", EditorStyles.boldLabel );

            if ( EnsureSelectedSkillSerializedObject() == false )
            {
                EditorGUILayout.HelpBox( "편집할 CSkillDefinition 에셋을 선택하세요.", MessageType.Info );
                EditorGUILayout.EndVertical();
                return;
            }

            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            selectedSkillSerializedObject.Update();
            DrawHeaderCard();
            DrawSummaryPropertySection();
            DrawProgressionPropertySection();
            DrawExecutionPropertySection();
            DrawPassivePropertySection();
            DrawAudioPropertySection();
            DrawVfxPropertySection();
            DrawUnlockPropertySection();
            DrawActiveEffectPropertySection();

            if ( selectedSkillSerializedObject.ApplyModifiedProperties() )
            {
                MarkEditedAssetsDirty();
                Repaint();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 미리보기 섹션 렌더링
        ///</summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( PreviewPanelWidth ) );
            EditorGUILayout.LabelField( "Range Preview", EditorStyles.boldLabel );

            if ( EnsureSelectedSkillSerializedObject() == false )
            {
                EditorGUILayout.HelpBox( "선택된 스킬이 없습니다.", MessageType.Info );
                EditorGUILayout.EndVertical();
                return;
            }

            previewScrollPosition = EditorGUILayout.BeginScrollView( previewScrollPosition );
            DrawRangePreviewPanel();
            DrawPreviewSummaryPanel();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 헤더 카드 렌더링
        ///</summary>
        private void DrawHeaderCard()
        {
            Rect contentRect;
            Sprite skillIcon = selectedSkillDefinition != null ? selectedSkillDefinition.GetSkillIcon() : null;
            string skillName = selectedSkillDefinition != null ? selectedSkillDefinition.GetSkillName() : "Skill";
            string skillId = selectedSkillDefinition != null ? selectedSkillDefinition.GetSkillId() : string.Empty;
            eActiveSkillType activeSkillType = selectedSkillDefinition != null ? selectedSkillDefinition.GetActiveSkillType() : eActiveSkillType.NONE;
            string typeLabel = CSkillEditorPreviewUtility.BuildTypeSummaryText( skillTypeProperty, activeSkillType );
            Rect iconRect;
            Rect titleRect;
            Rect subTitleRect;
            Rect infoRect;
            Color headerColor = new Color( 0.18f, 0.22f, 0.28f, 0.45f );

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            contentRect = EditorGUILayout.GetControlRect( false, 72.0f );
            iconRect = new Rect( contentRect.x + 8.0f, contentRect.y + 8.0f, 56.0f, 56.0f );
            titleRect = new Rect( iconRect.xMax + 10.0f, contentRect.y + 8.0f, contentRect.width - 76.0f, 22.0f );
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
            EditorGUILayout.Space( 6.0f );
        }

        ///<summary>
        /// 기본 정보 속성 섹션 렌더링
        ///</summary>
        private void DrawSummaryPropertySection()
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "기본 정보", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( skillIdProperty );
            EditorGUILayout.PropertyField( skillNameProperty );
            EditorGUILayout.PropertyField( skillIconProperty );
            EditorGUILayout.PropertyField( skillTypeProperty );

            using ( new EditorGUI.DisabledScope( true ) )
            {
                eActiveSkillType activeSkillType = selectedSkillDefinition != null ? selectedSkillDefinition.GetActiveSkillType() : eActiveSkillType.NONE;
                EditorGUILayout.EnumPopup( "Active Skill Type", activeSkillType );
            }

            EditorGUILayout.PropertyField( descriptionProperty );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 사용 설정 속성 섹션 렌더링
        ///</summary>
        private void DrawExecutionPropertySection()
        {
            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "사용 설정", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( quickSlotIndexProperty );
            EditorGUILayout.PropertyField( cooldownSecondsProperty );
            EditorGUILayout.PropertyField( mpCostProperty );
            DrawCastPropertyBlock();
            EditorGUILayout.PropertyField( activeSkillEffectProperty );
            EditorGUILayout.PropertyField( activeActionProperty );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 강화 설정 속성 섹션 렌더링
        ///</summary>
        private void DrawProgressionPropertySection()
        {
            bool isActiveSkill = CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty );

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "강화 설정", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( learnSpCostProperty );
            EditorGUILayout.PropertyField( levelUpSpCostProperty );
            EditorGUILayout.PropertyField( maxSkillLevelProperty );

            if ( isActiveSkill )
            {
                EditorGUILayout.Space( 2.0f );
                EditorGUILayout.LabelField( "레벨별 증감", EditorStyles.boldLabel );
                EditorGUILayout.PropertyField( cooldownReductionPerLevelProperty );
                EditorGUILayout.PropertyField( damageMultiplierBonusPerLevelProperty );
                EditorGUILayout.PropertyField( flatDamageBonusPerLevelProperty );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 패시브 속성 섹션 렌더링
        ///</summary>
        private void DrawPassivePropertySection()
        {
            if ( CSkillEditorPreviewUtility.IsPassiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "패시브 설정", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( passiveStatBonusProperty, true );
            EditorGUILayout.PropertyField( passiveSkillEffectListProperty, true );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// VFX 속성 섹션 렌더링
        ///</summary>
        private void DrawVfxPropertySection()
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "VFX 설정", EditorStyles.boldLabel );
            DrawVfxBlock( "Cast VFX", castVfxPrefabProperty, castVfxOffsetProperty, castVfxReturnDelayProperty );
            DrawVfxBlock( "Hit VFX", hitVfxPrefabProperty, hitVfxOffsetProperty, hitVfxReturnDelayProperty );
            DrawVfxBlock( "Projectile VFX", projectileVfxPrefabProperty, projectileVfxOffsetProperty, projectileVfxReturnDelayProperty );
            DrawVfxBlock( "Loop VFX", loopVfxPrefabProperty, loopVfxOffsetProperty, loopVfxReturnDelayProperty );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 오디오 속성 섹션 렌더링
        ///</summary>
        private void DrawAudioPropertySection()
        {
            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "오디오 설정", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( castSfxClipNameProperty, new GUIContent( "시전 SFX" ) );
            EditorGUILayout.PropertyField( hitSfxClipNameProperty, new GUIContent( "타격 SFX" ) );

            if ( IsSelectedSkillPlaceType() )
            {
                EditorGUILayout.PropertyField( loopSfxClipNameProperty, new GUIContent( "설치형 Loop SFX" ) );
                EditorGUILayout.HelpBox( "설치형 스킬 지속시간 동안 반복 재생할 SFX 이름입니다. 확장자를 제외한 파일 이름을 입력합니다.", MessageType.None );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 해금 속성 섹션 렌더링
        ///</summary>
        private void DrawUnlockPropertySection()
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "해금 조건", EditorStyles.boldLabel );
            EditorGUILayout.PropertyField( unlockConditionListProperty, true );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 액티브 효과 속성 섹션 렌더링
        ///</summary>
        private void DrawActiveEffectPropertySection()
        {
            UnityEngine.Object activeEffectObject;

            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                return;
            }

            activeEffectObject = activeSkillEffectProperty.objectReferenceValue;

            if ( activeEffectObject == null )
            {
                return;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "액티브 효과 세부 편집", EditorStyles.boldLabel );
            UnityEditor.Editor.CreateCachedEditor( activeEffectObject, null, ref cachedActiveEffectEditor );

            if ( cachedActiveEffectEditor != null )
            {
                cachedActiveEffectEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 범위 미리보기 패널 렌더링
        ///</summary>
        private void DrawRangePreviewPanel()
        {
            UnityEngine.Object activeEffectObject;
            CSkillRangePreviewData previewData;

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );

            if ( CSkillEditorPreviewUtility.IsActiveSkill( skillTypeProperty ) == false )
            {
                EditorGUILayout.HelpBox( "패시브 스킬은 범위 프리뷰 대신 패시브 설정을 사용합니다.", MessageType.Info );
                EditorGUILayout.EndVertical();
                return;
            }

            activeEffectObject = activeSkillEffectProperty.objectReferenceValue;
            previewData = CSkillEditorPreviewUtility.BuildRangePreviewData( activeEffectObject );

            if ( previewData.isValid == false )
            {
                EditorGUILayout.HelpBox( "현재 액티브 효과는 범위 프리뷰를 제공하지 않습니다.", MessageType.Info );
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField( previewData.title, EditorStyles.boldLabel );
            EditorGUILayout.LabelField( previewData.detail, EditorStyles.wordWrappedMiniLabel );
            GUILayout.Space( 6.0f );
            CSkillEditorPreviewUtility.DrawRangePreviewCanvas(
                GUILayoutUtility.GetRect( 10.0f, PreviewHeight, GUILayout.ExpandWidth( true ) ),
                previewData
            );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 미리보기 요약 패널 렌더링
        ///</summary>
        private void DrawPreviewSummaryPanel()
        {
            string summaryText = BuildPreviewSummaryText();
            string dynamicDescriptionPreviewText = BuildDynamicDescriptionPreviewText();

            if ( string.IsNullOrWhiteSpace( dynamicDescriptionPreviewText ) == false )
            {
                summaryText += "\n" + dynamicDescriptionPreviewText;
            }

            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "요약", EditorStyles.boldLabel );
            EditorGUILayout.SelectableLabel( summaryText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight( 96.0f ) );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 미리보기 요약 문구 생성
        ///</summary>
        private string BuildPreviewSummaryText()
        {
            string skillName = selectedSkillDefinition != null ? selectedSkillDefinition.GetSkillName() : "Unknown";
            string skillId = selectedSkillDefinition != null ? selectedSkillDefinition.GetSkillId() : string.Empty;
            eActiveSkillType activeSkillType = selectedSkillDefinition != null ? selectedSkillDefinition.GetActiveSkillType() : eActiveSkillType.NONE;
            string typeSummary = CSkillEditorPreviewUtility.BuildTypeSummaryText( skillTypeProperty, activeSkillType );
            string description = descriptionProperty != null ? descriptionProperty.stringValue : string.Empty;
            string result = $"이름: {skillName}\nID: {skillId}\n{typeSummary}\n설명: {description}";
            return result;
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

        ///<summary>
        /// 선택된 스킬 직렬화 오브젝트 보장
        ///</summary>
        private bool EnsureSelectedSkillSerializedObject()
        {
            if ( selectedSkillDefinition == null )
            {
                return false;
            }

            if ( selectedSkillSerializedObject == null || string.Equals( lastLoadedSkillAssetPath, selectedSkillAssetPath, StringComparison.Ordinal ) == false )
            {
                selectedSkillSerializedObject = new SerializedObject( selectedSkillDefinition );
                CacheSkillProperties();
                lastLoadedSkillAssetPath = selectedSkillAssetPath;
            }

            return true;
        }

        ///<summary>
        /// 스킬 속성 캐시 처리
        ///</summary>
        private void CacheSkillProperties()
        {
            skillIdProperty = selectedSkillSerializedObject.FindProperty( "skillId" );
            skillNameProperty = selectedSkillSerializedObject.FindProperty( "skillName" );
            skillIconProperty = selectedSkillSerializedObject.FindProperty( "skillIcon" );
            skillTypeProperty = selectedSkillSerializedObject.FindProperty( "skillType" );
            activeSkillTypeProperty = selectedSkillSerializedObject.FindProperty( "activeSkillType" );
            quickSlotIndexProperty = selectedSkillSerializedObject.FindProperty( "quickSlotIndex" );
            cooldownSecondsProperty = selectedSkillSerializedObject.FindProperty( "cooldownSeconds" );
            mpCostProperty = selectedSkillSerializedObject.FindProperty( "mpCost" );
            learnSpCostProperty = selectedSkillSerializedObject.FindProperty( "learnSpCost" );
            levelUpSpCostProperty = selectedSkillSerializedObject.FindProperty( "levelUpSpCost" );
            maxSkillLevelProperty = selectedSkillSerializedObject.FindProperty( "maxSkillLevel" );
            cooldownReductionPerLevelProperty = selectedSkillSerializedObject.FindProperty( "cooldownReductionPerLevel" );
            damageMultiplierBonusPerLevelProperty = selectedSkillSerializedObject.FindProperty( "damageMultiplierBonusPerLevel" );
            flatDamageBonusPerLevelProperty = selectedSkillSerializedObject.FindProperty( "flatDamageBonusPerLevel" );
            castLockDurationSecondsProperty = selectedSkillSerializedObject.FindProperty( "castLockDurationSeconds" );
            castAnimationProperty = selectedSkillSerializedObject.FindProperty( "castAnimation" );
            castAnimationNameProperty = selectedSkillSerializedObject.FindProperty( "castAnimationName" );
            castAnimationSpeedProperty = selectedSkillSerializedObject.FindProperty( "castAnimationSpeed" );
            descriptionProperty = selectedSkillSerializedObject.FindProperty( "description" );
            castVfxPrefabProperty = selectedSkillSerializedObject.FindProperty( "castVfxPrefab" );
            castVfxOffsetProperty = selectedSkillSerializedObject.FindProperty( "castVfxOffset" );
            castVfxReturnDelayProperty = selectedSkillSerializedObject.FindProperty( "castVfxReturnDelay" );
            hitVfxPrefabProperty = selectedSkillSerializedObject.FindProperty( "hitVfxPrefab" );
            hitVfxOffsetProperty = selectedSkillSerializedObject.FindProperty( "hitVfxOffset" );
            hitVfxReturnDelayProperty = selectedSkillSerializedObject.FindProperty( "hitVfxReturnDelay" );
            castSfxClipNameProperty = selectedSkillSerializedObject.FindProperty( "castSfxClipName" );
            hitSfxClipNameProperty = selectedSkillSerializedObject.FindProperty( "hitSfxClipName" );
            loopSfxClipNameProperty = selectedSkillSerializedObject.FindProperty( "loopSfxClipName" );
            projectileVfxPrefabProperty = selectedSkillSerializedObject.FindProperty( "projectileVfxPrefab" );
            projectileVfxOffsetProperty = selectedSkillSerializedObject.FindProperty( "projectileVfxOffset" );
            projectileVfxReturnDelayProperty = selectedSkillSerializedObject.FindProperty( "projectileVfxReturnDelay" );
            loopVfxPrefabProperty = selectedSkillSerializedObject.FindProperty( "loopVfxPrefab" );
            loopVfxOffsetProperty = selectedSkillSerializedObject.FindProperty( "loopVfxOffset" );
            loopVfxReturnDelayProperty = selectedSkillSerializedObject.FindProperty( "loopVfxReturnDelay" );
            passiveStatBonusProperty = selectedSkillSerializedObject.FindProperty( "passiveStatBonus" );
            activeActionProperty = selectedSkillSerializedObject.FindProperty( "activeAction" );
            activeSkillEffectProperty = selectedSkillSerializedObject.FindProperty( "activeSkillEffect" );
            passiveSkillEffectListProperty = selectedSkillSerializedObject.FindProperty( "passiveSkillEffectList" );
            unlockConditionListProperty = selectedSkillSerializedObject.FindProperty( "unlockConditionList" );
        }

        ///<summary>
        /// 스킬 정의 목록 새로고침
        ///</summary>
        private void RefreshSkillDefinitionItemList()
        {
            string[] skillDefinitionGuidArray = AssetDatabase.FindAssets( "t:CSkillDefinition" );
            skillDefinitionItemList.Clear();

            for ( int index = 0; index < skillDefinitionGuidArray.Length; index++ )
            {
                string skillDefinitionGuid = skillDefinitionGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( skillDefinitionGuid );
                CSkillDefinition skillDefinition = AssetDatabase.LoadAssetAtPath<CSkillDefinition>( assetPath );
                CSkillDefinitionListItem skillDefinitionItem;

                if ( skillDefinition == null )
                {
                    continue;
                }

                skillDefinitionItem = new CSkillDefinitionListItem();
                skillDefinitionItem.assetPath = assetPath;
                skillDefinitionItem.displayName = skillDefinition.GetSkillName();
                skillDefinitionItem.skillId = skillDefinition.GetSkillId();
                skillDefinitionItem.skillType = skillDefinition.GetSkillType();
                skillDefinitionItemList.Add( skillDefinitionItem );
            }

            skillDefinitionItemList.Sort( CompareSkillDefinitionItem );
        }

        ///<summary>
        /// 스킬 목록 정렬 비교 처리
        ///</summary>
        private int CompareSkillDefinitionItem( CSkillDefinitionListItem _left, CSkillDefinitionListItem _right )
        {
            string leftName = _left != null ? _left.displayName : string.Empty;
            string rightName = _right != null ? _right.displayName : string.Empty;
            int result = string.Compare( leftName, rightName, StringComparison.OrdinalIgnoreCase );
            return result;
        }

        ///<summary>
        /// 필터링된 스킬 목록 반환
        ///</summary>
        private List<CSkillDefinitionListItem> GetFilteredSkillDefinitionItemList()
        {
            List<CSkillDefinitionListItem> filteredItemList = new List<CSkillDefinitionListItem>();
            string loweredSearchText = searchText != null ? searchText.Trim().ToLowerInvariant() : string.Empty;

            for ( int index = 0; index < skillDefinitionItemList.Count; index++ )
            {
                CSkillDefinitionListItem skillDefinitionItem = skillDefinitionItemList[ index ];
                bool isMatched;

                if ( skillDefinitionItem == null )
                {
                    continue;
                }

                if ( string.IsNullOrEmpty( loweredSearchText ) )
                {
                    filteredItemList.Add( skillDefinitionItem );
                    continue;
                }

                isMatched =
                    ContainsLoweredText( skillDefinitionItem.displayName, loweredSearchText ) ||
                    ContainsLoweredText( skillDefinitionItem.skillId, loweredSearchText ) ||
                    ContainsLoweredText( skillDefinitionItem.assetPath, loweredSearchText );

                if ( isMatched )
                {
                    filteredItemList.Add( skillDefinitionItem );
                }
            }

            return filteredItemList;
        }

        ///<summary>
        /// 소문자 텍스트 포함 여부 판정
        ///</summary>
        private bool ContainsLoweredText( string _sourceText, string _loweredSearchText )
        {
            string loweredSourceText;
            bool result;

            if ( string.IsNullOrEmpty( _sourceText ) )
            {
                return false;
            }

            loweredSourceText = _sourceText.ToLowerInvariant();
            result = loweredSourceText.Contains( _loweredSearchText );
            return result;
        }

        ///<summary>
        /// 선택된 스킬 복원 처리
        ///</summary>
        private void RestoreSelectedSkillDefinition()
        {
            if ( string.IsNullOrEmpty( selectedSkillAssetPath ) )
            {
                SelectFirstSkillDefinition();
                return;
            }

            SelectSkillDefinitionByAssetPath( selectedSkillAssetPath, false );
        }

        ///<summary>
        /// 첫 번째 스킬 선택 처리
        ///</summary>
        private void SelectFirstSkillDefinition()
        {
            if ( skillDefinitionItemList.Count <= 0 )
            {
                ClearSelectedSkillDefinition();
                return;
            }

            SelectSkillDefinitionByAssetPath( skillDefinitionItemList[ 0 ].assetPath, false );
        }

        ///<summary>
        /// 스킬 에셋 경로 기준 선택 처리
        ///</summary>
        private void SelectSkillDefinitionByAssetPath( string _assetPath )
        {
            SelectSkillDefinitionByAssetPath( _assetPath, true );
        }

        ///<summary>
        /// 스킬 에셋 경로 기준 선택 내부 처리
        ///</summary>
        private void SelectSkillDefinitionByAssetPath( string _assetPath, bool _pingAsset )
        {
            CSkillDefinition skillDefinition;

            if ( string.IsNullOrEmpty( _assetPath ) )
            {
                ClearSelectedSkillDefinition();
                return;
            }

            skillDefinition = AssetDatabase.LoadAssetAtPath<CSkillDefinition>( _assetPath );

            if ( skillDefinition == null )
            {
                ClearSelectedSkillDefinition();
                return;
            }

            ClearInputFieldFocus();
            ReleaseCachedEditors();
            selectedSkillAssetPath = _assetPath;
            selectedSkillDefinition = skillDefinition;
            selectedSkillSerializedObject = new SerializedObject( selectedSkillDefinition );
            CacheSkillProperties();
            lastLoadedSkillAssetPath = selectedSkillAssetPath;

            if ( _pingAsset )
            {
                EditorGUIUtility.PingObject( selectedSkillDefinition );
            }

            Repaint();
        }

        ///<summary>
        /// 선택된 스킬 초기화 처리
        ///</summary>
        private void ClearSelectedSkillDefinition()
        {
            ClearInputFieldFocus();
            selectedSkillAssetPath = string.Empty;
            selectedSkillDefinition = null;
            selectedSkillSerializedObject = null;
            lastLoadedSkillAssetPath = string.Empty;
            ReleaseCachedEditors();
        }

        ///<summary>
        /// 편집 필드 포커스 해제
        ///</summary>
        private void ClearInputFieldFocus()
        {
            GUI.FocusControl( null );
            EditorGUI.FocusTextInControl( string.Empty );
            GUIUtility.keyboardControl = 0;
        }

        ///<summary>
        /// 선택된 스킬 핑 처리
        ///</summary>
        private void PingSelectedSkillDefinition()
        {
            if ( selectedSkillDefinition == null )
            {
                return;
            }

            EditorGUIUtility.PingObject( selectedSkillDefinition );
        }

        ///<summary>
        /// 선택된 스킬 인스펙터 오픈 처리
        ///</summary>
        private void OpenSelectedSkillDefinitionInInspector()
        {
            if ( selectedSkillDefinition == null )
            {
                return;
            }

            Selection.activeObject = selectedSkillDefinition;
            EditorGUIUtility.PingObject( selectedSkillDefinition );
        }

        ///<summary>
        /// 수정된 에셋 더티 처리
        ///</summary>
        private void MarkEditedAssetsDirty()
        {
            EditorUtility.SetDirty( selectedSkillDefinition );

            if ( activeSkillEffectProperty != null && activeSkillEffectProperty.objectReferenceValue != null )
            {
                EditorUtility.SetDirty( activeSkillEffectProperty.objectReferenceValue );
            }
        }

        ///<summary>
        /// 캐시된 에디터 정리 처리
        ///</summary>
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
        /// 캐시된 에디터 정리 처리
        ///</summary>
        ///<summary>
        /// 동적 설명 미리보기 문자열 반환
        ///</summary>
        private string BuildDynamicDescriptionPreviewText()
        {
            if ( selectedSkillDefinition == null )
            {
                return string.Empty;
            }

            string descriptionTemplate = descriptionProperty != null ? descriptionProperty.stringValue : string.Empty;
            int maxSkillLevel = selectedSkillDefinition.GetMaxSkillLevel();
            string levelOneDescription = selectedSkillDefinition.GetFormattedDescription( 1 );
            string maxLevelDescription = selectedSkillDefinition.GetFormattedDescription( maxSkillLevel );
            string supportedTokenText = BuildSupportedTokenText();
            string result = $"Template: {descriptionTemplate}\nLevel 1: {levelOneDescription}\nMax Level: {maxLevelDescription}\nTokens: {supportedTokenText}";
            return result;
        }

        ///<summary>
        /// 지원 토큰 문자열 반환
        ///</summary>
        private string BuildSupportedTokenText()
        {
            System.Collections.Generic.IReadOnlyList<string> supportedTokenList = CSkillDescriptionFormatter.GetSupportedTokenList();
            string result = string.Join( ", ", supportedTokenList );
            return result;
        }

        ///<summary>
        /// 선택 스킬 설치형 여부 반환
        ///</summary>
        private bool IsSelectedSkillPlaceType()
        {
            if ( selectedSkillDefinition == null )
            {
                return false;
            }

            bool result = selectedSkillDefinition.GetActiveSkillType() == eActiveSkillType.PLACE;
            return result;
        }

        private void ReleaseCachedEditors()
        {
            if ( cachedActiveEffectEditor != null )
            {
                DestroyImmediate( cachedActiveEffectEditor );
                cachedActiveEffectEditor = null;
            }
        }
    }
}
