using System.Collections.Generic;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 분신 재생용 행동 기록기
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class CPlayerCloneRecorder : MonoBehaviour
    {
        public struct CFrameRecord
        {
            public float time;
            public Vector3 worldPosition;
            public Vector3 localScale;
            public string animationStateName;
            public float animationNormalizedTime;
            public float animatorSpeed;
        }

        public struct CAttackRecord
        {
            public float time;
            public float attackStatValue;
            public float skillAttackPowerMultiplier;
        }

        public struct CSkillRecord
        {
            public float time;
            public string skillId;
            public float attackStatValue;
            public float skillAttackPowerMultiplier;
        }

        private const float DefaultHistoryDurationSeconds = 12.0f;

        [SerializeField] private float historyDurationSeconds = DefaultHistoryDurationSeconds;
        [SerializeField] private PlayerController targetPlayerController;
        [SerializeField] private CPlayerStatManager targetStatManager;
        [SerializeField] private CSkillManager targetSkillManager;

        private readonly List<CFrameRecord> frameRecordList = new List<CFrameRecord>();
        private readonly List<CAttackRecord> attackRecordList = new List<CAttackRecord>();
        private readonly List<CSkillRecord> skillRecordList = new List<CSkillRecord>();

        ///<summary>
        /// 기록기 초기화 처리
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 기록 이벤트 구독 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
        }

        ///<summary>
        /// 기록 이벤트 구독 해제 처리
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        ///<summary>
        /// 분신 재생 프레임 기록 처리
        ///</summary>
        private void Update()
        {
            ResolveReferences();
            RecordCurrentFrame();
            TrimExpiredRecords();
        }

        ///<summary>
        /// 지정 시각 기준 프레임 기록 조회
        ///</summary>
        public bool TryGetFrameAtTime( float _time, out CFrameRecord _frameRecord )
        {
            _frameRecord = default;

            if ( frameRecordList.Count <= 0 )
            {
                return false;
            }

            for ( int index = frameRecordList.Count - 1; index >= 0; index-- )
            {
                CFrameRecord frameRecord = frameRecordList[ index ];

                if ( frameRecord.time > _time )
                {
                    continue;
                }

                _frameRecord = frameRecord;
                return true;
            }

            _frameRecord = frameRecordList[ 0 ];
            return true;
        }

        ///<summary>
        /// 지정 구간 공격 기록 수집 처리
        ///</summary>
        public void CollectAttackRecords( float _fromTimeExclusive, float _toTimeInclusive, List<CAttackRecord> _resultList )
        {
            if ( _resultList == null )
            {
                return;
            }

            _resultList.Clear();

            for ( int index = 0; index < attackRecordList.Count; index++ )
            {
                CAttackRecord attackRecord = attackRecordList[ index ];

                if ( attackRecord.time <= _fromTimeExclusive || attackRecord.time > _toTimeInclusive )
                {
                    continue;
                }

                _resultList.Add( attackRecord );
            }
        }

        ///<summary>
        /// 지정 구간 스킬 기록 수집 처리
        ///</summary>
        public void CollectSkillRecords( float _fromTimeExclusive, float _toTimeInclusive, List<CSkillRecord> _resultList )
        {
            if ( _resultList == null )
            {
                return;
            }

            _resultList.Clear();

            for ( int index = 0; index < skillRecordList.Count; index++ )
            {
                CSkillRecord skillRecord = skillRecordList[ index ];

                if ( skillRecord.time <= _fromTimeExclusive || skillRecord.time > _toTimeInclusive )
                {
                    continue;
                }

                _resultList.Add( skillRecord );
            }
        }

        ///<summary>
        /// 참조 컴포넌트 결정 처리
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetPlayerController == null )
            {
                PlayerController resolvedPlayerController = GetComponent<PlayerController>();
                targetPlayerController = resolvedPlayerController;
            }

            if ( targetStatManager == null )
            {
                CPlayerStatManager resolvedStatManager = targetPlayerController != null ? targetPlayerController.GetPlayerStatManager() : null;
                targetStatManager = resolvedStatManager;
            }

            if ( targetSkillManager == null )
            {
                CSkillManager resolvedSkillManager = targetPlayerController != null ? targetPlayerController.GetSkillManager() : null;
                targetSkillManager = resolvedSkillManager;
            }
        }

        ///<summary>
        /// 기록 이벤트 구독 처리
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetPlayerController != null )
            {
                targetPlayerController.OnAttackHitTriggered -= HandleAttackHitTriggered;
                targetPlayerController.OnAttackHitTriggered += HandleAttackHitTriggered;
            }

            if ( targetSkillManager != null )
            {
                targetSkillManager.OnSkillExecuted -= HandleSkillExecuted;
                targetSkillManager.OnSkillExecuted += HandleSkillExecuted;
            }
        }

        ///<summary>
        /// 기록 이벤트 구독 해제 처리
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetPlayerController != null )
            {
                targetPlayerController.OnAttackHitTriggered -= HandleAttackHitTriggered;
            }

            if ( targetSkillManager != null )
            {
                targetSkillManager.OnSkillExecuted -= HandleSkillExecuted;
            }
        }

        ///<summary>
        /// 현재 프레임 기록 추가 처리
        ///</summary>
        private void RecordCurrentFrame()
        {
            if ( targetPlayerController == null )
            {
                return;
            }

            Transform playerTransform = targetPlayerController.transform;
            CFrameRecord frameRecord = new CFrameRecord();
            frameRecord.time = Time.time;
            frameRecord.worldPosition = playerTransform.position;
            frameRecord.localScale = playerTransform.localScale;
            frameRecord.animationStateName = targetPlayerController.GetCurrentAnimationStateName();
            frameRecord.animationNormalizedTime = ResolveAnimationNormalizedTime();
            frameRecord.animatorSpeed = targetPlayerController.GetCurrentAnimatorSpeed();
            frameRecordList.Add( frameRecord );
        }

        ///<summary>
        /// 애니메이션 정규화 시간 결정 처리
        ///</summary>
        private float ResolveAnimationNormalizedTime()
        {
            if ( targetPlayerController == null )
            {
                return 0.0f;
            }

            Animator targetAnimator = targetPlayerController.GetTargetAnimator();

            if ( targetAnimator == null )
            {
                return 0.0f;
            }

            AnimatorStateInfo animatorStateInfo = targetAnimator.GetCurrentAnimatorStateInfo( 0 );
            float normalizedTime = animatorStateInfo.normalizedTime;
            return normalizedTime;
        }

        ///<summary>
        /// 공격 기록 추가 처리
        ///</summary>
        private void HandleAttackHitTriggered()
        {
            CAttackRecord attackRecord = new CAttackRecord();
            attackRecord.time = Time.time;
            attackRecord.attackStatValue = ResolveCurrentAttackStatValue();
            attackRecord.skillAttackPowerMultiplier = ResolveCurrentSkillAttackPowerMultiplier();
            attackRecordList.Add( attackRecord );
        }

        ///<summary>
        /// 스킬 기록 추가 처리
        ///</summary>
        private void HandleSkillExecuted( CSkillDefinition _skillDefinition )
        {
            if ( _skillDefinition == null )
            {
                return;
            }

            CSkillRecord skillRecord = new CSkillRecord();
            skillRecord.time = Time.time;
            skillRecord.skillId = _skillDefinition.GetSkillId();
            skillRecord.attackStatValue = ResolveCurrentAttackStatValue();
            skillRecord.skillAttackPowerMultiplier = ResolveCurrentSkillAttackPowerMultiplier();
            skillRecordList.Add( skillRecord );
        }

        ///<summary>
        /// 현재 공격력 스냅샷 결정 처리
        ///</summary>
        private float ResolveCurrentAttackStatValue()
        {
            if ( targetStatManager == null )
            {
                return 0.0f;
            }

            float result = targetStatManager.GetFinalStatValue( ePlayerStatType.ATK );
            return result;
        }

        ///<summary>
        /// 현재 스킬 공격 배수 스냅샷 결정 처리
        ///</summary>
        private float ResolveCurrentSkillAttackPowerMultiplier()
        {
            if ( targetPlayerController == null )
            {
                return 1.0f;
            }

            float result = targetPlayerController.GetSkillAttackPowerMultiplier();
            return result;
        }

        ///<summary>
        /// 만료 기록 정리 처리
        ///</summary>
        private void TrimExpiredRecords()
        {
            float minimumKeepTime = Time.time - Mathf.Max( 1.0f, historyDurationSeconds );
            TrimFrameRecords( minimumKeepTime );
            TrimAttackRecords( minimumKeepTime );
            TrimSkillRecords( minimumKeepTime );
        }

        ///<summary>
        /// 만료 프레임 기록 정리 처리
        ///</summary>
        private void TrimFrameRecords( float _minimumKeepTime )
        {
            while ( frameRecordList.Count > 1 )
            {
                CFrameRecord frameRecord = frameRecordList[ 1 ];

                if ( frameRecord.time >= _minimumKeepTime )
                {
                    break;
                }

                frameRecordList.RemoveAt( 0 );
            }
        }

        ///<summary>
        /// 만료 공격 기록 정리 처리
        ///</summary>
        private void TrimAttackRecords( float _minimumKeepTime )
        {
            while ( attackRecordList.Count > 0 )
            {
                CAttackRecord attackRecord = attackRecordList[ 0 ];

                if ( attackRecord.time >= _minimumKeepTime )
                {
                    break;
                }

                attackRecordList.RemoveAt( 0 );
            }
        }

        ///<summary>
        /// 만료 스킬 기록 정리 처리
        ///</summary>
        private void TrimSkillRecords( float _minimumKeepTime )
        {
            while ( skillRecordList.Count > 0 )
            {
                CSkillRecord skillRecord = skillRecordList[ 0 ];

                if ( skillRecord.time >= _minimumKeepTime )
                {
                    break;
                }

                skillRecordList.RemoveAt( 0 );
            }
        }
    }
}
