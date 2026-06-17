using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 설치형 스킬 지속 영역 처리 컴포넌트
    ///</summary>
    public sealed class CPlacedSkillAreaRuntime : MonoBehaviour
    {
        private const int DefaultOverlapBufferSize = 24;

        private readonly Collider2D[] overlapBuffer = new Collider2D[ DefaultOverlapBufferSize ];
        private readonly HashSet<int> processedMonsterInstanceIdSet = new HashSet<int>();

        private CSkillContext skillContext;
        private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
        private float durationSeconds;
        private float damageStartDelaySeconds;
        private float tickIntervalSeconds;
        private float areaRadius;
        private float damageMultiplier;
        private int flatDamageBonus;
        private int maxTargetCount;
        private float elapsedTime;
        private float tickElapsedTime;
        private CSkillPooledVfxHandle loopVfxHandle;

        ///<summary>
        /// 활성 설치형 스킬 영역 일괄 정리
        ///</summary>
        public static void ReleaseAllActivePlacedSkillAreas()
        {
            CPlacedSkillAreaRuntime[] activePlacedSkillAreaRuntimes = FindObjectsByType<CPlacedSkillAreaRuntime>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int activeRuntimeCount = activePlacedSkillAreaRuntimes.Length;

            for ( int index = 0; index < activeRuntimeCount; index++ )
            {
                CPlacedSkillAreaRuntime placedSkillAreaRuntime = activePlacedSkillAreaRuntimes[ index ];

                if ( placedSkillAreaRuntime == null )
                {
                    continue;
                }

                placedSkillAreaRuntime.ReleaseRuntimeObject();
            }
        }

        ///<summary>
        /// 설치형 스킬 효과 데이터 초기화
        ///</summary>
        public void Initialize( CSkillContext _skillContext, float _durationSeconds, float _damageStartDelaySeconds, float _tickIntervalSeconds, float _areaRadius, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount, List<CEnemyDebuffEffectBase> _debuffEffectList )
        {
            skillContext = _skillContext;
            durationSeconds = Mathf.Max( 0.01f, _durationSeconds );
            damageStartDelaySeconds = Mathf.Clamp( _damageStartDelaySeconds, 0.0f, durationSeconds );
            tickIntervalSeconds = Mathf.Max( 0.01f, _tickIntervalSeconds );
            areaRadius = Mathf.Max( 0.1f, _areaRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            maxTargetCount = Mathf.Max( 1, _maxTargetCount );
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
            elapsedTime = 0.0f;
            tickElapsedTime = -damageStartDelaySeconds;
            loopVfxHandle = CSkillVfxUtility.PlayLoopVfx( skillContext, transform, durationSeconds );
        }

        ///<summary>
        /// 설치형 스킬 런타임 오브젝트 정리
        ///</summary>
        public void ReleaseRuntimeObject()
        {
            ReturnLoopVfx();
            Destroy( gameObject );
        }

        ///<summary>
        /// 설치형 스킬 지속 처리
        ///</summary>
        private void Update()
        {
            elapsedTime += Time.deltaTime;
            tickElapsedTime += Time.deltaTime;

            if ( tickElapsedTime >= tickIntervalSeconds )
            {
                tickElapsedTime = 0.0f;
                ApplyAreaTick();
            }

            if ( elapsedTime < durationSeconds )
            {
                return;
            }

            ReleaseRuntimeObject();
        }

        ///<summary>
        /// 설치형 스킬 주기 효과 적용
        ///</summary>
        private void ApplyAreaTick()
        {
            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            Vector2 areaCenter = transform.position;
            int hitCount = Physics2D.OverlapCircle( areaCenter, areaRadius, contactFilter, overlapBuffer );
            ApplyDamageToTargets( hitCount );
        }

        ///<summary>
        /// 몬스터 전용 충돌 필터 생성
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
        /// 범위 내 대상 피해 및 디버프 적용
        ///</summary>
        private void ApplyDamageToTargets( int _hitCount )
        {
            if ( skillContext == null )
            {
                return;
            }

            int processedTargetCount = 0;
            processedMonsterInstanceIdSet.Clear();

            for ( int index = 0; index < _hitCount; index++ )
            {
                if ( processedTargetCount >= maxTargetCount )
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

                bool wasAliveBeforeHit = monsterObject.GetCurrentHp() > 0;
                long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( skillContext, monsterObject, damageMultiplier, flatDamageBonus );
                monsterObject.TakeDamage( damage );
                CSkillVfxUtility.PlayHitVfx( skillContext, monsterObject.transform );
                ApplyDebuffs( monsterObject );
                CSkillDamageUtility.TryAwardMonsterExp( skillContext, monsterObject, wasAliveBeforeHit );
                processedTargetCount++;
            }
        }

        ///<summary>
        /// 설치형 스킬 루프 이펙트 반환
        ///</summary>
        private void ReturnLoopVfx()
        {
            if ( loopVfxHandle == null )
            {
                return;
            }

            loopVfxHandle.ForceReturn();
            loopVfxHandle = null;
        }

        ///<summary>
        /// 적 디버프 효과 적용
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

        ///<summary>
        /// 파괴 시 루프 이펙트 정리
        ///</summary>
        private void OnDestroy()
        {
            ReturnLoopVfx();
        }
    }
}
