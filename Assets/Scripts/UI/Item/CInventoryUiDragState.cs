namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 UI 드래그 공유 상태
    ///</summary>
    public static class CInventoryUiDragState
    {
        private static bool isDragging;
        private static int draggedSlotIndex = -1;

        ///<summary>
        /// 드래그 시작 상태 설정
        ///</summary>
        public static void BeginDrag( int _slotIndex )
        {
            isDragging = _slotIndex >= 0;
            draggedSlotIndex = isDragging ? _slotIndex : -1;
        }

        ///<summary>
        /// 드래그 종료 상태 초기화
        ///</summary>
        public static void EndDrag()
        {
            isDragging = false;
            draggedSlotIndex = -1;
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
        /// 현재 드래그 슬롯 인덱스 반환
        ///</summary>
        public static int GetDraggedSlotIndex()
        {
            int result = draggedSlotIndex;
            return result;
        }
    }
}
