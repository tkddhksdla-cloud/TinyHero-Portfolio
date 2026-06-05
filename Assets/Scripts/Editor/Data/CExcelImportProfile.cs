using TinyHero.Core.Data;
using UnityEditor;
using UnityEngine;

namespace TinyHero.DataEditor
{
    /// <summary>
    /// 엑셀 파일과 대상 테이블 자산을 연결하는 import 설정 자산이다.
    /// </summary>
    [CreateAssetMenu( fileName = "ExcelImportProfile", menuName = "TinyHero/Data/Excel Import Profile" )]
    public sealed class CExcelImportProfile : ScriptableObject
    {
        [SerializeField] private DefaultAsset sourceExcelFile;
        [SerializeField] private string worksheetName = string.Empty;
        [SerializeField] private CExcelTableDataBase targetTableData;

        /// <summary>
        /// 원본 엑셀 에셋을 반환한다.
        /// </summary>
        public DefaultAsset GetSourceExcelFile()
        {
            DefaultAsset result = sourceExcelFile;
            return result;
        }

        /// <summary>
        /// 가져올 워크시트 이름을 반환한다.
        /// </summary>
        public string GetWorksheetName()
        {
            string result = worksheetName;
            return result;
        }

        /// <summary>
        /// 데이터를 저장할 대상 테이블 자산을 반환한다.
        /// </summary>
        public CExcelTableDataBase GetTargetTableData()
        {
            CExcelTableDataBase result = targetTableData;
            return result;
        }
    }
}
