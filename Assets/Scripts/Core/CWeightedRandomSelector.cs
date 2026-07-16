using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core
{
    /// <summary>
    /// 양수 가중치를 가진 후보 목록에서 하나를 균등 누적 가중치 방식으로 선택합니다.
    /// </summary>
    public static class CWeightedRandomSelector
    {
        public static bool TrySelect<T>( IReadOnlyList<T> _candidateList, Func<T, float> _weightSelector, out T _selectedCandidate )
        {
            _selectedCandidate = default;

            if ( _candidateList == null || _candidateList.Count == 0 || _weightSelector == null )
            {
                return false;
            }

            float totalWeight = 0.0f;
            T lastValidCandidate = default;
            bool hasValidCandidate = false;

            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                T candidate = _candidateList[ index ];
                float weight = Mathf.Max( 0.0f, _weightSelector( candidate ) );

                if ( weight <= 0.0f )
                {
                    continue;
                }

                totalWeight += weight;
                lastValidCandidate = candidate;
                hasValidCandidate = true;
            }

            if ( hasValidCandidate == false || totalWeight <= 0.0f )
            {
                return false;
            }

            float randomWeight = UnityEngine.Random.Range( 0.0f, totalWeight );
            float accumulatedWeight = 0.0f;

            for ( int index = 0; index < _candidateList.Count; index++ )
            {
                T candidate = _candidateList[ index ];
                float weight = Mathf.Max( 0.0f, _weightSelector( candidate ) );

                if ( weight <= 0.0f )
                {
                    continue;
                }

                accumulatedWeight += weight;

                if ( randomWeight >= accumulatedWeight )
                {
                    continue;
                }

                _selectedCandidate = candidate;
                return true;
            }

            _selectedCandidate = lastValidCandidate;
            return true;
        }
    }
}
