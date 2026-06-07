using UnityEngine;

public class PortalObject : MonoBehaviour
{
    [SerializeField] private string targetSceneID;

    ///<summary>
    /// 포탈의 목표 맵 ID를 설정한다.
    ///</summary>
    public void SetTargetSceneID( string assignedTargetSceneID )
    {
        targetSceneID = assignedTargetSceneID;
    }

    ///<summary>
    /// 포탈의 목표 맵 ID를 반환한다.
    ///</summary>
    public string GetTargetSceneID()
    {
        string result = targetSceneID;
        return result;
    }
}
