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
        public static long ResolvePlayerSkillDamage( CSkillContext _skillContext, MonsterObject _monsterObject, float _damageMultiplier, int _flatDamageBonus, out bool _isCritical )
        {
            _isCritical = false;

            if ( _skillContext == null || _monsterObject == null )
            {
                return 0;
            }

            CPlayerStatManager playerStatManager = _skillContext.GetPlayerStatManager();

            if ( playerStatManager == null )
            {
                return 0;
            }

            float attackStatOverride = _skillContext.GetAttackStatOverride();
            float playerAtk = attackStatOverride >= 0.0f ? attackStatOverride : playerStatManager.GetFinalStatValue( ePlayerStatType.ATK );
            PlayerController playerController = _skillContext.GetPlayerController();
            float attackMultiplierOverride = _skillContext.GetSkillAttackPowerMultiplierOverride();
            float attackMultiplier = attackMultiplierOverride >= 0.0f ? attackMultiplierOverride : ( playerController != null ? playerController.GetSkillAttackPowerMultiplier() : 1.0f );
            float rawDamage = playerAtk * attackMultiplier * _damageMultiplier + _flatDamageBonus - _monsterObject.GetDef();
            float resolvedDamage = CPlayerCombatStatUtility.ResolveCombatDamage( playerStatManager, rawDamage, out bool isCritical );
            _isCritical = isCritical;
            long damage = Mathf.Max( 0, Mathf.RoundToInt( resolvedDamage ) );
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

            PlayerController playerController = _skillContext.GetPlayerController();

            if ( playerController == null )
            {
                return;
            }

            _monsterObject.TryGrantReward( playerController );
        }
    }
}
