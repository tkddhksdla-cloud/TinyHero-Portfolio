using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 액티브 스킬 효과 베이스 정의
    ///</summary>
    public abstract class CActiveSkillEffectBase : ScriptableObject
    {
        protected const int DefaultSimultaneousTargetCount = 16;
        protected const float DefaultToolPreviewDurationSeconds = 0.9f;
        protected const float DefaultSelfPreviewRadius = 0.75f;

        [SerializeField] private int simultaneousTargetCount = DefaultSimultaneousTargetCount;
        [SerializeField] private float damageStartDelaySeconds;

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public abstract eActiveSkillType GetActiveSkillType();

        ///<summary>
        /// 동시 영향 대상 수 반환
        ///</summary>
        public int GetSimultaneousTargetCount()
        {
            int result = Mathf.Max( 1, simultaneousTargetCount );
            return result;
        }

        ///<summary>
        /// 데미지 시작 지연 시간 반환
        ///</summary>
        public float GetDamageStartDelaySeconds()
        {
            float result = Mathf.Max( 0.0f, damageStartDelaySeconds );
            return result;
        }

        ///<summary>
        /// 스킬 실행 가능 여부 판정
        ///</summary>
        public virtual bool CanExecute( CSkillContext _skillContext )
        {
            bool result = _skillContext != null && _skillContext.GetOwnerTransform() != null;
            return result;
        }

        ///<summary>
        /// 툴 미리보기 범위 데이터 반환
        ///</summary>
        public virtual bool TryGetToolRangePreviewData( Transform _ownerTransform, out CSkillToolRangePreviewData _previewData )
        {
            _previewData = default;
            return false;
        }

        ///<summary>
        /// 툴 미리보기 표시 시간 반환
        ///</summary>
        public virtual float GetToolPreviewDurationSeconds()
        {
            float previewDurationSeconds = Mathf.Max( DefaultToolPreviewDurationSeconds, GetDamageStartDelaySeconds() );
            return previewDurationSeconds;
        }

        ///<summary>
        /// 스킬 실행 처리
        ///</summary>
        public abstract bool Execute( CSkillContext _skillContext );

        ///<summary>
        /// 동시 영향 대상 수 설정
        ///</summary>
        protected void SetSimultaneousTargetCount( int _simultaneousTargetCount )
        {
            simultaneousTargetCount = Mathf.Max( 1, _simultaneousTargetCount );
        }

        ///<summary>
        /// 데미지 시작 지연 시간 설정
        ///</summary>
        protected void SetDamageStartDelaySeconds( float _damageStartDelaySeconds )
        {
            damageStartDelaySeconds = Mathf.Max( 0.0f, _damageStartDelaySeconds );
        }

        ///<summary>
        /// 동시 영향 대상 설정값 반환
        ///</summary>
        protected int GetConfiguredSimultaneousTargetCount()
        {
            int result = simultaneousTargetCount;
            return result;
        }
    }
}
