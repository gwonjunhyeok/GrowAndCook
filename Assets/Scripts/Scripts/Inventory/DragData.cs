public class DragData
{
    public bool fromMain;               // true면 mainItems, false면 subItems
    public int originIndex;             // 드래그 시작 슬롯 인덱스
    public ItemStack draggedItem;       // 드래그 중인 아이템 정보
    public int draggedCount;            // 드래그 중인 수량
    public bool isSplit;                // Ctrl로 절반만 드래그했는지 여부
}