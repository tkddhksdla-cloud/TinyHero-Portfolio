using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 이펙트 풀링 관리자
    ///</summary>
    public static class CSkillVfxPoolManager
    {
        private sealed class CPooledVfxEntry
        {
            public GameObject Prefab;
            public CObjectPool<GameObject> Pool;
        }

        private static readonly Dictionary<int, CPooledVfxEntry> pooledVfxEntryByPrefabId = new Dictionary<int, CPooledVfxEntry>();
        private static readonly Dictionary<int, CObjectPool<GameObject>> pooledVfxPoolByInstanceId = new Dictionary<int, CObjectPool<GameObject>>();

        ///<summary>
        /// 스킬 이펙트 생성 처리
        ///</summary>
        public static CSkillPooledVfxHandle Spawn( GameObject _prefab, Vector3 _worldPosition, Transform _parentTransform, float _returnDelaySeconds )
        {
            if ( _prefab == null )
            {
                return null;
            }

            CPooledVfxEntry pooledVfxEntry = GetOrCreatePoolEntry( _prefab );

            if ( pooledVfxEntry == null || pooledVfxEntry.Pool == null )
            {
                return null;
            }

            GameObject spawnedObject = pooledVfxEntry.Pool.Get();

            if ( spawnedObject == null )
            {
                return null;
            }

            Transform spawnedTransform = spawnedObject.transform;
            spawnedTransform.SetParent( _parentTransform, false );
            spawnedTransform.position = _worldPosition;

            Vector3 prefabScale = _prefab.transform.localScale;
            spawnedTransform.localScale = prefabScale;

            CAutoPoolReturnObject autoPoolReturnObject = spawnedObject.GetComponent<CAutoPoolReturnObject>();

            if ( autoPoolReturnObject != null )
            {
                autoPoolReturnObject.SetReturnDelay( _returnDelaySeconds );
            }

            CSkillPooledVfxHandle result = new CSkillPooledVfxHandle( spawnedObject, autoPoolReturnObject );
            return result;
        }

        ///<summary>
        /// 비지속성 활성 스킬 이펙트 일괄 정리
        ///</summary>
        public static void ReleaseAllTransientActiveVfx()
        {
            CAutoPoolReturnObject[] autoPoolReturnObjectArray = Object.FindObjectsByType<CAutoPoolReturnObject>( FindObjectsInactive.Include, FindObjectsSortMode.None );
            int objectCount = autoPoolReturnObjectArray.Length;

            for ( int index = 0; index < objectCount; index++ )
            {
                CAutoPoolReturnObject autoPoolReturnObject = autoPoolReturnObjectArray[ index ];

                if ( autoPoolReturnObject == null )
                {
                    continue;
                }

                GameObject targetObject = autoPoolReturnObject.gameObject;

                if ( targetObject.activeInHierarchy == false )
                {
                    continue;
                }

                Transform parentTransform = targetObject.transform.parent;

                if ( parentTransform != null )
                {
                    continue;
                }

                HandleAutoReturnObjectToPool( autoPoolReturnObject );
            }
        }

        ///<summary>
        /// 프리팹 기반 풀 엔트리 반환
        ///</summary>
        private static CPooledVfxEntry GetOrCreatePoolEntry( GameObject _prefab )
        {
            int prefabInstanceId = _prefab.GetInstanceID();
            bool hasEntry = pooledVfxEntryByPrefabId.TryGetValue( prefabInstanceId, out CPooledVfxEntry pooledVfxEntry );

            if ( hasEntry )
            {
                return pooledVfxEntry;
            }

            CPooledVfxEntry createdEntry = new CPooledVfxEntry();
            createdEntry.Prefab = _prefab;
            createdEntry.Pool = new CObjectPool<GameObject>(
                () => CreateInstance( _prefab ),
                _item => OnGetInstance( _item ),
                _item => OnReleaseInstance( _item ) );
            pooledVfxEntryByPrefabId.Add( prefabInstanceId, createdEntry );
            return createdEntry;
        }

        ///<summary>
        /// 이펙트 인스턴스 생성 처리
        ///</summary>
        private static GameObject CreateInstance( GameObject _prefab )
        {
            GameObject createdObject = Object.Instantiate( _prefab );
            createdObject.name = _prefab.name;
            CSkillRenderUtility.ApplyForegroundSorting( createdObject );
            createdObject.SetActive( false );
            CAutoPoolReturnObject autoPoolReturnObject = createdObject.GetComponent<CAutoPoolReturnObject>();

            if ( autoPoolReturnObject == null )
            {
                autoPoolReturnObject = createdObject.AddComponent<CAutoPoolReturnObject>();
            }

            int createdInstanceId = createdObject.GetInstanceID();

            if ( pooledVfxPoolByInstanceId.ContainsKey( createdInstanceId ) == false )
            {
                bool hasEntry = pooledVfxEntryByPrefabId.TryGetValue( _prefab.GetInstanceID(), out CPooledVfxEntry pooledVfxEntry );

                if ( hasEntry && pooledVfxEntry != null )
                {
                    pooledVfxPoolByInstanceId.Add( createdInstanceId, pooledVfxEntry.Pool );
                }
            }

            autoPoolReturnObject.SetReturnToPoolHandler( HandleAutoReturnObjectToPool );
            return createdObject;
        }

        ///<summary>
        /// 이펙트 인스턴스 대여 처리
        ///</summary>
        private static void OnGetInstance( GameObject _item )
        {
            if ( _item == null )
            {
                return;
            }

            ResetParticleSystems( _item );
            _item.SetActive( true );
            PlayParticleSystems( _item );
        }

        ///<summary>
        /// 이펙트 인스턴스 반환 처리
        ///</summary>
        private static void OnReleaseInstance( GameObject _item )
        {
            if ( _item == null )
            {
                return;
            }

            Transform itemTransform = _item.transform;
            itemTransform.SetParent( null, false );
            StopParticleSystems( _item );
            _item.SetActive( false );
        }

        ///<summary>
        /// 자동 반환 오브젝트 풀 반환 처리
        ///</summary>
        private static void HandleAutoReturnObjectToPool( CAutoPoolReturnObject _autoPoolReturnObject )
        {
            if ( _autoPoolReturnObject == null )
            {
                return;
            }

            int instanceId = _autoPoolReturnObject.gameObject.GetInstanceID();
            bool hasPool = pooledVfxPoolByInstanceId.TryGetValue( instanceId, out CObjectPool<GameObject> pooledVfxPool );

            if ( hasPool == false || pooledVfxPool == null )
            {
                _autoPoolReturnObject.gameObject.SetActive( false );
                return;
            }

            pooledVfxPool.Release( _autoPoolReturnObject.gameObject );
        }

        ///<summary>
        /// 파티클 시스템 정지 및 초기화
        ///</summary>
        private static void ResetParticleSystems( GameObject _item )
        {
            if ( _item == null )
            {
                return;
            }

            ParticleSystem[] particleSystemArray = _item.GetComponentsInChildren<ParticleSystem>( true );
            int particleSystemCount = particleSystemArray.Length;

            for ( int index = 0; index < particleSystemCount; index++ )
            {
                ParticleSystem particleSystem = particleSystemArray[ index ];

                if ( particleSystem == null )
                {
                    continue;
                }

                particleSystem.Stop( true, ParticleSystemStopBehavior.StopEmittingAndClear );
                particleSystem.Simulate( 0.0f, true, true );
            }
        }

        ///<summary>
        /// 파티클 시스템 재생 시작
        ///</summary>
        private static void PlayParticleSystems( GameObject _item )
        {
            if ( _item == null )
            {
                return;
            }

            ParticleSystem[] particleSystemArray = _item.GetComponentsInChildren<ParticleSystem>( true );
            int particleSystemCount = particleSystemArray.Length;

            for ( int index = 0; index < particleSystemCount; index++ )
            {
                ParticleSystem particleSystem = particleSystemArray[ index ];

                if ( particleSystem == null )
                {
                    continue;
                }

                particleSystem.Play( true );
            }
        }

        ///<summary>
        /// 파티클 시스템 재생 중단
        ///</summary>
        private static void StopParticleSystems( GameObject _item )
        {
            if ( _item == null )
            {
                return;
            }

            ParticleSystem[] particleSystemArray = _item.GetComponentsInChildren<ParticleSystem>( true );
            int particleSystemCount = particleSystemArray.Length;

            for ( int index = 0; index < particleSystemCount; index++ )
            {
                ParticleSystem particleSystem = particleSystemArray[ index ];

                if ( particleSystem == null )
                {
                    continue;
                }

                particleSystem.Stop( true, ParticleSystemStopBehavior.StopEmittingAndClear );
            }
        }
    }
}
