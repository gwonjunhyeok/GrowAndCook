using UnityEngine;

// 역할: 키 바인딩 기준으로 인벤토리와 설정 UI를 토글하고 게임 정지 상태를 갱신한다.
public sealed class UIControlManager : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;

    private KeyBindingSettings keyBindings;
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        keyBindings = gameManager != null ? gameManager.CurrentKeyBindings : null;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        RefreshPauseState();
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (keyBindings == null && gameManager != null)
            keyBindings = gameManager.CurrentKeyBindings;

        if (keyBindings == null)
            return;

        if (Input.GetKeyDown(keyBindings.settings))
        {
            if (settingsPanel == null)
                return;

            bool nextState = !settingsPanel.activeSelf;

            if (nextState && inventoryPanel != null)
                inventoryPanel.SetActive(false);

            settingsPanel.SetActive(nextState);
            RefreshPauseState();
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
            return;

        if (Input.GetKeyDown(keyBindings.inventory))
        {
            if (inventoryPanel == null)
                return;

            bool nextState = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(nextState);

            // 인벤토리를 다시 열 때 숨겨진 동안 바뀐 슬롯 상태를 한 번 더 반영한다.
            if (nextState && Inven_System.instance != null)
                Inven_System.instance.FreshSlot();

            RefreshPauseState();
        }
    }

    private void RefreshPauseState()
    {
        if (gameManager == null)
            return;

        bool isInventoryOpen = inventoryPanel != null && inventoryPanel.activeSelf;
        bool isSettingsOpen = settingsPanel != null && settingsPanel.activeSelf;
        bool isAnyUiOpen = isInventoryOpen || isSettingsOpen;

        gameManager.SetGameplayPaused(isAnyUiOpen);
    }
}
