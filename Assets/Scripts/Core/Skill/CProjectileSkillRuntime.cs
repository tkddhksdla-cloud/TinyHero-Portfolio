using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 발사체 스킬 런타임 처리 컴포넌트
    ///</summary>
    public sealed class CProjectileSkillRuntime : MonoBehaviour
    {
        private const int DefaultOverlapBufferSize = 16;

        private readonly Collider2D[] overlapBuffer = new Collider2D[ DefaultOverlapBufferSize ];
        private readonly HashSet<int> processedMonsterInstanceIdSet = new HashSet<int>();

        private Vector2 moveDirection = Vector2.right;
        private float collisionRadius;
        private float travelDistance;
        private float travelSpeed;
        private float damageMultiplier;
        private float traveledDistance;
        private int flatDamageBonus;
        private int simultaneousTargetCount = 1;
        private bool destroyOnFirstHit = true;
        private bool isInitialized;
        private float facingDirection = 1.0f;
        private CSkillContext skillContext;
        private CSkillPooledVfxHandle projectileVfxHandle;
        private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
        private List<CEnemyCrowdControlEffectBase> crowdControlEffectList = new List<CEnemyCrowdControlEffectBase>();

        ///<summary>
        /// 활성 투사체 스킬 런타임 일괄 정리
        ///</summary>
        public static void ReleaseAllActiveProjectileSkillRuntimes()
        {
            CProjectileSkillRuntime[] activeProjectileSkillRuntimeArray = FindObjectsByType<CProjectileSkillRuntime>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int runtimeCount = activeProjectileSkillRuntimeArray.Length;

            for ( int index = 0; index < runtimeCount; index++ )
            {
                CProjectileSkillRuntime projectileSkillRuntime = activeProjectileSkillRuntimeArray[ index ];

                if ( projectileSkillRuntime == null )
                {
                    continue;
                }

                Destroy( projectileSkillRuntime.gameObject );
            }
        }

        ///<summary>
        /// 발사체 런타임 초기화
        ///</summary>
        public void Initialize( CSkillContext _skillContext, Vector2 _moveDirection, float _collisionRadius, float _travelDistance, float _travelSpeed, float _damageMultiplier, int _flatDamageBonus, int _simultaneousTargetCount, bool _destroyOnFirstHit, List<CEnemyDebuffEffectBase> _debuffEffectList, List<CEnemyCrowdControlEffectBase> _crowdControlEffectList )
        {
            skillContext = _skillContext;
            moveDirection = _moveDirection.sqrMagnitude > 0.0f ? _moveDirection.normalized : Vector2.right;
            collisionRadius = Mathf.Max( 0.05f, _collisionRadius );
            travelDistance = Mathf.Max( collisionRadius, _travelDistance );
            travelSpeed = Mathf.Max( 0.01f, _travelSpeed );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            simultaneousTargetCount = Mathf.Max( 1, _simultaneousTargetCount );
            destroyOnFirstHit = _destroyOnFirstHit;
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
            crowdControlEffectList = _crowdControlEffectList != null ? _crowdControlEffectList : new List<CEnemyCrowdControlEffectBase>();
            traveledDistance = 0.0f;
            facingDirection = moveDirection.x < 0.0f ? -1.0f : 1.0f;
            isInitialized = true;
            projectileVfxHandle = CSkillVfxUtility.PlayProjectileVfx( skillContext, transform.position, facingDirection );
            RefreshProjectileVfxPosition();
            EvaluateCollision();
        }

        ///<summary>
        /// 프레임 이동 및 충돌 처리
        ///</summary>
        private void Update()
        {
            if ( isInitialized == false )
            {
                return;
            }

            float moveDistance = travelSpeed * Time.deltaTime;
            Vector3 movement = new Vector3( moveDirection.x * moveDistance, moveDirection.y * moveDistance, 0.0f );
            transform.position += movement;
            traveledDistance += moveDistance;
            RefreshProjectileVfxPosition();

            bool didDestroyOnHit = EvaluateCollision();

            if ( didDestroyOnHit )
            {
                return;
            }

            if ( traveledDistance >= travelDistance )
            {
                Destroy( gameObject );
            }
        }

        ///<summary>
        /// 종료 시 발사체 이펙트 반환
        ///</summary>
        private void OnDestroy()
        {
            if ( projectileVfxHandle == null )
            {
                return;
            }

            projectileVfxHandle.ForceReturn();
            projectileVfxHandle = null;
        }

        ///<summary>
        /// 발사체 충돌 판정 처리
        ///</summary>
        private bool EvaluateCollision()
        {
            Vector2 projectileCenter = transform.position;
            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            int hitCount = Physics2D.OverlapCircle( projectileCenter, collisionRadius, contactFilter, overlapBuffer );
            bool didDestroy = ApplyDamageToTargets( hitCount );
            return didDestroy;
        }

        ///<summary>
        /// 몬스터용 충돌 필터 생성
        ///</summary>
        private ContactFilter2D CreateMonsterContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.useTriggers = true;
            contactFilter.layerMask = LayerMask.GetMask( "Monster" );
            return contactFilter;
        }

        ///<summary>
        /// 범위 내 대상 피해 적용 처리
        ///</summary>
        private bool ApplyDamageToTargets( int _hitCount )
        {
            int appliedTargetCount = 0;

            for ( int index = 0; index < _hitCount; index++ )
            {
                if ( appliedTargetCount >= simultaneousTargetCount )
                {
                    break;
                }

                Collider2D overlapCollider = overlapBuffer[ index ];
                MonsterObject monsterObject = ResolveMonsterObject( overlapCollider );

                if ( monsterObject == null )
                {
                    continue;
                }

                int monsterInstanceId = monsterObject.GetInstanceID();

                if ( processedMonsterInstanceIdSet.Contains( monsterInstanceId ) )
                {
                    continue;
                }

                processedMonsterInstanceIdSet.Add( monsterInstanceId );

                if ( monsterObject.GetCurrentHp() <= 0 )
                {
                    continue;
                }

                bool isNewSingleHitExecution = skillContext == null || monsterObject.TryRegisterSingleHitSkillExecution( skillContext.GetExecutionId() );

                if ( isNewSingleHitExecution == false )
                {
                    continue;
                }

                bool wasAliveBeforeHit = monsterObject.GetCurrentHp() > 0;
                long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( skillContext, monsterObject, damageMultiplier, flatDamageBonus, out bool isCritical );
                monsterObject.TakeDamage( damage, isCritical );
                CSkillVfxUtility.PlayHitVfx( skillContext, monsterObject.transform );
                ApplyDebuffs( monsterObject );
                ApplyCrowdControls( monsterObject );
                CSkillDamageUtility.TryAwardMonsterExp( skillContext, monsterObject, wasAliveBeforeHit );
                appliedTargetCount++;

                if ( destroyOnFirstHit )
                {
                    Destroy( gameObject );
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 발사체 이펙트 위치 동기화
        ///</summary>
        private void RefreshProjectileVfxPosition()
        {
            if ( projectileVfxHandle == null || skillContext == null )
            {
                return;
            }

            GameObject projectileVfxObject = projectileVfxHandle.GetSpawnedObject();

            if ( projectileVfxObject == null )
            {
                return;
            }

            CSkillDefinition skillDefinition = skillContext.GetSkillDefinition();

            if ( skillDefinition == null )
            {
                return;
            }

            Vector3 offset = skillDefinition.GetProjectileVfxOffset();
            offset.x *= facingDirection;
            projectileVfxObject.transform.position = transform.position + offset;
        }

        ///<summary>
        /// 적 디버프 적용 처리
        ///</summary>
        private void ApplyDebuffs( MonsterObject _monsterObject )
        {
            for ( int index = 0; index < debuffEffectList.Count; index++ )
            {
                CEnemyDebuffEffectBase debuffEffect = debuffEffectList[ index ];

                if ( debuffEffect == null )
                {
                    continue;
                }

                debuffEffect.TryApply( skillContext, _monsterObject );
            }
        }

        ///<summary>
        /// 군중제어 효과 적용
        ///</summary>
        private void ApplyCrowdControls( MonsterObject _monsterObject )
        {
            for ( int index = 0; index < crowdControlEffectList.Count; index++ )
            {
                CEnemyCrowdControlEffectBase crowdControlEffect = crowdControlEffectList[ index ];

                if ( crowdControlEffect == null )
                {
                    continue;
                }

                crowdControlEffect.TryApply( skillContext, _monsterObject );
            }
        }

        ///<summary>
        /// 콜라이더 기반 몬스터 결정
        ///</summary>
        private MonsterObject ResolveMonsterObject( Collider2D _overlapCollider )
        {
            if ( _overlapCollider == null )
            {
                return null;
            }

            MonsterObject monsterObject = _overlapCollider.GetComponent<MonsterObject>();

            if ( monsterObject != null )
            {
                return monsterObject;
            }

            MonsterObject parentMonsterObject = _overlapCollider.GetComponentInParent<MonsterObject>();
            return parentMonsterObject;
        }
    }
}
