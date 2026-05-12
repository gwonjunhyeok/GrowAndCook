using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// 역할: 스타트씬의 New Game / Load Game / Exit 흐름을 제어한다.
public sealed class StartSceneMenuController : MonoBehaviour
{
    private const int MinSlotIndex = 1;
    private const int MaxSlotIndex = 3;

    [SerializeField] private GameObject loadUiCanvas;
    [SerializeField] private LoadGameUIController loadGameUIController;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string mainGameSceneName = "MainGameScene";
    [SerializeField] private Vector2 newGameStartPosition = Vector2.zero;

    private void Awake()
    {
        BindButton(newGameButton, OnClickNewGame);
        BindButton(loadGameButton, OnClickLoadGame);
        BindButton(exitButton, OnClickExit);
    }

    private void Start()
    {
        GameSession.Clear();

        if (loadUiCanvas != null)
            loadUiCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        UnbindButton(newGameButton, OnClickNewGame);
        UnbindButton(loadGameButton, OnClickLoadGame);
        UnbindButton(exitButton, OnClickExit);
    }

    public void OnClickNewGame()
    {
        int emptySlotIndex = FindFirstEmptySlotIndex();
        if (emptySlotIndex > 0)
        {
            StartNewGameInSlot(emptySlotIndex);
            return;
        }

        OpenLoadUI();
    }

    public void OnClickLoadGame()
    {
        OpenLoadUI();
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        GameSession.StartNewGame(validSlotIndex, mainGameSceneName, newGameStartPosition);
        SceneManager.LoadScene(mainGameSceneName);
    }

    public void LoadGameFromSlot(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        if (!GameSession.LoadGame(validSlotIndex))
            return;

        SaveGameData saveData = GameSession.CurrentSaveData;
        string sceneName = saveData != null && saveData.player != null && !string.IsNullOrWhiteSpace(saveData.player.sceneName)
            ? saveData.player.sceneName
            : mainGameSceneName;

        SceneManager.LoadScene(sceneName);
    }

    public void DeleteSlot(int slotIndex)
    {
        SaveFileService.DeleteSlot(slotIndex);

        if (loadGameUIController != null)
            loadGameUIController.RefreshSlots();
    }

    public void OpenLoadUI()
    {
        if (loadUiCanvas == null)
            return;

        loadUiCanvas.SetActive(true);

        if (loadGameUIController != null)
            loadGameUIController.RefreshSlots();
    }

    public void CloseLoadUI()
    {
        if (loadUiCanvas != null)
            loadUiCanvas.SetActive(false);
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

    private static int FindFirstEmptySlotIndex()
    {
        for (int slotIndex = MinSlotIndex; slotIndex <= MaxSlotIndex; slotIndex++)
        {
            if (!SaveFileService.Exists(slotIndex))
                return slotIndex;
        }

        return -1;
    }
}
