using System;
using TinyHero.Core;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 스탯과 자원 상태 관리
    ///</summary>
    public sealed class CPlayerStatManager : MonoBehaviour
    {
        private const string PlayerDefaultStatTableResourcePath = "Data/Player/PlayerDefaultStatTableData";
        private const string PlayerLevelStatTableResourcePath = "Data/Player/PlayerLevelStatTableData";
        private const float DefaultAttackIntervalSeconds = 0.5f;
        private const float DefaultAttackRatePerSecond = 2.0f;
        private const float DefaultMoveSpeedPercent = 0.0f;

        [Header( "Fallback Base Stats" )]
        [SerializeField] private CPlayerStatDefinition baseStatDefinition = new CPlayerStatDefinition();

        [Header( "Bonus Stats" )]
        [SerializeField] private CPlayerStatRuntimeData equipmentStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private CPlayerStatRuntimeData equipmentPercentStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private CPlayerStatRuntimeData levelStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private CPlayerStatRuntimeData skillStatBonus = new CPlayerStatRuntimeData();
        [SerializeField] private CPlayerModifierRuntimeData equipmentModifierBonus = new CPlayerModifierRuntimeData();

        [Header( "Runtime" )]
        [SerializeField] private bool restoreResourceOnAwake = true;
        [SerializeField] private bool enableAutoRecovery = true;
        [SerializeField] private int unspentStatPoint;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float currentExp;
        [SerializeField] private float maxExp = 100.0f;

        private static CPlayerDefaultStatTableData cachedPlayerDefaultStatTableData;
        private static CPlayerLevelStatTableData cachedPlayerLevelStatTableData;

        private float currentHp;
        private float currentMp;
        private bool isRuntimeInitialized;

        public event Action<CPlayerStatManager> OnStatChanged;
        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnMpChanged;
        public event Action<int> OnStatPointChanged;
        public event Action<int, float, float> OnLevelExpChanged;

        ///<summary>
        /// 초기 자원 상태 구성
        ///</summary>
        private void Awake()
        {
            InitializeRuntimeState();
        }

        ///<summary>
        /// 자동 회복 처리
        ///</summary>
        private void Update()
        {
            TickAutoRecovery();
        }

        ///<summary>
        /// 전체 스탯 변경 이벤트 전달
        ///</summary>
        public void NotifyStatChanged()
        {
            ClampCurrentResourcesToMax();
            RefreshMaxExpByCurrentLevel();

            if ( OnStatChanged != null )
            {
                OnStatChanged( this );
            }

            RaiseHpChanged();
            RaiseMpChanged();
            RaiseStatPointChanged();
            RaiseLevelExpChanged();
        }

        ///<summary>
        /// 기본 스탯 값 반환
        ///</summary>
        public float GetBaseStatValue( ePlayerStatType _statType )
        {
            bool hasTableValue = TryGetTableBaseStatValue( _statType, out float tableValue );

            if ( hasTableValue )
            {
                return tableValue;
            }

            float fallbackValue = baseStatDefinition.GetStatValue( _statType );
            return fallbackValue;
        }

        ///<summary>
        /// 장비 보너스 값 반환
        ///</summary>
        public float GetEquipmentStatValue( ePlayerStatType _statType )
        {
            float result = equipmentStatBonus.GetStatValue( _statType );
            return result;
        }

        ///<summary>
        /// 장비 퍼센트 보너스 값 반환
        ///</summary>
        public float GetEquipmentPercentStatValue( ePlayerStatType _statType )
        {
            float result = equipmentPercentStatBonus.GetStatValue( _statType );
            return result;
        }

        ///<summary>
        /// 레벨 보너스 값 반환
        ///</summary>
        public float GetLevelStatValue( ePlayerStatType _statType )
        {
            float result = levelStatBonus.GetStatValue( _statType );
            return result;
        }

        ///<summary>
        /// 스킬 보너스 스탯 반환
        ///</summary>
        public float GetSkillStatValue( ePlayerStatType _statType )
        {
            float result = skillStatBonus.GetStatValue( _statType );
            return result;
        }

        ///<summary>
        /// 최종 스탯 값 반환
        ///</summary>
        public float GetFinalStatValue( ePlayerStatType _statType )
        {
            float baseValue = GetBaseStatValue( _statType );
            float equipmentValue = GetEquipmentStatValue( _statType );
            float levelValue = GetLevelStatValue( _statType );
            float skillValue = GetSkillStatValue( _statType );
            float summedValue = baseValue + equipmentValue + levelValue + skillValue;
            float equipmentPercentValue = GetEquipmentPercentStatValue( _statType );
            float multipliedValue = summedValue * ( 1.0f + equipmentPercentValue * 0.01f );
            float result = Mathf.Max( 0.0f, multipliedValue );
            return result;
        }

        ///<summary>
        /// 최대 체력 반환
        ///</summary>
        public float GetMaxHp()
        {
            float result = GetFinalStatValue( ePlayerStatType.HP );
            return result;
        }

        ///<summary>
        /// 초당 체력 회복값 반환
        ///</summary>
        public float GetHpRecoveryPerSecond()
        {
            float result = GetFinalStatValue( ePlayerStatType.HR );
            return result;
        }

        ///<summary>
        /// 최대 마나 반환
        ///</summary>
        public float GetMaxMp()
        {
            float result = GetFinalStatValue( ePlayerStatType.MP );
            return result;
        }

        ///<summary>
        /// 초당 마나 회복값 반환
        ///</summary>
        public float GetMpRecoveryPerSecond()
        {
            float result = GetFinalStatValue( ePlayerStatType.MR );
            return result;
        }

        ///<summary>
        /// 현재 체력 반환
        ///</summary>
        public float GetCurrentHp()
        {
            float result = currentHp;
            return result;
        }

        ///<summary>
        /// 현재 마나 반환
        ///</summary>
        public float GetCurrentMp()
        {
            float result = currentMp;
            return result;
        }

        ///<summary>
        /// 남은 스탯 포인트 반환
        ///</summary>
        public int GetUnspentStatPoint()
        {
            int result = unspentStatPoint;
            return result;
        }

        ///<summary>
        /// 현재 레벨 반환
        ///</summary>
        public int GetCurrentLevel()
        {
            int result = currentLevel;
            return result;
        }

        ///<summary>
        /// 현재 경험치 반환
        ///</summary>
        public float GetCurrentExp()
        {
            float result = currentExp;
            return result;
        }

        ///<summary>
        /// 다음 레벨 기준 경험치 반환
        ///</summary>
        public float GetMaxExp()
        {
            float result = maxExp;
            return result;
        }

        ///<summary>
        /// 현재 레벨 구간 경험치 반환
        ///</summary>
        public float GetCurrentLevelExpProgress()
        {
            float currentLevelStartExp = GetCurrentLevelStartExp();
            float currentLevelExpProgress = Mathf.Max( 0.0f, currentExp - currentLevelStartExp );
            return currentLevelExpProgress;
        }

        ///<summary>
        /// 현재 레벨 구간 최대 경험치 반환
        ///</summary>
        public float GetCurrentLevelExpRequirement()
        {
            float currentLevelStartExp = GetCurrentLevelStartExp();
            float currentLevelExpRequirement = Mathf.Max( 0.0f, maxExp - currentLevelStartExp );
            return currentLevelExpRequirement;
        }

        ///<summary>
        /// 경험치 획득 배수 반환
        ///</summary>
        public float GetExpGainMultiplier()
        {
            float expGainPercent = equipmentModifierBonus != null ? equipmentModifierBonus.GetExpGainPercent() : 0.0f;
            float result = Mathf.Max( 0.0f, 1.0f + expGainPercent * 0.01f );
            return result;
        }

        ///<summary>
        /// 골드 획득 배수 반환
        ///</summary>
        public float GetGoldGainMultiplier()
        {
            float goldGainPercent = equipmentModifierBonus != null ? equipmentModifierBonus.GetGoldGainPercent() : 0.0f;
            float result = Mathf.Max( 0.0f, 1.0f + goldGainPercent * 0.01f );
            return result;
        }

        ///<summary>
        /// 장비 최종 공격력 증가율 반환
        ///</summary>
        public float GetEquipmentFinalAttackPercentBonus()
        {
            float result = equipmentModifierBonus != null ? equipmentModifierBonus.GetFinalAttackPercent() * 0.01f : 0.0f;
            return result;
        }

        ///<summary>
        /// 현재 레벨 시작 누적 경험치 반환
        ///</summary>
        public float GetCurrentLevelStartExp()
        {
            float result = ResolveMinimumExpForLevel( currentLevel );
            return result;
        }

        ///<summary>
        /// 지정 레벨 시작 누적 경험치 반환
        ///</summary>
        public float GetLevelStartExp( int _level )
        {
            int resolvedLevel = ResolveAvailableLevel( _level );
            float result = ResolveMinimumExpForLevel( resolvedLevel );
            return result;
        }

        ///<summary>
        /// 지정 레벨 구간 경험치 진행값 반환
        ///</summary>
        public float GetLevelExpProgress( int _level, float _currentExp )
        {
            float levelStartExp = GetLevelStartExp( _level );
            float result = Mathf.Max( 0.0f, _currentExp - levelStartExp );
            return result;
        }

        ///<summary>
        /// 지정 레벨 구간 최대 경험치 반환
        ///</summary>
        public float GetLevelExpRequirement( int _level, float _maxExp )
        {
            float levelStartExp = GetLevelStartExp( _level );
            float result = Mathf.Max( 0.0f, _maxExp - levelStartExp );
            return result;
        }

        ///<summary>
        /// 공격 주기 초 반환
        ///</summary>
        public float GetAttackIntervalSeconds()
        {
            float finalAts = GetFinalStatValue( ePlayerStatType.ATS );
            float resolvedAttackRatePerSecond = finalAts > 0.0f ? finalAts : DefaultAttackRatePerSecond;
            float result = resolvedAttackRatePerSecond > 0.0f ? 1.0f / resolvedAttackRatePerSecond : DefaultAttackIntervalSeconds;
            return result;
        }

        ///<summary>
        /// 초당 공격 횟수 반환
        ///</summary>
        public float GetAttackRatePerSecond()
        {
            float finalAts = GetFinalStatValue( ePlayerStatType.ATS );
            float result = finalAts > 0.0f ? finalAts : DefaultAttackRatePerSecond;
            return result;
        }

        ///<summary>
        /// 공격 애니메이션 속도 배율 반환
        ///</summary>
        public float GetAttackAnimationSpeedMultiplier()
        {
            float baseAttackRatePerSecond = GetBaseStatValue( ePlayerStatType.ATS );

            if ( baseAttackRatePerSecond <= 0.0f )
            {
                baseAttackRatePerSecond = DefaultAttackRatePerSecond;
            }

            float finalAttackRatePerSecond = GetAttackRatePerSecond();
            float result = Mathf.Max( 0.01f, finalAttackRatePerSecond / baseAttackRatePerSecond );
            return result;
        }

        ///<summary>
        /// 이동 속도 배율 반환
        ///</summary>
        public float GetMoveSpeedMultiplier()
        {
            float finalMovePercent = GetFinalStatValue( ePlayerStatType.MOVE );
            float result = Mathf.Max( 0.0f, 1.0f + finalMovePercent * 0.01f );
            return result;
        }

        ///<summary>
        /// 공격 범위 배율 반환
        ///</summary>
        public float GetRangeMultiplier()
        {
            float finalRangePercent = GetFinalStatValue( ePlayerStatType.RANGE );
            float result = Mathf.Max( 0.1f, 1.0f + finalRangePercent * 0.01f );
            return result;
        }

        ///<summary>
        /// 기본 스탯 값 설정
        ///</summary>
        public void SetBaseStatValue( ePlayerStatType _statType, float _value )
        {
            baseStatDefinition.SetStatValue( _statType, _value );
            NotifyStatChanged();
        }

        ///<summary>
        /// 장비 스탯 값 설정
        ///</summary>
        public void SetEquipmentStatValue( ePlayerStatType _statType, float _value )
        {
            equipmentStatBonus.SetStatValue( _statType, _value );
            NotifyStatChanged();
        }

        ///<summary>
        /// 장비 스탯 일괄 반영
        ///</summary>
        public void ApplyEquipmentStatBonus( CPlayerStatRuntimeData _bonusData )
        {
            equipmentStatBonus.CopyFrom( _bonusData );
            NotifyStatChanged();
        }

        ///<summary>
        /// 장비 퍼센트 스탯 일괄 반영
        ///</summary>
        public void ApplyEquipmentPercentStatBonus( CPlayerStatRuntimeData _bonusData )
        {
            equipmentPercentStatBonus.CopyFrom( _bonusData );
            NotifyStatChanged();
        }

        ///<summary>
        /// 장비 스탯 초기화
        ///</summary>
        public void ClearEquipmentStatBonus()
        {
            equipmentStatBonus.Clear();
            equipmentPercentStatBonus.Clear();
            NotifyStatChanged();
        }

        ///<summary>
        /// 장비 특수 보너스 일괄 반영
        ///</summary>
        public void ApplyEquipmentModifierBonus( CPlayerModifierRuntimeData _modifierData )
        {
            equipmentModifierBonus.CopyFrom( _modifierData );
            NotifyStatChanged();
        }

        ///<summary>
        /// 스킬 스탯 보너스 일괄 반영
        ///</summary>
        public void ApplySkillStatBonus( CPlayerStatRuntimeData _bonusData )
        {
            skillStatBonus.CopyFrom( _bonusData );
            NotifyStatChanged();
        }

        ///<summary>
        /// 스킬 스탯 보너스 초기화
        ///</summary>
        public void ClearSkillStatBonus()
        {
            skillStatBonus.Clear();
            NotifyStatChanged();
        }

        ///<summary>
        /// 레벨 보너스 스탯 값 설정
        ///</summary>
        public void SetLevelStatValue( ePlayerStatType _statType, float _value )
        {
            levelStatBonus.SetStatValue( _statType, _value );
            NotifyStatChanged();
        }

        ///<summary>
        /// 레벨 보너스 스탯 누적
        ///</summary>
        public void AddLevelStatValue( ePlayerStatType _statType, float _value )
        {
            levelStatBonus.AddStatValue( _statType, _value );
            NotifyStatChanged();
        }

        ///<summary>
        /// 스탯 포인트 지급
        ///</summary>
        public void AddStatPoint( int _amount )
        {
            int nextStatPoint = unspentStatPoint + _amount;
            unspentStatPoint = Mathf.Max( 0, nextStatPoint );
            RaiseStatPointChanged();
        }

        ///<summary>
        /// 현재 레벨 설정
        ///</summary>
        public void SetCurrentLevel( int _level )
        {
            int previousLevel = currentLevel;
            int resolvedLevel = ResolveAvailableLevel( _level );
            currentLevel = resolvedLevel;

            float minimumExpForLevel = ResolveMinimumExpForLevel( currentLevel );

            if ( currentExp < minimumExpForLevel )
            {
                currentExp = minimumExpForLevel;
            }

            if ( currentLevel > previousLevel )
            {
                currentHp = GetMaxHp();
                currentMp = GetMaxMp();
            }

            NotifyStatChanged();
        }

        ///<summary>
        /// 다음 레벨 기준 경험치 설정
        ///</summary>
        public void SetMaxExp( float _value )
        {
            float nextMaxExp = Mathf.Max( 1.0f, _value );
            maxExp = nextMaxExp;
            currentExp = Mathf.Clamp( currentExp, 0.0f, maxExp );
            RaiseLevelExpChanged();
        }

        ///<summary>
        /// 현재 경험치 설정
        ///</summary>
        public void SetCurrentExp( float _value )
        {
            currentExp = Mathf.Max( 0.0f, _value );
            RefreshProgressionFromExperience();
        }

        ///<summary>
        /// 경험치 증가
        ///</summary>
        public void AddExp( float _amount )
        {
            if ( _amount <= 0.0f )
            {
                return;
            }

            float nextExp = currentExp + _amount;
            currentExp = nextExp;
            RefreshProgressionFromExperience();
        }

        ///<summary>
        /// 스탯 포인트 투자 시도
        ///</summary>
        public bool TryInvestStatPoint( ePlayerStatType _statType, float _valuePerPoint )
        {
            if ( unspentStatPoint <= 0 )
            {
                return false;
            }

            if ( _valuePerPoint <= 0.0f )
            {
                return false;
            }

            levelStatBonus.AddStatValue( _statType, _valuePerPoint );
            unspentStatPoint--;
            NotifyStatChanged();
            return true;
        }

        ///<summary>
        /// 현재 체력 설정
        ///</summary>
        public void SetCurrentHp( float _value )
        {
            float resolvedMaxHp = GetMaxHp();
            float nextHp = Mathf.Clamp( _value, 0.0f, resolvedMaxHp );

            if ( Mathf.Approximately( currentHp, nextHp ) )
            {
                return;
            }

            currentHp = nextHp;
            RaiseHpChanged();
        }

        ///<summary>
        /// 현재 마나 설정
        ///</summary>
        public void SetCurrentMp( float _value )
        {
            float resolvedMaxMp = GetMaxMp();
            float nextMp = Mathf.Clamp( _value, 0.0f, resolvedMaxMp );

            if ( Mathf.Approximately( currentMp, nextMp ) )
            {
                return;
            }

            currentMp = nextMp;
            RaiseMpChanged();
        }

        ///<summary>
        /// 체력 회복
        ///</summary>
        public void RecoverHp( float _amount )
        {
            if ( _amount <= 0.0f )
            {
                return;
            }

            float nextHp = currentHp + _amount;
            SetCurrentHp( nextHp );
        }

        ///<summary>
        /// 마나 회복
        ///</summary>
        public void RecoverMp( float _amount )
        {
            if ( _amount <= 0.0f )
            {
                return;
            }

            float nextMp = currentMp + _amount;
            SetCurrentMp( nextMp );
        }

        ///<summary>
        /// 체력 소모
        ///</summary>
        public void ConsumeHp( float _amount )
        {
            if ( _amount <= 0.0f )
            {
                return;
            }

            float nextHp = currentHp - _amount;
            SetCurrentHp( nextHp );
        }

        ///<summary>
        /// 마나 소모 시도
        ///</summary>
        public bool TryConsumeMp( float _amount )
        {
            if ( _amount <= 0.0f )
            {
                return true;
            }

            if ( currentMp < _amount )
            {
                return false;
            }

            float nextMp = currentMp - _amount;
            SetCurrentMp( nextMp );
            return true;
        }

        ///<summary>
        /// 체력과 마나 최대치 복원
        ///</summary>
        public void RestoreFullResources()
        {
            currentHp = GetMaxHp();
            currentMp = GetMaxMp();
            RaiseHpChanged();
            RaiseMpChanged();
        }

        ///<summary>
        /// 플레이어 스탯 저장 데이터 생성
        ///</summary>
        public CPlayerStatSnapshotData CreateSnapshotData()
        {
            CPlayerStatSnapshotData snapshotData = new CPlayerStatSnapshotData();
            snapshotData.currentLevel = currentLevel;
            snapshotData.currentExp = currentExp;
            snapshotData.currentHp = currentHp;
            snapshotData.currentMp = currentMp;
            snapshotData.unspentStatPoint = unspentStatPoint;
            snapshotData.levelStatBonus.CopyFrom( levelStatBonus );
            return snapshotData;
        }

        ///<summary>
        /// 플레이어 스탯 저장 데이터 로드
        ///</summary>
        public void LoadSnapshotData( CPlayerStatSnapshotData _snapshotData )
        {
            if ( _snapshotData == null )
            {
                return;
            }

            currentLevel = Mathf.Max( 1, _snapshotData.currentLevel );
            currentExp = Mathf.Max( 0.0f, _snapshotData.currentExp );
            unspentStatPoint = Mathf.Max( 0, _snapshotData.unspentStatPoint );
            levelStatBonus.CopyFrom( _snapshotData.levelStatBonus );
            RefreshProgressionFromExperience( false );
            currentHp = Mathf.Max( 0.0f, _snapshotData.currentHp );
            currentMp = Mathf.Max( 0.0f, _snapshotData.currentMp );
            ClampCurrentResourcesToMax();
            NotifyStatChanged();
        }

        ///<summary>
        /// 사망 상태 여부 반환
        ///</summary>
        public bool IsDead()
        {
            bool result = currentHp <= 0.0f;
            return result;
        }

        ///<summary>
        /// 초기 자원 상태 구성
        ///</summary>
        private void InitializeRuntimeState()
        {
            if ( isRuntimeInitialized )
            {
                return;
            }

            isRuntimeInitialized = true;
            RefreshProgressionFromExperience( false );

            if ( restoreResourceOnAwake )
            {
                currentHp = GetMaxHp();
                currentMp = GetMaxMp();
                return;
            }

            ClampCurrentResourcesToMax();
        }

        ///<summary>
        /// 자동 회복 처리
        ///</summary>
        private void TickAutoRecovery()
        {
            if ( enableAutoRecovery == false )
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            if ( deltaTime <= 0.0f )
            {
                return;
            }

            float hpRecoveryAmount = GetHpRecoveryPerSecond() * deltaTime;
            float mpRecoveryAmount = GetMpRecoveryPerSecond() * deltaTime;

            if ( hpRecoveryAmount > 0.0f )
            {
                RecoverHp( hpRecoveryAmount );
            }

            if ( mpRecoveryAmount > 0.0f )
            {
                RecoverMp( mpRecoveryAmount );
            }
        }

        ///<summary>
        /// 경험치 기준 진행도 갱신
        ///</summary>
        private void RefreshProgressionFromExperience()
        {
            RefreshProgressionFromExperience( true );
        }

        ///<summary>
        /// 경험치 기준 진행도 갱신
        ///</summary>
        private void RefreshProgressionFromExperience( bool _notifyStatChanged )
        {
            int previousLevel = currentLevel;
            int resolvedLevel = ResolveLevelFromExperience( currentExp );
            currentLevel = resolvedLevel;

            if ( currentLevel > previousLevel )
            {
                currentHp = GetMaxHp();
                currentMp = GetMaxMp();
            }

            if ( _notifyStatChanged )
            {
                NotifyStatChanged();
                return;
            }

            ClampCurrentResourcesToMax();
            RefreshMaxExpByCurrentLevel();
        }

        ///<summary>
        /// 경험치 기준 레벨 결정
        ///</summary>
        private int ResolveLevelFromExperience( float _currentExp )
        {
            CPlayerLevelStatTableData levelStatTableData = ResolvePlayerLevelStatTableData();

            if ( levelStatTableData == null )
            {
                int fallbackLevel = Mathf.Max( 1, currentLevel );
                return fallbackLevel;
            }

            CPlayerLevelStatRow rowData = levelStatTableData.GetRowByExp( _currentExp );

            if ( rowData == null )
            {
                int fallbackLevel = Mathf.Max( 1, currentLevel );
                return fallbackLevel;
            }

            int resolvedLevel = Mathf.Max( 1, rowData.GetLv() );
            return resolvedLevel;
        }

        ///<summary>
        /// 사용 가능 레벨 결정
        ///</summary>
        private int ResolveAvailableLevel( int _level )
        {
            int normalizedLevel = Mathf.Max( 1, _level );
            CPlayerLevelStatTableData levelStatTableData = ResolvePlayerLevelStatTableData();

            if ( levelStatTableData == null )
            {
                return normalizedLevel;
            }

            CPlayerLevelStatRow rowData = levelStatTableData.GetClosestRow( normalizedLevel );

            if ( rowData == null )
            {
                return normalizedLevel;
            }

            int resolvedLevel = Mathf.Max( 1, rowData.GetLv() );
            return resolvedLevel;
        }

        ///<summary>
        /// 현재 레벨 최소 경험치 결정
        ///</summary>
        private float ResolveMinimumExpForLevel( int _level )
        {
            CPlayerLevelStatRow currentLevelRow = ResolveCurrentLevelRow( _level );

            if ( currentLevelRow == null )
            {
                return 0.0f;
            }

            float result = Mathf.Max( 0.0f, currentLevelRow.GetNeedExp() );
            return result;
        }

        ///<summary>
        /// 현재 레벨 다음 기준 경험치 갱신
        ///</summary>
        private void RefreshMaxExpByCurrentLevel()
        {
            CPlayerLevelStatTableData levelStatTableData = ResolvePlayerLevelStatTableData();

            if ( levelStatTableData == null )
            {
                maxExp = Mathf.Max( 1.0f, currentExp, maxExp );
                return;
            }

            CPlayerLevelStatRow nextRow = levelStatTableData.GetNextRow( currentLevel );

            if ( nextRow != null )
            {
                maxExp = Mathf.Max( 1.0f, nextRow.GetNeedExp() );
                return;
            }

            CPlayerLevelStatRow currentRow = ResolveCurrentLevelRow( currentLevel );
            float currentLevelNeedExp = currentRow != null ? currentRow.GetNeedExp() : 1.0f;
            maxExp = Mathf.Max( 1.0f, currentExp, currentLevelNeedExp );
        }

        ///<summary>
        /// 현재 레벨 행 결정
        ///</summary>
        private CPlayerLevelStatRow ResolveCurrentLevelRow( int _level )
        {
            CPlayerLevelStatTableData levelStatTableData = ResolvePlayerLevelStatTableData();

            if ( levelStatTableData == null )
            {
                return null;
            }

            bool isFound = levelStatTableData.TryGetRow( _level, out CPlayerLevelStatRow rowData );

            if ( isFound )
            {
                return rowData;
            }

            CPlayerLevelStatRow closestRow = levelStatTableData.GetClosestRow( _level );
            return closestRow;
        }

        ///<summary>
        /// 테이블 기반 기본 스탯 조회 시도
        ///</summary>
        private bool TryGetTableBaseStatValue( ePlayerStatType _statType, out float _value )
        {
            if ( _statType == ePlayerStatType.ATS || _statType == ePlayerStatType.MOVE || _statType == ePlayerStatType.CRT || _statType == ePlayerStatType.CRD || _statType == ePlayerStatType.ACC || _statType == ePlayerStatType.RANGE )
            {
                CPlayerDefaultStatTableData defaultStatTableData = ResolvePlayerDefaultStatTableData();

                if ( defaultStatTableData == null )
                {
                    _value = 0.0f;
                    return false;
                }

                CPlayerDefaultStatRow defaultRow = defaultStatTableData.GetDefaultRow();

                if ( defaultRow == null )
                {
                    _value = 0.0f;
                    return false;
                }

                if ( _statType == ePlayerStatType.ATS )
                {
                    _value = Mathf.Max( 0.0f, defaultRow.GetAts() );
                    return true;
                }

                if ( _statType == ePlayerStatType.MOVE )
                {
                    _value = Mathf.Max( DefaultMoveSpeedPercent, defaultRow.GetMov() );
                    return true;
                }

                if ( _statType == ePlayerStatType.CRT )
                {
                    _value = defaultRow.GetCrt();
                    return true;
                }

                if ( _statType == ePlayerStatType.CRD )
                {
                    _value = defaultRow.GetCrd();
                    return true;
                }

                if ( _statType == ePlayerStatType.ACC )
                {
                    _value = defaultRow.GetAcc();
                    return true;
                }

                if ( _statType == ePlayerStatType.RANGE )
                {
                    _value = defaultRow.GetRange();
                    return true;
                }

                _value = 0.0f;
                return true;
            }

            if ( _statType == ePlayerStatType.HP || _statType == ePlayerStatType.MP || _statType == ePlayerStatType.ATK || _statType == ePlayerStatType.DEF || _statType == ePlayerStatType.HR || _statType == ePlayerStatType.MR )
            {
                CPlayerLevelStatRow currentLevelRow = ResolveCurrentLevelRow( currentLevel );

                if ( currentLevelRow == null )
                {
                    _value = 0.0f;
                    return false;
                }

                switch ( _statType )
                {
                    case ePlayerStatType.HP:
                        _value = currentLevelRow.GetHp();
                        return true;

                    case ePlayerStatType.MP:
                        _value = currentLevelRow.GetMp();
                        return true;

                    case ePlayerStatType.ATK:
                        _value = currentLevelRow.GetAtk();
                        return true;

                    case ePlayerStatType.DEF:
                        _value = currentLevelRow.GetDef();
                        return true;

                    case ePlayerStatType.HR:
                        _value = currentLevelRow.GetHr();
                        return true;

                    case ePlayerStatType.MR:
                        _value = currentLevelRow.GetMr();
                        return true;
                }
            }

            _value = 0.0f;
            return false;
        }

        ///<summary>
        /// 플레이어 기본 스탯 테이블 결정
        ///</summary>
        private CPlayerDefaultStatTableData ResolvePlayerDefaultStatTableData()
        {
            if ( cachedPlayerDefaultStatTableData != null )
            {
                return cachedPlayerDefaultStatTableData;
            }

            CPlayerDefaultStatTableData loadedTableData = Resources.Load<CPlayerDefaultStatTableData>( PlayerDefaultStatTableResourcePath );
            cachedPlayerDefaultStatTableData = loadedTableData;
            return cachedPlayerDefaultStatTableData;
        }

        ///<summary>
        /// 플레이어 레벨 스탯 테이블 결정
        ///</summary>
        private CPlayerLevelStatTableData ResolvePlayerLevelStatTableData()
        {
            if ( cachedPlayerLevelStatTableData != null )
            {
                return cachedPlayerLevelStatTableData;
            }

            CPlayerLevelStatTableData loadedTableData = Resources.Load<CPlayerLevelStatTableData>( PlayerLevelStatTableResourcePath );
            cachedPlayerLevelStatTableData = loadedTableData;
            return cachedPlayerLevelStatTableData;
        }

        ///<summary>
        /// 현재 자원 상한 보정
        ///</summary>
        private void ClampCurrentResourcesToMax()
        {
            float resolvedMaxHp = GetMaxHp();
            float resolvedMaxMp = GetMaxMp();
            currentHp = Mathf.Clamp( currentHp, 0.0f, resolvedMaxHp );
            currentMp = Mathf.Clamp( currentMp, 0.0f, resolvedMaxMp );
        }

        ///<summary>
        /// 체력 변경 이벤트 전달
        ///</summary>
        private void RaiseHpChanged()
        {
            if ( OnHpChanged != null )
            {
                OnHpChanged( currentHp, GetMaxHp() );
            }
        }

        ///<summary>
        /// 마나 변경 이벤트 전달
        ///</summary>
        private void RaiseMpChanged()
        {
            if ( OnMpChanged != null )
            {
                OnMpChanged( currentMp, GetMaxMp() );
            }
        }

        ///<summary>
        /// 스탯 포인트 변경 이벤트 전달
        ///</summary>
        private void RaiseStatPointChanged()
        {
            if ( OnStatPointChanged != null )
            {
                OnStatPointChanged( unspentStatPoint );
            }
        }

        ///<summary>
        /// 레벨과 경험치 변경 이벤트 전달
        ///</summary>
        private void RaiseLevelExpChanged()
        {
            if ( OnLevelExpChanged != null )
            {
                OnLevelExpChanged( currentLevel, currentExp, maxExp );
            }
        }
    }
}
