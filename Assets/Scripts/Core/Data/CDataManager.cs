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
        private const eTextLanguage DefaultTextLanguage = eTextLanguage.KR;
        private const string TextTableResourcePath = "Data/Text";

        [SerializeField] private List<CExcelTableDataBase> tableDataList = new List<CExcelTableDataBase>();
        [SerializeField] private eTextLanguage currentTextLanguage = DefaultTextLanguage;

        private readonly Dictionary<Type, CExcelTableDataBase> tableDataDictionary = new Dictionary<Type, CExcelTableDataBase>();
        private readonly Dictionary<string, CTextTableRow> textRowDictionary = new Dictionary<string, CTextTableRow>();

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
            RebuildTextCache();
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
        public bool TryGetTable<TTable>( out TTable _tableData ) where TTable : CExcelTableDataBase
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

        ///<summary>
        /// 현재 텍스트 언어 반환
        ///</summary>
        public eTextLanguage GetCurrentTextLanguage()
        {
            eTextLanguage result = currentTextLanguage;
            return result;
        }

        ///<summary>
        /// 현재 텍스트 언어 설정
        ///</summary>
        public void SetCurrentTextLanguage( eTextLanguage _textLanguage )
        {
            currentTextLanguage = _textLanguage;
        }

        ///<summary>
        /// 텍스트 키 기반 문자열 반환
        ///</summary>
        public static string GetText( string _key )
        {
            bool hasDataManager = TryGetExistingInstance( out CDataManager dataManager );
            string result = hasDataManager && dataManager != null ? dataManager.ResolveText( _key ) : NormalizeTextKey( _key );
            return result;
        }

        ///<summary>
        /// 텍스트 키와 언어 기반 문자열 반환
        ///</summary>
        public static string GetText( string _key, eTextLanguage _textLanguage )
        {
            bool hasDataManager = TryGetExistingInstance( out CDataManager dataManager );
            string result = hasDataManager && dataManager != null ? dataManager.ResolveText( _key, _textLanguage ) : NormalizeTextKey( _key );
            return result;
        }

        ///<summary>
        /// 텍스트 키 기반 문자열 반환
        ///</summary>
        private string ResolveText( string _key )
        {
            string result = ResolveText( _key, currentTextLanguage );
            return result;
        }

        ///<summary>
        /// 텍스트 키와 언어 기반 문자열 반환
        ///</summary>
        private string ResolveText( string _key, eTextLanguage _textLanguage )
        {
            string normalizedKey = NormalizeTextKey( _key );

            if ( string.IsNullOrWhiteSpace( normalizedKey ) )
            {
                return string.Empty;
            }

            bool hasText = TryGetText( normalizedKey, _textLanguage, out string text );

            if ( hasText == false )
            {
                return normalizedKey;
            }

            string result = text;
            return result;
        }

        ///<summary>
        /// 텍스트 키 기반 문자열 조회 시도
        ///</summary>
        public bool TryGetText( string _key, out string _text )
        {
            bool result = TryGetText( _key, currentTextLanguage, out _text );
            return result;
        }

        ///<summary>
        /// 텍스트 키와 언어 기반 문자열 조회 시도
        ///</summary>
        public bool TryGetText( string _key, eTextLanguage _textLanguage, out string _text )
        {
            string normalizedKey = NormalizeTextKey( _key );

            if ( string.IsNullOrWhiteSpace( normalizedKey ) )
            {
                _text = string.Empty;
                return false;
            }

            if ( textRowDictionary.Count == 0 )
            {
                RebuildTextCache();
            }

            bool hasRow = textRowDictionary.TryGetValue( normalizedKey, out CTextTableRow textTableRow );

            if ( hasRow == false || textTableRow == null )
            {
                _text = normalizedKey;
                return false;
            }

            string resolvedText = textTableRow.GetText( _textLanguage );

            if ( string.IsNullOrWhiteSpace( resolvedText ) )
            {
                _text = normalizedKey;
                return false;
            }

            _text = resolvedText;
            return true;
        }

        ///<summary>
        /// 텍스트 테이블 캐시 재구성
        ///</summary>
        private void RebuildTextCache()
        {
            textRowDictionary.Clear();

            for ( int index = 0; index < tableDataList.Count; index++ )
            {
                CExcelTableDataBase tableData = tableDataList[ index ];
                CTextTableData textTableData = tableData as CTextTableData;

                if ( textTableData == null )
                {
                    continue;
                }

                RegisterTextTableRows( textTableData );
            }

            RegisterResourceTextTableRows();
        }

        ///<summary>
        /// Resources 텍스트 테이블 행 등록
        ///</summary>
        private void RegisterResourceTextTableRows()
        {
            CTextTableData[] textTableDataArray = Resources.LoadAll<CTextTableData>( TextTableResourcePath );

            if ( textTableDataArray == null || textTableDataArray.Length == 0 )
            {
                return;
            }

            for ( int index = 0; index < textTableDataArray.Length; index++ )
            {
                CTextTableData textTableData = textTableDataArray[ index ];

                if ( textTableData == null )
                {
                    continue;
                }

                RegisterTextTableRows( textTableData );
            }
        }

        ///<summary>
        /// 텍스트 테이블 행 등록
        ///</summary>
        private void RegisterTextTableRows( CTextTableData _textTableData )
        {
            if ( _textTableData == null )
            {
                return;
            }

            List<CTextTableRow> rowList = _textTableData.GetRowList();

            if ( rowList == null )
            {
                return;
            }

            for ( int index = 0; index < rowList.Count; index++ )
            {
                CTextTableRow row = rowList[ index ];

                if ( row == null )
                {
                    continue;
                }

                string key = row.GetKey();

                if ( string.IsNullOrWhiteSpace( key ) )
                {
                    continue;
                }

                textRowDictionary[ key ] = row;
            }
        }

        ///<summary>
        /// 텍스트 키 정규화
        ///</summary>
        private static string NormalizeTextKey( string _key )
        {
            string result = string.IsNullOrWhiteSpace( _key ) ? string.Empty : _key.Trim();
            return result;
        }
    }
}


