using System;
using System.Collections.Generic;

namespace TinyHero.Maps
{
    [Serializable]
    public sealed class CMapToolSaveData
    {
        public string mapId;
        public string mapName;
        public string backgroundSpriteName;
        public List<CMapToolPortalSaveData> portals = new List<CMapToolPortalSaveData>();
        public List<CMapToolMonsterSaveData> monsters = new List<CMapToolMonsterSaveData>();
    }

    [Serializable]
    public sealed class CMapToolPortalSaveData
    {
        public string prefabName;
        public string resourcePath;
        public string portalId;
        public string targetMapId;
        public string targetPortalId;
        public CMapToolTransformData transform;
    }

    [Serializable]
    public sealed class CMapToolMonsterSaveData
    {
        public string prefabName;
        public string resourcePath;
        public CMapToolTransformData transform;
    }

    [Serializable]
    public sealed class CMapToolTransformData
    {
        public float[] position = new float[ 3 ];
        public float[] rotation = new float[ 3 ];
        public float[] scale = new float[ 3 ];
    }
}


