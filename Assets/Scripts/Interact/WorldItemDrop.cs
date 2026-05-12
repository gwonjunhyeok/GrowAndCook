using UnityEngine;

// 역할: 월드에 떨어진 아이템의 표시, 추격, 획득 상태를 관리한다.
public sealed class WorldItemDrop : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Spawn Animation")]
    [SerializeField] private float spawnDuration = 0.25f;
    [SerializeField] private float startScaleMultiplier = 0.25f;
    [SerializeField] private float endScaleMultiplier = 1f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 2.2f;

    private ItemData itemData;
    private int itemCount;
    private float spawnElapsedTime;
    private bool isSpawnAnimating;
    private bool isFollowing;
    private bool isCollected;
    private Transform followTarget;

    public ItemData ItemData => itemData;
    public int ItemCount => itemCount;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoBind();

        if (spawnDuration < 0.01f)
            spawnDuration = 0.01f;

        if (startScaleMultiplier < 0f)
            startScaleMultiplier = 0f;

        if (followSpeed < 0f)
            followSpeed = 0f;
    }
#endif

    private void Awake()
    {
        if (iconRenderer == null)
            AutoBind();
    }

    private void OnDisable()
    {
        isSpawnAnimating = false;
        isFollowing = false;
        isCollected = false;
        followTarget = null;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        UpdateSpawnAnimation(deltaTime);
        UpdateFollow(deltaTime);
    }

    // 역할: 큰 감지 범위가 플레이어를 찾았을 때 추격 상태를 시작한다.
    public void BeginFollow(Transform target)
    {
        if (isCollected || target == null)
            return;

        followTarget = target;
        isFollowing = true;
    }

    // 역할: 작은 획득 범위에 플레이어가 닿았을 때 아이템을 지급한다.
    public void Collect()
    {
        if (isCollected || itemData == null)
            return;

        Inven_System inventorySystem = Inven_System.instance;
        if (inventorySystem == null)
        {
            Debug.LogWarning($"{name}: 아이템 지급 실패 - Inven_System.instance가 없습니다.");
            return;
        }

        ItemData inventoryItem = inventorySystem.FindItemById(itemData.id);
        if (inventoryItem == null)
        {
            Debug.LogWarning($"{name}: 아이템 지급 실패 - id={itemData.id}에 해당하는 ItemData를 찾지 못했습니다.");
            return;
        }

        isCollected = true;
        inventorySystem.AddItem(inventoryItem);
        Debug.Log($"{name}: 아이템 획득 완료 - id={inventoryItem.id}, name={inventoryItem.GetDisplayName()}");
        gameObject.SetActive(false);
    }

    // 역할: 풀에서 꺼낸 드랍 오브젝트를 현재 아이템 상태로 초기화한다.
    public void Initialize(ItemData newItemData, int newItemCount)
    {
        itemData = newItemData;
        itemCount = Mathf.Max(1, newItemCount);
        isCollected = false;
        isFollowing = false;
        followTarget = null;

        if (iconRenderer != null)
        {
            iconRenderer.sprite = itemData != null ? itemData.icon : null;
            iconRenderer.enabled = iconRenderer.sprite != null;
        }

        spawnElapsedTime = 0f;
        isSpawnAnimating = true;
        transform.localScale = Vector3.one * startScaleMultiplier;
    }

    private void UpdateSpawnAnimation(float deltaTime)
    {
        if (!isSpawnAnimating)
            return;

        spawnElapsedTime += deltaTime;
        float normalizedTime = Mathf.Clamp01(spawnElapsedTime / spawnDuration);
        float scale = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, normalizedTime);
        transform.localScale = Vector3.one * scale;

        if (normalizedTime >= 1f)
            isSpawnAnimating = false;
    }

    private void UpdateFollow(float deltaTime)
    {
        if (!isFollowing || followTarget == null || isCollected)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = followTarget.position;
        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, followSpeed * deltaTime);
    }

    private void AutoBind()
    {
        if (iconRenderer == null)
            iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }
}
