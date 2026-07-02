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
    }
}
