using TinyHero.Core;
using TinyHero.Player;

namespace TinyHero.UI
{
    ///<summary>
    /// 큐브 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CCubeUiManager : CSingleTon<CCubeUiManager>
    {
        private PopupCube cubeUiController;

        ///<summary>
        /// 큐브 UI 열기 처리
        ///</summary>
        public bool OpenCubeUi( CPlayerInventoryManager _inventoryManager, CPlayerEquipmentManager _equipmentManager, int _cubeInventorySlotIndex )
        {
            if ( cubeUiController != null )
            {
                bool didOpenCubeUi = cubeUiController.TryOpen( _inventoryManager, _equipmentManager, _cubeInventorySlotIndex );
                return didOpenCubeUi;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return false;
            }

            navigationController.AddPopupAsync<PopupCube>(
                eResourceKey.POPUP_CUBE,
                true,
                ( PopupCube _createdCubeUiController ) =>
                {
                    if ( _createdCubeUiController == null )
                    {
                        return;
                    }

                    cubeUiController = _createdCubeUiController;
                    cubeUiController.SetVisible( false );
                    cubeUiController.TryOpen( _inventoryManager, _equipmentManager, _cubeInventorySlotIndex );
                } );

            return true;
        }
    }
}
