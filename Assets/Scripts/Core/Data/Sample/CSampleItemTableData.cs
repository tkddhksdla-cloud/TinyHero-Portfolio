using UnityEngine;

namespace TinyHero.Core.Data.Sample
{
    ///<summary>
    /// 샘플 아이템 테이블 데이터 데이터
    ///</summary>
    [CreateAssetMenu( fileName = "SampleItemTableData", menuName = "TinyHero/Data/Sample Item Table Data" )]
    public sealed class CSampleItemTableData : CExcelTableData<CSampleItemRow>
    {
    }
}


