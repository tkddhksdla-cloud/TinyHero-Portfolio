using UnityEngine;

namespace TinyHero.Player
{
    ///<summary>
    /// 플레이어 전투 스탯 계산 유틸리티
    ///</summary>
    public static class CPlayerCombatStatUtility
    {
        private const float PercentToRate = 0.01f;
        private const float BaseDamageMultiplier = 1.0f;
        private const float BaseCriticalDamageMultiplier = 1.0f;
        private const float BaseMinDamageModifier = -0.5f;
        private const float BaseMaxDamageModifier = 0.05f;
        private const float AccuracyMinDamageModifierPerPoint = 0.005f;
        private const float AccuracyMaxDamageModifierPerPoint = 0.002f;

        ///<summary>
        /// 전투 보정 적용 최종 피해량 계산
        ///</summary>
        public static float ResolveCombatDamage( CPlayerStatManager _playerStatManager, float _baseDamage, out bool _isCritical )
        {
            float clampedBaseDamage = Mathf.Max( 0.0f, _baseDamage );
            float criticalAppliedDamage = ResolveCriticalDamage( _playerStatManager, clampedBaseDamage, out bool isCritical );
            float accuracyAdjustedDamage = ResolveAccuracyAdjustedDamage( _playerStatManager, criticalAppliedDamage );
            float minimumAdjustedDamage = Mathf.Max( 1.0f, accuracyAdjustedDamage );
            _isCritical = isCritical;
            return minimumAdjustedDamage;
        }

        ///<summary>
        /// 크리티컬 적용 최종 피해량 계산
        ///</summary>
        public static float ResolveCriticalDamage( CPlayerStatManager _playerStatManager, float _baseDamage, out bool _isCritical )
        {
            _isCritical = false;
            float clampedBaseDamage = Mathf.Max( 0.0f, _baseDamage );

            if ( _playerStatManager == null )
            {
                return clampedBaseDamage;
            }

            bool isCritical = RollCriticalHit( _playerStatManager );

            if ( isCritical == false )
            {
                return clampedBaseDamage;
            }

            float criticalDamageMultiplier = ResolveCriticalDamageMultiplier( _playerStatManager );
            float criticalDamage = clampedBaseDamage * criticalDamageMultiplier;
            _isCritical = true;
            return criticalDamage;
        }

        ///<summary>
        /// 크리티컬 확률 판정
        ///</summary>
        public static bool RollCriticalHit( CPlayerStatManager _playerStatManager )
        {
            if ( _playerStatManager == null )
            {
                return false;
            }

            float criticalChancePercent = Mathf.Max( 0.0f, _playerStatManager.GetFinalStatValue( ePlayerStatType.CRT ) );
            float criticalChanceRate = Mathf.Clamp01( criticalChancePercent * PercentToRate );
            bool result = Random.value < criticalChanceRate;
            return result;
        }

        ///<summary>
        /// 크리티컬 피해 배율 계산
        ///</summary>
        public static float ResolveCriticalDamageMultiplier( CPlayerStatManager _playerStatManager )
        {
            if ( _playerStatManager == null )
            {
                return BaseCriticalDamageMultiplier;
            }

            float criticalDamagePercent = Mathf.Max( 0.0f, _playerStatManager.GetFinalStatValue( ePlayerStatType.CRD ) );
            float result = BaseCriticalDamageMultiplier + criticalDamagePercent * PercentToRate;
            return result;
        }

        ///<summary>
        /// 정확도 보정 최종 피해량 계산
        ///</summary>
        public static float ResolveAccuracyAdjustedDamage( CPlayerStatManager _playerStatManager, float _baseDamage )
        {
            float clampedBaseDamage = Mathf.Max( 0.0f, _baseDamage );

            if ( _playerStatManager == null )
            {
                return clampedBaseDamage;
            }

            float minDamageModifier = ResolveMinimumDamageModifier( _playerStatManager );
            float maxDamageModifier = ResolveMaximumDamageModifier( _playerStatManager );
            float appliedDamageModifier = Random.Range( minDamageModifier, maxDamageModifier );
            float result = clampedBaseDamage * ( BaseDamageMultiplier + appliedDamageModifier );
            return result;
        }

        ///<summary>
        /// 최소 피해 보정률 계산
        ///</summary>
        public static float ResolveMinimumDamageModifier( CPlayerStatManager _playerStatManager )
        {
            if ( _playerStatManager == null )
            {
                return BaseMinDamageModifier;
            }

            float accuracyValue = Mathf.Max( 0.0f, _playerStatManager.GetFinalStatValue( ePlayerStatType.ACC ) );
            float minimumDamageModifier = BaseMinDamageModifier + accuracyValue * AccuracyMinDamageModifierPerPoint;
            float result = Mathf.Min( 0.0f, minimumDamageModifier );
            return result;
        }

        ///<summary>
        /// 최대 피해 보정률 계산
        ///</summary>
        public static float ResolveMaximumDamageModifier( CPlayerStatManager _playerStatManager )
        {
            if ( _playerStatManager == null )
            {
                return BaseMaxDamageModifier;
            }

            float accuracyValue = Mathf.Max( 0.0f, _playerStatManager.GetFinalStatValue( ePlayerStatType.ACC ) );
            float result = BaseMaxDamageModifier + accuracyValue * AccuracyMaxDamageModifierPerPoint;
            return result;
        }
    }
}
