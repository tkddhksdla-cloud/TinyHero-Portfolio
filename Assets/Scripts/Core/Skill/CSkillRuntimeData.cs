using System;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 런타임 상태 데이터
    ///</summary>
    [Serializable]
    public sealed class CSkillRuntimeData
    {
        [SerializeField] private CSkillDefinition skillDefinition;
        [SerializeField] private bool isUnlocked;
        [SerializeField] private int skillLevel = 1;
        [SerializeField] private int assignedQuickSlotIndex = -1;
        [SerializeField] private float lastUsedTime = -9999.0f;

        ///<summary>
        /// 스킬 정의 반환
        ///</summary>
        public CSkillDefinition GetSkillDefinition()
        {
            CSkillDefinition result = skillDefinition;
            return result;
        }

        ///<summary>
        /// 해금 상태 반환
        ///</summary>
        public bool IsUnlocked()
        {
            bool result = isUnlocked;
            return result;
        }

        ///<summary>
        /// 스킬 레벨 반환
        ///</summary>
        public int GetSkillLevel()
        {
            int result = skillLevel;
            return result;
        }

        ///<summary>
        /// 배정된 퀵슬롯 인덱스 반환
        ///</summary>
        public int GetAssignedQuickSlotIndex()
        {
            int result = assignedQuickSlotIndex;
            return result;
        }

        ///<summary>
        /// 마지막 사용 시간 반환
        ///</summary>
        public float GetLastUsedTime()
        {
            float result = lastUsedTime;
            return result;
        }

        ///<summary>
        /// 스킬 정의 설정
        ///</summary>
        public void SetSkillDefinition( CSkillDefinition _skillDefinition )
        {
            skillDefinition = _skillDefinition;
        }

        ///<summary>
        /// 해금 상태 설정
        ///</summary>
        public void SetUnlocked( bool _isUnlocked )
        {
            isUnlocked = _isUnlocked;
        }

        ///<summary>
        /// 스킬 레벨 설정
        ///</summary>
        public void SetSkillLevel( int _skillLevel )
        {
            int resolvedSkillLevel = Mathf.Max( 0, _skillLevel );
            skillLevel = resolvedSkillLevel;
        }

        ///<summary>
        /// 배정된 퀵슬롯 인덱스 설정
        ///</summary>
        public void SetAssignedQuickSlotIndex( int _assignedQuickSlotIndex )
        {
            assignedQuickSlotIndex = _assignedQuickSlotIndex;
        }

        ///<summary>
        /// 사용 시간 기록
        ///</summary>
        public void MarkUsed( float _time )
        {
            lastUsedTime = _time;
        }

        ///<summary>
        /// 남은 쿨타임 반환
        ///</summary>
        public float GetRemainingCooldown( float _currentTime )
        {
            if ( skillDefinition == null )
            {
                return 0.0f;
            }

            float cooldownSeconds = skillDefinition.GetCooldownSeconds( skillLevel );
            float elapsedTime = _currentTime - lastUsedTime;
            float remainingCooldown = cooldownSeconds - elapsedTime;
            float result = Mathf.Max( 0.0f, remainingCooldown );
            return result;
        }

        ///<summary>
        /// 쿨타임 진행 상태 반환
        ///</summary>
        public bool IsOnCooldown( float _currentTime )
        {
            float remainingCooldown = GetRemainingCooldown( _currentTime );
            bool result = remainingCooldown > 0.0f;
            return result;
        }
    }
}
