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
        public string bgmClipName;
        public bool hasCustomRightBoundary;
        public float customRightBoundaryX;
        public List<CMapToolPortalSaveData> portals = new List<CMapToolPortalSaveData>();
        public List<CMapToolMonsterSaveData> monsters = new List<CMapToolMonsterSaveData>();
        public List<CMapToolNpcSaveData> npcs = new List<CMapToolNpcSaveData>();
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
    public sealed class CMapToolNpcSaveData
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


