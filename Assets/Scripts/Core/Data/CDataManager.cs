using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 데이터 관리 컴포넌트
    ///</summary>
    public sealed class CDataManager : CSingleTon<CDataManager>
    {
        [SerializeField] private List<CExcelTableDataBase> tableDataList = new List<CExcelTableDataBase>();

        private readonly Dictionary<Type, CExcelTableDataBase> tableDataDictionary = new Dictionary<Type, CExcelTableDataBase>();

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( Instance != this )
            {
                return;
            }

            RebuildTableCache();
        }

        ///<summary>
        /// 테이블 캐시 재구성
        ///</summary>
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

        ///<summary>
        /// 테이블 데이터 반환
        ///</summary>
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

        ///<summary>
        /// 테이블 데이터 조회 시도
        ///</summary>
        public bool TryGetTable<TTable>(out TTable _tableData) where TTable : CExcelTableDataBase
        {
            Type tableType = typeof( TTable );
            bool isFound = tableDataDictionary.TryGetValue( tableType, out CExcelTableDataBase baseTableData );

            if ( isFound == false )
            {
                _tableData = null;
                return false;
            }

            _tableData = baseTableData as TTable;
            bool hasResult = _tableData != null;
            return hasResult;
        }
    }
}


