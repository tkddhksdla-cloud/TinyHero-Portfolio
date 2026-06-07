using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴이 배치한 런타임 오브젝트의 저장 메타데이터를 보관한다.
    ///</summary>
    public sealed class MapToolPlacedObject : MonoBehaviour
    {
        public enum eMapToolPlacedObjectType
        {
            MONSTER,
            PORTAL
        }

        [SerializeField] private eMapToolPlacedObjectType placedObjectType;
        [SerializeField] private string prefabName;
        [SerializeField] private string resourcePath;
        [SerializeField] private string targetMapId;

        ///<summary>
        /// 배치 오브젝트 종류를 반환한다.
        ///</summary>
        public eMapToolPlacedObjectType GetPlacedObjectType()
        {
            eMapToolPlacedObjectType result = placedObjectType;
            return result;
        }

        ///<summary>
        /// 저장 대상 프리팹 이름을 반환한다.
        ///</summary>
        public string GetPrefabName()
        {
            string result = prefabName;
            return result;
        }

        ///<summary>
        /// 저장 대상 리소스 경로를 반환한다.
        ///</summary>
        public string GetResourcePath()
        {
            string result = resourcePath;
            return result;
        }

        ///<summary>
        /// 포탈 목표 맵 ID를 반환한다.
        ///</summary>
        public string GetTargetMapId()
        {
            string result = targetMapId;
            return result;
        }

        ///<summary>
        /// 몬스터 배치 메타데이터를 설정한다.
        ///</summary>
        public void SetupMonster( string assignedPrefabName, string assignedResourcePath )
        {
            placedObjectType = eMapToolPlacedObjectType.MONSTER;
            prefabName = assignedPrefabName;
            resourcePath = assignedResourcePath;
            targetMapId = string.Empty;
        }

        ///<summary>
        /// 포탈 배치 메타데이터를 설정한다.
        ///</summary>
        public void SetupPortal( string assignedPrefabName, string assignedResourcePath, string assignedTargetMapId )
        {
            placedObjectType = eMapToolPlacedObjectType.PORTAL;
            prefabName = assignedPrefabName;
            resourcePath = assignedResourcePath;
            targetMapId = assignedTargetMapId;
        }
    }
}
