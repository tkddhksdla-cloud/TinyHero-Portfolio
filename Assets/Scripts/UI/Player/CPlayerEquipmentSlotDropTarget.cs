using TinyHero.Core.Data;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyHero.UI
{
    ///<summary>
    /// 장비 슬롯 드롭 처리 컴포넌트
    ///</summary>
    public sealed class CPlayerEquipmentSlotDropTarget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private eEquipmentType equipmentType = eEquipmentType.NONE;
        [SerializeField] private PopupItemInventory targetInventoryUiController;
        [SerializeField] private CPlayerEquipmentManager targetEquipmentManager;

        ///<summary>
        /// 드롭 대상 참조 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 드롭 대상 참조 재결정
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 드롭 대상 구성
        ///</summary>
        public void Configure( eEquipmentType _equipmentType, PopupItemInventory _targetInventoryUiController, CPlayerEquipmentManager _targetEquipmentManager )
        {
            equipmentType = _equipmentType;
            targetInventoryUiController = _targetInventoryUiController;
            targetEquipmentManager = _targetEquipmentManager;
        }

        ///<summary>
        /// 장비 슬롯 드롭 처리
        ///</summary>
        public void OnDrop( PointerEventData _eventData )
        {
            ResolveReferences();

            if ( equipmentType == eEquipmentType.NONE )
            {
                return;
            }

            if ( targetInventoryUiController == null || targetEquipmentManager == null )
            {
                return;
            }

            targetInventoryUiController.TryEquipDraggedSlotToEquipmentType( equipmentType );
        }

        ///<summary>
        /// 드롭 대상 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( targetInventoryUiController == null )
            {
                PopupItemInventory resolvedInventoryUiController = FindFirstObjectByType<PopupItemInventory>();
                targetInventoryUiController = resolvedInventoryUiController;
            }

            if ( targetEquipmentManager == null )
            {
                CPlayerEquipmentManager resolvedEquipmentManager = FindFirstObjectByType<CPlayerEquipmentManager>();
                targetEquipmentManager = resolvedEquipmentManager;
            }
        }
    }
}
