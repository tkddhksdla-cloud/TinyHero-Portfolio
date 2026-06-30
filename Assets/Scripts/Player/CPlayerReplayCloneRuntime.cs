using System.Collections.Generic;
using TinyHero.Skill;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 분신 지연 재생 런타임
    ///</summary>
    public sealed class CPlayerReplayCloneRuntime : MonoBehaviour
    {
        private const string AttackHitColliderObjectName = "AttackHitCollider";
        private const string DefaultAttackSlashFxResourcePath = "Prefabs/FX/FX_DefaultAttack_Slash";
        private const int CloneSortingOrderOffset = -50;
        private static readonly Color CloneTintColor = new Color( 0.12f, 0.12f, 0.12f, 0.58f );

        private readonly Collider2D[] attackHitResultBuffer = new Collider2D[ 16 ];
        private readonly List<CPlayerCloneRecorder.CAttackRecord> attackReplayBuffer = new List<CPlayerCloneRecorder.CAttackRecord>();
        private readonly List<CPlayerCloneRecorder.CSkillRecord> skillReplayBuffer = new List<CPlayerCloneRecorder.CSkillRecord>();

        private PlayerController sourcePlayerController;
        private CPlayerCloneRecorder sourceRecorder;
        private CPlayerStatManager sourceStatManager;
        private CSkillManager sourceSkillManager;
        private GameObject cloneVisualObject;
        private Animator cloneAnimator;
        private Collider2D cloneAttackHitCollider;
        private float cloneAttackHitColliderBaseCircleRadius;
        private float sourceStartTime;
        private float followDelaySeconds;
        private float durationSeconds;
        private float damageMultiplier;
        private Vector3 replayOffset;
        private Vector2 cloneAttackHitColliderBaseBoxOffset;
        private Vector2 cloneAttackHitColliderBaseBoxSize;
        private Vector2 cloneAttackHitColliderBaseCapsuleOffset;
        private Vector2 cloneAttackHitColliderBaseCapsuleSize;
        private Vector2 cloneAttackHitColliderBaseCircleOffset;
        private Vector3 cloneAttackHitColliderBaseLocalPosition;
        private bool isInitialized;
        private bool hasCachedCloneAttackHitColliderBaseline;

        private float lastProcessedAttackTime;
        private float lastProcessedSkillTime;

        ///<summary>
        /// 분신 지연 재생 초기화 처리
        ///</summary>
        public void Initialize( PlayerController _sourcePlayerController, CPlayerCloneRecorder _sourceRecorder, float _durationSeconds, float _followDelaySeconds, Vector3 _replayOffset, float _damageMultiplier )
        {
            if ( _sourcePlayerController == null || _sourceRecorder == null )
            {
                return;
            }

            sourcePlayerController = _sourcePlayerController;
            sourceRecorder = _sourceRecorder;
            sourceStatManager = _sourcePlayerController.GetPlayerStatManager();
            sourceSkillManager = _sourcePlayerController.GetSkillManager();
            durationSeconds = Mathf.Max( 0.1f, _durationSeconds );
            followDelaySeconds = Mathf.Max( 0.0f, _followDelaySeconds );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            replayOffset = _replayOffset;
            sourceStartTime = Time.time;
            lastProcessedAttackTime = sourceStartTime;
            lastProcessedSkillTime = sourceStartTime;
            CreateCloneVisualObject();
            isInitialized = cloneVisualObject != null;
        }

        ///<summary>
        /// 분신 지연 재생 프레임 처리
        ///</summary>
        private void Update()
        {
            if ( isInitialized == false )
            {
                return;
            }

            float elapsedTime = Time.time - sourceStartTime;

            if ( elapsedTime >= durationSeconds )
            {
                Destroy( gameObject );
                return;
            }

            float replayWindowEndTime = Mathf.Min( sourceStartTime + durationSeconds, Time.time - followDelaySeconds );

            if ( replayWindowEndTime < sourceStartTime )
            {
                return;
            }

            ApplyReplayFrame( replayWindowEndTime );
            ReplayAttackRecords( replayWindowEndTime );
            ReplaySkillRecords( replayWindowEndTime );
        }

        ///<summary>
        /// 분신 정리 처리
        ///</summary>
        private void OnDestroy()
        {
            if ( cloneVisualObject == null )
            {
                return;
            }

            Destroy( cloneVisualObject );
        }

        ///<summary>
        /// 분신 비주얼 오브젝트 생성 처리
        ///</summary>
        private void CreateCloneVisualObject()
        {
            GameObject sourcePlayerObject = sourcePlayerController != null ? sourcePlayerController.gameObject : null;

            if ( sourcePlayerObject == null )
            {
                return;
            }

            GameObject createdCloneVisualObject = Instantiate( sourcePlayerObject, transform );
            createdCloneVisualObject.name = "ReplayCloneVisual";
            cloneVisualObject = createdCloneVisualObject;
            SanitizeCloneVisualObject();
            ApplyCloneShadowVisual();
            ApplyCloneBackgroundSorting();
            cloneAnimator = cloneVisualObject.GetComponentInChildren<Animator>( true );
            cloneAttackHitCollider = ResolveCloneAttackHitCollider();
            ConfigureCloneAttackHitCollider();
            CacheCloneAttackHitColliderBaseline();
            ApplyCloneAttackHitColliderRange();
            SetCloneAttackHitColliderActive( false );
        }

        ///<summary>
        /// 분신 비주얼 오브젝트 정리 처리
        ///</summary>
        private void SanitizeCloneVisualObject()
        {
            if ( cloneVisualObject == null )
            {
                return;
            }

            MonoBehaviour[] behaviourArray = cloneVisualObject.GetComponentsInChildren<MonoBehaviour>( true );
            int behaviourCount = behaviourArray.Length;

            for ( int index = 0; index < behaviourCount; index++ )
            {
                MonoBehaviour behaviour = behaviourArray[ index ];

                if ( behaviour == null || behaviour == this )
                {
                    continue;
                }

                behaviour.enabled = false;
            }

            Rigidbody2D[] rigidbodyArray = cloneVisualObject.GetComponentsInChildren<Rigidbody2D>( true );
            int rigidbodyCount = rigidbodyArray.Length;

            for ( int index = 0; index < rigidbodyCount; index++ )
            {
                Rigidbody2D targetRigidbody = rigidbodyArray[ index ];

                if ( targetRigidbody == null )
                {
                    continue;
                }

                targetRigidbody.linearVelocity = Vector2.zero;
                targetRigidbody.angularVelocity = 0.0f;
                targetRigidbody.simulated = false;
            }

            Collider2D[] colliderArray = cloneVisualObject.GetComponentsInChildren<Collider2D>( true );
            int colliderCount = colliderArray.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                Collider2D targetCollider = colliderArray[ index ];

                if ( targetCollider == null )
                {
                    continue;
                }

                targetCollider.enabled = false;
            }
        }

        ///<summary>
        /// 분신 그림자 색상 적용
        ///</summary>
        private void ApplyCloneShadowVisual()
        {
            if ( cloneVisualObject == null )
            {
                return;
            }

            SpriteRenderer[] spriteRendererArray = cloneVisualObject.GetComponentsInChildren<SpriteRenderer>( true );
            int spriteRendererCount = spriteRendererArray.Length;

            for ( int index = 0; index < spriteRendererCount; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRendererArray[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                Color sourceColor = spriteRenderer.color;
                Color tintedColor = new Color(
                    sourceColor.r * CloneTintColor.r,
                    sourceColor.g * CloneTintColor.g,
                    sourceColor.b * CloneTintColor.b,
                    sourceColor.a * CloneTintColor.a );
                spriteRenderer.color = tintedColor;
            }
        }

        ///<summary>
        /// 분신 후방 정렬 적용
        ///</summary>
        private void ApplyCloneBackgroundSorting()
        {
            if ( cloneVisualObject == null )
            {
                return;
            }

            SortingGroup[] sortingGroupArray = cloneVisualObject.GetComponentsInChildren<SortingGroup>( true );
            int sortingGroupCount = sortingGroupArray.Length;

            for ( int index = 0; index < sortingGroupCount; index++ )
            {
                SortingGroup sortingGroup = sortingGroupArray[ index ];

                if ( sortingGroup == null )
                {
                    continue;
                }

                sortingGroup.sortingOrder += CloneSortingOrderOffset;
            }

            Renderer[] rendererArray = cloneVisualObject.GetComponentsInChildren<Renderer>( true );
            int rendererCount = rendererArray.Length;

            for ( int index = 0; index < rendererCount; index++ )
            {
                Renderer targetRenderer = rendererArray[ index ];

                if ( targetRenderer == null )
                {
                    continue;
                }

                targetRenderer.sortingOrder += CloneSortingOrderOffset;
            }
        }

        ///<summary>
        /// 분신 프레임 상태 적용 처리
        ///</summary>
        private void ApplyReplayFrame( float _replayTime )
        {
            if ( cloneVisualObject == null || sourceRecorder == null )
            {
                return;
            }

            bool hasFrameRecord = sourceRecorder.TryGetFrameAtTime( _replayTime, out CPlayerCloneRecorder.CFrameRecord frameRecord );

            if ( hasFrameRecord == false )
            {
                return;
            }

            Vector3 resolvedOffset = ResolveReplayOffset( frameRecord.localScale );
            cloneVisualObject.transform.position = frameRecord.worldPosition + resolvedOffset;
            cloneVisualObject.transform.localScale = frameRecord.localScale;
            ApplyReplayAnimation( frameRecord );
        }

        ///<summary>
        /// 분신 재생 오프셋 계산 처리
        ///</summary>
        private Vector3 ResolveReplayOffset( Vector3 _localScale )
        {
            Vector3 resolvedOffset = replayOffset;
            float facingDirection = _localScale.x < 0.0f ? -1.0f : 1.0f;
            resolvedOffset.x *= facingDirection;
            return resolvedOffset;
        }

        ///<summary>
        /// 분신 애니메이션 상태 적용 처리
        ///</summary>
        private void ApplyReplayAnimation( CPlayerCloneRecorder.CFrameRecord _frameRecord )
        {
            if ( cloneAnimator == null || string.IsNullOrWhiteSpace( _frameRecord.animationStateName ) )
            {
                return;
            }

            float normalizedTime = Mathf.Repeat( _frameRecord.animationNormalizedTime, 1.0f );
            cloneAnimator.speed = Mathf.Max( 0.01f, _frameRecord.animatorSpeed );
            cloneAnimator.Play( _frameRecord.animationStateName, 0, normalizedTime );
            cloneAnimator.Update( 0.0f );
        }

        ///<summary>
        /// 지연 공격 이벤트 재생 처리
        ///</summary>
        private void ReplayAttackRecords( float _replayWindowEndTime )
        {
            if ( sourceRecorder == null || _replayWindowEndTime <= lastProcessedAttackTime )
            {
                return;
            }

            sourceRecorder.CollectAttackRecords( lastProcessedAttackTime, _replayWindowEndTime, attackReplayBuffer );
            lastProcessedAttackTime = _replayWindowEndTime;

            for ( int index = 0; index < attackReplayBuffer.Count; index++ )
            {
                CPlayerCloneRecorder.CAttackRecord attackRecord = attackReplayBuffer[ index ];
                ReplayAttackRecord( attackRecord );
            }
        }

        ///<summary>
        /// 지연 스킬 이벤트 재생 처리
        ///</summary>
        private void ReplaySkillRecords( float _replayWindowEndTime )
        {
            if ( sourceRecorder == null || _replayWindowEndTime <= lastProcessedSkillTime )
            {
                return;
            }

            sourceRecorder.CollectSkillRecords( lastProcessedSkillTime, _replayWindowEndTime, skillReplayBuffer );
            lastProcessedSkillTime = _replayWindowEndTime;

            for ( int index = 0; index < skillReplayBuffer.Count; index++ )
            {
                CPlayerCloneRecorder.CSkillRecord skillRecord = skillReplayBuffer[ index ];
                ReplaySkillRecord( skillRecord );
            }
        }

        ///<summary>
        /// 공격 기록 단건 재생 처리
        ///</summary>
        private void ReplayAttackRecord( CPlayerCloneRecorder.CAttackRecord _attackRecord )
        {
            PlayAttackSlashFx();
            MonsterObject attackTarget = ResolveHighestPriorityAttackTarget();

            if ( attackTarget == null )
            {
                return;
            }

            bool wasAliveBeforeHit = attackTarget.GetCurrentHp() > 0;
            long attackDamage = ResolveReplayAttackDamage( attackTarget, _attackRecord.attackStatValue, _attackRecord.skillAttackPowerMultiplier, out bool isCritical );
            attackTarget.TakeDamage( attackDamage, isCritical );
            TryGrantMonsterReward( attackTarget, wasAliveBeforeHit );
        }

        ///<summary>
        /// 스킬 기록 단건 재생 처리
        ///</summary>
        private void ReplaySkillRecord( CPlayerCloneRecorder.CSkillRecord _skillRecord )
        {
            if ( sourceSkillManager == null || cloneVisualObject == null )
            {
                return;
            }

            CSkillDefinition skillDefinition = sourceSkillManager.GetSkillDefinition( _skillRecord.skillId );

            if ( skillDefinition == null || skillDefinition.GetSkillType() != eSkillType.ACTIVE )
            {
                return;
            }

            if ( skillDefinition.GetActiveSkillType() == eActiveSkillType.CLONE )
            {
                return;
            }

            if ( skillDefinition.GetActiveSkillType() == eActiveSkillType.BUFF )
            {
                ReplayBuffSkillVisual( skillDefinition, _skillRecord );
                return;
            }

            CSkillRuntimeData runtimeData = sourceSkillManager.GetSkillRuntimeData( _skillRecord.skillId );
            CSkillContext skillContext = new CSkillContext(
                sourceSkillManager,
                sourcePlayerController,
                sourceStatManager,
                skillDefinition,
                runtimeData,
                cloneVisualObject.transform,
                _skillRecord.attackStatValue * damageMultiplier,
                _skillRecord.skillAttackPowerMultiplier );
            CActiveSkillEffectBase activeSkillEffect = skillDefinition.GetActiveSkillEffect();
            CSkillActionBase activeAction = skillDefinition.GetActiveAction();

            if ( activeSkillEffect != null )
            {
                bool canExecuteEffect = activeSkillEffect.CanExecute( skillContext );

                if ( canExecuteEffect )
                {
                    activeSkillEffect.Execute( skillContext );
                }

                return;
            }

            if ( activeAction == null )
            {
                return;
            }

            bool canExecuteAction = activeAction.CanExecute( skillContext );

            if ( canExecuteAction == false )
            {
                return;
            }

            activeAction.Execute( skillContext );
        }

        ///<summary>
        /// 버프 스킬 비주얼 재생 처리
        ///</summary>
        private void ReplayBuffSkillVisual( CSkillDefinition _skillDefinition, CPlayerCloneRecorder.CSkillRecord _skillRecord )
        {
            if ( _skillDefinition == null || cloneVisualObject == null )
            {
                return;
            }

            CSkillRuntimeData runtimeData = sourceSkillManager != null ? sourceSkillManager.GetSkillRuntimeData( _skillRecord.skillId ) : null;
            CSkillContext skillContext = new CSkillContext(
                sourceSkillManager,
                sourcePlayerController,
                sourceStatManager,
                _skillDefinition,
                runtimeData,
                cloneVisualObject.transform,
                _skillRecord.attackStatValue * damageMultiplier,
                _skillRecord.skillAttackPowerMultiplier );
            CSkillVfxUtility.PlayCastVfx( skillContext );

            if ( _skillDefinition.GetLoopVfxPrefab() != null )
            {
                CSkillVfxUtility.PlayLoopVfx( skillContext, cloneVisualObject.transform, 0.0f );
            }
        }

        ///<summary>
        /// 분신 공격 대상 결정 처리
        ///</summary>
        private MonsterObject ResolveHighestPriorityAttackTarget()
        {
            if ( cloneAttackHitCollider == null )
            {
                return null;
            }

            SetCloneAttackHitColliderActive( true );
            ContactFilter2D contactFilter = new ContactFilter2D();
            int monsterLayer = LayerMask.NameToLayer( "Monster" );
            contactFilter.useLayerMask = monsterLayer >= 0;
            contactFilter.useTriggers = true;
            contactFilter.layerMask = monsterLayer >= 0 ? LayerMask.GetMask( "Monster" ) : Physics2D.AllLayers;
            int overlapCount = cloneAttackHitCollider.Overlap( contactFilter, attackHitResultBuffer );
            MonsterObject highestPriorityMonster = null;
            int highestPriorityScore = int.MinValue;

            for ( int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++ )
            {
                Collider2D overlapCollider = attackHitResultBuffer[ overlapIndex ];
                MonsterObject monsterObject = ResolveMonsterObjectFromCollider( overlapCollider );

                if ( monsterObject == null )
                {
                    continue;
                }

                int priorityScore = ResolveMonsterPriorityScore( monsterObject );

                if ( priorityScore <= highestPriorityScore )
                {
                    continue;
                }

                highestPriorityScore = priorityScore;
                highestPriorityMonster = monsterObject;
            }

            SetCloneAttackHitColliderActive( false );
            return highestPriorityMonster;
        }

        ///<summary>
        /// 분신 공격 피해량 계산 처리
        ///</summary>
        private long ResolveReplayAttackDamage( MonsterObject _monsterObject, float _attackStatValue, float _skillAttackPowerMultiplier, out bool _isCritical )
        {
            _isCritical = false;

            if ( _monsterObject == null )
            {
                return 0L;
            }

            float rawDamage = ( _attackStatValue * damageMultiplier ) * _skillAttackPowerMultiplier - _monsterObject.GetDef();
            float resolvedDamage = CPlayerCombatStatUtility.ResolveCombatDamage( sourceStatManager, rawDamage, out bool isCritical );
            _isCritical = isCritical;
            long result = System.Math.Max( 0L, ( long )System.Math.Round( resolvedDamage ) );
            return result;
        }

        ///<summary>
        /// 분신 처치 경험치 지급 처리
        ///</summary>
        private void TryGrantMonsterReward( MonsterObject _monsterObject, bool _wasAliveBeforeHit )
        {
            if ( sourcePlayerController == null || _monsterObject == null || _wasAliveBeforeHit == false )
            {
                return;
            }

            if ( _monsterObject.GetCurrentHp() > 0 )
            {
                return;
            }

            _monsterObject.TryGrantReward( sourcePlayerController );
        }

        ///<summary>
        /// 분신 공격 이펙트 재생 처리
        ///</summary>
        private void PlayAttackSlashFx()
        {
            if ( cloneAttackHitCollider == null )
            {
                return;
            }

            GameObject attackSlashFxPrefab = Resources.Load<GameObject>( DefaultAttackSlashFxResourcePath );

            if ( attackSlashFxPrefab == null )
            {
                return;
            }

            GameObject attackSlashFxObject = Instantiate( attackSlashFxPrefab );
            Transform fxTransform = attackSlashFxObject.transform;
            fxTransform.position = cloneAttackHitCollider.transform.position;
            fxTransform.rotation = attackSlashFxPrefab.transform.rotation;
            Vector3 fxScale = attackSlashFxPrefab.transform.localScale;
            float facingDirection = cloneVisualObject.transform.localScale.x < 0.0f ? -1.0f : 1.0f;
            float rangeMultiplier = sourceStatManager != null ? sourceStatManager.GetRangeMultiplier() : 1.0f;
            rangeMultiplier = Mathf.Max( 0.1f, rangeMultiplier );
            fxScale.x *= rangeMultiplier;
            fxScale.y *= rangeMultiplier;
            fxScale.z *= rangeMultiplier;
            fxScale.x = Mathf.Abs( fxScale.x ) * facingDirection;
            fxTransform.localScale = fxScale;
            CSkillRenderUtility.ApplyForegroundSorting( attackSlashFxObject );
            Destroy( attackSlashFxObject, 0.5f );
        }

        ///<summary>
        /// 분신 공격 판정 콜라이더 결정 처리
        ///</summary>
        private Collider2D ResolveCloneAttackHitCollider()
        {
            if ( cloneVisualObject == null )
            {
                return null;
            }

            Collider2D[] colliderArray = cloneVisualObject.GetComponentsInChildren<Collider2D>( true );
            int colliderCount = colliderArray.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                Collider2D targetCollider = colliderArray[ index ];

                if ( targetCollider == null )
                {
                    continue;
                }

                if ( string.Equals( targetCollider.gameObject.name, AttackHitColliderObjectName, System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                return targetCollider;
            }

            return null;
        }

        ///<summary>
        /// 분신 공격 범위 콜라이더 설정
        ///</summary>
        private void ConfigureCloneAttackHitCollider()
        {
            if ( cloneAttackHitCollider == null )
            {
                return;
            }

            cloneAttackHitCollider.isTrigger = true;
            int monsterLayer = LayerMask.NameToLayer( "Monster" );

            if ( monsterLayer < 0 )
            {
                return;
            }

            cloneAttackHitCollider.includeLayers = LayerMask.GetMask( "Monster" );
            cloneAttackHitCollider.excludeLayers = ~LayerMask.GetMask( "Monster" );
        }

        ///<summary>
        /// 분신 공격 콜라이더 기준값 캐시
        ///</summary>
        private void CacheCloneAttackHitColliderBaseline()
        {
            if ( cloneAttackHitCollider == null || hasCachedCloneAttackHitColliderBaseline )
            {
                return;
            }

            Transform attackColliderTransform = cloneAttackHitCollider.transform;

            if ( attackColliderTransform != null )
            {
                cloneAttackHitColliderBaseLocalPosition = attackColliderTransform.localPosition;
            }

            BoxCollider2D boxCollider = cloneAttackHitCollider as BoxCollider2D;

            if ( boxCollider != null )
            {
                cloneAttackHitColliderBaseBoxOffset = boxCollider.offset;
                cloneAttackHitColliderBaseBoxSize = boxCollider.size;
            }

            CircleCollider2D circleCollider = cloneAttackHitCollider as CircleCollider2D;

            if ( circleCollider != null )
            {
                cloneAttackHitColliderBaseCircleOffset = circleCollider.offset;
                cloneAttackHitColliderBaseCircleRadius = circleCollider.radius;
            }

            CapsuleCollider2D capsuleCollider = cloneAttackHitCollider as CapsuleCollider2D;

            if ( capsuleCollider != null )
            {
                cloneAttackHitColliderBaseCapsuleOffset = capsuleCollider.offset;
                cloneAttackHitColliderBaseCapsuleSize = capsuleCollider.size;
            }

            hasCachedCloneAttackHitColliderBaseline = true;
        }

        ///<summary>
        /// 분신 공격 콜라이더 범위 배율 적용
        ///</summary>
        private void ApplyCloneAttackHitColliderRange()
        {
            if ( cloneAttackHitCollider == null )
            {
                return;
            }

            CacheCloneAttackHitColliderBaseline();
            float rangeMultiplier = sourceStatManager != null ? sourceStatManager.GetRangeMultiplier() : 1.0f;
            rangeMultiplier = Mathf.Max( 0.1f, rangeMultiplier );
            Transform attackColliderTransform = cloneAttackHitCollider.transform;

            if ( attackColliderTransform != null )
            {
                Vector3 adjustedLocalPosition = cloneAttackHitColliderBaseLocalPosition;
                adjustedLocalPosition.x *= rangeMultiplier;
                attackColliderTransform.localPosition = adjustedLocalPosition;
            }

            BoxCollider2D boxCollider = cloneAttackHitCollider as BoxCollider2D;

            if ( boxCollider != null )
            {
                Vector2 adjustedOffset = cloneAttackHitColliderBaseBoxOffset;
                adjustedOffset.x *= rangeMultiplier;
                Vector2 adjustedSize = cloneAttackHitColliderBaseBoxSize;
                adjustedSize.x *= rangeMultiplier;
                boxCollider.offset = adjustedOffset;
                boxCollider.size = adjustedSize;
            }

            CircleCollider2D circleCollider = cloneAttackHitCollider as CircleCollider2D;

            if ( circleCollider != null )
            {
                Vector2 adjustedOffset = cloneAttackHitColliderBaseCircleOffset;
                adjustedOffset.x *= rangeMultiplier;
                circleCollider.offset = adjustedOffset;
                circleCollider.radius = cloneAttackHitColliderBaseCircleRadius * rangeMultiplier;
            }

            CapsuleCollider2D capsuleCollider = cloneAttackHitCollider as CapsuleCollider2D;

            if ( capsuleCollider != null )
            {
                Vector2 adjustedOffset = cloneAttackHitColliderBaseCapsuleOffset;
                adjustedOffset.x *= rangeMultiplier;
                Vector2 adjustedSize = cloneAttackHitColliderBaseCapsuleSize;

                if ( capsuleCollider.direction == CapsuleDirection2D.Horizontal )
                {
                    adjustedSize.x *= rangeMultiplier;
                }
                else
                {
                    adjustedSize.y *= rangeMultiplier;
                }

                capsuleCollider.offset = adjustedOffset;
                capsuleCollider.size = adjustedSize;
            }
        }

        ///<summary>
        /// 분신 공격 범위 활성 상태 설정
        ///</summary>
        private void SetCloneAttackHitColliderActive( bool _isActive )
        {
            if ( cloneAttackHitCollider == null )
            {
                return;
            }

            GameObject attackHitObject = cloneAttackHitCollider.gameObject;

            if ( attackHitObject.activeSelf != _isActive )
            {
                attackHitObject.SetActive( _isActive );
            }

            cloneAttackHitCollider.enabled = _isActive;
        }

        ///<summary>
        /// 콜라이더 기반 몬스터 결정 처리
        ///</summary>
        private MonsterObject ResolveMonsterObjectFromCollider( Collider2D _overlapCollider )
        {
            if ( _overlapCollider == null )
            {
                return null;
            }

            MonsterObject resolvedMonsterObject = _overlapCollider.GetComponent<MonsterObject>();

            if ( resolvedMonsterObject != null )
            {
                return resolvedMonsterObject;
            }

            MonsterObject resolvedParentMonsterObject = _overlapCollider.GetComponentInParent<MonsterObject>();
            return resolvedParentMonsterObject;
        }

        ///<summary>
        /// 몬스터 표시 우선순위 점수 계산 처리
        ///</summary>
        private int ResolveMonsterPriorityScore( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return int.MinValue;
            }

            SpriteRenderer[] spriteRendererArray = _monsterObject.GetComponentsInChildren<SpriteRenderer>( true );
            int highestScore = int.MinValue;

            for ( int index = 0; index < spriteRendererArray.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRendererArray[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                int sortingLayerValue = SortingLayer.GetLayerValueFromID( spriteRenderer.sortingLayerID );
                int currentScore = sortingLayerValue * 10000 + spriteRenderer.sortingOrder;

                if ( currentScore > highestScore )
                {
                    highestScore = currentScore;
                }
            }

            return highestScore;
        }
    }
}
