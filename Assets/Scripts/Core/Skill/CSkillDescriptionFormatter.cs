using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TinyHero.Skill
{
    ///<summary>
    /// 스킬 설명 토큰 포맷터
    ///</summary>
    public static class CSkillDescriptionFormatter
    {
        private static readonly Regex TokenRegex = new Regex( "\\{(?<token>[a-zA-Z0-9_]+)\\}", RegexOptions.Compiled );

        ///<summary>
        /// 지원 토큰 목록 반환
        ///</summary>
        public static IReadOnlyList<string> GetSupportedTokenList()
        {
            IReadOnlyList<string> result = CSkillDescriptionTokenResolver.GetSupportedTokenList();
            return result;
        }

        ///<summary>
        /// 스킬 설명 포맷 문자열 반환
        ///</summary>
        public static string Format( CSkillDefinition _skillDefinition, int _skillLevel )
        {
            if ( _skillDefinition == null )
            {
                return string.Empty;
            }

            string template = _skillDefinition.GetDescription();

            if ( string.IsNullOrWhiteSpace( template ) )
            {
                return string.Empty;
            }

            string result = TokenRegex.Replace( template, _match =>
            {
                System.Text.RegularExpressions.Group tokenGroup = _match.Groups[ "token" ];
                string tokenName = tokenGroup != null ? tokenGroup.Value : string.Empty;

                if ( string.IsNullOrWhiteSpace( tokenName ) )
                {
                    return _match.Value;
                }

                bool isResolved = CSkillDescriptionTokenResolver.TryResolveTokenValue( _skillDefinition, _skillLevel, tokenName, out string resolvedValue );
                string replacementValue = isResolved ? resolvedValue : _match.Value;
                return replacementValue;
            } );
            string supplementalDescription = BuildSupplementalDescription( _skillDefinition, _skillLevel, template );

            if ( string.IsNullOrWhiteSpace( supplementalDescription ) )
            {
                return result;
            }

            string combinedResult = $"{result}\n{supplementalDescription}";
            return combinedResult;
        }

        ///<summary>
        /// 설명 누락 효과 문구 구성
        ///</summary>
        private static string BuildSupplementalDescription( CSkillDefinition _skillDefinition, int _skillLevel, string _template )
        {
            string debuffDescription = BuildDebuffDescription( _skillDefinition, _skillLevel, _template );
            string result = debuffDescription;
            return result;
        }

        ///<summary>
        /// 디버프 보강 문구 구성
        ///</summary>
        private static string BuildDebuffDescription( CSkillDefinition _skillDefinition, int _skillLevel, string _template )
        {
            bool hasAttackReduction = CSkillDescriptionTokenResolver.TryResolveTokenValue( _skillDefinition, _skillLevel, "atkReduction", out string attackReductionText );
            bool hasDefenseReduction = CSkillDescriptionTokenResolver.TryResolveTokenValue( _skillDefinition, _skillLevel, "defReduction", out string defenseReductionText );
            bool hasDebuffDuration = CSkillDescriptionTokenResolver.TryResolveTokenValue( _skillDefinition, _skillLevel, "debuffDuration", out string debuffDurationText );

            if ( hasAttackReduction == false && hasDefenseReduction == false )
            {
                return string.Empty;
            }

            bool containsAttackReductionToken = ContainsToken( _template, "atkReduction" );
            bool containsDefenseReductionToken = ContainsToken( _template, "defReduction" );
            bool containsDebuffDurationToken = ContainsToken( _template, "debuffDuration" );
            List<string> descriptionPartList = new List<string>();

            if ( hasAttackReduction && ( containsAttackReductionToken == false || containsDebuffDurationToken == false ) )
            {
                string attackReductionDescription = hasDebuffDuration
                    ? $"적의 공격력을 {attackReductionText} 감소시킨다. 지속시간 {debuffDurationText}초."
                    : $"적의 공격력을 {attackReductionText} 감소시킨다.";
                descriptionPartList.Add( attackReductionDescription );
            }

            if ( hasDefenseReduction && ( containsDefenseReductionToken == false || containsDebuffDurationToken == false ) )
            {
                string defenseReductionDescription = hasDebuffDuration
                    ? $"적의 방어력을 {defenseReductionText}% 감소시킨다. 지속시간 {debuffDurationText}초."
                    : $"적의 방어력을 {defenseReductionText}% 감소시킨다.";
                descriptionPartList.Add( defenseReductionDescription );
            }

            if ( descriptionPartList.Count == 0 )
            {
                return string.Empty;
            }

            string result = string.Join( " ", descriptionPartList );
            return result;
        }

        ///<summary>
        /// 설명 토큰 포함 여부 확인
        ///</summary>
        private static bool ContainsToken( string _template, string _tokenName )
        {
            if ( string.IsNullOrWhiteSpace( _template ) || string.IsNullOrWhiteSpace( _tokenName ) )
            {
                return false;
            }

            string tokenText = $"{{{_tokenName}}}";
            bool result = _template.Contains( tokenText );
            return result;
        }
    }
}
