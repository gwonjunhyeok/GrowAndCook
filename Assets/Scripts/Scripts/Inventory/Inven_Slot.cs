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

    private void Start()
    {
        invenSystem = FindObjectOfType<Inven_System>();
    }

    private void Update()
    {
        if (isPointerDown)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= requiredHoldTime)
            {
                isPointerDown = false;

                // 아이템이 있는 경우에만 드래그 시작
                if (hasItem)
                {
                    invenSystem.OnSlotClicked(isMainSlot, slotIndex);
                }
            }
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
            // null 데이터나 0개수면 빈 슬롯 처리
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
        // 필요하면 아이콘 자체를 숨기고 싶을 때 주석 해제
        // image.enabled = false;

        countText.text = "";
    }

    public void OnPointerDown(PointerEventData eventData) // 함수는 해당 오브젝트 위에서 마우스 버튼이 내려가는 순간
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isPointerDown = true;
            holdTime = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData) // 함수는 해당 오브젝트 위에서  눌렀던 마우스 버튼을 떼는 순간
    {
        isPointerDown = false;
        holdTime = 0f;

        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 드래그 중이었다면 드롭 처리
        if (DragSlot.Instance.dragData != null)
        {
            if (invenSystem.IsPointerOverSlot(out Inven_Slot targetSlot))
            {
                invenSystem.OnSlotDrop(targetSlot.isMainSlot, targetSlot.slotIndex);
            }
            else
            {
                if (invenSystem.IsPointerOutsideInventory())
                {
                    invenSystem.DiscardItem(); // 아이템 버리기 처리
                }
                else
                {
                    invenSystem.ReturnDragItem(); // 인벤 내부지만 슬롯 아님: 되돌리기
                }
            }
        }
    }
}
