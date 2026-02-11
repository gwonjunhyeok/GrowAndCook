using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Inven_System : MonoBehaviour
{
    public List<ItemStack> items = new(); // 현재 미사용(원하면 제거 가능)
    public static Inven_System instance;

    [SerializeField] private Transform mainSlotParent;
    [SerializeField] private Transform subSlotParent;
    [SerializeField] private Inven_Slot[] mainSlots; // 핫바(10칸)
    [SerializeField] private Inven_Slot[] subSlots;  // 메인 인벤(예: 10x2 등)
    [SerializeField] private RectTransform[] inventoryPanels; // 드래그 외부 판단용(추후 완전 제거 가능)

    // 고정 슬롯처럼 사용할 리스트(길이를 슬롯 개수로 고정)
    private List<ItemStack> mainItems = new();
    private List<ItemStack> subItems = new();

    // 핫바 선택 인덱스 (-1이면 선택 안됨)
    private int selectedHotbarIndex = -1;

    // ItemData 캐싱 (성능/안정성)
    private readonly Dictionary<int, ItemData> idToItem = new();
    private readonly Dictionary<string, ItemData> nameToItem = new();

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
        if (instance == null) instance = this;

        // 슬롯 메타 세팅
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

        BuildItemDatabase();
        InitFixedSlots();
        FreshSlot();
    }

    private void BuildItemDatabase()
    {
        idToItem.Clear();
        nameToItem.Clear();

        ItemData[] allItems = Resources.LoadAll<ItemData>("");

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData item = allItems[i];
            if (item == null) continue;

            // id 캐시
            if (idToItem.ContainsKey(item.id))
            {
                Debug.LogWarning($"ItemData ID 중복 발견: id={item.id}, name={item.itemName}");
            }
            else
            {
                idToItem.Add(item.id, item);
            }

            // name 캐시(호환용). 동일 이름이 있으면 마지막 것으로 덮어씀
            if (!string.IsNullOrEmpty(item.itemName))
            {
                nameToItem[item.itemName] = item;
            }
        }
    }

    private void InitFixedSlots()
    {
        mainItems.Clear();
        subItems.Clear();

        for (int i = 0; i < mainSlots.Length; i++)
            mainItems.Add(null);

        for (int i = 0; i < subSlots.Length; i++)
            subItems.Add(null);

        selectedHotbarIndex = -1;
    }

    private void Update()
    {
        // 1~0 키로 핫바 선택
        HandleHotbarSelection_1to0();

        // 좌클릭 테스트(현재 선택된 핫바 아이템명 출력)
        HandleLeftClick_Test();

        // 테스트용 아이템 지급(원하면 제거)
        if (Input.GetKeyDown(KeyCode.H))
        {
            // 예시: 이름으로 찾기(테스트용)
            ItemData item = FindItemByName("flashlight");
            if (item != null) AddItem(item);
            else Debug.LogWarning("아이템 'flashlight'가 없습니다.");
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

        if (TryAddToList(mainItems, item)) { FreshSlot(); return; }
        if (TryAddToList(subItems, item)) { FreshSlot(); return; }

        Debug.Log("인벤토리 공간이 부족합니다.");
    }

    // 고정 슬롯 리스트: 스택 합치기 -> 빈칸 넣기 (ID 기준)
    private bool TryAddToList(List<ItemStack> list, ItemData item)
    {
        int id = item.id;

        // 기존 스택에 추가
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

        // 빈 칸에 새로 생성
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

        if (isSplit)
        {
            stack.Remove(moveCount);
        }
        else
        {
            list[index] = null;
        }

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
        var originIndex = drag.originIndex;

        int draggedId = dragged.itemData.id;

        // 타겟에 아이템이 있는 경우
        if (toList[targetIndex] != null && !toList[targetIndex].IsEmpty)
        {
            var targetStack = toList[targetIndex];

            // Ctrl(절반 이동)인데 다른 아이템이면 무조건 복귀
            if (drag.isSplit && targetStack.itemData.id != draggedId)
            {
                ReturnDragItem();
                return;
            }

            // 같은 아이템이면 합치기
            if (targetStack.itemData.id == draggedId)
            {
                // targetStack에 dragged.count 만큼 넣고, 남은 건 원복
                int added = targetStack.Add(dragged.count);
                int remain = dragged.count - added;

                if (remain <= 0)
                {
                    DragSlot.Instance.ClearDrag();
                    FreshSlot();
                    return;
                }

                // 남은 수량은 복귀
                dragged.count = remain;
                ReturnDragItem();
                return;
            }

            // 다른 아이템: 전체 이동일 때만 스왑 허용
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

        // 타겟이 빈 슬롯인 경우
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

        // Ctrl 절반 이동: 원본 슬롯에 수량을 다시 더해 원복
        if (drag.isSplit)
        {
            var originStack = list[origin];

            if (originStack == null)
            {
                list[origin] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);
            }
            else if (!originStack.IsEmpty && originStack.itemData.id == id)
            {
                originStack.Add(drag.draggedItem.count);
            }
            else if (originStack.IsEmpty)
            {
                list[origin] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);
            }
            else
            {
                // 꼬임 방지: 빈칸 탐색 후 넣기
                bool placed = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null)
                    {
                        list[i] = new ItemStack(drag.draggedItem.itemData, drag.draggedItem.count);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    Debug.LogWarning("ReturnDragItem 실패: 빈 슬롯이 없어 분할 아이템을 되돌릴 수 없습니다.");
                }
            }
        }
        else
        {
            // 전체 이동: 원본 슬롯에 다시 넣기
            list[origin] = drag.draggedItem;
        }

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

    // 상단 숫자키 1~0으로 핫바 선택
    private void HandleHotbarSelection_1to0()
    {
        // 1~9 -> 0~8
        for (int n = 1; n <= 9; n++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + n); // Alpha1..Alpha9
            if (!Input.GetKeyDown(key)) continue;

            SelectHotbarIndex(n - 1);
            return;
        }

        // 0 -> 9
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SelectHotbarIndex(9);
        }
    }

    private void SelectHotbarIndex(int index)
    {
        if (index < 0 || index >= mainItems.Count)
        {
            selectedHotbarIndex = -1;
            Debug.Log("핫바 선택 해제(범위 밖)");
            return;
        }

        if (mainItems[index] == null || mainItems[index].IsEmpty)
        {
            selectedHotbarIndex = -1;
            Debug.Log($"{index + 1}번 핫바: 아이템이 없습니다.");
            return;
        }

        selectedHotbarIndex = index;
        Debug.Log($"{GetHotbarKeyLabel(index)}번 핫바 선택: {mainItems[index].itemData.itemName} (id={mainItems[index].itemData.id})");
    }

    private string GetHotbarKeyLabel(int index)
    {
        // index 0..8 -> "1".."9", index 9 -> "0"
        if (index == 9) return "0";
        return (index + 1).ToString();
    }

    // 좌클릭 테스트(선택된 핫바 아이템명 출력)
    private void HandleLeftClick_Test()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (selectedHotbarIndex < 0) return;
        if (selectedHotbarIndex >= mainItems.Count) return;
        if (mainItems[selectedHotbarIndex] == null || mainItems[selectedHotbarIndex].IsEmpty) return;

        string itemName = mainItems[selectedHotbarIndex].itemData.itemName;
        int id = mainItems[selectedHotbarIndex].itemData.id;
        Debug.Log($"좌클릭: 선택된 핫바 아이템 = {itemName} (id={id})");
    }

    // ID로 ItemData 찾기
    public ItemData FindItemById(int id)
    {
        if (idToItem.TryGetValue(id, out var item)) return item;
        return null;
    }

    // 이름으로 ItemData 찾기(호환/테스트용)
    private ItemData FindItemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (nameToItem.TryGetValue(name, out var item)) return item;
        return null;
    }

    public bool IsPointerOutsideInventory()
    {
        Vector2 mousePos = Input.mousePosition;
        foreach (RectTransform panel in inventoryPanels)
        {
            if (panel != null && RectTransformUtility.RectangleContainsScreenPoint(panel, mousePos))
                return false;
        }
        return true;
    }

    public void DiscardItem()
    {
        var drag = DragSlot.Instance.dragData;
        if (drag == null || drag.draggedItem == null || drag.draggedItem.itemData == null) return;

        Debug.Log($"아이템 버려짐: {drag.draggedItem.itemData.itemName} x{drag.draggedItem.count} (id={drag.draggedItem.itemData.id})");
        DragSlot.Instance.ClearDrag();
        FreshSlot();
    }

    public Inven_Slot[] GetMainSlots() => mainSlots;
    public Inven_Slot[] GetSubSlots() => subSlots;

    public void ClearAllSlots()
    {
        for (int i = 0; i < mainItems.Count; i++) mainItems[i] = null;
        for (int i = 0; i < subItems.Count; i++) subItems[i] = null;

        selectedHotbarIndex = -1;
        FreshSlot();
    }

    // 세이브/로드: 기존 이름 기반 유지(호환용). 추후 int id 기반으로 바꾸는 걸 추천
    public void SetMainSlot(int index, string itemName, int count)
    {
        if (index < 0 || index >= mainSlots.Length) return;

        ItemData item = FindItemByName(itemName);
        if (item == null)
        {
            Debug.LogWarning($"SetMainSlot 실패: '{itemName}' 아이템을 찾을 수 없음");
            return;
        }

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        mainItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }

    public void SetSubSlot(int index, string itemName, int count)
    {
        if (index < 0 || index >= subSlots.Length) return;

        ItemData item = FindItemByName(itemName);
        if (item == null)
        {
            Debug.LogWarning($"SetSubSlot 실패: '{itemName}' 아이템을 찾을 수 없음");
            return;
        }

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        subItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }

    // 세이브/로드: ID 기반(권장)
    public void SetMainSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= mainSlots.Length) return;

        ItemData item = FindItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"SetMainSlotById 실패: id={itemId} 아이템을 찾을 수 없음");
            return;
        }

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        mainItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }

    public void SetSubSlotById(int index, int itemId, int count)
    {
        if (index < 0 || index >= subSlots.Length) return;

        ItemData item = FindItemById(itemId);
        if (item == null)
        {
            Debug.LogWarning($"SetSubSlotById 실패: id={itemId} 아이템을 찾을 수 없음");
            return;
        }

        int clampedCount = Mathf.Clamp(count, 1, item.maxStack);
        subItems[index] = new ItemStack(item, clampedCount);

        FreshSlot();
    }
}
