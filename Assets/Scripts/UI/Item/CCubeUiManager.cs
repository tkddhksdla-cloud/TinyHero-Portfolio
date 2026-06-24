using TinyHero.Core;
using TinyHero.Player;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 큐브 UI 생성 및 재사용 관리 매니저
    ///</summary>
    public sealed class CCubeUiManager : CSingleTon<CCubeUiManager>
    {
        private const string CubePopupPrefabResourcePath = "Prefabs/UI/Popup/PopupCube";
        private const string LegacyCubePopupPrefabResourcePath = "Prefabs/UI/Inventory/CubeUI";

        private GameObject cubePopupPrefabObject;
        private PopupCube cubeUiController;

        ///<summary>
        /// 큐브 UI 열기 처리
        ///</summary>
        public bool OpenCubeUi( CPlayerInventoryManager _inventoryManager, CPlayerEquipmentManager _equipmentManager, int _cubeInventorySlotIndex )
        {
            PopupCube resolvedCubeUiController = ResolveOrCreateCubeUiController();

            if ( resolvedCubeUiController == null )
            {
                return false;
            }

            bool didOpenCubeUi = resolvedCubeUiController.TryOpen( _inventoryManager, _equipmentManager, _cubeInventorySlotIndex );
            return didOpenCubeUi;
        }

        ///<summary>
        /// 큐브 UI 컨트롤러 결정
        ///</summary>
        private PopupCube ResolveOrCreateCubeUiController()
        {
            if ( cubeUiController != null )
            {
                return cubeUiController;
            }

            if ( cubePopupPrefabObject == null )
            {
                cubePopupPrefabObject = LoadCubePopupPrefabObject();
            }

            if ( cubePopupPrefabObject == null )
            {
                return null;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( navigationController == null )
            {
                return null;
            }

            PopupCube createdCubeUiController = navigationController.AddPopup<PopupCube>( cubePopupPrefabObject, true );

            if ( createdCubeUiController == null )
            {
                return null;
            }

            createdCubeUiController.SetVisible( false );
            cubeUiController = createdCubeUiController;
            return cubeUiController;
        }

        ///<summary>
        /// 큐브 팝업 프리팹 로드
        ///</summary>
        private GameObject LoadCubePopupPrefabObject()
        {
            GameObject loadedPrefabObject = Resources.Load<GameObject>( CubePopupPrefabResourcePath );

            if ( loadedPrefabObject != null )
            {
                return loadedPrefabObject;
            }

            GameObject legacyLoadedPrefabObject = Resources.Load<GameObject>( LegacyCubePopupPrefabResourcePath );
            return legacyLoadedPrefabObject;
        }
    }
}
