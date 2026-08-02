using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 부유 무기 소환수의 수명, 편대, 대상 예약과 장비 외형 동기화 관리
    ///</summary>
    public sealed class CFloatingWeaponSummonRuntime : MonoBehaviour
    {
        private const int TargetBufferSize = 64;
        private const float MinimumTargetRetrySeconds = 0.2f;
        private static readonly Color PlayerWeaponTintColor = Color.white;
        private static readonly Color ReplayCloneWeaponTintColor = Color.black;
        private static readonly Color PlayerTrailColor = Color.white;
        private static readonly Color ReplayCloneTrailColor = Color.black;
        private static readonly Dictionary<int, CFloatingWeaponSummonRuntime> ActiveRuntimeByOwnerId = new Dictionary<int, CFloatingWeaponSummonRuntime>();

        private readonly Collider2D[] targetBuffer = new Collider2D[ TargetBufferSize ];
        private readonly List<CFloatingWeaponCompanionRuntime> companionList = new List<CFloatingWeaponCompanionRuntime>();
        private CSkillContext skillContext;
        private Transform ownerTransform;
        private CPlayerEquipmentManager equipmentManager;
        private Vector2 formationBaseOffset;
        private float formationVerticalSpacing;
        private float hoverAmplitude;
        private float hoverFrequency;
        private float targetSearchRadius;
        private float attackIntervalSeconds;
        private float flightSpeed;
        private float hitRadius;
        private float damageMultiplier;
        private int flatDamageBonus;
        private float attackRotationSpeed;
        private float expirationTime;
        private int ownerInstanceId;
        private bool isInitialized;
        private bool isReplayCloneOwner;

        ///<summary>
        /// 부유 무기 소환 런타임 초기화
        ///</summary>
        public bool Initialize( CSkillContext _skillContext, int _companionCount, float _durationSeconds, Vector2 _formationBaseOffset, float _formationVerticalSpacing, float _hoverAmplitude, float _hoverFrequency, float _targetSearchRadius, float _attackIntervalSeconds, float _flightSpeed, float _hitRadius, float _damageMultiplier, int _flatDamageBonus, GameObject _floatingWeaponPrefab, GameObject _floatingWeaponTrailPrefab, float _weaponVisualScale, float _attackRotationSpeed )
        {
            if ( _skillContext == null || _skillContext.GetOwnerTransform() == null )
            {
                return false;
            }

            if ( _floatingWeaponPrefab == null || _floatingWeaponTrailPrefab == null || CFloatingWeaponVisualUtility.TryResolveWeaponSprite( _skillContext, out Sprite weaponSprite ) == false )
            {
                return false;
            }

            skillContext = _skillContext;
            ownerTransform = _skillContext.GetOwnerTransform();
            isReplayCloneOwner = ownerTransform.GetComponentInParent<CPlayerReplayCloneRuntime>() != null;
            formationBaseOffset = _formationBaseOffset;
            formationVerticalSpacing = Mathf.Max( 0.0f, _formationVerticalSpacing );
            hoverAmplitude = Mathf.Max( 0.0f, _hoverAmplitude );
            hoverFrequency = Mathf.Max( 0.0f, _hoverFrequency );
            targetSearchRadius = Mathf.Max( 0.01f, _targetSearchRadius );
            attackIntervalSeconds = Mathf.Max( 0.01f, _attackIntervalSeconds );
            flightSpeed = Mathf.Max( 0.01f, _flightSpeed );
            hitRadius = Mathf.Max( 0.01f, _hitRadius );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            attackRotationSpeed = Mathf.Max( 0.0f, _attackRotationSpeed );
            expirationTime = Time.time + Mathf.Max( 0.01f, _durationSeconds );
            ownerInstanceId = ownerTransform.GetInstanceID();
            ReplaceExistingRuntime();
            ResolveEquipmentManager();
            SubscribeEquipmentChanged();
            CreateCompanions( Mathf.Max( 1, _companionCount ), weaponSprite, _floatingWeaponPrefab, _floatingWeaponTrailPrefab, Mathf.Max( 0.01f, _weaponVisualScale ) );
            isInitialized = companionList.Count > 0;
            return isInitialized;
        }

        ///<summary>
        /// 소환 수명과 소유자 유효성 확인
        ///</summary>
        private void Update()
        {
            if ( isInitialized == false )
            {
                return;
            }

            if ( ownerTransform == null || Time.time >= expirationTime )
            {
                Destroy( gameObject );
            }
        }

        ///<summary>
        /// 이벤트와 활성 런타임 등록 해제
        ///</summary>
        private void OnDestroy()
        {
            UnsubscribeEquipmentChanged();

            if ( ownerInstanceId == 0 )
            {
                return;
            }

            if ( ActiveRuntimeByOwnerId.TryGetValue( ownerInstanceId, out CFloatingWeaponSummonRuntime activeRuntime ) && activeRuntime == this )
            {
                ActiveRuntimeByOwnerId.Remove( ownerInstanceId );
            }
        }

        ///<summary>
        /// 편대 위치 반환
        ///</summary>
        public Vector3 GetFormationWorldPosition( int _companionIndex, int _companionCount, float _hoverPhase )
        {
            if ( ownerTransform == null )
            {
                return transform.position;
            }

            float facingDirection = ResolveFacingDirection();
            float centerIndex = ( Mathf.Max( 1, _companionCount ) - 1 ) * 0.5f;
            float verticalOffset = ( _companionIndex - centerIndex ) * formationVerticalSpacing;
            float hoverOffset = Mathf.Sin( Time.time * hoverFrequency + _hoverPhase ) * hoverAmplitude;
            Vector3 resolvedOffset = new Vector3( formationBaseOffset.x * facingDirection, formationBaseOffset.y + verticalOffset + hoverOffset, 0.0f );
            Vector3 result = ownerTransform.position + resolvedOffset;
            return result;
        }

        ///<summary>
        /// 주변에서 다른 무기가 예약하지 않은 가장 가까운 적 결정
        ///</summary>
        public MonsterObject AcquireTarget( CFloatingWeaponCompanionRuntime _requester )
        {
            if ( ownerTransform == null )
            {
                return null;
            }

            ContactFilter2D contactFilter = CreateMonsterContactFilter();
            Vector2 searchCenter = ownerTransform.position;
            int hitCount = Physics2D.OverlapCircle( searchCenter, targetSearchRadius, contactFilter, targetBuffer );
            MonsterObject nearestUnreservedTarget = null;
            MonsterObject nearestFallbackTarget = null;
            float nearestUnreservedDistance = float.MaxValue;
            float nearestFallbackDistance = float.MaxValue;

            for ( int index = 0; index < hitCount; index++ )
            {
                Collider2D targetCollider = targetBuffer[ index ];
                MonsterObject monsterObject = ResolveMonsterObject( targetCollider );

                if ( IsValidTarget( monsterObject ) == false )
                {
                    continue;
                }

                float distance = ( monsterObject.transform.position - ownerTransform.position ).sqrMagnitude;

                if ( distance < nearestFallbackDistance )
                {
                    nearestFallbackDistance = distance;
                    nearestFallbackTarget = monsterObject;
                }

                if ( IsReservedByOtherCompanion( monsterObject, _requester ) || distance >= nearestUnreservedDistance )
                {
                    continue;
                }

                nearestUnreservedDistance = distance;
                nearestUnreservedTarget = monsterObject;
            }

            MonsterObject result = nearestUnreservedTarget != null ? nearestUnreservedTarget : nearestFallbackTarget;
            return result;
        }

        ///<summary>
        /// 소환 스킬 실행 문맥 반환
        ///</summary>
        public CSkillContext GetSkillContext()
        {
            CSkillContext result = skillContext;
            return result;
        }

        public float GetAttackIntervalSeconds()
        {
            float result = attackIntervalSeconds;
            return result;
        }

        public float GetTargetRetrySeconds()
        {
            float result = MinimumTargetRetrySeconds;
            return result;
        }

        public float GetFlightSpeed()
        {
            float result = flightSpeed;
            return result;
        }

        public float GetHitRadius()
        {
            float result = hitRadius;
            return result;
        }

        public float GetDamageMultiplier()
        {
            float result = damageMultiplier;
            return result;
        }

        public int GetFlatDamageBonus()
        {
            int result = flatDamageBonus;
            return result;
        }

        public float GetAttackRotationSpeed()
        {
            float result = attackRotationSpeed;
            return result;
        }

        public float ResolveFacingDirection()
        {
            if ( ownerTransform == null )
            {
                return 1.0f;
            }

            float result = ownerTransform.localScale.x < 0.0f ? -1.0f : 1.0f;
            return result;
        }

        ///<summary>
        /// 지정 소유자의 활성 부유 무기를 즉시 편대 위치로 이동
        ///</summary>
        public static void SnapActiveRuntimeToOwner( Transform _ownerTransform )
        {
            if ( _ownerTransform == null )
            {
                return;
            }

            int ownerTransformInstanceId = _ownerTransform.GetInstanceID();

            if ( ActiveRuntimeByOwnerId.TryGetValue( ownerTransformInstanceId, out CFloatingWeaponSummonRuntime activeRuntime ) == false || activeRuntime == null )
            {
                return;
            }

            activeRuntime.SnapCompanionsToFormation();
        }

        ///<summary>
        /// 새 시전 시 동일 플레이어의 기존 소환 제거
        ///</summary>
        private void ReplaceExistingRuntime()
        {
            if ( ActiveRuntimeByOwnerId.TryGetValue( ownerInstanceId, out CFloatingWeaponSummonRuntime existingRuntime ) && existingRuntime != null && existingRuntime != this )
            {
                Destroy( existingRuntime.gameObject );
            }

            ActiveRuntimeByOwnerId[ ownerInstanceId ] = this;
        }

        ///<summary>
        /// 무기 소환수 생성
        ///</summary>
        private void CreateCompanions( int _companionCount, Sprite _weaponSprite, GameObject _floatingWeaponPrefab, GameObject _floatingWeaponTrailPrefab, float _weaponVisualScale )
        {
            for ( int index = 0; index < _companionCount; index++ )
            {
                GameObject companionObject = Instantiate( _floatingWeaponPrefab, transform );
                companionObject.name = $"FloatingWeapon_{index + 1}";
                companionObject.transform.position = GetFormationWorldPosition( index, _companionCount, index * 1.7f );
                SpriteRenderer spriteRenderer = companionObject.GetComponent<SpriteRenderer>();

                if ( spriteRenderer == null )
                {
                    Destroy( companionObject );
                    continue;
                }

                spriteRenderer.sprite = _weaponSprite;
                spriteRenderer.sortingLayerName = "SkillEffect";
                spriteRenderer.sortingOrder = 10 + index;
                spriteRenderer.color = isReplayCloneOwner ? ReplayCloneWeaponTintColor : PlayerWeaponTintColor;
                companionObject.transform.localScale = Vector3.one * _weaponVisualScale;
                GameObject trailObject = Instantiate( _floatingWeaponTrailPrefab, companionObject.transform );
                trailObject.name = "FloatingWeaponTrail";
                trailObject.transform.localPosition = Vector3.zero;
                trailObject.transform.localRotation = Quaternion.identity;
                trailObject.transform.localScale = Vector3.one;
                TrailRenderer trailRenderer = trailObject.GetComponent<TrailRenderer>();

                if ( trailRenderer == null )
                {
                    Destroy( companionObject );
                    continue;
                }

                Color trailColor = isReplayCloneOwner ? ReplayCloneTrailColor : PlayerTrailColor;
                ApplyTrailColor( trailRenderer, trailColor );
                CFloatingWeaponCompanionRuntime companionRuntime = companionObject.AddComponent<CFloatingWeaponCompanionRuntime>();
                companionRuntime.Initialize( this, index, _companionCount, index * 1.7f, spriteRenderer, trailRenderer );
                companionList.Add( companionRuntime );
            }
        }

        private static void ApplyTrailColor( TrailRenderer _trailRenderer, Color _trailColor )
        {
            if ( _trailRenderer == null )
            {
                return;
            }

            Gradient trailGradient = new Gradient();
            GradientColorKey[] colorKeys = new[]
            {
                new GradientColorKey( _trailColor, 0.0f ),
                new GradientColorKey( _trailColor, 1.0f )
            };
            GradientAlphaKey[] alphaKeys = new[]
            {
                new GradientAlphaKey( 0.9f, 0.0f ),
                new GradientAlphaKey( 0.0f, 1.0f )
            };
            trailGradient.SetKeys( colorKeys, alphaKeys );
            _trailRenderer.colorGradient = trailGradient;
        }

        ///<summary>
        /// 모든 부유 무기를 현재 소유자 편대 위치로 즉시 복귀
        ///</summary>
        private void SnapCompanionsToFormation()
        {
            for ( int index = 0; index < companionList.Count; index++ )
            {
                CFloatingWeaponCompanionRuntime companionRuntime = companionList[ index ];

                if ( companionRuntime == null )
                {
                    continue;
                }

                companionRuntime.SnapToFormation();
            }
        }

        ///<summary>
        /// 장비 매니저 결정
        ///</summary>
        private void ResolveEquipmentManager()
        {
            PlayerController playerController = skillContext != null ? skillContext.GetPlayerController() : null;
            equipmentManager = playerController != null ? playerController.GetEquipmentManager() : null;
        }

        private void SubscribeEquipmentChanged()
        {
            if ( equipmentManager == null )
            {
                return;
            }

            equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        private void UnsubscribeEquipmentChanged()
        {
            if ( equipmentManager == null )
            {
                return;
            }

            equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        ///<summary>
        /// 장비 변경 시 소환 무기 외형 동기화
        ///</summary>
        private void HandleEquipmentChanged( CPlayerEquipmentManager _equipmentManager )
        {
            Sprite weaponSprite = null;
            CFloatingWeaponVisualUtility.TryResolveWeaponSprite( skillContext, out weaponSprite );

            for ( int index = 0; index < companionList.Count; index++ )
            {
                CFloatingWeaponCompanionRuntime companionRuntime = companionList[ index ];

                if ( companionRuntime == null )
                {
                    continue;
                }

                companionRuntime.SetWeaponSprite( weaponSprite );
            }
        }

        private ContactFilter2D CreateMonsterContactFilter()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useLayerMask = true;
            contactFilter.useTriggers = true;
            contactFilter.layerMask = LayerMask.GetMask( "Monster" );
            return contactFilter;
        }

        private bool IsReservedByOtherCompanion( MonsterObject _monsterObject, CFloatingWeaponCompanionRuntime _requester )
        {
            for ( int index = 0; index < companionList.Count; index++ )
            {
                CFloatingWeaponCompanionRuntime companionRuntime = companionList[ index ];

                if ( companionRuntime == null || companionRuntime == _requester )
                {
                    continue;
                }

                if ( companionRuntime.GetTarget() == _monsterObject )
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsValidTarget( MonsterObject _monsterObject )
        {
            bool result = _monsterObject != null && _monsterObject.gameObject.activeInHierarchy && _monsterObject.GetCurrentHp() > 0;
            return result;
        }

        private MonsterObject ResolveMonsterObject( Collider2D _targetCollider )
        {
            if ( _targetCollider == null )
            {
                return null;
            }

            MonsterObject monsterObject = _targetCollider.GetComponent<MonsterObject>();

            if ( monsterObject != null )
            {
                return monsterObject;
            }

            MonsterObject result = _targetCollider.GetComponentInParent<MonsterObject>();
            return result;
        }
    }
}
