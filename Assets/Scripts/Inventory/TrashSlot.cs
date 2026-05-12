using UnityEngine;
using UnityEngine.EventSystems;

// 역할: 드래그된 아이템을 원위치로 돌린 뒤 TrashSlot 위에 폐기 패널을 띄운다.
public class TrashSlot : MonoBehaviour, IDropHandler
{
    [Header("Popup")]
    [SerializeField] private DiscardConfirmPopup confirmPopup;

    private DiscardRequest pendingRequest;

    public void OnDrop(PointerEventData eventData)
    {
        HandleDropRequest();
    }

    public void HandleDropRequest()
    {
        if (Inven_System.instance == null)
            return;

        if (DragSlot.Instance == null || DragSlot.Instance.dragData == null || DragSlot.Instance.dragData.draggedItem == null)
            return;

        if (Inven_System.instance.IsInteractionLocked)
            return;

        if (!Inven_System.instance.TryCreateDiscardRequest(out DiscardRequest request))
            return;

        // TrashSlot은 슬롯이 아니므로 드롭 즉시 아이템은 원래 슬롯으로 복귀한다.
        Inven_System.instance.ReturnDragItem();

        if (confirmPopup == null)
        {
            Debug.LogWarning("TrashSlot confirmPopup이 비어 있습니다.");
            return;
        }

        pendingRequest = request;
        Inven_System.instance.SetInteractionLocked(true);
        confirmPopup.Show(
            pendingRequest,
            transform as RectTransform,
            OnConfirmDiscard,
            OnCancelDiscard);
    }

    private void OnConfirmDiscard(int discardCount)
    {
        if (Inven_System.instance == null || pendingRequest == null)
        {
            CloseConfirm();
            return;
        }

        if (!Inven_System.instance.TryDiscardItem(pendingRequest, discardCount))
            Debug.LogWarning("폐기 요청 처리에 실패했습니다.");

        CloseConfirm();
    }

    private void OnCancelDiscard()
    {
        CloseConfirm();
    }

    private void CloseConfirm()
    {
        pendingRequest = null;

        if (Inven_System.instance != null)
            Inven_System.instance.SetInteractionLocked(false);
    }
}
