using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 페이즈 스트라이크 액티브 스킬 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "PhaseStrikeActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Phase Strike" )]
    public sealed class CPhaseStrikeActiveSkillEffect : CActiveSkillEffectBase
    {
        [SerializeField] private int hitCount = 10;
        [SerializeField] private float hitIntervalSeconds = 0.15f;
        [SerializeField] private float damageMultiplier = 1.1f;
        [SerializeField] private int flatDamageBonus;
        [SerializeField] private List<CEnemyDebuffEffectBase> debuffEffectList = new List<CEnemyDebuffEffectBase>();
        [SerializeField] private List<CEnemyCrowdControlEffectBase> crowdControlEffectList = new List<CEnemyCrowdControlEffectBase>();

        ///<summary>
        /// 페이즈 스트라이크 효과 데이터 구성
        ///</summary>
        public void Configure( int _hitCount, float _hitIntervalSeconds, float _damageMultiplier, int _flatDamageBonus )
        {
            hitCount = Mathf.Max( 1, _hitCount );
            hitIntervalSeconds = Mathf.Max( 0.01f, _hitIntervalSeconds );
            damageMultiplier = Mathf.Max( 0.0f, _damageMultiplier );
            flatDamageBonus = _flatDamageBonus;
            SetSimultaneousTargetCount( 1 );
        }

        ///<summary>
        /// 타격 횟수 반환
        ///</summary>
        public int GetHitCount()
        {
            int result = Mathf.Max( 1, hitCount );
            return result;
        }

        ///<summary>
        /// 타격 간격 반환
        ///</summary>
        public float GetHitIntervalSeconds()
        {
            float result = Mathf.Max( 0.01f, hitIntervalSeconds );
            return result;
        }

        ///<summary>
        /// 총 지속 시간 반환
        ///</summary>
        public float GetDurationSeconds()
        {
            int resolvedHitCount = GetHitCount();
            float resolvedHitIntervalSeconds = GetHitIntervalSeconds();
            float result = Mathf.Max( 0, resolvedHitCount - 1 ) * resolvedHitIntervalSeconds;
            return result;
        }

        ///<summary>
        /// 기본 데미지 배수 반환
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
        /// 군중제어 효과 목록 설정
        ///</summary>
        public void SetCrowdControlEffects( List<CEnemyCrowdControlEffectBase> _crowdControlEffectList )
        {
            crowdControlEffectList = _crowdControlEffectList != null ? _crowdControlEffectList : new List<CEnemyCrowdControlEffectBase>();
        }

        ///<summary>
        /// 군중제어 효과 목록 반환
        ///</summary>
        public List<CEnemyCrowdControlEffectBase> GetCrowdControlEffects()
        {
            List<CEnemyCrowdControlEffectBase> result = crowdControlEffectList;
            return result;
        }

        ///<summary>
        /// 액티브 스킬 타입 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.PHASE_STRIKE;
            return result;
        }

        ///<summary>
        /// 페이즈 스트라이크 실행 가능 여부 판정
        ///</summary>
        public override bool CanExecute( CSkillContext _skillContext )
        {
            bool canExecuteBase = base.CanExecute( _skillContext );

            if ( canExecuteBase == false )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();

            if ( playerController == null )
            {
                return false;
            }

            bool hasTarget = CPhaseStrikeSkillRuntime.HasAnyVisibleMonsterTarget();
            return hasTarget;
        }

        ///<summary>
        /// 미리보기 범위 데이터 반환
        ///</summary>
        public override bool TryGetToolRangePreviewData( Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;

            if ( _ownerTransform == null )
            {
                return false;
            }

            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = _ownerTransform.position;
            _previewData.radius = DefaultSelfPreviewRadius;
            return true;
        }

        ///<summary>
        /// 미리보기 표시 시간 반환
        ///</summary>
        public override float GetToolPreviewDurationSeconds()
        {
            float result = Mathf.Max( GetDurationSeconds(), base.GetToolPreviewDurationSeconds() );
            return result;
        }

        ///<summary>
        /// 페이즈 스트라이크 실행 처리
        ///</summary>
        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            CSkillManager skillManager = _skillContext.GetSkillManager();
            PlayerController playerController = _skillContext.GetPlayerController();

            if ( skillManager == null || playerController == null )
            {
                return false;
            }

            CSkillVfxUtility.PlayCastVfx( _skillContext );
            GameObject runtimeObject = new GameObject( "PhaseStrikeSkillRuntime" );
            CPhaseStrikeSkillRuntime phaseStrikeSkillRuntime = runtimeObject.AddComponent<CPhaseStrikeSkillRuntime>();
            phaseStrikeSkillRuntime.Initialize( _skillContext, GetHitCount(), GetHitIntervalSeconds(), damageMultiplier, flatDamageBonus, debuffEffectList, crowdControlEffectList );
            return true;
        }
    }
}
