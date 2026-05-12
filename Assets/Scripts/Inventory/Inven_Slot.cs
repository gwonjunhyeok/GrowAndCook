using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Inven_Slot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;           // 아이템 아이콘 이미지 (이것만 컨트롤)
    [SerializeField] private TextMeshProUGUI countText; // 수량 텍스트

    public int slotIndex;
    public bool isMainSlot;

    private bool isPointerDown = false;
    private float holdTime = 0f;
    private const float requiredHoldTime = 0.3f;

    private Inven_System invenSystem;
    private InventoryTooltipController tooltipController;

    private bool hasItem = false;
    private ItemData currentItemData = null;
    private int currentCount = 0;

    public bool HasItem => hasItem;
    public int ItemId => hasItem && currentItemData != null ? currentItemData.id : -1;
    public int Count => hasItem ? currentCount : 0;
    public ItemData ItemData => currentItemData;

    public void Init(bool isMain, int index)
    {
        isMainSlot = isMain;
        slotIndex = index;
    }

#if UNITY_EDITOR
    private void Reset() { AutoBind(); }
    private void OnValidate()
    {
        if (!Application.isPlaying) AutoBind();
    }
#endif

    private void AutoBind()
    {
        // 구조: Root -> "Image (1)"(배경) -> "Image"(아이콘), "Text (TMP)"(수량)
        Transform bg = transform.Find("Image (1)");
        if (bg != null)
        {
            if (iconImage == null)
            {
                Transform icon = bg.Find("Image");
                if (icon != null) iconImage = icon.GetComponent<Image>();
            }

            if (countText == null)
            {
                Transform t = bg.Find("Text (TMP)");
                if (t != null) countText = t.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void Awake()
    {
        invenSystem = Inven_System.instance != null
            ? Inven_System.instance
            : Object.FindFirstObjectByType<Inven_System>();
        tooltipController = GetComponentInParent<InventoryTooltipController>(true);

        if (iconImage == null || countText == null)
            AutoBind();

        // 아이콘/텍스트는 레이캐스트 먹지 않게(슬롯 루트/배경이 입력 받도록)
        if (iconImage != null) iconImage.raycastTarget = false;
        if (countText != null) countText.raycastTarget = false;

        // 시작 상태는 빈 슬롯으로 강제
        ApplyEmptyVisual();
    }

    private void Update()
    {
        if (!isPointerDown) return;

        // UI는 일시정지 중에도 드래그 가능해야 하므로 비스케일 시간을 사용한다.
        holdTime += Time.unscaledDeltaTime;
        if (holdTime < requiredHoldTime) return;

        isPointerDown = false;
        holdTime = 0f;

        if (hasItem && invenSystem != null)
            invenSystem.OnSlotClicked(isMainSlot, slotIndex);
    }

    public void SetItem(ItemData data, int count)
    {
        HideTooltip();

        currentItemData = data;
        currentCount = count;

        if (data != null && count > 0)
        {
            hasItem = true;

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = (data.icon != null); // 아이콘 없으면 보이는 게 정상적으로 불가
            }

            if (countText != null)
            {
                countText.enabled = true;
                countText.text = count > 1 ? count.ToString() : "";
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        HideTooltip();

        currentItemData = null;
        currentCount = 0;
        hasItem = false;

        ApplyEmptyVisual();
    }

    private void ApplyEmptyVisual()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false; // 아이템 없으면 반드시 꺼짐
        }

        if (countText != null)
        {
            countText.text = "";
            countText.enabled = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HideTooltip();

        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (invenSystem == null) return;
        if (invenSystem.IsInteractionLocked) return;
        if (!hasItem) return;

        isPointerDown = true;
        holdTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        holdTime = 0f;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (invenSystem == null)
            return;

        if (DragSlot.Instance == null || DragSlot.Instance.dragData == null)
            return;

        if (invenSystem.IsPointerOverEquipmentSlot(out EquipmentSlotUI equipmentSlot) && equipmentSlot != null)
        {
            invenSystem.OnEquipmentSlotDrop(equipmentSlot);
            return;
        }

        if (invenSystem.IsPointerOverSlot(out Inven_Slot targetSlot) && targetSlot != null)
        {
            invenSystem.OnSlotDrop(targetSlot.isMainSlot, targetSlot.slotIndex);
            return;
        }

        if (invenSystem.IsPointerOverTrashSlot(out TrashSlot trashSlot) && trashSlot != null)
        {
            trashSlot.HandleDropRequest();
            return;
        }

        invenSystem.ReturnDragItem();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasItem || currentItemData == null)
            return;

        if (invenSystem != null && invenSystem.IsInteractionLocked)
            return;

        if (tooltipController == null)
            tooltipController = GetComponentInParent<InventoryTooltipController>(true);

        tooltipController?.RequestShowTooltip(this, currentItemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltipController == null)
            return;

        tooltipController.HideTooltip(this);
    }
}

