using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Inven_System : MonoBehaviour
{
    public static Inven_System instance;

    [Header("Parents")]
    [SerializeField] private Transform mainSlotParent; // Content
    [SerializeField] private Transform subSlotParent;  // Hotbar

    [Header("Slots")]
    [SerializeField] private Inven_Slot[] mainSlots;   // Content slots
    [SerializeField] private Inven_Slot[] subSlots;    // Hotbar slots

    [Header("Add Slots")]
    [SerializeField] private Inven_Slot mainSlotPrefab; // 메인 슬롯 프리팹(루트에 Inven_Slot 붙어있는 프리팹)
    [SerializeField] private int addCountPerPress = 9;  // P 키 누를 때 추가 개수

    [SerializeField] private RectTransform[] inventoryPanels; // 드롭 외부 판정용(선택)

    private readonly List<ItemStack> mainItems = new();
    private readonly List<ItemStack> subItems = new();

    private int selectedHotbarIndex = -1;
    private readonly Dictionary<int, ItemData> idToItem = new();

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
        if (instance == null) instance = this;

        RebuildSlotArraysAndReindex();

        BuildItemDatabase();
        InitFixedSlots();
        FreshSlot();
    }

    private void Update()
    {
        HandleHotbarSelection_1to9();
        HandleLeftClick_Test();

        // P 키: 메인 슬롯 9개 추가
        if (Input.GetKeyDown(KeyCode.P))
        {
            AddMainSlots(addCountPerPress);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            const int testItemId = 1;
            ItemData item = FindItemById(testItemId);
            if (item != null) AddItem(item);
            else Debug.LogWarning($"아이템 id={testItemId}가 없습니다.");
        }
    }

    // 메인 슬롯을 count개 추가하고, 배열/인덱스/데이터를 모두 동기화
    private void AddMainSlots(int count)
    {
        if (count <= 0) return;
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
        {
            Instantiate(mainSlotPrefab, mainSlotParent);
        }

        // 슬롯 배열 재수집 + slotIndex 재부여 + mainItems 길이 동기화
        RebuildSlotArraysAndReindex();

        FreshSlot();
    }

    // Content/Hotbar 슬롯을 다시 수집하고, slotIndex/isMainSlot을 재부여하며,
    // mainItems/subItems 리스트 길이를 슬롯 수에 맞게 맞춘다.
    private void RebuildSlotArraysAndReindex()
    {
        if (mainSlotParent != null)
            mainSlots = mainSlotParent.GetComponentsInChildren<Inven_Slot>(true);

        if (subSlotParent != null)
            subSlots = subSlotParent.GetComponentsInChildren<Inven_Slot>(true);

        // 메인 슬롯 인덱스 재부여
        if (mainSlots != null)
        {
            for (int i = 0; i < mainSlots.Length; i++)
            {
                mainSlots[i].Init(true, i);
            }
        }

        // 핫바 슬롯 인덱스 재부여
        if (subSlots != null)
        {
            for (int i = 0; i < subSlots.Length; i++)
            {
                subSlots[i].Init(false, i);
            }
        }

        // 데이터 리스트 길이 맞추기
        EnsureListSize(mainItems, mainSlots != null ? mainSlots.Length : 0);
        EnsureListSize(subItems, subSlots != null ? subSlots.Length : 0);
    }

    private static void EnsureListSize(List<ItemStack> list, int size)
    {
        if (list == null) return;
        while (list.Count < size) list.Add(null);
        // 슬롯이 줄어드는 케이스는 지금 요구사항엔 없으니 Remove는 하지 않음
    }

    private void BuildItemDatabase()
    {
        idToItem.Clear();
        ItemData[] allItems = Resources.LoadAll<ItemData>("");

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData item = allItems[i];
            if (item == null) continue;

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
        // mainItems/subItems는 EnsureListSize로 이미 슬롯 길이 맞춰짐
        selectedHotbarIndex = -1;
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
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (TryAddToList(subItems, item)) { FreshSlot(); return; }
        if (TryAddToList(mainItems, item)) { FreshSlot(); return; }

        Debug.Log("인벤토리 공간이 부족합니다.");
    }

    private bool TryAddToList(List<ItemStack> list, ItemData item)
    {
        int id = item.id;

        for (int i = 0; i < list.Count; i++)
        {
            var stack = list[i];
            if (stack == null || stack.IsEmpty) continue;

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
        var list = isMain ? mainItems : subItems;
        if (index < 0 || index >= list.Count) return;
        if (list[index] == null || list[index].IsEmpty) return;

        var stack = list[index];

        bool isSplit = Input.GetKey(KeyCode.LeftControl);
        int moveCount = isSplit ? Mathf.FloorToInt(stack.count / 2f) : stack.count;
        if (moveCount <= 0) return;

        if (isSplit) stack.Remove(moveCount);
        else list[index] = null;

        DragSlot.Instance.StartDrag(isMain, index, new ItemStack(stack.itemData, moveCount), isSplit);
        FreshSlot();
    }

    public void OnSlotDrop(bool isMainTarget, int targetIndex)
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null || drag.draggedItem == null || drag.draggedItem.itemData == null)
            return;

        var fromList = drag.fromMain ? mainItems : subItems;
        var toList = isMainTarget ? mainItems : subItems;

        if (targetIndex < 0 || targetIndex >= toList.Count)
        {
            ReturnDragItem();
            return;
        }

        var dragged = drag.draggedItem;
        int draggedId = dragged.itemData.id;
        int originIndex = drag.originIndex;

        if (toList[targetIndex] != null && !toList[targetIndex].IsEmpty)
        {
            var targetStack = toList[targetIndex];

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
                    return;
                }

                dragged.count = remain;
                ReturnDragItem();
                return;
            }

            if (!drag.isSplit)
            {
                var temp = toList[targetIndex];
                toList[targetIndex] = dragged;

                if (originIndex >= 0 && originIndex < fromList.Count)
                    fromList[originIndex] = temp;

                DragSlot.Instance.ClearDrag();
                FreshSlot();
                return;
            }

            ReturnDragItem();
            return;
        }

        toList[targetIndex] = dragged;

        if (!drag.isSplit && originIndex >= 0 && originIndex < fromList.Count)
            fromList[originIndex] = null;

        DragSlot.Instance.ClearDrag();
        FreshSlot();
    }

    public void ReturnDragItem()
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null || drag.draggedItem == null || drag.draggedItem.itemData == null) return;

        var list = drag.fromMain ? mainItems : subItems;
        int origin = drag.originIndex;

        if (origin < 0 || origin >= list.Count)
        {
            DragSlot.Instance.ClearDrag();
            FreshSlot();
            return;
        }

        int id = drag.draggedItem.itemData.id;

        if (drag.isSplit)
        {
            var originStack = list[origin];

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

    public void DiscardItem()
    {
        if (DragSlot.Instance == null || DragSlot.Instance.dragData == null) return;

        var drag = DragSlot.Instance.dragData;
        if (drag.draggedItem == null || drag.draggedItem.itemData == null) return;

        Debug.Log($"아이템 버려짐: {drag.draggedItem.itemData.itemName} x{drag.draggedItem.count} (id={drag.draggedItem.itemData.id})");

        DragSlot.Instance.ClearDrag();
        FreshSlot();
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
            if (slot != null) return true;
        }

        slot = null;
        return false;
    }

    private void HandleHotbarSelection_1to9()
    {
        for (int n = 1; n <= 9; n++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + n);
            if (!Input.GetKeyDown(key)) continue;

            SelectHotbarIndex(n - 1);
            return;
        }
    }

    private void SelectHotbarIndex(int index)
    {
        if (index < 0 || index >= subItems.Count)
        {
            selectedHotbarIndex = -1;
            return;
        }

        if (subItems[index] == null || subItems[index].IsEmpty)
        {
            selectedHotbarIndex = -1;
            return;
        }

        selectedHotbarIndex = index;
        Debug.Log($"{index + 1}번 핫바 선택: {subItems[index].itemData.itemName} (id={subItems[index].itemData.id})");
    }

    private void HandleLeftClick_Test()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (selectedHotbarIndex < 0) return;
        if (selectedHotbarIndex >= subItems.Count) return;
        if (subItems[selectedHotbarIndex] == null || subItems[selectedHotbarIndex].IsEmpty) return;

        Debug.Log($"좌클릭: 핫바 아이템 = {subItems[selectedHotbarIndex].itemData.itemName} (id={subItems[selectedHotbarIndex].itemData.id})");
    }

    public ItemData FindItemById(int id)
    {
        if (idToItem.TryGetValue(id, out var item)) return item;
        return null;
    }

    public void SetMainSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= mainSlots.Length) return;

        ItemData item = FindItemById(itemId);
        if (item == null) return;

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        EnsureListSize(mainItems, mainSlots.Length);
        mainItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }

    public void SetSubSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= subSlots.Length) return;

        ItemData item = FindItemById(itemId);
        if (item == null) return;

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        EnsureListSize(subItems, subSlots.Length);
        subItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }
}