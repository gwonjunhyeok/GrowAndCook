using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    public bool FollowTrigger = true; // true: 플레이어 위치로 순간이동(고정), false: 고정 해제(현재 위치 유지)
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    private float CameraZoom = 5f;
    public float sensivity = 0.5f;
    public float Max_zoom = 5f;
    public float Min_zoom = 2f;
    private Vector3 velocity;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            CameraZoom = Mathf.Clamp(cam.orthographicSize, 3f, 5f);
            cam.orthographicSize = CameraZoom;
        }
    }

    private void LateUpdate()
    {
        ZoomInZoonOut();
        if (Input.GetKeyDown(KeyCode.Space) && FollowTrigger)
            FollowTrigger = false;
        else if (Input.GetKeyDown(KeyCode.Space) && !FollowTrigger)
            FollowTrigger = true;
        if (target == null) return;

        if (FollowTrigger)
        {
            // 플레이어 위치로 "순간이동"
            transform.position = target.position + offset;
        }
    }
    private void ZoomInZoonOut()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) <= 0.0001f) return;

        CameraZoom -= scroll * sensivity;
        CameraZoom = Mathf.Clamp(CameraZoom, Min_zoom, Max_zoom);

        if (cam != null)
            cam.orthographicSize = CameraZoom;
    }
}
