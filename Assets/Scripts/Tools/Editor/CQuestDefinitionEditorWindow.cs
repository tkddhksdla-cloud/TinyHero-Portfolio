using System;
using System.Collections.Generic;
using System.IO;
using TinyHero.Core.Data;
using TinyHero.Quest;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 퀘스트 정의 목록 정보
    ///</summary>
    [Serializable]
    public sealed class QuestDefinitionInfo
    {
        public string questId;
        public string questName;
        public string assetPath;
        public eQuestType questType;
    }

    ///<summary>
    /// NPC 선택 목록 정보
    ///</summary>
    [Serializable]
    public sealed class QuestNpcSelectionInfo
    {
        public string npcId;
        public string npcName;
        public string assetPath;
    }

    ///<summary>
    /// 몬스터 선택 목록 정보
    ///</summary>
    [Serializable]
    public sealed class QuestMonsterSelectionInfo
    {
        public string monsterId;
        public string monsterName;
    }

    ///<summary>
    /// 퀘스트 정의 에디터 창
    ///</summary>
    public sealed class CQuestDefinitionEditorWindow : CEditorToolWindowBase<QuestDefinitionInfo>
    {
        private const string QuestDefinitionFolderPath = "Assets/Resources/Data/Quest/Definitions";
        private const string NpcInteractionDataSearchRootPath = "Assets";
        private const string MonsterStatTableAssetPath = "Assets/Resources/Data/Monster/MonsterStatTableData.asset";
        private const string NormalQuestIdPrefix = "QUEST_NORMAL_";
        private const string RepeatableQuestIdPrefix = "QUEST_REPEATABLE_";
        private const string DefaultQuestDescriptionTemplate = "{GIVER}의 의뢰";
        private const int QuestIdNumberDigits = 5;
        private const float ListViewHeight = 500.0f;
        private const float ListItemHeight = 44.0f;
        private const float DialogueLineTextAreaMinHeight = 72.0f;
        private const float DialogueColumnMinWidth = 420.0f;
        private const float QuestEntryColumnMinWidth = DialogueColumnMinWidth;
        private const float QuestEditorColumnSpacing = 4.0f;
        private const float ActionButtonSectionWidth = ( DialogueColumnMinWidth * 2.0f ) + QuestEditorColumnSpacing;

        [SerializeField] private List<QuestDefinitionInfo> questDefinitionInfoList = new List<QuestDefinitionInfo>();
        [SerializeField] private List<QuestNpcSelectionInfo> questNpcSelectionInfoList = new List<QuestNpcSelectionInfo>();
        [SerializeField] private List<QuestMonsterSelectionInfo> questMonsterSelectionInfoList = new List<QuestMonsterSelectionInfo>();
        [SerializeField] private int selectedQuestIndex = -1;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private int selectedTypeFilterIndex;
        [SerializeField] private string newQuestAssetName = string.Empty;
        [SerializeField] private eQuestType newQuestType = eQuestType.NORMAL;
        [SerializeField] private string currentQuestAssetNameDraft = string.Empty;
        [SerializeField] private string currentQuestAssetPathDraft = string.Empty;
        [SerializeField] private string giverNpcSearchText = string.Empty;
        [SerializeField] private string completerNpcSearchText = string.Empty;
        [SerializeField] private bool isGiverNpcSearchFoldout;
        [SerializeField] private bool isCompleterNpcSearchFoldout;
        [SerializeField] private string monsterSearchText = string.Empty;
        [SerializeField] private bool isMonsterSearchFoldout;

        private Vector2 questListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "퀘스트 정의 목록을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private CQuestDefinition workingQuestDefinition;
        private string loadedQuestAssetPath = string.Empty;

        private static readonly string[] QuestTypeFilterOptionArray =
        {
            "ALL",
            eQuestType.NORMAL.ToString(),
            eQuestType.REPEATABLE.ToString()
        };

        ///<summary>
        /// 퀘스트 정의 에디터 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Quest Definition Editor" )]
        private static void ShowWindow()
        {
            CQuestDefinitionEditorWindow window = GetWindow<CQuestDefinitionEditorWindow>();
            window.titleContent = new GUIContent( "Quest Definition Editor" );
            window.minSize = new Vector2( 1240.0f, 780.0f );
            window.Show();
        }

        ///<summary>
        /// 퀘스트 정의 에디터 창 열기
        ///</summary>
        public static void OpenWindow()
        {
            ShowWindow();
        }

        ///<summary>
        /// 편집 창 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            if ( string.IsNullOrWhiteSpace( newQuestAssetName ) )
            {
                ResetNewQuestAssetName();
            }

            RefreshQuestDefinitionInfos();
            RefreshNpcSelectionInfos();
            RefreshMonsterSelectionInfos();
        }

        ///<summary>
        /// 편집 창 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            DestroyWorkingQuestDefinition();
        }

        ///<summary>
        /// 편집 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            DrawWindowHeader( "Quest Definition Editor", "퀘스트 정의를 검색, 생성, 저장, 이름 변경하고 NPC 연결 정보와 대화/조건/보상을 편집합니다." );
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawQuestListSection();
            DrawEditorSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical( "box", GUILayout.MinHeight( 96.0f ) );
            EditorGUILayout.LabelField( "Browser", EditorStyles.boldLabel );
            string updatedSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( updatedSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = updatedSearchText;
            }

            int updatedTypeFilterIndex = EditorGUILayout.Popup( "Type", selectedTypeFilterIndex, QuestTypeFilterOptionArray, GUILayout.Width( 240.0f ) );

            if ( updatedTypeFilterIndex != selectedTypeFilterIndex )
            {
                selectedTypeFilterIndex = updatedTypeFilterIndex;
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 100.0f ) ) )
            {
                RefreshQuestDefinitionInfos();
                RefreshNpcSelectionInfos();
                RefreshMonsterSelectionInfos();
            }

            if ( GUILayout.Button( "Auto Create Missing", GUILayout.Width( 160.0f ) ) )
            {
                AutoCreateMissingQuestDefinitions();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox( $"Loaded Quests {questDefinitionInfoList.Count} / NPC {questNpcSelectionInfoList.Count} / Monster {questMonsterSelectionInfoList.Count}", MessageType.None );
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical( "box", GUILayout.MinHeight( 96.0f ) );
            EditorGUILayout.LabelField( "Create Quest", EditorStyles.boldLabel );
            eQuestType updatedQuestType = ( eQuestType )EditorGUILayout.EnumPopup( "Create Type", newQuestType, GUILayout.Width( 260.0f ) );

            if ( updatedQuestType != newQuestType )
            {
                newQuestType = updatedQuestType;
                ResetNewQuestAssetName();
            }

            newQuestAssetName = EditorGUILayout.TextField( "Asset Name", newQuestAssetName );
            string nextQuestId = GenerateNextQuestId( newQuestType );
            EditorGUILayout.LabelField( "Default Quest Id", nextQuestId );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Reset Name", GUILayout.Width( 120.0f ) ) )
            {
                ResetNewQuestAssetName();
            }

            if ( GUILayout.Button( "Create", GUILayout.Width( 100.0f ) ) )
            {
                CreateNewQuestDefinition();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 퀘스트 목록 영역 렌더링
        ///</summary>
        private void DrawQuestListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( 420.0f ) );
            EditorGUILayout.LabelField( "Quest Definitions", EditorStyles.boldLabel );
            List<QuestDefinitionInfo> filteredInfoList = GetFilteredQuestDefinitionInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfoList.Count}개", MessageType.None );
            questListScrollPosition = EditorGUILayout.BeginScrollView( questListScrollPosition, GUILayout.Height( ListViewHeight ) );

            for ( int index = 0; index < filteredInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = filteredInfoList[ index ];
                DrawQuestListItem( questInfo, filteredInfoList.Count, index );
            }

            EditorGUILayout.EndScrollView();
            DrawStatusMessage( statusMessage, statusMessageType );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 퀘스트 목록 항목 렌더링
        ///</summary>
        private void DrawQuestListItem( QuestDefinitionInfo _questInfo, int _filteredItemCount, int _filteredIndex )
        {
            if ( _questInfo == null )
            {
                return;
            }

            int sourceIndex = questDefinitionInfoList.IndexOf( _questInfo );
            bool isSelected = sourceIndex == selectedQuestIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string controlName = BuildQuestControlName( sourceIndex );
            string buttonLabel = $"[ {_questInfo.questType} ] {_questInfo.questName}\n{_questInfo.questId}";
            GUI.SetNextControlName( controlName );
            bool wasClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( wasClicked )
            {
                SelectQuestByIndex( sourceIndex, _filteredIndex, _filteredItemCount );
            }

            if ( isSelected && isPendingFocusToSelection )
            {
                GUI.FocusControl( controlName );
                isPendingFocusToSelection = false;
            }

            if ( isSelected )
            {
                Rect itemRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect( itemRect, new Color( 0.2f, 0.5f, 0.85f, 0.18f ) );
            }
        }

        ///<summary>
        /// 편집 영역 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            CQuestDefinition selectedQuestDefinition = GetSelectedQuestDefinition();

            if ( selectedQuestDefinition == null )
            {
                EditorGUILayout.HelpBox( "편집할 퀘스트 정의를 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            EnsureWorkingQuestDefinitionLoaded();

            if ( workingQuestDefinition == null )
            {
                EditorGUILayout.HelpBox( "작업용 퀘스트 데이터를 준비하지 못했습니다.", MessageType.Warning );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawAssetInfoSection( selectedQuestDefinition );
            EditorGUILayout.Space();
            DrawQuestPropertySection();
            EditorGUILayout.Space();
            DrawNpcAssignmentSection();
            EditorGUILayout.Space();
            DrawDialogueSection();
            EditorGUILayout.Space();
            DrawConditionRewardColumnSection();
            EditorGUILayout.Space();
            DrawActionButtonSection( selectedQuestDefinition );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 자산 정보 영역 렌더링
        ///</summary>
        private void DrawAssetInfoSection( CQuestDefinition _selectedQuestDefinition )
        {
            EditorGUILayout.LabelField( "Asset Info", EditorStyles.boldLabel );
            string assetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );
            EnsureAssetRenameDraft( _selectedQuestDefinition, assetPath );
            EditorGUILayout.LabelField( "Asset Path", assetPath );
            currentQuestAssetNameDraft = EditorGUILayout.TextField( "Asset Name", currentQuestAssetNameDraft );
            EditorGUILayout.LabelField( "Asset File", Path.GetFileNameWithoutExtension( assetPath ) );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Rename Asset", GUILayout.Width( 120.0f ) ) )
            {
                RenameQuestDefinitionAsset( _selectedQuestDefinition, currentQuestAssetNameDraft );
            }

            if ( GUILayout.Button( "Reset Name", GUILayout.Width( 120.0f ) ) )
            {
                ResetCurrentQuestAssetNameDraft();
            }

            if ( GUILayout.Button( "Ping Asset", GUILayout.Width( 120.0f ) ) )
            {
                EditorGUIUtility.PingObject( _selectedQuestDefinition );
                Selection.activeObject = _selectedQuestDefinition;
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 퀘스트 기본 속성 영역 렌더링
        ///</summary>
        private void DrawQuestPropertySection()
        {
            EditorGUILayout.LabelField( "Quest Info", EditorStyles.boldLabel );
            string questId = EditorGUILayout.TextField( "Quest Id", workingQuestDefinition.GetQuestId() );
            workingQuestDefinition.SetQuestId( questId );
            string questName = EditorGUILayout.TextField( "Quest Name", workingQuestDefinition.GetQuestName() );
            workingQuestDefinition.SetQuestName( questName );
            EditorGUILayout.LabelField( "Description" );
            string description = EditorGUILayout.TextArea( workingQuestDefinition.GetDescriptionTemplate(), GUILayout.MinHeight( 80.0f ) );
            workingQuestDefinition.SetDescription( description );
            DrawQuestDescriptionSymbolGuide();
            eQuestType questType = ( eQuestType )EditorGUILayout.EnumPopup( "Quest Type", workingQuestDefinition.GetQuestType() );
            workingQuestDefinition.SetQuestType( questType );
            EditorGUILayout.HelpBox( $"Recommended Prefix: {ResolveQuestIdPrefix( questType )}", MessageType.None );
        }

        ///<summary>
        /// 퀘스트 설명 심볼 안내 렌더링
        ///</summary>
        private void DrawQuestDescriptionSymbolGuide()
        {
            string supportedTokenText = string.Join( ", ", CQuestDescriptionFormatter.GetQuestTokenList() );
            string previewText = BuildQuestDescriptionPreviewText();
            EditorGUILayout.HelpBox( $"사용 가능 심볼: {supportedTokenText}\nPreview: {previewText}", MessageType.None );
        }

        ///<summary>
        /// 퀘스트 설명 미리보기 문자열 생성
        ///</summary>
        private string BuildQuestDescriptionPreviewText()
        {
            if ( workingQuestDefinition == null )
            {
                return string.Empty;
            }

            string result = workingQuestDefinition.GetDescription();
            return result;
        }

        ///<summary>
        /// NPC 할당 영역 렌더링
        ///</summary>
        private void DrawNpcAssignmentSection()
        {
            EditorGUILayout.LabelField( "NPC Assignment", EditorStyles.boldLabel );
            string giverNpcId = EditorGUILayout.TextField( "Giver NPC Id", workingQuestDefinition.GetGiverNpcId() );
            workingQuestDefinition.SetGiverNpcId( giverNpcId );
            DrawAssignedNpcInfo( workingQuestDefinition.GetGiverNpcId(), "Giver NPC" );
            DrawNpcSearchFoldout( "Giver NPC Search", ref isGiverNpcSearchFoldout, ref giverNpcSearchText, true );
            string completerNpcId = EditorGUILayout.TextField( "Completer NPC Id", workingQuestDefinition.GetCompleterNpcId() );
            workingQuestDefinition.SetCompleterNpcId( completerNpcId );
            DrawAssignedNpcInfo( workingQuestDefinition.GetCompleterNpcId(), "Completer NPC" );
            DrawNpcSearchFoldout( "Completer NPC Search", ref isCompleterNpcSearchFoldout, ref completerNpcSearchText, false );
        }

        ///<summary>
        /// NPC 검색 폴드아웃 렌더링
        ///</summary>
        private void DrawNpcSearchFoldout( string _label, ref bool _isFoldout, ref string _searchKeyword, bool _isGiver )
        {
            _isFoldout = EditorGUILayout.Foldout( _isFoldout, _label, true );

            if ( _isFoldout == false )
            {
                return;
            }

            EditorGUI.indentLevel++;
            _searchKeyword = EditorGUILayout.TextField( "Search", _searchKeyword );
            List<QuestNpcSelectionInfo> filteredNpcInfoList = GetFilteredNpcSelectionInfos( _searchKeyword );

            for ( int index = 0; index < filteredNpcInfoList.Count; index++ )
            {
                QuestNpcSelectionInfo npcInfo = filteredNpcInfoList[ index ];

                if ( npcInfo == null )
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal( "box" );
                EditorGUILayout.LabelField( npcInfo.npcName, GUILayout.Width( 180.0f ) );
                EditorGUILayout.LabelField( npcInfo.npcId );

                if ( GUILayout.Button( "Assign", GUILayout.Width( 80.0f ) ) )
                {
                    if ( _isGiver )
                    {
                        workingQuestDefinition.SetGiverNpcId( npcInfo.npcId );
                    }
                    else
                    {
                        workingQuestDefinition.SetCompleterNpcId( npcInfo.npcId );
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if ( filteredNpcInfoList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "검색된 NPC가 없습니다.", MessageType.Info );
            }

            EditorGUI.indentLevel--;
        }

        ///<summary>
        /// 대화 설정 영역 렌더링
        ///</summary>
        private void DrawDialogueSection()
        {
            EditorGUILayout.LabelField( "Quest Dialogue", EditorStyles.boldLabel );
            SerializedObject serializedQuestDefinition = new SerializedObject( workingQuestDefinition );
            serializedQuestDefinition.Update();
            SerializedProperty useAcceptDialogueProperty = serializedQuestDefinition.FindProperty( "useAcceptDialogue" );
            SerializedProperty useCompleteDialogueProperty = serializedQuestDefinition.FindProperty( "useCompleteDialogue" );
            EditorGUILayout.PropertyField( useAcceptDialogueProperty, new GUIContent( "Use Accept Dialogue" ) );
            EditorGUILayout.PropertyField( useCompleteDialogueProperty, new GUIContent( "Use Complete Dialogue" ) );
            serializedQuestDefinition.ApplyModifiedPropertiesWithoutUndo();

            DrawDialoguePresetColumnLayout( useAcceptDialogueProperty.boolValue, useCompleteDialogueProperty.boolValue );
        }

        ///<summary>
        /// 퀘스트 대화 프리셋 컬럼 레이아웃 렌더링
        ///</summary>
        private void DrawDialoguePresetColumnLayout( bool _useAcceptDialogue, bool _useCompleteDialogue )
        {
            if ( _useAcceptDialogue == false && _useCompleteDialogue == false )
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if ( _useAcceptDialogue )
            {
                EditorGUILayout.BeginVertical( GUILayout.Width( DialogueColumnMinWidth ) );
                DrawDialoguePresetEditor( "Accept Dialogue", workingQuestDefinition.GetAcceptDialoguePreset() );
                EditorGUILayout.EndVertical();
            }

            if ( _useCompleteDialogue )
            {
                EditorGUILayout.BeginVertical( GUILayout.Width( DialogueColumnMinWidth ) );
                DrawDialoguePresetEditor( "Complete Dialogue", workingQuestDefinition.GetCompleteDialoguePreset() );
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 퀘스트 대화 프리셋 편집 UI 렌더링
        ///</summary>
        private void DrawDialoguePresetEditor( string _title, CNPCDialoguePreset _dialoguePreset )
        {
            if ( _dialoguePreset == null )
            {
                EditorGUILayout.HelpBox( $"{_title} 데이터가 없습니다.", MessageType.Warning );
                return;
            }

            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.LabelField( _title, EditorStyles.boldLabel );
            string presetName = EditorGUILayout.TextField( "Preset Name", _dialoguePreset.GetPresetName() );
            _dialoguePreset.SetPresetName( presetName );
            DrawDialogueLineListEditor( _dialoguePreset.GetDialogueLineList() );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 퀘스트 대화 라인 목록 편집 UI 렌더링
        ///</summary>
        private void DrawDialogueLineListEditor( List<string> _dialogueLineList )
        {
            if ( _dialogueLineList == null )
            {
                EditorGUILayout.HelpBox( "Dialogue Line List가 없습니다.", MessageType.Warning );
                return;
            }

            EditorGUILayout.LabelField( "Dialogue Line List", EditorStyles.boldLabel );

            if ( _dialogueLineList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 대화 라인이 없습니다.", MessageType.None );
            }

            for ( int index = 0; index < _dialogueLineList.Count; index++ )
            {
                bool shouldStopDrawing = DrawDialogueLineElement( _dialogueLineList, index );

                if ( shouldStopDrawing )
                {
                    return;
                }
            }

            if ( GUILayout.Button( "Dialogue Line 추가", GUILayout.Height( 28.0f ) ) )
            {
                _dialogueLineList.Add( string.Empty );
            }
        }

        ///<summary>
        /// 퀘스트 대화 라인 단일 항목 편집 UI 렌더링
        ///</summary>
        private bool DrawDialogueLineElement( List<string> _dialogueLineList, int _index )
        {
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Element {_index}", EditorStyles.boldLabel );

            GUI.enabled = _index > 0;

            if ( GUILayout.Button( "▲", GUILayout.Width( 28.0f ) ) )
            {
                MoveDialogueLine( _dialogueLineList, _index, _index - 1 );
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }

            GUI.enabled = _index < _dialogueLineList.Count - 1;

            if ( GUILayout.Button( "▼", GUILayout.Width( 28.0f ) ) )
            {
                MoveDialogueLine( _dialogueLineList, _index, _index + 1 );
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }

            GUI.enabled = true;

            if ( GUILayout.Button( "Copy", GUILayout.Width( 52.0f ) ) )
            {
                string copiedLine = _dialogueLineList[ _index ];
                _dialogueLineList.Insert( _index + 1, copiedLine );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }

            if ( GUILayout.Button( "Remove", GUILayout.Width( 68.0f ) ) )
            {
                _dialogueLineList.RemoveAt( _index );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }

            EditorGUILayout.EndHorizontal();
            string currentLine = _dialogueLineList[ _index ];
            string updatedLine = EditorGUILayout.TextArea( currentLine, GUILayout.MinHeight( DialogueLineTextAreaMinHeight ), GUILayout.ExpandWidth( true ) );
            _dialogueLineList[ _index ] = updatedLine;
            EditorGUILayout.EndVertical();
            return false;
        }

        ///<summary>
        /// 퀘스트 대화 라인 순서 이동
        ///</summary>
        private void MoveDialogueLine( List<string> _dialogueLineList, int _sourceIndex, int _targetIndex )
        {
            if ( _dialogueLineList == null )
            {
                return;
            }

            bool isInvalidSource = _sourceIndex < 0 || _sourceIndex >= _dialogueLineList.Count;
            bool isInvalidTarget = _targetIndex < 0 || _targetIndex >= _dialogueLineList.Count;

            if ( isInvalidSource || isInvalidTarget )
            {
                return;
            }

            string sourceLine = _dialogueLineList[ _sourceIndex ];
            _dialogueLineList.RemoveAt( _sourceIndex );
            _dialogueLineList.Insert( _targetIndex, sourceLine );
        }

        ///<summary>
        /// 조건과 보상 컬럼 영역 렌더링
        ///</summary>
        private void DrawConditionRewardColumnSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical( GUILayout.Width( QuestEntryColumnMinWidth ) );
            DrawConditionSection();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical( GUILayout.Width( QuestEntryColumnMinWidth ) );
            DrawRewardSection();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 조건 설정 영역 렌더링
        ///</summary>
        private void DrawConditionSection()
        {
            EditorGUILayout.LabelField( "Quest Conditions", EditorStyles.boldLabel );
            List<CQuestConditionEntry> conditionEntryList = workingQuestDefinition.GetConditionEntryList();

            if ( conditionEntryList == null || conditionEntryList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 퀘스트 조건이 없습니다.", MessageType.None );
            }
            else
            {
                for ( int index = 0; index < conditionEntryList.Count; index++ )
                {
                    CQuestConditionEntry conditionEntry = conditionEntryList[ index ];
                    DrawConditionEntry( conditionEntryList, conditionEntry, index );
                }
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Add Kill Monster" ) )
            {
                AddConditionEntry( eQuestConditionType.KILL_MONSTER );
            }

            if ( GUILayout.Button( "Add Reach Level" ) )
            {
                AddConditionEntry( eQuestConditionType.REACH_LEVEL );
            }

            if ( GUILayout.Button( "Add Turn In Item" ) )
            {
                AddConditionEntry( eQuestConditionType.TURN_IN_ITEM );
            }

            EditorGUILayout.EndHorizontal();
            NormalizeConditionEntryIds();
        }

        ///<summary>
        /// 보상 설정 영역 렌더링
        ///</summary>
        private void DrawRewardSection()
        {
            EditorGUILayout.LabelField( "Quest Rewards", EditorStyles.boldLabel );
            List<CQuestRewardEntry> rewardEntryList = workingQuestDefinition.GetRewardEntryList();

            if ( rewardEntryList == null || rewardEntryList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 퀘스트 보상이 없습니다.", MessageType.None );
            }
            else
            {
                for ( int index = 0; index < rewardEntryList.Count; index++ )
                {
                    CQuestRewardEntry rewardEntry = rewardEntryList[ index ];
                    DrawRewardEntry( rewardEntryList, rewardEntry, index );
                }
            }

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Add EXP Reward" ) )
            {
                AddRewardEntry( eQuestRewardType.EXP );
            }

            if ( GUILayout.Button( "Add Item Reward" ) )
            {
                AddRewardEntry( eQuestRewardType.ITEM );
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 하단 액션 버튼 영역 렌더링
        ///</summary>
                ///<summary>
        /// 조건 엔트리 렌더링
        ///</summary>
        private void DrawConditionEntry( List<CQuestConditionEntry> _conditionEntryList, CQuestConditionEntry _conditionEntry, int _conditionIndex )
        {
            if ( _conditionEntryList == null || _conditionEntry == null )
            {
                return;
            }

            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Condition { _conditionIndex + 1 }", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Remove", GUILayout.Width( 90.0f ) ) )
            {
                _conditionEntryList.RemoveAt( _conditionIndex );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            string conditionId = EditorGUILayout.TextField( "Condition Id", _conditionEntry.GetConditionId() );
            _conditionEntry.SetConditionId( conditionId );
            eQuestConditionType conditionType = ( eQuestConditionType )EditorGUILayout.EnumPopup( "Condition Type", _conditionEntry.GetConditionType() );
            _conditionEntry.SetConditionType( conditionType );

            switch ( conditionType )
            {
                case eQuestConditionType.KILL_MONSTER:
                    DrawKillMonsterConditionFields( _conditionEntry );
                    break;

                case eQuestConditionType.REACH_LEVEL:
                    DrawReachLevelConditionFields( _conditionEntry );
                    break;

                case eQuestConditionType.TURN_IN_ITEM:
                    DrawTurnInItemConditionFields( _conditionEntry );
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 몬스터 처치 조건 입력 렌더링
        ///</summary>
        private void DrawKillMonsterConditionFields( CQuestConditionEntry _conditionEntry )
        {
            string targetMonsterId = EditorGUILayout.TextField( "Target Monster Id", _conditionEntry.GetTargetMonsterId() );
            _conditionEntry.SetTargetMonsterId( targetMonsterId );
            string resolvedMonsterName = ResolveMonsterDisplayName( _conditionEntry.GetTargetMonsterId() );
            EditorGUILayout.LabelField( "Monster Name", resolvedMonsterName );
            int requiredKillCount = EditorGUILayout.IntField( "Required Kill Count", _conditionEntry.GetRequiredKillCount() );
            _conditionEntry.SetRequiredKillCount( requiredKillCount );
            DrawMonsterSearchFoldout( _conditionEntry );
        }

        ///<summary>
        /// 레벨 달성 조건 입력 렌더링
        ///</summary>
        private void DrawReachLevelConditionFields( CQuestConditionEntry _conditionEntry )
        {
            int requiredLevel = EditorGUILayout.IntField( "Required Level", _conditionEntry.GetRequiredLevel() );
            _conditionEntry.SetRequiredLevel( requiredLevel );
            EditorGUILayout.HelpBox( "퀘스트 수락 후 이미 레벨 조건을 만족한 경우도 완료 대상으로 처리됩니다.", MessageType.None );
        }

        ///<summary>
        /// 아이템 전달 조건 입력 렌더링
        ///</summary>
        private void DrawTurnInItemConditionFields( CQuestConditionEntry _conditionEntry )
        {
            CItemDefinition targetItemDefinition = ( CItemDefinition )EditorGUILayout.ObjectField( "Target Item", _conditionEntry.GetTargetItemDefinition(), typeof( CItemDefinition ), false );
            _conditionEntry.SetTargetItemDefinition( targetItemDefinition );
            long requiredItemCount = EditorGUILayout.LongField( "Required Item Count", _conditionEntry.GetRequiredItemCount() );
            _conditionEntry.SetRequiredItemCount( requiredItemCount );

            if ( targetItemDefinition != null )
            {
                EditorGUILayout.LabelField( "Item Name", targetItemDefinition.GetItemName() );
            }
        }

        ///<summary>
        /// 보상 엔트리 렌더링
        ///</summary>
        private void DrawRewardEntry( List<CQuestRewardEntry> _rewardEntryList, CQuestRewardEntry _rewardEntry, int _rewardIndex )
        {
            if ( _rewardEntryList == null || _rewardEntry == null )
            {
                return;
            }

            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Reward { _rewardIndex + 1 }", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Remove", GUILayout.Width( 90.0f ) ) )
            {
                _rewardEntryList.RemoveAt( _rewardIndex );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            eQuestRewardType rewardType = ( eQuestRewardType )EditorGUILayout.EnumPopup( "Reward Type", _rewardEntry.GetRewardType() );
            _rewardEntry.SetRewardType( rewardType );

            switch ( rewardType )
            {
                case eQuestRewardType.EXP:
                    int expAmount = EditorGUILayout.IntField( "EXP Amount", _rewardEntry.GetExpAmount() );
                    _rewardEntry.SetExpAmount( expAmount );
                    EditorGUILayout.HelpBox( "경험치 보상은 인벤토리 아이템이 아니라 플레이어 경험치에 직접 반영됩니다.", MessageType.None );
                    break;

                case eQuestRewardType.ITEM:
                    CItemDefinition itemDefinition = ( CItemDefinition )EditorGUILayout.ObjectField( "Item Definition", _rewardEntry.GetItemDefinition(), typeof( CItemDefinition ), false );
                    _rewardEntry.SetItemDefinition( itemDefinition );
                    long itemCount = EditorGUILayout.LongField( "Item Count", _rewardEntry.GetItemCount() );
                    _rewardEntry.SetItemCount( itemCount );

                    if ( itemDefinition != null )
                    {
                        EditorGUILayout.LabelField( "Item Name", itemDefinition.GetItemName() );
                    }
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 조건 엔트리 추가
        ///</summary>
        private void AddConditionEntry( eQuestConditionType _conditionType )
        {
            if ( workingQuestDefinition == null )
            {
                return;
            }

            List<CQuestConditionEntry> conditionEntryList = workingQuestDefinition.GetConditionEntryList();

            if ( conditionEntryList == null )
            {
                return;
            }

            CQuestConditionEntry createdConditionEntry = new CQuestConditionEntry();
            createdConditionEntry.SetConditionType( _conditionType );
            createdConditionEntry.SetConditionId( $"COND_{conditionEntryList.Count + 1:00}" );
            conditionEntryList.Add( createdConditionEntry );
        }

        ///<summary>
        /// 보상 엔트리 추가
        ///</summary>
        private void AddRewardEntry( eQuestRewardType _rewardType )
        {
            if ( workingQuestDefinition == null )
            {
                return;
            }

            List<CQuestRewardEntry> rewardEntryList = workingQuestDefinition.GetRewardEntryList();

            if ( rewardEntryList == null )
            {
                return;
            }

            CQuestRewardEntry createdRewardEntry = new CQuestRewardEntry();
            createdRewardEntry.SetRewardType( _rewardType );
            rewardEntryList.Add( createdRewardEntry );
        }

        ///<summary>
        /// 할당된 NPC 정보 렌더링
        ///</summary>
        private void DrawAssignedNpcInfo( string _npcId, string _label )
        {
            QuestNpcSelectionInfo matchedNpcInfo = FindNpcSelectionInfo( _npcId );
            string resolvedNpcName = matchedNpcInfo == null ? "-" : matchedNpcInfo.npcName;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"{_label} Name", resolvedNpcName );

            using ( new EditorGUI.DisabledScope( matchedNpcInfo == null ) )
            {
                if ( GUILayout.Button( "Ping", GUILayout.Width( 80.0f ) ) && matchedNpcInfo != null )
                {
                    UnityEngine.Object npcAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( matchedNpcInfo.assetPath );

                    if ( npcAsset != null )
                    {
                        EditorGUIUtility.PingObject( npcAsset );
                        Selection.activeObject = npcAsset;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 몬스터 검색 폴드아웃 렌더링
        ///</summary>
        private void DrawMonsterSearchFoldout( CQuestConditionEntry _conditionEntry )
        {
            isMonsterSearchFoldout = EditorGUILayout.Foldout( isMonsterSearchFoldout, "Monster Search", true );

            if ( isMonsterSearchFoldout == false )
            {
                return;
            }

            EditorGUI.indentLevel++;
            monsterSearchText = EditorGUILayout.TextField( "Search", monsterSearchText );
            List<QuestMonsterSelectionInfo> filteredMonsterInfoList = GetFilteredMonsterSelectionInfos( monsterSearchText );
            int drawCount = Mathf.Min( filteredMonsterInfoList.Count, 12 );

            for ( int index = 0; index < drawCount; index++ )
            {
                QuestMonsterSelectionInfo monsterInfo = filteredMonsterInfoList[ index ];

                if ( monsterInfo == null )
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal( "box" );
                EditorGUILayout.LabelField( monsterInfo.monsterName, GUILayout.Width( 180.0f ) );
                EditorGUILayout.LabelField( monsterInfo.monsterId );

                if ( GUILayout.Button( "Assign", GUILayout.Width( 80.0f ) ) )
                {
                    _conditionEntry.SetTargetMonsterId( monsterInfo.monsterId );
                }

                EditorGUILayout.EndHorizontal();
            }

            if ( filteredMonsterInfoList.Count > drawCount )
            {
                EditorGUILayout.HelpBox( $"Showing first {drawCount} results. Refine the search text for more precise selection.", MessageType.None );
            }

            if ( filteredMonsterInfoList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "검색된 몬스터가 없습니다.", MessageType.Info );
            }

            EditorGUI.indentLevel--;
        }

        ///<summary>
        /// 하단 저장 액션 영역 렌더링
        ///</summary>
        private void DrawActionButtonSection( CQuestDefinition _selectedQuestDefinition )
        {
            string validationMessage = BuildValidationSummary();

            if ( string.IsNullOrWhiteSpace( validationMessage ) == false )
            {
                EditorGUILayout.HelpBox( validationMessage, MessageType.Warning );
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical( GUILayout.Width( ActionButtonSectionWidth ) );
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Save", GUILayout.Height( 32.0f ) ) )
            {
                SaveWorkingQuestDefinition( _selectedQuestDefinition );
            }

            if ( GUILayout.Button( "Revert", GUILayout.Height( 32.0f ) ) )
            {
                ReloadWorkingQuestDefinition();
            }

            if ( GUILayout.Button( "Duplicate", GUILayout.Height( 32.0f ) ) )
            {
                DuplicateQuestDefinition( _selectedQuestDefinition );
            }

            if ( GUILayout.Button( "Delete", GUILayout.Height( 32.0f ) ) )
            {
                DeleteQuestDefinition( _selectedQuestDefinition );
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 작업용 퀘스트 정의 저장
        ///</summary>
        private void SaveWorkingQuestDefinition( CQuestDefinition _selectedQuestDefinition )
        {
            if ( _selectedQuestDefinition == null || workingQuestDefinition == null )
            {
                SetStatus( "저장할 퀘스트 데이터가 없습니다.", MessageType.Warning );
                return;
            }

            NormalizeConditionEntryIds();

            if ( ValidateWorkingQuestDefinition( _selectedQuestDefinition ) == false )
            {
                return;
            }

            EditorUtility.CopySerialized( workingQuestDefinition, _selectedQuestDefinition );
            EditorUtility.SetDirty( _selectedQuestDefinition );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string selectedAssetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );
            RefreshQuestDefinitionInfos();
            RestoreSelectionByAssetPath( selectedAssetPath );
            EnsureAssetRenameDraft( _selectedQuestDefinition, selectedAssetPath );
            SetStatus( "퀘스트 정의를 저장했습니다.", MessageType.Info );
        }

        ///<summary>
        /// 작업용 퀘스트 정의 유효성 검사
        ///</summary>
        private bool ValidateWorkingQuestDefinition( CQuestDefinition _selectedQuestDefinition )
        {
            if ( string.IsNullOrWhiteSpace( workingQuestDefinition.GetQuestId() ) )
            {
                SetStatus( "Quest Id를 입력하세요.", MessageType.Warning );
                return false;
            }

            string normalizedQuestId = workingQuestDefinition.GetQuestId().Trim();
            string selectedAssetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );

            for ( int index = 0; index < questDefinitionInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = questDefinitionInfoList[ index ];

                if ( questInfo == null || string.IsNullOrWhiteSpace( questInfo.questId ) )
                {
                    continue;
                }

                bool isSameAsset = string.Equals( questInfo.assetPath, selectedAssetPath, StringComparison.Ordinal );

                if ( isSameAsset )
                {
                    continue;
                }

                bool isDuplicateQuestId = string.Equals( questInfo.questId, normalizedQuestId, StringComparison.Ordinal );

                if ( isDuplicateQuestId )
                {
                    SetStatus( $"중복된 Quest Id가 있습니다. ({normalizedQuestId})", MessageType.Warning );
                    return false;
                }
            }

            bool isConditionValid = ValidateConditionEntries();

            if ( isConditionValid == false )
            {
                return false;
            }

            bool isRewardValid = ValidateRewardEntries();

            if ( isRewardValid == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 조건 엔트리 유효성 검사
        ///</summary>
        private bool ValidateConditionEntries()
        {
            List<CQuestConditionEntry> conditionEntryList = workingQuestDefinition.GetConditionEntryList();

            if ( conditionEntryList == null || conditionEntryList.Count == 0 )
            {
                SetStatus( "최소 1개 이상의 퀘스트 조건이 필요합니다.", MessageType.Warning );
                return false;
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    SetStatus( $"조건 {index + 1}이 비어 있습니다.", MessageType.Warning );
                    return false;
                }

                switch ( conditionEntry.GetConditionType() )
                {
                    case eQuestConditionType.KILL_MONSTER:
                        if ( string.IsNullOrWhiteSpace( conditionEntry.GetTargetMonsterId() ) )
                        {
                            SetStatus( $"조건 {index + 1}의 Target Monster Id를 입력하세요.", MessageType.Warning );
                            return false;
                        }
                        break;

                    case eQuestConditionType.REACH_LEVEL:
                        if ( conditionEntry.GetRequiredLevel() <= 0 )
                        {
                            SetStatus( $"조건 {index + 1}의 Required Level이 올바르지 않습니다.", MessageType.Warning );
                            return false;
                        }
                        break;

                    case eQuestConditionType.TURN_IN_ITEM:
                        if ( conditionEntry.GetTargetItemDefinition() == null )
                        {
                            SetStatus( $"조건 {index + 1}의 Target Item을 지정하세요.", MessageType.Warning );
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        ///<summary>
        /// 보상 엔트리 유효성 검사
        ///</summary>
        private bool ValidateRewardEntries()
        {
            List<CQuestRewardEntry> rewardEntryList = workingQuestDefinition.GetRewardEntryList();

            if ( rewardEntryList == null || rewardEntryList.Count == 0 )
            {
                SetStatus( "최소 1개 이상의 퀘스트 보상이 필요합니다.", MessageType.Warning );
                return false;
            }

            for ( int index = 0; index < rewardEntryList.Count; index++ )
            {
                CQuestRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    SetStatus( $"보상 {index + 1}이 비어 있습니다.", MessageType.Warning );
                    return false;
                }

                if ( rewardEntry.GetRewardType() == eQuestRewardType.ITEM && rewardEntry.GetItemDefinition() == null )
                {
                    SetStatus( $"보상 {index + 1}의 Item Definition을 지정하세요.", MessageType.Warning );
                    return false;
                }
            }

            return true;
        }

        ///<summary>
        /// 작업용 퀘스트 정의 되돌리기
        ///</summary>
        private void ReloadWorkingQuestDefinition()
        {
            DestroyWorkingQuestDefinition();
            EnsureWorkingQuestDefinitionLoaded();
            SetStatus( "작업용 퀘스트 데이터를 다시 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 새 퀘스트 정의 생성
        ///</summary>
        private void CreateNewQuestDefinition()
        {
            EnsureQuestDefinitionFolderExists();
            string nextQuestId = GenerateNextQuestId( newQuestType );
            string sanitizedAssetName = SanitizeFileName( string.IsNullOrWhiteSpace( newQuestAssetName ) ? $"Quest_{nextQuestId}" : newQuestAssetName );

            if ( string.IsNullOrWhiteSpace( sanitizedAssetName ) )
            {
                SetStatus( "Asset Name을 입력하세요.", MessageType.Warning );
                return;
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath( $"{QuestDefinitionFolderPath}/{sanitizedAssetName}.asset" );
            CQuestDefinition createdQuestDefinition = CreateInstance<CQuestDefinition>();
            createdQuestDefinition.SetQuestType( newQuestType );
            createdQuestDefinition.SetQuestId( nextQuestId );
            createdQuestDefinition.SetQuestName( nextQuestId );
            createdQuestDefinition.SetDescription( DefaultQuestDescriptionTemplate );
            AssetDatabase.CreateAsset( createdQuestDefinition, assetPath );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshQuestDefinitionInfos();
            RestoreSelectionByAssetPath( assetPath );
            ResetNewQuestAssetName();
            SetStatus( $"퀘스트 정의를 생성했습니다. ({Path.GetFileNameWithoutExtension( assetPath )})", MessageType.Info );
        }

        ///<summary>
        /// 누락된 퀘스트 정의 자동 생성
        ///</summary>
        private void AutoCreateMissingQuestDefinitions()
        {
            EnsureQuestDefinitionFolderExists();
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CNPCInteractionData", new string[] { NpcInteractionDataSearchRootPath } );
            HashSet<string> preparedQuestIdSet = new HashSet<string>( StringComparer.Ordinal );
            int createdCount = 0;

            for ( int index = 0; index < questDefinitionInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = questDefinitionInfoList[ index ];

                if ( questInfo == null || string.IsNullOrWhiteSpace( questInfo.questId ) )
                {
                    continue;
                }

                preparedQuestIdSet.Add( questInfo.questId.Trim() );
            }

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuidArray[ index ] );
                CNPCInteractionData interactionData = AssetDatabase.LoadAssetAtPath<CNPCInteractionData>( assetPath );

                if ( interactionData == null )
                {
                    continue;
                }

                List<CNPCInteractionActionEntry> actionEntryList = interactionData.GetActionEntryList();

                if ( actionEntryList == null )
                {
                    continue;
                }

                for ( int actionIndex = 0; actionIndex < actionEntryList.Count; actionIndex++ )
                {
                    CNPCInteractionActionEntry actionEntry = actionEntryList[ actionIndex ];

                    if ( actionEntry == null || actionEntry.GetActionType() != eNPCInteractionAction.QUEST )
                    {
                        continue;
                    }

                    string questId = actionEntry.GetLinkedQuestId();

                    if ( string.IsNullOrWhiteSpace( questId ) )
                    {
                        continue;
                    }

                    string normalizedQuestId = questId.Trim();

                    if ( preparedQuestIdSet.Contains( normalizedQuestId ) )
                    {
                        continue;
                    }

                    CreateAutoQuestDefinitionAsset( normalizedQuestId, interactionData.GetNpcId() );
                    preparedQuestIdSet.Add( normalizedQuestId );
                    createdCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshQuestDefinitionInfos();
            SetStatus( $"누락 퀘스트 정의 {createdCount}개를 생성했습니다.", MessageType.Info );
        }

        ///<summary>
        /// 자동 생성 퀘스트 정의 자산 생성
        ///</summary>
        private void CreateAutoQuestDefinitionAsset( string _questId, string _npcId )
        {
            string sanitizedQuestId = _questId.Trim();
            string sanitizedFileName = SanitizeFileName( $"Quest_{sanitizedQuestId}" );
            string assetPath = AssetDatabase.GenerateUniqueAssetPath( $"{QuestDefinitionFolderPath}/{sanitizedFileName}.asset" );
            CQuestDefinition createdQuestDefinition = CreateInstance<CQuestDefinition>();
            createdQuestDefinition.SetQuestId( sanitizedQuestId );
            createdQuestDefinition.SetQuestName( sanitizedQuestId );
            createdQuestDefinition.SetDescription( DefaultQuestDescriptionTemplate );
            createdQuestDefinition.SetQuestType( ResolveQuestTypeFromQuestId( sanitizedQuestId ) );
            createdQuestDefinition.SetGiverNpcId( _npcId );
            createdQuestDefinition.SetCompleterNpcId( _npcId );
            AssetDatabase.CreateAsset( createdQuestDefinition, assetPath );
        }

        ///<summary>
        /// 퀘스트 정의 복제
        ///</summary>
        private void DuplicateQuestDefinition( CQuestDefinition _selectedQuestDefinition )
        {
            if ( _selectedQuestDefinition == null )
            {
                return;
            }

            string selectedAssetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );
            string selectedAssetName = Path.GetFileNameWithoutExtension( selectedAssetPath );
            string requestedAssetPath = $"{QuestDefinitionFolderPath}/{selectedAssetName}_Copy.asset";
            bool isCopied = TryDuplicateAsset( _selectedQuestDefinition, requestedAssetPath, out string duplicatedAssetPath );

            if ( isCopied == false )
            {
                SetStatus( "퀘스트 정의 복제에 실패했습니다.", MessageType.Warning );
                return;
            }

            RefreshQuestDefinitionInfos();
            RestoreSelectionByAssetPath( duplicatedAssetPath );
            SetStatus( $"퀘스트 정의를 복제했습니다. ({Path.GetFileNameWithoutExtension( duplicatedAssetPath )})", MessageType.Info );
        }

        ///<summary>
        /// 퀘스트 정의 삭제
        ///</summary>
        private void DeleteQuestDefinition( CQuestDefinition _selectedQuestDefinition )
        {
            if ( _selectedQuestDefinition == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );
            bool isConfirmed = EditorUtility.DisplayDialog( "Quest Definition Delete", $"정말 삭제할까요?\n{assetPath}", "Delete", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            bool isDeleted = AssetDatabase.DeleteAsset( assetPath );

            if ( isDeleted == false )
            {
                SetStatus( "퀘스트 정의 삭제에 실패했습니다.", MessageType.Warning );
                return;
            }

            selectedQuestIndex = -1;
            DestroyWorkingQuestDefinition();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshQuestDefinitionInfos();
            SetStatus( "퀘스트 정의를 삭제했습니다.", MessageType.Info );
        }

        ///<summary>
        /// 작업용 퀘스트 정의 로드 보장
        ///</summary>
        private void EnsureWorkingQuestDefinitionLoaded()
        {
            CQuestDefinition selectedQuestDefinition = GetSelectedQuestDefinition();

            if ( selectedQuestDefinition == null )
            {
                DestroyWorkingQuestDefinition();
                return;
            }

            string selectedAssetPath = AssetDatabase.GetAssetPath( selectedQuestDefinition );
            bool isSameAssetLoaded = workingQuestDefinition != null && string.Equals( loadedQuestAssetPath, selectedAssetPath, StringComparison.Ordinal );

            if ( isSameAssetLoaded )
            {
                return;
            }

            DestroyWorkingQuestDefinition();
            workingQuestDefinition = Instantiate( selectedQuestDefinition );
            loadedQuestAssetPath = selectedAssetPath;
        }

        ///<summary>
        /// 작업용 퀘스트 정의 파기
        ///</summary>
        private void DestroyWorkingQuestDefinition()
        {
            if ( workingQuestDefinition != null )
            {
                DestroyImmediate( workingQuestDefinition );
            }

            workingQuestDefinition = null;
            loadedQuestAssetPath = string.Empty;
        }

        ///<summary>
        /// 퀘스트 정의 목록 정보 갱신
        ///</summary>
        private void RefreshQuestDefinitionInfos()
        {
            string selectedAssetPath = GetSelectedQuestAssetPath();
            questDefinitionInfoList.Clear();
            EnsureQuestDefinitionFolderExists();
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CQuestDefinition", new string[] { QuestDefinitionFolderPath } );
            Array.Sort( assetGuidArray, CompareAssetGuid );

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuidArray[ index ] );
                CQuestDefinition questDefinition = AssetDatabase.LoadAssetAtPath<CQuestDefinition>( assetPath );

                if ( questDefinition == null )
                {
                    continue;
                }

                QuestDefinitionInfo questInfo = new QuestDefinitionInfo();
                questInfo.questId = questDefinition.GetQuestId();
                questInfo.questName = questDefinition.GetQuestName();
                questInfo.assetPath = assetPath;
                questInfo.questType = questDefinition.GetQuestType();
                questDefinitionInfoList.Add( questInfo );
            }

            RestoreSelectionByAssetPath( selectedAssetPath );

            if ( questDefinitionInfoList.Count == 0 )
            {
                selectedQuestIndex = -1;
                DestroyWorkingQuestDefinition();
            }

            SetStatus( $"퀘스트 정의 {questDefinitionInfoList.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// NPC 선택 정보 갱신
        ///</summary>
        private void RefreshNpcSelectionInfos()
        {
            questNpcSelectionInfoList.Clear();
            string[] assetGuidArray = AssetDatabase.FindAssets( "t:CNPCInteractionData", new string[] { NpcInteractionDataSearchRootPath } );
            Array.Sort( assetGuidArray, CompareAssetGuid );

            for ( int index = 0; index < assetGuidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuidArray[ index ] );
                CNPCInteractionData interactionData = AssetDatabase.LoadAssetAtPath<CNPCInteractionData>( assetPath );

                if ( interactionData == null )
                {
                    continue;
                }

                QuestNpcSelectionInfo npcInfo = new QuestNpcSelectionInfo();
                npcInfo.npcId = interactionData.GetNpcId();
                npcInfo.npcName = interactionData.GetNpcName();
                npcInfo.assetPath = assetPath;
                questNpcSelectionInfoList.Add( npcInfo );
            }
        }

        ///<summary>
        /// 저장 전 검증 요약 반환
        ///</summary>
        private string BuildValidationSummary()
        {
            if ( workingQuestDefinition == null )
            {
                return "작업 중인 퀘스트 데이터가 없습니다.";
            }

            if ( string.IsNullOrWhiteSpace( workingQuestDefinition.GetQuestId() ) )
            {
                return "Quest Id를 입력하세요.";
            }

            if ( string.IsNullOrWhiteSpace( workingQuestDefinition.GetQuestName() ) )
            {
                return "Quest Name을 입력하세요.";
            }

            List<CQuestConditionEntry> conditionEntryList = workingQuestDefinition.GetConditionEntryList();

            if ( conditionEntryList == null || conditionEntryList.Count == 0 )
            {
                return "퀘스트 조건을 1개 이상 추가하세요.";
            }

            List<CQuestRewardEntry> rewardEntryList = workingQuestDefinition.GetRewardEntryList();

            if ( rewardEntryList == null || rewardEntryList.Count == 0 )
            {
                return "퀘스트 보상을 1개 이상 추가하세요.";
            }

            return string.Empty;
        }

        ///<summary>
        /// 몬스터 선택 정보 갱신
        ///</summary>
        private void RefreshMonsterSelectionInfos()
        {
            questMonsterSelectionInfoList.Clear();
            CMonsterStatTableData monsterStatTableData = AssetDatabase.LoadAssetAtPath<CMonsterStatTableData>( MonsterStatTableAssetPath );

            if ( monsterStatTableData == null )
            {
                return;
            }

            List<CMonsterStatRow> rowList = monsterStatTableData.GetRowList();

            if ( rowList == null )
            {
                return;
            }

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CMonsterStatRow rowData = rowList[ index ];

                if ( rowData == null || string.IsNullOrWhiteSpace( rowData.GetId() ) )
                {
                    continue;
                }

                QuestMonsterSelectionInfo monsterInfo = new QuestMonsterSelectionInfo();
                monsterInfo.monsterId = rowData.GetId();
                monsterInfo.monsterName = string.IsNullOrWhiteSpace( rowData.GetName() ) ? rowData.GetId() : rowData.GetName();
                questMonsterSelectionInfoList.Add( monsterInfo );
            }
        }

        ///<summary>
        /// 퀘스트 필터 목록 반환
        ///</summary>
        private List<QuestDefinitionInfo> GetFilteredQuestDefinitionInfos()
        {
            List<QuestDefinitionInfo> filteredInfoList = new List<QuestDefinitionInfo>();

            for ( int index = 0; index < questDefinitionInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = questDefinitionInfoList[ index ];

                if ( questInfo == null )
                {
                    continue;
                }

                if ( IsQuestTypeFilteredOut( questInfo ) )
                {
                    continue;
                }

                if ( IsSearchMatch( questInfo, searchText ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( questInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// NPC 필터 목록 반환
        ///</summary>
        private List<QuestNpcSelectionInfo> GetFilteredNpcSelectionInfos( string _searchKeyword )
        {
            List<QuestNpcSelectionInfo> filteredInfoList = new List<QuestNpcSelectionInfo>();
            string normalizedKeyword = string.IsNullOrWhiteSpace( _searchKeyword ) ? string.Empty : _searchKeyword.Trim();

            for ( int index = 0; index < questNpcSelectionInfoList.Count; index++ )
            {
                QuestNpcSelectionInfo npcInfo = questNpcSelectionInfoList[ index ];

                if ( npcInfo == null )
                {
                    continue;
                }

                if ( string.IsNullOrWhiteSpace( normalizedKeyword ) == false )
                {
                    bool containsId = string.IsNullOrWhiteSpace( npcInfo.npcId ) == false && npcInfo.npcId.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;
                    bool containsName = string.IsNullOrWhiteSpace( npcInfo.npcName ) == false && npcInfo.npcName.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;

                    if ( containsId == false && containsName == false )
                    {
                        continue;
                    }
                }

                filteredInfoList.Add( npcInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 몬스터 필터 목록 반환
        ///</summary>
        private List<QuestMonsterSelectionInfo> GetFilteredMonsterSelectionInfos( string _searchKeyword )
        {
            List<QuestMonsterSelectionInfo> filteredInfoList = new List<QuestMonsterSelectionInfo>();
            string normalizedKeyword = string.IsNullOrWhiteSpace( _searchKeyword ) ? string.Empty : _searchKeyword.Trim();

            for ( int index = 0; index < questMonsterSelectionInfoList.Count; index++ )
            {
                QuestMonsterSelectionInfo monsterInfo = questMonsterSelectionInfoList[ index ];

                if ( monsterInfo == null )
                {
                    continue;
                }

                if ( string.IsNullOrWhiteSpace( normalizedKeyword ) == false )
                {
                    bool containsId = string.IsNullOrWhiteSpace( monsterInfo.monsterId ) == false && monsterInfo.monsterId.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;
                    bool containsName = string.IsNullOrWhiteSpace( monsterInfo.monsterName ) == false && monsterInfo.monsterName.IndexOf( normalizedKeyword, StringComparison.OrdinalIgnoreCase ) >= 0;

                    if ( containsId == false && containsName == false )
                    {
                        continue;
                    }
                }

                filteredInfoList.Add( monsterInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// NPC 선택 정보 조회
        ///</summary>
        private QuestNpcSelectionInfo FindNpcSelectionInfo( string _npcId )
        {
            if ( string.IsNullOrWhiteSpace( _npcId ) )
            {
                return null;
            }

            for ( int index = 0; index < questNpcSelectionInfoList.Count; index++ )
            {
                QuestNpcSelectionInfo npcInfo = questNpcSelectionInfoList[ index ];

                if ( npcInfo == null || string.IsNullOrWhiteSpace( npcInfo.npcId ) )
                {
                    continue;
                }

                bool isMatched = string.Equals( npcInfo.npcId, _npcId.Trim(), StringComparison.OrdinalIgnoreCase );

                if ( isMatched )
                {
                    return npcInfo;
                }
            }

            return null;
        }

        ///<summary>
        /// 몬스터 표시 이름 반환
        ///</summary>
        private string ResolveMonsterDisplayName( string _monsterId )
        {
            if ( string.IsNullOrWhiteSpace( _monsterId ) )
            {
                return "-";
            }

            for ( int index = 0; index < questMonsterSelectionInfoList.Count; index++ )
            {
                QuestMonsterSelectionInfo monsterInfo = questMonsterSelectionInfoList[ index ];

                if ( monsterInfo == null || string.IsNullOrWhiteSpace( monsterInfo.monsterId ) )
                {
                    continue;
                }

                bool isMatched = string.Equals( monsterInfo.monsterId, _monsterId.Trim(), StringComparison.OrdinalIgnoreCase );

                if ( isMatched )
                {
                    string resolvedName = string.IsNullOrWhiteSpace( monsterInfo.monsterName ) ? monsterInfo.monsterId : monsterInfo.monsterName;
                    return resolvedName;
                }
            }

            return _monsterId.Trim();
        }

        ///<summary>
        /// 퀘스트 타입 필터 제외 여부 반환
        ///</summary>
        private bool IsQuestTypeFilteredOut( QuestDefinitionInfo _questInfo )
        {
            if ( selectedTypeFilterIndex <= 0 )
            {
                return false;
            }

            eQuestType filterType = ( eQuestType )( selectedTypeFilterIndex - 1 );
            bool result = _questInfo.questType != filterType;
            return result;
        }

        ///<summary>
        /// 퀘스트 검색 필터 제외 여부 반환
        ///</summary>
        protected override bool IsSearchMatch( QuestDefinitionInfo _questInfo, string _searchText )
        {
            if ( _questInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( _searchText ) )
            {
                return true;
            }

            string normalizedSearchText = _searchText.Trim();
            bool containsQuestId = string.IsNullOrWhiteSpace( _questInfo.questId ) == false && _questInfo.questId.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            bool containsQuestName = string.IsNullOrWhiteSpace( _questInfo.questName ) == false && _questInfo.questName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            bool result = containsQuestId || containsQuestName;
            return result;
        }

        ///<summary>
        /// 선택 퀘스트 정의 반환
        ///</summary>
        private CQuestDefinition GetSelectedQuestDefinition()
        {
            if ( selectedQuestIndex < 0 || selectedQuestIndex >= questDefinitionInfoList.Count )
            {
                return null;
            }

            QuestDefinitionInfo questInfo = questDefinitionInfoList[ selectedQuestIndex ];

            if ( questInfo == null || string.IsNullOrWhiteSpace( questInfo.assetPath ) )
            {
                return null;
            }

            CQuestDefinition result = AssetDatabase.LoadAssetAtPath<CQuestDefinition>( questInfo.assetPath );
            return result;
        }

        ///<summary>
        /// 선택 퀘스트 자산 경로 반환
        ///</summary>
        private string GetSelectedQuestAssetPath()
        {
            if ( selectedQuestIndex < 0 || selectedQuestIndex >= questDefinitionInfoList.Count )
            {
                return string.Empty;
            }

            QuestDefinitionInfo questInfo = questDefinitionInfoList[ selectedQuestIndex ];
            string result = questInfo != null ? questInfo.assetPath : string.Empty;
            return result;
        }

        ///<summary>
        /// 퀘스트 선택 처리
        ///</summary>
        private void SelectQuestByIndex( int _sourceIndex, int _filteredIndex, int _filteredItemCount )
        {
            bool isValidIndex = _sourceIndex >= 0 && _sourceIndex < questDefinitionInfoList.Count;

            if ( isValidIndex == false )
            {
                return;
            }

            selectedQuestIndex = _sourceIndex;
            DestroyWorkingQuestDefinition();
            CQuestDefinition selectedQuestDefinition = GetSelectedQuestDefinition();

            if ( selectedQuestDefinition != null )
            {
                string assetPath = AssetDatabase.GetAssetPath( selectedQuestDefinition );
                EnsureAssetRenameDraft( selectedQuestDefinition, assetPath );
            }

            isPendingFocusToSelection = _filteredItemCount > 0 && _filteredIndex >= 0 && _filteredIndex < _filteredItemCount;
        }

        ///<summary>
        /// 자산 경로 기준 선택 복원
        ///</summary>
        private void RestoreSelectionByAssetPath( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return;
            }

            for ( int index = 0; index < questDefinitionInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = questDefinitionInfoList[ index ];

                if ( questInfo == null )
                {
                    continue;
                }

                bool isMatched = string.Equals( questInfo.assetPath, _assetPath, StringComparison.Ordinal );

                if ( isMatched == false )
                {
                    continue;
                }

                selectedQuestIndex = index;
                return;
            }
        }

        ///<summary>
        /// 새 퀘스트 자산명 초기화
        ///</summary>
        private void ResetNewQuestAssetName()
        {
            string nextQuestId = GenerateNextQuestId( newQuestType );
            newQuestAssetName = $"Quest_{nextQuestId}";
        }

        ///<summary>
        /// 현재 퀘스트 자산명 초안 보장
        ///</summary>
        private void EnsureAssetRenameDraft( CQuestDefinition _selectedQuestDefinition, string _assetPath )
        {
            if ( _selectedQuestDefinition == null || string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return;
            }

            bool isSameAsset = string.Equals( currentQuestAssetPathDraft, _assetPath, StringComparison.Ordinal );

            if ( isSameAsset )
            {
                return;
            }

            currentQuestAssetPathDraft = _assetPath;
            currentQuestAssetNameDraft = Path.GetFileNameWithoutExtension( _assetPath );
        }

        ///<summary>
        /// 현재 퀘스트 자산명 초안 초기화
        ///</summary>
        private void ResetCurrentQuestAssetNameDraft()
        {
            CQuestDefinition selectedQuestDefinition = GetSelectedQuestDefinition();

            if ( selectedQuestDefinition == null )
            {
                return;
            }

            string questId = selectedQuestDefinition.GetQuestId();
            string fallbackName = string.IsNullOrWhiteSpace( questId ) ? Path.GetFileNameWithoutExtension( AssetDatabase.GetAssetPath( selectedQuestDefinition ) ) : $"Quest_{questId}";
            currentQuestAssetNameDraft = SanitizeFileName( fallbackName );
        }

        ///<summary>
        /// 퀘스트 자산 이름 변경
        ///</summary>
        private void RenameQuestDefinitionAsset( CQuestDefinition _selectedQuestDefinition, string _newAssetName )
        {
            if ( _selectedQuestDefinition == null )
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath( _selectedQuestDefinition );
            string sanitizedAssetName = SanitizeFileName( _newAssetName );

            if ( string.IsNullOrWhiteSpace( sanitizedAssetName ) )
            {
                SetStatus( "변경할 Asset Name을 입력하세요.", MessageType.Warning );
                return;
            }

            string renameError = AssetDatabase.RenameAsset( assetPath, sanitizedAssetName );

            if ( string.IsNullOrWhiteSpace( renameError ) == false )
            {
                SetStatus( $"자산 이름 변경에 실패했습니다. ({renameError})", MessageType.Warning );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string renamedAssetPath = $"{Path.GetDirectoryName( assetPath )?.Replace( '\\', '/' )}/{sanitizedAssetName}.asset";
            RefreshQuestDefinitionInfos();
            RestoreSelectionByAssetPath( renamedAssetPath );
            currentQuestAssetNameDraft = sanitizedAssetName;
            currentQuestAssetPathDraft = renamedAssetPath;
            SetStatus( $"자산 이름을 변경했습니다. ({sanitizedAssetName})", MessageType.Info );
        }

        ///<summary>
        /// 조건 엔트리 ID 정규화
        ///</summary>
        private void NormalizeConditionEntryIds()
        {
            if ( workingQuestDefinition == null )
            {
                return;
            }

            List<CQuestConditionEntry> conditionEntryList = workingQuestDefinition.GetConditionEntryList();

            if ( conditionEntryList == null )
            {
                return;
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    continue;
                }

                string currentConditionId = conditionEntry.GetConditionId();

                if ( string.IsNullOrWhiteSpace( currentConditionId ) == false )
                {
                    continue;
                }

                conditionEntry.SetConditionId( $"COND_{index + 1:00}" );
            }
        }

        ///<summary>
        /// 퀘스트 타입별 다음 ID 생성
        ///</summary>
        private string GenerateNextQuestId( eQuestType _questType )
        {
            string prefix = ResolveQuestIdPrefix( _questType );
            int maxNumber = 0;

            for ( int index = 0; index < questDefinitionInfoList.Count; index++ )
            {
                QuestDefinitionInfo questInfo = questDefinitionInfoList[ index ];

                if ( questInfo == null || string.IsNullOrWhiteSpace( questInfo.questId ) )
                {
                    continue;
                }

                if ( questInfo.questId.StartsWith( prefix, StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                string numberText = questInfo.questId.Substring( prefix.Length );

                if ( int.TryParse( numberText, out int parsedNumber ) == false )
                {
                    continue;
                }

                if ( parsedNumber > maxNumber )
                {
                    maxNumber = parsedNumber;
                }
            }

            int nextNumber = maxNumber + 1;
            string result = $"{prefix}{nextNumber.ToString( $"D{QuestIdNumberDigits}" )}";
            return result;
        }

        ///<summary>
        /// 퀘스트 타입별 ID 접두사 반환
        ///</summary>
        private string ResolveQuestIdPrefix( eQuestType _questType )
        {
            string result = _questType == eQuestType.REPEATABLE ? RepeatableQuestIdPrefix : NormalQuestIdPrefix;
            return result;
        }

        ///<summary>
        /// 퀘스트 ID 기반 퀘스트 타입 결정
        ///</summary>
        private eQuestType ResolveQuestTypeFromQuestId( string _questId )
        {
            if ( string.IsNullOrWhiteSpace( _questId ) == false && _questId.StartsWith( RepeatableQuestIdPrefix, StringComparison.Ordinal ) )
            {
                return eQuestType.REPEATABLE;
            }

            return eQuestType.NORMAL;
        }

        ///<summary>
        /// 퀘스트 정의 폴더 존재 보장
        ///</summary>
        private void EnsureQuestDefinitionFolderExists()
        {
            EnsureFolderPath( "Assets/Resources" );
            EnsureFolderPath( "Assets/Resources/Data" );
            EnsureFolderPath( "Assets/Resources/Data/Quest" );
            EnsureFolderPath( QuestDefinitionFolderPath );
        }

        ///<summary>
        /// 폴더 경로 존재 보장
        ///</summary>
        private void EnsureFolderPath( string _folderPath )
        {
            bool isRootAssets = string.Equals( _folderPath, "Assets", StringComparison.OrdinalIgnoreCase );

            if ( isRootAssets )
            {
                return;
            }

            bool isFolderExists = AssetDatabase.IsValidFolder( _folderPath );

            if ( isFolderExists )
            {
                return;
            }

            int lastSlashIndex = _folderPath.LastIndexOf( '/' );
            string parentFolderPath = _folderPath.Substring( 0, lastSlashIndex );
            string folderName = _folderPath.Substring( lastSlashIndex + 1 );
            EnsureFolderPath( parentFolderPath );
            AssetDatabase.CreateFolder( parentFolderPath, folderName );
        }

        ///<summary>
        /// 파일명 정리 문자열 반환
        ///</summary>
        private string SanitizeFileName( string _sourceText )
        {
            if ( string.IsNullOrWhiteSpace( _sourceText ) )
            {
                return string.Empty;
            }

            string sanitizedText = _sourceText.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for ( int index = 0; index < invalidCharacters.Length; index++ )
            {
                char invalidCharacter = invalidCharacters[ index ];
                sanitizedText = sanitizedText.Replace( invalidCharacter.ToString(), string.Empty );
            }

            string result = sanitizedText;
            return result;
        }

        ///<summary>
        /// 자산 GUID 비교 결과 반환
        ///</summary>
        private int CompareAssetGuid( string _leftGuid, string _rightGuid )
        {
            string leftAssetPath = AssetDatabase.GUIDToAssetPath( _leftGuid );
            string rightAssetPath = AssetDatabase.GUIDToAssetPath( _rightGuid );
            int result = string.Compare( leftAssetPath, rightAssetPath, StringComparison.OrdinalIgnoreCase );
            return result;
        }

        ///<summary>
        /// 퀘스트 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildQuestControlName( int _sourceIndex )
        {
            string result = $"QuestDefinitionItem_{_sourceIndex}";
            return result;
        }

        ///<summary>
        /// 키보드 탐색 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            if ( questDefinitionInfoList.Count == 0 )
            {
                return;
            }

            bool hasDirection = TryGetKeyboardNavigationDirection( out int direction );

            if ( hasDirection == false )
            {
                return;
            }

            int lastIndex = questDefinitionInfoList.Count - 1;
            int nextIndex = Mathf.Clamp( selectedQuestIndex + direction, 0, lastIndex );
            SelectQuestByIndex( nextIndex, nextIndex, questDefinitionInfoList.Count );
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus( string _message, MessageType _messageType )
        {
            statusMessage = _message;
            statusMessageType = _messageType;
            Repaint();
        }
    }
}
