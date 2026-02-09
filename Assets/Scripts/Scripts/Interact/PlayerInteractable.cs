using UnityEngine;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;

    [Header("Targeting")]
    [SerializeField] private LayerMask interactableMask; // Interactable 레이어만 체크
    [SerializeField] private float clickPickRadius = 0f; // 0이면 점(OverlapPoint), >0이면 원(OverlapCircle)

    [Header("Global Cooltime")]
    [SerializeField] private float Interact_Cooltime = 0.5f; // 전역 쿨타임(초)
    private float nextReadyTime = 0f;

    private void Reset()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        P_Interact();
    }

    void P_Interact()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // 전역 쿨타임 체크
        if (Time.time < nextReadyTime) return;

        Vector2 clickWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        Collider2D col = PickCollider(clickWorld);
        if (col == null) return;

        IInteractable target = col.GetComponent<IInteractable>();
        if (target == null)
            target = col.GetComponentInParent<IInteractable>();

        if (target == null) return;

        float dist = Vector2.Distance(transform.position, col.transform.position);
        if (dist > target.InteractRange) return;

        // 조건 통과 -> 전역 쿨타임 시작
        nextReadyTime = Time.time + Interact_Cooltime;

        // 조건 통과 -> 오브젝트에 상호작용 요청
        target.Interact(transform);
    }

    private Collider2D PickCollider(Vector2 clickWorld)
    {
        if (clickPickRadius <= 0f)
        {
            return Physics2D.OverlapPoint(clickWorld, interactableMask);
        }

        return Physics2D.OverlapCircle(clickWorld, clickPickRadius, interactableMask);
    }
}
