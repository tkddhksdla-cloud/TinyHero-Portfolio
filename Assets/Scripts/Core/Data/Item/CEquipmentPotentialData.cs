using System;
using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 장비 잠재 등급
    ///</summary>
    public enum eEquipmentPotentialRank
    {
        COMMON,
        RARE,
        UNIQUE,
        LEGENDARY
    }

    ///<summary>
    /// 장비 잠재 수치 타입
    ///</summary>
    public enum eEquipmentPotentialValueType
    {
        VALUE,
        PERCENT
    }

    ///<summary>
    /// 장비 잠재 옵션 종류
    ///</summary>
    public enum eEquipmentPotentialOptionType
    {
        NONE,
        HP,
        HR,
        MP,
        MR,
        ATK,
        DEF,
        CRT,
        CRD,
        ACC,
        ATS,
        MOVE,
        EXP_GAIN_PERCENT,
        GOLD_GAIN_PERCENT,
        FINAL_ATTACK_PERCENT,
        RANGE
    }

    ///<summary>
    /// 장비 잠재 1줄 데이터
    ///</summary>
    [Serializable]
    public sealed class CEquipmentPotentialLineData
    {
        [SerializeField] private string optionKey = string.Empty;
        [SerializeField] private eEquipmentPotentialOptionType optionType = eEquipmentPotentialOptionType.NONE;
        [SerializeField] private eEquipmentPotentialValueType valueType = eEquipmentPotentialValueType.VALUE;
        [SerializeField] private eEquipmentPotentialRank lineRank = eEquipmentPotentialRank.COMMON;
        [SerializeField] private float value;

        ///<summary>
        /// 잠재 옵션 안정 키 반환
        ///</summary>
        public string GetOptionKey()
        {
            string result = string.IsNullOrWhiteSpace( optionKey ) ? string.Empty : optionKey.Trim();
            return result;
        }

        ///<summary>
        /// 잠재 옵션 안정 키 설정
        ///</summary>
        public void SetOptionKey( string _optionKey )
        {
            optionKey = string.IsNullOrWhiteSpace( _optionKey ) ? string.Empty : _optionKey.Trim();
        }

        ///<summary>
        /// 잠재 옵션 종류 반환
        ///</summary>
        public eEquipmentPotentialOptionType GetOptionType()
        {
            eEquipmentPotentialOptionType result = optionType;
            return result;
        }

        ///<summary>
        /// 잠재 옵션 종류 설정
        ///</summary>
        public void SetOptionType( eEquipmentPotentialOptionType _optionType )
        {
            optionType = _optionType;
        }

        ///<summary>
        /// 잠재 수치 타입 반환
        ///</summary>
        public eEquipmentPotentialValueType GetValueType()
        {
            eEquipmentPotentialValueType result = CEquipmentPotentialUtility.ShouldForcePercentValueType( optionType ) ? eEquipmentPotentialValueType.PERCENT : valueType;
            return result;
        }

        ///<summary>
        /// 잠재 수치 타입 설정
        ///</summary>
        public void SetValueType( eEquipmentPotentialValueType _valueType )
        {
            valueType = _valueType;
        }

        ///<summary>
        /// 잠재 줄 등급 반환
        ///</summary>
        public eEquipmentPotentialRank GetLineRank()
        {
            eEquipmentPotentialRank result = lineRank;
            return result;
        }

        ///<summary>
        /// 잠재 줄 등급 설정
        ///</summary>
        public void SetLineRank( eEquipmentPotentialRank _lineRank )
        {
            lineRank = _lineRank;
        }

        ///<summary>
        /// 잠재 옵션 수치 반환
        ///</summary>
        public float GetValue()
        {
            float result = value;
            return result;
        }

        ///<summary>
        /// 잠재 옵션 수치 설정
        ///</summary>
        public void SetValue( float _value )
        {
            value = _value;
        }

        ///<summary>
        /// 잠재 옵션 유효 여부 반환
        ///</summary>
        public bool HasValue()
        {
            bool result = optionType != eEquipmentPotentialOptionType.NONE && Mathf.Approximately( value, 0.0f ) == false;
            return result;
        }

        ///<summary>
        /// 잠재 옵션 초기화
        ///</summary>
        public void Clear()
        {
            optionKey = string.Empty;
            optionType = eEquipmentPotentialOptionType.NONE;
            valueType = eEquipmentPotentialValueType.VALUE;
            lineRank = eEquipmentPotentialRank.COMMON;
            value = 0.0f;
        }

        ///<summary>
        /// 잠재 옵션 복사 생성
        ///</summary>
        public CEquipmentPotentialLineData CreateCopy()
        {
            CEquipmentPotentialLineData copiedData = new CEquipmentPotentialLineData();
            copiedData.optionKey = optionKey;
            copiedData.optionType = optionType;
            copiedData.valueType = valueType;
            copiedData.lineRank = lineRank;
            copiedData.value = value;
            return copiedData;
        }

        ///<summary>
        /// 잠재 옵션 복사 반영
        ///</summary>
        public void CopyFrom( CEquipmentPotentialLineData _sourceData )
        {
            if ( _sourceData == null )
            {
                Clear();
                return;
            }

            optionKey = _sourceData.GetOptionKey();
            optionType = _sourceData.GetOptionType();
            valueType = _sourceData.GetValueType();
            lineRank = _sourceData.GetLineRank();
            value = _sourceData.GetValue();
        }

        ///<summary>
        /// 잠재 옵션 테이블 엔트리 반영
        ///</summary>
        public void CopyFromOptionEntry( CEquipmentPotentialOptionEntry _optionEntry, eEquipmentPotentialRank _lineRank )
        {
            if ( _optionEntry == null )
            {
                Clear();
                return;
            }

            optionKey = _optionEntry.GetOptionKey();
            optionType = _optionEntry.GetOptionType();
            valueType = _optionEntry.GetValueType();
            lineRank = _lineRank;
            value = _optionEntry.GetValue();
        }
    }

    ///<summary>
    /// 장비 잠재 줄 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CEquipmentPotentialLineSnapshotData
    {
        public int lineIndex;
        public string optionKey = string.Empty;
        public eEquipmentPotentialRank lineRank = eEquipmentPotentialRank.COMMON;
    }

    ///<summary>
    /// 장비 잠재 저장 데이터
    ///</summary>
    [Serializable]
    public sealed class CEquipmentPotentialSnapshotData
    {
        public eEquipmentPotentialRank rank = eEquipmentPotentialRank.COMMON;
        public List<CEquipmentPotentialLineSnapshotData> lineSnapshotList = new List<CEquipmentPotentialLineSnapshotData>();

        ///<summary>
        /// 잠재 저장 데이터 보유 여부 반환
        ///</summary>
        public bool HasPotential()
        {
            bool result = lineSnapshotList != null && lineSnapshotList.Count > 0;
            return result;
        }
    }

    ///<summary>
    /// 장비 잠재 전체 데이터
    ///</summary>
    [Serializable]
    public sealed class CEquipmentPotentialData
    {
        public const int FixedLineCount = 3;

        [SerializeField] private eEquipmentPotentialRank rank = eEquipmentPotentialRank.COMMON;
        [SerializeField] private List<CEquipmentPotentialLineData> lineDataList = new List<CEquipmentPotentialLineData>();

        ///<summary>
        /// 잠재 등급 반환
        ///</summary>
        public eEquipmentPotentialRank GetRank()
        {
            eEquipmentPotentialRank result = rank;
            return result;
        }

        ///<summary>
        /// 잠재 등급 설정
        ///</summary>
        public void SetRank( eEquipmentPotentialRank _rank )
        {
            rank = _rank;
        }

        ///<summary>
        /// 잠재 줄 목록 반환
        ///</summary>
        public IReadOnlyList<CEquipmentPotentialLineData> GetLineDataList()
        {
            EnsureLineCapacity();
            IReadOnlyList<CEquipmentPotentialLineData> result = lineDataList;
            return result;
        }

        ///<summary>
        /// 잠재 줄 데이터 반환
        ///</summary>
        public CEquipmentPotentialLineData GetLineData( int _index )
        {
            EnsureLineCapacity();

            if ( _index < 0 || _index >= lineDataList.Count )
            {
                return null;
            }

            CEquipmentPotentialLineData result = lineDataList[ _index ];
            return result;
        }

        ///<summary>
        /// 잠재 줄 데이터 설정
        ///</summary>
        public void SetLineData( int _index, CEquipmentPotentialLineData _lineData )
        {
            EnsureLineCapacity();

            if ( _index < 0 || _index >= lineDataList.Count )
            {
                return;
            }

            CEquipmentPotentialLineData targetLineData = lineDataList[ _index ];

            if ( targetLineData == null )
            {
                targetLineData = new CEquipmentPotentialLineData();
                lineDataList[ _index ] = targetLineData;
            }

            targetLineData.CopyFrom( _lineData );
        }

        ///<summary>
        /// 잠재 보유 여부 반환
        ///</summary>
        public bool HasPotential()
        {
            EnsureLineCapacity();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null || lineData.HasValue() == false )
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        ///<summary>
        /// 잠재 스탯 보너스 누적
        ///</summary>
        public void AccumulateStatBonus( CPlayerStatRuntimeData _targetFlatStatBonus, CPlayerStatRuntimeData _targetPercentStatBonus )
        {
            EnsureLineCapacity();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null || lineData.HasValue() == false )
                {
                    continue;
                }

                bool isPlayerStatOption = CEquipmentPotentialUtility.TryConvertToPlayerStatType( lineData.GetOptionType(), out ePlayerStatType statType );

                if ( isPlayerStatOption == false )
                {
                    continue;
                }

                if ( lineData.GetValueType() == eEquipmentPotentialValueType.PERCENT )
                {
                    if ( _targetPercentStatBonus != null )
                    {
                        _targetPercentStatBonus.AddStatValue( statType, lineData.GetValue() );
                    }

                    continue;
                }

                if ( _targetFlatStatBonus != null )
                {
                    _targetFlatStatBonus.AddStatValue( statType, lineData.GetValue() );
                }
            }
        }

        ///<summary>
        /// 잠재 스탯 보너스 누적
        ///</summary>
        public void AccumulateStatBonus( CPlayerStatRuntimeData _targetStatBonus )
        {
            AccumulateStatBonus( _targetStatBonus, null );
        }

        ///<summary>
        /// 잠재 특수 보너스 누적
        ///</summary>
        public void AccumulateModifierBonus( CPlayerModifierRuntimeData _targetModifierBonus )
        {
            if ( _targetModifierBonus == null )
            {
                return;
            }

            EnsureLineCapacity();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null || lineData.HasValue() == false )
                {
                    continue;
                }

                eEquipmentPotentialOptionType optionType = lineData.GetOptionType();
                float optionValue = lineData.GetValue();

                switch ( optionType )
                {
                    case eEquipmentPotentialOptionType.EXP_GAIN_PERCENT:
                        _targetModifierBonus.AddExpGainPercent( optionValue );
                        break;

                    case eEquipmentPotentialOptionType.GOLD_GAIN_PERCENT:
                        _targetModifierBonus.AddGoldGainPercent( optionValue );
                        break;

                    case eEquipmentPotentialOptionType.FINAL_ATTACK_PERCENT:
                        _targetModifierBonus.AddFinalAttackPercent( optionValue );
                        break;
                }
            }
        }

        ///<summary>
        /// 잠재 전체 초기화
        ///</summary>
        public void Clear()
        {
            rank = eEquipmentPotentialRank.COMMON;
            EnsureLineCapacity();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null )
                {
                    lineData = new CEquipmentPotentialLineData();
                    lineDataList[ index ] = lineData;
                }

                lineData.Clear();
            }
        }

        ///<summary>
        /// 잠재 복사 생성
        ///</summary>
        public CEquipmentPotentialData CreateCopy()
        {
            CEquipmentPotentialData copiedData = new CEquipmentPotentialData();
            copiedData.CopyFrom( this );
            return copiedData;
        }

        ///<summary>
        /// 잠재 저장 데이터 생성
        ///</summary>
        public CEquipmentPotentialSnapshotData CreateSnapshotData()
        {
            EnsureLineCapacity();
            CEquipmentPotentialSnapshotData snapshotData = new CEquipmentPotentialSnapshotData();
            snapshotData.rank = rank;

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null || lineData.HasValue() == false )
                {
                    continue;
                }

                string resolvedOptionKey = lineData.GetOptionKey();

                if ( string.IsNullOrWhiteSpace( resolvedOptionKey ) )
                {
                    bool hasResolvedKey = CEquipmentPotentialDatabase.TryResolveOptionKey( lineData, out string fallbackOptionKey );
                    resolvedOptionKey = hasResolvedKey ? fallbackOptionKey : string.Empty;
                }

                if ( string.IsNullOrWhiteSpace( resolvedOptionKey ) )
                {
                    continue;
                }

                CEquipmentPotentialLineSnapshotData lineSnapshotData = new CEquipmentPotentialLineSnapshotData();
                lineSnapshotData.lineIndex = index;
                lineSnapshotData.optionKey = resolvedOptionKey;
                lineSnapshotData.lineRank = lineData.GetLineRank();
                snapshotData.lineSnapshotList.Add( lineSnapshotData );
            }

            return snapshotData;
        }

        ///<summary>
        /// 잠재 저장 데이터 반영
        ///</summary>
        public void LoadSnapshotData( CEquipmentPotentialSnapshotData _snapshotData )
        {
            Clear();

            if ( _snapshotData == null || _snapshotData.lineSnapshotList == null )
            {
                return;
            }

            rank = _snapshotData.rank;

            for ( int index = 0; index < _snapshotData.lineSnapshotList.Count; index++ )
            {
                CEquipmentPotentialLineSnapshotData lineSnapshotData = _snapshotData.lineSnapshotList[ index ];

                if ( lineSnapshotData == null )
                {
                    continue;
                }

                if ( lineSnapshotData.lineIndex < 0 || lineSnapshotData.lineIndex >= FixedLineCount )
                {
                    continue;
                }

                bool hasOptionEntry = CEquipmentPotentialDatabase.TryGetOptionEntryByKey( lineSnapshotData.optionKey, out CEquipmentPotentialOptionEntry optionEntry );

                if ( hasOptionEntry == false || optionEntry == null )
                {
                    continue;
                }

                CEquipmentPotentialLineData lineData = GetLineData( lineSnapshotData.lineIndex );

                if ( lineData == null )
                {
                    continue;
                }

                lineData.CopyFromOptionEntry( optionEntry, lineSnapshotData.lineRank );
            }
        }

        ///<summary>
        /// 잠재 복사 반영
        ///</summary>
        public void CopyFrom( CEquipmentPotentialData _sourceData )
        {
            EnsureLineCapacity();

            if ( _sourceData == null )
            {
                Clear();
                return;
            }

            rank = _sourceData.rank;
            IReadOnlyList<CEquipmentPotentialLineData> sourceLineDataList = _sourceData.GetLineDataList();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData targetLineData = lineDataList[ index ];

                if ( targetLineData == null )
                {
                    targetLineData = new CEquipmentPotentialLineData();
                    lineDataList[ index ] = targetLineData;
                }

                CEquipmentPotentialLineData sourceLineData = index < sourceLineDataList.Count ? sourceLineDataList[ index ] : null;
                targetLineData.CopyFrom( sourceLineData );
            }
        }

        ///<summary>
        /// 고정 줄 수 보정
        ///</summary>
        private void EnsureLineCapacity()
        {
            if ( lineDataList == null )
            {
                lineDataList = new List<CEquipmentPotentialLineData>();
            }

            while ( lineDataList.Count < FixedLineCount )
            {
                lineDataList.Add( new CEquipmentPotentialLineData() );
            }

            while ( lineDataList.Count > FixedLineCount )
            {
                lineDataList.RemoveAt( lineDataList.Count - 1 );
            }
        }
    }
}
