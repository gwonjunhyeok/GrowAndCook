using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// 역할: TrashSlot 폐기 팝업의 수량 선택과 동적 텍스트 갱신을 담당한다.
public sealed class DiscardConfirmPopup : MonoBehaviour
{
    private const string LanguageTableName = "Language Table";
    private const string DiscardCountKey = "Discard_Count_Key";
    private const string DiscardConfirmKey = "Discard_Confirm_Format_Key";
    private const string DefaultDiscardCountFormat = "Discard Count: ({0} / {1})";
    private const string DefaultDiscardConfirmFormat = "Discard x{0} {1}?";

    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI discardCountText;
    [SerializeField] private TextMeshProUGUI selectedCountText;
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button confirmButton;

    private DiscardRequest currentRequest;
    private Action<int> confirmAction;
    private Action cancelAction;
    private int selectedCount;

    private void Awake()
    {
        EnsureReferences();
        DisableDynamicLocalizeEvents();
    }

    public void Show(DiscardRequest request, RectTransform anchorRect, Action<int> onConfirm, Action onCancel)
    {
        if (request == null)
            return;

        EnsureReferences();

        currentRequest = request;
        confirmAction = onConfirm;
        cancelAction = onCancel;
        selectedCount = 0;

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        RefreshView();
    }

    public void IncreaseCount()
    {
        if (currentRequest == null)
            return;

        if (selectedCount >= currentRequest.maxCount)
            return;

        selectedCount++;
        RefreshView();
    }

    public void DecreaseCount()
    {
        if (currentRequest == null)
            return;

        if (selectedCount <= 0)
            return;

        selectedCount--;
        RefreshView();
    }

    public void ConfirmDiscard()
    {
        if (currentRequest == null || selectedCount <= 0)
            return;

        confirmAction?.Invoke(selectedCount);
        CloseInternal();
    }

    public void CancelDiscard()
    {
        if (currentRequest == null)
            return;

        cancelAction?.Invoke();
        CloseInternal();
    }

    private void RefreshView()
    {
        if (currentRequest == null)
            return;

        if (itemIconImage != null)
        {
            Sprite icon = currentRequest.itemData != null ? currentRequest.itemData.icon : null;
            itemIconImage.sprite = icon;
            itemIconImage.enabled = icon != null;
        }

        if (discardCountText != null)
            discardCountText.text = string.Format(GetLocalizedFormat(DiscardCountKey, DefaultDiscardCountFormat), selectedCount, currentRequest.maxCount);

        if (selectedCountText != null)
            selectedCountText.text = selectedCount.ToString();

        if (confirmMessageText != null)
        {
            string itemName = currentRequest.itemData != null
                ? currentRequest.itemData.itemName
                : "Item";

            confirmMessageText.text = string.Format(GetLocalizedFormat(DiscardConfirmKey, DefaultDiscardConfirmFormat), selectedCount, itemName);
        }

        if (decreaseButton != null)
            decreaseButton.interactable = selectedCount > 0;

        if (increaseButton != null)
            increaseButton.interactable = selectedCount < currentRequest.maxCount;

        if (confirmButton != null)
            confirmButton.interactable = selectedCount > 0;
    }

    private void CloseInternal()
    {
        currentRequest = null;
        confirmAction = null;
        cancelAction = null;
        selectedCount = 0;

        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (panelRect == null)
            panelRect = popupRoot.transform as RectTransform;
    }

    // 동적 TMP는 코드가 직접 갱신해야 템플릿 문자열이 그대로 노출되지 않는다.
    private void DisableDynamicLocalizeEvents()
    {
        DisableLocalizeEvent(discardCountText);
        DisableLocalizeEvent(confirmMessageText);
    }

    private static void DisableLocalizeEvent(Component target)
    {
        if (target == null)
            return;

        LocalizeStringEvent localizeEvent = target.GetComponent<LocalizeStringEvent>();
        if (localizeEvent != null)
            localizeEvent.enabled = false;
    }

    private static string GetLocalizedFormat(string key, string fallback)
    {
        if (LocalizationSettings.HasSettings && LocalizationSettings.StringDatabase != null)
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString(LanguageTableName, key);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        return fallback;
    }
}
