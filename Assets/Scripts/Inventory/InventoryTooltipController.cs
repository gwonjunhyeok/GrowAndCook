using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 역할: 인벤토리 부모에서 툴팁 패널 표시/숨김과 아이템 정보 갱신, 위치 추적을 관리한다.
public sealed class InventoryTooltipController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Timing")]
    [SerializeField] private float hoverDelaySeconds = 0.5f;

    [Header("Position")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(24f, 24f);
    [SerializeField] private Vector2 screenPadding = new Vector2(12f, 12f);

    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private CanvasGroup tooltipCanvasGroup;
    private Object currentSource;
    private Object pendingSource;
    private ItemData pendingItemData;
    private float hoverStartTime;
    private bool isPending;
    private bool isVisible;

    private void Awake()
    {
        CacheCanvasReferences();
        ConfigureTooltipRaycast();
        HideImmediate();
    }

    private void OnDisable()
    {
        HideImmediate();
    }

    private void Update()
    {
        if (isPending)
        {
            if (Time.unscaledTime - hoverStartTime >= hoverDelaySeconds)
                ShowPendingTooltip();
        }

        if (!isVisible)
            return;

        UpdateTooltipPosition();
    }

    public void RequestShowTooltip(Object source, ItemData itemData)
    {
        if (source == null || itemData == null)
            return;

        CacheCanvasReferences();

        if (currentSource != source || !isVisible)
            HideImmediate();

        pendingSource = source;
        pendingItemData = itemData;
        hoverStartTime = Time.unscaledTime;
        isPending = true;

        if (hoverDelaySeconds <= 0f)
            ShowPendingTooltip();
    }

    public void HideTooltip(Object source)
    {
        if (source == null)
            return;

        if (currentSource != source && pendingSource != source)
            return;

        HideImmediate();
    }

    public void HideImmediate()
    {
        currentSource = null;
        pendingSource = null;
        pendingItemData = null;
        isPending = false;
        SetTooltipVisible(false);
    }

    private void ApplyItemData(ItemData itemData)
    {
        if (itemIconImage != null)
        {
            itemIconImage.sprite = itemData.icon;
            itemIconImage.enabled = itemData.icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = itemData.GetDisplayName();

        if (itemTypeText != null)
            itemTypeText.text = itemData.itemType.ToString();

        if (itemDescriptionText != null)
            itemDescriptionText.text = itemData.GetDescription();
    }

    private void SetTooltipVisible(bool visible)
    {
        isVisible = visible;

        if (tooltipPanel == null)
            return;

        if (tooltipPanel.activeSelf != visible)
            tooltipPanel.SetActive(visible);
    }

    private void ShowPendingTooltip()
    {
        if (pendingSource == null || pendingItemData == null)
        {
            HideImmediate();
            return;
        }

        currentSource = pendingSource;
        ApplyItemData(pendingItemData);
        SetTooltipVisible(true);
        Canvas.ForceUpdateCanvases();
        UpdateTooltipPosition();

        pendingSource = null;
        pendingItemData = null;
        isPending = false;
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || canvasRect == null || rootCanvas == null)
            return;

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector2 panelSize = tooltipRect.rect.size;
        Vector2 pivot = tooltipRect.pivot;

        Vector2 topLeftScreenPosition = (Vector2)Input.mousePosition + mouseOffset;
        float minTopLeftX = screenPadding.x;
        float maxTopLeftX = Screen.width - panelSize.x - screenPadding.x;
        float minTopLeftY = panelSize.y + screenPadding.y;
        float maxTopLeftY = Screen.height - screenPadding.y;

        topLeftScreenPosition.x = Mathf.Clamp(topLeftScreenPosition.x, minTopLeftX, maxTopLeftX);
        topLeftScreenPosition.y = Mathf.Clamp(topLeftScreenPosition.y, minTopLeftY, maxTopLeftY);

        Vector2 pivotScreenPosition = topLeftScreenPosition + new Vector2(
            panelSize.x * pivot.x,
            -panelSize.y * (1f - pivot.y));

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, pivotScreenPosition, uiCamera, out Vector3 worldPoint))
            return;

        tooltipRect.position = worldPoint;
    }

    private void CacheCanvasReferences()
    {
        if (tooltipPanel == null)
            return;

        if (tooltipRect == null)
            tooltipRect = tooltipPanel.transform as RectTransform;

        if (tooltipRect == null)
            return;

        if (rootCanvas == null)
            rootCanvas = tooltipRect.GetComponentInParent<Canvas>(true);

        if (canvasRect == null && rootCanvas != null)
            canvasRect = rootCanvas.transform as RectTransform;
    }

    private void ConfigureTooltipRaycast()
    {
        if (tooltipPanel == null)
            return;

        tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipCanvasGroup.interactable = false;
    }
}
