using UnityEngine;

namespace TinyHero.Maps
{
    ///<summary>
    /// 맵 툴 배치 오브젝트 오브젝트
    ///</summary>
    public sealed class MapToolPlacedObject : MonoBehaviour
    {
        public enum eMapToolPlacedObjectType
        {
            MONSTER,
            PORTAL,
            NPC
        }

        [SerializeField] private eMapToolPlacedObjectType placedObjectType;
        [SerializeField] private string prefabName;
        [SerializeField] private string resourcePath;
        [SerializeField] private string portalId;
        [SerializeField] private string targetMapId;
        [SerializeField] private string targetPortalId;

        ///<summary>
        /// 배치 오브젝트 타입 반환
        ///</summary>
        public eMapToolPlacedObjectType GetPlacedObjectType()
        {
            eMapToolPlacedObjectType result = placedObjectType;
            return result;
        }

        ///<summary>
        /// 프리팹 이름 반환
        ///</summary>
        public string GetPrefabName()
        {
            string result = prefabName;
            return result;
        }

        ///<summary>
        /// 리소스 경로 반환
        ///</summary>
        public string GetResourcePath()
        {
            string result = resourcePath;
            return result;
        }

        ///<summary>
        /// 포탈 ID 반환
        ///</summary>
        public string GetPortalId()
        {
            string result = portalId;
            return result;
        }

        ///<summary>
        /// 대상 맵 ID 반환
        ///</summary>
        public string GetTargetMapId()
        {
            string result = targetMapId;
            return result;
        }

        ///<summary>
        /// 대상 포탈 ID 반환
        ///</summary>
        public string GetTargetPortalId()
        {
            string result = targetPortalId;
            return result;
        }

        ///<summary>
        /// 몬스터 배치 정보 설정
        ///</summary>
        public void SetupMonster(string _assignedPrefabName, string _assignedResourcePath)
        {
            placedObjectType = eMapToolPlacedObjectType.MONSTER;
            prefabName = _assignedPrefabName;
            resourcePath = _assignedResourcePath;
            portalId = string.Empty;
            targetMapId = string.Empty;
            targetPortalId = string.Empty;
        }

        ///<summary>
        /// 포탈 배치 정보 설정
        ///</summary>
        public void SetupPortal(string _assignedPrefabName, string _assignedResourcePath, string _assignedPortalId, string _assignedTargetMapId, string _assignedTargetPortalId)
        {
            placedObjectType = eMapToolPlacedObjectType.PORTAL;
            prefabName = _assignedPrefabName;
            resourcePath = _assignedResourcePath;
            portalId = _assignedPortalId;
            targetMapId = _assignedTargetMapId;
            targetPortalId = _assignedTargetPortalId;
        }

        ///<summary>
        /// NPC 배치 정보 설정
        ///</summary>
        public void SetupNpc( string _assignedPrefabName, string _assignedResourcePath )
        {
            placedObjectType = eMapToolPlacedObjectType.NPC;
            prefabName = _assignedPrefabName;
            resourcePath = _assignedResourcePath;
            portalId = string.Empty;
            targetMapId = string.Empty;
            targetPortalId = string.Empty;
        }
    }
}


