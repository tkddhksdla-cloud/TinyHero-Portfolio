using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 피해 및 처치 보상 보조 유틸리티
    ///</summary>
    public static class CSkillDamageUtility
    {
        ///<summary>
        /// 플레이어 기반 스킬 피해량 계산
        ///</summary>
        public static long ResolvePlayerSkillDamage( CSkillContext _skillContext, MonsterObject _monsterObject, float _damageMultiplier, int _flatDamageBonus )
        {
            if ( _skillContext == null || _monsterObject == null )
            {
                return 0;
            }

            CPlayerStatManager playerStatManager = _skillContext.GetPlayerStatManager();

            if ( playerStatManager == null )
            {
                return 0;
            }

            float playerAtk = playerStatManager.GetFinalStatValue( ePlayerStatType.ATK );
            PlayerController playerController = _skillContext.GetPlayerController();
            float attackMultiplier = playerController != null ? playerController.GetSkillAttackPowerMultiplier() : 1.0f;
            float rawDamage = playerAtk * attackMultiplier * _damageMultiplier + _flatDamageBonus - _monsterObject.GetDef();
            long damage = Mathf.Max( 0, Mathf.RoundToInt( rawDamage ) );
            return damage;
        }

        ///<summary>
        /// 스킬 처치 경험치 지급 처리
        ///</summary>
        public static void TryAwardMonsterExp( CSkillContext _skillContext, MonsterObject _monsterObject, bool _wasAliveBeforeHit )
        {
            if ( _skillContext == null || _monsterObject == null )
            {
                return;
            }

            if ( _wasAliveBeforeHit == false )
            {
                return;
            }

            if ( _monsterObject.GetCurrentHp() > 0 )
            {
                return;
            }

            CPlayerStatManager playerStatManager = _skillContext.GetPlayerStatManager();

            if ( playerStatManager == null )
            {
                return;
            }

            long expReward = _monsterObject.GetExpReward();

            if ( expReward <= 0 )
            {
                return;
            }

            playerStatManager.AddExp( expReward );
        }
    }
}
