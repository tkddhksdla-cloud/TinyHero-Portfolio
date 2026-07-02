using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Quest;
using TinyHero.Skill;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// TinyHero 데이터 검증 에디터 창
    ///</summary>
    public sealed class TinyHeroDataValidationWindow : EditorWindow
    {
        private const string MenuPath = "Tools/TinyHero/Data Validation Dashboard";
        private const string ItemDefinitionSearchRootPath = "Assets/Resources/Data/Item/Definitions";
        private const string QuestDefinitionSearchRootPath = "Assets/Resources/Data/Quest/Definitions";
        private const string ShopDefinitionSearchRootPath = "Assets/Resources/Data/Shop/Definitions";
        private const string SkillDefinitionSearchRootPath = "Assets/Resources/Data/Skill/Definitions";
        private const string RandomBoxRewardTableSearchRootPath = "Assets/Resources/Data/Item/RandomBoxes";
        private const string AddressableGroupName = "TinyHero_Local";
        private const float SummaryBoxHeight = 58.0f;
        private const float ResultRowMinHeight = 52.0f;

        [SerializeField] private List<CValidationResult> validationResultList = new List<CValidationResult>();
        [SerializeField] private int selectedSeverityFilterIndex;
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private string lastValidationSummaryText = "Run Validation으로 프로젝트 데이터를 검사하세요.";
        [SerializeField] private MessageType lastValidationMessageType = MessageType.Info;

        private Vector2 resultScrollPosition;

        private static readonly string[] SeverityFilterOptionArray =
        {
            "ALL",
            eValidationSeverity.ERROR.ToString(),
            eValidationSeverity.WARNING.ToString(),
            eValidationSeverity.INFO.ToString()
        };

        ///<summary>
        /// 검증 결과 심각도
        ///</summary>
        private enum eValidationSeverity
        {
            ERROR,
            WARNING,
            INFO
        }

        ///<summary>
        /// 검증 결과 데이터
        ///</summary>
        [Serializable]
        private sealed class CValidationResult
        {
            public eValidationSeverity severity;
            public string category;
            public string title;
            public string message;
            public string assetPath;

            ///<summary>
            /// 검증 결과 초기화
            ///</summary>
            public CValidationResult( eValidationSeverity _severity, string _category, string _title, string _message, string _assetPath )
            {
                severity = _severity;
                category = _category;
                title = _title;
                message = _message;
                assetPath = _assetPath;
            }
        }

        ///<summary>
        /// 데이터 검증 창 표시
        ///</summary>
        [MenuItem( MenuPath )]
        private static void ShowWindow()
        {
            TinyHeroDataValidationWindow window = GetWindow<TinyHeroDataValidationWindow>();
            window.titleContent = new GUIContent( "Data Validation Dashboard" );
            window.minSize = new Vector2( 1120.0f, 720.0f );
            window.Show();
        }

        ///<summary>
        /// 데이터 검증 창 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            if ( validationResultList == null )
            {
                validationResultList = new List<CValidationResult>();
            }
        }

        ///<summary>
        /// 데이터 검증 창 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Data Validation Dashboard", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "아이템, 퀘스트, 상점, 스킬, 보상 테이블, Addressables key를 통합 검증합니다.", MessageType.None );
            EditorGUILayout.Space();
            DrawToolbarSection();
            EditorGUILayout.Space();
            DrawSummarySection();
            EditorGUILayout.Space();
            DrawResultListSection();
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Run Validation", GUILayout.Width( 140.0f ) ) )
            {
                RunValidation();
            }

            if ( GUILayout.Button( "Clear", GUILayout.Width( 80.0f ) ) )
            {
                ClearResults();
            }

            GUILayout.Space( 12.0f );
            EditorGUILayout.LabelField( "Severity", GUILayout.Width( 54.0f ) );
            selectedSeverityFilterIndex = EditorGUILayout.Popup( selectedSeverityFilterIndex, SeverityFilterOptionArray, GUILayout.Width( 120.0f ) );
            GUILayout.Space( 12.0f );
            EditorGUILayout.LabelField( "Search", GUILayout.Width( 44.0f ) );
            searchText = EditorGUILayout.TextField( searchText );
            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 검증 요약 영역 렌더링
        ///</summary>
        private void DrawSummarySection()
        {
            EditorGUILayout.BeginVertical( "box", GUILayout.Height( SummaryBoxHeight ) );
            EditorGUILayout.HelpBox( lastValidationSummaryText, lastValidationMessageType );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 검증 결과 목록 렌더링
        ///</summary>
        private void DrawResultListSection()
        {
            List<CValidationResult> filteredResultList = GetFilteredResultList();
            EditorGUILayout.LabelField( $"Results: {filteredResultList.Count} / {validationResultList.Count}", EditorStyles.boldLabel );
            resultScrollPosition = EditorGUILayout.BeginScrollView( resultScrollPosition );

            if ( filteredResultList.Count == 0 )
            {
                EditorGUILayout.HelpBox( "표시할 검증 결과가 없습니다.", MessageType.Info );
            }

            for ( int index = 0; index < filteredResultList.Count; index++ )
            {
                CValidationResult validationResult = filteredResultList[ index ];
                DrawResultRow( validationResult );
            }

            EditorGUILayout.EndScrollView();
        }

        ///<summary>
        /// 단일 검증 결과 행 렌더링
        ///</summary>
        private void DrawResultRow( CValidationResult _validationResult )
        {
            if ( _validationResult == null )
            {
                return;
            }

            EditorGUILayout.BeginVertical( "box", GUILayout.MinHeight( ResultRowMinHeight ) );
            EditorGUILayout.BeginHorizontal();
            GUIStyle severityStyle = new GUIStyle( EditorStyles.boldLabel );
            severityStyle.normal.textColor = ResolveSeverityColor( _validationResult.severity );
            EditorGUILayout.LabelField( _validationResult.severity.ToString(), severityStyle, GUILayout.Width( 80.0f ) );
            EditorGUILayout.LabelField( _validationResult.category, GUILayout.Width( 140.0f ) );
            EditorGUILayout.LabelField( _validationResult.title, EditorStyles.boldLabel );

            if ( string.IsNullOrWhiteSpace( _validationResult.assetPath ) == false && GUILayout.Button( "Ping", GUILayout.Width( 60.0f ) ) )
            {
                PingAsset( _validationResult.assetPath );
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField( _validationResult.message, EditorStyles.wordWrappedLabel );

            if ( string.IsNullOrWhiteSpace( _validationResult.assetPath ) == false )
            {
                EditorGUILayout.LabelField( _validationResult.assetPath, EditorStyles.miniLabel );
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 검증 실행
        ///</summary>
        private void RunValidation()
        {
            validationResultList.Clear();
            List<CItemDefinition> itemDefinitionList = LoadAssets<CItemDefinition>( ItemDefinitionSearchRootPath );
            List<CQuestDefinition> questDefinitionList = LoadAssets<CQuestDefinition>( QuestDefinitionSearchRootPath );
            List<CShopDefinition> shopDefinitionList = LoadAssets<CShopDefinition>( ShopDefinitionSearchRootPath );
            List<CSkillDefinition> skillDefinitionList = LoadAssets<CSkillDefinition>( SkillDefinitionSearchRootPath );
            List<CRandomBoxRewardTable> randomBoxRewardTableList = LoadAssets<CRandomBoxRewardTable>( RandomBoxRewardTableSearchRootPath );

            ValidateItemDefinitions( itemDefinitionList, skillDefinitionList );
            ValidateQuestDefinitions( questDefinitionList, itemDefinitionList );
            ValidateShopDefinitions( shopDefinitionList, itemDefinitionList );
            ValidateSkillDefinitions( skillDefinitionList );
            ValidateRandomBoxRewardTables( randomBoxRewardTableList );
            ValidateAddressableKeys();
            SortResults();
            UpdateSummary();
        }

        ///<summary>
        /// 검증 결과 초기화
        ///</summary>
        private void ClearResults()
        {
            validationResultList.Clear();
            lastValidationSummaryText = "검증 결과를 초기화했습니다.";
            lastValidationMessageType = MessageType.Info;
        }

        ///<summary>
        /// 아이템 정의 검증
        ///</summary>
        private void ValidateItemDefinitions( List<CItemDefinition> _itemDefinitionList, List<CSkillDefinition> _skillDefinitionList )
        {
            ValidateIdSet<CItemDefinition>( _itemDefinitionList, "Item", "Item Id", GetItemId, ItemDefinitionSearchRootPath );
            HashSet<string> skillIdSet = BuildIdSet<CSkillDefinition>( _skillDefinitionList, GetSkillId );

            for ( int index = 0; index < _itemDefinitionList.Count; index++ )
            {
                CItemDefinition itemDefinition = _itemDefinitionList[ index ];

                if ( itemDefinition == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( itemDefinition );
                ValidateItemSellPrice( itemDefinition, assetPath );
                ValidateSkillBookLink( itemDefinition, skillIdSet, assetPath );
                ValidateRandomBoxLink( itemDefinition, assetPath );
            }
        }

        ///<summary>
        /// 아이템 판매 가격 검증
        ///</summary>
        private void ValidateItemSellPrice( CItemDefinition _itemDefinition, string _assetPath )
        {
            if ( _itemDefinition.HasSellPrice() == false )
            {
                return;
            }

            string sellPriceItemId = _itemDefinition.GetSellPriceItemId();

            if ( string.IsNullOrWhiteSpace( sellPriceItemId ) )
            {
                AddResult( eValidationSeverity.ERROR, "Item", "Empty sell price item id", $"{_itemDefinition.GetItemId()}의 판매 가격 아이템 ID가 비어 있습니다.", _assetPath );
                return;
            }
        }

        ///<summary>
        /// 스킬북 연결 스킬 검증
        ///</summary>
        private void ValidateSkillBookLink( CItemDefinition _itemDefinition, HashSet<string> _skillIdSet, string _assetPath )
        {
            if ( _itemDefinition.IsSkillBook() == false )
            {
                return;
            }

            string linkedSkillId = _itemDefinition.GetLinkedSkillId();

            if ( string.IsNullOrWhiteSpace( linkedSkillId ) )
            {
                AddResult( eValidationSeverity.ERROR, "Item", "Empty linked skill id", $"{_itemDefinition.GetItemId()} 스킬북의 연결 스킬 ID가 비어 있습니다.", _assetPath );
                return;
            }

            if ( _skillIdSet.Contains( linkedSkillId ) == false )
            {
                AddResult( eValidationSeverity.ERROR, "Item", "Missing linked skill", $"{_itemDefinition.GetItemId()} 스킬북이 존재하지 않는 스킬 ID를 참조합니다. SkillId: {linkedSkillId}", _assetPath );
            }
        }

        ///<summary>
        /// 랜덤상자 연결 보상 테이블 검증
        ///</summary>
        private void ValidateRandomBoxLink( CItemDefinition _itemDefinition, string _assetPath )
        {
            if ( _itemDefinition.IsRandomBox() == false )
            {
                return;
            }

            if ( _itemDefinition.GetRandomBoxRewardTable() == null )
            {
                AddResult( eValidationSeverity.ERROR, "Item", "Missing random box table", $"{_itemDefinition.GetItemId()} 랜덤상자에 보상 테이블이 연결되지 않았습니다.", _assetPath );
            }
        }

        ///<summary>
        /// 퀘스트 정의 검증
        ///</summary>
        private void ValidateQuestDefinitions( List<CQuestDefinition> _questDefinitionList, List<CItemDefinition> _itemDefinitionList )
        {
            ValidateIdSet<CQuestDefinition>( _questDefinitionList, "Quest", "Quest Id", GetQuestId, QuestDefinitionSearchRootPath );

            for ( int index = 0; index < _questDefinitionList.Count; index++ )
            {
                CQuestDefinition questDefinition = _questDefinitionList[ index ];

                if ( questDefinition == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( questDefinition );
                ValidateQuestConditions( questDefinition, assetPath );
                ValidateQuestRewards( questDefinition, assetPath );
            }
        }

        ///<summary>
        /// 퀘스트 조건 검증
        ///</summary>
        private void ValidateQuestConditions( CQuestDefinition _questDefinition, string _assetPath )
        {
            List<CQuestConditionEntry> conditionEntryList = _questDefinition.GetConditionEntryList();

            if ( conditionEntryList == null || conditionEntryList.Count == 0 )
            {
                AddResult( eValidationSeverity.WARNING, "Quest", "Empty condition list", $"{_questDefinition.GetQuestId()} 퀘스트에 조건이 없습니다.", _assetPath );
                return;
            }

            for ( int index = 0; index < conditionEntryList.Count; index++ )
            {
                CQuestConditionEntry conditionEntry = conditionEntryList[ index ];

                if ( conditionEntry == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Quest", "Null condition", $"{_questDefinition.GetQuestId()} 퀘스트의 조건 {index}가 비어 있습니다.", _assetPath );
                    continue;
                }

                if ( conditionEntry.GetConditionType() == eQuestConditionType.KILL_MONSTER && string.IsNullOrWhiteSpace( conditionEntry.GetTargetMonsterId() ) )
                {
                    AddResult( eValidationSeverity.ERROR, "Quest", "Empty target monster id", $"{_questDefinition.GetQuestId()} 퀘스트의 처치 조건 대상 몬스터 ID가 비어 있습니다.", _assetPath );
                }

                if ( conditionEntry.GetConditionType() == eQuestConditionType.TURN_IN_ITEM && conditionEntry.GetTargetItemDefinition() == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Quest", "Missing turn-in item", $"{_questDefinition.GetQuestId()} 퀘스트의 아이템 제출 조건에 아이템 정의가 없습니다.", _assetPath );
                }
            }
        }

        ///<summary>
        /// 퀘스트 보상 검증
        ///</summary>
        private void ValidateQuestRewards( CQuestDefinition _questDefinition, string _assetPath )
        {
            List<CQuestRewardEntry> rewardEntryList = _questDefinition.GetRewardEntryList();

            if ( rewardEntryList == null || rewardEntryList.Count == 0 )
            {
                AddResult( eValidationSeverity.WARNING, "Quest", "Empty reward list", $"{_questDefinition.GetQuestId()} 퀘스트에 보상이 없습니다.", _assetPath );
                return;
            }

            for ( int index = 0; index < rewardEntryList.Count; index++ )
            {
                CQuestRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Quest", "Null reward", $"{_questDefinition.GetQuestId()} 퀘스트의 보상 {index}가 비어 있습니다.", _assetPath );
                    continue;
                }

                if ( rewardEntry.GetRewardType() == eQuestRewardType.ITEM && rewardEntry.GetItemDefinition() == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Quest", "Missing reward item", $"{_questDefinition.GetQuestId()} 퀘스트의 아이템 보상에 아이템 정의가 없습니다.", _assetPath );
                }
            }
        }

        ///<summary>
        /// 상점 정의 검증
        ///</summary>
        private void ValidateShopDefinitions( List<CShopDefinition> _shopDefinitionList, List<CItemDefinition> _itemDefinitionList )
        {
            ValidateIdSet<CShopDefinition>( _shopDefinitionList, "Shop", "Shop Id", GetShopId, ShopDefinitionSearchRootPath );
            HashSet<string> itemIdSet = BuildIdSet<CItemDefinition>( _itemDefinitionList, GetItemId );

            for ( int index = 0; index < _shopDefinitionList.Count; index++ )
            {
                CShopDefinition shopDefinition = _shopDefinitionList[ index ];

                if ( shopDefinition == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( shopDefinition );
                List<CShopEntryData> shopEntryDataList = shopDefinition.GetShopEntryDataList();

                if ( shopEntryDataList == null || shopEntryDataList.Count == 0 )
                {
                    AddResult( eValidationSeverity.WARNING, "Shop", "Empty shop entries", $"{shopDefinition.GetShopId()} 상점에 판매 항목이 없습니다.", assetPath );
                    continue;
                }

                ValidateShopEntries( shopDefinition, shopEntryDataList, itemIdSet, assetPath );
            }
        }

        ///<summary>
        /// 상점 판매 항목 검증
        ///</summary>
        private void ValidateShopEntries( CShopDefinition _shopDefinition, List<CShopEntryData> _shopEntryDataList, HashSet<string> _itemIdSet, string _assetPath )
        {
            for ( int index = 0; index < _shopEntryDataList.Count; index++ )
            {
                CShopEntryData shopEntryData = _shopEntryDataList[ index ];

                if ( shopEntryData == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Shop", "Null shop entry", $"{_shopDefinition.GetShopId()} 상점의 판매 항목 {index}가 비어 있습니다.", _assetPath );
                    continue;
                }

                ValidateShopItemId( _shopDefinition, shopEntryData.GetItemId(), _itemIdSet, "Sale item", _assetPath );
                ValidateShopItemId( _shopDefinition, shopEntryData.GetPriceItemId(), _itemIdSet, "Price item", _assetPath );
            }
        }

        ///<summary>
        /// 상점 아이템 ID 검증
        ///</summary>
        private void ValidateShopItemId( CShopDefinition _shopDefinition, string _itemId, HashSet<string> _itemIdSet, string _label, string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _itemId ) )
            {
                AddResult( eValidationSeverity.ERROR, "Shop", $"Empty {_label}", $"{_shopDefinition.GetShopId()} 상점의 {_label} ID가 비어 있습니다.", _assetPath );
                return;
            }

            if ( _itemIdSet.Contains( _itemId ) == false )
            {
                AddResult( eValidationSeverity.ERROR, "Shop", $"Missing {_label}", $"{_shopDefinition.GetShopId()} 상점이 존재하지 않는 아이템 ID를 참조합니다. ItemId: {_itemId}", _assetPath );
            }
        }

        ///<summary>
        /// 스킬 정의 검증
        ///</summary>
        private void ValidateSkillDefinitions( List<CSkillDefinition> _skillDefinitionList )
        {
            ValidateIdSet<CSkillDefinition>( _skillDefinitionList, "Skill", "Skill Id", GetSkillId, SkillDefinitionSearchRootPath );

            for ( int index = 0; index < _skillDefinitionList.Count; index++ )
            {
                CSkillDefinition skillDefinition = _skillDefinitionList[ index ];

                if ( skillDefinition == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( skillDefinition );

                if ( skillDefinition.GetSkillType() == eSkillType.ACTIVE && skillDefinition.GetActiveSkillEffect() == null && skillDefinition.GetActiveAction() == null )
                {
                    AddResult( eValidationSeverity.WARNING, "Skill", "Missing active effect", $"{skillDefinition.GetSkillId()} 액티브 스킬에 실행 효과가 없습니다.", assetPath );
                }
            }
        }

        ///<summary>
        /// 랜덤상자 보상 테이블 검증
        ///</summary>
        private void ValidateRandomBoxRewardTables( List<CRandomBoxRewardTable> _randomBoxRewardTableList )
        {
            for ( int index = 0; index < _randomBoxRewardTableList.Count; index++ )
            {
                CRandomBoxRewardTable rewardTable = _randomBoxRewardTableList[ index ];

                if ( rewardTable == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( rewardTable );
                IReadOnlyList<CRandomBoxRewardEntry> rewardEntryList = rewardTable.GetRewardEntryList();

                if ( rewardEntryList == null || rewardEntryList.Count == 0 )
                {
                    AddResult( eValidationSeverity.WARNING, "Reward", "Empty random box table", $"{rewardTable.name} 보상 테이블에 엔트리가 없습니다.", assetPath );
                    continue;
                }

                ValidateRandomBoxRewardEntries( rewardTable, rewardEntryList, assetPath );
            }
        }

        ///<summary>
        /// 랜덤상자 보상 엔트리 검증
        ///</summary>
        private void ValidateRandomBoxRewardEntries( CRandomBoxRewardTable _rewardTable, IReadOnlyList<CRandomBoxRewardEntry> _rewardEntryList, string _assetPath )
        {
            for ( int index = 0; index < _rewardEntryList.Count; index++ )
            {
                CRandomBoxRewardEntry rewardEntry = _rewardEntryList[ index ];

                if ( rewardEntry == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Reward", "Null reward entry", $"{_rewardTable.name} 보상 테이블의 엔트리 {index}가 비어 있습니다.", _assetPath );
                    continue;
                }

                if ( rewardEntry.GetItemDefinition() == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Reward", "Missing reward item", $"{_rewardTable.name} 보상 테이블의 엔트리 {index}에 아이템 정의가 없습니다.", _assetPath );
                }

                if ( rewardEntry.GetWeight() <= 0.0f )
                {
                    AddResult( eValidationSeverity.WARNING, "Reward", "Zero reward weight", $"{_rewardTable.name} 보상 테이블의 엔트리 {index} 가중치가 0 이하입니다.", _assetPath );
                }
            }
        }

        ///<summary>
        /// Addressables key 검증
        ///</summary>
        private void ValidateAddressableKeys()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if ( settings == null )
            {
                AddResult( eValidationSeverity.ERROR, "Addressables", "Missing settings", "AddressableAssetSettings를 찾을 수 없습니다.", string.Empty );
                return;
            }

            AddressableAssetGroup group = settings.FindGroup( AddressableGroupName );

            if ( group == null )
            {
                AddResult( eValidationSeverity.ERROR, "Addressables", "Missing group", $"Addressables 그룹을 찾을 수 없습니다. Group: {AddressableGroupName}", string.Empty );
                return;
            }

            List<string> syncTargetAssetPathList = FindAddressableSyncTargetAssetPaths();

            for ( int index = 0; index < syncTargetAssetPathList.Count; index++ )
            {
                string assetPath = syncTargetAssetPathList[ index ];
                string guid = AssetDatabase.AssetPathToGUID( assetPath );
                AddressableAssetEntry entry = settings.FindAssetEntry( guid );
                string expectedAddress = BuildAddressableKey( assetPath );

                if ( entry == null )
                {
                    AddResult( eValidationSeverity.ERROR, "Addressables", "Missing addressable entry", $"Addressables entry가 없습니다. ExpectedKey: {expectedAddress}", assetPath );
                    continue;
                }

                if ( string.Equals( entry.address, expectedAddress, StringComparison.Ordinal ) == false )
                {
                    AddResult( eValidationSeverity.ERROR, "Addressables", "Address mismatch", $"Addressables key가 예상값과 다릅니다. Current: {entry.address}, Expected: {expectedAddress}", assetPath );
                }
            }
        }

        ///<summary>
        /// Addressables 동기화 대상 에셋 경로 목록 반환
        ///</summary>
        private List<string> FindAddressableSyncTargetAssetPaths()
        {
            List<string> assetPathList = new List<string>();
            AddAssetPaths( assetPathList, "Assets/Resources/MapData", "t:TextAsset" );
            AddAssetPaths( assetPathList, "Assets/Resources/RawImages/BG", "t:Texture2D" );
            AddAssetPaths( assetPathList, "Assets/Resources/Prefabs/UI/Popup", "t:Prefab" );
            AddAssetPaths( assetPathList, "Assets/Resources/Prefabs/Portal", "t:Prefab" );
            AddAssetPaths( assetPathList, "Assets/Resources/Prefabs/Character/Monster", "t:Prefab" );
            AddAssetPaths( assetPathList, "Assets/Resources/Prefabs/Character/NPC", "t:Prefab" );
            assetPathList.Sort( StringComparer.Ordinal );
            return assetPathList;
        }

        ///<summary>
        /// 특정 경로의 에셋 경로 추가
        ///</summary>
        private void AddAssetPaths( List<string> _assetPathList, string _searchRootPath, string _searchFilter )
        {
            if ( _assetPathList == null || AssetDatabase.IsValidFolder( _searchRootPath ) == false )
            {
                return;
            }

            string[] searchRootPathArray = new string[]
            {
                _searchRootPath
            };
            string[] guidArray = AssetDatabase.FindAssets( _searchFilter, searchRootPathArray );

            for ( int index = 0; index < guidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( guidArray[ index ] );

                if ( string.IsNullOrWhiteSpace( assetPath ) || assetPath.EndsWith( ".meta", StringComparison.OrdinalIgnoreCase ) )
                {
                    continue;
                }

                _assetPathList.Add( assetPath );
            }
        }

        ///<summary>
        /// ID 세트 검증
        ///</summary>
        private void ValidateIdSet<TAsset>( List<TAsset> _assetList, string _category, string _idName, Func<TAsset, string> _idGetter, string _searchRootPath ) where TAsset : UnityEngine.Object
        {
            if ( _assetList == null || _assetList.Count == 0 )
            {
                AddResult( eValidationSeverity.WARNING, _category, $"No {_category} assets", $"{_searchRootPath} 경로에서 {_category} 에셋을 찾지 못했습니다.", _searchRootPath );
                return;
            }

            Dictionary<string, string> assetPathById = new Dictionary<string, string>();

            for ( int index = 0; index < _assetList.Count; index++ )
            {
                TAsset asset = _assetList[ index ];

                if ( asset == null )
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath( asset );
                string id = _idGetter( asset );

                if ( string.IsNullOrWhiteSpace( id ) )
                {
                    AddResult( eValidationSeverity.ERROR, _category, $"Empty {_idName}", $"{asset.name}의 {_idName} 값이 비어 있습니다.", assetPath );
                    continue;
                }

                if ( assetPathById.ContainsKey( id ) )
                {
                    AddResult( eValidationSeverity.ERROR, _category, $"Duplicate {_idName}", $"{_idName}가 중복됩니다. Id: {id}, First: {assetPathById[ id ]}", assetPath );
                    continue;
                }

                assetPathById.Add( id, assetPath );
            }
        }

        ///<summary>
        /// 에셋 목록 로드
        ///</summary>
        private List<TAsset> LoadAssets<TAsset>( string _searchRootPath ) where TAsset : UnityEngine.Object
        {
            List<TAsset> assetList = new List<TAsset>();

            if ( AssetDatabase.IsValidFolder( _searchRootPath ) == false )
            {
                return assetList;
            }

            string[] searchRootPathArray = new string[]
            {
                _searchRootPath
            };
            string filter = $"t:{typeof( TAsset ).Name}";
            string[] guidArray = AssetDatabase.FindAssets( filter, searchRootPathArray );
            Array.Sort( guidArray, StringComparer.Ordinal );

            for ( int index = 0; index < guidArray.Length; index++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( guidArray[ index ] );
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>( assetPath );

                if ( asset == null )
                {
                    continue;
                }

                assetList.Add( asset );
            }

            return assetList;
        }

        ///<summary>
        /// ID 집합 생성
        ///</summary>
        private HashSet<string> BuildIdSet<TAsset>( List<TAsset> _assetList, Func<TAsset, string> _idGetter )
        {
            HashSet<string> idSet = new HashSet<string>();

            if ( _assetList == null )
            {
                return idSet;
            }

            for ( int index = 0; index < _assetList.Count; index++ )
            {
                TAsset asset = _assetList[ index ];

                if ( asset == null )
                {
                    continue;
                }

                string id = _idGetter( asset );

                if ( string.IsNullOrWhiteSpace( id ) )
                {
                    continue;
                }

                idSet.Add( id );
            }

            return idSet;
        }

        ///<summary>
        /// 필터 적용 검증 결과 목록 반환
        ///</summary>
        private List<CValidationResult> GetFilteredResultList()
        {
            List<CValidationResult> filteredResultList = new List<CValidationResult>();

            for ( int index = 0; index < validationResultList.Count; index++ )
            {
                CValidationResult validationResult = validationResultList[ index ];

                if ( validationResult == null || IsMatchedSeverityFilter( validationResult ) == false || IsMatchedSearchFilter( validationResult ) == false )
                {
                    continue;
                }

                filteredResultList.Add( validationResult );
            }

            return filteredResultList;
        }

        ///<summary>
        /// 심각도 필터 일치 여부 반환
        ///</summary>
        private bool IsMatchedSeverityFilter( CValidationResult _validationResult )
        {
            if ( selectedSeverityFilterIndex <= 0 )
            {
                return true;
            }

            string severityText = SeverityFilterOptionArray[ selectedSeverityFilterIndex ];
            bool result = string.Equals( _validationResult.severity.ToString(), severityText, StringComparison.Ordinal );
            return result;
        }

        ///<summary>
        /// 검색 필터 일치 여부 반환
        ///</summary>
        private bool IsMatchedSearchFilter( CValidationResult _validationResult )
        {
            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string normalizedSearchText = searchText.Trim();
            bool result = ContainsIgnoreCase( _validationResult.category, normalizedSearchText )
                || ContainsIgnoreCase( _validationResult.title, normalizedSearchText )
                || ContainsIgnoreCase( _validationResult.message, normalizedSearchText )
                || ContainsIgnoreCase( _validationResult.assetPath, normalizedSearchText );
            return result;
        }

        ///<summary>
        /// 문자열 포함 여부 반환
        ///</summary>
        private bool ContainsIgnoreCase( string _sourceText, string _searchText )
        {
            if ( string.IsNullOrWhiteSpace( _sourceText ) || string.IsNullOrWhiteSpace( _searchText ) )
            {
                return false;
            }

            bool result = _sourceText.IndexOf( _searchText, StringComparison.OrdinalIgnoreCase ) >= 0;
            return result;
        }

        ///<summary>
        /// 검증 결과 추가
        ///</summary>
        private void AddResult( eValidationSeverity _severity, string _category, string _title, string _message, string _assetPath )
        {
            CValidationResult validationResult = new CValidationResult( _severity, _category, _title, _message, _assetPath );
            validationResultList.Add( validationResult );
        }

        ///<summary>
        /// 검증 결과 정렬
        ///</summary>
        private void SortResults()
        {
            validationResultList.Sort( CompareValidationResult );
        }

        ///<summary>
        /// 검증 결과 정렬 비교
        ///</summary>
        private int CompareValidationResult( CValidationResult _left, CValidationResult _right )
        {
            if ( _left == null && _right == null )
            {
                return 0;
            }

            if ( _left == null )
            {
                return 1;
            }

            if ( _right == null )
            {
                return -1;
            }

            int severityCompare = _left.severity.CompareTo( _right.severity );

            if ( severityCompare != 0 )
            {
                return severityCompare;
            }

            int categoryCompare = string.Compare( _left.category, _right.category, StringComparison.Ordinal );

            if ( categoryCompare != 0 )
            {
                return categoryCompare;
            }

            int assetPathCompare = string.Compare( _left.assetPath, _right.assetPath, StringComparison.Ordinal );
            return assetPathCompare;
        }

        ///<summary>
        /// 검증 요약 갱신
        ///</summary>
        private void UpdateSummary()
        {
            int errorCount = CountSeverity( eValidationSeverity.ERROR );
            int warningCount = CountSeverity( eValidationSeverity.WARNING );
            int infoCount = CountSeverity( eValidationSeverity.INFO );
            lastValidationSummaryText = $"Validation Complete - Error: {errorCount}, Warning: {warningCount}, Info: {infoCount}";
            lastValidationMessageType = errorCount > 0 ? MessageType.Error : ( warningCount > 0 ? MessageType.Warning : MessageType.Info );

            if ( validationResultList.Count == 0 )
            {
                AddResult( eValidationSeverity.INFO, "Validation", "No issues found", "검증 결과 발견된 문제가 없습니다.", string.Empty );
                lastValidationSummaryText = "Validation Complete - No issues found.";
            }
        }

        ///<summary>
        /// 심각도 개수 반환
        ///</summary>
        private int CountSeverity( eValidationSeverity _severity )
        {
            int count = 0;

            for ( int index = 0; index < validationResultList.Count; index++ )
            {
                CValidationResult validationResult = validationResultList[ index ];

                if ( validationResult != null && validationResult.severity == _severity )
                {
                    count++;
                }
            }

            return count;
        }

        ///<summary>
        /// 심각도 색상 반환
        ///</summary>
        private Color ResolveSeverityColor( eValidationSeverity _severity )
        {
            Color result = Color.white;

            switch ( _severity )
            {
                case eValidationSeverity.ERROR:
                    result = new Color( 1.0f, 0.35f, 0.35f );
                    break;
                case eValidationSeverity.WARNING:
                    result = new Color( 1.0f, 0.76f, 0.25f );
                    break;
                case eValidationSeverity.INFO:
                    result = new Color( 0.55f, 0.8f, 1.0f );
                    break;
            }

            return result;
        }

        ///<summary>
        /// 에셋 Ping 처리
        ///</summary>
        private void PingAsset( string _assetPath )
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>( _assetPath );

            if ( asset == null )
            {
                return;
            }

            EditorGUIUtility.PingObject( asset );
            Selection.activeObject = asset;
        }

        ///<summary>
        /// Resources 기준 Addressables 키 구성
        ///</summary>
        private string BuildAddressableKey( string _assetPath )
        {
            if ( string.IsNullOrWhiteSpace( _assetPath ) )
            {
                return string.Empty;
            }

            string result = CTinyHeroDataValidationRules.BuildAddressableKey( _assetPath );
            return result;
        }

        ///<summary>
        /// 아이템 ID 반환
        ///</summary>
        private string GetItemId( CItemDefinition _itemDefinition )
        {
            string result = _itemDefinition != null ? _itemDefinition.GetItemId() : string.Empty;
            return result;
        }

        ///<summary>
        /// 퀘스트 ID 반환
        ///</summary>
        private string GetQuestId( CQuestDefinition _questDefinition )
        {
            string result = _questDefinition != null ? _questDefinition.GetQuestId() : string.Empty;
            return result;
        }

        ///<summary>
        /// 상점 ID 반환
        ///</summary>
        private string GetShopId( CShopDefinition _shopDefinition )
        {
            string result = _shopDefinition != null ? _shopDefinition.GetShopId() : string.Empty;
            return result;
        }

        ///<summary>
        /// 스킬 ID 반환
        ///</summary>
        private string GetSkillId( CSkillDefinition _skillDefinition )
        {
            string result = _skillDefinition != null ? _skillDefinition.GetSkillId() : string.Empty;
            return result;
        }
    }
}
