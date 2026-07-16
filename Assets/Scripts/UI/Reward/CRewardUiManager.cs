using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;

namespace TinyHero.UI
{
    ///<summary>
    /// 보상 획득 UI 표시 매니저
    ///</summary>
    public sealed class CRewardUiManager : CSingleTon<CRewardUiManager>
    {
        private readonly List<List<CRewardItemData>> pendingRewardItemDataListQueue = new List<List<CRewardItemData>>();

        private readonly CPopupAsyncHandle<PopupReward> rewardPopupHandle = new CPopupAsyncHandle<PopupReward>( eResourceKey.POPUP_REWARD, true );

        ///<summary>
        /// 아이템 보상 팝업 표시
        ///</summary>
        public void ShowItemReward( CItemDefinition _itemDefinition, long _itemCount )
        {
            List<CRewardItemData> rewardItemDataList = new List<CRewardItemData>();
            rewardItemDataList.Add( new CRewardItemData( _itemDefinition, _itemCount ) );
            ShowItemRewardList( rewardItemDataList );
        }

        ///<summary>
        /// 아이템 보상 목록 팝업 표시
        ///</summary>
        public void ShowItemRewardList( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            List<CRewardItemData> validRewardItemDataList = CreateValidRewardItemDataList( _rewardItemDataList );

            if ( validRewardItemDataList.Count == 0 )
            {
                return;
            }

            PopupReward cachedRewardPopup = rewardPopupHandle.GetCachedPopup();

            if ( cachedRewardPopup != null )
            {
                cachedRewardPopup.SetLayerVisible( true );
                cachedRewardPopup.BringLayerToFront();
                cachedRewardPopup.ShowRewardList( validRewardItemDataList );
                return;
            }

            pendingRewardItemDataListQueue.Add( validRewardItemDataList );
            bool didRequest = rewardPopupHandle.Request(
                ( PopupReward _createdRewardPopup ) =>
                {
                    if ( _createdRewardPopup == null )
                    {
                        pendingRewardItemDataListQueue.Clear();
                        return;
                    }

                    FlushPendingRewardItemDataListQueue();
                } );

            if ( didRequest == false )
            {
                pendingRewardItemDataListQueue.Clear();
            }
        }

        ///<summary>
        /// 대기 중인 보상 목록 일괄 표시
        ///</summary>
        private void FlushPendingRewardItemDataListQueue()
        {
            PopupReward cachedRewardPopup = rewardPopupHandle.GetCachedPopup();

            if ( cachedRewardPopup == null )
            {
                pendingRewardItemDataListQueue.Clear();
                return;
            }

            for ( int index = 0; index < pendingRewardItemDataListQueue.Count; index++ )
            {
                List<CRewardItemData> rewardItemDataList = pendingRewardItemDataListQueue[ index ];

                if ( rewardItemDataList == null || rewardItemDataList.Count == 0 )
                {
                    continue;
                }

                cachedRewardPopup.ShowRewardList( rewardItemDataList );
            }

            pendingRewardItemDataListQueue.Clear();
        }

        ///<summary>
        /// 유효 보상 아이템 목록 생성
        ///</summary>
        private List<CRewardItemData> CreateValidRewardItemDataList( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            List<CRewardItemData> validRewardItemDataList = new List<CRewardItemData>();

            if ( _rewardItemDataList == null )
            {
                return validRewardItemDataList;
            }

            for ( int index = 0; index < _rewardItemDataList.Count; index++ )
            {
                CRewardItemData rewardItemData = _rewardItemDataList[ index ];

                if ( rewardItemData == null || rewardItemData.IsValid() == false )
                {
                    continue;
                }

                validRewardItemDataList.Add( rewardItemData );
            }

            return validRewardItemDataList;
        }
    }
}
