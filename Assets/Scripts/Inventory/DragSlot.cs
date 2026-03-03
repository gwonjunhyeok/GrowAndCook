using UnityEngine;
using UnityEngine.UI;

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
        // DragSlot 오브젝트 아래 자식에 있는 Image를 자동으로 잡는다
        if (mItemImage == null)
            mItemImage = GetComponentInChildren<Image>(true);
    }

    protected override void Awake()
    {
        base.Awake();

        // 런타임에서도 혹시 누락됐으면 1회 보정
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
            Debug.LogWarning("[DragSlot] mItemImage가 할당되지 않았습니다. (자식 Image 누락)");
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