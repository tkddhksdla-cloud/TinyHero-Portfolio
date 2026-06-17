namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 사용 판정 결과
    ///</summary>
    public enum eSkillUseResult
    {
        SUCCESS,
        INVALID_SKILL,
        LOCKED,
        PASSIVE_SKILL,
        COOLDOWN,
        NOT_ENOUGH_MP,
        MISSING_ACTION,
        BLOCKED
    }
}
