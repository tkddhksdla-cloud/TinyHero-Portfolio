using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 이펙트 생성 보조 유틸리티
    ///</summary>
    public static class CSkillVfxUtility
    {
        ///<summary>
        /// 스킬 시전 이펙트 생성
        ///</summary>
        public static CSkillPooledVfxHandle PlayCastVfx( CSkillContext _skillContext )
        {
            if ( _skillContext == null )
            {
                return null;
            }

            CSkillDefinition skillDefinition = _skillContext.GetSkillDefinition();
            Transform ownerTransform = _skillContext.GetOwnerTransform();

            if ( skillDefinition == null || ownerTransform == null )
            {
                return null;
            }

            Vector3 castOffset = skillDefinition.GetCastVfxOffset();
            Vector3 worldPosition = ResolveWorldPosition( ownerTransform, castOffset );
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetCastVfxPrefab(), worldPosition, null, skillDefinition.GetCastVfxReturnDelay() );
            ApplyRangeScale( _skillContext, result );
            return result;
        }

        ///<summary>
        /// 스킬 타격 이펙트 생성
        ///</summary>
        public static CSkillPooledVfxHandle PlayHitVfx( CSkillContext _skillContext, Transform _targetTransform )
        {
            if ( _skillContext == null || _targetTransform == null )
            {
                return null;
            }

            CSkillDefinition skillDefinition = _skillContext.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return null;
            }

            Vector3 worldPosition = ResolveWorldPosition( _targetTransform, skillDefinition.GetHitVfxOffset() );
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetHitVfxPrefab(), worldPosition, null, skillDefinition.GetHitVfxReturnDelay() );
            return result;
        }

        ///<summary>
        /// 스킬 투사체 이펙트 생성
        ///</summary>
        public static CSkillPooledVfxHandle PlayProjectileVfx( CSkillContext _skillContext, Vector3 _worldPosition, float _facingDirection )
        {
            if ( _skillContext == null )
            {
                return null;
            }

            CSkillDefinition skillDefinition = _skillContext.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return null;
            }

            Vector3 resolvedOffset = skillDefinition.GetProjectileVfxOffset();
            resolvedOffset.x *= _facingDirection;
            Vector3 spawnPosition = _worldPosition + resolvedOffset;
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetProjectileVfxPrefab(), spawnPosition, null, skillDefinition.GetProjectileVfxReturnDelay() );

            if ( result == null )
            {
                return null;
            }

            GameObject spawnedObject = result.GetSpawnedObject();

            if ( spawnedObject == null )
            {
                return result;
            }

            Transform spawnedTransform = spawnedObject.transform;
            ApplyRangeScale( _skillContext, spawnedTransform );
            Vector3 localScale = spawnedTransform.localScale;
            localScale.x = Mathf.Abs( localScale.x ) * ( _facingDirection < 0.0f ? -1.0f : 1.0f );
            spawnedTransform.localScale = localScale;
            return result;
        }

        ///<summary>
        /// 스킬 지속 이펙트 생성
        ///</summary>
        public static CSkillPooledVfxHandle PlayLoopVfx( CSkillContext _skillContext, Transform _anchorTransform, float _overrideReturnDelay )
        {
            if ( _skillContext == null || _anchorTransform == null )
            {
                return null;
            }

            CSkillDefinition skillDefinition = _skillContext.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return null;
            }

            Vector3 loopOffset = skillDefinition.GetLoopVfxOffset();
            Vector3 worldPosition = ResolveWorldPosition( _anchorTransform, loopOffset );
            float returnDelay = _overrideReturnDelay > 0.0f ? _overrideReturnDelay : skillDefinition.GetLoopVfxReturnDelay();
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetLoopVfxPrefab(), worldPosition, _anchorTransform, returnDelay );
            ApplyRangeScale( _skillContext, result );
            return result;
        }

        ///<summary>
        /// 범위 배율 기반 이펙트 크기 적용
        ///</summary>
        private static void ApplyRangeScale( CSkillContext _skillContext, CSkillPooledVfxHandle _vfxHandle )
        {
            if ( _vfxHandle == null )
            {
                return;
            }

            GameObject spawnedObject = _vfxHandle.GetSpawnedObject();

            if ( spawnedObject == null )
            {
                return;
            }

            ApplyRangeScale( _skillContext, spawnedObject.transform );
        }

        ///<summary>
        /// 범위 배율 기반 트랜스폼 크기 적용
        ///</summary>
        private static void ApplyRangeScale( CSkillContext _skillContext, Transform _targetTransform )
        {
            if ( _skillContext == null || _targetTransform == null )
            {
                return;
            }

            float rangeMultiplier = Mathf.Max( 0.1f, _skillContext.GetRangeMultiplier() );
            Vector3 localScale = _targetTransform.localScale;
            localScale.x *= rangeMultiplier;
            localScale.y *= rangeMultiplier;
            localScale.z *= rangeMultiplier;
            _targetTransform.localScale = localScale;
        }

        ///<summary>
        /// 기준 트랜스폼 기반 월드 위치 계산
        ///</summary>
        private static Vector3 ResolveWorldPosition( Transform _referenceTransform, Vector3 _offset )
        {
            if ( _referenceTransform == null )
            {
                return _offset;
            }

            float facingDirection = _referenceTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            Vector3 resolvedOffset = _offset;
            resolvedOffset.x *= facingDirection;
            Vector3 worldPosition = _referenceTransform.position + resolvedOffset;
            return worldPosition;
        }
    }
}
