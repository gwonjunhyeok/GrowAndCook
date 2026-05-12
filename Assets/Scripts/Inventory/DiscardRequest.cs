using UnityEngine;

// 역할: TrashSlot 드롭 순간의 원본 슬롯 정보와 아이템 정보를 보관한다.
public sealed class DiscardRequest
{
    public bool fromMain;
    public int originIndex;
    public bool fromEquipment;
    public EquipSlotType equipmentSlotType = EquipSlotType.None;
    public int itemId = -1;
    public int maxCount = 0;
    public ItemData itemData;

    public static DiscardRequest Create(DragData dragData)
    {
        if (dragData == null || dragData.draggedItem == null || dragData.draggedItem.itemData == null)
            return null;

        ItemData draggedItemData = dragData.draggedItem.itemData;

        return new DiscardRequest
        {
            fromMain = dragData.fromMain,
            originIndex = dragData.originIndex,
            fromEquipment = dragData.fromEquipment,
            equipmentSlotType = dragData.originEquipmentSlot != null
                ? dragData.originEquipmentSlot.SlotType
                : EquipSlotType.None,
            itemId = draggedItemData.id,
            maxCount = Mathf.Max(0, dragData.draggedItem.count),
            itemData = draggedItemData
        };
    }
}
