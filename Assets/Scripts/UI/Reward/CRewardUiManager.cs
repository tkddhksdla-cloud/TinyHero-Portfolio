using System.Collections.Generic;
using TinyHero.Core;
using TinyHero.Core.Data;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 보상 획득 UI 표시 매니저
    ///</summary>
    public sealed class CRewardUiManager : CSingleTon<CRewardUiManager>
    {
        private GameObject rewardPopupPrefabObject;
        private PopupReward rewardPopup;

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
            if ( HasValidRewardItem( _rewardItemDataList ) == false )
            {
                return;
            }

            PopupReward resolvedRewardPopup = ResolveOrCreateRewardPopup();

            if ( resolvedRewardPopup == null )
            {
                return;
            }

            resolvedRewardPopup.ShowRewardList( _rewardItemDataList );
        }

        ///<summary>
        /// 보상 팝업 생성 또는 반환
        ///</summary>
        private PopupReward ResolveOrCreateRewardPopup()
        {
            if ( rewardPopup != null )
            {
                rewardPopup.SetLayerVisible( true );
                rewardPopup.BringLayerToFront();
                return rewardPopup;
            }

            if ( rewardPopupPrefabObject == null )
            {
                CResourceManager resourceManager = CResourceManager.Instance;
                rewardPopupPrefabObject = resourceManager != null ? resourceManager.GetRewardPopupPrefab() : null;
            }

            if ( rewardPopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            rewardPopup = navigationController.AddPopup<PopupReward>( rewardPopupPrefabObject, true );
            return rewardPopup;
        }

        ///<summary>
        /// 유효 보상 아이템 포함 여부 반환
        ///</summary>
        private bool HasValidRewardItem( IReadOnlyList<CRewardItemData> _rewardItemDataList )
        {
            if ( _rewardItemDataList == null )
            {
                return false;
            }

            for ( int index = 0; index < _rewardItemDataList.Count; index++ )
            {
                CRewardItemData rewardItemData = _rewardItemDataList[ index ];

                if ( rewardItemData == null || rewardItemData.IsValid() == false )
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
