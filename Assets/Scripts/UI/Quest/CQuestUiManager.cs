using System;
using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 퀘스트 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CQuestUiManager : CSingleTon<CQuestUiManager>
    {
        private PlayerController targetPlayerController;
        private PopupQuestList npcQuestUiController;
        private PopupQuestList playerQuestUiController;

        ///<summary>
        /// 플레이어 제어 컴포넌트 바인딩
        ///</summary>
        public void BindPlayerController( PlayerController _targetPlayerController )
        {
            targetPlayerController = _targetPlayerController;
        }

        ///<summary>
        /// 플레이어 퀘스트 저널 토글 처리
        ///</summary>
        public void TogglePlayerQuestListUi()
        {
            PlayerController resolvedPlayerController = ResolvePlayerController();

            if ( resolvedPlayerController == null )
            {
                return;
            }

            if ( playerQuestUiController != null )
            {
                ToggleResolvedPlayerQuestListUi( playerQuestUiController, resolvedPlayerController, false );
                return;
            }

            RequestPlayerQuestUiController(
                ( PopupQuestList _createdQuestUiController ) =>
                {
                    if ( _createdQuestUiController == null )
                    {
                        return;
                    }

                    ToggleResolvedPlayerQuestListUi( _createdQuestUiController, resolvedPlayerController, true );
                } );
        }

        ///<summary>
        /// NPC 퀘스트 목록 UI 표시
        ///</summary>
        public void ShowNpcQuestListUi( CNPCObject _npcObject, PlayerController _playerController )
        {
            if ( _npcObject == null || _playerController == null )
            {
                return;
            }

            if ( npcQuestUiController != null )
            {
                ShowResolvedNpcQuestListUi( npcQuestUiController, _npcObject, _playerController );
                return;
            }

            RequestNpcQuestUiController(
                ( PopupQuestList _createdQuestUiController ) =>
                {
                    ShowResolvedNpcQuestListUi( _createdQuestUiController, _npcObject, _playerController );
                } );
        }

        ///<summary>
        /// 플레이어 퀘스트 UI 컨트롤러 비동기 요청
        ///</summary>
        private void RequestPlayerQuestUiController( Action<PopupQuestList> _onCompleted )
        {
            RequestQuestUiController( eResourceKey.POPUP_QUEST_LIST_PLAYER, ( PopupQuestList _questUiController ) =>
            {
                playerQuestUiController = _questUiController;
                InvokeQuestUiControllerCompletedHandler( _onCompleted, _questUiController );
            } );
        }

        ///<summary>
        /// NPC 퀘스트 UI 컨트롤러 비동기 요청
        ///</summary>
        private void RequestNpcQuestUiController( Action<PopupQuestList> _onCompleted )
        {
            RequestQuestUiController( eResourceKey.POPUP_QUEST_LIST_NPC, ( PopupQuestList _questUiController ) =>
            {
                npcQuestUiController = _questUiController;
                InvokeQuestUiControllerCompletedHandler( _onCompleted, _questUiController );
            } );
        }

        ///<summary>
        /// 퀘스트 UI 컨트롤러 공통 비동기 요청
        ///</summary>
        private void RequestQuestUiController( eResourceKey _resourceKey, Action<PopupQuestList> _onCompleted )
        {
            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                InvokeQuestUiControllerCompletedHandler( _onCompleted, null );
                return;
            }

            navigationController.AddPopupAsync<PopupQuestList>( _resourceKey, true, _onCompleted );
        }

        ///<summary>
        /// 준비된 플레이어 퀘스트 UI 토글
        ///</summary>
        private void ToggleResolvedPlayerQuestListUi( PopupQuestList _questUiController, PlayerController _playerController, bool _isNewlyCreated )
        {
            if ( _questUiController == null || _playerController == null )
            {
                return;
            }

            if ( _isNewlyCreated )
            {
                _questUiController.SetLayerVisible( false );
            }

            bool hasVisibleQuestUi = PopupQuestList.IsAnyQuestUiVisible();
            bool isPlayerQuestUiVisible = _questUiController.IsQuestListVisible();

            if ( isPlayerQuestUiVisible )
            {
                _questUiController.TogglePlayerQuestListUi( _playerController );
                return;
            }

            if ( hasVisibleQuestUi )
            {
                return;
            }

            _questUiController.TogglePlayerQuestListUi( _playerController );
        }

        ///<summary>
        /// 준비된 NPC 퀘스트 UI 표시
        ///</summary>
        private void ShowResolvedNpcQuestListUi( PopupQuestList _questUiController, CNPCObject _npcObject, PlayerController _playerController )
        {
            if ( _questUiController == null || _npcObject == null || _playerController == null )
            {
                return;
            }

            _questUiController.SetLayerVisible( false );
            _questUiController.ShowQuestListUi( _npcObject, _playerController );
        }

        ///<summary>
        /// 퀘스트 UI 컨트롤러 요청 완료 콜백 호출
        ///</summary>
        private void InvokeQuestUiControllerCompletedHandler( Action<PopupQuestList> _onCompleted, PopupQuestList _questUiController )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _questUiController );
        }

        ///<summary>
        /// 활성 플레이어 제어 컴포넌트 결정
        ///</summary>
        private PlayerController ResolvePlayerController()
        {
            if ( targetPlayerController != null && targetPlayerController.gameObject.activeInHierarchy )
            {
                return targetPlayerController;
            }

            bool hasGameManager = CGameManager.TryGetExistingInstance( out CGameManager gameManager );
            PlayerController playerController = null;
            bool hasPlayerController = hasGameManager && gameManager.TryGetActivePlayerController( out playerController );
            targetPlayerController = hasPlayerController ? playerController : null;
            return targetPlayerController;
        }
    }
}
