using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    /// <summary>
    /// 등록된 데이터 테이블 자산을 타입 기준으로 관리하는 매니저이다.
    /// </summary>
    public sealed class CDataManager : CSingleTon<CDataManager>
    {
        [SerializeField] private List<CExcelTableDataBase> tableDataList = new List<CExcelTableDataBase>();

        private readonly Dictionary<Type, CExcelTableDataBase> tableDataDictionary = new Dictionary<Type, CExcelTableDataBase>();

        /// <summary>
        /// 데이터 테이블 캐시를 초기화한다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if ( Instance != this )
            {
                return;
            }

            RebuildTableCache();
        }

        /// <summary>
        /// 인스펙터에 등록된 테이블 목록으로 캐시를 다시 구성한다.
        /// </summary>
        public void RebuildTableCache()
        {
            tableDataDictionary.Clear();

            for ( int i = 0; i < tableDataList.Count; i++ )
            {
                CExcelTableDataBase tableData = tableDataList[ i ];

                if ( tableData == null )
                {
                    continue;
                }

                Type tableType = tableData.GetType();
                tableDataDictionary[ tableType ] = tableData;
            }
        }

        /// <summary>
        /// 요청한 타입의 데이터 테이블을 반환한다.
        /// </summary>
        public TTable GetTable<TTable>() where TTable : CExcelTableDataBase
        {
            bool isFound = TryGetTable( out TTable tableData );

            if ( isFound == false )
            {
                Debug.LogError( $"{typeof( TTable ).Name} table is not registered in CDataManager." );
                return null;
            }

            TTable result = tableData;
            return result;
        }

        /// <summary>
        /// 요청한 타입의 데이터 테이블을 안전하게 조회한다.
        /// </summary>
        public bool TryGetTable<TTable>( out TTable tableData ) where TTable : CExcelTableDataBase
        {
            Type tableType = typeof( TTable );
            bool isFound = tableDataDictionary.TryGetValue( tableType, out CExcelTableDataBase baseTableData );

            if ( isFound == false )
            {
                tableData = null;
                return false;
            }

            tableData = baseTableData as TTable;
            bool hasResult = tableData != null;
            return hasResult;
        }
    }
}
