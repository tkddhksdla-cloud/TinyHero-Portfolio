using System.Collections.Generic;
using NUnit.Framework;
using TinyHero.Core.Data;
using TinyHero.Tools;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 데이터 검증 공용 규칙 EditMode 검증
    ///</summary>
    public sealed class DataValidationRulesTests
    {
        ///<summary>
        /// Resources 경로 기반 Addressables 키 변환 검증
        ///</summary>
        [Test]
        public void BuildAddressableKey_ConvertsResourcesAssetPath()
        {
            string addressableKey = CTinyHeroDataValidationRules.BuildAddressableKey( "Assets/Resources/Prefabs/UI/Popup/PopupCommonNotice.prefab" );

            Assert.AreEqual( "Prefabs/UI/Popup/PopupCommonNotice", addressableKey );
        }

        ///<summary>
        /// Resources 외부 경로 Addressables 키 제외 검증
        ///</summary>
        [Test]
        public void BuildAddressableKey_ReturnsEmptyForNonResourcesPath()
        {
            string addressableKey = CTinyHeroDataValidationRules.BuildAddressableKey( "Assets/Data/Skill/Definitions/Skill_Test.asset" );

            Assert.AreEqual( string.Empty, addressableKey );
        }

        ///<summary>
        /// Resources 프리팹 루트 자동 동기화 규칙 검증
        ///</summary>
        [Test]
        public void CreateAddressableSyncRuleList_IncludesPrefabRoot()
        {
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();
            bool hasPrefabRootRule = HasSyncRule( syncRuleList, "Assets/Resources/Prefabs", "t:Prefab" );

            Assert.IsTrue( hasPrefabRootRule );
        }

        ///<summary>
        /// Resources SpriteAtlas 원격 자동 동기화 규칙 검증
        /// </summary>
        [Test]
        public void CreateAddressableSyncRuleList_IncludesRemoteSpriteAtlasRoot()
        {
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();
            string atlasGroupName = FindSyncRuleGroupName( syncRuleList, "Assets/Resources/Atlas", "t:SpriteAtlas" );

            Assert.AreEqual( CTinyHeroDataValidationRules.RemoteAddressableGroupName, atlasGroupName );
        }

        ///<summary>
        /// 독립 로딩 데이터 자동 동기화 규칙 검증
        ///</summary>
        [Test]
        public void CreateAddressableSyncRuleList_IncludesRuntimeDataRoots()
        {
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();

            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Item/Definitions", "t:CItemDefinition", CTinyHeroDataValidationRules.ItemDataAddressableLabel ) );
            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Quest/Definitions", "t:CQuestDefinition", CTinyHeroDataValidationRules.QuestDataAddressableLabel ) );
            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Shop/Definitions", "t:CShopDefinition", CTinyHeroDataValidationRules.ShopDataAddressableLabel ) );
            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Player", "t:CPlayerDefaultStatTableData", CTinyHeroDataValidationRules.PlayerDataAddressableLabel ) );
            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Monster", "t:CMonsterStatTableData", CTinyHeroDataValidationRules.MonsterDataAddressableLabel ) );
            Assert.IsTrue( HasSyncRuleWithLabel( syncRuleList, "Assets/Resources/Data/Text", "t:CTextTableData", CTinyHeroDataValidationRules.TextDataAddressableLabel ) );
        }

        ///<summary>
        /// 직렬화 종속 스킬 데이터 자동 동기화 제외 검증
        ///</summary>
        [Test]
        public void CreateAddressableSyncRuleList_ExcludesSerializedSkillDependencies()
        {
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();
            bool hasSkillDefinitionRule = HasSyncRule( syncRuleList, "Assets/Resources/Data/Skill/Definitions", "t:CSkillDefinition" );
            bool hasSkillIconRule = HasSyncRule( syncRuleList, "Assets/Resources/Data/Skill/Icons", "t:Texture2D" );

            Assert.IsFalse( hasSkillDefinitionRule );
            Assert.IsFalse( hasSkillIconRule );
        }

        ///<summary>
        /// 런타임 리소스 배포 그룹 분리 검증
        ///</summary>
        [Test]
        public void CreateAddressableSyncRuleList_AssignsLocalAndRemoteGroups()
        {
            List<CTinyHeroDataValidationRules.CAddressableSyncRule> syncRuleList = CTinyHeroDataValidationRules.CreateAddressableSyncRuleList();
            string prefabGroupName = FindSyncRuleGroupName( syncRuleList, "Assets/Resources/Prefabs", "t:Prefab" );
            string itemDataGroupName = FindSyncRuleGroupName( syncRuleList, "Assets/Resources/Data/Item/Definitions", "t:CItemDefinition" );

            Assert.AreEqual( CTinyHeroDataValidationRules.AddressableGroupName, prefabGroupName );
            Assert.AreEqual( CTinyHeroDataValidationRules.RemoteAddressableGroupName, itemDataGroupName );
        }

        ///<summary>
        /// Addressables 동기화 대상 경로 판정 검증
        ///</summary>
        [Test]
        public void IsAddressableSyncTargetAssetPath_FiltersNonResourcesPaths()
        {
            bool isResourcesPath = CTinyHeroDataValidationRules.IsAddressableSyncTargetAssetPath( "Assets/Resources/Prefabs/UI/Popup/PopupShop.prefab" );
            bool isNonResourcesPath = CTinyHeroDataValidationRules.IsAddressableSyncTargetAssetPath( "Assets/Prefabs/UI/Popup/PopupShop.prefab" );
            bool isMetaPath = CTinyHeroDataValidationRules.IsAddressableSyncTargetAssetPath( "Assets/Resources/Prefabs/UI/Popup/PopupShop.prefab.meta" );

            Assert.IsTrue( isResourcesPath );
            Assert.IsFalse( isNonResourcesPath );
            Assert.IsFalse( isMetaPath );
        }

        ///<summary>
        /// 빈 ID와 중복 ID 검증 결과 생성 검증
        ///</summary>
        [Test]
        public void ValidateIdSet_ReturnsEmptyAndDuplicateIdResults()
        {
            CItemDefinition emptyItemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
            emptyItemDefinition.name = "EmptyItem";
            emptyItemDefinition.Configure( string.Empty, "Empty Item", eItemType.MATERIAL, string.Empty, null, true, 99L );

            CItemDefinition firstItemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
            firstItemDefinition.name = "FirstItem";
            firstItemDefinition.Configure( "ITEM_DUPLICATE", "First Item", eItemType.MATERIAL, string.Empty, null, true, 99L );

            CItemDefinition secondItemDefinition = ScriptableObject.CreateInstance<CItemDefinition>();
            secondItemDefinition.name = "SecondItem";
            secondItemDefinition.Configure( "ITEM_DUPLICATE", "Second Item", eItemType.MATERIAL, string.Empty, null, true, 99L );

            List<CItemDefinition> itemDefinitionList = new List<CItemDefinition>
            {
                emptyItemDefinition,
                firstItemDefinition,
                secondItemDefinition
            };

            List<CTinyHeroDataValidationResult> resultList = CTinyHeroDataValidationRules.ValidateIdSet(
                itemDefinitionList,
                "Item",
                "Item Id",
                GetItemId,
                "Assets/Resources/Data/Item/Definitions",
                GetAssetName
            );

            Object.DestroyImmediate( emptyItemDefinition );
            Object.DestroyImmediate( firstItemDefinition );
            Object.DestroyImmediate( secondItemDefinition );

            Assert.AreEqual( 2, resultList.Count );
            Assert.AreEqual( eTinyHeroDataValidationSeverity.ERROR, resultList[ 0 ].severity );
            Assert.AreEqual( "Empty Item Id", resultList[ 0 ].title );
            Assert.AreEqual( eTinyHeroDataValidationSeverity.ERROR, resultList[ 1 ].severity );
            Assert.AreEqual( "Duplicate Item Id", resultList[ 1 ].title );
        }

        ///<summary>
        /// 검증 결과 빌드 차단 정책 검증
        ///</summary>
        [Test]
        public void HasBlockingIssue_RespectsWarningPolicy()
        {
            List<CTinyHeroDataValidationResult> warningOnlyResultList = new List<CTinyHeroDataValidationResult>
            {
                new CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity.WARNING, "Skill", "Missing active effect", "warning", "Skill.asset" )
            };
            List<CTinyHeroDataValidationResult> errorResultList = new List<CTinyHeroDataValidationResult>
            {
                new CTinyHeroDataValidationResult( eTinyHeroDataValidationSeverity.ERROR, "Addressables", "Missing entry", "error", "Popup.prefab" )
            };

            Assert.IsFalse( CTinyHeroDataValidationRules.HasBlockingIssue( warningOnlyResultList, false ) );
            Assert.IsTrue( CTinyHeroDataValidationRules.HasBlockingIssue( warningOnlyResultList, true ) );
            Assert.IsTrue( CTinyHeroDataValidationRules.HasBlockingIssue( errorResultList, false ) );
        }

        ///<summary>
        /// 테스트 아이템 ID 반환
        ///</summary>
        private static string GetItemId( CItemDefinition _itemDefinition )
        {
            string result = _itemDefinition != null ? _itemDefinition.GetItemId() : string.Empty;
            return result;
        }

        ///<summary>
        /// 테스트 에셋 이름 반환
        ///</summary>
        private static string GetAssetName( CItemDefinition _itemDefinition )
        {
            string result = _itemDefinition != null ? _itemDefinition.name : string.Empty;
            return result;
        }

        ///<summary>
        /// 테스트용 Addressables 동기화 규칙 존재 여부 반환
        ///</summary>
        private static bool HasSyncRule( List<CTinyHeroDataValidationRules.CAddressableSyncRule> _syncRuleList, string _searchRootPath, string _searchFilter )
        {
            if ( _syncRuleList == null )
            {
                return false;
            }

            for ( int index = 0; index < _syncRuleList.Count; index++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = _syncRuleList[ index ];

                if ( syncRule == null )
                {
                    continue;
                }

                if ( syncRule.searchRootPath == _searchRootPath && syncRule.searchFilter == _searchFilter )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 테스트용 Addressables 동기화 규칙과 라벨 존재 여부 반환
        ///</summary>
        private static bool HasSyncRuleWithLabel( List<CTinyHeroDataValidationRules.CAddressableSyncRule> _syncRuleList, string _searchRootPath, string _searchFilter, string _label )
        {
            if ( _syncRuleList == null )
            {
                return false;
            }

            for ( int ruleIndex = 0; ruleIndex < _syncRuleList.Count; ruleIndex++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = _syncRuleList[ ruleIndex ];

                if ( syncRule == null || syncRule.searchRootPath != _searchRootPath || syncRule.searchFilter != _searchFilter || syncRule.labelArray == null )
                {
                    continue;
                }

                for ( int labelIndex = 0; labelIndex < syncRule.labelArray.Length; labelIndex++ )
                {
                    string label = syncRule.labelArray[ labelIndex ];

                    if ( label == _label )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///<summary>
        /// 테스트용 Addressables 동기화 규칙 그룹명 반환
        ///</summary>
        private static string FindSyncRuleGroupName( List<CTinyHeroDataValidationRules.CAddressableSyncRule> _syncRuleList, string _searchRootPath, string _searchFilter )
        {
            if ( _syncRuleList == null )
            {
                return string.Empty;
            }

            for ( int index = 0; index < _syncRuleList.Count; index++ )
            {
                CTinyHeroDataValidationRules.CAddressableSyncRule syncRule = _syncRuleList[ index ];

                if ( syncRule == null || syncRule.searchRootPath != _searchRootPath || syncRule.searchFilter != _searchFilter )
                {
                    continue;
                }

                string result = syncRule.targetGroupName;
                return result;
            }

            return string.Empty;
        }
    }
}
