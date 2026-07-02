using NUnit.Framework;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 스킬 정의와 런타임 데이터 EditMode 검증
    ///</summary>
    public sealed class SkillDataTests
    {
        ///<summary>
        /// 액티브 스킬 구성 시 기본 수치 보정 검증
        ///</summary>
        [Test]
        public void SkillDefinition_ConfigureActiveSkill_NormalizesBasicValues()
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();

            skillDefinition.ConfigureActiveSkill( "skill_test", "Test Skill", null, -3, 1, -5.0f, -10.0f, "desc", null );

            Assert.AreEqual( "skill_test", skillDefinition.GetSkillId() );
            Assert.AreEqual( "Test Skill", skillDefinition.GetSkillName() );
            Assert.AreEqual( eSkillType.ACTIVE, skillDefinition.GetSkillType() );
            Assert.AreEqual( eActiveSkillType.NONE, skillDefinition.GetActiveSkillType() );
            Assert.AreEqual( 0, skillDefinition.GetQuickSlotIndex() );
            Assert.AreEqual( 0.0f, skillDefinition.GetCooldownSeconds(), 0.0001f );
            Assert.AreEqual( 0.0f, skillDefinition.GetMpCost(), 0.0001f );
            Assert.IsTrue( skillDefinition.IsAssignableToQuickSlot() );

            Object.DestroyImmediate( skillDefinition );
        }

        ///<summary>
        /// 패시브 스킬 구성 시 퀵슬롯 배정 불가 검증
        ///</summary>
        [Test]
        public void SkillDefinition_ConfigurePassiveSkill_DisablesQuickSlotAssignment()
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();

            skillDefinition.ConfigurePassiveSkill( "skill_passive", "Passive Skill", null, "desc", null );

            Assert.AreEqual( "skill_passive", skillDefinition.GetSkillId() );
            Assert.AreEqual( eSkillType.PASSIVE, skillDefinition.GetSkillType() );
            Assert.AreEqual( eActiveSkillType.NONE, skillDefinition.GetActiveSkillType() );
            Assert.AreEqual( -1, skillDefinition.GetQuickSlotIndex() );
            Assert.IsFalse( skillDefinition.IsAssignableToQuickSlot() );
            Assert.AreEqual( 0.0f, skillDefinition.GetCooldownSeconds(), 0.0001f );
            Assert.AreEqual( 0.0f, skillDefinition.GetMpCost(), 0.0001f );

            Object.DestroyImmediate( skillDefinition );
        }

        ///<summary>
        /// 스킬 MP 소모량 레벨 보정 검증
        ///</summary>
        [Test]
        public void SkillDefinition_GetMpCost_AppliesLevelReduction()
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();

            skillDefinition.ConfigureActiveSkill( "skill_mp", "MP Skill", null, 0, 1, 1.0f, 10.0f, "desc", null );
            skillDefinition.ConfigureMpScaling( 2.5f );

            Assert.AreEqual( 10.0f, skillDefinition.GetMpCost( 1 ), 0.0001f );
            Assert.AreEqual( 5.0f, skillDefinition.GetMpCost( 3 ), 0.0001f );
            Assert.AreEqual( 0.0f, skillDefinition.GetMpCost( 99 ), 0.0001f );

            Object.DestroyImmediate( skillDefinition );
        }

        ///<summary>
        /// 시전 애니메이션 이름 결정 검증
        ///</summary>
        [Test]
        public void SkillDefinition_GetResolvedCastAnimationName_UsesDefaultAndCustomNames()
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();

            skillDefinition.ConfigureActiveSkill( "skill_cast", "Cast Skill", null, 0, 1, 1.0f, 1.0f, "desc", null );
            skillDefinition.ConfigureCastSetting( 0.1f, ePlayerSkillCastAnimation.MOVE, string.Empty, 1.0f );

            Assert.AreEqual( "Move", skillDefinition.GetResolvedCastAnimationName() );

            skillDefinition.ConfigureCastSetting( 0.1f, ePlayerSkillCastAnimation.CUSTOM, "SpecialCast", 1.0f );

            Assert.AreEqual( "SpecialCast", skillDefinition.GetResolvedCastAnimationName() );

            Object.DestroyImmediate( skillDefinition );
        }

        ///<summary>
        /// 스킬 런타임 데이터 쿨타임 계산 검증
        ///</summary>
        [Test]
        public void SkillRuntimeData_GetRemainingCooldown_UsesSkillLevelCooldown()
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            CSkillRuntimeData runtimeData = new CSkillRuntimeData();

            skillDefinition.ConfigureActiveSkill( "skill_cooldown", "Cooldown Skill", null, 0, 1, 5.0f, 1.0f, "desc", null );
            runtimeData.SetSkillDefinition( skillDefinition );
            runtimeData.SetSkillLevel( 2 );
            runtimeData.MarkUsed( 10.0f );

            Assert.IsTrue( runtimeData.IsOnCooldown( 12.0f ) );
            Assert.AreEqual( 3.0f, runtimeData.GetRemainingCooldown( 12.0f ), 0.0001f );
            Assert.IsFalse( runtimeData.IsOnCooldown( 15.0f ) );
            Assert.AreEqual( 0.0f, runtimeData.GetRemainingCooldown( 15.0f ), 0.0001f );

            Object.DestroyImmediate( skillDefinition );
        }

        ///<summary>
        /// 스킬 런타임 데이터 상태 설정 검증
        ///</summary>
        [Test]
        public void SkillRuntimeData_Setters_NormalizeAndKeepState()
        {
            CSkillRuntimeData runtimeData = new CSkillRuntimeData();

            runtimeData.SetUnlocked( true );
            runtimeData.SetSkillLevel( -5 );
            runtimeData.SetAssignedQuickSlotIndex( 4 );
            runtimeData.MarkUsed( 20.0f );

            Assert.IsTrue( runtimeData.IsUnlocked() );
            Assert.AreEqual( 0, runtimeData.GetSkillLevel() );
            Assert.AreEqual( 4, runtimeData.GetAssignedQuickSlotIndex() );
            Assert.AreEqual( 20.0f, runtimeData.GetLastUsedTime(), 0.0001f );
        }
    }
}
