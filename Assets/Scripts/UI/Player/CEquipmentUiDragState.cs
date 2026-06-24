using TinyHero.Core.Data;

namespace TinyHero.UI
{
    ///<summary>
    /// 장비 UI 드래그 공유 상태
    ///</summary>
    public static class CEquipmentUiDragState
    {
        private static bool isDragging;
        private static eEquipmentType draggedEquipmentType = eEquipmentType.NONE;

        ///<summary>
        /// 드래그 시작 상태 설정
        ///</summary>
        public static void BeginDrag( eEquipmentType _equipmentType )
        {
            isDragging = _equipmentType != eEquipmentType.NONE;
            draggedEquipmentType = isDragging ? _equipmentType : eEquipmentType.NONE;
        }

        ///<summary>
        /// 드래그 종료 상태 초기화
        ///</summary>
        public static void EndDrag()
        {
            isDragging = false;
            draggedEquipmentType = eEquipmentType.NONE;
        }

        ///<summary>
        /// 드래그 활성 여부 반환
        ///</summary>
        public static bool IsDragging()
        {
            bool result = isDragging;
            return result;
        }

        ///<summary>
        /// 현재 드래그 장비 타입 반환
        ///</summary>
        public static eEquipmentType GetDraggedEquipmentType()
        {
            eEquipmentType result = draggedEquipmentType;
            return result;
        }
    }
}
