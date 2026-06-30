using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core.Data
{
    ///<summary>
    /// 랜덤상자 보상 항목 데이터
    ///</summary>
    [Serializable]
    public sealed class CRandomBoxRewardEntry
    {
        [SerializeField] private CItemDefinition itemDefinition;
        [SerializeField] private float weight = 1.0f;
        [SerializeField] private long minRewardCountValue = 1L;
        [SerializeField] private long maxRewardCountValue = 1L;

        ///<summary>
        /// 보상 아이템 정의 반환
        ///</summary>
        public CItemDefinition GetItemDefinition()
        {
            CItemDefinition result = itemDefinition;
            return result;
        }

        ///<summary>
        /// 보상 가중치 반환
        ///</summary>
        public float GetWeight()
        {
            float result = Mathf.Max( 0.0f, weight );
            return result;
        }

        ///<summary>
        /// 최소 보상 수량 반환
        ///</summary>
        public long GetMinRewardCount()
        {
            long result = Math.Max( 1L, minRewardCountValue );
            return result;
        }

        ///<summary>
        /// 최대 보상 수량 반환
        ///</summary>
        public long GetMaxRewardCount()
        {
            long minRewardCount = GetMinRewardCount();
            long result = Math.Max( minRewardCount, maxRewardCountValue );
            return result;
        }

        ///<summary>
        /// 보상 항목 유효 여부 반환
        ///</summary>
        public bool IsValid()
        {
            bool result = itemDefinition != null && GetWeight() > 0.0f && GetMaxRewardCount() > 0L;
            return result;
        }

        ///<summary>
        /// 랜덤 보상 수량 결정
        ///</summary>
        public long ResolveRewardCount()
        {
            long minRewardCount = GetMinRewardCount();
            long maxRewardCount = GetMaxRewardCount();

            if ( maxRewardCount <= minRewardCount )
            {
                return minRewardCount;
            }

            double randomValue = UnityEngine.Random.value;
            double randomCount = minRewardCount + ( ( maxRewardCount - minRewardCount + 1L ) * randomValue );
            long result = Math.Min( maxRewardCount, ( long )randomCount );
            return result;
        }
    }

    ///<summary>
    /// 랜덤상자 보상 테이블 에셋
    ///</summary>
    [CreateAssetMenu( fileName = "RandomBoxRewardTable", menuName = "TinyHero/Data/Random Box Reward Table" )]
    public sealed class CRandomBoxRewardTable : ScriptableObject
    {
        [SerializeField] private List<CRandomBoxRewardEntry> rewardEntryList = new List<CRandomBoxRewardEntry>();

        ///<summary>
        /// 보상 항목 목록 반환
        ///</summary>
        public IReadOnlyList<CRandomBoxRewardEntry> GetRewardEntryList()
        {
            IReadOnlyList<CRandomBoxRewardEntry> result = rewardEntryList;
            return result;
        }

        ///<summary>
        /// 보상 추첨 시도
        ///</summary>
        public bool TryRollReward( out CItemDefinition _itemDefinition, out long _rewardCount )
        {
            _itemDefinition = null;
            _rewardCount = 0L;
            float totalWeight = CalculateTotalWeight();

            if ( totalWeight <= 0.0f )
            {
                return false;
            }

            float randomWeight = UnityEngine.Random.Range( 0.0f, totalWeight );
            float accumulatedWeight = 0.0f;

            for ( int index = 0; index < rewardEntryList.Count; index++ )
            {
                CRandomBoxRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null || rewardEntry.IsValid() == false )
                {
                    continue;
                }

                accumulatedWeight += rewardEntry.GetWeight();

                if ( randomWeight > accumulatedWeight )
                {
                    continue;
                }

                _itemDefinition = rewardEntry.GetItemDefinition();
                _rewardCount = rewardEntry.ResolveRewardCount();
                return _itemDefinition != null && _rewardCount > 0L;
            }

            return TryResolveFallbackReward( out _itemDefinition, out _rewardCount );
        }

        ///<summary>
        /// 전체 가중치 합계 계산
        ///</summary>
        public float CalculateTotalWeight()
        {
            float totalWeight = 0.0f;

            for ( int index = 0; index < rewardEntryList.Count; index++ )
            {
                CRandomBoxRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null || rewardEntry.IsValid() == false )
                {
                    continue;
                }

                totalWeight += rewardEntry.GetWeight();
            }

            return totalWeight;
        }

        ///<summary>
        /// 마지막 유효 보상 결정
        ///</summary>
        private bool TryResolveFallbackReward( out CItemDefinition _itemDefinition, out long _rewardCount )
        {
            _itemDefinition = null;
            _rewardCount = 0L;

            for ( int index = rewardEntryList.Count - 1; index >= 0; index-- )
            {
                CRandomBoxRewardEntry rewardEntry = rewardEntryList[ index ];

                if ( rewardEntry == null || rewardEntry.IsValid() == false )
                {
                    continue;
                }

                _itemDefinition = rewardEntry.GetItemDefinition();
                _rewardCount = rewardEntry.ResolveRewardCount();
                return _itemDefinition != null && _rewardCount > 0L;
            }

            return false;
        }
    }
}
