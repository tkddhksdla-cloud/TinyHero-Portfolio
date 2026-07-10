using System.Collections;
using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    public sealed class CSkyfallStrikeSkillRuntime : MonoBehaviour
    {
        private const float LandingInvincibilityPostDelaySeconds = 0.5f;

        private readonly Collider2D[] overlapBuffer = new Collider2D[ 24 ];
        private readonly HashSet<int> processedMonsterIdSet = new HashSet<int>();
        private CSkillContext skillContext;
        private Rigidbody2D targetRigidbody;
        private float launchDurationSeconds;
        private float plungeDurationSeconds;
        private float launchDistance;
        private float launchHeight;
        private float landingDistance;
        private float areaRadius;
        private float damageMultiplier;
        private int flatDamageBonus;
        private int maxTargetCount;
        private List<CEnemyCrowdControlEffectBase> crowdControlEffectList;
        private CSkyfallStrikeActiveSkillEffect skyfallStrikeEffect;
        private bool isSuperArmorApplied;
        private bool isAirborneCast;
        private float resolvedRangeMultiplier = 1.0f;
        private float additionalVfxRangeScaleMultiplier = 1.0f;
        private float damageScaleMultiplier = 1.0f;

        public void Initialize( CSkillContext _skillContext, float _launchDurationSeconds, float _plungeDurationSeconds, float _launchDistance, float _launchHeight, float _landingDistance, float _areaRadius, float _damageMultiplier, int _flatDamageBonus, int _maxTargetCount, List<CEnemyCrowdControlEffectBase> _crowdControlEffectList )
        {
            skillContext = _skillContext;
            targetRigidbody = skillContext.GetPlayerController().GetComponent<Rigidbody2D>();
            launchDurationSeconds = _launchDurationSeconds;
            plungeDurationSeconds = _plungeDurationSeconds;
            launchDistance = _launchDistance;
            launchHeight = _launchHeight;
            landingDistance = _landingDistance;
            areaRadius = _areaRadius;
            damageMultiplier = _damageMultiplier;
            flatDamageBonus = _flatDamageBonus;
            maxTargetCount = _maxTargetCount;
            crowdControlEffectList = _crowdControlEffectList;
            skyfallStrikeEffect = skillContext.GetSkillDefinition().GetActiveSkillEffect() as CSkyfallStrikeActiveSkillEffect;
            isAirborneCast = skillContext.GetPlayerController().IsGrounded() == false;
            float playerRangeMultiplier = skillContext.GetRangeMultiplier();
            resolvedRangeMultiplier = playerRangeMultiplier;

            if ( isAirborneCast && skyfallStrikeEffect != null )
            {
                resolvedRangeMultiplier += skyfallStrikeEffect.GetAirborneRangeIncreasePercent();
                damageScaleMultiplier += skyfallStrikeEffect.GetAirborneDamageIncreasePercent();
            }

            float minimumRangeMultiplier = Mathf.Max( 0.1f, playerRangeMultiplier );
            additionalVfxRangeScaleMultiplier = resolvedRangeMultiplier / minimumRangeMultiplier;

            StartCoroutine( IE_Execute() );
        }

        private IEnumerator IE_Execute()
        {
            Transform ownerTransform = skillContext.GetOwnerTransform();
            Vector3 startPosition = ownerTransform.position;
            float facingDirection = ownerTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            Vector3 apexPosition = startPosition + new Vector3( facingDirection * launchDistance, launchHeight, 0.0f );
            float resolvedLandingDistance = landingDistance * resolvedRangeMultiplier;
            Vector3 landingPosition = ResolveGroundLandingPosition( startPosition, facingDirection, resolvedLandingDistance );
            float originalGravityScale = targetRigidbody != null ? targetRigidbody.gravityScale : 0.0f;

            if ( targetRigidbody != null )
            {
                targetRigidbody.gravityScale = 0.0f;
                targetRigidbody.linearVelocity = Vector2.zero;
            }

            skillContext.GetPlayerController().BeginSuperArmor();
            isSuperArmorApplied = true;

            yield return IE_MoveTo( apexPosition, launchDurationSeconds );
            float invincibilityDuration = plungeDurationSeconds + LandingInvincibilityPostDelaySeconds;
            skillContext.GetPlayerController().ApplySkillInvincibility( invincibilityDuration );
            yield return IE_MoveTo( landingPosition, plungeDurationSeconds );
            ApplyLandingDamage( landingPosition );

            if ( targetRigidbody != null )
            {
                targetRigidbody.gravityScale = originalGravityScale;
                targetRigidbody.linearVelocity = Vector2.zero;
            }

            ReleaseSuperArmor();

            Destroy( gameObject );
        }

        private void OnDestroy()
        {
            ReleaseSuperArmor();
        }

        private void ReleaseSuperArmor()
        {
            if ( isSuperArmorApplied == false || skillContext == null )
            {
                return;
            }

            PlayerController playerController = skillContext.GetPlayerController();

            if ( playerController != null )
            {
                playerController.EndSuperArmor();
            }

            isSuperArmorApplied = false;
        }

        private IEnumerator IE_MoveTo( Vector3 _targetPosition, float _durationSeconds )
        {
            Transform ownerTransform = skillContext.GetOwnerTransform();
            Vector3 startPosition = ownerTransform.position;
            float elapsedTime = 0.0f;

            while ( elapsedTime < _durationSeconds )
            {
                elapsedTime += Time.fixedDeltaTime;
                float progress = Mathf.Clamp01( elapsedTime / _durationSeconds );
                Vector3 position = Vector3.Lerp( startPosition, _targetPosition, progress );

                if ( targetRigidbody != null )
                {
                    targetRigidbody.MovePosition( position );
                }
                else
                {
                    ownerTransform.position = position;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        private void ApplyLandingDamage( Vector3 _landingPosition )
        {
            PlayLandingVfx( _landingPosition );
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.useTriggers = true;
            filter.layerMask = LayerMask.GetMask( "Monster" );
            float resolvedAreaRadius = areaRadius * resolvedRangeMultiplier;
            int count = Physics2D.OverlapCircle( _landingPosition, resolvedAreaRadius, filter, overlapBuffer );
            processedMonsterIdSet.Clear();
            int processedCount = 0;

            for ( int index = 0; index < count && processedCount < maxTargetCount; index++ )
            {
                MonsterObject monsterObject = overlapBuffer[ index ].GetComponentInParent<MonsterObject>();
                if ( monsterObject == null || monsterObject.GetCurrentHp() <= 0 || processedMonsterIdSet.Add( monsterObject.GetInstanceID() ) == false )
                {
                    continue;
                }

                bool wasAlive = monsterObject.GetCurrentHp() > 0;
                float resolvedDamageMultiplier = damageMultiplier * damageScaleMultiplier;
                long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( skillContext, monsterObject, resolvedDamageMultiplier, flatDamageBonus, out bool isCritical );
                monsterObject.TakeDamage( damage, isCritical );
                CSkillPooledVfxHandle hitVfxHandle = CSkillVfxUtility.PlayHitVfx( skillContext, monsterObject.transform );
                ApplyAdditionalVfxScale( hitVfxHandle );
                CSkillAudioUtility.PlayHitSfx( skillContext );

                for ( int crowdControlIndex = 0; crowdControlEffectList != null && crowdControlIndex < crowdControlEffectList.Count; crowdControlIndex++ )
                {
                    CEnemyCrowdControlEffectBase crowdControlEffect = crowdControlEffectList[ crowdControlIndex ];
                    if ( crowdControlEffect != null ) crowdControlEffect.TryApply( skillContext, monsterObject );
                }

                CSkillDamageUtility.TryAwardMonsterExp( skillContext, monsterObject, wasAlive );
                processedCount++;
            }
        }

        private void PlayLandingVfx( Vector3 _landingPosition )
        {
            if ( skyfallStrikeEffect == null )
            {
                return;
            }

            CSkillPooledVfxHandle vfxHandle = CSkillVfxUtility.PlayVfxAtWorldPosition( skillContext, skyfallStrikeEffect.GetLandingVfxPrefab(), _landingPosition, skyfallStrikeEffect.GetLandingVfxOffset(), skyfallStrikeEffect.GetLandingVfxReturnDelay() );
            ApplyAdditionalVfxScale( vfxHandle );
        }

        private Vector3 ResolveGroundLandingPosition( Vector3 _startPosition, float _facingDirection, float _landingDistance )
        {
            float rayOriginY = _startPosition.y + Mathf.Max( launchHeight, 1.0f ) + 20.0f;
            Vector2 rayOrigin = new Vector2( _startPosition.x + _facingDirection * _landingDistance, rayOriginY );
            RaycastHit2D groundHit = Physics2D.Raycast( rayOrigin, Vector2.down, 50.0f, LayerMask.GetMask( "Ground" ) );
            float landingY = groundHit.collider != null ? groundHit.point.y : _startPosition.y;
            Vector3 result = new Vector3( rayOrigin.x, landingY, _startPosition.z );
            return result;
        }

        private void ApplyAdditionalVfxScale( CSkillPooledVfxHandle _vfxHandle )
        {
            if ( _vfxHandle == null || additionalVfxRangeScaleMultiplier <= 1.0f )
            {
                return;
            }

            GameObject spawnedObject = _vfxHandle.GetSpawnedObject();

            if ( spawnedObject == null )
            {
                return;
            }

            Transform spawnedTransform = spawnedObject.transform;
            spawnedTransform.localScale *= additionalVfxRangeScaleMultiplier;
        }
    }
}
