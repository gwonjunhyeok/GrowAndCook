using UnityEngine;

// 역할: 작은 획득 범위에서 플레이어를 감지해 드랍 지급을 요청한다.
public sealed class WorldItemDropPickupTrigger : MonoBehaviour
{
    [SerializeField] private WorldItemDrop owner;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null)
            return;

        PlayerMove player = other.GetComponentInParent<PlayerMove>();
        if (player == null)
            return;

        owner.Collect();
    }

    private void AutoBind()
    {
        if (owner == null)
            owner = GetComponentInParent<WorldItemDrop>();
    }
}
