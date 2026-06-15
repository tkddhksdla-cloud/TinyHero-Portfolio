using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 플레이어 레벨 스탯 테이블 데이터
    ///</summary>
    [CreateAssetMenu( fileName = "PlayerLevelStatTableData", menuName = "TinyHero/Data/Player Level Stat Table Data" )]
    public sealed class CPlayerLevelStatTableData : CExcelTableData<CPlayerLevelStatRow>
    {
        ///<summary>
        /// 레벨 기준 행 조회 시도
        ///</summary>
        public bool TryGetRow( int _level, out CPlayerLevelStatRow _rowData )
        {
            List<CPlayerLevelStatRow> rowList = GetRowList();

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CPlayerLevelStatRow rowData = rowList[ index ];

                if ( rowData == null )
                {
                    continue;
                }

                if ( rowData.GetLv() != _level )
                {
                    continue;
                }

                _rowData = rowData;
                return true;
            }

            _rowData = null;
            return false;
        }

        ///<summary>
        /// 레벨 이하 최대 행 반환
        ///</summary>
        public CPlayerLevelStatRow GetClosestRow( int _level )
        {
            List<CPlayerLevelStatRow> rowList = GetRowList();
            CPlayerLevelStatRow closestRow = null;

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CPlayerLevelStatRow rowData = rowList[ index ];

                if ( rowData == null )
                {
                    continue;
                }

                if ( rowData.GetLv() > _level )
                {
                    continue;
                }

                if ( closestRow == null || rowData.GetLv() > closestRow.GetLv() )
                {
                    closestRow = rowData;
                }
            }

            return closestRow;
        }

        ///<summary>
        /// 다음 레벨 행 반환
        ///</summary>
        public CPlayerLevelStatRow GetNextRow( int _level )
        {
            List<CPlayerLevelStatRow> rowList = GetRowList();
            CPlayerLevelStatRow nextRow = null;

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CPlayerLevelStatRow rowData = rowList[ index ];

                if ( rowData == null )
                {
                    continue;
                }

                if ( rowData.GetLv() <= _level )
                {
                    continue;
                }

                if ( nextRow == null || rowData.GetLv() < nextRow.GetLv() )
                {
                    nextRow = rowData;
                }
            }

            return nextRow;
        }

        ///<summary>
        /// 경험치 기준 현재 레벨 행 반환
        ///</summary>
        public CPlayerLevelStatRow GetRowByExp( float _currentExp )
        {
            List<CPlayerLevelStatRow> rowList = GetRowList();
            CPlayerLevelStatRow resolvedRow = null;

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CPlayerLevelStatRow rowData = rowList[ index ];

                if ( rowData == null )
                {
                    continue;
                }

                if ( rowData.GetNeedExp() > _currentExp )
                {
                    continue;
                }

                if ( resolvedRow == null || rowData.GetLv() > resolvedRow.GetLv() )
                {
                    resolvedRow = rowData;
                }
            }

            if ( resolvedRow != null )
            {
                return resolvedRow;
            }

            if ( rowList == null || rowList.Count <= 0 )
            {
                return null;
            }

            CPlayerLevelStatRow firstRow = rowList[ 0 ];
            return firstRow;
        }
    }
}
