using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 오디오 재생 유틸리티
    ///</summary>
    public static class CSkillAudioUtility
    {
        ///<summary>
        /// 스킬 시전 효과음 재생
        ///</summary>
        public static void PlayCastSfx( CSkillContext _skillContext )
        {
            CSkillDefinition skillDefinition = ResolveSkillDefinition( _skillContext );

            if ( skillDefinition == null )
            {
                return;
            }

            PlaySfx( skillDefinition.GetCastSfxClipName() );
        }

        ///<summary>
        /// 스킬 타격 효과음 재생
        ///</summary>
        public static void PlayHitSfx( CSkillContext _skillContext )
        {
            CSkillDefinition skillDefinition = ResolveSkillDefinition( _skillContext );

            if ( skillDefinition == null )
            {
                return;
            }

            PlaySfx( skillDefinition.GetHitSfxClipName() );
        }

        ///<summary>
        /// 스킬 지속 루프 효과음 재생
        ///</summary>
        public static CAudioLoopSfxHandle PlayLoopSfx( CSkillContext _skillContext, Transform _parentTransform )
        {
            CSkillDefinition skillDefinition = ResolveSkillDefinition( _skillContext );

            if ( skillDefinition == null )
            {
                return null;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return null;
            }

            CAudioLoopSfxHandle result = audioManager.PlayLoopSfx( skillDefinition.GetLoopSfxClipName(), _parentTransform );
            return result;
        }

        ///<summary>
        /// 스킬 정의 반환
        ///</summary>
        private static CSkillDefinition ResolveSkillDefinition( CSkillContext _skillContext )
        {
            if ( _skillContext == null )
            {
                return null;
            }

            CSkillDefinition result = _skillContext.GetSkillDefinition();
            return result;
        }

        ///<summary>
        /// 효과음 재생
        ///</summary>
        private static void PlaySfx( string _clipName )
        {
            if ( string.IsNullOrWhiteSpace( _clipName ) )
            {
                return;
            }

            CAudioManager audioManager = CAudioManager.Instance;

            if ( audioManager == null )
            {
                return;
            }

            audioManager.PlaySfx( _clipName );
        }
    }
}
