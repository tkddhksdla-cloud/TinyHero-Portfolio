using System;
using System.Collections.Generic;
using System.IO;
using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 몬스터 행동 패턴 편집 대상 정보
    ///</summary>
    [Serializable]
    public sealed class MonsterBehaviorPrefabInfo
    {
        public string prefabName;
        public string assetPath;
    }

    ///<summary>
    /// 몬스터 드랍 설정 데이터
    ///</summary>
    [Serializable]
    public sealed class MonsterBehaviorDropSetting
    {
        public CItemDefinition itemDefinition;
        public float dropChance = 1.0f;
        public int minDropCount = 1;
        public int maxDropCount = 1;
    }

    ///<summary>
    /// 몬스터 행동 패턴 편집 에디터 창
    ///</summary>
    public sealed class MonsterBehaviorPatternEditorWindow : CEditorToolWindowBase<MonsterBehaviorPrefabInfo>
    {
        private const string MonsterPrefabFolderPath = "Assets/Resources/Prefabs/Character/Monster";
        private const string BehaviorPatternFolderPath = "Assets/Data/Monster/BehaviorPatterns";
        private const float PrefabListViewHeight = 420.0f;
        private const float PrefabListItemHeight = 38.0f;
        private const float PrefabListItemSpacing = 4.0f;
        private const int PreviewSize = 220;

        private static readonly eMonsterBehaviorAction[] AlwaysAllowedActions =
        {
            eMonsterBehaviorAction.IDLE,
            eMonsterBehaviorAction.WANDER,
            eMonsterBehaviorAction.TELEPORT_TO_PLAYER
        };

        private static readonly eMonsterBehaviorAction[] PlayerDistanceAllowedActions =
        {
            eMonsterBehaviorAction.TRACE_PLAYER,
            eMonsterBehaviorAction.LOOK_PLAYER
        };

        private static readonly eMonsterBehaviorAction[] AttackAllowedActions =
        {
            eMonsterBehaviorAction.ATTACK,
            eMonsterBehaviorAction.SKILL
        };

        [SerializeField] private List<MonsterBehaviorPrefabInfo> monsterPrefabInfos = new List<MonsterBehaviorPrefabInfo>();
        [SerializeField] private int selectedPrefabIndex = -1;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private int selectedCopySourcePrefabIndex = -1;
        [SerializeField] private bool useItemDrop;
        [SerializeField] private List<MonsterBehaviorDropSetting> itemDropSettingList = new List<MonsterBehaviorDropSetting>();

        private Vector2 prefabListScrollPosition;
        private Vector2 editorScrollPosition;
        private string statusMessage = "몬스터 프리팹을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;
        private CMonsterBehaviorPatternData workingPatternData;

        ///<summary>
        /// 행동 패턴 에디터 창 반환
        ///</summary>
        public static MonsterBehaviorPatternEditorWindow OpenWindow()
        {
            MonsterBehaviorPatternEditorWindow window = GetWindow<MonsterBehaviorPatternEditorWindow>();
            window.titleContent = new GUIContent( "Monster Behavior Editor" );
            window.minSize = new Vector2( 1100.0f, 800.0f );
            window.Show();
            return window;
        }

        ///<summary>
        /// 행동 패턴 에디터 창 메뉴 진입점
        ///</summary>
        [MenuItem( "Tools/TinyHero/Monster Behavior Pattern Editor" )]
        private static void ShowWindow()
        {
            OpenWindow();
        }

        ///<summary>
        /// 에디터 창 초기화
        ///</summary>
        private void OnEnable()
        {
            RefreshMonsterPrefabInfos();
        }

        ///<summary>
        /// 에디터 창 정리
        ///</summary>
        private void OnDisable()
        {
            DestroyWorkingPatternData();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            DrawWindowHeader( "Monster Behavior Pattern Editor", "몬스터 프리팹별 ALWAYS, PLAYER_DISTANCE, 공격 패턴을 편집하고 저장합니다." );
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawPrefabListSection();
            DrawEditorSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            string newSearchText = EditorGUILayout.TextField("Search", searchText);

            if (string.Equals(newSearchText, searchText, StringComparison.Ordinal) == false)
            {
                searchText = newSearchText;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(120.0f)))
            {
                RefreshMonsterPrefabInfos();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 프리팹 목록 영역 렌더링
        ///</summary>
        private void DrawPrefabListSection()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(360.0f));
            EditorGUILayout.LabelField("Monster Prefabs", EditorStyles.boldLabel);
            List<MonsterBehaviorPrefabInfo> filteredInfos = GetFilteredMonsterPrefabInfos();
            EditorGUILayout.HelpBox($"검색 결과 {filteredInfos.Count}개", MessageType.None);
            prefabListScrollPosition = EditorGUILayout.BeginScrollView(prefabListScrollPosition, GUILayout.Height(PrefabListViewHeight));

            for (int index = 0; index < filteredInfos.Count; index++)
            {
                MonsterBehaviorPrefabInfo prefabInfo = filteredInfos[index];
                DrawPrefabListItem(prefabInfo, filteredInfos.Count, index);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 프리팹 목록 항목 렌더링
        ///</summary>
        private void DrawPrefabListItem(MonsterBehaviorPrefabInfo _prefabInfo, int _filteredItemCount, int _filteredIndex)
        {
            if (_prefabInfo == null)
            {
                return;
            }

            int sourceIndex = monsterPrefabInfos.IndexOf(_prefabInfo);
            bool isSelected = sourceIndex == selectedPrefabIndex;
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = PrefabListItemHeight;
            string controlName = BuildPrefabItemControlName(sourceIndex);
            GUI.SetNextControlName(controlName);
            bool isClicked = GUILayout.Button(_prefabInfo.prefabName, buttonStyle);

            if (isClicked)
            {
                SelectPrefabByIndex(sourceIndex, _filteredIndex, _filteredItemCount);
            }

            if (isSelected && isPendingFocusToSelection)
            {
                GUI.FocusControl(controlName);
                isPendingFocusToSelection = false;
            }

            if (isSelected)
            {
                Rect itemRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.5f, 0.85f, 0.18f));
            }
        }

        ///<summary>
        /// 편집 영역 렌더링
        ///</summary>
        private void DrawEditorSection()
        {
            EditorGUILayout.BeginVertical();
            editorScrollPosition = EditorGUILayout.BeginScrollView(editorScrollPosition);
            MonsterBehaviorPrefabInfo selectedInfo = GetSelectedMonsterPrefabInfo();

            if (selectedInfo == null)
            {
                EditorGUILayout.HelpBox("행동 패턴을 편집할 몬스터 프리팹을 선택하세요.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            EnsureWorkingPatternDataLoaded();
            DrawPreviewSection(selectedInfo);
            EditorGUILayout.Space();
            DrawPatternEditorSection(selectedInfo);
            EditorGUILayout.Space();
            DrawSaveSection(selectedInfo);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 프리팹 미리보기 영역 렌더링
        ///</summary>
        private void DrawPreviewSection(MonsterBehaviorPrefabInfo _selectedInfo)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(_selectedInfo.assetPath);
            Texture previewTexture = AssetPreview.GetAssetPreview(prefabAsset);

            if (previewTexture == null)
            {
                previewTexture = AssetPreview.GetMiniThumbnail(prefabAsset);
                Repaint();
            }

            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(previewRect, new Color(0.16f, 0.16f, 0.16f, 1.0f));

            if (previewTexture != null)
            {
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.LabelField("Prefab Name", _selectedInfo.prefabName);
            EditorGUILayout.LabelField("Prefab Path", _selectedInfo.assetPath);
            EditorGUILayout.LabelField("Pattern Asset Path", GetBehaviorPatternAssetPath(_selectedInfo.prefabName));
        }

        ///<summary>
        /// 패턴 편집 영역 렌더링
        ///</summary>
///<summary>
/// 패턴 편집 영역 렌더링
///</summary>
private void DrawPatternEditorSection( MonsterBehaviorPrefabInfo _selectedInfo )
{
    if ( workingPatternData == null )
    {
        EditorGUILayout.HelpBox( "패턴 데이터를 준비하지 못했습니다.", MessageType.Warning );
        return;
    }

    DrawPatternCopySection( _selectedInfo );
    EditorGUILayout.Space();
    EditorGUILayout.LabelField( "Pattern Settings", EditorStyles.boldLabel );
    EditorGUILayout.LabelField( "Monster Id", workingPatternData.GetMonsterId() );
    float updatedRespawnDelaySeconds = EditorGUILayout.FloatField( "Respawn Delay Seconds", workingPatternData.GetRespawnDelaySeconds() );
    workingPatternData.SetRespawnDelaySeconds( updatedRespawnDelaySeconds );
    EditorGUILayout.Space();
    DrawPatternColumnsSection();
    EditorGUILayout.Space();
    DrawItemDropSection();
}

///<summary>
/// 행동 패턴 복사 섹션 렌더링
///</summary>
private void DrawPatternCopySection( MonsterBehaviorPrefabInfo _selectedInfo )
{
    EditorGUILayout.BeginVertical( "box" );
    EditorGUILayout.LabelField( "Copy Pattern", EditorStyles.boldLabel );
    string[] copySourceOptionArray = BuildCopySourceOptionArray();
    int popupIndex = ResolveCopySourcePopupIndex();
    int updatedPopupIndex = EditorGUILayout.Popup( "Source Monster", popupIndex, copySourceOptionArray );
    selectedCopySourcePrefabIndex = ResolveCopySourcePrefabIndex( updatedPopupIndex );
    MonsterBehaviorPrefabInfo sourceInfo = GetCopySourcePrefabInfo();
    bool canCopy = sourceInfo != null && _selectedInfo != null && string.Equals( sourceInfo.assetPath, _selectedInfo.assetPath, StringComparison.Ordinal ) == false;

    if ( sourceInfo == null )
    {
        EditorGUILayout.HelpBox( "복사할 몬스터를 선택해 주세요.", MessageType.Info );
    }
    else if ( canCopy == false )
    {
        EditorGUILayout.HelpBox( "현재 선택된 몬스터 자신은 복사 원본으로 사용할 수 없습니다.", MessageType.Warning );
    }
    else
    {
        EditorGUILayout.HelpBox( $"원본 몬스터: {sourceInfo.prefabName}", MessageType.None );
    }

    using ( new EditorGUI.DisabledScope( canCopy == false ) )
    {
        if ( GUILayout.Button( "Copy Behavior Pattern", GUILayout.Height( 30.0f ) ) )
        {
            CopyPatternDataFromSource( _selectedInfo, sourceInfo );
        }
    }

    EditorGUILayout.EndVertical();
}


        ///<summary>
        /// 아이템 드랍 설정 섹션 렌더링
        ///</summary>
        private void DrawItemDropSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Item Drop Settings", EditorStyles.boldLabel);
            bool updatedUseItemDrop = EditorGUILayout.ToggleLeft("Use Item Drop", useItemDrop);
            useItemDrop = updatedUseItemDrop;

            if ( useItemDrop )
            {
                for ( int index = 0; index < itemDropSettingList.Count; index++ )
                {
                    MonsterBehaviorDropSetting dropSetting = itemDropSettingList[ index ];

                    if ( dropSetting == null )
                    {
                        continue;
                    }

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Drop Entry {index + 1}", EditorStyles.boldLabel);

                    if ( GUILayout.Button("Remove", GUILayout.Width(80.0f)) )
                    {
                        itemDropSettingList.RemoveAt( index );
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    dropSetting.itemDefinition = EditorGUILayout.ObjectField("Item", dropSetting.itemDefinition, typeof( CItemDefinition ), false ) as CItemDefinition;
                    dropSetting.dropChance = EditorGUILayout.Slider("Drop Chance", dropSetting.dropChance, 0.0f, 1.0f);
                    dropSetting.minDropCount = EditorGUILayout.IntField("Min Count", dropSetting.minDropCount);
                    dropSetting.maxDropCount = EditorGUILayout.IntField("Max Count", dropSetting.maxDropCount);
                    dropSetting.minDropCount = Mathf.Max( 0, dropSetting.minDropCount );
                    dropSetting.maxDropCount = Mathf.Max( dropSetting.minDropCount, dropSetting.maxDropCount );
                    EditorGUILayout.EndVertical();
                }

                if ( GUILayout.Button("Add Drop Entry") )
                {
                    MonsterBehaviorDropSetting createdDropSetting = new MonsterBehaviorDropSetting();
                    itemDropSettingList.Add( createdDropSetting );
                }
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 패턴 2열 영역 렌더링
        ///</summary>
        private void DrawPatternColumnsSection()
        {
            EditorGUILayout.BeginHorizontal();
            DrawAlwaysPatternSection();
            GUILayout.Space(12.0f);
            DrawPlayerDistancePatternSection();
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 상시 패턴 영역 렌더링
        ///</summary>
        private void DrawAlwaysPatternSection()
        {
            CMonsterAlwaysPatternData alwaysPatternData = workingPatternData.GetAlwaysPatternData();
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("ALWAYS Pattern", EditorStyles.boldLabel);
            DrawActionEntryList(alwaysPatternData.GetActionEntryList(), AlwaysAllowedActions, "ALWAYS 행동", "ALWAYS 행동 추가");
            EditorGUILayout.Space();
            DrawAttackPatternSection(alwaysPatternData.GetAttackPatternData(), "ALWAYS Attack Pattern");
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 거리 패턴 영역 렌더링
        ///</summary>
        private void DrawPlayerDistancePatternSection()
        {
            CMonsterPlayerDistancePatternData playerDistancePatternData = workingPatternData.GetPlayerDistancePatternData();
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("PLAYER_DISTANCE Pattern", EditorStyles.boldLabel);
            float updatedPlayerDistance = EditorGUILayout.FloatField("Player Distance", playerDistancePatternData.GetPlayerDistance());
            playerDistancePatternData.SetPlayerDistance(updatedPlayerDistance);
            DrawActionEntryList(playerDistancePatternData.GetActionEntryList(), PlayerDistanceAllowedActions, "PLAYER_DISTANCE 행동", "PLAYER_DISTANCE 행동 추가");
            EditorGUILayout.Space();
            DrawAttackPatternSection(playerDistancePatternData.GetAttackPatternData(), "PLAYER_DISTANCE Attack Pattern");
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 공격 패턴 영역 렌더링
        ///</summary>
        private void DrawAttackPatternSection(CMonsterAttackPatternData _attackPatternData, string _title)
        {
            if (_attackPatternData == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
            bool updatedUseAttackPattern = EditorGUILayout.ToggleLeft("Use Attack Pattern", _attackPatternData.GetUseAttackPattern());
            _attackPatternData.SetUseAttackPattern(updatedUseAttackPattern);

            if (_attackPatternData.GetUseAttackPattern())
            {
                float updatedAttackDistance = EditorGUILayout.FloatField("Attack Distance", _attackPatternData.GetAttackDistance());
                _attackPatternData.SetAttackDistance(updatedAttackDistance);
                DrawActionEntryList(_attackPatternData.GetActionEntryList(), AttackAllowedActions, "공격 행동", "공격 행동 추가");
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 행동 엔트리 목록 영역 렌더링
        ///</summary>
        private void DrawActionEntryList(List<CMonsterBehaviorActionEntry> _entryList, eMonsterBehaviorAction[] _allowedActions, string _sectionLabel, string _addButtonLabel)
        {
            EditorGUILayout.LabelField(_sectionLabel, EditorStyles.boldLabel);

            if (_entryList.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 행동이 없습니다.", MessageType.None);
            }

            for (int index = 0; index < _entryList.Count; index++)
            {
                CMonsterBehaviorActionEntry entryData = _entryList[index];

                if (entryData == null)
                {
                    entryData = new CMonsterBehaviorActionEntry();
                    eMonsterBehaviorAction defaultAction = _allowedActions[0];
                    entryData.SetActionType(defaultAction);
                    _entryList[index] = entryData;
                }

                DrawActionEntryRow(_entryList, entryData, _allowedActions, index);
            }

            if (GUILayout.Button(_addButtonLabel))
            {
                AddActionEntry(_entryList, _allowedActions);
            }
        }

        ///<summary>
        /// 행동 엔트리 행 렌더링
        ///</summary>
        private void DrawActionEntryRow(List<CMonsterBehaviorActionEntry> _entryList, CMonsterBehaviorActionEntry _entryData, eMonsterBehaviorAction[] _allowedActions, int _index)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entry {_index + 1}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80.0f)))
            {
                _entryList.RemoveAt(_index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();
            eMonsterBehaviorAction updatedActionType = DrawAllowedActionPopup("Action", _entryData.GetActionType(), _allowedActions);
            _entryData.SetActionType(updatedActionType);
            float updatedWeight = EditorGUILayout.FloatField("Weight", _entryData.GetWeight());
            _entryData.SetWeight(updatedWeight);
            float updatedDurationSeconds = EditorGUILayout.FloatField("Duration Seconds", _entryData.GetDurationSeconds());
            _entryData.SetDurationSeconds(updatedDurationSeconds);
            float updatedCooldownSeconds = EditorGUILayout.FloatField("Cooldown Seconds", _entryData.GetCooldownSeconds());
            _entryData.SetCooldownSeconds(updatedCooldownSeconds);
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 허용 행동 팝업 렌더링
        ///</summary>
        private eMonsterBehaviorAction DrawAllowedActionPopup(string _label, eMonsterBehaviorAction _currentAction, eMonsterBehaviorAction[] _allowedActions)
        {
            string[] optionLabels = BuildActionOptionLabels(_allowedActions);
            int currentIndex = GetAllowedActionIndex(_currentAction, _allowedActions);
            int nextIndex = EditorGUILayout.Popup(_label, currentIndex, optionLabels);
            eMonsterBehaviorAction result = _allowedActions[nextIndex];
            return result;
        }

        ///<summary>
        /// 저장 영역 렌더링
        ///</summary>
        private void DrawSaveSection(MonsterBehaviorPrefabInfo _selectedInfo)
        {
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
            string validationMessage = ValidateWorkingPatternData();

            if (string.IsNullOrEmpty(validationMessage) == false)
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(validationMessage) == false))
            {
                if (GUILayout.Button("패턴 저장", GUILayout.Height(36.0f)))
                {
                    SavePatternData(_selectedInfo);
                }
            }

            EditorGUILayout.HelpBox(statusMessage, statusMessageType);
        }

        ///<summary>
        /// 몬스터 프리팹 목록 갱신
        ///</summary>
///<summary>
/// 몬스터 프리팹 목록 갱신
///</summary>
private void RefreshMonsterPrefabInfos()
{
    monsterPrefabInfos.Clear();

    if ( AssetDatabase.IsValidFolder( MonsterPrefabFolderPath ) )
    {
        string[] assetGuids = AssetDatabase.FindAssets( "t:Prefab", new string[] { MonsterPrefabFolderPath } );
        Array.Sort( assetGuids, CompareAssetGuid );

        for ( int index = 0; index < assetGuids.Length; index++ )
        {
            string assetGuid = assetGuids[ index ];
            string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
            string prefabName = Path.GetFileNameWithoutExtension( assetPath );
            MonsterBehaviorPrefabInfo prefabInfo = new MonsterBehaviorPrefabInfo();
            prefabInfo.prefabName = prefabName;
            prefabInfo.assetPath = assetPath;
            monsterPrefabInfos.Add( prefabInfo );
        }
    }

    if ( monsterPrefabInfos.Count == 0 )
    {
        selectedPrefabIndex = -1;
        selectedCopySourcePrefabIndex = -1;
        DestroyWorkingPatternData();
        SetStatus( "몬스터 프리팹을 찾지 못했습니다.", MessageType.Warning );
        return;
    }

    if ( selectedPrefabIndex < 0 || selectedPrefabIndex >= monsterPrefabInfos.Count )
    {
        selectedPrefabIndex = 0;
    }

    if ( selectedCopySourcePrefabIndex < 0 || selectedCopySourcePrefabIndex >= monsterPrefabInfos.Count )
    {
        selectedCopySourcePrefabIndex = -1;
    }

    LoadWorkingPatternDataForSelection();
    SetStatus( $"몬스터 프리팹 {monsterPrefabInfos.Count}개를 불러왔습니다.", MessageType.Info );
}

        ///<summary>
        /// 에셋 GUID 비교
        ///</summary>
        private int CompareAssetGuid(string _leftGuid, string _rightGuid)
        {
            string leftPath = AssetDatabase.GUIDToAssetPath(_leftGuid);
            string rightPath = AssetDatabase.GUIDToAssetPath(_rightGuid);
            int result = string.Compare(leftPath, rightPath, StringComparison.Ordinal);
            return result;
        }

        ///<summary>
        /// 필터링된 프리팹 목록 반환
        ///</summary>
        private List<MonsterBehaviorPrefabInfo> GetFilteredMonsterPrefabInfos()
        {
            List<MonsterBehaviorPrefabInfo> filteredInfos = new List<MonsterBehaviorPrefabInfo>();

            for (int index = 0; index < monsterPrefabInfos.Count; index++)
            {
                MonsterBehaviorPrefabInfo prefabInfo = monsterPrefabInfos[index];

                if ( IsSearchMatch( prefabInfo, searchText ) == false )
                {
                    continue;
                }

                filteredInfos.Add(prefabInfo);
            }

            return filteredInfos;
        }

        ///<summary>
        /// 검색 일치 여부 반환
        ///</summary>
        protected override bool IsSearchMatch( MonsterBehaviorPrefabInfo _prefabInfo, string _searchText )
        {
            if (_prefabInfo == null)
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( _searchText ) )
            {
                return true;
            }

            string normalizedSearchText = _searchText.Trim();
            bool isMatched = _prefabInfo.prefabName.IndexOf(normalizedSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            return isMatched;
        }

        ///<summary>
        /// 현재 선택 프리팹 반환
        ///</summary>
        private MonsterBehaviorPrefabInfo GetSelectedMonsterPrefabInfo()
        {
            if (selectedPrefabIndex < 0 || selectedPrefabIndex >= monsterPrefabInfos.Count)
            {
                return null;
            }

            MonsterBehaviorPrefabInfo result = monsterPrefabInfos[selectedPrefabIndex];
            return result;
        }

        ///<summary>
        /// 선택 프리팹 데이터 준비
        ///</summary>
        private void EnsureWorkingPatternDataLoaded()
        {
            if (workingPatternData != null)
            {
                return;
            }

            LoadWorkingPatternDataForSelection();
        }

        ///<summary>
        /// 선택 프리팹 패턴 데이터 불러오기
        ///</summary>
///<summary>
/// 선택 프리팹 작업용 행동 패턴 로드
///</summary>
private void LoadWorkingPatternDataForSelection()
{
    DestroyWorkingPatternData();
    MonsterBehaviorPrefabInfo selectedInfo = GetSelectedMonsterPrefabInfo();

    if ( selectedInfo == null )
    {
        return;
    }

    workingPatternData = CreatePatternDataFromPrefabInfo( selectedInfo );
    LoadDropSettingsFromPrefab( selectedInfo.assetPath );
}

///<summary>
/// 복사 원본 프리팹 정보 반환
///</summary>
private MonsterBehaviorPrefabInfo GetCopySourcePrefabInfo()
{
    if ( selectedCopySourcePrefabIndex < 0 || selectedCopySourcePrefabIndex >= monsterPrefabInfos.Count )
    {
        return null;
    }

    MonsterBehaviorPrefabInfo result = monsterPrefabInfos[ selectedCopySourcePrefabIndex ];
    return result;
}

///<summary>
/// 복사 원본 선택 옵션 배열 구성
///</summary>
private string[] BuildCopySourceOptionArray()
{
    string[] optionArray = new string[ monsterPrefabInfos.Count + 1 ];
    optionArray[ 0 ] = "Select Monster";

    for ( int index = 0; index < monsterPrefabInfos.Count; index++ )
    {
        MonsterBehaviorPrefabInfo prefabInfo = monsterPrefabInfos[ index ];
        optionArray[ index + 1 ] = prefabInfo != null ? prefabInfo.prefabName : string.Empty;
    }

    return optionArray;
}

///<summary>
/// 복사 원본 팝업 인덱스 결정
///</summary>
private int ResolveCopySourcePopupIndex()
{
    if ( selectedCopySourcePrefabIndex < 0 || selectedCopySourcePrefabIndex >= monsterPrefabInfos.Count )
    {
        return 0;
    }

    int result = selectedCopySourcePrefabIndex + 1;
    return result;
}

///<summary>
/// 복사 원본 프리팹 인덱스 결정
///</summary>
private int ResolveCopySourcePrefabIndex( int _popupIndex )
{
    if ( _popupIndex <= 0 )
    {
        return -1;
    }

    int prefabIndex = _popupIndex - 1;

    if ( prefabIndex < 0 || prefabIndex >= monsterPrefabInfos.Count )
    {
        return -1;
    }

    return prefabIndex;
}

///<summary>
/// 프리팹 기준 작업용 행동 패턴 데이터 생성
///</summary>
private CMonsterBehaviorPatternData CreatePatternDataFromPrefabInfo( MonsterBehaviorPrefabInfo _prefabInfo )
{
    if ( _prefabInfo == null )
    {
        return null;
    }

    string assetPath = GetBehaviorPatternAssetPath( _prefabInfo.prefabName );
    CMonsterBehaviorPatternData savedPatternData = AssetDatabase.LoadAssetAtPath<CMonsterBehaviorPatternData>( assetPath );
    CMonsterBehaviorPatternData createdPatternData = ScriptableObject.CreateInstance<CMonsterBehaviorPatternData>();
    createdPatternData.hideFlags = HideFlags.HideAndDontSave;

    if ( savedPatternData != null )
    {
        string serializedJson = EditorJsonUtility.ToJson( savedPatternData );
        EditorJsonUtility.FromJsonOverwrite( serializedJson, createdPatternData );
    }
    else
    {
        InitializeDefaultPatternData( createdPatternData, _prefabInfo.prefabName );
    }

    createdPatternData.SetMonsterId( _prefabInfo.prefabName );
    return createdPatternData;
}

///<summary>
/// 원본 몬스터 행동 패턴 복사
///</summary>
private void CopyPatternDataFromSource( MonsterBehaviorPrefabInfo _targetInfo, MonsterBehaviorPrefabInfo _sourceInfo )
{
    if ( _targetInfo == null || _sourceInfo == null || workingPatternData == null )
    {
        SetStatus( "복사할 행동 패턴 데이터가 없습니다.", MessageType.Warning );
        return;
    }

    bool isSamePrefab = string.Equals( _targetInfo.assetPath, _sourceInfo.assetPath, StringComparison.Ordinal );

    if ( isSamePrefab )
    {
        SetStatus( "현재 선택된 몬스터 자신은 복사 원본으로 사용할 수 없습니다.", MessageType.Warning );
        return;
    }

    CMonsterBehaviorPatternData sourcePatternData = CreatePatternDataFromPrefabInfo( _sourceInfo );

    if ( sourcePatternData == null )
    {
        SetStatus( "복사 원본 행동 패턴을 불러오지 못했습니다.", MessageType.Warning );
        return;
    }

    string serializedJson = EditorJsonUtility.ToJson( sourcePatternData );
    EditorJsonUtility.FromJsonOverwrite( serializedJson, workingPatternData );
    workingPatternData.SetMonsterId( _targetInfo.prefabName );
    DestroyImmediate( sourcePatternData );
    LoadDropSettingsFromPrefab( _sourceInfo.assetPath );
    SetStatus( $"행동 패턴을 {_sourceInfo.prefabName} 에서 복사했습니다.", MessageType.Info );
}


        ///<summary>
        /// 임시 패턴 데이터 정리
        ///</summary>
        private void DestroyWorkingPatternData()
        {
            if (workingPatternData == null)
            {
                return;
            }

            DestroyImmediate(workingPatternData);
            workingPatternData = null;
        }

        ///<summary>
        /// 기본 패턴 데이터 초기화
        ///</summary>
        private void InitializeDefaultPatternData(CMonsterBehaviorPatternData _patternData, string _monsterId)
        {
            if (_patternData == null)
            {
                return;
            }

            _patternData.SetMonsterId(_monsterId);
            List<CMonsterBehaviorActionEntry> alwaysEntries = _patternData.GetAlwaysPatternData().GetActionEntryList();
            alwaysEntries.Clear();
            AddActionEntry(alwaysEntries, AlwaysAllowedActions);
            List<CMonsterBehaviorActionEntry> playerDistanceEntries = _patternData.GetPlayerDistancePatternData().GetActionEntryList();
            playerDistanceEntries.Clear();
            AddActionEntry(playerDistanceEntries, PlayerDistanceAllowedActions);
        }

        ///<summary>
        /// 행동 엔트리 추가
        ///</summary>
        private void AddActionEntry(List<CMonsterBehaviorActionEntry> _entryList, eMonsterBehaviorAction[] _allowedActions)
        {
            CMonsterBehaviorActionEntry entryData = new CMonsterBehaviorActionEntry();
            eMonsterBehaviorAction defaultAction = _allowedActions[0];
            entryData.SetActionType(defaultAction);
            entryData.SetWeight(1.0f);
            entryData.SetDurationSeconds(1.0f);
            entryData.SetCooldownSeconds(1.0f);
            _entryList.Add(entryData);
        }

        ///<summary>
        /// 행동 라벨 배열 구성
        ///</summary>
        private string[] BuildActionOptionLabels(eMonsterBehaviorAction[] _allowedActions)
        {
            string[] labels = new string[_allowedActions.Length];

            for (int index = 0; index < _allowedActions.Length; index++)
            {
                eMonsterBehaviorAction actionType = _allowedActions[index];
                labels[index] = actionType.ToString();
            }

            return labels;
        }

        ///<summary>
        /// 허용 행동 인덱스 반환
        ///</summary>
        private int GetAllowedActionIndex(eMonsterBehaviorAction _currentAction, eMonsterBehaviorAction[] _allowedActions)
        {
            for (int index = 0; index < _allowedActions.Length; index++)
            {
                eMonsterBehaviorAction actionType = _allowedActions[index];

                if (actionType == _currentAction)
                {
                    return index;
                }
            }

            return 0;
        }

        ///<summary>
        /// 패턴 저장 처리
        ///</summary>
        private void SavePatternData(MonsterBehaviorPrefabInfo _selectedInfo)
        {
            if (_selectedInfo == null || workingPatternData == null)
            {
                SetStatus("저장할 패턴 데이터가 없습니다.", MessageType.Warning);
                return;
            }

            string validationMessage = ValidateWorkingPatternData();

            if (string.IsNullOrEmpty(validationMessage) == false)
            {
                SetStatus(validationMessage, MessageType.Warning);
                return;
            }

            EnsureBehaviorPatternFolderExists();
            string assetPath = GetBehaviorPatternAssetPath(_selectedInfo.prefabName);
            CMonsterBehaviorPatternData savedPatternData = SaveOrUpdatePatternAsset(assetPath);

            if (savedPatternData == null)
            {
                SetStatus("행동 패턴 에셋 저장에 실패했습니다.", MessageType.Error);
                return;
            }

            bool isAssigned = AssignPatternAssetToPrefab(_selectedInfo.assetPath, savedPatternData);

            if (isAssigned == false)
            {
                SetStatus("행동 패턴 에셋은 저장되었지만 프리팹 연결에 실패했습니다.", MessageType.Warning);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus($"행동 패턴 저장 완료: {assetPath}", MessageType.Info);
        }

        ///<summary>
        /// 패턴 에셋 저장 또는 갱신
        ///</summary>
        private CMonsterBehaviorPatternData SaveOrUpdatePatternAsset(string _assetPath)
        {
            CMonsterBehaviorPatternData savedPatternData = AssetDatabase.LoadAssetAtPath<CMonsterBehaviorPatternData>(_assetPath);

            if (savedPatternData == null)
            {
                savedPatternData = ScriptableObject.CreateInstance<CMonsterBehaviorPatternData>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(workingPatternData), savedPatternData);
                AssetDatabase.CreateAsset(savedPatternData, _assetPath);
                return savedPatternData;
            }

            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(workingPatternData), savedPatternData);
            EditorUtility.SetDirty(savedPatternData);
            return savedPatternData;
        }

        ///<summary>
        /// 패턴 에셋 프리팹 연결
        ///</summary>
        private bool AssignPatternAssetToPrefab(string _prefabAssetPath, CMonsterBehaviorPatternData _patternData)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(_prefabAssetPath);

            try
            {
                MonsterObject monsterObject = prefabRoot.GetComponent<MonsterObject>();

                if (monsterObject == null)
                {
                    SetStatus("선택 프리팹에 MonsterObject 컴포넌트가 없습니다.", MessageType.Error);
                    return false;
                }

                SerializedObject serializedObject = new SerializedObject(monsterObject);
                SerializedProperty behaviorPatternProperty = serializedObject.FindProperty("behaviorPatternData");
                SerializedProperty useItemDropProperty = serializedObject.FindProperty("useItemDrop");
                SerializedProperty itemDropEntryListProperty = serializedObject.FindProperty("itemDropEntryList");

                if (behaviorPatternProperty == null)
                {
                    SetStatus("MonsterObject에 behaviorPatternData 필드가 없습니다.", MessageType.Error);
                    return false;
                }

                behaviorPatternProperty.objectReferenceValue = _patternData;

                if ( useItemDropProperty != null )
                {
                    useItemDropProperty.boolValue = useItemDrop;
                }

                if ( itemDropEntryListProperty != null )
                {
                    ApplyItemDropEntries( itemDropEntryListProperty );
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, _prefabAssetPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        ///<summary>
        /// 편집 데이터 검증
        ///</summary>
        private string ValidateWorkingPatternData()
        {
            if (workingPatternData == null)
            {
                return "패턴 데이터를 준비하지 못했습니다.";
            }

            string alwaysValidationMessage = ValidateActionEntryList(workingPatternData.GetAlwaysPatternData().GetActionEntryList(), "ALWAYS");

            if (string.IsNullOrEmpty(alwaysValidationMessage) == false)
            {
                return alwaysValidationMessage;
            }

            CMonsterAttackPatternData alwaysAttackPatternData = workingPatternData.GetAlwaysPatternData().GetAttackPatternData();
            string alwaysAttackValidationMessage = ValidateAttackPatternData(alwaysAttackPatternData, "ALWAYS");

            if (string.IsNullOrEmpty(alwaysAttackValidationMessage) == false)
            {
                return alwaysAttackValidationMessage;
            }

            CMonsterPlayerDistancePatternData playerDistancePatternData = workingPatternData.GetPlayerDistancePatternData();

            if (playerDistancePatternData.GetPlayerDistance() <= 0.0f)
            {
                return "PLAYER_DISTANCE의 감지 거리는 0보다 커야 합니다.";
            }

            string playerDistanceValidationMessage = ValidateActionEntryList(playerDistancePatternData.GetActionEntryList(), "PLAYER_DISTANCE");

            if (string.IsNullOrEmpty(playerDistanceValidationMessage) == false)
            {
                return playerDistanceValidationMessage;
            }

            string playerDistanceAttackValidationMessage = ValidateAttackPatternData(playerDistancePatternData.GetAttackPatternData(), "PLAYER_DISTANCE");

            if (string.IsNullOrEmpty(playerDistanceAttackValidationMessage) == false)
            {
                return playerDistanceAttackValidationMessage;
            }

            if ( useItemDrop && BuildValidDropSettingList().Count == 0 )
            {
                return "아이템 드랍 사용 시 유효한 드랍 엔트리를 하나 이상 추가하세요.";
            }

            return string.Empty;
        }

        ///<summary>
        /// 프리팹 드랍 설정 로드
        ///</summary>
        private void LoadDropSettingsFromPrefab( string _prefabAssetPath )
        {
            useItemDrop = false;
            itemDropSettingList.Clear();
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>( _prefabAssetPath );

            if ( prefabAsset == null )
            {
                return;
            }

            MonsterObject monsterObject = prefabAsset.GetComponent<MonsterObject>();

            if ( monsterObject == null )
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject( monsterObject );
            SerializedProperty useItemDropProperty = serializedObject.FindProperty( "useItemDrop" );
            SerializedProperty itemDropEntryListProperty = serializedObject.FindProperty( "itemDropEntryList" );
            useItemDrop = useItemDropProperty != null && useItemDropProperty.boolValue;

            if ( itemDropEntryListProperty == null )
            {
                return;
            }

            for ( int index = 0; index < itemDropEntryListProperty.arraySize; index++ )
            {
                SerializedProperty entryProperty = itemDropEntryListProperty.GetArrayElementAtIndex( index );

                if ( entryProperty == null )
                {
                    continue;
                }

                MonsterBehaviorDropSetting dropSetting = new MonsterBehaviorDropSetting();
                SerializedProperty itemDefinitionProperty = entryProperty.FindPropertyRelative( "itemDefinition" );
                SerializedProperty dropChanceProperty = entryProperty.FindPropertyRelative( "dropChance" );
                SerializedProperty minDropCountProperty = entryProperty.FindPropertyRelative( "minDropCount" );
                SerializedProperty maxDropCountProperty = entryProperty.FindPropertyRelative( "maxDropCount" );
                dropSetting.itemDefinition = itemDefinitionProperty != null ? itemDefinitionProperty.objectReferenceValue as CItemDefinition : null;
                dropSetting.dropChance = dropChanceProperty != null ? dropChanceProperty.floatValue : 1.0f;
                dropSetting.minDropCount = minDropCountProperty != null ? minDropCountProperty.intValue : 1;
                dropSetting.maxDropCount = maxDropCountProperty != null ? maxDropCountProperty.intValue : 1;
                itemDropSettingList.Add( dropSetting );
            }
        }

        ///<summary>
        /// 프리팹 드랍 엔트리 적용
        ///</summary>
        private void ApplyItemDropEntries( SerializedProperty _itemDropEntryListProperty )
        {
            if ( _itemDropEntryListProperty == null )
            {
                return;
            }

            List<MonsterBehaviorDropSetting> validDropSettingList = BuildValidDropSettingList();
            _itemDropEntryListProperty.arraySize = validDropSettingList.Count;

            for ( int index = 0; index < validDropSettingList.Count; index++ )
            {
                MonsterBehaviorDropSetting dropSetting = validDropSettingList[ index ];
                SerializedProperty entryProperty = _itemDropEntryListProperty.GetArrayElementAtIndex( index );
                SerializedProperty itemDefinitionProperty = entryProperty.FindPropertyRelative( "itemDefinition" );
                SerializedProperty dropChanceProperty = entryProperty.FindPropertyRelative( "dropChance" );
                SerializedProperty minDropCountProperty = entryProperty.FindPropertyRelative( "minDropCount" );
                SerializedProperty maxDropCountProperty = entryProperty.FindPropertyRelative( "maxDropCount" );

                if ( itemDefinitionProperty != null )
                {
                    itemDefinitionProperty.objectReferenceValue = dropSetting.itemDefinition;
                }

                if ( dropChanceProperty != null )
                {
                    dropChanceProperty.floatValue = Mathf.Clamp01( dropSetting.dropChance );
                }

                if ( minDropCountProperty != null )
                {
                    minDropCountProperty.intValue = Mathf.Max( 0, dropSetting.minDropCount );
                }

                if ( maxDropCountProperty != null )
                {
                    maxDropCountProperty.intValue = Mathf.Max( Mathf.Max( 0, dropSetting.minDropCount ), dropSetting.maxDropCount );
                }
            }
        }

        ///<summary>
        /// 유효 드랍 설정 목록 구성
        ///</summary>
        private List<MonsterBehaviorDropSetting> BuildValidDropSettingList()
        {
            List<MonsterBehaviorDropSetting> validDropSettingList = new List<MonsterBehaviorDropSetting>();

            if ( useItemDrop == false )
            {
                return validDropSettingList;
            }

            for ( int index = 0; index < itemDropSettingList.Count; index++ )
            {
                MonsterBehaviorDropSetting dropSetting = itemDropSettingList[ index ];

                if ( dropSetting == null || dropSetting.itemDefinition == null )
                {
                    continue;
                }

                validDropSettingList.Add( dropSetting );
            }

            return validDropSettingList;
        }

        ///<summary>
        /// 행동 엔트리 목록 검증
        ///</summary>
        private string ValidateActionEntryList(List<CMonsterBehaviorActionEntry> _entryList, string _groupName)
        {
            if (_entryList == null || _entryList.Count == 0)
            {
                return $"{_groupName} 행동을 하나 이상 추가하세요.";
            }

            for (int index = 0; index < _entryList.Count; index++)
            {
                CMonsterBehaviorActionEntry entryData = _entryList[index];

                if (entryData == null)
                {
                    return $"{_groupName} 행동 엔트리 {index + 1}이 비어 있습니다.";
                }

                if (entryData.GetWeight() <= 0.0f)
                {
                    return $"{_groupName} 행동 엔트리 {index + 1}의 Weight는 0보다 커야 합니다.";
                }
            }

            return string.Empty;
        }

        ///<summary>
        /// 공격 패턴 검증
        ///</summary>
        private string ValidateAttackPatternData(CMonsterAttackPatternData _attackPatternData, string _groupName)
        {
            if (_attackPatternData == null || _attackPatternData.GetUseAttackPattern() == false)
            {
                return string.Empty;
            }

            if (_attackPatternData.GetAttackDistance() <= 0.0f)
            {
                return $"{_groupName} 공격 가능 거리는 0보다 커야 합니다.";
            }

            string actionValidationMessage = ValidateActionEntryList(_attackPatternData.GetActionEntryList(), $"{_groupName} 공격");
            return actionValidationMessage;
        }

        ///<summary>
        /// 행동 패턴 에셋 경로 반환
        ///</summary>
        private string GetBehaviorPatternAssetPath(string _prefabName)
        {
            string result = $"{BehaviorPatternFolderPath}/{_prefabName}_BehaviorPattern.asset";
            return result;
        }

        ///<summary>
        /// 행동 패턴 폴더 생성 보장
        ///</summary>
        private void EnsureBehaviorPatternFolderExists()
        {
            if (AssetDatabase.IsValidFolder(BehaviorPatternFolderPath))
            {
                return;
            }

            string[] folderSegments = BehaviorPatternFolderPath.Split('/');
            string currentPath = folderSegments[0];

            for (int index = 1; index < folderSegments.Length; index++)
            {
                string folderName = folderSegments[index];
                string combinedPath = $"{currentPath}/{folderName}";

                if (AssetDatabase.IsValidFolder(combinedPath) == false)
                {
                    AssetDatabase.CreateFolder(currentPath, folderName);
                }

                currentPath = combinedPath;
            }
        }

        ///<summary>
        /// 키보드 선택 이동 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            Event currentEvent = Event.current;

            if (currentEvent == null)
            {
                return;
            }

            if (EditorGUIUtility.editingTextField)
            {
                return;
            }

            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.DownArrow)
            {
                MoveSelectionInFilteredList(1);
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.UpArrow)
            {
                MoveSelectionInFilteredList(-1);
                currentEvent.Use();
            }
        }

        ///<summary>
        /// 필터 목록 선택 이동
        ///</summary>
        private void MoveSelectionInFilteredList(int _direction)
        {
            List<MonsterBehaviorPrefabInfo> filteredInfos = GetFilteredMonsterPrefabInfos();

            if (filteredInfos.Count == 0)
            {
                return;
            }

            int filteredSelectedIndex = 0;
            MonsterBehaviorPrefabInfo selectedInfo = GetSelectedMonsterPrefabInfo();

            if (selectedInfo != null)
            {
                int resolvedIndex = filteredInfos.IndexOf(selectedInfo);

                if (resolvedIndex >= 0)
                {
                    filteredSelectedIndex = resolvedIndex;
                }
            }

            int lastIndex = filteredInfos.Count - 1;
            int nextFilteredIndex = Mathf.Clamp(filteredSelectedIndex + _direction, 0, lastIndex);
            MonsterBehaviorPrefabInfo nextInfo = filteredInfos[nextFilteredIndex];
            int sourceIndex = monsterPrefabInfos.IndexOf(nextInfo);
            SelectPrefabByIndex(sourceIndex, nextFilteredIndex, filteredInfos.Count);
        }

        ///<summary>
        /// 프리팹 선택 처리
        ///</summary>
        private void SelectPrefabByIndex(int _sourceIndex, int _filteredIndex, int _filteredItemCount)
        {
            if (_sourceIndex < 0 || _sourceIndex >= monsterPrefabInfos.Count)
            {
                return;
            }

            selectedPrefabIndex = _sourceIndex;
            isPendingFocusToSelection = true;
            EnsureSelectionVisibleByIndex(_filteredIndex, _filteredItemCount);
            LoadWorkingPatternDataForSelection();
            Repaint();
        }

        ///<summary>
        /// 선택 항목 스크롤 보정
        ///</summary>
        private void EnsureSelectionVisibleByIndex(int _filteredSelectedIndex, int _filteredItemCount)
        {
            float itemStride = PrefabListItemHeight + PrefabListItemSpacing;
            float itemTop = _filteredSelectedIndex * itemStride;
            float itemBottom = itemTop + PrefabListItemHeight;
            float contentHeight = Mathf.Max(0.0f, _filteredItemCount * itemStride);
            float maxScrollY = Mathf.Max(0.0f, contentHeight - PrefabListViewHeight);

            if (itemTop < prefabListScrollPosition.y)
            {
                prefabListScrollPosition.y = itemTop;
            }
            else if (itemBottom > prefabListScrollPosition.y + PrefabListViewHeight)
            {
                prefabListScrollPosition.y = itemBottom - PrefabListViewHeight;
            }

            prefabListScrollPosition.y = Mathf.Clamp(prefabListScrollPosition.y, 0.0f, maxScrollY);
        }

        ///<summary>
        /// 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildPrefabItemControlName(int _sourceIndex)
        {
            string result = $"MonsterBehaviorPrefabItem_{_sourceIndex}";
            return result;
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus(string _message, MessageType _messageType)
        {
            statusMessage = _message;
            statusMessageType = _messageType;
        }
    }
}
