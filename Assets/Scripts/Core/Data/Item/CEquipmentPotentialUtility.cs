using System.Collections.Generic;
using System.Text;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 장비 잠재 공용 유틸리티
    ///</summary>
    public static class CEquipmentPotentialUtility
    {
        private static readonly Color CommonColor = new Color32( 201, 201, 201, 255 );
        private static readonly Color RareColor = new Color32( 66, 156, 255, 255 );
        private static readonly Color UniqueColor = new Color32( 255, 202, 63, 255 );
        private static readonly Color LegendaryColor = new Color32( 66, 255, 170, 255 );

        ///<summary>
        /// 잠재 옵션의 플레이어 스탯 변환 시도
        ///</summary>
        public static bool TryConvertToPlayerStatType( eEquipmentPotentialOptionType _optionType, out ePlayerStatType _statType )
        {
            _statType = ePlayerStatType.HP;

            switch ( _optionType )
            {
                case eEquipmentPotentialOptionType.HP:
                    _statType = ePlayerStatType.HP;
                    return true;

                case eEquipmentPotentialOptionType.HR:
                    _statType = ePlayerStatType.HR;
                    return true;

                case eEquipmentPotentialOptionType.MP:
                    _statType = ePlayerStatType.MP;
                    return true;

                case eEquipmentPotentialOptionType.MR:
                    _statType = ePlayerStatType.MR;
                    return true;

                case eEquipmentPotentialOptionType.ATK:
                    _statType = ePlayerStatType.ATK;
                    return true;

                case eEquipmentPotentialOptionType.DEF:
                    _statType = ePlayerStatType.DEF;
                    return true;

                case eEquipmentPotentialOptionType.CRT:
                    _statType = ePlayerStatType.CRT;
                    return true;

                case eEquipmentPotentialOptionType.CRD:
                    _statType = ePlayerStatType.CRD;
                    return true;

                case eEquipmentPotentialOptionType.ACC:
                    _statType = ePlayerStatType.ACC;
                    return true;

                case eEquipmentPotentialOptionType.ATS:
                    _statType = ePlayerStatType.ATS;
                    return true;

                case eEquipmentPotentialOptionType.MOVE:
                    _statType = ePlayerStatType.MOVE;
                    return true;
            }

            return false;
        }

        ///<summary>
        /// 잠재 옵션 표시 이름 반환
        ///</summary>
        public static string GetOptionLabel( eEquipmentPotentialOptionType _optionType )
        {
            switch ( _optionType )
            {
                case eEquipmentPotentialOptionType.HP:
                    return "최대 HP 증가";

                case eEquipmentPotentialOptionType.HR:
                    return "HP 회복 증가";

                case eEquipmentPotentialOptionType.MP:
                    return "최대 MP 증가";

                case eEquipmentPotentialOptionType.MR:
                    return "MP 회복 증가";

                case eEquipmentPotentialOptionType.ATK:
                    return "공격력 증가";

                case eEquipmentPotentialOptionType.DEF:
                    return "방어력 증가";

                case eEquipmentPotentialOptionType.CRT:
                    return "치명타 확률 증가";

                case eEquipmentPotentialOptionType.CRD:
                    return "치명타 피해 증가";

                case eEquipmentPotentialOptionType.ACC:
                    return "명중률 증가";

                case eEquipmentPotentialOptionType.ATS:
                    return "공격 속도 증가";

                case eEquipmentPotentialOptionType.MOVE:
                    return "이동 속도 증가";

                case eEquipmentPotentialOptionType.EXP_GAIN_PERCENT:
                    return "경험치 획득량 증가";

                case eEquipmentPotentialOptionType.GOLD_GAIN_PERCENT:
                    return "골드 획득량 증가";

                case eEquipmentPotentialOptionType.FINAL_ATTACK_PERCENT:
                    return "최종 공격력 증가";
            }

            return "없음";
        }

        ///<summary>
        /// 퍼센트 전용 옵션 여부 반환
        ///</summary>
        public static bool ShouldForcePercentValueType( eEquipmentPotentialOptionType _optionType )
        {
            bool result = _optionType == eEquipmentPotentialOptionType.EXP_GAIN_PERCENT
                || _optionType == eEquipmentPotentialOptionType.GOLD_GAIN_PERCENT
                || _optionType == eEquipmentPotentialOptionType.FINAL_ATTACK_PERCENT;
            return result;
        }

        ///<summary>
        /// 잠재 옵션 기본 수치 타입 반환
        ///</summary>
        public static eEquipmentPotentialValueType GetDefaultValueType( eEquipmentPotentialOptionType _optionType )
        {
            if ( ShouldForcePercentValueType( _optionType ) )
            {
                return eEquipmentPotentialValueType.PERCENT;
            }

            return eEquipmentPotentialValueType.VALUE;
        }

        ///<summary>
        /// 잠재 옵션 기본 퍼센트 여부 반환
        ///</summary>
        public static bool IsPercentOption( eEquipmentPotentialOptionType _optionType )
        {
            bool result = GetDefaultValueType( _optionType ) == eEquipmentPotentialValueType.PERCENT;
            return result;
        }

        ///<summary>
        /// 잠재 등급 단축 문자 반환
        ///</summary>
        public static string GetRankShortLabel( eEquipmentPotentialRank _rank )
        {
            switch ( _rank )
            {
                case eEquipmentPotentialRank.COMMON:
                    return "C";

                case eEquipmentPotentialRank.RARE:
                    return "R";

                case eEquipmentPotentialRank.UNIQUE:
                    return "U";

                case eEquipmentPotentialRank.LEGENDARY:
                    return "L";
            }

            return "?";
        }

        ///<summary>
        /// 잠재 등급 색상 반환
        ///</summary>
        public static Color GetRankColor( eEquipmentPotentialRank _rank )
        {
            switch ( _rank )
            {
                case eEquipmentPotentialRank.COMMON:
                    return CommonColor;

                case eEquipmentPotentialRank.RARE:
                    return RareColor;

                case eEquipmentPotentialRank.UNIQUE:
                    return UniqueColor;

                case eEquipmentPotentialRank.LEGENDARY:
                    return LegendaryColor;
            }

            return CommonColor;
        }

        ///<summary>
        /// 한 단계 낮은 잠재 등급 반환
        ///</summary>
        public static eEquipmentPotentialRank GetPreviousRank( eEquipmentPotentialRank _rank )
        {
            switch ( _rank )
            {
                case eEquipmentPotentialRank.RARE:
                    return eEquipmentPotentialRank.COMMON;

                case eEquipmentPotentialRank.UNIQUE:
                    return eEquipmentPotentialRank.RARE;

                case eEquipmentPotentialRank.LEGENDARY:
                    return eEquipmentPotentialRank.UNIQUE;
            }

            return eEquipmentPotentialRank.COMMON;
        }

        ///<summary>
        /// 잠재 한 줄 표시 문자열 반환
        ///</summary>
        public static string BuildLineText( eEquipmentPotentialRank _rank, CEquipmentPotentialLineData _lineData )
        {
            eEquipmentPotentialRank resolvedRank = _lineData != null ? _lineData.GetLineRank() : _rank;

            if ( _lineData == null || _lineData.HasValue() == false )
            {
                string emptyResult = $"[{GetRankShortLabel( resolvedRank )}] 잠재 없음";
                return emptyResult;
            }

            string optionLabel = GetOptionLabel( _lineData.GetOptionType() );
            string valueText = FormatOptionValue( _lineData.GetValueType(), _lineData.GetValue() );
            string result = $"[{GetRankShortLabel( resolvedRank )}] {optionLabel} {valueText}";
            return result;
        }

        ///<summary>
        /// 잠재 전체 표시 문자열 반환
        ///</summary>
        public static string BuildSummaryText( CEquipmentPotentialData _potentialData )
        {
            if ( _potentialData == null || _potentialData.HasPotential() == false )
            {
                return string.Empty;
            }

            StringBuilder summaryBuilder = new StringBuilder();
            eEquipmentPotentialRank rank = _potentialData.GetRank();
            IReadOnlyList<CEquipmentPotentialLineData> lineDataList = _potentialData.GetLineDataList();

            for ( int index = 0; index < lineDataList.Count; index++ )
            {
                CEquipmentPotentialLineData lineData = lineDataList[ index ];

                if ( lineData == null || lineData.HasValue() == false )
                {
                    continue;
                }

                if ( summaryBuilder.Length > 0 )
                {
                    summaryBuilder.AppendLine();
                }

                string lineText = BuildLineText( rank, lineData );
                summaryBuilder.Append( lineText );
            }

            string result = summaryBuilder.ToString();
            return result;
        }

        ///<summary>
        /// 잠재 수치 표시 문자열 반환
        ///</summary>
        public static string FormatOptionValue( eEquipmentPotentialValueType _valueType, float _value )
        {
            string prefixText = _value >= 0.0f ? "+" : string.Empty;
            bool isPercentValue = _valueType == eEquipmentPotentialValueType.PERCENT;
            string result = isPercentValue ? $"{prefixText}{_value:0.##}%" : $"{prefixText}{_value:0.##}";
            return result;
        }
    }
}
