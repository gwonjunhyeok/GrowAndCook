using UnityEngine;
using UnityEngine.UI;

public class DragSlot : Singleton<DragSlot>
{
    [SerializeField] private Image mItemImage;
    [HideInInspector] public DragData dragData;

    protected override void Awake()
    {
        base.Awake(); // Singleton의 Awake 실행(중복 인스턴스 제거 등)

        // 네가 DragSlot에서 하던 초기화 코드
        if (mItemImage != null)
        {
            mItemImage.enabled = false;
            mItemImage.sprite = null;

            Color c = mItemImage.color;
            c.a = 0f;
            mItemImage.color = c;
        }
    }

    private void Update()
    {
        if (dragData == null || dragData.draggedItem == null) return;
        if (mItemImage == null) return;

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
            isSplit = isSplit
        };

        if (mItemImage == null)
        {
            Debug.LogWarning("[DragSlot] mItemImage가 할당되지 않았습니다.");
            return;
        }

        // 아이콘 설정
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

    public void ClearDrag()
    {
        dragData = null;

        if (mItemImage == null) return;

        mItemImage.sprite = null;
        mItemImage.enabled = false;
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (mItemImage == null) return;

        Color c = mItemImage.color;
        c.a = alpha;
        mItemImage.color = c;
    }
}
