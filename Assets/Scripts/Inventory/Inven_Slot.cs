using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Inven_Slot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI countText;

    public int slotIndex;      // 이 슬롯의 인덱스
    public bool isMainSlot;    // 메인 인벤인지 여부

    private bool isPointerDown = false;
    private float holdTime = 0f;
    private const float requiredHoldTime = 0.3f;

    private Inven_System invenSystem;

    // 슬롯이 실제로 들고 있는 아이템 정보 (세이브/로드용 포함)
    private bool hasItem = false;
    private ItemData currentItemData = null;
    private int currentCount = 0;

    // 외부에서 읽기용 프로퍼티 (SaveAndLoad 등에서 사용)
    public bool HasItem => hasItem;
    public string ItemName => hasItem && currentItemData != null ? currentItemData.itemName : "";
    public int Count => hasItem ? currentCount : 0;
    public ItemData ItemData => currentItemData;

    private void Awake()
    {
        // FindObjectOfType는 슬롯이 많아지면 비용이 커서 지양
        invenSystem = Inven_System.instance != null
    ? Inven_System.instance
    : Object.FindFirstObjectByType<Inven_System>();
    }

    private void Update()
    {
        if (!isPointerDown) return;

        holdTime += Time.deltaTime;
        if (holdTime < requiredHoldTime) return;

        isPointerDown = false;
        holdTime = 0f;

        // 아이템이 있는 경우에만 드래그 시작
        if (hasItem && invenSystem != null)
        {
            invenSystem.OnSlotClicked(isMainSlot, slotIndex);
        }
    }

    // Inven_System에서 호출: 이 슬롯에 아이템 세팅
    public void SetItem(ItemData data, int count)
    {
        currentItemData = data;
        currentCount = count;

        if (data != null && count > 0)
        {
            image.sprite = data.icon;
            image.enabled = true;
            countText.text = count > 1 ? count.ToString() : "";
            hasItem = true;
        }
        else
        {
            ClearSlot();
        }
    }

    // 이 슬롯 비우기
    public void ClearSlot()
    {
        currentItemData = null;
        currentCount = 0;
        hasItem = false;

        image.sprite = null;
        // 빈 슬롯은 아이콘을 숨기는 게 일반적이라 enabled=false 권장
        image.enabled = false;

        countText.text = "";
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 인벤 시스템이 없는 경우 방어
        if (invenSystem == null) return;

        // 슬롯에 아이템이 없으면 홀드 체크 자체를 하지 않음(불필요한 Update 비용 감소)
        if (!hasItem) return;

        isPointerDown = true;
        holdTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        holdTime = 0f;

        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (invenSystem == null) return;

        // 드래그 중이었다면 드롭 처리
        if (DragSlot.Instance != null && DragSlot.Instance.dragData != null)
        {
            // 슬롯 위에 올려놓은 경우(타겟 슬롯 찾기)
            if (invenSystem.IsPointerOverSlot(out Inven_Slot targetSlot) && targetSlot != null)
            {
                invenSystem.OnSlotDrop(targetSlot.isMainSlot, targetSlot.slotIndex);
            }
            else
            {
                // 슬롯 위가 아니라면 무조건 원위치 복귀
                // (휴지통은 별도의 TrashSlot(IDropHandler)에서 처리할 예정)
                invenSystem.ReturnDragItem();
            }
        }
    }
}
