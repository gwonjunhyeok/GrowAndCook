using UnityEngine.EventSystems;
using UnityEngine.UI;

// 역할: 마우스 드래그로 스크롤되는 동작을 완전히 막고, 휠/스크롤바만 허용한다.
public sealed class WheelOnlyScrollRect : ScrollRect
{
    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // ScrollRect가 useDragThreshold를 건드리거나 드래그 준비 상태를 만들지 못하게 막는다.
        // 휠 스크롤/스크롤바는 이 메서드와 무관하게 정상 동작한다.
    }

    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}