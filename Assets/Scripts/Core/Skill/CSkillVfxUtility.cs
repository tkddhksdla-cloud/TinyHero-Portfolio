using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 이펙트 재생 보조 유틸리티
    ///</summary>
    public static class CSkillVfxUtility
    {
        ///<summary>
        /// 스킬 시전 이펙트 재생
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

            Vector3 worldPosition = ResolveWorldPosition( ownerTransform, skillDefinition.GetCastVfxOffset() );
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetCastVfxPrefab(), worldPosition, null, skillDefinition.GetCastVfxReturnDelay() );
            return result;
        }

        ///<summary>
        /// 스킬 타격 이펙트 재생
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
        /// 스킬 지속 이펙트 재생
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

            Vector3 worldPosition = ResolveWorldPosition( _anchorTransform, skillDefinition.GetLoopVfxOffset() );
            float returnDelay = _overrideReturnDelay > 0.0f ? _overrideReturnDelay : skillDefinition.GetLoopVfxReturnDelay();
            CSkillPooledVfxHandle result = CSkillVfxPoolManager.Spawn( skillDefinition.GetLoopVfxPrefab(), worldPosition, _anchorTransform, returnDelay );
            return result;
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
