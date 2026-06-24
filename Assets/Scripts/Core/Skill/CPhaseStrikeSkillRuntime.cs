using System.Collections;
using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 페이즈 스트라이크 런타임 처리 컴포넌트
    ///</summary>
    public sealed class CPhaseStrikeSkillRuntime : MonoBehaviour
    {
        private struct CMonsterTargetEntry
        {
            public MonsterObject monsterObject;
            public float sortX;
            public int instanceId;
        }

        private readonly List<CMonsterTargetEntry> visibleTargetList = new List<CMonsterTargetEntry>();
        private readonly List<SpriteRenderer> hiddenSpriteRendererList = new List<SpriteRenderer>();
        private readonly List<bool> hiddenSpriteRendererEnabledStateList = new List<bool>();

        private CSkillContext skillContext;
        private Transform ownerTransform;
        private PlayerController playerController;
        private Vector3 castPosition;
        private float damageMultiplier;
        private float hitIntervalSeconds;
        private int flatDamageBonus;
        private int hitCount;
        private int previousTargetInstanceId;
        private float previousTargetSortX;
        private bool isInitialized;
        private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
        private List<CEnemyCrowdControlEffectBase> crowdControlEffectList = new List<CEnemyCrowdControlEffectBase>();

        ///<summary>
        /// 화면 내 타격 가능 대상 존재 여부 반환
        ///</summary>
        public static bool HasAnyVisibleMonsterTarget()
        {
            List<CMonsterTargetEntry> visibleMonsterTargetList = BuildVisibleMonsterTargetList();
            bool result = visibleMonsterTargetList.Count > 0;
            return result;
        }

        ///<summary>
        /// 페이즈 스트라이크 런타임 초기화
        ///</summary>
        public void Initialize( CSkillContext _skillContext, int _hitCount, float _hitIntervalSeconds, float _damageMultiplier, int _flatDamageBonus, List<CEnemyDebuffEffectBase> _debuffEffectList, List<CEnemyCrowdControlEffectBase> _crowdControlEffectList )
        {
            skillContext = _skillContext;
            ownerTransform = _skillContext != null ? _skillContext.GetOwnerTransform() : null;
            playerController = _skillContext != null ? _skillContext.GetPlayerController() : null;
            castPosition = ownerTransform != null ? ownerTransform.position : Vector3.zero;
            hitCount = Mathf.Max( 1, _hitCount );
            hitIntervalSeconds = Mathf.Max( 0.01f, _hitIntervalSeconds );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            previousTargetInstanceId = 0;
            previousTargetSortX = float.MinValue;
            debuffEffectList = _debuffEffectList != null ? _debuffEffectList : new List<CEnemyDebuffEffectBase>();
            crowdControlEffectList = _crowdControlEffectList != null ? _crowdControlEffectList : new List<CEnemyCrowdControlEffectBase>();
            isInitialized = true;
            StartCoroutine( IE_ExecutePhaseStrike() );
        }

        ///<summary>
        /// 런타임 종료 시 플레이어 상태 복원
        ///</summary>
        private void OnDestroy()
        {
            RestorePlayerState();
        }

        ///<summary>
        /// 페이즈 스트라이크 순차 타격 코루틴
        ///</summary>
        private IEnumerator IE_ExecutePhaseStrike()
        {
            if ( isInitialized == false || ownerTransform == null )
            {
                Destroy( gameObject );
                yield break;
            }

            EnterPhaseStrikeVisualState();

            for ( int hitIndex = 0; hitIndex < hitCount; hitIndex++ )
            {
                MonsterObject targetMonsterObject = ResolveNextVisibleTarget();

                if ( targetMonsterObject == null )
                {
                    break;
                }

                ApplyDamageToTarget( targetMonsterObject );

                if ( hitIndex < hitCount - 1 )
                {
                    yield return new WaitForSeconds( hitIntervalSeconds );
                }
            }

            RestorePlayerState();
            Destroy( gameObject );
        }

        ///<summary>
        /// 다음 화면 내 타격 대상 결정
        ///</summary>
        private MonsterObject ResolveNextVisibleTarget()
        {
            visibleTargetList.Clear();
            List<CMonsterTargetEntry> resolvedVisibleTargetList = BuildVisibleMonsterTargetList();
            visibleTargetList.AddRange( resolvedVisibleTargetList );

            if ( visibleTargetList.Count == 0 )
            {
                return null;
            }

            int selectedIndex = ResolveNextTargetIndex( visibleTargetList );
            CMonsterTargetEntry targetEntry = visibleTargetList[ selectedIndex ];
            previousTargetInstanceId = targetEntry.instanceId;
            previousTargetSortX = targetEntry.sortX;
            MonsterObject result = targetEntry.monsterObject;
            return result;
        }

        ///<summary>
        /// 다음 타격 대상 인덱스 계산
        ///</summary>
        private int ResolveNextTargetIndex( List<CMonsterTargetEntry> _visibleTargetList )
        {
            if ( _visibleTargetList == null || _visibleTargetList.Count == 0 )
            {
                return 0;
            }

            if ( previousTargetInstanceId == 0 )
            {
                return 0;
            }

            for ( int index = 0; index < _visibleTargetList.Count; index++ )
            {
                CMonsterTargetEntry targetEntry = _visibleTargetList[ index ];

                if ( targetEntry.instanceId != previousTargetInstanceId )
                {
                    continue;
                }

                int nextIndex = ( index + 1 ) % _visibleTargetList.Count;
                return nextIndex;
            }

            for ( int index = 0; index < _visibleTargetList.Count; index++ )
            {
                CMonsterTargetEntry targetEntry = _visibleTargetList[ index ];

                if ( targetEntry.sortX <= previousTargetSortX )
                {
                    continue;
                }

                return index;
            }

            return 0;
        }

        ///<summary>
        /// 단일 대상 피해 적용
        ///</summary>
        private void ApplyDamageToTarget( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null || _monsterObject.GetCurrentHp() <= 0 )
            {
                return;
            }

            bool wasAliveBeforeHit = _monsterObject.GetCurrentHp() > 0;
            long damage = CSkillDamageUtility.ResolvePlayerSkillDamage( skillContext, _monsterObject, damageMultiplier, flatDamageBonus, out bool isCritical );
            _monsterObject.TakeDamage( damage, isCritical );
            CSkillVfxUtility.PlayHitVfx( skillContext, _monsterObject.transform );
            ApplyDebuffs( _monsterObject );
            ApplyCrowdControls( _monsterObject );
            CSkillDamageUtility.TryAwardMonsterExp( skillContext, _monsterObject, wasAliveBeforeHit );
        }

        ///<summary>
        /// 디버프 효과 적용
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
        /// 플레이어 상태 복원
        ///</summary>
        private void RestorePlayerState()
        {
            if ( ownerTransform == null )
            {
                return;
            }

            bool shouldControlPlayerState = ShouldControlPlayerState();

            if ( shouldControlPlayerState )
            {
                playerController.EndPhaseStrikeState( castPosition );
                playerController = null;
                ownerTransform = null;
                return;
            }

            RestoreOwnerSpriteRendererState();
            playerController = null;
            ownerTransform = null;
        }

        ///<summary>
        /// 페이즈 스트라이크 시각 상태 진입
        ///</summary>
        private void EnterPhaseStrikeVisualState()
        {
            bool shouldControlPlayerState = ShouldControlPlayerState();

            if ( shouldControlPlayerState )
            {
                playerController.BeginPhaseStrikeState();
                return;
            }

            CacheOwnerSpriteRendererState();
            ApplyOwnerSpriteRendererVisible( false );
        }

        ///<summary>
        /// 원본 플레이어 제어 여부 판정
        ///</summary>
        private bool ShouldControlPlayerState()
        {
            if ( playerController == null || ownerTransform == null )
            {
                return false;
            }

            bool result = ownerTransform == playerController.transform;
            return result;
        }

        ///<summary>
        /// 실행 주체 스프라이트 상태 캐시
        ///</summary>
        private void CacheOwnerSpriteRendererState()
        {
            hiddenSpriteRendererList.Clear();
            hiddenSpriteRendererEnabledStateList.Clear();

            if ( ownerTransform == null )
            {
                return;
            }

            SpriteRenderer[] spriteRendererArray = ownerTransform.GetComponentsInChildren<SpriteRenderer>( true );

            for ( int index = 0; index < spriteRendererArray.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRendererArray[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                hiddenSpriteRendererList.Add( spriteRenderer );
                hiddenSpriteRendererEnabledStateList.Add( spriteRenderer.enabled );
            }
        }

        ///<summary>
        /// 실행 주체 스프라이트 표시 상태 적용
        ///</summary>
        private void ApplyOwnerSpriteRendererVisible( bool _isVisible )
        {
            for ( int index = 0; index < hiddenSpriteRendererList.Count; index++ )
            {
                SpriteRenderer spriteRenderer = hiddenSpriteRendererList[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                spriteRenderer.enabled = _isVisible;
            }
        }

        ///<summary>
        /// 실행 주체 스프라이트 상태 복원
        ///</summary>
        private void RestoreOwnerSpriteRendererState()
        {
            for ( int index = 0; index < hiddenSpriteRendererList.Count; index++ )
            {
                SpriteRenderer spriteRenderer = hiddenSpriteRendererList[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                bool previousEnabledState = index < hiddenSpriteRendererEnabledStateList.Count
                    ? hiddenSpriteRendererEnabledStateList[ index ]
                    : true;
                spriteRenderer.enabled = previousEnabledState;
            }

            hiddenSpriteRendererList.Clear();
            hiddenSpriteRendererEnabledStateList.Clear();
        }

        ///<summary>
        /// 화면 내 생존 몬스터 목록 생성
        ///</summary>
        private static List<CMonsterTargetEntry> BuildVisibleMonsterTargetList()
        {
            List<CMonsterTargetEntry> result = new List<CMonsterTargetEntry>();
            Camera targetCamera = Camera.main;

            if ( targetCamera == null )
            {
                return result;
            }

            MonsterObject[] monsterObjectArray = FindObjectsByType<MonsterObject>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );

            for ( int index = 0; index < monsterObjectArray.Length; index++ )
            {
                MonsterObject monsterObject = monsterObjectArray[ index ];

                if ( monsterObject == null || monsterObject.GetCurrentHp() <= 0 )
                {
                    continue;
                }

                Transform monsterTransform = monsterObject.transform;

                if ( monsterTransform == null )
                {
                    continue;
                }

                Vector3 viewportPosition = targetCamera.WorldToViewportPoint( monsterTransform.position );
                bool isVisible = viewportPosition.z > 0.0f
                    && viewportPosition.x >= 0.0f
                    && viewportPosition.x <= 1.0f
                    && viewportPosition.y >= 0.0f
                    && viewportPosition.y <= 1.0f;

                if ( isVisible == false )
                {
                    continue;
                }

                CMonsterTargetEntry targetEntry = new CMonsterTargetEntry
                {
                    monsterObject = monsterObject,
                    sortX = monsterTransform.position.x,
                    instanceId = monsterObject.GetInstanceID()
                };
                result.Add( targetEntry );
            }

            result.Sort( CompareMonsterTargetEntry );
            return result;
        }

        ///<summary>
        /// 몬스터 정렬 기준 비교
        ///</summary>
        private static int CompareMonsterTargetEntry( CMonsterTargetEntry _left, CMonsterTargetEntry _right )
        {
            int compareResult = _left.sortX.CompareTo( _right.sortX );

            if ( compareResult != 0 )
            {
                return compareResult;
            }

            int result = _left.instanceId.CompareTo( _right.instanceId );
            return result;
        }
    }
}
