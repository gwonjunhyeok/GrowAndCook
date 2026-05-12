using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 역할: 장착 슬롯 UI를 관리하고, 길게 눌러 드래그 시작 및 드롭 입력만 시스템에 전달한다.
public class EquipmentSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Slot Info")]
    [SerializeField] private EquipSlotType slotType = EquipSlotType.None;

    [Header("Empty Slot Sprite")]
    [SerializeField] private Sprite emptySlotSprite;

    private bool hasItem = false;
    private ItemData currentItemData = null;
    private InventoryTooltipController tooltipController;

    private bool isPointerDown = false;
    private float holdTime = 0f;
    private const float requiredHoldTime = 0.3f;

    public bool HasItem => hasItem;
    public ItemData ItemData => currentItemData;
    public EquipSlotType SlotType => slotType;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoBind();
    }
#endif

    private void AutoBind()
    {
        Transform bg = transform.Find("Image (1)");
        if (bg != null)
        {
            if (iconImage == null)
            {
                Transform icon = bg.Find("Image");
                if (icon != null)
                    iconImage = icon.GetComponent<Image>();
            }
        }

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
    }

    private void Awake()
    {
        tooltipController = GetComponentInParent<InventoryTooltipController>(true);

        if (iconImage == null)
            AutoBind();

        if (iconImage != null)
            iconImage.raycastTarget = false;

        ApplyEmptyVisual();
    }

    private void Update()
    {
        if (!isPointerDown)
            return;

        if (Inven_System.instance != null && Inven_System.instance.IsInteractionLocked)
        {
            isPointerDown = false;
            holdTime = 0f;
            return;
        }

        // UI는 일시정지 중에도 드래그 가능해야 하므로 비스케일 시간을 사용한다.
        holdTime += Time.unscaledDeltaTime;
        if (holdTime < requiredHoldTime)
            return;

        isPointerDown = false;
        holdTime = 0f;

        if (!hasItem || currentItemData == null)
            return;

        if (DragSlot.Instance == null)
            return;

        ItemStack stack = new ItemStack(currentItemData, 1);
        DragSlot.Instance.StartDrag(this, stack);
        ClearSlot();
    }

    public void Init(EquipSlotType type)
    {
        slotType = type;
        RefreshUI();
    }

    public bool CanAccept(ItemData data)
    {
        if (data == null)
            return false;

        return data.CanEquipTo(slotType);
    }

    public void SetItem(ItemData data)
    {
        HideTooltip();

        if (data == null)
        {
            ClearSlot();
            return;
        }

        hasItem = true;
        currentItemData = data;
        RefreshUI();
    }

    public void ClearSlot()
    {
        HideTooltip();

        hasItem = false;
        currentItemData = null;
        ApplyEmptyVisual();
    }

    public void RefreshUI()
    {
        if (iconImage == null)
            return;

        if (hasItem && currentItemData != null)
        {
            iconImage.sprite = currentItemData.icon;
            iconImage.enabled = currentItemData.icon != null;
        }
        else
        {
            ApplyEmptyVisual();
        }
    }

    private void ApplyEmptyVisual()
    {
        if (iconImage == null)
            return;

        iconImage.sprite = emptySlotSprite;
        iconImage.enabled = emptySlotSprite != null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HideTooltip();

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Inven_System.instance != null && Inven_System.instance.IsInteractionLocked)
            return;

        if (!hasItem || currentItemData == null)
            return;

        isPointerDown = true;
        holdTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        holdTime = 0f;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        Inven_System invenSystem = Inven_System.instance;
        if (invenSystem == null)
            return;

        if (DragSlot.Instance == null || DragSlot.Instance.dragData == null)
            return;

        if (invenSystem.IsPointerOverEquipmentSlot(out EquipmentSlotUI targetEquipmentSlot) && targetEquipmentSlot != null)
        {
            invenSystem.OnEquipmentSlotDrop(targetEquipmentSlot);
            return;
        }

        if (invenSystem.IsPointerOverSlot(out Inven_Slot targetInvenSlot) && targetInvenSlot != null)
        {
            invenSystem.OnSlotDrop(targetInvenSlot.isMainSlot, targetInvenSlot.slotIndex);
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

        if (Inven_System.instance != null && Inven_System.instance.IsInteractionLocked)
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

