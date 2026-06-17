using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 버프 액티브 스킬 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "BuffActiveSkillEffect", menuName = "TinyHero/Skill/Effect/Active/Buff" )]
    public sealed class CBuffActiveSkillEffect : CActiveSkillEffectBase, ISerializationCallbackReceiver
    {
        private const int BuffTargetCount = 1;

        [SerializeField] private List<CPlayerBuffEffectBase> playerBuffEffectList = new List<CPlayerBuffEffectBase>();

        ///<summary>
        /// 버프 효과 목록 설정
        ///</summary>
        public void SetBuffEffects( List<CPlayerBuffEffectBase> _playerBuffEffectList )
        {
            playerBuffEffectList = _playerBuffEffectList != null ? _playerBuffEffectList : new List<CPlayerBuffEffectBase>();
        }

        ///<summary>
        /// 액티브 스킬 세부 분류 반환
        ///</summary>
        public override eActiveSkillType GetActiveSkillType()
        {
            eActiveSkillType result = eActiveSkillType.BUFF;
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
            _previewData.radius = DefaultSelfPreviewRadius;
            return true;
        }

        ///<summary>
        /// 툴 미리보기 표시 시간 반환
        ///</summary>
        public override float GetToolPreviewDurationSeconds()
        {
            float previewDurationSeconds = 1.25f;
            return previewDurationSeconds;
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

            SetSimultaneousTargetCount( BuffTargetCount );
            CSkillVfxUtility.PlayCastVfx( _skillContext );
            bool didApplyAnyBuff = false;

            for ( int index = 0; index < playerBuffEffectList.Count; index++ )
            {
                CPlayerBuffEffectBase playerBuffEffect = playerBuffEffectList[ index ];

                if ( playerBuffEffect == null )
                {
                    continue;
                }

                bool didApplyBuff = playerBuffEffect.ApplyBuff( _skillContext );

                if ( didApplyBuff )
                {
                    didApplyAnyBuff = true;
                }
            }

            if ( didApplyAnyBuff )
            {
                Transform ownerTransform = _skillContext.GetOwnerTransform();
                CSkillVfxUtility.PlayLoopVfx( _skillContext, ownerTransform, 0.0f );
            }

            return didApplyAnyBuff;
        }

        ///<summary>
        /// 직렬화 이전 버프 대상 수 고정
        ///</summary>
        public void OnBeforeSerialize()
        {
            SetSimultaneousTargetCount( BuffTargetCount );
        }

        ///<summary>
        /// 역직렬화 이후 버프 대상 수 고정
        ///</summary>
        public void OnAfterDeserialize()
        {
            SetSimultaneousTargetCount( BuffTargetCount );
        }
    }
}
