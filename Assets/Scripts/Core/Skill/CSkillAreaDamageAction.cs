using TinyHero.Player;
using UnityEngine;
using System.Collections.Generic;

namespace TinyHero.Skill
{
    ///<summary>
    /// 전방 범위 피해형 액티브 스킬 정의
    ///</summary>
    public sealed class CSkillAreaDamageAction : CSkillActionBase
    {
        private const int DefaultOverlapBufferSize = 16;

        [SerializeField] private Vector2 areaOffset;
        [SerializeField] private float areaRadius = 1.5f;
        [SerializeField] private float damageMultiplier = 1.5f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] private int maxTargetCount = 16;

        private readonly Collider2D[] overlapBuffer = new Collider2D[ DefaultOverlapBufferSize ];
        private readonly HashSet<int> processedMonsterInstanceIdSet = new HashSet<int>();

        ///<summary>
        /// 범위 피해 액션 데이터 구성
        ///</summary>
        public void Configure( Vector2 _areaOffset, float _areaRadius, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount )
        {
            areaOffset = _areaOffset;
            areaRadius = Mathf.Max( 0.1f, _areaRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = Mathf.Max( 0, _flatDamageBonus );
            maxTargetCount = Mathf.Max( 1, _maxTargetCount );
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
        /// 스킬 실행 가능 여부 판정
        ///</summary>
        public override bool CanExecute( CSkillContext _skillContext )
        {
            bool canExecuteBase = base.CanExecute( _skillContext );

            if ( canExecuteBase == false )
            {
                return false;
            }

            CPlayerStatManager statManager = _skillContext.GetPlayerStatManager();
            bool result = statManager != null;
            return result;
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

            Transform ownerTransform = _skillContext.GetOwnerTransform();
            Vector2 attackCenter = ResolveAttackCenter( _skillContext, ownerTransform );
            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            float scaledAreaRadius = ResolveScaledAreaRadius( _skillContext );
            int hitCount = Physics2D.OverlapCircle( attackCenter, scaledAreaRadius, contactFilter, overlapBuffer );
            CSkillAudioUtility.PlayCastSfx( _skillContext );
            ApplyDamageToTargets( _skillContext, hitCount );
            return true;
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
        /// 공격 중심 좌표 계산
        ///</summary>
        private Vector2 ResolveAttackCenter( CSkillContext _skillContext, Transform _ownerTransform )
        {
            float facingDirection = ResolveFacingDirection( _ownerTransform );
            Vector2 resolvedOffset = ResolveScaledAreaOffset( _skillContext );
            resolvedOffset.x *= facingDirection;
            Vector2 ownerPosition = _ownerTransform.position;
            Vector2 attackCenter = ownerPosition + resolvedOffset;
            return attackCenter;
        }

        ///<summary>
        /// 범위 스킬 반경 계산
        ///</summary>
        private float ResolveScaledAreaRadius( CSkillContext _skillContext )
        {
            float scaledAreaRadius = _skillContext != null ? _skillContext.ScaleRangeValue( areaRadius ) : areaRadius;
            float result = Mathf.Max( 0.1f, scaledAreaRadius );
            return result;
        }

        ///<summary>
        /// 범위 스킬 오프셋 계산
        ///</summary>
        private Vector2 ResolveScaledAreaOffset( CSkillContext _skillContext )
        {
            Vector2 result = _skillContext != null ? _skillContext.ScaleRangeOffset( areaOffset ) : areaOffset;
            return result;
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
        /// 범위 내 대상 피해 적용
        ///</summary>
        private bool ApplyDamageToTargets( CSkillContext _skillContext, int _hitCount )
        {
            bool didHitAnyTarget = false;
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

                bool isNewSingleHitExecution = monsterObject.TryRegisterSingleHitSkillExecution( _skillContext.GetExecutionId() );

                if ( isNewSingleHitExecution == false )
                {
                    continue;
                }

                bool wasAliveBeforeHit = monsterObject.GetCurrentHp() > 0;
                long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( _skillContext, monsterObject, damageMultiplier, flatDamageBonus, out bool isCritical );
                monsterObject.TakeDamage( damage, isCritical );
                CSkillAudioUtility.PlayHitSfx( _skillContext );
                CSkillDamageUtility.TryAwardMonsterExp( _skillContext, monsterObject, wasAliveBeforeHit );
                processedTargetCount++;
                didHitAnyTarget = true;
            }

            return didHitAnyTarget;
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
        /// 피해량 계산
        ///</summary>
        private long ResolveDamage( CPlayerStatManager _playerStatManager, MonsterObject _monsterObject )
        {
            if ( _playerStatManager == null || _monsterObject == null )
            {
                return 0;
            }

            float playerAtk = _playerStatManager.GetFinalStatValue( ePlayerStatType.ATK );
            float rawDamage = playerAtk * damageMultiplier + flatDamageBonus - _monsterObject.GetDef();
            long damage = Mathf.Max( 1, Mathf.RoundToInt( rawDamage ) );
            return damage;
        }

        ///<summary>
        /// 스킬 처치 경험치 지급
        ///</summary>
        private void TryAwardMonsterExp( CPlayerStatManager _playerStatManager, MonsterObject _monsterObject, bool _wasAliveBeforeHit )
        {
            if ( _playerStatManager == null || _monsterObject == null )
            {
                return;
            }

            if ( _wasAliveBeforeHit == false )
            {
                return;
            }

            if ( _monsterObject.GetCurrentHp() > 0 )
            {
                return;
            }

            long expReward = _monsterObject.GetExpReward();

            if ( expReward <= 0 )
            {
                return;
            }

            _playerStatManager.AddExp( expReward );
        }
    }
}
