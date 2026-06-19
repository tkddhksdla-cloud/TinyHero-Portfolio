using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 분신 지연 재생 액티브 스킬 이펙트
    ///</summary>
    [CreateAssetMenu( fileName = "CloneReplayActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Clone Replay" )]
    public sealed class CCloneReplayActiveSkillEffect : CActiveSkillEffectBase
    {
        [SerializeField] private float durationSeconds = 6.0f;
        [SerializeField] private float followDelaySeconds = 0.45f;
        [SerializeField] private float cloneDamageMultiplier = 0.65f;
        [SerializeField] private Vector3 replayOffset = new Vector3( -0.35f, 0.0f, 0.0f );
        [SerializeField] private float previewRadius = 0.8f;

        ///<summary>
        /// 분신 지연 재생 이펙트 구성
        ///</summary>
        public void Configure( float _durationSeconds, float _followDelaySeconds, float _cloneDamageMultiplier, Vector3 _replayOffset, float _previewRadius )
        {
            durationSeconds = Mathf.Max( 0.1f, _durationSeconds );
            followDelaySeconds = Mathf.Max( 0.0f, _followDelaySeconds );
            cloneDamageMultiplier = Mathf.Max( 0.0f, _cloneDamageMultiplier );
            replayOffset = _replayOffset;
            previewRadius = Mathf.Max( 0.1f, _previewRadius );
            SetSimultaneousTargetCount( 1 );
        }

        ///<summary>
        /// 액티브 스킬 타입 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.CLONE;
            return result;
        }

        ///<summary>
        /// 분신 스킬 실행 가능 여부 판정
        ///</summary>
        public override bool CanExecute( CSkillContext _skillContext )
        {
            bool canExecuteBase = base.CanExecute( _skillContext );

            if ( canExecuteBase == false )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();
            bool result = playerController != null;
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

            _previewData.isValid = true;
            _previewData.shapeType = eSkillToolRangePreviewShape.CIRCLE;
            _previewData.worldCenterPosition = _ownerTransform.position;
            _previewData.radius = previewRadius;
            return true;
        }

        ///<summary>
        /// 툴 미리보기 표시 시간 반환
        ///</summary>
        public override float GetToolPreviewDurationSeconds()
        {
            float result = Mathf.Max( durationSeconds, base.GetToolPreviewDurationSeconds() );
            return result;
        }

        ///<summary>
        /// 분신 스킬 실행 처리
        ///</summary>
        public override bool Execute( CSkillContext _skillContext )
        {
            if ( CanExecute( _skillContext ) == false )
            {
                return false;
            }

            PlayerController playerController = _skillContext.GetPlayerController();

            if ( playerController == null )
            {
                return false;
            }

            CPlayerCloneRecorder cloneRecorder = playerController.GetComponent<CPlayerCloneRecorder>();

            if ( cloneRecorder == null )
            {
                cloneRecorder = playerController.gameObject.AddComponent<CPlayerCloneRecorder>();
            }

            CSkillVfxUtility.PlayCastVfx( _skillContext );
            GameObject cloneRuntimeObject = new GameObject( "ReplayCloneRuntime" );
            CPlayerReplayCloneRuntime cloneRuntime = cloneRuntimeObject.AddComponent<CPlayerReplayCloneRuntime>();
            cloneRuntime.Initialize( playerController, cloneRecorder, durationSeconds, followDelaySeconds, replayOffset, cloneDamageMultiplier );
            return true;
        }
    }
}
