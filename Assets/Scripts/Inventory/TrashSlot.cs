using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlot : MonoBehaviour, IDropHandler
{
    [Header("Optional Confirm UI")]
    [SerializeField] private GameObject confirmPanel; // 확인창 루트(없으면 즉시 버리기)
    [SerializeField] private TextMeshProUGUI confirmText; // "OOO xN 버릴까요?" 같은 문구 표시용(선택)

    // 드롭 순간의 드래그 정보를 캡쳐(Confirm 중에 dragData가 바뀌는 문제 방지)
    private DragData pendingDrag;
    private string pendingName;
    private int pendingCount;

    public void OnDrop(PointerEventData eventData)
    {
        if (Inven_System.instance == null) return;
        if (DragSlot.Instance == null) return;
        if (DragSlot.Instance.dragData == null) return;
        if (DragSlot.Instance.dragData.draggedItem == null) return;

        // confirm UI가 없으면 즉시 버리기
        if (confirmPanel == null)
        {
            Inven_System.instance.DiscardItem();
            return;
        }

        // 드롭 순간의 데이터를 캡쳐
        CachePending(DragSlot.Instance.dragData);

        // 확인창 표시
        confirmPanel.SetActive(true);

        // 문구 업데이트(선택)
        if (confirmText != null)
        {
            confirmText.text = $"{pendingName} x{pendingCount}\n버리시겠습니까?";
        }
    }

    private void CachePending(DragData drag)
    {
        // dragData 자체는 참조 타입이라, 필드를 복사해서 스냅샷처럼 보관
        pendingDrag = new DragData
        {
            fromMain = drag.fromMain,
            originIndex = drag.originIndex,
            draggedItem = drag.draggedItem,
            isSplit = drag.isSplit
        };

        pendingName = (pendingDrag.draggedItem != null && pendingDrag.draggedItem.itemData != null)
            ? pendingDrag.draggedItem.itemData.itemName
            : "아이템";

        pendingCount = pendingDrag.draggedItem != null ? pendingDrag.draggedItem.count : 0;
    }

    // 확인창 "예" 버튼 OnClick에 연결
    public void ConfirmYes()
    {
        if (Inven_System.instance == null) { CloseConfirm(); return; }
        if (pendingDrag == null) { CloseConfirm(); return; }

        // 안전: Confirm 도중 dragData가 다른 것으로 바뀐 경우, 잘못 버리지 않도록 확인
        if (DragSlot.Instance == null || DragSlot.Instance.dragData == null || DragSlot.Instance.dragData.draggedItem == null)
        {
            // 드래그가 이미 사라졌으면 그냥 닫기
            CloseConfirm();
            return;
        }

        // 현재 드래그 아이템과 pending이 다른 경우: 버리지 말고 원복 시도 후 닫기
        if (!IsSameCurrentDrag(DragSlot.Instance.dragData, pendingDrag))
        {
            Inven_System.instance.ReturnDragItem();
            CloseConfirm();
            return;
        }

        Inven_System.instance.DiscardItem();
        CloseConfirm();
    }

    // 확인창 "아니오" 버튼 OnClick에 연결
    public void ConfirmNo()
    {
        if (Inven_System.instance == null) { CloseConfirm(); return; }
        if (pendingDrag == null) { CloseConfirm(); return; }

        // dragData가 남아있고, pending과 동일할 때만 ReturnDragItem
        if (DragSlot.Instance != null && DragSlot.Instance.dragData != null && DragSlot.Instance.dragData.draggedItem != null)
        {
            if (IsSameCurrentDrag(DragSlot.Instance.dragData, pendingDrag))
            {
                Inven_System.instance.ReturnDragItem();
            }
            else
            {
                // 다른 드래그로 바뀌었으면 안전하게 현재 드래그만 복귀
                Inven_System.instance.ReturnDragItem();
            }
        }

        CloseConfirm();
    }

    private bool IsSameCurrentDrag(DragData current, DragData pending)
    {
        if (current == null || pending == null) return false;
        if (current.originIndex != pending.originIndex) return false;
        if (current.fromMain != pending.fromMain) return false;
        if (current.isSplit != pending.isSplit) return false;

        // 아이템 비교(레퍼런스 기준 + itemName fallback)
        if (current.draggedItem == null || pending.draggedItem == null) return false;
        if (current.draggedItem.itemData == null || pending.draggedItem.itemData == null) return false;

        return current.draggedItem.itemData.itemName == pending.draggedItem.itemData.itemName
               && current.draggedItem.count == pending.draggedItem.count;
    }

    private void CloseConfirm()
    {
        pendingDrag = null;
        pendingName = null;
        pendingCount = 0;

        if (confirmPanel != null) confirmPanel.SetActive(false);
    }
}
