using NUnit.Framework;
using TinyHero.Core;
using TinyHero.Skill;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 스킬 이펙트 풀 활성화 순서 검증
    ///</summary>
    public sealed class SkillVfxPoolManagerTests
    {
        private GameObject objectPoolManagerObject;
        private GameObject effectPrefabObject;

        ///<summary>
        /// 테스트용 풀 매니저와 이펙트 프리팹 생성
        ///</summary>
        [SetUp]
        public void SetUp()
        {
            objectPoolManagerObject = new GameObject( "SkillVfxPoolManagerTests_CObjectPoolManager" );
            objectPoolManagerObject.AddComponent<CObjectPoolManager>();

            effectPrefabObject = new GameObject( "SkillVfxPoolManagerTests_EffectPrefab" );
            effectPrefabObject.SetActive( false );

            ParticleSystem particleSystem = effectPrefabObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.duration = 1.0f;
            mainModule.loop = true;
            mainModule.prewarm = true;
            mainModule.startLifetime = 5.0f;
            mainModule.startSpeed = 0.0f;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.rateOverTime = 10.0f;

            ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
            shapeModule.enabled = false;
        }

        ///<summary>
        /// 테스트 오브젝트 정리
        ///</summary>
        [TearDown]
        public void TearDown()
        {
            if ( effectPrefabObject != null )
            {
                Object.DestroyImmediate( effectPrefabObject );
            }

            if ( objectPoolManagerObject != null )
            {
                Object.DestroyImmediate( objectPoolManagerObject );
            }
        }

        ///<summary>
        /// 최초 생성 이펙트가 시전 위치 설정 후 활성화되는지 검증
        ///</summary>
        [Test]
        public void Spawn_FirstInstance_ActivatesAfterWorldPositionIsApplied()
        {
            Vector3 spawnPosition = new Vector3( 12.0f, 7.0f, 0.0f );
            CSkillPooledVfxHandle pooledVfxHandle = CSkillVfxPoolManager.Spawn( effectPrefabObject, spawnPosition, null, 3.0f );

            Assert.IsNotNull( pooledVfxHandle, "최초 스킬 FX 풀 대여 핸들이 생성되어야 한다." );

            GameObject spawnedObject = pooledVfxHandle.GetSpawnedObject();
            Assert.IsNotNull( spawnedObject, "풀 대여 핸들이 활성 FX 오브젝트를 보유해야 한다." );

            ParticleSystem particleSystem = spawnedObject.GetComponent<ParticleSystem>();
            Assert.IsNotNull( particleSystem, "풀에서 생성된 FX가 원본 파티클 시스템을 유지해야 한다." );
            Assert.Greater( particleSystem.particleCount, 0 );

            ParticleSystem.Particle[] particleArray = new ParticleSystem.Particle[ particleSystem.particleCount ];
            int particleCount = particleSystem.GetParticles( particleArray );
            Assert.Greater( particleCount, 0 );

            ParticleSystem.Particle firstParticle = particleArray[ 0 ];
            float distanceFromSpawnPosition = Vector3.Distance( spawnPosition, firstParticle.position );
            Assert.Less( distanceFromSpawnPosition, 0.01f );

            pooledVfxHandle.ForceReturn();
        }
    }
}
