using UnityEngine;

// 역할: 큰 감지 범위에서 플레이어를 감지해 드랍 추격을 시작한다.
public sealed class WorldItemDropMagnetTrigger : MonoBehaviour
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

        owner.BeginFollow(player.transform);
    }

    private void AutoBind()
    {
        if (owner == null)
            owner = GetComponentInParent<WorldItemDrop>();
    }
}
