using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 역할: 로드 UI의 슬롯 하나를 저장 있음/없음 상태에 맞게 표시한다.
public sealed class LoadGameSlotView : MonoBehaviour
{
    private const string EmptyText = "???";

    [SerializeField] private int slotIndex = 1;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text levelText;

    private StartSceneMenuController menuController;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        BindButton(loadButton, HandleLoadClicked);
        BindButton(deleteButton, HandleDeleteClicked);
        BindButton(newGameButton, HandleNewGameClicked);
    }

    private void OnDestroy()
    {
        UnbindButton(loadButton, HandleLoadClicked);
        UnbindButton(deleteButton, HandleDeleteClicked);
        UnbindButton(newGameButton, HandleNewGameClicked);
    }

    public void BindSaved(SaveGameData saveData, StartSceneMenuController controller)
    {
        menuController = controller;

        SetText(levelText, saveData != null && saveData.player != null ? saveData.player.level.ToString() : EmptyText);
        SetText(goldText, saveData != null && saveData.data != null ? saveData.data.gold.ToString() : EmptyText);
        SetText(playTimeText, saveData != null && saveData.data != null
            ? FormatPlayTime(saveData.data.playTimeSeconds)
            : EmptyText);

        SetActive(loadButton, true);
        SetActive(deleteButton, true);
        SetActive(newGameButton, false);
    }

    public void BindEmpty(StartSceneMenuController controller)
    {
        menuController = controller;

        SetText(levelText, EmptyText);
        SetText(goldText, EmptyText);
        SetText(playTimeText, EmptyText);

        SetActive(loadButton, false);
        SetActive(deleteButton, false);
        SetActive(newGameButton, true);
    }

    private void HandleLoadClicked()
    {
        if (menuController != null)
            menuController.LoadGameFromSlot(slotIndex);
    }

    private void HandleDeleteClicked()
    {
        if (menuController != null)
            menuController.DeleteSlot(slotIndex);
    }

    private void HandleNewGameClicked()
    {
        if (menuController != null)
            menuController.StartNewGameInSlot(slotIndex);
    }

    private static void SetActive(Button button, bool isActive)
    {
        if (button != null)
            button.gameObject.SetActive(isActive);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string FormatPlayTime(float playTimeSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(playTimeSeconds));
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds / 60) % 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
            return $"{hours:00}:{minutes:00}:{seconds:00}";

        return $"{minutes:00}:{seconds:00}";
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }
}
