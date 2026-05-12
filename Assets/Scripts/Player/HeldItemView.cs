using UnityEngine;

// 역할: 현재 선택한 핫바 아이템 아이콘을 손에 표시하고, 부모 기준 좌우 위치와 크기를 갱신한다.
public class HeldItemView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private bool flipSpriteWhenFacingLeft = true;
    [SerializeField] private float targetIconPixelSize = 32f;

    private Inven_System inventorySystem;
    private PlayerMove playerMove;
    private Vector3 rightFacingLocalPosition;
    private Vector3 leftFacingLocalPosition;
    private Vector3 baseLocalScale;
    private ItemData lastItemData;
    private bool lastFacingLeft;
    private bool hasCachedTransformState;

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

    private void Awake()
    {
        AutoBind();
        inventorySystem = Inven_System.instance;
        CacheTransformState();
    }

    private void OnEnable()
    {
        RefreshView(true);
    }

    private void LateUpdate()
    {
        RefreshView(false);
    }

    private void AutoBind()
    {
        if (playerSpriteRenderer == null)
        {
            playerMove = GetComponentInParent<PlayerMove>();
            if (playerMove != null)
                playerSpriteRenderer = playerMove.GetComponent<SpriteRenderer>();
        }

        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    // 역할: 선택 아이템 또는 방향이 바뀐 경우에만 손 아이템 표시를 갱신한다.
    private void RefreshView(bool forceRefresh)
    {
        if (inventorySystem == null)
            inventorySystem = Inven_System.instance;

        ItemData currentItemData = null;
        if (inventorySystem != null)
            inventorySystem.TryGetSelectedHotbarItem(out currentItemData);

        bool isFacingLeft = playerSpriteRenderer != null && playerSpriteRenderer.flipX;
        if (!forceRefresh && ReferenceEquals(lastItemData, currentItemData) && lastFacingLeft == isFacingLeft)
            return;

        lastItemData = currentItemData;
        lastFacingLeft = isFacingLeft;

        RefreshHeldItemSprite(currentItemData);
        RefreshFacing(isFacingLeft);
    }

    // 역할: 현재 선택 슬롯 아이템의 아이콘을 손 스프라이트에 반영한다.
    private void RefreshHeldItemSprite(ItemData itemData)
    {
        if (targetRenderer == null)
            return;

        if (itemData == null || itemData.icon == null)
        {
            targetRenderer.sprite = null;
            targetRenderer.enabled = false;
            transform.localScale = baseLocalScale;
            return;
        }

        targetRenderer.sprite = itemData.icon;
        targetRenderer.enabled = true;
        ApplySpriteScale(itemData.icon);
    }

    // 역할: 플레이어 좌우 방향에 맞춰 부모 기준 손 위치와 반전을 갱신한다.
    private void RefreshFacing(bool isFacingLeft)
    {
        CacheTransformState();
        transform.localPosition = isFacingLeft ? leftFacingLocalPosition : rightFacingLocalPosition;

        if (targetRenderer != null && flipSpriteWhenFacingLeft)
            targetRenderer.flipX = isFacingLeft;
    }

    // 역할: 오른쪽 기준 손 위치와 기본 스케일을 캐싱해 좌우 반전을 안정적으로 유지한다.
    private void CacheTransformState()
    {
        if (hasCachedTransformState)
            return;

        rightFacingLocalPosition = transform.localPosition;
        leftFacingLocalPosition = rightFacingLocalPosition;
        leftFacingLocalPosition.x = -rightFacingLocalPosition.x;
        baseLocalScale = transform.localScale;
        hasCachedTransformState = true;
    }

    // 역할: 아이콘 해상도와 무관하게 손에 드는 스프라이트 크기를 일정하게 맞춘다.
    private void ApplySpriteScale(Sprite sprite)
    {
        if (sprite == null || targetIconPixelSize <= 0f)
        {
            transform.localScale = baseLocalScale;
            return;
        }

        float width = sprite.rect.width;
        float height = sprite.rect.height;
        if (width <= 0f || height <= 0f)
        {
            transform.localScale = baseLocalScale;
            return;
        }

        Vector3 nextScale = baseLocalScale;
        nextScale.x *= targetIconPixelSize / width;
        nextScale.y *= targetIconPixelSize / height;
        transform.localScale = nextScale;
    }
}
