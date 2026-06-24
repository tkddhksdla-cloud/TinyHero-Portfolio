using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 에어본 군중제어 효과 정의
    ///</summary>
    [CreateAssetMenu( fileName = "AirborneCrowdControlEffect", menuName = "TinyHero/Skill/Effect/Crowd Control/Airborne" )]
    public sealed class CAirborneCrowdControlEffect : CEnemyCrowdControlEffectBase
    {
        [SerializeField] private float height = 1.2f;

        ///<summary>
        /// 에어본 높이 반환
        ///</summary>
        public float GetHeight()
        {
            float result = Mathf.Max( 0.0f, height );
            return result;
        }

        ///<summary>
        /// 에어본 군중제어 적용 처리
        ///</summary>
        protected override bool ApplyCrowdControl( CSkillContext _skillContext, MonsterObject _monsterObject )
        {
            float duration = GetDurationSeconds();
            float airborneHeight = GetHeight();

            if ( duration <= 0.0f || airborneHeight <= 0.0f )
            {
                return false;
            }

            _monsterObject.ApplyAirborneCrowdControl( duration, airborneHeight, GetCrowdControlVfxPrefab(), GetCrowdControlVfxOffset() );
            return true;
        }
    }
}
