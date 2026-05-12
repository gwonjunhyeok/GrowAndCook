using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 역할: 인벤토리/핫바/장비 슬롯 간 아이템 이동과 드래그 처리를 관리한다.
public class Inven_System : MonoBehaviour
{
    public static Inven_System instance;

    [Header("Parents")]
    [SerializeField] private Transform mainSlotParent;
    [SerializeField] private Transform subSlotParent;

    [Header("Slots")]
    [SerializeField] private Inven_Slot[] mainSlots;
    [SerializeField] private Inven_Slot[] subSlots;

    [Header("Add Slots")]
    [SerializeField] private Inven_Slot mainSlotPrefab;
    [SerializeField] private int addCountPerPress = 9;
    [SerializeField] private int bagExtraSlotCount = 9;

    [SerializeField] private RectTransform[] inventoryPanels;
    [SerializeField] private float currentSlotStepX = 124f;
    [SerializeField] private RectTransform currentSlotIndicator;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotUI[] equipmentSlots;

    private readonly List<ItemStack> mainItems = new();
    private readonly List<ItemStack> subItems = new();

    private int selectedHotbarIndex = 0;
    private int defaultMainSlotCount;
    private readonly Dictionary<int, ItemData> idToItem = new();
    private Vector2 currentSlotIndicatorStartAnchoredPosition;
    private bool hasCurrentSlotIndicatorStartPosition;

    public bool IsInteractionLocked { get; private set; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainSlotParent != null)
            mainSlots = mainSlotParent.GetComponentsInChildren<Inven_Slot>(true);

        if (subSlotParent != null)
            subSlots = subSlotParent.GetComponentsInChildren<Inven_Slot>(true);
    }
#endif

    private void Awake()
    {
        if (instance == null)
            instance = this;

        RebuildSlotArraysAndReindex();
        defaultMainSlotCount = mainSlots != null ? mainSlots.Length : 0;
        BuildItemDatabase();
        InitFixedSlots();
        FreshSlot();
    }

    private void Update()
    {
        HandleHotbarSelection_1to9();
        HandleLeftClick_Test();

        if (Input.GetKeyDown(KeyCode.L))
            AddMainSlots(addCountPerPress);

        if (Input.GetKeyDown(KeyCode.H))
        {
            const int testItemId = 2;
            ItemData item = FindItemById(testItemId);
            if (item != null)
                AddItem(item);
            else
                Debug.LogWarning($"아이템 id={testItemId}가 없습니다.");
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            const int testItemId = 3;
            ItemData item = FindItemById(testItemId);
            if (item != null)
                AddItem(item);
            else
                Debug.LogWarning($"아이템 id={testItemId}가 없습니다.");
        }
    }

    private void AddMainSlots(int count)
    {
        if (count <= 0)
            return;

        if (mainSlotParent == null)
        {
            Debug.LogWarning("mainSlotParent(Content)가 비어있습니다.");
            return;
        }

        if (mainSlotPrefab == null)
        {
            Debug.LogWarning("mainSlotPrefab이 비어있습니다. 슬롯 프리팹을 할당하세요.");
            return;
        }

        for (int i = 0; i < count; i++)
            Instantiate(mainSlotPrefab, mainSlotParent);

        RebuildSlotArraysAndReindex();
        FreshSlot();
        MarkInventoryDirty();
    }

    private void RemoveMainSlots(int count)
    {
        if (count <= 0)
            return;

        if (mainSlotParent == null || mainSlots == null)
            return;

        int removableCount = Mathf.Min(count, GetCurrentExtraMainSlotCount());
        if (removableCount <= 0)
            return;

        for (int i = 0; i < removableCount; i++)
        {
            if (mainItems.Count > 0)
                mainItems.RemoveAt(mainItems.Count - 1);

            int lastChildIndex = mainSlotParent.childCount - 1;
            if (lastChildIndex < 0)
                continue;

            Transform child = mainSlotParent.GetChild(lastChildIndex);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        RebuildSlotArraysAndReindex();
        MarkInventoryDirty();
    }

    private void RebuildSlotArraysAndReindex()
    {
        if (mainSlotParent != null)
            mainSlots = mainSlotParent.GetComponentsInChildren<Inven_Slot>(true);

        if (subSlotParent != null)
            subSlots = subSlotParent.GetComponentsInChildren<Inven_Slot>(true);

        if (mainSlots != null)
        {
            for (int i = 0; i < mainSlots.Length; i++)
                mainSlots[i].Init(true, i);
        }

        if (subSlots != null)
        {
            for (int i = 0; i < subSlots.Length; i++)
                subSlots[i].Init(false, i);
        }

        EnsureListSize(mainItems, mainSlots != null ? mainSlots.Length : 0);
        EnsureListSize(subItems, subSlots != null ? subSlots.Length : 0);
    }

    private static void EnsureListSize(List<ItemStack> list, int size)
    {
        if (list == null)
            return;

        while (list.Count < size)
            list.Add(null);
    }

    private void BuildItemDatabase()
    {
        idToItem.Clear();
        ItemData[] allItems = Resources.LoadAll<ItemData>("");

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData item = allItems[i];
            if (item == null)
                continue;

            if (idToItem.ContainsKey(item.id))
            {
                Debug.LogWarning($"ItemData ID 중복: id={item.id}, name={item.itemName}");
                continue;
            }

            idToItem.Add(item.id, item);
        }
    }

    private void InitFixedSlots()
    {
        selectedHotbarIndex = subSlots != null && subSlots.Length > 0 ? 0 : -1;
        CacheCurrentSlotIndicatorStartPosition();
    }

    public void FreshSlot()
    {
        for (int i = 0; i < mainSlots.Length; i++)
        {
            if (i < mainItems.Count && mainItems[i] != null && !mainItems[i].IsEmpty)
                mainSlots[i].SetItem(mainItems[i].itemData, mainItems[i].count);
            else
                mainSlots[i].ClearSlot();
        }

        for (int i = 0; i < subSlots.Length; i++)
        {
            if (i < subItems.Count && subItems[i] != null && !subItems[i].IsEmpty)
                subSlots[i].SetItem(subItems[i].itemData, subItems[i].count);
            else
                subSlots[i].ClearSlot();
        }

        RefreshCurrentSlotIndicator();
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
            return;

        if (TryAddToList(subItems, item))
        {
            FreshSlot();
            MarkInventoryDirty();
            return;
        }

        if (TryAddToList(mainItems, item))
        {
            FreshSlot();
            MarkInventoryDirty();
            return;
        }

        Debug.Log("인벤토리 공간이 부족합니다.");
    }

    private bool TryAddToList(List<ItemStack> list, ItemData item)
    {
        int id = item.id;

        for (int i = 0; i < list.Count; i++)
        {
            ItemStack stack = list[i];
            if (stack == null || stack.IsEmpty)
                continue;

            if (stack.itemData != null && stack.itemData.id == id && !stack.IsFull)
            {
                stack.Add(1);
                return true;
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                list[i] = new ItemStack(item, 1);
                return true;
            }
        }

        return false;
    }

    public void OnSlotClicked(bool isMain, int index)
    {
        if (IsInteractionLocked)
            return;

        List<ItemStack> list = isMain ? mainItems : subItems;
        if (!IsValidIndex(list, index))
            return;

        ItemStack stack = list[index];
        if (stack == null || stack.IsEmpty)
            return;

        bool isSplit = Input.GetKey(KeyCode.LeftControl);
        int moveCount = isSplit ? Mathf.FloorToInt(stack.count / 2f) : stack.count;
        if (moveCount <= 0)
            return;

        if (isSplit)
            stack.Remove(moveCount);
        else
            list[index] = null;

        DragSlot.Instance.StartDrag(isMain, index, new ItemStack(stack.itemData, moveCount), isSplit);
        FreshSlot();
    }

    public void OnEquipmentSlotClicked(EquipmentSlotUI slot)
    {
        if (IsInteractionLocked)
            return;

        if (slot == null || !slot.HasItem || slot.ItemData == null)
            return;

        if (DragSlot.Instance == null)
            return;

        ItemStack stack = new ItemStack(slot.ItemData, 1);
        DragSlot.Instance.StartDrag(slot, stack);
        slot.ClearSlot();
    }

    public void OnSlotDrop(bool isMainTarget, int targetIndex)
    {
        if (IsInteractionLocked)
            return;

        DragData drag = GetCurrentDrag();
        if (drag == null)
            return;

        if (drag.fromEquipment)
        {
            HandleEquipmentToInventoryDrop(isMainTarget, targetIndex, drag);
            return;
        }

        HandleInventoryToInventoryDrop(isMainTarget, targetIndex, drag);
    }

    private void HandleInventoryToInventoryDrop(bool isMainTarget, int targetIndex, DragData drag)
    {
        List<ItemStack> fromList = drag.fromMain ? mainItems : subItems;
        List<ItemStack> toList = isMainTarget ? mainItems : subItems;

        if (!IsValidIndex(toList, targetIndex))
        {
            ReturnDragItem();
            return;
        }

        ItemStack dragged = drag.draggedItem;
        int draggedId = dragged.itemData.id;
        int originIndex = drag.originIndex;

        // 같은 슬롯으로 되돌린 경우는 이동 없이 원복 처리한다.
        if (!drag.isSplit && ReferenceEquals(fromList, toList) && originIndex == targetIndex)
        {
            ReturnDragItem();
            return;
        }

        if (toList[targetIndex] != null && !toList[targetIndex].IsEmpty)
        {
            ItemStack targetStack = toList[targetIndex];

            if (drag.isSplit && targetStack.itemData.id != draggedId)
            {
                ReturnDragItem();
                return;
            }

            if (targetStack.itemData.id == draggedId)
            {
                int added = targetStack.Add(dragged.count);
                int remain = dragged.count - added;

                if (remain <= 0)
                {
                    DragSlot.Instance.ClearDrag();
                    FreshSlot();
                    MarkInventoryDirty();
                    return;
                }

                dragged.count = remain;
                ReturnDragItem();
                return;
            }

            if (!drag.isSplit)
            {
                ItemStack temp = toList[targetIndex];
                toList[targetIndex] = dragged;

                if (IsValidIndex(fromList, originIndex))
                    fromList[originIndex] = temp;

                DragSlot.Instance.ClearDrag();
                FreshSlot();
                MarkInventoryDirty();
                return;
            }

            ReturnDragItem();
            return;
        }

        toList[targetIndex] = dragged;

        if (!drag.isSplit && IsValidIndex(fromList, originIndex))
            fromList[originIndex] = null;

        DragSlot.Instance.ClearDrag();
        FreshSlot();
        MarkInventoryDirty();
    }

    private void HandleEquipmentToInventoryDrop(bool isMainTarget, int targetIndex, DragData drag)
    {
        List<ItemStack> toList = isMainTarget ? mainItems : subItems;

        if (!IsValidIndex(toList, targetIndex))
        {
            ReturnDragItem();
            return;
        }

        if (toList[targetIndex] != null && !toList[targetIndex].IsEmpty)
        {
            ReturnDragItem();
            return;
        }

        toList[targetIndex] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);

        if (IsBagUnequipDrag(drag) && !TryShrinkInventoryForUnequippedBag())
        {
            toList[targetIndex] = null;
            ReturnDragItem();
            return;
        }

        DragSlot.Instance.ClearDrag();
        FreshSlot();
        MarkInventoryDirty();
    }

    public void OnEquipmentSlotDrop(EquipmentSlotUI targetSlot)
    {
        if (IsInteractionLocked)
            return;

        DragData drag = GetCurrentDrag();
        if (drag == null)
            return;

        if (targetSlot == null)
        {
            ReturnDragItem();
            return;
        }

        ItemData draggedItemData = drag.draggedItem.itemData;
        if (!targetSlot.CanAccept(draggedItemData))
        {
            ReturnDragItem();
            return;
        }

        if (drag.fromEquipment)
        {
            HandleEquipmentToEquipmentDrop(targetSlot, drag, draggedItemData);
            return;
        }

        HandleInventoryToEquipmentDrop(targetSlot, drag, draggedItemData);
    }

    private void HandleEquipmentToEquipmentDrop(EquipmentSlotUI targetSlot, DragData drag, ItemData draggedItemData)
    {
        EquipmentSlotUI originEquipmentSlot = drag.originEquipmentSlot;
        if (originEquipmentSlot == null)
        {
            ReturnDragItem();
            return;
        }

        if (originEquipmentSlot == targetSlot)
        {
            originEquipmentSlot.SetItem(draggedItemData);
            DragSlot.Instance.ClearDrag();
            return;
        }

        ItemData targetItemData = targetSlot.HasItem ? targetSlot.ItemData : null;

        if (targetItemData != null)
        {
            if (!originEquipmentSlot.CanAccept(targetItemData))
            {
                ReturnDragItem();
                return;
            }

            targetSlot.SetItem(draggedItemData);
            originEquipmentSlot.SetItem(targetItemData);
        }
        else
        {
            targetSlot.SetItem(draggedItemData);
        }

        DragSlot.Instance.ClearDrag();
        MarkInventoryDirty();
    }

    private void HandleInventoryToEquipmentDrop(EquipmentSlotUI targetSlot, DragData drag, ItemData draggedItemData)
    {
        List<ItemStack> fromList = drag.fromMain ? mainItems : subItems;
        int originIndex = drag.originIndex;

        if (!IsValidIndex(fromList, originIndex))
        {
            ReturnDragItem();
            return;
        }

        if (drag.isSplit)
        {
            ReturnDragItem();
            return;
        }

        ItemData targetItemData = targetSlot.HasItem ? targetSlot.ItemData : null;

        if (targetItemData == null)
        {
            targetSlot.SetItem(draggedItemData);
            TryExpandInventoryForEquippedBag(targetSlot, draggedItemData);
            DragSlot.Instance.ClearDrag();
            FreshSlot();
            return;
        }

        fromList[originIndex] = new ItemStack(targetItemData, 1);
        targetSlot.SetItem(draggedItemData);
        TryExpandInventoryForEquippedBag(targetSlot, draggedItemData);

        DragSlot.Instance.ClearDrag();
        FreshSlot();
        MarkInventoryDirty();
    }

    public void ReturnDragItem()
    {
        DragData drag = GetCurrentDrag();
        if (drag == null)
            return;

        if (drag.fromEquipment)
        {
            if (drag.originEquipmentSlot != null)
                drag.originEquipmentSlot.SetItem(drag.draggedItem.itemData);

            DragSlot.Instance.ClearDrag();
            FreshSlot();
            return;
        }

        List<ItemStack> list = drag.fromMain ? mainItems : subItems;
        int origin = drag.originIndex;

        if (!IsValidIndex(list, origin))
        {
            DragSlot.Instance.ClearDrag();
            FreshSlot();
            return;
        }

        int id = drag.draggedItem.itemData.id;

        if (drag.isSplit)
        {
            ItemStack originStack = list[origin];

            if (originStack == null || originStack.IsEmpty)
            {
                list[origin] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);
            }
            else if (originStack.itemData.id == id)
            {
                originStack.Add(drag.draggedItem.count);
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null)
                    {
                        list[i] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);
                        break;
                    }
                }
            }
        }
        else
        {
            list[origin] = drag.draggedItem;
        }

        DragSlot.Instance.ClearDrag();
        FreshSlot();
    }

    public void SetInteractionLocked(bool isLocked)
    {
        IsInteractionLocked = isLocked;
    }

    public bool TryCreateDiscardRequest(out DiscardRequest request)
    {
        request = DiscardRequest.Create(GetCurrentDrag());
        return request != null && request.itemId > 0 && request.maxCount > 0;
    }

    public bool TryDiscardItem(DiscardRequest request)
    {
        if (request == null)
            return false;

        return TryDiscardItem(request, request.maxCount);
    }

    public bool TryDiscardItem(DiscardRequest request, int discardCount)
    {
        if (request == null || request.itemId <= 0 || request.maxCount <= 0)
            return false;

        int clampedDiscardCount = Mathf.Clamp(discardCount, 0, request.maxCount);
        if (clampedDiscardCount <= 0)
            return false;

        bool discarded = request.fromEquipment
            ? TryDiscardEquipmentItem(request, clampedDiscardCount)
            : TryDiscardInventoryItem(request, clampedDiscardCount);

        if (discarded)
        {
            FreshSlot();
            MarkInventoryDirty();
        }

        return discarded;
    }

    public void DiscardItem()
    {
        DragData drag = GetCurrentDrag();
        if (drag == null)
            return;

        Debug.Log($"아이템 버려짐: {drag.draggedItem.itemData.itemName} x{drag.draggedItem.count} (id={drag.draggedItem.itemData.id})");

        DragSlot.Instance.ClearDrag();
        FreshSlot();
        MarkInventoryDirty();
    }

    public bool IsPointerOverSlot(out Inven_Slot slot)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            slot = result.gameObject.GetComponentInParent<Inven_Slot>();
            if (slot != null)
                return true;
        }

        slot = null;
        return false;
    }

    public bool IsPointerOverEquipmentSlot(out EquipmentSlotUI equipmentSlot)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            equipmentSlot = result.gameObject.GetComponentInParent<EquipmentSlotUI>();
            if (equipmentSlot != null)
                return true;
        }

        equipmentSlot = null;
        return false;
    }

    public bool IsPointerOverTrashSlot(out TrashSlot trashSlot)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            trashSlot = result.gameObject.GetComponentInParent<TrashSlot>();
            if (trashSlot != null)
                return true;
        }

        trashSlot = null;
        return false;
    }

    private void HandleHotbarSelection_1to9()
    {
        for (int n = 1; n <= 9; n++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + n);
            if (!Input.GetKeyDown(key))
                continue;

            SelectHotbarIndex(n - 1);
            return;
        }
    }

    private void SelectHotbarIndex(int index)
    {
        if (!IsValidIndex(subItems, index))
        {
            return;
        }

        selectedHotbarIndex = index;
        RefreshCurrentSlotIndicator();

        ItemStack selectedStack = subItems[index];
        if (selectedStack == null || selectedStack.IsEmpty || selectedStack.itemData == null)
        {
            Debug.Log($"{index + 1}번 핫바 선택: 빈 슬롯");
            return;
        }

        Debug.Log($"{index + 1}번 핫바 선택: {selectedStack.itemData.itemName} (id={selectedStack.itemData.id})");
    }

    private void HandleLeftClick_Test()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (!IsValidIndex(subItems, selectedHotbarIndex))
            return;

        if (subItems[selectedHotbarIndex] == null || subItems[selectedHotbarIndex].IsEmpty)
            return;

        Debug.Log($"좌클릭: 핫바 아이템 = {subItems[selectedHotbarIndex].itemData.itemName} (id={subItems[selectedHotbarIndex].itemData.id})");
    }

    // 역할: 현재 선택된 핫바 슬롯 위치로 선택 이미지를 이동시킨다.
    private void RefreshCurrentSlotIndicator()
    {
        if (currentSlotIndicator == null)
            return;

        if (subSlots == null || subSlots.Length == 0 || !IsValidIndex(subItems, selectedHotbarIndex))
        {
            currentSlotIndicator.gameObject.SetActive(false);
            return;
        }

        CacheCurrentSlotIndicatorStartPosition();

        if (!currentSlotIndicator.gameObject.activeSelf)
            currentSlotIndicator.gameObject.SetActive(true);

        Vector2 targetAnchoredPosition = currentSlotIndicatorStartAnchoredPosition;
        targetAnchoredPosition.x += currentSlotStepX * selectedHotbarIndex;
        currentSlotIndicator.anchoredPosition = targetAnchoredPosition;
    }

    // 역할: 선택 표시 이미지의 1번 슬롯 기준 시작 위치를 한 번만 캐싱한다.
    private void CacheCurrentSlotIndicatorStartPosition()
    {
        if (hasCurrentSlotIndicatorStartPosition || currentSlotIndicator == null)
            return;

        currentSlotIndicatorStartAnchoredPosition = currentSlotIndicator.anchoredPosition;
        hasCurrentSlotIndicatorStartPosition = true;
    }

    public ItemData FindItemById(int id)
    {
        if (idToItem.TryGetValue(id, out ItemData item))
            return item;

        return null;
    }

    // 역할: 현재 선택된 핫바 슬롯의 아이템 데이터를 조회한다.
    public bool TryGetSelectedHotbarItem(out ItemData itemData)
    {
        itemData = null;

        if (!IsValidIndex(subItems, selectedHotbarIndex))
            return false;

        ItemStack selectedStack = subItems[selectedHotbarIndex];
        if (selectedStack == null || selectedStack.IsEmpty || selectedStack.itemData == null)
            return false;

        itemData = selectedStack.itemData;
        return true;
    }

    public void SetMainSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= mainSlots.Length)
            return;

        ItemData item = FindItemById(itemId);
        if (item == null)
            return;

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        EnsureListSize(mainItems, mainSlots.Length);
        mainItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
        MarkInventoryDirty();
    }

    public void SetSubSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= subSlots.Length)
            return;

        ItemData item = FindItemById(itemId);
        if (item == null)
            return;

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        EnsureListSize(subItems, subSlots.Length);
        subItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
        MarkInventoryDirty();
    }

    public void WriteInventorySaveData(InventorySaveData saveData)
    {
        if (saveData == null)
            return;

        saveData.mainSlots.Clear();
        saveData.subSlots.Clear();
        saveData.equipmentSlots.Clear();

        // 빈 슬롯은 제외하고 점유 슬롯만 저장한다.
        AppendSlotSaveData(mainItems, saveData.mainSlots);
        AppendSlotSaveData(subItems, saveData.subSlots);
        AppendEquipmentSaveData(saveData.equipmentSlots);
    }

    public void LoadInventorySaveData(InventorySaveData saveData)
    {
        // 기존 런타임 상태를 먼저 비운 뒤 저장 데이터를 다시 적용한다.
        ResetInventoryState();

        if (saveData == null)
        {
            FreshSlot();
            return;
        }

        // Bag 장비를 먼저 복원해야 확장 슬롯 수가 맞는다.
        ApplyEquipmentSaveData(saveData.equipmentSlots, true);
        ApplySlotSaveData(mainItems, mainSlots, saveData.mainSlots);
        ApplySlotSaveData(subItems, subSlots, saveData.subSlots);
        ApplyEquipmentSaveData(saveData.equipmentSlots, false);

        FreshSlot();
    }

    private static void MarkInventoryDirty()
    {
        GameSession.MarkDataDirty();
    }

    private void ResetInventoryState()
    {
        ClearItemList(mainItems);
        ClearItemList(subItems);
        ClearEquipmentSlots();
        ResetMainSlotsToDefaultCount();
        FreshSlot();
    }

    private void ResetMainSlotsToDefaultCount()
    {
        int extraSlotCount = GetCurrentExtraMainSlotCount();
        if (extraSlotCount > 0)
            RemoveMainSlots(extraSlotCount);
    }

    private void ClearEquipmentSlots()
    {
        if (equipmentSlots == null)
            return;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
                equipmentSlots[i].ClearSlot();
        }
    }

    private static void ClearItemList(List<ItemStack> list)
    {
        if (list == null)
            return;

        for (int i = 0; i < list.Count; i++)
            list[i] = null;
    }

    private static void AppendSlotSaveData(List<ItemStack> source, List<SlotItemSaveData> destination)
    {
        if (source == null || destination == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ItemStack stack = source[i];

            // 빈 슬롯은 저장하지 않는다.
            if (stack == null || stack.IsEmpty || stack.itemData == null)
                continue;

            destination.Add(new SlotItemSaveData(i, stack.itemData.id, stack.count));
        }
    }

    private void AppendEquipmentSaveData(List<EquipmentItemSaveData> destination)
    {
        if (equipmentSlots == null || destination == null)
            return;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            EquipmentSlotUI slot = equipmentSlots[i];
            if (slot == null || !slot.HasItem || slot.ItemData == null)
                continue;

            destination.Add(new EquipmentItemSaveData(slot.SlotType, slot.ItemData.id));
        }
    }

    private void ApplySlotSaveData(List<ItemStack> targetList, Inven_Slot[] targetSlots, List<SlotItemSaveData> source)
    {
        if (targetList == null || targetSlots == null || source == null)
            return;

        EnsureListSize(targetList, targetSlots.Length);

        for (int i = 0; i < source.Count; i++)
        {
            SlotItemSaveData slotSaveData = source[i];
            if (slotSaveData == null)
                continue;

            if (slotSaveData.slotIndex < 0 || slotSaveData.slotIndex >= targetList.Count)
                continue;

            ItemData item = FindItemById(slotSaveData.itemId);
            if (item == null)
                continue;

            targetList[slotSaveData.slotIndex] = new ItemStack(item, slotSaveData.count);
        }
    }

    private void ApplyEquipmentSaveData(List<EquipmentItemSaveData> source, bool bagOnly)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            EquipmentItemSaveData slotSaveData = source[i];
            if (slotSaveData == null)
                continue;

            bool isBagSlot = slotSaveData.slotType == EquipSlotType.Bag;
            if (bagOnly != isBagSlot)
                continue;

            ItemData item = FindItemById(slotSaveData.itemId);
            if (item == null)
                continue;

            EquipmentSlotUI slot = FindEquipmentSlot(slotSaveData.slotType);
            if (slot == null)
                continue;

            slot.SetItem(item);
            TryExpandInventoryForEquippedBag(slot, item);
        }
    }

    private EquipmentSlotUI FindEquipmentSlot(EquipSlotType slotType)
    {
        if (equipmentSlots == null)
            return null;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            EquipmentSlotUI slot = equipmentSlots[i];
            if (slot == null)
                continue;

            if (slot.SlotType == slotType)
                return slot;
        }

        return null;
    }
    private DragData GetCurrentDrag()
    {
        if (DragSlot.Instance == null)
            return null;

        DragData drag = DragSlot.Instance.dragData;
        if (drag == null || drag.draggedItem == null || drag.draggedItem.itemData == null)
            return null;

        return drag;
    }

    private int GetCurrentExtraMainSlotCount()
    {
        int currentMainSlotCount = mainSlots != null ? mainSlots.Length : 0;
        return Mathf.Max(0, currentMainSlotCount - defaultMainSlotCount);
    }

    private bool IsBagUnequipDrag(DragData drag)
    {
        if (drag == null || !drag.fromEquipment || drag.originEquipmentSlot == null)
            return false;

        return drag.originEquipmentSlot.SlotType == EquipSlotType.Bag && IsBagItem(drag.draggedItem.itemData);
    }

    private bool IsBagItem(ItemData itemData)
    {
        return itemData != null && itemData.CanEquipTo(EquipSlotType.Bag);
    }

    private void TryExpandInventoryForEquippedBag(EquipmentSlotUI targetSlot, ItemData equippedItem)
    {
        if (targetSlot == null || targetSlot.SlotType != EquipSlotType.Bag)
            return;

        if (!IsBagItem(equippedItem))
            return;

        int missingExtraSlotCount = bagExtraSlotCount - GetCurrentExtraMainSlotCount();
        if (missingExtraSlotCount > 0)
            AddMainSlots(missingExtraSlotCount);
    }

    private bool TryShrinkInventoryForUnequippedBag()
    {
        int removableSlotCount = Mathf.Min(bagExtraSlotCount, GetCurrentExtraMainSlotCount());
        if (removableSlotCount <= 0)
            return true;

        if (CountEmptySlots(mainItems) < removableSlotCount)
            return false;

        CompactMainItems();
        RemoveMainSlots(removableSlotCount);
        return true;
    }

    private static int CountEmptySlots(List<ItemStack> list)
    {
        if (list == null)
            return 0;

        int emptyCount = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || list[i].IsEmpty)
                emptyCount++;
        }

        return emptyCount;
    }

    private void CompactMainItems()
    {
        int writeIndex = 0;

        for (int readIndex = 0; readIndex < mainItems.Count; readIndex++)
        {
            ItemStack stack = mainItems[readIndex];
            if (stack == null || stack.IsEmpty)
                continue;

            if (writeIndex != readIndex)
            {
                mainItems[writeIndex] = stack;
                mainItems[readIndex] = null;
            }

            writeIndex++;
        }

        for (int i = writeIndex; i < mainItems.Count; i++)
            mainItems[i] = null;
    }

    private static bool IsValidIndex(List<ItemStack> list, int index)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    private bool TryDiscardInventoryItem(DiscardRequest request, int discardCount)
    {
        List<ItemStack> list = request.fromMain ? mainItems : subItems;
        if (!IsValidIndex(list, request.originIndex))
            return false;

        ItemStack stack = list[request.originIndex];
        if (stack == null || stack.IsEmpty || stack.itemData == null)
            return false;

        if (stack.itemData.id != request.itemId || stack.count < discardCount)
            return false;

        int removedCount = stack.Remove(discardCount);
        if (removedCount <= 0)
            return false;

        if (stack.IsEmpty)
            list[request.originIndex] = null;

        Debug.Log($"아이템 버려짐: id={request.itemId}, count={removedCount}");
        return true;
    }

    private bool TryDiscardEquipmentItem(DiscardRequest request, int discardCount)
    {
        if (discardCount != 1)
            return false;

        EquipmentSlotUI slot = FindEquipmentSlot(request.equipmentSlotType);
        if (slot == null || !slot.HasItem || slot.ItemData == null)
            return false;

        if (slot.ItemData.id != request.itemId)
            return false;

        if (slot.SlotType == EquipSlotType.Bag && IsBagItem(slot.ItemData))
        {
            if (!TryShrinkInventoryForUnequippedBag())
                return false;
        }

        slot.ClearSlot();
        Debug.Log($"장비 아이템 버려짐: id={request.itemId}, count=1");
        return true;
    }
}


