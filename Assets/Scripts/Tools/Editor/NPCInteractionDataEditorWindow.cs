using System;
using System.Collections.Generic;
using System.IO;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// NPC 상호작용 프리팹 정보
    ///</summary>
    [Serializable]
    public sealed class NPCInteractionPrefabInfo
    {
        public string prefabName;
        public string assetPath;
    }

    ///<summary>
    /// NPC 상호작용 데이터 편집 창
    ///</summary>
    public sealed class NPCInteractionDataEditorWindow : EditorWindow
    {
        private const string NpcPrefabFolderPath = "Assets/Resources/Prefabs/Character/NPC";
        private const string InteractionDataFolderPath = "Assets/Data/NPC/InteractionData";
        private const string InteractionRangeObjectName = "InteractionRange";
        private const float InteractionRangeOffsetY = 0.4f;
        private const float MinimumInteractionRangeWidth = 1.6f;
        private const float MinimumInteractionRangeHeight = 1.6f;
        private const float InteractionRangeExtraWidth = 0.8f;
        private const float InteractionRangeExtraHeight = 0.8f;
        private const float PrefabListViewHeight = 420.0f;
        private const float PrefabListItemHeight = 38.0f;
        private const float PrefabListItemSpacing = 4.0f;
        private const int PreviewSize = 220;

        [SerializeField] private List<NPCInteractionPrefabInfo> npcPrefabInfoList = new List<NPCInteractionPrefabInfo>();
        [SerializeField] private int selectedPrefabIndex = -1;
        [SerializeField] private string searchText = string.Empty;

        private Vector2 prefabListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "NPC 프리팹을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private CNPCInteractionData workingInteractionData;

        ///<summary>
        /// 상호작용 데이터 편집 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/NPC Interaction Data Editor" )]
        private static void ShowWindow()
        {
            NPCInteractionDataEditorWindow window = GetWindow<NPCInteractionDataEditorWindow>();
            window.titleContent = new GUIContent( "NPC Interaction Editor" );
            window.minSize = new Vector2( 1120.0f, 780.0f );
            window.Show();
        }

        ///<summary>
        /// NPC 상호작용 데이터 편집 창 열기
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
            RefreshNpcPrefabInfos();
        }

        ///<summary>
        /// 편집 창 비활성화 처리
        ///</summary>
        private void OnDisable()
        {
            DestroyWorkingInteractionData();
        }

        ///<summary>
        /// 편집 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "NPC Interaction Data Editor", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "NPC별 상호작용 액션 엔트리를 구성하고 대화 프리셋을 여러 개 저장합니다.", MessageType.None );
            EditorGUILayout.Space();
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawPrefabListSection();
            DrawEditorSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 툴바 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            string updatedSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( updatedSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = updatedSearchText;
            }

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 120.0f ) ) )
            {
                RefreshNpcPrefabInfos();
            }

            if ( GUILayout.Button( "Sync All", GUILayout.Width( 120.0f ) ) )
            {
                SyncAllNpcPrefabs();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 프리팹 목록 섹션 렌더링
        ///</summary>
        private void DrawPrefabListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( 360.0f ) );
            EditorGUILayout.LabelField( "NPC Prefabs", EditorStyles.boldLabel );
            List<NPCInteractionPrefabInfo> filteredInfoList = GetFilteredNpcPrefabInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfoList.Count}개", MessageType.None );
            prefabListScrollPosition = EditorGUILayout.BeginScrollView( prefabListScrollPosition, GUILayout.Height( PrefabListViewHeight ) );

            for ( int index = 0; index < filteredInfoList.Count; index++ )
            {
                NPCInteractionPrefabInfo prefabInfo = filteredInfoList[ index ];
                DrawPrefabListItem( prefabInfo, filteredInfoList.Count, index );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 프리팹 목록 항목 렌더링
        ///</summary>
        private void DrawPrefabListItem( NPCInteractionPrefabInfo _prefabInfo, int _filteredItemCount, int _filteredIndex )
        {
            if ( _prefabInfo == null )
            {
                return;
            }

            int sourceIndex = npcPrefabInfoList.IndexOf( _prefabInfo );
            bool isSelected = sourceIndex == selectedPrefabIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = PrefabListItemHeight;
            string controlName = BuildPrefabItemControlName( sourceIndex );
            GUI.SetNextControlName( controlName );
            bool wasClicked = GUILayout.Button( _prefabInfo.prefabName, buttonStyle );

            if ( wasClicked )
            {
                SelectPrefabByIndex( sourceIndex, _filteredIndex, _filteredItemCount );
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
        /// 편집 섹션 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView( editorScrollPosition );
            NPCInteractionPrefabInfo selectedInfo = GetSelectedNpcPrefabInfo();

            if ( selectedInfo == null )
            {
                EditorGUILayout.HelpBox( "편집할 NPC 프리팹을 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            EnsureWorkingInteractionDataLoaded();
            DrawPreviewSection( selectedInfo );
            EditorGUILayout.Space();
            DrawDataSection( selectedInfo );
            EditorGUILayout.Space();
            DrawSaveSection( selectedInfo );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 프리뷰 섹션 렌더링
        ///</summary>
        private void DrawPreviewSection( NPCInteractionPrefabInfo _selectedInfo )
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>( _selectedInfo.assetPath );
            Texture previewTexture = AssetPreview.GetAssetPreview( prefabAsset );

            if ( previewTexture == null )
            {
                previewTexture = AssetPreview.GetMiniThumbnail( prefabAsset );
                Repaint();
            }

            Rect previewRect = GUILayoutUtility.GetRect( PreviewSize, PreviewSize, GUILayout.ExpandWidth( false ) );
            EditorGUI.DrawRect( previewRect, new Color( 0.16f, 0.16f, 0.16f, 1.0f ) );

            if ( previewTexture != null )
            {
                GUI.DrawTexture( previewRect, previewTexture, ScaleMode.ScaleToFit );
            }

            EditorGUILayout.LabelField( "Prefab Name", _selectedInfo.prefabName );
            EditorGUILayout.LabelField( "Prefab Path", _selectedInfo.assetPath );
            EditorGUILayout.LabelField( "Interaction Asset Path", GetInteractionDataAssetPath( _selectedInfo.prefabName ) );
        }

        ///<summary>
        /// 데이터 섹션 렌더링
        ///</summary>
        private void DrawDataSection( NPCInteractionPrefabInfo _selectedInfo )
        {
            if ( workingInteractionData == null )
            {
                EditorGUILayout.HelpBox( "상호작용 데이터를 준비하지 못했습니다.", MessageType.Warning );
                return;
            }

            EditorGUILayout.LabelField( "Interaction Settings", EditorStyles.boldLabel );
            string npcId = EditorGUILayout.TextField( "NPC Id", workingInteractionData.GetNpcId() );
            workingInteractionData.SetNpcId( npcId );
            string npcName = EditorGUILayout.TextField( "NPC Name", workingInteractionData.GetNpcName() );
            workingInteractionData.SetNpcName( npcName );
            EditorGUILayout.Space();
            DrawActionEntryList( workingInteractionData.GetActionEntryList() );
        }

        ///<summary>
        /// 액션 엔트리 목록 렌더링
        ///</summary>
        private void DrawActionEntryList( List<CNPCInteractionActionEntry> _actionEntryList )
        {
            EditorGUILayout.LabelField( "Action Entries", EditorStyles.boldLabel );

            if ( _actionEntryList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 액션 엔트리가 없습니다.", MessageType.None );
            }

            for ( int index = 0; index < _actionEntryList.Count; index++ )
            {
                CNPCInteractionActionEntry actionEntry = _actionEntryList[ index ];

                if ( actionEntry == null )
                {
                    actionEntry = new CNPCInteractionActionEntry();
                    _actionEntryList[ index ] = actionEntry;
                }

                DrawActionEntry( _actionEntryList, actionEntry, index );
            }

            if ( GUILayout.Button( "액션 엔트리 추가" ) )
            {
                _actionEntryList.Add( new CNPCInteractionActionEntry() );
            }
        }

        ///<summary>
        /// 액션 엔트리 렌더링
        ///</summary>
        private void DrawActionEntry( List<CNPCInteractionActionEntry> _actionEntryList, CNPCInteractionActionEntry _actionEntry, int _entryIndex )
        {
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Entry {_entryIndex + 1}", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Remove", GUILayout.Width( 80.0f ) ) )
            {
                _actionEntryList.RemoveAt( _entryIndex );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            eNPCInteractionAction actionType = ( eNPCInteractionAction )EditorGUILayout.EnumPopup( "Action", _actionEntry.GetActionType() );
            _actionEntry.SetActionType( actionType );
            bool useDialogue = EditorGUILayout.ToggleLeft( "Use Dialogue", _actionEntry.GetUseDialogue() );
            _actionEntry.SetUseDialogue( useDialogue );

            if ( actionType == eNPCInteractionAction.QUEST )
            {
                string questId = EditorGUILayout.TextField( "Quest Id", _actionEntry.GetLinkedQuestId() );
                _actionEntry.SetLinkedQuestId( questId );
            }

            if ( actionType == eNPCInteractionAction.SHOP )
            {
                string shopId = EditorGUILayout.TextField( "Shop Id", _actionEntry.GetLinkedShopId() );
                _actionEntry.SetLinkedShopId( shopId );
            }

            if ( useDialogue )
            {
                DrawDialoguePresetList( _actionEntry.GetDialoguePresetList() );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 대화 프리셋 목록 렌더링
        ///</summary>
        private void DrawDialoguePresetList( List<CNPCDialoguePreset> _dialoguePresetList )
        {
            EditorGUILayout.Space( 2.0f );
            EditorGUILayout.LabelField( "Dialogue Presets", EditorStyles.boldLabel );

            if ( _dialoguePresetList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 대화 프리셋이 없습니다.", MessageType.None );
            }

            for ( int index = 0; index < _dialoguePresetList.Count; index++ )
            {
                CNPCDialoguePreset dialoguePreset = _dialoguePresetList[ index ];

                if ( dialoguePreset == null )
                {
                    dialoguePreset = new CNPCDialoguePreset();
                    _dialoguePresetList[ index ] = dialoguePreset;
                }

                DrawDialoguePreset( _dialoguePresetList, dialoguePreset, index );
            }

            if ( GUILayout.Button( "대화 프리셋 추가" ) )
            {
                CNPCDialoguePreset createdPreset = new CNPCDialoguePreset();
                createdPreset.SetPresetName( $"Preset {_dialoguePresetList.Count + 1}" );
                _dialoguePresetList.Add( createdPreset );
            }
        }

        ///<summary>
        /// 대화 프리셋 렌더링
        ///</summary>
        private void DrawDialoguePreset( List<CNPCDialoguePreset> _dialoguePresetList, CNPCDialoguePreset _dialoguePreset, int _presetIndex )
        {
            EditorGUILayout.BeginVertical( "box" );
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( $"Preset {_presetIndex + 1}", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Remove", GUILayout.Width( 80.0f ) ) )
            {
                _dialoguePresetList.RemoveAt( _presetIndex );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            string presetName = EditorGUILayout.TextField( "Preset Name", _dialoguePreset.GetPresetName() );
            _dialoguePreset.SetPresetName( presetName );
            DrawDialogueLineList( _dialoguePreset.GetDialogueLineList() );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 대화 라인 목록 렌더링
        ///</summary>
        private void DrawDialogueLineList( List<string> _dialogueLineList )
        {
            EditorGUILayout.LabelField( "Dialogue Lines", EditorStyles.boldLabel );

            if ( _dialogueLineList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "등록된 대화 라인이 없습니다.", MessageType.None );
            }

            for ( int index = 0; index < _dialogueLineList.Count; index++ )
            {
                EditorGUILayout.BeginHorizontal();
                string currentLine = _dialogueLineList[ index ];
                string updatedLine = EditorGUILayout.TextArea( currentLine, GUILayout.MinHeight( 48.0f ) );
                _dialogueLineList[ index ] = updatedLine;

                if ( GUILayout.Button( "X", GUILayout.Width( 24.0f ), GUILayout.Height( 24.0f ) ) )
                {
                    _dialogueLineList.RemoveAt( index );
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }

            if ( GUILayout.Button( "대화 라인 추가" ) )
            {
                _dialogueLineList.Add( string.Empty );
            }
        }

        ///<summary>
        /// 저장 섹션 렌더링
        ///</summary>
        private void DrawSaveSection( NPCInteractionPrefabInfo _selectedInfo )
        {
            EditorGUILayout.LabelField( "Save", EditorStyles.boldLabel );
            string validationMessage = ValidateWorkingInteractionData();

            if ( string.IsNullOrEmpty( validationMessage ) == false )
            {
                EditorGUILayout.HelpBox( validationMessage, MessageType.Warning );
            }

            using ( new EditorGUI.DisabledScope( string.IsNullOrEmpty( validationMessage ) == false ) )
            {
                if ( GUILayout.Button( "상호작용 데이터 저장", GUILayout.Height( 36.0f ) ) )
                {
                    SaveInteractionData( _selectedInfo );
                }
            }

            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        ///<summary>
        /// NPC 프리팹 목록 갱신
        ///</summary>
        private void RefreshNpcPrefabInfos()
        {
            npcPrefabInfoList.Clear();

            if ( AssetDatabase.IsValidFolder( NpcPrefabFolderPath ) )
            {
                string[] assetGuidArray = AssetDatabase.FindAssets( "t:Prefab", new string[] { NpcPrefabFolderPath } );
                Array.Sort( assetGuidArray, CompareAssetGuid );

                for ( int index = 0; index < assetGuidArray.Length; index++ )
                {
                    string assetGuid = assetGuidArray[ index ];
                    string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                    string prefabName = Path.GetFileNameWithoutExtension( assetPath );
                    NPCInteractionPrefabInfo prefabInfo = new NPCInteractionPrefabInfo();
                    prefabInfo.prefabName = prefabName;
                    prefabInfo.assetPath = assetPath;
                    npcPrefabInfoList.Add( prefabInfo );
                }
            }

            if ( npcPrefabInfoList.Count == 0 )
            {
                selectedPrefabIndex = -1;
                DestroyWorkingInteractionData();
                SetStatus( "NPC 프리팹을 찾지 못했습니다.", MessageType.Warning );
                return;
            }

            if ( selectedPrefabIndex < 0 || selectedPrefabIndex >= npcPrefabInfoList.Count )
            {
                selectedPrefabIndex = 0;
            }

            LoadWorkingInteractionDataForSelection();
            SetStatus( $"NPC 프리팹 {npcPrefabInfoList.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 모든 NPC 프리팹 동기화
        ///</summary>
        private void SyncAllNpcPrefabs()
        {
            RefreshNpcPrefabInfos();

            for ( int index = 0; index < npcPrefabInfoList.Count; index++ )
            {
                NPCInteractionPrefabInfo prefabInfo = npcPrefabInfoList[ index ];

                if ( prefabInfo == null )
                {
                    continue;
                }

                EnsureInteractionDataFolderExists();
                CNPCInteractionData interactionData = EnsureInteractionDataAsset( prefabInfo.prefabName );
                AssignInteractionDataToPrefab( prefabInfo.assetPath, interactionData );
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshNpcPrefabInfos();
            SetStatus( "모든 NPC 프리팹 동기화를 완료했습니다.", MessageType.Info );
        }

        ///<summary>
        /// 선택 프리팹 상호작용 데이터 준비
        ///</summary>
        private void EnsureWorkingInteractionDataLoaded()
        {
            if ( workingInteractionData != null )
            {
                return;
            }

            LoadWorkingInteractionDataForSelection();
        }

        ///<summary>
        /// 선택 프리팹 상호작용 데이터 로드
        ///</summary>
        private void LoadWorkingInteractionDataForSelection()
        {
            DestroyWorkingInteractionData();
            NPCInteractionPrefabInfo selectedInfo = GetSelectedNpcPrefabInfo();

            if ( selectedInfo == null )
            {
                return;
            }

            string assetPath = GetInteractionDataAssetPath( selectedInfo.prefabName );
            CNPCInteractionData savedInteractionData = AssetDatabase.LoadAssetAtPath<CNPCInteractionData>( assetPath );
            workingInteractionData = ScriptableObject.CreateInstance<CNPCInteractionData>();
            workingInteractionData.hideFlags = HideFlags.HideAndDontSave;

            if ( savedInteractionData != null )
            {
                string serializedJson = EditorJsonUtility.ToJson( savedInteractionData );
                EditorJsonUtility.FromJsonOverwrite( serializedJson, workingInteractionData );
            }
            else
            {
                InitializeDefaultInteractionData( workingInteractionData, selectedInfo.prefabName );
            }

            if ( string.IsNullOrWhiteSpace( workingInteractionData.GetNpcId() ) )
            {
                workingInteractionData.SetNpcId( selectedInfo.prefabName );
            }

            if ( string.IsNullOrWhiteSpace( workingInteractionData.GetNpcName() ) )
            {
                workingInteractionData.SetNpcName( selectedInfo.prefabName );
            }
        }

        ///<summary>
        /// 임시 상호작용 데이터 정리
        ///</summary>
        private void DestroyWorkingInteractionData()
        {
            if ( workingInteractionData == null )
            {
                return;
            }

            DestroyImmediate( workingInteractionData );
            workingInteractionData = null;
        }

        ///<summary>
        /// 기본 상호작용 데이터 초기화
        ///</summary>
        private void InitializeDefaultInteractionData( CNPCInteractionData _interactionData, string _prefabName )
        {
            if ( _interactionData == null )
            {
                return;
            }

            _interactionData.SetNpcId( _prefabName );
            _interactionData.SetNpcName( _prefabName );
            List<CNPCInteractionActionEntry> actionEntryList = _interactionData.GetActionEntryList();
            actionEntryList.Clear();

            CNPCInteractionActionEntry defaultEntry = new CNPCInteractionActionEntry();
            defaultEntry.SetActionType( eNPCInteractionAction.DIALOGUE );
            defaultEntry.SetUseDialogue( true );
            CNPCDialoguePreset defaultPreset = new CNPCDialoguePreset();
            defaultPreset.SetPresetName( "기본 대화" );
            List<string> dialogueLineList = defaultPreset.GetDialogueLineList();
            dialogueLineList.Add( "안녕하세요." );
            defaultEntry.GetDialoguePresetList().Add( defaultPreset );
            actionEntryList.Add( defaultEntry );
        }

        ///<summary>
        /// 상호작용 데이터 저장
        ///</summary>
        private void SaveInteractionData( NPCInteractionPrefabInfo _selectedInfo )
        {
            if ( _selectedInfo == null || workingInteractionData == null )
            {
                SetStatus( "저장할 상호작용 데이터가 없습니다.", MessageType.Warning );
                return;
            }

            string validationMessage = ValidateWorkingInteractionData();

            if ( string.IsNullOrEmpty( validationMessage ) == false )
            {
                SetStatus( validationMessage, MessageType.Warning );
                return;
            }

            EnsureInteractionDataFolderExists();
            string assetPath = GetInteractionDataAssetPath( _selectedInfo.prefabName );
            CNPCInteractionData savedInteractionData = SaveOrUpdateInteractionDataAsset( assetPath );

            if ( savedInteractionData == null )
            {
                SetStatus( "상호작용 데이터 자산 저장에 실패했습니다.", MessageType.Error );
                return;
            }

            bool isAssigned = AssignInteractionDataToPrefab( _selectedInfo.assetPath, savedInteractionData );

            if ( isAssigned == false )
            {
                SetStatus( "상호작용 데이터는 저장됐지만 프리팹 연결에 실패했습니다.", MessageType.Warning );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus( $"상호작용 데이터 저장 완료: {assetPath}", MessageType.Info );
        }

        ///<summary>
        /// 상호작용 데이터 자산 저장 또는 갱신
        ///</summary>
        private CNPCInteractionData SaveOrUpdateInteractionDataAsset( string _assetPath )
        {
            CNPCInteractionData savedInteractionData = AssetDatabase.LoadAssetAtPath<CNPCInteractionData>( _assetPath );

            if ( savedInteractionData == null )
            {
                savedInteractionData = ScriptableObject.CreateInstance<CNPCInteractionData>();
                EditorJsonUtility.FromJsonOverwrite( EditorJsonUtility.ToJson( workingInteractionData ), savedInteractionData );
                AssetDatabase.CreateAsset( savedInteractionData, _assetPath );
                return savedInteractionData;
            }

            EditorJsonUtility.FromJsonOverwrite( EditorJsonUtility.ToJson( workingInteractionData ), savedInteractionData );
            EditorUtility.SetDirty( savedInteractionData );
            return savedInteractionData;
        }

        ///<summary>
        /// 상호작용 데이터 자산 보장
        ///</summary>
        private CNPCInteractionData EnsureInteractionDataAsset( string _prefabName )
        {
            string assetPath = GetInteractionDataAssetPath( _prefabName );
            CNPCInteractionData interactionData = AssetDatabase.LoadAssetAtPath<CNPCInteractionData>( assetPath );

            if ( interactionData != null )
            {
                return interactionData;
            }

            interactionData = ScriptableObject.CreateInstance<CNPCInteractionData>();
            interactionData.SetNpcId( _prefabName );
            interactionData.SetNpcName( _prefabName );
            AssetDatabase.CreateAsset( interactionData, assetPath );
            return interactionData;
        }

        ///<summary>
        /// 상호작용 데이터 프리팹 연결
        ///</summary>
        private bool AssignInteractionDataToPrefab( string _prefabAssetPath, CNPCInteractionData _interactionData )
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( _prefabAssetPath );

            try
            {
                CNPCObject npcObject = prefabRoot.GetComponent<CNPCObject>();

                if ( npcObject == null )
                {
                    npcObject = prefabRoot.AddComponent<CNPCObject>();
                }

                BoxCollider2D bodyCollider = prefabRoot.GetComponent<BoxCollider2D>();

                if ( bodyCollider == null )
                {
                    bodyCollider = prefabRoot.AddComponent<BoxCollider2D>();
                }

                Transform interactionRangeTransform = prefabRoot.transform.Find( InteractionRangeObjectName );

                if ( interactionRangeTransform == null )
                {
                    GameObject interactionRangeObject = new GameObject( InteractionRangeObjectName );
                    Transform createdTransform = interactionRangeObject.transform;
                    createdTransform.SetParent( prefabRoot.transform, false );
                    interactionRangeTransform = createdTransform;
                }

                BoxCollider2D interactionRangeCollider = interactionRangeTransform.GetComponent<BoxCollider2D>();

                if ( interactionRangeCollider == null )
                {
                    interactionRangeCollider = interactionRangeTransform.gameObject.AddComponent<BoxCollider2D>();
                }

                interactionRangeCollider.isTrigger = true;
                interactionRangeCollider.offset = new Vector2( 0.0f, InteractionRangeOffsetY );
                interactionRangeCollider.size = ResolveInteractionRangeSize( bodyCollider );

                CNPCInteractionRange interactionRange = interactionRangeTransform.GetComponent<CNPCInteractionRange>();

                if ( interactionRange == null )
                {
                    interactionRange = interactionRangeTransform.gameObject.AddComponent<CNPCInteractionRange>();
                }

                interactionRange.ConfigureRange( npcObject, interactionRangeCollider );

                SerializedObject serializedObject = new SerializedObject( npcObject );
                SerializedProperty npcIdProperty = serializedObject.FindProperty( "npcId" );
                SerializedProperty npcNameProperty = serializedObject.FindProperty( "npcName" );
                SerializedProperty interactionDataProperty = serializedObject.FindProperty( "interactionData" );
                SerializedProperty bodyColliderProperty = serializedObject.FindProperty( "bodyCollider" );
                SerializedProperty interactionRangeColliderProperty = serializedObject.FindProperty( "interactionRangeCollider" );

                if ( npcIdProperty != null )
                {
                    string npcId = string.IsNullOrWhiteSpace( _interactionData.GetNpcId() ) ? prefabRoot.name : _interactionData.GetNpcId();
                    npcIdProperty.stringValue = npcId;
                }

                if ( npcNameProperty != null )
                {
                    string npcName = string.IsNullOrWhiteSpace( _interactionData.GetNpcName() ) ? prefabRoot.name : _interactionData.GetNpcName();
                    npcNameProperty.stringValue = npcName;
                }

                if ( interactionDataProperty != null )
                {
                    interactionDataProperty.objectReferenceValue = _interactionData;
                }

                if ( bodyColliderProperty != null )
                {
                    bodyColliderProperty.objectReferenceValue = bodyCollider;
                }

                if ( interactionRangeColliderProperty != null )
                {
                    interactionRangeColliderProperty.objectReferenceValue = interactionRangeCollider;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset( prefabRoot, _prefabAssetPath );
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( prefabRoot );
            }
        }

        ///<summary>
        /// 편집 데이터 검증
        ///</summary>
        private string ValidateWorkingInteractionData()
        {
            if ( workingInteractionData == null )
            {
                return "상호작용 데이터를 준비하지 못했습니다.";
            }

            if ( string.IsNullOrWhiteSpace( workingInteractionData.GetNpcId() ) )
            {
                return "NPC Id를 입력하세요.";
            }

            if ( string.IsNullOrWhiteSpace( workingInteractionData.GetNpcName() ) )
            {
                return "NPC Name을 입력하세요.";
            }

            List<CNPCInteractionActionEntry> actionEntryList = workingInteractionData.GetActionEntryList();

            if ( actionEntryList == null || actionEntryList.Count == 0 )
            {
                return "액션 엔트리를 하나 이상 추가하세요.";
            }

            for ( int index = 0; index < actionEntryList.Count; index++ )
            {
                CNPCInteractionActionEntry actionEntry = actionEntryList[ index ];

                if ( actionEntry == null )
                {
                    return $"Entry {index + 1} 데이터가 비어 있습니다.";
                }

                if ( actionEntry.GetUseDialogue() )
                {
                    string dialogueValidationMessage = ValidateDialoguePresetList( actionEntry.GetDialoguePresetList(), index );

                    if ( string.IsNullOrEmpty( dialogueValidationMessage ) == false )
                    {
                        return dialogueValidationMessage;
                    }
                }

                if ( actionEntry.GetActionType() == eNPCInteractionAction.QUEST && string.IsNullOrWhiteSpace( actionEntry.GetLinkedQuestId() ) )
                {
                    return $"Entry {index + 1} 퀘스트 ID를 입력하세요.";
                }

                if ( actionEntry.GetActionType() == eNPCInteractionAction.SHOP && string.IsNullOrWhiteSpace( actionEntry.GetLinkedShopId() ) )
                {
                    return $"Entry {index + 1} 상점 ID를 입력하세요.";
                }
            }

            return string.Empty;
        }

        ///<summary>
        /// 대화 프리셋 목록 검증
        ///</summary>
        private string ValidateDialoguePresetList( List<CNPCDialoguePreset> _dialoguePresetList, int _entryIndex )
        {
            if ( _dialoguePresetList == null || _dialoguePresetList.Count == 0 )
            {
                return $"Entry {_entryIndex + 1} 대화 프리셋을 하나 이상 추가하세요.";
            }

            for ( int presetIndex = 0; presetIndex < _dialoguePresetList.Count; presetIndex++ )
            {
                CNPCDialoguePreset dialoguePreset = _dialoguePresetList[ presetIndex ];

                if ( dialoguePreset == null )
                {
                    return $"Entry {_entryIndex + 1} Preset {presetIndex + 1} 데이터가 비어 있습니다.";
                }

                List<string> dialogueLineList = dialoguePreset.GetDialogueLineList();

                if ( dialogueLineList == null || dialogueLineList.Count == 0 )
                {
                    return $"Entry {_entryIndex + 1} Preset {presetIndex + 1} 대화 라인을 하나 이상 추가하세요.";
                }

                for ( int lineIndex = 0; lineIndex < dialogueLineList.Count; lineIndex++ )
                {
                    string dialogueLine = dialogueLineList[ lineIndex ];

                    if ( string.IsNullOrWhiteSpace( dialogueLine ) )
                    {
                        return $"Entry {_entryIndex + 1} Preset {presetIndex + 1} Line {lineIndex + 1} 내용을 입력하세요.";
                    }
                }
            }

            return string.Empty;
        }

        ///<summary>
        /// 상호작용 데이터 자산 경로 반환
        ///</summary>
        private string GetInteractionDataAssetPath( string _prefabName )
        {
            string result = $"{InteractionDataFolderPath}/{_prefabName}_InteractionData.asset";
            return result;
        }

        ///<summary>
        /// 상호작용 데이터 폴더 생성 보장
        ///</summary>
        private void EnsureInteractionDataFolderExists()
        {
            if ( AssetDatabase.IsValidFolder( InteractionDataFolderPath ) )
            {
                return;
            }

            string[] folderSegmentArray = InteractionDataFolderPath.Split( '/' );
            string currentPath = folderSegmentArray[ 0 ];

            for ( int index = 1; index < folderSegmentArray.Length; index++ )
            {
                string folderName = folderSegmentArray[ index ];
                string combinedPath = $"{currentPath}/{folderName}";

                if ( AssetDatabase.IsValidFolder( combinedPath ) == false )
                {
                    AssetDatabase.CreateFolder( currentPath, folderName );
                }

                currentPath = combinedPath;
            }
        }

        ///<summary>
        /// 프리팹 상호작용 범위 크기 결정
        ///</summary>
        private Vector2 ResolveInteractionRangeSize( BoxCollider2D _bodyCollider )
        {
            if ( _bodyCollider == null )
            {
                Vector2 fallbackSize = new Vector2( MinimumInteractionRangeWidth, MinimumInteractionRangeHeight );
                return fallbackSize;
            }

            Vector2 bodySize = _bodyCollider.size;
            float width = Mathf.Max( MinimumInteractionRangeWidth, bodySize.x + InteractionRangeExtraWidth );
            float height = Mathf.Max( MinimumInteractionRangeHeight, bodySize.y + InteractionRangeExtraHeight );
            Vector2 result = new Vector2( width, height );
            return result;
        }

        ///<summary>
        /// 자산 GUID 비교
        ///</summary>
        private int CompareAssetGuid( string _leftGuid, string _rightGuid )
        {
            string leftPath = AssetDatabase.GUIDToAssetPath( _leftGuid );
            string rightPath = AssetDatabase.GUIDToAssetPath( _rightGuid );
            int result = string.Compare( leftPath, rightPath, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 필터 적용 프리팹 목록 반환
        ///</summary>
        private List<NPCInteractionPrefabInfo> GetFilteredNpcPrefabInfos()
        {
            List<NPCInteractionPrefabInfo> filteredInfoList = new List<NPCInteractionPrefabInfo>();

            for ( int index = 0; index < npcPrefabInfoList.Count; index++ )
            {
                NPCInteractionPrefabInfo prefabInfo = npcPrefabInfoList[ index ];

                if ( IsMatchedSearch( prefabInfo ) == false )
                {
                    continue;
                }

                filteredInfoList.Add( prefabInfo );
            }

            return filteredInfoList;
        }

        ///<summary>
        /// 검색 일치 여부 반환
        ///</summary>
        private bool IsMatchedSearch( NPCInteractionPrefabInfo _prefabInfo )
        {
            if ( _prefabInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();
            bool isMatched = _prefabInfo.prefabName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return isMatched;
        }

        ///<summary>
        /// 선택 프리팹 정보 반환
        ///</summary>
        private NPCInteractionPrefabInfo GetSelectedNpcPrefabInfo()
        {
            if ( selectedPrefabIndex < 0 || selectedPrefabIndex >= npcPrefabInfoList.Count )
            {
                return null;
            }

            NPCInteractionPrefabInfo result = npcPrefabInfoList[ selectedPrefabIndex ];
            return result;
        }

        ///<summary>
        /// 프리팹 선택 처리
        ///</summary>
        private void SelectPrefabByIndex( int _sourceIndex, int _filteredIndex, int _filteredItemCount )
        {
            if ( _sourceIndex < 0 || _sourceIndex >= npcPrefabInfoList.Count )
            {
                return;
            }

            selectedPrefabIndex = _sourceIndex;
            isPendingFocusToSelection = true;
            EnsureSelectionVisibleByIndex( _filteredIndex, _filteredItemCount );
            LoadWorkingInteractionDataForSelection();
            Repaint();
        }

        ///<summary>
        /// 선택 항목 스크롤 보정
        ///</summary>
        private void EnsureSelectionVisibleByIndex( int _filteredSelectedIndex, int _filteredItemCount )
        {
            float itemStride = PrefabListItemHeight + PrefabListItemSpacing;
            float itemTop = _filteredSelectedIndex * itemStride;
            float itemBottom = itemTop + PrefabListItemHeight;
            float contentHeight = Mathf.Max( 0.0f, _filteredItemCount * itemStride );
            float maxScrollY = Mathf.Max( 0.0f, contentHeight - PrefabListViewHeight );

            if ( itemTop < prefabListScrollPosition.y )
            {
                prefabListScrollPosition.y = itemTop;
            }
            else if ( itemBottom > prefabListScrollPosition.y + PrefabListViewHeight )
            {
                prefabListScrollPosition.y = itemBottom - PrefabListViewHeight;
            }

            prefabListScrollPosition.y = Mathf.Clamp( prefabListScrollPosition.y, 0.0f, maxScrollY );
        }

        ///<summary>
        /// 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildPrefabItemControlName( int _sourceIndex )
        {
            string result = $"NPCInteractionPrefabItem_{_sourceIndex}";
            return result;
        }

        ///<summary>
        /// 키보드 선택 이동 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            Event currentEvent = Event.current;

            if ( currentEvent == null )
            {
                return;
            }

            if ( EditorGUIUtility.editingTextField )
            {
                return;
            }

            if ( currentEvent.type != EventType.KeyDown )
            {
                return;
            }

            if ( currentEvent.keyCode == KeyCode.DownArrow )
            {
                MoveSelectionInFilteredList( 1 );
                currentEvent.Use();
                return;
            }

            if ( currentEvent.keyCode == KeyCode.UpArrow )
            {
                MoveSelectionInFilteredList( -1 );
                currentEvent.Use();
            }
        }

        ///<summary>
        /// 필터 목록 선택 이동
        ///</summary>
        private void MoveSelectionInFilteredList( int _direction )
        {
            List<NPCInteractionPrefabInfo> filteredInfoList = GetFilteredNpcPrefabInfos();

            if ( filteredInfoList.Count == 0 )
            {
                return;
            }

            int filteredSelectedIndex = 0;
            NPCInteractionPrefabInfo selectedInfo = GetSelectedNpcPrefabInfo();

            if ( selectedInfo != null )
            {
                int resolvedIndex = filteredInfoList.IndexOf( selectedInfo );

                if ( resolvedIndex >= 0 )
                {
                    filteredSelectedIndex = resolvedIndex;
                }
            }

            int lastIndex = filteredInfoList.Count - 1;
            int nextFilteredIndex = Mathf.Clamp( filteredSelectedIndex + _direction, 0, lastIndex );
            NPCInteractionPrefabInfo nextInfo = filteredInfoList[ nextFilteredIndex ];
            int sourceIndex = npcPrefabInfoList.IndexOf( nextInfo );
            SelectPrefabByIndex( sourceIndex, nextFilteredIndex, filteredInfoList.Count );
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus( string _message, MessageType _messageType )
        {
            statusMessage = _message;
            statusMessageType = _messageType;
        }
    }
}
