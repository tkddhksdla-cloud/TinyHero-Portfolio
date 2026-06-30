using System;
using System.Collections.Generic;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Quest;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 게임 저장 루트 데이터
    ///</summary>
    [Serializable]
    public sealed class CGameSaveData
    {
        public int saveVersion = 1;
        public string mapId = string.Empty;
        public Vector3 playerWorldPosition = Vector3.zero;
        public CPlayerStatSnapshotData playerStatSnapshotData = new CPlayerStatSnapshotData();
        public CPlayerInventorySnapshotData playerInventorySnapshotData = new CPlayerInventorySnapshotData();
        public CPlayerEquipmentSnapshotData playerEquipmentSnapshotData = new CPlayerEquipmentSnapshotData();
        public CQuestRuntimeSnapshotData questRuntimeSnapshotData = new CQuestRuntimeSnapshotData();
        public CSkillSnapshotData skillSnapshotData = new CSkillSnapshotData();
    }

    ///<summary>
    /// 플레이어 스탯 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerStatSnapshotData
    {
        public int currentLevel = 1;
        public float currentExp;
        public float currentHp;
        public float currentMp;
        public int unspentStatPoint;
        public CPlayerStatRuntimeData levelStatBonus = new CPlayerStatRuntimeData();
    }

    ///<summary>
    /// 플레이어 장비 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CPlayerEquipmentSnapshotData
    {
        public List<CPlayerEquippedItemSnapshotData> equippedItemSnapshotList = new List<CPlayerEquippedItemSnapshotData>();
        public List<CPlayerEquipmentSlotEntryData> equipmentSlotEntryList = new List<CPlayerEquipmentSlotEntryData>();
    }

    ///<summary>
    /// 스킬 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CSkillSnapshotData
    {
        public int currentSkillPoint;
        public int lastGrantedPlayerLevel = 1;
        public List<CSkillRuntimeSnapshotEntryData> skillRuntimeEntryList = new List<CSkillRuntimeSnapshotEntryData>();
    }

    ///<summary>
    /// 스킬 개별 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CSkillRuntimeSnapshotEntryData
    {
        public string skillId = string.Empty;
        public bool isUnlocked;
        public int skillLevel;
        public int assignedQuickSlotIndex = -1;
    }
}
