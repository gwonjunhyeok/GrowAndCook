using UnityEngine;
using UnityEngine.UI;

// 역할: 현재 드래그 중인 아이템 정보를 보관하고, 마우스를 따라가는 아이콘을 표시한다.
public class DragSlot : Singleton<DragSlot>
{
    [SerializeField] private Image mItemImage;
    [HideInInspector] public DragData dragData;

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
        if (mItemImage == null)
            mItemImage = GetComponentInChildren<Image>(true);
    }

    protected override void Awake()
    {
        base.Awake();

        if (mItemImage == null)
            AutoBind();

        if (mItemImage != null)
        {
            mItemImage.enabled = false;
            mItemImage.sprite = null;

            Color c = mItemImage.color;
            c.a = 0f;
            mItemImage.color = c;
        }
        else
        {
            Debug.LogWarning("[DragSlot] 자식 Image를 찾지 못했습니다. DragSlot 오브젝트 아래에 Image가 있어야 합니다.");
        }
    }

    private void Update()
    {
        if (dragData == null || dragData.draggedItem == null)
            return;

        if (mItemImage == null)
            return;

        mItemImage.transform.position = Input.mousePosition;
    }

    public void StartDrag(bool fromMain, int index, ItemStack stack, bool isSplit)
    {
        if (stack == null || stack.itemData == null)
        {
            Debug.LogWarning("[DragSlot] StartDrag 실패: stack 또는 itemData가 null");
            ClearDrag();
            return;
        }

        dragData = new DragData
        {
            fromMain = fromMain,
            originIndex = index,
            draggedItem = stack,
            isSplit = isSplit,
            fromEquipment = false,
            originEquipmentSlot = null
        };

        ApplyDragVisual(stack);
    }

    public void StartDrag(EquipmentSlotUI equipmentSlot, ItemStack stack)
    {
        if (equipmentSlot == null || stack == null || stack.itemData == null)
        {
            Debug.LogWarning("[DragSlot] StartDrag 실패: equipmentSlot 또는 stack/itemData가 null");
            ClearDrag();
            return;
        }

        dragData = new DragData
        {
            fromMain = false,
            originIndex = -1,
            draggedItem = stack,
            isSplit = false,
            fromEquipment = true,
            originEquipmentSlot = equipmentSlot
        };

        ApplyDragVisual(stack);
    }

    public void ClearDrag()
    {
        dragData = null;

        if (mItemImage == null)
            return;

        mItemImage.sprite = null;
        mItemImage.enabled = false;
        SetAlpha(0f);
    }

    private void ApplyDragVisual(ItemStack stack)
    {
        if (mItemImage == null)
        {
            Debug.LogWarning("[DragSlot] mItemImage가 할당되지 않았습니다. (자식 Image 누락)");
            return;
        }

        if (stack.itemData.icon != null)
        {
            mItemImage.sprite = stack.itemData.icon;
            mItemImage.enabled = true;
            SetAlpha(1f);
            mItemImage.transform.position = Input.mousePosition;
        }
        else
        {
            Debug.LogWarning($"[DragSlot] 드래그할 아이템에 아이콘이 없음: {stack.itemData.itemName}");
            mItemImage.sprite = null;
            mItemImage.enabled = false;
            SetAlpha(0f);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (mItemImage == null)
            return;

        Color c = mItemImage.color;
        c.a = alpha;
        mItemImage.color = c;
    }
}