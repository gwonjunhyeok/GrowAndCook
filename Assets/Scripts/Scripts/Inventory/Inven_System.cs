using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Inven_System : MonoBehaviour
{
    public List<ItemStack> items = new();
    public static Inven_System instance;

    [SerializeField] private Transform mainSlotParent;
    [SerializeField] private Transform subSlotParent;
    [SerializeField] private Inven_Slot[] mainSlots;
    [SerializeField] private Inven_Slot[] subSlots;
    [SerializeField] private RectTransform[] inventoryPanels; // 드래그 외부 판단용

    private List<ItemStack> mainItems = new();
    private List<ItemStack> subItems = new();

    // 현재 선택된 서브 슬롯 인덱스 (-1이면 선택 안됨)
    private int SelectedSlot = -1;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainSlotParent != null)
            mainSlots = mainSlotParent.GetComponentsInChildren<Inven_Slot>();
        if (subSlotParent != null)
            subSlots = subSlotParent.GetComponentsInChildren<Inven_Slot>();
    }
#endif

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < mainSlots.Length; i++)
        {
            mainSlots[i].slotIndex = i;
            mainSlots[i].isMainSlot = true;
        }

        for (int i = 0; i < subSlots.Length; i++)
        {
            subSlots[i].slotIndex = i;
            subSlots[i].isMainSlot = false;
        }

        FreshSlot();
    }

    void Update()
    {
        // 1~4키로 서브 슬롯 선택
        HandleSubSlotSelection();
        // 좌클릭 시 현재 선택된 슬롯 아이템 출력
        HandleLeftClick();

        // 테스트용 아이템 지급
        if (Input.GetKeyDown(KeyCode.H))
        {
            ItemData item = FindItemByName("flashlight");
            if (item != null) AddItem(item);
            else Debug.LogWarning("아이템 'sword'가 없습니다.");
        }
        else if (Input.GetKeyDown(KeyCode.Y))
        {
            ItemData item = FindItemByName("Gun");
            if (item != null) AddItem(item);
            else Debug.Log("아이템 Gun 이 존재하지 않습니다");
        }
    }

    public void FreshSlot()
    {
        for (int i = 0; i < mainSlots.Length; i++)
        {
            if (i < mainItems.Count && mainItems[i] != null)
                mainSlots[i].SetItem(mainItems[i].itemData, mainItems[i].count);
            else
                mainSlots[i].ClearSlot();
        }

        for (int i = 0; i < subSlots.Length; i++)
        {
            if (i < subItems.Count && subItems[i] != null)
                subSlots[i].SetItem(subItems[i].itemData, subItems[i].count);
            else
                subSlots[i].ClearSlot();
        }
    }

    public void AddItem(ItemData item)
    {
        if (TryAddToList(mainItems, mainSlots.Length, item)) return;
        if (TryAddToList(subItems, subSlots.Length, item)) return;
        Debug.Log("인벤토리 공간이 부족합니다.");
    }

    private bool TryAddToList(List<ItemStack> list, int limit, ItemData item)
    {
        // 기존 스택에 추가
        foreach (var stack in list)
        {
            if (stack != null && stack.itemData.itemName == item.itemName && !stack.IsFull)
            {
                stack.count++;
                FreshSlot();
                return true;
            }
        }

        // null 자리 재사용
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                list[i] = new ItemStack(item);
                FreshSlot();
                return true;
            }
        }

        // 새 슬롯 추가
        if (list.Count < limit)
        {
            list.Add(new ItemStack(item));
            FreshSlot();
            return true;
        }

        return false;
    }

    public void OnSlotClicked(bool isMain, int index)
    {
        var list = isMain ? mainItems : subItems;
        if (index >= list.Count || list[index] == null) return;

        var stack = list[index];
        bool isSplit = Input.GetKey(KeyCode.LeftControl);
        int moveCount = isSplit ? Mathf.FloorToInt(stack.count / 2f) : stack.count;
        if (moveCount <= 0) return;

        if (isSplit) stack.count -= moveCount;
        else list[index] = null;

        DragSlot.Instance.StartDrag(isMain, index, new ItemStack(stack.itemData, moveCount), isSplit);
        FreshSlot();
    }

    public void OnSlotDrop(bool isMainTarget, int targetIndex)
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null) return;

        var fromList = drag.fromMain ? mainItems : subItems;
        var toList = isMainTarget ? mainItems : subItems;

        var dragged = drag.draggedItem;
        var originIndex = drag.originIndex;

        if (targetIndex < toList.Count && toList[targetIndex] != null)
        {
            var targetStack = toList[targetIndex];

            if (targetStack.itemData.itemName == dragged.itemData.itemName)
            {
                int total = targetStack.count + dragged.count;
                int max = targetStack.itemData.maxStack;

                if (total <= max)
                {
                    targetStack.count = total;
                    if (!drag.isSplit) fromList[originIndex] = null;
                }
                else
                {
                    int canMove = max - targetStack.count;
                    targetStack.count = max;
                    dragged.count -= canMove;

                    if (!drag.isSplit)
                    {
                        if (fromList[originIndex] == null)
                            fromList[originIndex] = dragged;
                        else
                            fromList[originIndex].count = dragged.count;
                    }
                    ReturnDragItem();
                }

                DragSlot.Instance.ClearDrag();
                FreshSlot();
                return;
            }
        }

        if (targetIndex < toList.Count && toList[targetIndex] != null)
        {
            var temp = toList[targetIndex];
            toList[targetIndex] = dragged;
            fromList[originIndex] = temp;
        }
        else
        {
            while (toList.Count <= targetIndex) toList.Add(null);
            toList[targetIndex] = dragged;

            if (!drag.isSplit && originIndex < fromList.Count)
                fromList[originIndex] = null;
        }

        DragSlot.Instance.ClearDrag();
        FreshSlot();
    }

    public void ReturnDragItem()
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null) return;

        var list = drag.fromMain ? mainItems : subItems;
        if (drag.originIndex < list.Count && list[drag.originIndex] == null)
            list[drag.originIndex] = drag.draggedItem;
        else if (!list.Contains(drag.draggedItem))
            list.Insert(drag.originIndex, drag.draggedItem);

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

    // 1~4 키로 서브 슬롯 선택
    private void HandleSubSlotSelection()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int index = i; // 0~3

                // 범위를 벗어나면 선택 해제
                if (index >= subItems.Count)
                {
                    Debug.Log($"{index + 1}번 슬롯: 슬롯이 비어있습니다.");
                    SelectedSlot = -1;
                    return;
                }

                // 슬롯엔 있지만 null이면 아이템 없음
                if (subItems[index] == null)
                {
                    Debug.Log($"{index + 1}번 슬롯: 아이템이 없습니다.");
                    SelectedSlot = -1;
                    return;
                }

                // 여기까지 왔으면 아이템 존재
                SelectedSlot = index;
                string itemName = subItems[index].itemData.itemName;
                Debug.Log($"{index + 1}번 슬롯 선택: {itemName}");

                // GameManage.moveState 로직 제거됨 (이번 프로젝트에 GameManage 없음)
                return;
            }
        }
    }

    // 좌클릭 시 현재 선택된 슬롯 아이템 이름 출력
    private void HandleLeftClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (SelectedSlot < 0) return;
            if (SelectedSlot >= subItems.Count) return;
            if (subItems[SelectedSlot] == null) return;

            string itemName = subItems[SelectedSlot].itemData.itemName;
            Debug.Log($"{SelectedSlot + 1}번 슬롯 아이템명: {itemName}");

            if (itemName == "Gun")
            {
                Debug.Log("좌클릭: 현재 선택된 아이템은 Gun 입니다.");
            }
        }
    }

    private ItemData FindItemByName(string name)
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        foreach (var item in allItems)
        {
            if (item.itemName == name) return item;
        }
        return null;
    }

    public bool IsPointerOutsideInventory()
    {
        Vector2 mousePos = Input.mousePosition;
        foreach (RectTransform panel in inventoryPanels)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(panel, mousePos)) return false;
        }
        return true;
    }

    public void DiscardItem()
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null) return;

        Debug.Log($"아이템 버려짐: {drag.draggedItem.itemData.itemName} x{drag.draggedItem.count}");
        DragSlot.Instance.ClearDrag();
        FreshSlot();
    }

    // =====================================================================
    // 여기부터 세이브/로드용으로 추가한 메서드들
    // =====================================================================

    // SaveAndLoad에서 슬롯 배열을 얻을 때 사용
    public Inven_Slot[] GetMainSlots()
    {
        return mainSlots;
    }

    public Inven_Slot[] GetSubSlots()
    {
        return subSlots;
    }

    // 모든 인벤토리 비우기 (로드 전에 호출)
    public void ClearAllSlots()
    {
        mainItems.Clear();
        subItems.Clear();
        FreshSlot();
    }

    // 특정 메인 슬롯에 아이템 세팅 (세이브 로드에서 사용)
    public void SetMainSlot(int index, string itemName, int count)
    {
        if (index < 0 || index >= mainSlots.Length) return;

        ItemData item = FindItemByName(itemName);
        if (item == null)
        {
            Debug.LogWarning($"SetMainSlot 실패: '{itemName}' 이라는 아이템을 찾을 수 없음");
            return;
        }

        while (mainItems.Count <= index)
            mainItems.Add(null);

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);

        ItemStack stack = new ItemStack(item);
        stack.count = clampedCount;

        mainItems[index] = stack;

        FreshSlot();
    }

    // 특정 서브 슬롯에 아이템 세팅 (세이브 로드에서 사용)
    public void SetSubSlot(int index, string itemName, int count)
    {
        if (index < 0 || index >= subSlots.Length) return;

        ItemData item = FindItemByName(itemName);
        if (item == null)
        {
            Debug.LogWarning($"SetSubSlot 실패: '{itemName}' 이라는 아이템을 찾을 수 없음");
            return;
        }

        while (subItems.Count <= index)
            subItems.Add(null);

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);

        ItemStack stack = new ItemStack(item);
        stack.count = clampedCount;

        subItems[index] = stack;

        FreshSlot();
    }
}
