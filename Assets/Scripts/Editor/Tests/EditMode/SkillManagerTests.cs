using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TinyHero.Core;
using TinyHero.Player;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 스킬 매니저 EditMode 검증
    ///</summary>
    public sealed class SkillManagerTests
    {
        private readonly List<ScriptableObject> createdScriptableObjectList = new List<ScriptableObject>();
        private GameObject skillManagerObject;
        private CSkillManager skillManager;

        ///<summary>
        /// 테스트용 스킬 매니저 생성
        ///</summary>
        [SetUp]
        public void SetUp()
        {
            skillManagerObject = new GameObject( "SkillManagerTests_CSkillManager" );
            skillManager = skillManagerObject.AddComponent<CSkillManager>();
        }

        ///<summary>
        /// 테스트용 스킬 매니저 정리
        ///</summary>
        [TearDown]
        public void TearDown()
        {
            for ( int index = 0; index < createdScriptableObjectList.Count; index++ )
            {
                ScriptableObject scriptableObject = createdScriptableObjectList[ index ];

                if ( scriptableObject == null )
                {
                    continue;
                }

                Object.DestroyImmediate( scriptableObject );
            }

            createdScriptableObjectList.Clear();

            if ( skillManagerObject != null )
            {
                Object.DestroyImmediate( skillManagerObject );
            }
        }

        ///<summary>
        /// 스킬 정의 주입 시 런타임 데이터 재구성 검증
        ///</summary>
        [Test]
        public void SetSkillDefinitions_RebuildsRuntimeData()
        {
            CSkillDefinition firstSkillDefinition = CreateActiveSkillDefinition( "skill_first", "First Skill" );
            CSkillDefinition secondSkillDefinition = CreateActiveSkillDefinition( "skill_second", "Second Skill" );

            skillManager.SetSkillDefinitions( new List<CSkillDefinition> { firstSkillDefinition, secondSkillDefinition } );

            Assert.AreEqual( 2, skillManager.GetSkillCount() );
            Assert.IsNotNull( skillManager.GetSkillRuntimeData( "skill_first" ) );
            Assert.IsNotNull( skillManager.GetSkillRuntimeData( "skill_second" ) );
            Assert.IsNull( skillManager.GetSkillRuntimeData( "skill_missing" ) );
        }

        ///<summary>
        /// 스킬 학습 시 포인트 차감과 해금 상태 검증
        ///</summary>
        [Test]
        public void TryLearnSkill_DeductsSkillPointAndUnlocksSkill()
        {
            CSkillDefinition skillDefinition = CreateActiveSkillDefinition( "skill_learn", "Learn Skill" );
            skillManager.SetSkillDefinitions( new List<CSkillDefinition> { skillDefinition } );
            skillManager.LoadSnapshotData( CreateSnapshot( 2, 1 ) );

            bool didLearn = skillManager.TryLearnSkill( "skill_learn" );

            Assert.IsTrue( didLearn );
            Assert.IsTrue( skillManager.IsSkillUnlocked( "skill_learn" ) );
            Assert.AreEqual( 1, skillManager.GetSkillLevel( "skill_learn" ) );
            Assert.AreEqual( 1, skillManager.GetCurrentSkillPoint() );
        }

        ///<summary>
        /// 스킬 레벨업 시 포인트 차감과 레벨 증가 검증
        ///</summary>
        [Test]
        public void TryLevelUpSkill_DeductsSkillPointAndIncreasesLevel()
        {
            CSkillDefinition skillDefinition = CreateActiveSkillDefinition( "skill_level", "Level Skill" );
            skillManager.SetSkillDefinitions( new List<CSkillDefinition> { skillDefinition } );
            CSkillSnapshotData snapshotData = CreateSnapshot( 2, 1 );
            snapshotData.skillRuntimeEntryList.Add( CreateSnapshotEntry( "skill_level", true, 1, -1 ) );
            skillManager.LoadSnapshotData( snapshotData );

            bool didLevelUp = skillManager.TryLevelUpSkill( "skill_level" );

            Assert.IsTrue( didLevelUp );
            Assert.AreEqual( 2, skillManager.GetSkillLevel( "skill_level" ) );
            Assert.AreEqual( 1, skillManager.GetCurrentSkillPoint() );
        }

        ///<summary>
        /// 퀵슬롯 배정 시 점유 스킬 교체 검증
        ///</summary>
        [Test]
        public void TryAssignSkillToQuickSlot_SwapsOccupiedSlot()
        {
            CSkillDefinition firstSkillDefinition = CreateActiveSkillDefinition( "skill_slot_first", "Slot First" );
            CSkillDefinition secondSkillDefinition = CreateActiveSkillDefinition( "skill_slot_second", "Slot Second" );
            skillManager.SetSkillDefinitions( new List<CSkillDefinition> { firstSkillDefinition, secondSkillDefinition } );
            CSkillSnapshotData snapshotData = CreateSnapshot( 0, 1 );
            snapshotData.skillRuntimeEntryList.Add( CreateSnapshotEntry( "skill_slot_first", true, 1, 0 ) );
            snapshotData.skillRuntimeEntryList.Add( CreateSnapshotEntry( "skill_slot_second", true, 1, 1 ) );
            skillManager.LoadSnapshotData( snapshotData );

            bool didAssign = skillManager.TryAssignSkillToQuickSlot( "skill_slot_second", 0 );

            Assert.IsTrue( didAssign );
            Assert.AreEqual( 1, skillManager.GetAssignedQuickSlotIndex( "skill_slot_first" ) );
            Assert.AreEqual( 0, skillManager.GetAssignedQuickSlotIndex( "skill_slot_second" ) );
            Assert.AreEqual( secondSkillDefinition, skillManager.GetSkillDefinitionByQuickSlotIndex( 0 ) );
        }

        ///<summary>
        /// 스킬 스냅샷 생성과 복원 값 검증
        ///</summary>
        [Test]
        public void CreateSnapshotData_CapturesRuntimeState()
        {
            CSkillDefinition skillDefinition = CreateActiveSkillDefinition( "skill_snapshot", "Snapshot Skill" );
            skillManager.SetSkillDefinitions( new List<CSkillDefinition> { skillDefinition } );
            CSkillSnapshotData snapshotData = CreateSnapshot( 5, 3 );
            snapshotData.skillRuntimeEntryList.Add( CreateSnapshotEntry( "skill_snapshot", true, 2, 4 ) );
            skillManager.LoadSnapshotData( snapshotData );

            CSkillSnapshotData createdSnapshotData = skillManager.CreateSnapshotData();

            Assert.AreEqual( 5, createdSnapshotData.currentSkillPoint );
            Assert.AreEqual( 3, createdSnapshotData.lastGrantedPlayerLevel );
            Assert.AreEqual( 1, createdSnapshotData.skillRuntimeEntryList.Count );
            Assert.AreEqual( "skill_snapshot", createdSnapshotData.skillRuntimeEntryList[ 0 ].skillId );
            Assert.IsTrue( createdSnapshotData.skillRuntimeEntryList[ 0 ].isUnlocked );
            Assert.AreEqual( 2, createdSnapshotData.skillRuntimeEntryList[ 0 ].skillLevel );
            Assert.AreEqual( 4, createdSnapshotData.skillRuntimeEntryList[ 0 ].assignedQuickSlotIndex );
        }

        ///<summary>
        /// 분리된 스킬 매니저가 플레이어 트랜스폼을 실행 주체로 사용하는지 검증
        ///</summary>
        [Test]
        public void CreateSkillContext_UsesBoundPlayerTransformAsOwner()
        {
            GameObject playerObject = new GameObject( "SkillManagerTests_Player" );

            try
            {
                PlayerController playerController = playerObject.AddComponent<PlayerController>();
                skillManager.BindRuntimeReferences( playerController, null, null );
                CSkillDefinition skillDefinition = CreateActiveSkillDefinition( "skill_owner", "Owner Skill" );
                MethodInfo createSkillContextMethod = typeof( CSkillManager ).GetMethod( "CreateSkillContext", BindingFlags.Instance | BindingFlags.NonPublic );

                Assert.IsNotNull( createSkillContextMethod );

                object[] parameterArray = new object[] { skillDefinition, null };
                CSkillContext skillContext = createSkillContextMethod.Invoke( skillManager, parameterArray ) as CSkillContext;

                Assert.IsNotNull( skillContext );
                Assert.AreEqual( playerController.transform, skillContext.GetOwnerTransform() );
            }
            finally
            {
                Object.DestroyImmediate( playerObject );
            }
        }

        ///<summary>
        /// 테스트용 액티브 스킬 정의 생성
        ///</summary>
        private CSkillDefinition CreateActiveSkillDefinition( string _skillId, string _skillName )
        {
            CSkillDefinition skillDefinition = ScriptableObject.CreateInstance<CSkillDefinition>();
            skillDefinition.ConfigureActiveSkill( _skillId, _skillName, null, 0, 1, 1.0f, 0.0f, string.Empty, null );
            createdScriptableObjectList.Add( skillDefinition );
            return skillDefinition;
        }

        ///<summary>
        /// 테스트용 스킬 스냅샷 생성
        ///</summary>
        private static CSkillSnapshotData CreateSnapshot( int _skillPoint, int _lastGrantedLevel )
        {
            CSkillSnapshotData snapshotData = new CSkillSnapshotData();
            snapshotData.currentSkillPoint = _skillPoint;
            snapshotData.lastGrantedPlayerLevel = _lastGrantedLevel;
            return snapshotData;
        }

        ///<summary>
        /// 테스트용 스킬 스냅샷 엔트리 생성
        ///</summary>
        private static CSkillRuntimeSnapshotEntryData CreateSnapshotEntry( string _skillId, bool _isUnlocked, int _skillLevel, int _assignedQuickSlotIndex )
        {
            CSkillRuntimeSnapshotEntryData snapshotEntryData = new CSkillRuntimeSnapshotEntryData();
            snapshotEntryData.skillId = _skillId;
            snapshotEntryData.isUnlocked = _isUnlocked;
            snapshotEntryData.skillLevel = _skillLevel;
            snapshotEntryData.assignedQuickSlotIndex = _assignedQuickSlotIndex;
            return snapshotEntryData;
        }
    }
}
