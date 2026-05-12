using UnityEngine;

public class PlayerInteractable : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;

    [Header("Targeting")]
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private float clickPickRadius = 0f;

    [Header("Global Cooltime")]
    [SerializeField] private float Interact_Cooltime = 0.5f;
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
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayPaused)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (Time.time < nextReadyTime)
            return;

        Vector2 clickWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        Collider2D col = PickCollider(clickWorld);
        if (col == null)
            return;

        IInteractable target = col.GetComponent<IInteractable>();
        if (target == null)
            target = col.GetComponentInParent<IInteractable>();

        if (target == null)
            return;

        float dist = Vector2.Distance(transform.position, col.transform.position);
        if (dist > target.InteractRange)
            return;

        nextReadyTime = Time.time + Interact_Cooltime;
        target.Interact(transform);
    }

    private Collider2D PickCollider(Vector2 clickWorld)
    {
        if (clickPickRadius <= 0f)
            return Physics2D.OverlapPoint(clickWorld, interactableMask);

        return Physics2D.OverlapCircle(clickWorld, clickPickRadius, interactableMask);
    }
}
