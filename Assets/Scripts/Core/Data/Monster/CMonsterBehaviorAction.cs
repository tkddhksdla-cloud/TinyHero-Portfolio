namespace TinyHero.Core.Data
{
    ///<summary>
    /// 몬스터 행동 종류 열거형
    ///</summary>
    public enum eMonsterBehaviorAction
    {
        IDLE,
        WANDER,
        TELEPORT_TO_PLAYER,
        TRACE_PLAYER,
        LOOK_PLAYER,
        ATTACK,
        SKILL
    }
}
