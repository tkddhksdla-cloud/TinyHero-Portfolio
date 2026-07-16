using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Maps
{
    /// <summary>
    /// 맵 런타임과 맵 제작 툴이 공유하는 배경 및 캐릭터 프리팹 카탈로그입니다.
    /// </summary>
    public sealed class CMapRuntimeAssetCatalog
    {
        public const string BackgroundSpriteResourceFolderPath = "RawImages/BG";
        public const string MonsterPrefabResourceFolderPath = "Prefabs/Character/Monster";
        public const string NpcPrefabResourceFolderPath = "Prefabs/Character/NPC";

        private readonly List<Sprite> backgroundSpriteList = new List<Sprite>();
        private readonly List<GameObject> monsterPrefabList = new List<GameObject>();
        private readonly List<GameObject> npcPrefabList = new List<GameObject>();
        private readonly Dictionary<string, Sprite> backgroundSpriteByName = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, GameObject> monsterPrefabByName = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> npcPrefabByName = new Dictionary<string, GameObject>();

        public void Reload()
        {
            ReloadBackgroundSprites();
            ReloadMonsterPrefabs();
            ReloadNpcPrefabs();
        }

        public IReadOnlyList<Sprite> GetBackgroundSpriteList()
        {
            IReadOnlyList<Sprite> result = backgroundSpriteList;
            return result;
        }

        public IReadOnlyList<GameObject> GetMonsterPrefabList()
        {
            IReadOnlyList<GameObject> result = monsterPrefabList;
            return result;
        }

        public IReadOnlyList<GameObject> GetNpcPrefabList()
        {
            IReadOnlyList<GameObject> result = npcPrefabList;
            return result;
        }

        public bool TryGetBackgroundSprite( string _spriteName, out Sprite _backgroundSprite )
        {
            string normalizedSpriteName = CGameUtils.NormalizeId( _spriteName );
            bool result = backgroundSpriteByName.TryGetValue( normalizedSpriteName, out _backgroundSprite );
            return result;
        }

        public bool TryGetMonsterPrefab( string _prefabName, out GameObject _monsterPrefab )
        {
            string normalizedPrefabName = CGameUtils.NormalizeId( _prefabName );
            bool result = monsterPrefabByName.TryGetValue( normalizedPrefabName, out _monsterPrefab );
            return result;
        }

        public bool TryGetNpcPrefab( string _prefabName, out GameObject _npcPrefab )
        {
            string normalizedPrefabName = CGameUtils.NormalizeId( _prefabName );
            bool result = npcPrefabByName.TryGetValue( normalizedPrefabName, out _npcPrefab );
            return result;
        }

        public void CacheMonsterPrefab( string _prefabName, GameObject _monsterPrefab )
        {
            CachePrefab( _prefabName, _monsterPrefab, monsterPrefabList, monsterPrefabByName );
        }

        public void CacheNpcPrefab( string _prefabName, GameObject _npcPrefab )
        {
            CachePrefab( _prefabName, _npcPrefab, npcPrefabList, npcPrefabByName );
        }

        private void ReloadBackgroundSprites()
        {
            backgroundSpriteList.Clear();
            backgroundSpriteByName.Clear();
            Sprite[] loadedBackgroundSpriteArray = Resources.LoadAll<Sprite>( BackgroundSpriteResourceFolderPath );

            for ( int index = 0; index < loadedBackgroundSpriteArray.Length; index++ )
            {
                Sprite backgroundSprite = loadedBackgroundSpriteArray[ index ];

                if ( backgroundSprite == null )
                {
                    continue;
                }

                backgroundSpriteList.Add( backgroundSprite );
                backgroundSpriteByName[ backgroundSprite.name ] = backgroundSprite;
            }
        }

        private void ReloadMonsterPrefabs()
        {
            monsterPrefabList.Clear();
            monsterPrefabByName.Clear();
            GameObject[] loadedMonsterPrefabArray = Resources.LoadAll<GameObject>( MonsterPrefabResourceFolderPath );

            for ( int index = 0; index < loadedMonsterPrefabArray.Length; index++ )
            {
                GameObject monsterPrefab = loadedMonsterPrefabArray[ index ];
                CachePrefab( monsterPrefab != null ? monsterPrefab.name : string.Empty, monsterPrefab, monsterPrefabList, monsterPrefabByName );
            }
        }

        private void ReloadNpcPrefabs()
        {
            npcPrefabList.Clear();
            npcPrefabByName.Clear();
            GameObject[] loadedNpcPrefabArray = Resources.LoadAll<GameObject>( NpcPrefabResourceFolderPath );

            for ( int index = 0; index < loadedNpcPrefabArray.Length; index++ )
            {
                GameObject npcPrefab = loadedNpcPrefabArray[ index ];
                CachePrefab( npcPrefab != null ? npcPrefab.name : string.Empty, npcPrefab, npcPrefabList, npcPrefabByName );
            }
        }

        private void CachePrefab( string _prefabName, GameObject _prefab, List<GameObject> _prefabList, Dictionary<string, GameObject> _prefabByName )
        {
            if ( _prefab == null )
            {
                return;
            }

            if ( _prefabList.Contains( _prefab ) == false )
            {
                _prefabList.Add( _prefab );
            }

            string normalizedPrefabName = CGameUtils.NormalizeId( _prefabName );

            if ( string.IsNullOrWhiteSpace( normalizedPrefabName ) == false )
            {
                _prefabByName[ normalizedPrefabName ] = _prefab;
            }

            string normalizedAssetName = CGameUtils.NormalizeId( _prefab.name );

            if ( string.IsNullOrWhiteSpace( normalizedAssetName ) == false )
            {
                _prefabByName[ normalizedAssetName ] = _prefab;
            }
        }
    }
}
