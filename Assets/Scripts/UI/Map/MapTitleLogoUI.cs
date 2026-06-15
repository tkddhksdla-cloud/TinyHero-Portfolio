using TMPro;
using TinyHero.Maps;
using UnityEngine;

///<summary>
/// 현재 맵 이름 표시 UI 컴포넌트
///</summary>
public class MapTitleLogoUI : CAutoPoolReturnObject
{
    [SerializeField] private TextMeshProUGUI textMapName;

    ///<summary>
    /// 활성화 시 맵 이름 갱신
    ///</summary>
    protected override void OnAutoReturnObjectEnabled()
    {
        ResolveTextMapName();
        ApplyCurrentMapName();
    }

    ///<summary>
    /// 맵 이름 텍스트 참조 결정
    ///</summary>
    private void ResolveTextMapName()
    {
        if ( textMapName != null )
        {
            return;
        }

        TextMeshProUGUI resolvedTextMapName = GetComponentInChildren<TextMeshProUGUI>( true );
        textMapName = resolvedTextMapName;
    }

    ///<summary>
    /// 현재 맵 이름 텍스트 반영
    ///</summary>
    private void ApplyCurrentMapName()
    {
        if ( textMapName == null )
        {
            return;
        }

        if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
        {
            return;
        }

        string currentMapName = mapManager.GetCurrentMapName();
        textMapName.text = currentMapName;
    }
}
