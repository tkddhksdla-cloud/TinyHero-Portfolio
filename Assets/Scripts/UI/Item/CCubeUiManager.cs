using TinyHero.Core;
using TinyHero.Player;

namespace TinyHero.UI
{
    ///<summary>
    /// 큐브 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CCubeUiManager : CSingleTon<CCubeUiManager>
    {
        private readonly CPopupAsyncHandle<PopupCube> cubePopupHandle = new CPopupAsyncHandle<PopupCube>( eResourceKey.POPUP_CUBE, true );

        ///<summary>
        /// 큐브 UI 열기 처리
        ///</summary>
        public bool OpenCubeUi( CPlayerInventoryManager _inventoryManager, CPlayerEquipmentManager _equipmentManager, int _cubeInventorySlotIndex )
        {
            PopupCube cachedCubeUiController = cubePopupHandle.GetCachedPopup();

            if ( cachedCubeUiController != null )
            {
                bool didOpenCubeUi = cachedCubeUiController.TryOpen( _inventoryManager, _equipmentManager, _cubeInventorySlotIndex );
                return didOpenCubeUi;
            }

            bool didRequest = cubePopupHandle.Request(
                ( PopupCube _createdCubeUiController ) =>
                {
                    if ( _createdCubeUiController == null )
                    {
                        return;
                    }

                    _createdCubeUiController.SetVisible( false );
                    _createdCubeUiController.TryOpen( _inventoryManager, _equipmentManager, _cubeInventorySlotIndex );
                } );
            return didRequest;
        }
    }
}
