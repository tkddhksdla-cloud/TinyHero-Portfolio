using System.Collections.Generic;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 설명 토큰 해석기
    ///</summary>
    public static class CSkillDescriptionTokenResolver
    {
        private const string UnresolvedValueText = "-";
        private static readonly string[] SupportedTokenArray =
        {
            "damage",
            "cooldown",
            "duration",
            "tickInterval",
            "atkReduction",
            "defReduction",
            "debuffDuration",
            "finalAttackPercent",
            "invincibleDuration",
            "defense",
            "mpCost",
            "learnSpCost",
            "levelUpSpCost",
            "maxLevel",
            "skillLevel"
        };

        ///<summary>
        /// 지원 토큰 목록 반환
        ///</summary>
        public static IReadOnlyList<string> GetSupportedTokenList()
        {
            IReadOnlyList<string> result = SupportedTokenArray;
            return result;
        }

        ///<summary>
        /// 스킬 설명 토큰 값 반환
        ///</summary>
        public static bool TryResolveTokenValue( CSkillDefinition _skillDefinition, int _skillLevel, string _tokenName, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null || string.IsNullOrWhiteSpace( _tokenName ) )
            {
                return false;
            }

            int normalizedSkillLevel = Mathf.Max( 1, _skillLevel );

            switch ( _tokenName )
            {
                case "damage":
                    return TryResolveDamageValue( _skillDefinition, normalizedSkillLevel, out _resolvedValue );

                case "cooldown":
                    _resolvedValue = FormatFloatValue( _skillDefinition.GetCooldownSeconds( normalizedSkillLevel ) );
                    return true;

                case "duration":
                    return TryResolveDurationValue( _skillDefinition, normalizedSkillLevel, out _resolvedValue );

                case "tickInterval":
                    return TryResolveTickIntervalValue( _skillDefinition, out _resolvedValue );

                case "atkReduction":
                    return TryResolveAttackReductionValue( _skillDefinition, out _resolvedValue );

                case "defReduction":
                    return TryResolveDefenseReductionValue( _skillDefinition, out _resolvedValue );

                case "debuffDuration":
                    return TryResolveDebuffDurationValue( _skillDefinition, out _resolvedValue );

                case "finalAttackPercent":
                    return TryResolveFinalAttackPercentValue( _skillDefinition, out _resolvedValue );

                case "invincibleDuration":
                    return TryResolveInvincibleDurationValue( _skillDefinition, out _resolvedValue );

                case "defense":
                    return TryResolvePassiveStatValue( _skillDefinition, normalizedSkillLevel, ePlayerStatType.DEF, false, out _resolvedValue );

                case "mpCost":
                    _resolvedValue = FormatFloatValue( _skillDefinition.GetMpCost() );
                    return true;

                case "learnSpCost":
                    _resolvedValue = _skillDefinition.GetLearnSpCost().ToString();
                    return true;

                case "levelUpSpCost":
                    _resolvedValue = _skillDefinition.GetLevelUpSpCost().ToString();
                    return true;

                case "maxLevel":
                    _resolvedValue = _skillDefinition.GetMaxSkillLevel().ToString();
                    return true;

                case "skillLevel":
                    _resolvedValue = normalizedSkillLevel.ToString();
                    return true;
            }

            return false;
        }

        ///<summary>
        /// 공격력 감소 수치 토큰 값 반환
        ///</summary>
        private static bool TryResolveAttackReductionValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( TryResolveDebuffEffect( _skillDefinition, out CEnemyDebuffEffectBase debuffEffect ) == false )
            {
                return false;
            }

            if ( debuffEffect is CAtkReductionDebuffEffect atkReductionDebuffEffect == false )
            {
                return false;
            }

            long reductionAmount = atkReductionDebuffEffect.GetReductionAmount();

            if ( reductionAmount <= 0 )
            {
                return false;
            }

            _resolvedValue = reductionAmount.ToString();
            return true;
        }

        ///<summary>
        /// 방어력 감소 수치 토큰 값 반환
        ///</summary>
        private static bool TryResolveDefenseReductionValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( TryResolveDebuffEffect( _skillDefinition, out CEnemyDebuffEffectBase debuffEffect ) == false )
            {
                return false;
            }

            if ( debuffEffect is CDefReductionDebuffEffect defReductionDebuffEffect == false )
            {
                return false;
            }

            float reductionPercent = defReductionDebuffEffect.GetReductionPercent() * 100.0f;

            if ( Mathf.Approximately( reductionPercent, 0.0f ) )
            {
                return false;
            }

            _resolvedValue = FormatFloatValue( reductionPercent );
            return true;
        }

        ///<summary>
        /// 디버프 지속시간 토큰 값 반환
        ///</summary>
        private static bool TryResolveDebuffDurationValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( TryResolveDebuffEffect( _skillDefinition, out CEnemyDebuffEffectBase debuffEffect ) == false )
            {
                return false;
            }

            if ( debuffEffect is CAtkReductionDebuffEffect atkReductionDebuffEffect )
            {
                _resolvedValue = FormatFloatValue( atkReductionDebuffEffect.GetDurationSeconds() );
                return true;
            }

            if ( debuffEffect is CDefReductionDebuffEffect defReductionDebuffEffect )
            {
                _resolvedValue = FormatFloatValue( defReductionDebuffEffect.GetDurationSeconds() );
                return true;
            }

            return false;
        }

        ///<summary>
        /// 지속 시간 토큰 값 반환
        ///</summary>
        private static bool TryResolveDurationValue( CSkillDefinition _skillDefinition, int _skillLevel, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect is CPlaceActiveSkillEffect placeActiveSkillEffect )
            {
                _resolvedValue = FormatFloatValue( placeActiveSkillEffect.GetDurationSeconds() );
                return true;
            }

            if ( activeSkillEffect is CCloneReplayActiveSkillEffect cloneReplayActiveSkillEffect )
            {
                _resolvedValue = FormatFloatValue( cloneReplayActiveSkillEffect.GetDurationSeconds() );
                return true;
            }

            if ( activeSkillEffect is CBuffActiveSkillEffect buffActiveSkillEffect )
            {
                List<CPlayerBuffEffectBase> buffEffectList = buffActiveSkillEffect.GetBuffEffects();

                for ( int index = 0; index < buffEffectList.Count; index++ )
                {
                    CPlayerBuffEffectBase buffEffect = buffEffectList[ index ];

                    if ( buffEffect is CFinalAttackPercentBuffEffect finalAttackPercentBuffEffect )
                    {
                        _resolvedValue = FormatFloatValue( finalAttackPercentBuffEffect.GetDurationSeconds() );
                        return true;
                    }

                    if ( buffEffect is CInvincibleBuffEffect invincibleBuffEffect )
                    {
                        _resolvedValue = FormatFloatValue( invincibleBuffEffect.GetDurationSeconds() );
                        return true;
                    }
                }
            }

            return false;
        }

        ///<summary>
        /// 틱 간격 토큰 값 반환
        ///</summary>
        private static bool TryResolveTickIntervalValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect is CPlaceActiveSkillEffect placeActiveSkillEffect )
            {
                _resolvedValue = FormatFloatValue( placeActiveSkillEffect.GetTickIntervalSeconds() );
                return true;
            }

            return false;
        }

        ///<summary>
        /// 최종 공격력 증가 토큰 값 반환
        ///</summary>
        private static bool TryResolveFinalAttackPercentValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect is CBuffActiveSkillEffect buffActiveSkillEffect == false )
            {
                return false;
            }

            List<CPlayerBuffEffectBase> buffEffectList = buffActiveSkillEffect.GetBuffEffects();

            for ( int index = 0; index < buffEffectList.Count; index++ )
            {
                CPlayerBuffEffectBase buffEffect = buffEffectList[ index ];

                if ( buffEffect is CFinalAttackPercentBuffEffect finalAttackPercentBuffEffect == false )
                {
                    continue;
                }

                float percentValue = finalAttackPercentBuffEffect.GetIncreasePercent() * 100.0f;
                _resolvedValue = FormatFloatValue( percentValue );
                return true;
            }

            return false;
        }

        ///<summary>
        /// 무적 지속 시간 토큰 값 반환
        ///</summary>
        private static bool TryResolveInvincibleDurationValue( CSkillDefinition _skillDefinition, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect is CBuffActiveSkillEffect buffActiveSkillEffect == false )
            {
                return false;
            }

            List<CPlayerBuffEffectBase> buffEffectList = buffActiveSkillEffect.GetBuffEffects();

            for ( int index = 0; index < buffEffectList.Count; index++ )
            {
                CPlayerBuffEffectBase buffEffect = buffEffectList[ index ];

                if ( buffEffect is CInvincibleBuffEffect invincibleBuffEffect == false )
                {
                    continue;
                }

                _resolvedValue = FormatFloatValue( invincibleBuffEffect.GetDurationSeconds() );
                return true;
            }

            return false;
        }

        ///<summary>
        /// 패시브 스탯 토큰 값 반환
        ///</summary>
        private static bool TryResolvePassiveStatValue( CSkillDefinition _skillDefinition, int _skillLevel, ePlayerStatType _targetStatType, bool _isPercentValue, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            float resolvedValue = _skillDefinition.GetPassiveStatBonus().GetStatValue( _targetStatType ) * _skillLevel;
            List<CPassiveSkillEffectBase> passiveSkillEffectList = _skillDefinition.GetPassiveSkillEffectList();

            for ( int index = 0; index < passiveSkillEffectList.Count; index++ )
            {
                CPassiveSkillEffectBase passiveSkillEffect = passiveSkillEffectList[ index ];

                if ( passiveSkillEffect is CPassiveStatSkillEffect passiveStatSkillEffect == false )
                {
                    continue;
                }

                if ( passiveStatSkillEffect.GetTargetStatType() != _targetStatType )
                {
                    continue;
                }

                resolvedValue += passiveStatSkillEffect.GetBonusValue() * _skillLevel;
            }

            if ( Mathf.Approximately( resolvedValue, 0.0f ) )
            {
                return false;
            }

            _resolvedValue = _isPercentValue ? $"{FormatFloatValue( resolvedValue )}%" : FormatFloatValue( resolvedValue );
            return true;
        }

        ///<summary>
        /// 데미지 토큰 값 반환
        ///</summary>
        private static bool TryResolveDamageValue( CSkillDefinition _skillDefinition, int _skillLevel, out string _resolvedValue )
        {
            _resolvedValue = UnresolvedValueText;

            if ( _skillDefinition == null )
            {
                return false;
            }

            bool isResolved = TryResolveBaseDamageMultiplier( _skillDefinition, out float baseDamageMultiplier );

            if ( isResolved == false )
            {
                return false;
            }

            float resolvedDamageMultiplier = _skillDefinition.ResolveDamageMultiplier( baseDamageMultiplier, _skillLevel );
            float resolvedPercentValue = resolvedDamageMultiplier * 100.0f;
            _resolvedValue = FormatFloatValue( resolvedPercentValue );
            return true;
        }

        ///<summary>
        /// 기본 데미지 배율 반환
        ///</summary>
        private static bool TryResolveBaseDamageMultiplier( CSkillDefinition _skillDefinition, out float _baseDamageMultiplier )
        {
            _baseDamageMultiplier = 0.0f;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();

            if ( activeSkillEffect is CInstantActiveSkillEffect instantActiveSkillEffect )
            {
                _baseDamageMultiplier = instantActiveSkillEffect.GetDamageMultiplier();
                return true;
            }

            if ( activeSkillEffect is CPlaceActiveSkillEffect placeActiveSkillEffect )
            {
                _baseDamageMultiplier = placeActiveSkillEffect.GetDamageMultiplier();
                return true;
            }

            if ( activeSkillEffect is CProjectileActiveSkillEffect projectileActiveSkillEffect )
            {
                _baseDamageMultiplier = projectileActiveSkillEffect.GetDamageMultiplier();
                return true;
            }

            if ( activeSkillEffect is CCloneReplayActiveSkillEffect cloneReplayActiveSkillEffect )
            {
                _baseDamageMultiplier = cloneReplayActiveSkillEffect.GetCloneDamageMultiplier();
                return true;
            }

            CSkillActionBase activeAction = _skillDefinition.GetActiveAction();

            if ( activeAction is CSkillAreaDamageAction skillAreaDamageAction )
            {
                _baseDamageMultiplier = skillAreaDamageAction.GetDamageMultiplier();
                return true;
            }

            return false;
        }

        ///<summary>
        /// 스킬 디버프 이펙트 결정
        ///</summary>
        private static bool TryResolveDebuffEffect( CSkillDefinition _skillDefinition, out CEnemyDebuffEffectBase _debuffEffect )
        {
            _debuffEffect = null;

            if ( _skillDefinition == null )
            {
                return false;
            }

            CActiveSkillEffectBase activeSkillEffect = _skillDefinition.GetActiveSkillEffect();
            List<CEnemyDebuffEffectBase> debuffEffectList = null;

            if ( activeSkillEffect is CInstantActiveSkillEffect instantActiveSkillEffect )
            {
                debuffEffectList = instantActiveSkillEffect.GetDebuffEffects();
            }
            else if ( activeSkillEffect is CPlaceActiveSkillEffect placeActiveSkillEffect )
            {
                debuffEffectList = placeActiveSkillEffect.GetDebuffEffects();
            }
            else if ( activeSkillEffect is CProjectileActiveSkillEffect projectileActiveSkillEffect )
            {
                debuffEffectList = projectileActiveSkillEffect.GetDebuffEffects();
            }

            if ( debuffEffectList == null || debuffEffectList.Count == 0 )
            {
                return false;
            }

            for ( int index = 0; index < debuffEffectList.Count; index++ )
            {
                CEnemyDebuffEffectBase debuffEffect = debuffEffectList[ index ];

                if ( debuffEffect == null )
                {
                    continue;
                }

                _debuffEffect = debuffEffect;
                return true;
            }

            return false;
        }

        ///<summary>
        /// 소수 수치 문자열 반환
        ///</summary>
        private static string FormatFloatValue( float _value )
        {
            string result = _value.ToString( "0.##" );
            return result;
        }
    }
}
