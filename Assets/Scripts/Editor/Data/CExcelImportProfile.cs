using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    ///<summary>
    /// 엑셀 가져오기 프로필 클래스
    ///</summary>
    [CreateAssetMenu( fileName = "ExcelImportProfile", menuName = "TinyHero/Data/Excel Import Profile" )]
    public sealed class CExcelImportProfile : ScriptableObject
    {
        [SerializeField] private DefaultAsset sourceExcelFile;
        [SerializeField] private string worksheetName = string.Empty;
        [SerializeField] private CExcelTableDataBase targetTableData;

        ///<summary>
        /// 원본 엑셀 파일 반환
        ///</summary>
        public DefaultAsset GetSourceExcelFile()
        {
            DefaultAsset result = sourceExcelFile;
            return result;
        }

        ///<summary>
        /// 워크시트 이름 반환
        ///</summary>
        public string GetWorksheetName()
        {
            string result = worksheetName;
            return result;
        }

        ///<summary>
        /// 대상 테이블 데이터 반환
        ///</summary>
        public CExcelTableDataBase GetTargetTableData()
        {
            CExcelTableDataBase result = targetTableData;
            return result;
        }
    }
}


