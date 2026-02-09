using UnityEngine;
using UnityEngine.UI;

public class DragSlot : Singleton<DragSlot>
{
    [SerializeField] private Image mItemImage;
    [HideInInspector] public DragData dragData;

    private void Update()
    {
        if (dragData != null && dragData.draggedItem != null)
        {
            mItemImage.transform.position = Input.mousePosition;
        }
    }

    public void StartDrag(bool fromMain, int index, ItemStack stack, bool isSplit)
    {
        dragData = new DragData
        {
            fromMain = fromMain,
            originIndex = index,
            draggedItem = stack,
            draggedCount = stack.count,
            isSplit = isSplit
        };

        // 여기서 아이콘 직접 설정
        if (stack.itemData != null && stack.itemData.icon != null)
        {
            mItemImage.sprite = stack.itemData.icon;
            mItemImage.enabled = true;         //  꼭 활성화
            SetColor(1f);
        }
        else
        {
            Debug.LogWarning($"[DragSlot] 드래그할 아이템에 아이콘이 없음: {stack.itemData?.itemName}");
            mItemImage.enabled = false;
        }
    }

    public void ClearDrag()
    {
        dragData = null;
        mItemImage.sprite = null;
        SetColor(0f);
    }

    public void SetColor(float alpha)
    {
        Color c = mItemImage.color;
        c.a = alpha;
        mItemImage.color = c;
    }
}
