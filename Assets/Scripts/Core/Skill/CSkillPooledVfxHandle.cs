using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 풀링된 스킬 이펙트 핸들 데이터
    ///</summary>
    public sealed class CSkillPooledVfxHandle
    {
        private readonly GameObject spawnedObject;
        private readonly CAutoPoolReturnObject autoPoolReturnObject;

        ///<summary>
        /// 풀링된 스킬 이펙트 핸들 생성자
        ///</summary>
        public CSkillPooledVfxHandle( GameObject _spawnedObject, CAutoPoolReturnObject _autoPoolReturnObject )
        {
            spawnedObject = _spawnedObject;
            autoPoolReturnObject = _autoPoolReturnObject;
        }

        ///<summary>
        /// 이펙트 오브젝트 반환
        ///</summary>
        public GameObject GetSpawnedObject()
        {
            GameObject result = spawnedObject;
            return result;
        }

        ///<summary>
        /// 즉시 풀 반환 처리
        ///</summary>
        public void ForceReturn()
        {
            if ( autoPoolReturnObject != null )
            {
                autoPoolReturnObject.ForceReturnToPool();
                return;
            }

            if ( spawnedObject == null )
            {
                return;
            }

            spawnedObject.SetActive( false );
        }
    }
}
