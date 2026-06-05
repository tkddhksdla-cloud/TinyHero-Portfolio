using UnityEngine;

namespace TinyHero.Core.Data.Sample
{
    /// <summary>
    /// 샘플 아이템 엑셀 데이터를 보관하는 테이블 자산이다.
    /// </summary>
    [CreateAssetMenu( fileName = "SampleItemTableData", menuName = "TinyHero/Data/Sample Item Table Data" )]
    public sealed class CSampleItemTableData : CExcelTableData<CSampleItemRow>
    {
    }
}
