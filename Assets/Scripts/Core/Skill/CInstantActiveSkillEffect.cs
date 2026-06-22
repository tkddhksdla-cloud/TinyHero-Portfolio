using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 즉발 공격 액티브 스킬 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "InstantActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Instant" )]
    public sealed class CInstantActiveSkillEffect : CActiveSkillEffectBase, ISerializationCallbackReceiver
    {
        private const int DefaultOverlapBufferSize = 16;

        [SerializeField] private Vector2 areaOffset;
        [SerializeField] private float areaRadius = 1.25f;
        [SerializeField] private float damageMultiplier = 1.5f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] [HideInInspector] private int maxTargetCount = DefaultSimultaneousTargetCount;
        [SerializeField] private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();

        private readonly Collider2D[] overlapBuffer = new Collider2D[ DefaultOverlapBufferSize ];
        private readonly HashSet<int> processedMonsterInstanceIdSet = new HashSet<int>();

        ///<summary>
        /// 즉발 공격 효과 데이터 구성
        ///</summary>
        public void Configure( Vector2 _areaOffset, float _areaRadius, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount )
        {
            areaOffset = _areaOffset;
            areaRadius = Mathf.Max( 0.1f, _areaRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            SetSimultaneousTargetCount( _maxTargetCount );
            maxTargetCount = GetSimultaneousTargetCount();
        }

        ///<summary>
        /// 기본 데미지 배율 반환
        ///</summary>
        public float GetDamageMultiplier()
        {
            float result = Mathf.Max( 0.0f, damageMultiplier );
            return result;
        }

        ///<summary>
        /// 디버프 효과 목록 설정
        ///</summary>
        public void SetDebuffEffects( List<CEnemyDebuffEffectBase> _debuffEffectList )
        {
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
        }

        ///<summary>
        /// 디버프 효과 목록 반환
        ///</summary>
        public List<CEnemyDebuffEffectBase> GetDebuffEffects()
        {
            List<CEnemyDebuffEffectBase> result = debuffEffectList;
            return result;
        }

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.INSTANT;
            return result;
        }

        ///<summary>
        /// 툴 미리보기 범위 데이터 반환
        ///</summary>
        public override bool TryGetToolRangePreviewData( Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _ownerTransform == null )
            {
                return false;
            }

            Vector2 attackCenter = ResolveAttackCenter( _ownerTransform );
            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = new Vector3( attackCenter.x, attackCenter.y, _ownerTransform.position.z );
            _previewData.radius = areaRadius;
            return true;
        }

        ///<summary>
        /// 스킬 실행 처리
        ///</summary>
        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            CSkillVfxUtility.PlayCastVfx( _skillContext );
            float damageStartDelaySeconds = GetDamageStartDelaySeconds();

            if ( damageStartDelaySeconds <= 0.0f )
            {
                ApplyDelayedDamage( _skillContext );
                return true;
            }

            CSkillManager skillManager = _skillContext.GetSkillManager();

            if ( skillManager == null )
            {
                return false;
            }

            skillManager.StartCoroutine( IE_ApplyDelayedDamage( _skillContext, damageStartDelaySeconds ) );
            return true;
        }

        ///<summary>
        /// 직렬화 이전 대상 수 동기화
        ///</summary>
        public void OnBeforeSerialize()
        {
            maxTargetCount = GetSimultaneousTargetCount();
        }

        ///<summary>
        /// 역직렬화 이후 대상 수 마이그레이션
        ///</summary>
        public void OnAfterDeserialize()
        {
            SyncLegacyTargetCount();
        }

        ///<summary>
        /// 지연 즉발 데미지 적용 코루틴
        ///</summary>
        private IEnumerator IE_ApplyDelayedDamage( CSkillContext _skillContext, float _damageStartDelaySeconds )
        {
            yield return new WaitForSeconds( _damageStartDelaySeconds );
            ApplyDelayedDamage( _skillContext );
        }

        ///<summary>
        /// 즉발 데미지 실제 적용
        ///</summary>
        private bool ApplyDelayedDamage( CSkillContext _skillContext )
        {
            if ( _skillContext == null )
            {
                return false;
            }

            Transform ownerTransform = _skillContext.GetOwnerTransform();

            if ( ownerTransform == null )
            {
                return false;
            }

            Vector2 attackCenter = ResolveAttackCenter( ownerTransform );
            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            int hitCount = Physics2D.OverlapCircle( attackCenter, areaRadius, contactFilter, overlapBuffer );
            bool didHitAnyTarget = ApplyDamageToTargets( _skillContext, hitCount );
            return didHitAnyTarget;
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
        /// 공격 중심 좌표 계산
        ///</summary>
        private Vector2 ResolveAttackCenter( Transform _ownerTransform )
        {
            float facingDirection = ResolveFacingDirection( _ownerTransform );
            Vector2 resolvedOffset = areaOffset;
            resolvedOffset.x *= facingDirection;
            Vector2 ownerPosition = _ownerTransform.position;
            Vector2 attackCenter = ownerPosition + resolvedOffset;
            return attackCenter;
        }

        ///<summary>
        /// 바라보기 방향 계산
        ///</summary>
        private float ResolveFacingDirection( Transform _ownerTransform )
        {
            if ( _ownerTransform == null )
            {
                return 1.0f;
            }

            float scaleX = _ownerTransform.localScale.x;
            float result = scaleX < 0.0f ? -1.0f : 1.0f;
            return result;
        }

        ///<summary>
        /// 범위 내 대상 피해 및 디버프 적용
        ///</summary>
        private bool ApplyDamageToTargets( CSkillContext _skillContext, int _hitCount )
        {
            bool didHitAnyTarget = false;
            int processedTargetCount = 0;
            int simultaneousTargetCount = GetSimultaneousTargetCount();
            processedMonsterInstanceIdSet.Clear();

            for ( int index = 0; index < _hitCount; index++ )
            {
                if ( processedTargetCount >= simultaneousTargetCount )
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
                long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( _skillContext, monsterObject, damageMultiplier, flatDamageBonus, out bool isCritical );
                monsterObject.TakeDamage( damage, isCritical );
                CSkillVfxUtility.PlayHitVfx( _skillContext, monsterObject.transform );
                ApplyDebuffs( _skillContext, monsterObject );
                CSkillDamageUtility.TryAwardMonsterExp( _skillContext, monsterObject, wasAliveBeforeHit );
                processedTargetCount++;
                didHitAnyTarget = true;
            }

            return didHitAnyTarget;
        }

        ///<summary>
        /// 디버프 효과 적용
        ///</summary>
        private void ApplyDebuffs( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            for ( int index = 0; index < debuffEffectList.Count; index++ )
            {
                CEnemyDebuffEffectBase debuffEffect = debuffEffectList[ index ];

                if ( debuffEffect == null )
                {
                    continue;
                }

                debuffEffect.TryApply( _skillContext, _monsterObject );
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
        /// 레거시 대상 수 데이터 동기화
        ///</summary>
        private void SyncLegacyTargetCount()
        {
            int configuredTargetCount = GetConfiguredSimultaneousTargetCount();

            if ( configuredTargetCount == DefaultSimultaneousTargetCount && maxTargetCount != DefaultSimultaneousTargetCount )
            {
                SetSimultaneousTargetCount( maxTargetCount );
                return;
            }

            maxTargetCount = GetSimultaneousTargetCount();
        }
    }
}
