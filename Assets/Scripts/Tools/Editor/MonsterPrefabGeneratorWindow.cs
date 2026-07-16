using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyHero.Tools
{
    ///<summary>
    /// 몬스터 프리팹 생성 대상 정보
    ///</summary>
    [Serializable]
    public sealed class SourceMonsterPrefabInfo
    {
        public string sourceGroup;
        public string displayName;
        public string assetPath;
    }

    ///<summary>
    /// 몬스터 드랍 설정 데이터
    ///</summary>
    ///<summary>
    /// 몬스터 프리팹 생성 에디터 창
    ///</summary>
    public sealed class MonsterPrefabGeneratorWindow : CEditorToolWindowBase<SourceMonsterPrefabInfo>
    {
        private const string TargetFolderPath = "Assets/Resources/Prefabs/Character/Monster";
        private const string MonsterLayerName = "Monster";
        private const string PlayerLayerName = "Player";
        private const string MonsterSortingLayerName = "MonsterObject";
        private const string SourceFolderPathOne = "Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 1/Prefabs";
        private const string SourceFolderPathTwo = "Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 2/Prefabs";
        private const string MonsterNamePrefix = "Monster_";
        private const string ContactHitboxObjectName = "ContactHitbox";
        private const string BodyBoneObjectName = "Body_bone";
        private const float DefaultRootScale = 0.5f;
        private const float DefaultGravityScale = 1.0f;
        private const float SourceListViewHeight = 360.0f;
        private const float SourceListItemHeight = 40.0f;
        private const float SourceListItemSpacing = 4.0f;
        private const int PreviewSize = 220;

        [SerializeField] private List<SourceMonsterPrefabInfo> sourcePrefabInfos = new List<SourceMonsterPrefabInfo>();
        [SerializeField] private int selectedSourceIndex = -1;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private bool useManualPrefabName;
        [SerializeField] private string manualPrefabName = string.Empty;
        [SerializeField] private float rootScale = DefaultRootScale;

        private Vector2 sourceListScrollPosition;
        private string statusMessage = "몬스터 소스 프리팹을 불러오세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool isPendingFocusToSelection;

        ///<summary>
        /// 에디터 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Monster Prefab Generator" )]
        private static void ShowWindow()
        {
            MonsterPrefabGeneratorWindow window = GetWindow<MonsterPrefabGeneratorWindow>();
            window.titleContent = new GUIContent( "Monster Prefab Generator" );
            window.minSize = new Vector2( 920.0f, 520.0f );
            window.Show();
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshSourcePrefabInfos();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            HandleKeyboardNavigation();
            DrawWindowHeader( "Monster Prefab Generator", "EnemyMonster 1, EnemyMonster 2 소스 프리팹을 선택해 규약에 맞는 몬스터 프리팹을 생성합니다." );

            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawSourceListSection();
            DrawPreviewSection();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            DrawActionSection();
        }

        ///<summary>
        /// 툴바 섹션 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            string newSearchText = EditorGUILayout.TextField( "Search", searchText );

            if ( string.Equals( newSearchText, searchText, StringComparison.Ordinal ) == false )
            {
                searchText = newSearchText;
            }

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 120.0f ) ) )
            {
                RefreshSourcePrefabInfos();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 소스 목록 섹션 렌더링
        ///</summary>
        private void DrawSourceListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( 360.0f ) );
            EditorGUILayout.LabelField( "Source Prefabs", EditorStyles.boldLabel );
            List<SourceMonsterPrefabInfo> filteredInfos = GetFilteredSourcePrefabInfos();
            EditorGUILayout.HelpBox( $"검색 결과 {filteredInfos.Count}개", MessageType.None );
            sourceListScrollPosition = EditorGUILayout.BeginScrollView( sourceListScrollPosition, GUILayout.Height( SourceListViewHeight ) );

            for ( int index = 0; index < filteredInfos.Count; index++ )
            {
                SourceMonsterPrefabInfo sourcePrefabInfo = filteredInfos[ index ];
                DrawSourceListItem( sourcePrefabInfo );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 소스 목록 항목 렌더링
        ///</summary>
        private void DrawSourceListItem( SourceMonsterPrefabInfo _sourcePrefabInfo )
        {
            if ( _sourcePrefabInfo == null )
            {
                return;
            }

            int sourceIndex = sourcePrefabInfos.IndexOf( _sourcePrefabInfo );
            bool isSelected = sourceIndex == selectedSourceIndex;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = SourceListItemHeight;
            string buttonLabel = $"[ {_sourcePrefabInfo.sourceGroup} ] {_sourcePrefabInfo.displayName}";
            string controlName = BuildSourceItemControlName( sourceIndex );
            GUI.SetNextControlName( controlName );
            bool isClicked = GUILayout.Button( buttonLabel, buttonStyle );

            if ( isClicked )
            {
                selectedSourceIndex = sourceIndex;
                isPendingFocusToSelection = true;
                List<SourceMonsterPrefabInfo> filteredInfos = GetFilteredSourcePrefabInfos();
                int filteredSelectedIndex = filteredInfos.IndexOf( _sourcePrefabInfo );

                if ( filteredSelectedIndex >= 0 )
                {
                    EnsureSelectionVisibleByIndex( filteredSelectedIndex, filteredInfos.Count );
                }

                Repaint();
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
        /// 프리뷰 섹션 렌더링
        ///</summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField( "Preview", EditorStyles.boldLabel );
            SourceMonsterPrefabInfo selectedInfo = GetSelectedSourcePrefabInfo();

            if ( selectedInfo == null )
            {
                EditorGUILayout.HelpBox( "생성할 몬스터 소스 프리팹을 선택하세요.", MessageType.Info );
                EditorGUILayout.EndVertical();
                return;
            }

            GameObject sourcePrefab = LoadSourcePrefabAsset( selectedInfo.assetPath );
            Texture previewTexture = AssetPreview.GetAssetPreview( sourcePrefab );

            if ( previewTexture == null )
            {
                previewTexture = AssetPreview.GetMiniThumbnail( sourcePrefab );
                Repaint();
            }

            Rect previewRect = GUILayoutUtility.GetRect( PreviewSize, PreviewSize, GUILayout.ExpandWidth( false ) );
            EditorGUI.DrawRect( previewRect, new Color( 0.16f, 0.16f, 0.16f, 1.0f ) );

            if ( previewTexture != null )
            {
                GUI.DrawTexture( previewRect, previewTexture, ScaleMode.ScaleToFit );
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Source Group", selectedInfo.sourceGroup );
            EditorGUILayout.LabelField( "Source Name", selectedInfo.displayName );
            EditorGUILayout.LabelField( "Source Path", selectedInfo.assetPath );
            EditorGUILayout.LabelField( "Create Path", GetTargetPrefabPath() );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 동작 섹션 렌더링
        ///</summary>
        private void DrawActionSection()
        {
            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );
            SourceMonsterPrefabInfo selectedInfo = GetSelectedSourcePrefabInfo();
            DrawCreateNameSection();
            DrawCreateOptionSection();
            string prefabNameValidationMessage = ValidateTargetPrefabName();
            string rootScaleValidationMessage = ValidateRootScale();
            bool canCreate = selectedInfo != null && string.IsNullOrEmpty( prefabNameValidationMessage ) && string.IsNullOrEmpty( rootScaleValidationMessage );

            if ( string.IsNullOrEmpty( prefabNameValidationMessage ) == false )
            {
                EditorGUILayout.HelpBox( prefabNameValidationMessage, MessageType.Warning );
            }

            if ( string.IsNullOrEmpty( rootScaleValidationMessage ) == false )
            {
                EditorGUILayout.HelpBox( rootScaleValidationMessage, MessageType.Warning );
            }

            using ( new EditorGUI.DisabledScope( canCreate == false ) )
            {
                if ( GUILayout.Button( "몬스터 프리팹 생성", GUILayout.Height( 36.0f ) ) )
                {
                    CreateMonsterPrefab();
                }
            }

            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        ///<summary>
        /// 생성 옵션 섹션 렌더링
        ///</summary>
        private void DrawCreateOptionSection()
        {
            float newRootScale = EditorGUILayout.FloatField( "Root Scale", rootScale );

            if ( Mathf.Approximately( newRootScale, rootScale ) == false )
            {
                rootScale = newRootScale;
            }
        }

        ///<summary>
        /// 생성 이름 섹션 렌더링
        ///</summary>
        private void DrawCreateNameSection()
        {
            bool newUseManualPrefabName = EditorGUILayout.ToggleLeft( "프리팹 이름 수동 설정", useManualPrefabName );

            if ( newUseManualPrefabName != useManualPrefabName )
            {
                useManualPrefabName = newUseManualPrefabName;
            }

            if ( useManualPrefabName == false )
            {
                EditorGUILayout.LabelField( "Prefab Name", GetTargetPrefabName() );
                return;
            }

            string newManualPrefabName = EditorGUILayout.TextField( "Prefab Name", manualPrefabName );

            if ( string.Equals( newManualPrefabName, manualPrefabName, StringComparison.Ordinal ) == false )
            {
                manualPrefabName = newManualPrefabName;
            }
        }

        ///<summary>
        /// 소스 프리팹 목록 갱신
        ///</summary>
        private void RefreshSourcePrefabInfos()
        {
            sourcePrefabInfos.Clear();
            AppendSourcePrefabInfos( SourceFolderPathOne, "EnemyMonster 1" );
            AppendSourcePrefabInfos( SourceFolderPathTwo, "EnemyMonster 2" );

            if ( sourcePrefabInfos.Count == 0 )
            {
                selectedSourceIndex = -1;
                SetStatus( "소스 프리팹을 찾지 못했습니다.", MessageType.Warning );
                return;
            }

            if ( selectedSourceIndex < 0 || selectedSourceIndex >= sourcePrefabInfos.Count )
            {
                selectedSourceIndex = 0;
            }

            if ( useManualPrefabName == false )
            {
                manualPrefabName = string.Empty;
            }

            SetStatus( $"소스 프리팹 {sourcePrefabInfos.Count}개를 불러왔습니다.", MessageType.Info );
        }

        ///<summary>
        /// 소스 프리팹 목록 추가
        ///</summary>
        private void AppendSourcePrefabInfos( string _sourceFolderPath, string _sourceGroup )
        {
            if ( AssetDatabase.IsValidFolder( _sourceFolderPath ) == false )
            {
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets( "t:Prefab", new string[] { _sourceFolderPath } );
            Array.Sort( assetGuids, CompareAssetGuid );

            for ( int index = 0; index < assetGuids.Length; index++ )
            {
                string assetGuid = assetGuids[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                string fileName = Path.GetFileNameWithoutExtension( assetPath );
                SourceMonsterPrefabInfo sourcePrefabInfo = new SourceMonsterPrefabInfo();
                sourcePrefabInfo.sourceGroup = _sourceGroup;
                sourcePrefabInfo.displayName = fileName;
                sourcePrefabInfo.assetPath = assetPath;
                sourcePrefabInfos.Add( sourcePrefabInfo );
            }
        }

        ///<summary>
        /// 에셋 GUID 비교
        ///</summary>
        private int CompareAssetGuid( string _leftGuid, string _rightGuid )
        {
            string leftPath = AssetDatabase.GUIDToAssetPath( _leftGuid );
            string rightPath = AssetDatabase.GUIDToAssetPath( _rightGuid );
            int result = string.Compare( leftPath, rightPath, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 필터링된 소스 프리팹 목록 반환
        ///</summary>
        private List<SourceMonsterPrefabInfo> GetFilteredSourcePrefabInfos()
        {
            List<SourceMonsterPrefabInfo> filteredInfos = new List<SourceMonsterPrefabInfo>();

            for ( int index = 0; index < sourcePrefabInfos.Count; index++ )
            {
                SourceMonsterPrefabInfo sourcePrefabInfo = sourcePrefabInfos[ index ];

                if ( IsSearchMatch( sourcePrefabInfo, searchText ) == false )
                {
                    continue;
                }

                filteredInfos.Add( sourcePrefabInfo );
            }

            return filteredInfos;
        }

        ///<summary>
        /// 검색 일치 여부 반환
        ///</summary>
        protected override bool IsSearchMatch( SourceMonsterPrefabInfo _sourcePrefabInfo, string _searchText )
        {
            if ( _sourcePrefabInfo == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( _searchText ) )
            {
                return true;
            }

            string normalizedSearchText = _searchText.Trim();
            bool isDisplayNameMatched = _sourcePrefabInfo.displayName.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;

            if ( isDisplayNameMatched )
            {
                return true;
            }

            bool isGroupMatched = _sourcePrefabInfo.sourceGroup.IndexOf( normalizedSearchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return isGroupMatched;
        }

        ///<summary>
        /// 선택된 소스 프리팹 정보 반환
        ///</summary>
        private SourceMonsterPrefabInfo GetSelectedSourcePrefabInfo()
        {
            if ( selectedSourceIndex < 0 || selectedSourceIndex >= sourcePrefabInfos.Count )
            {
                return null;
            }

            SourceMonsterPrefabInfo result = sourcePrefabInfos[ selectedSourceIndex ];
            return result;
        }

        ///<summary>
        /// 소스 프리팹 에셋 반환
        ///</summary>
        private GameObject LoadSourcePrefabAsset( string _assetPath )
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>( _assetPath );
            return sourcePrefab;
        }

        ///<summary>
        /// 몬스터 프리팹 생성
        ///</summary>
        private void CreateMonsterPrefab()
        {
            SourceMonsterPrefabInfo selectedInfo = GetSelectedSourcePrefabInfo();
            bool isCreated = false;

            if ( selectedInfo == null )
            {
                SetStatus( "생성할 몬스터 소스 프리팹을 선택하세요.", MessageType.Warning );
                return;
            }

            string prefabNameValidationMessage = ValidateTargetPrefabName();

            if ( string.IsNullOrEmpty( prefabNameValidationMessage ) == false )
            {
                SetStatus( prefabNameValidationMessage, MessageType.Warning );
                return;
            }

            string rootScaleValidationMessage = ValidateRootScale();

            if ( string.IsNullOrEmpty( rootScaleValidationMessage ) == false )
            {
                SetStatus( rootScaleValidationMessage, MessageType.Warning );
                return;
            }

            EnsureTargetFolderExists();
            string targetPrefabPath = GetTargetPrefabPath();
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( selectedInfo.assetPath );

            try
            {
                string prefabName = Path.GetFileNameWithoutExtension( targetPrefabPath );
                prefabRoot.name = prefabName;
                ApplyRootSettings( prefabRoot );
                ApplyColliderAndRigidbodySettings( prefabRoot );
                ApplyContactHitboxSettings( prefabRoot );
                ApplyMonsterObjectReferences( prefabRoot );
                PrefabUtility.SaveAsPrefabAsset( prefabRoot, targetPrefabPath );
                isCreated = true;
                SetStatus( $"몬스터 프리팹 생성 완료: {targetPrefabPath}", MessageType.Info );

                if ( useManualPrefabName == false )
                {
                    manualPrefabName = string.Empty;
                }
            }
            catch ( Exception exception )
            {
                SetStatus( $"몬스터 프리팹 생성 실패: {exception.Message}", MessageType.Error );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( prefabRoot );
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if ( isCreated )
            {
                MonsterPrefabGeneratorUtility.QueueRefitMonsterPrefabCollider( targetPrefabPath );
            }
        }

        ///<summary>
        /// 루트 설정 적용
        ///</summary>
        private void ApplyRootSettings( GameObject _prefabRoot )
        {
            if ( _prefabRoot == null )
            {
                return;
            }

            SetLayerRecursively( _prefabRoot.transform, LayerMask.NameToLayer( MonsterLayerName ) );
            float appliedRootScale = rootScale;

            if ( appliedRootScale <= 0.0f )
            {
                appliedRootScale = DefaultRootScale;
            }

            _prefabRoot.transform.localScale = new Vector3( appliedRootScale, appliedRootScale, appliedRootScale );
            SortingGroup sortingGroup = _prefabRoot.GetComponent<SortingGroup>();

            if ( sortingGroup == null )
            {
                sortingGroup = _prefabRoot.AddComponent<SortingGroup>();
            }

            sortingGroup.sortingLayerName = MonsterSortingLayerName;
            sortingGroup.sortingOrder = 0;
        }

        ///<summary>
        /// 충돌체와 물리 설정 적용
        ///</summary>
        private void ApplyColliderAndRigidbodySettings( GameObject _prefabRoot )
        {
            if ( _prefabRoot == null )
            {
                return;
            }

            BoxCollider2D bodyCollider = _prefabRoot.GetComponent<BoxCollider2D>();

            if ( bodyCollider == null )
            {
                bodyCollider = _prefabRoot.AddComponent<BoxCollider2D>();
            }

            bodyCollider.isTrigger = false;
            bodyCollider.excludeLayers = LayerMask.GetMask( PlayerLayerName );

            Rigidbody2D targetRigidbody = _prefabRoot.GetComponent<Rigidbody2D>();

            if ( targetRigidbody == null )
            {
                targetRigidbody = _prefabRoot.AddComponent<Rigidbody2D>();
            }

            targetRigidbody.bodyType = RigidbodyType2D.Dynamic;
            targetRigidbody.gravityScale = DefaultGravityScale;
            targetRigidbody.freezeRotation = true;
            targetRigidbody.mass = 1.0f;
            targetRigidbody.linearDamping = 0.0f;
            targetRigidbody.angularDamping = 0.05f;
            targetRigidbody.sleepMode = RigidbodySleepMode2D.StartAwake;
            targetRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        }

        ///<summary>
        /// 접촉 히트박스 설정 적용
        ///</summary>
        private void ApplyContactHitboxSettings( GameObject _prefabRoot )
        {
            if ( _prefabRoot == null )
            {
                return;
            }

            Transform contactHitboxTransform = _prefabRoot.transform.Find( ContactHitboxObjectName );

            if ( contactHitboxTransform == null )
            {
                GameObject contactHitboxObject = new GameObject( ContactHitboxObjectName );
                contactHitboxTransform = contactHitboxObject.transform;
                contactHitboxTransform.SetParent( _prefabRoot.transform, false );
            }

            int monsterLayer = LayerMask.NameToLayer( MonsterLayerName );

            if ( monsterLayer >= 0 )
            {
                contactHitboxTransform.gameObject.layer = monsterLayer;
            }

            BoxCollider2D bodyCollider = _prefabRoot.GetComponent<BoxCollider2D>();
            BoxCollider2D contactHitboxCollider = contactHitboxTransform.GetComponent<BoxCollider2D>();

            if ( contactHitboxCollider == null )
            {
                contactHitboxCollider = contactHitboxTransform.gameObject.AddComponent<BoxCollider2D>();
            }

            contactHitboxCollider.isTrigger = true;

            if ( bodyCollider != null )
            {
                contactHitboxCollider.offset = bodyCollider.offset;
                contactHitboxCollider.size = bodyCollider.size;
            }

            MonsterContactHitbox contactHitbox = contactHitboxTransform.GetComponent<MonsterContactHitbox>();

            if ( contactHitbox == null )
            {
                contactHitboxTransform.gameObject.AddComponent<MonsterContactHitbox>();
            }
        }

        ///<summary>
        /// 몬스터 오브젝트 참조 설정 적용
        ///</summary>
        private void ApplyMonsterObjectReferences( GameObject _prefabRoot )
        {
            if ( _prefabRoot == null )
            {
                return;
            }

            MonsterObject monsterObject = _prefabRoot.GetComponent<MonsterObject>();

            if ( monsterObject == null )
            {
                monsterObject = _prefabRoot.AddComponent<MonsterObject>();
            }

            BoxCollider2D bodyCollider = _prefabRoot.GetComponent<BoxCollider2D>();
            Rigidbody2D targetRigidbody = _prefabRoot.GetComponent<Rigidbody2D>();
            Transform contactHitboxTransform = _prefabRoot.transform.Find( ContactHitboxObjectName );
            BoxCollider2D contactHitboxCollider = null;

            if ( contactHitboxTransform != null )
            {
                contactHitboxCollider = contactHitboxTransform.GetComponent<BoxCollider2D>();
            }

            SerializedObject serializedObject = new SerializedObject( monsterObject );
            SerializedProperty targetRigidbodyProperty = serializedObject.FindProperty( "targetRigidbody" );
            SerializedProperty bodyColliderProperty = serializedObject.FindProperty( "bodyCollider" );
            SerializedProperty contactHitboxColliderProperty = serializedObject.FindProperty( "contactHitboxCollider" );

            if ( targetRigidbodyProperty != null )
            {
                targetRigidbodyProperty.objectReferenceValue = targetRigidbody;
            }

            if ( bodyColliderProperty != null )
            {
                bodyColliderProperty.objectReferenceValue = bodyCollider;
            }

            if ( contactHitboxColliderProperty != null )
            {
                contactHitboxColliderProperty.objectReferenceValue = contactHitboxCollider;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        ///<summary>
        /// 레이어 일괄 적용
        ///</summary>
        private void SetLayerRecursively( Transform _targetTransform, int _layer )
        {
            if ( _targetTransform == null )
            {
                return;
            }

            if ( _layer >= 0 )
            {
                _targetTransform.gameObject.layer = _layer;
            }

            for ( int childIndex = 0; childIndex < _targetTransform.childCount; childIndex++ )
            {
                Transform childTransform = _targetTransform.GetChild( childIndex );
                SetLayerRecursively( childTransform, _layer );
            }
        }

        ///<summary>
        /// 대상 폴더 존재 보장
        ///</summary>
        private void EnsureTargetFolderExists()
        {
            if ( AssetDatabase.IsValidFolder( TargetFolderPath ) )
            {
                return;
            }

            string[] folderSegments = TargetFolderPath.Split( '/' );
            string currentPath = folderSegments[ 0 ];

            for ( int index = 1; index < folderSegments.Length; index++ )
            {
                string folderName = folderSegments[ index ];
                string combinedPath = $"{currentPath}/{folderName}";

                if ( AssetDatabase.IsValidFolder( combinedPath ) == false )
                {
                    AssetDatabase.CreateFolder( currentPath, folderName );
                }

                currentPath = combinedPath;
            }
        }

        ///<summary>
        /// 자동 프리팹 이름 구성
        ///</summary>
        private string BuildAutoPrefabName()
        {
            int nextIndex = GetNextMonsterIndex();
            string result = $"{MonsterNamePrefix}{nextIndex:D4}";
            return result;
        }

        ///<summary>
        /// 생성 대상 프리팹 이름 반환
        ///</summary>
        private string GetTargetPrefabName()
        {
            if ( useManualPrefabName )
            {
                string trimmedManualPrefabName = manualPrefabName.Trim();
                return trimmedManualPrefabName;
            }

            string result = BuildAutoPrefabName();
            return result;
        }

        ///<summary>
        /// 생성 대상 프리팹 경로 반환
        ///</summary>
        private string GetTargetPrefabPath()
        {
            string prefabName = GetTargetPrefabName();
            string result = $"{TargetFolderPath}/{prefabName}.prefab";
            return result;
        }

        ///<summary>
        /// 다음 몬스터 번호 반환
        ///</summary>
        private int GetNextMonsterIndex()
        {
            string[] assetGuids = AssetDatabase.FindAssets( "t:Prefab", new string[] { TargetFolderPath } );
            int maxIndex = 0;

            for ( int index = 0; index < assetGuids.Length; index++ )
            {
                string assetGuid = assetGuids[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                string fileName = Path.GetFileNameWithoutExtension( assetPath );
                Match indexMatch = Regex.Match( fileName, @"^Monster_(\d{4})$" );

                if ( indexMatch.Success == false )
                {
                    continue;
                }

                bool isParsed = int.TryParse( indexMatch.Groups[ 1 ].Value, out int parsedIndex );

                if ( isParsed == false )
                {
                    continue;
                }

                maxIndex = Mathf.Max( maxIndex, parsedIndex );
            }

            int result = maxIndex + 1;
            return result;
        }

        ///<summary>
        /// 생성 대상 프리팹 이름 검증
        ///</summary>
        private string ValidateTargetPrefabName()
        {
            string prefabName = GetTargetPrefabName();

            if ( string.IsNullOrWhiteSpace( prefabName ) )
            {
                string result = "프리팹 이름을 입력하세요.";
                return result;
            }

            char[] invalidFileNameCharacters = Path.GetInvalidFileNameChars();

            for ( int index = 0; index < invalidFileNameCharacters.Length; index++ )
            {
                char invalidFileNameCharacter = invalidFileNameCharacters[ index ];

                if ( prefabName.IndexOf( invalidFileNameCharacter ) >= 0 )
                {
                    string result = "프리팹 이름에 사용할 수 없는 문자가 포함되어 있습니다.";
                    return result;
                }
            }

            string targetPrefabPath = $"{TargetFolderPath}/{prefabName}.prefab";
            bool isAlreadyExists = File.Exists( targetPrefabPath );

            if ( isAlreadyExists )
            {
                string result = "같은 이름의 프리팹이 이미 존재합니다.";
                return result;
            }

            string validationResult = string.Empty;
            return validationResult;
        }

        ///<summary>
        /// 루트 스케일 값 검증
        ///</summary>
        private string ValidateRootScale()
        {
            if ( rootScale <= 0.0f )
            {
                string result = "Root Scale은 0보다 커야 합니다.";
                return result;
            }

            string validationResult = string.Empty;
            return validationResult;
        }

        ///<summary>
        /// 키보드 선택 이동 처리
        ///</summary>
        private void HandleKeyboardNavigation()
        {
            bool hasDirection = TryGetKeyboardNavigationDirection( out int direction );

            if ( hasDirection )
            {
                MoveSelectionInFilteredList( direction );
            }
        }

        ///<summary>
        /// 필터링 목록 선택 이동
        ///</summary>
        private void MoveSelectionInFilteredList( int _direction )
        {
            List<SourceMonsterPrefabInfo> filteredInfos = GetFilteredSourcePrefabInfos();

            if ( filteredInfos.Count == 0 )
            {
                return;
            }

            int filteredSelectedIndex = 0;
            SourceMonsterPrefabInfo selectedInfo = GetSelectedSourcePrefabInfo();

            if ( selectedInfo != null )
            {
                int resolvedIndex = filteredInfos.IndexOf( selectedInfo );

                if ( resolvedIndex >= 0 )
                {
                    filteredSelectedIndex = resolvedIndex;
                }
            }

            int lastIndex = filteredInfos.Count - 1;
            int nextFilteredIndex = Mathf.Clamp( filteredSelectedIndex + _direction, 0, lastIndex );
            SourceMonsterPrefabInfo nextInfo = filteredInfos[ nextFilteredIndex ];
            selectedSourceIndex = sourcePrefabInfos.IndexOf( nextInfo );
            isPendingFocusToSelection = true;
            EnsureSelectionVisibleByIndex( nextFilteredIndex, filteredInfos.Count );
            Repaint();
        }

        ///<summary>
        /// 선택 항목 노출 스크롤 조정
        ///</summary>
        private void EnsureSelectionVisibleByIndex( int _filteredSelectedIndex, int _filteredItemCount )
        {
            float itemStride = SourceListItemHeight + SourceListItemSpacing;
            float itemTop = _filteredSelectedIndex * itemStride;
            float itemBottom = itemTop + SourceListItemHeight;
            float contentHeight = Mathf.Max( 0.0f, _filteredItemCount * itemStride );
            float maxScrollY = Mathf.Max( 0.0f, contentHeight - SourceListViewHeight );

            if ( itemTop < sourceListScrollPosition.y )
            {
                sourceListScrollPosition.y = itemTop;
            }
            else if ( itemBottom > sourceListScrollPosition.y + SourceListViewHeight )
            {
                sourceListScrollPosition.y = itemBottom - SourceListViewHeight;
            }

            sourceListScrollPosition.y = Mathf.Clamp( sourceListScrollPosition.y, 0.0f, maxScrollY );
        }

        ///<summary>
        /// 소스 목록 컨트롤 이름 반환
        ///</summary>
        private string BuildSourceItemControlName( int _sourceIndex )
        {
            string result = $"MonsterSourceItem_{_sourceIndex}";
            return result;
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

    ///<summary>
    /// 몬스터 프리팹 콜라이더 보정 유틸리티
    ///</summary>
    public static class MonsterPrefabGeneratorUtility
    {
        private const string BodyBoneObjectName = "Body_bone";
        private const string ContactHitboxObjectName = "ContactHitbox";
        private const string PlayerLayerName = "Player";
        private const string Monster0003PrefabPath = "Assets/Resources/Prefabs/Character/Monster/Monster_0003.prefab";
        private const string Monster0004PrefabPath = "Assets/Resources/Prefabs/Character/Monster/Monster_0004.prefab";
        private static readonly HashSet<string> PendingRefitPaths = new HashSet<string>();

        ///<summary>
        /// 생성 몬스터 프리팹 콜라이더 재보정 메뉴
        ///</summary>
        [MenuItem( "Tools/TinyHero/Refit Generated Monster Colliders" )]
        public static void RefitGeneratedMonsterColliders()
        {
            string result0003 = RefitMonsterPrefabCollider( Monster0003PrefabPath );
            string result0004 = RefitMonsterPrefabCollider( Monster0004PrefabPath );
            Debug.Log( $"{result0003}\n{result0004}" );
        }

        ///<summary>
        /// 프리팹 콜라이더 지연 재보정 예약
        ///</summary>
        public static void QueueRefitMonsterPrefabCollider( string _prefabPath )
        {
            if ( string.IsNullOrWhiteSpace( _prefabPath ) )
            {
                return;
            }

            bool isAdded = PendingRefitPaths.Add( _prefabPath );

            if ( isAdded == false )
            {
                return;
            }

            EditorApplication.delayCall += ProcessPendingRefitMonsterPrefabColliders;
        }

        ///<summary>
        /// 예약된 프리팹 콜라이더 재보정 처리
        ///</summary>
        private static void ProcessPendingRefitMonsterPrefabColliders()
        {
            EditorApplication.delayCall -= ProcessPendingRefitMonsterPrefabColliders;
            string[] prefabPaths = new string[ PendingRefitPaths.Count ];
            PendingRefitPaths.CopyTo( prefabPaths );
            PendingRefitPaths.Clear();

            for ( int index = 0; index < prefabPaths.Length; index++ )
            {
                string prefabPath = prefabPaths[ index ];
                RefitMonsterPrefabCollider( prefabPath );
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        ///<summary>
        /// Body_bone 기준 콜라이더 경계 계산
        ///</summary>
        public static Bounds CalculateBodyBoneSpriteBounds( GameObject _prefabRoot )
        {
            Transform bodyBoneTransform = FindChildTransformRecursive( _prefabRoot.transform, BodyBoneObjectName );

            if ( bodyBoneTransform == null )
            {
                Bounds fallbackBounds = new Bounds( new Vector3( 0.0f, 0.5f, 0.0f ), Vector3.one );
                return fallbackBounds;
            }

            SpriteRenderer[] spriteRenderers = bodyBoneTransform.GetComponentsInChildren<SpriteRenderer>( false );
            bool hasBounds = false;
            Bounds result = new Bounds();

            for ( int index = 0; index < spriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRenderers[ index ];

                if ( spriteRenderer == null || spriteRenderer.sprite == null )
                {
                    continue;
                }

                if ( spriteRenderer.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                Bounds rendererBounds = spriteRenderer.bounds;
                Vector3 rendererMin = rendererBounds.min;
                Vector3 rendererMax = rendererBounds.max;
                Vector3[] worldCorners = new Vector3[ 4 ];
                worldCorners[ 0 ] = new Vector3( rendererMin.x, rendererMin.y, 0.0f );
                worldCorners[ 1 ] = new Vector3( rendererMin.x, rendererMax.y, 0.0f );
                worldCorners[ 2 ] = new Vector3( rendererMax.x, rendererMin.y, 0.0f );
                worldCorners[ 3 ] = new Vector3( rendererMax.x, rendererMax.y, 0.0f );

                for ( int cornerIndex = 0; cornerIndex < worldCorners.Length; cornerIndex++ )
                {
                    Vector3 worldCorner = worldCorners[ cornerIndex ];
                    Vector3 localCorner = _prefabRoot.transform.InverseTransformPoint( worldCorner );

                    if ( hasBounds == false )
                    {
                        result = new Bounds( localCorner, Vector3.zero );
                        hasBounds = true;
                        continue;
                    }

                    result.Encapsulate( localCorner );
                }
            }

            if ( hasBounds == false )
            {
                Bounds fallbackBounds = new Bounds( new Vector3( 0.0f, 0.5f, 0.0f ), Vector3.one );
                return fallbackBounds;
            }

            return result;
        }

        ///<summary>
        /// 하위 트랜스폼 재귀 탐색
        ///</summary>
        private static Transform FindChildTransformRecursive( Transform _rootTransform, string _targetName )
        {
            if ( _rootTransform == null )
            {
                return null;
            }

            if ( string.Equals( _rootTransform.name, _targetName, StringComparison.Ordinal ) )
            {
                return _rootTransform;
            }

            for ( int childIndex = 0; childIndex < _rootTransform.childCount; childIndex++ )
            {
                Transform childTransform = _rootTransform.GetChild( childIndex );
                Transform foundTransform = FindChildTransformRecursive( childTransform, _targetName );

                if ( foundTransform != null )
                {
                    return foundTransform;
                }
            }

            return null;
        }

        ///<summary>
        /// 프리팹 콜라이더 재보정 처리
        ///</summary>
        public static string RefitMonsterPrefabCollider( string _prefabPath )
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( _prefabPath );

            try
            {
                if ( prefabRoot == null )
                {
                    string nullResult = $"Load failed: {_prefabPath}";
                    return nullResult;
                }

                Bounds bodyBounds = CalculateBodyBoneSpriteBounds( prefabRoot );
                BoxCollider2D bodyCollider = prefabRoot.GetComponent<BoxCollider2D>();

                if ( bodyCollider == null )
                {
                    bodyCollider = prefabRoot.AddComponent<BoxCollider2D>();
                }

                bodyCollider.offset = bodyBounds.center;
                bodyCollider.size = bodyBounds.size;
                bodyCollider.isTrigger = false;
                bodyCollider.excludeLayers = LayerMask.GetMask( PlayerLayerName );

                Transform contactHitboxTransform = prefabRoot.transform.Find( ContactHitboxObjectName );

                if ( contactHitboxTransform != null )
                {
                    BoxCollider2D contactHitboxCollider = contactHitboxTransform.GetComponent<BoxCollider2D>();

                    if ( contactHitboxCollider == null )
                    {
                        contactHitboxCollider = contactHitboxTransform.gameObject.AddComponent<BoxCollider2D>();
                    }

                    contactHitboxCollider.offset = bodyBounds.center;
                    contactHitboxCollider.size = bodyBounds.size;
                    contactHitboxCollider.isTrigger = true;
                }

                PrefabUtility.SaveAsPrefabAsset( prefabRoot, _prefabPath );
                string result = $"Updated {_prefabPath}";
                return result;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( prefabRoot );
            }
        }
    }
}
