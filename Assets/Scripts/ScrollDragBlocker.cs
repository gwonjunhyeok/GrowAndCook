using UnityEngine;
using UnityEngine.EventSystems;

// 역할: ScrollRect가 드래그 핸들러로 선택되는 것을 막기 위해, 슬롯에서 드래그 이벤트를 소비한다.
public sealed class ScrollDragBlocker : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.Use();
    public void OnBeginDrag(PointerEventData eventData) => eventData.Use();
    public void OnDrag(PointerEventData eventData) => eventData.Use();
    public void OnEndDrag(PointerEventData eventData) => eventData.Use();
}