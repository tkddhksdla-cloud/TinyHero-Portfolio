using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 액티브 스킬 실행 정의 베이스
    ///</summary>
    public abstract class CSkillActionBase : ScriptableObject
    {
        ///<summary>
        /// 스킬 실행 가능 여부 판정
        ///</summary>
        public virtual bool CanExecute( CSkillContext _skillContext )
        {
            bool result = _skillContext != null && _skillContext.GetOwnerTransform() != null;
            return result;
        }

        ///<summary>
        /// 스킬 실행 처리
        ///</summary>
        public abstract bool Execute( CSkillContext _skillContext );
    }
}
