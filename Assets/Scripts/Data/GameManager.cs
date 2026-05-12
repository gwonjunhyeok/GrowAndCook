using UnityEngine;

// 역할: 게임 전역 데이터와 현재 키 바인딩, 게임플레이 정지 상태를 보관한다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private KeyBindingSettings currentKeyBindings = new KeyBindingSettings();

    public KeyBindingSettings CurrentKeyBindings => currentKeyBindings;
    public bool IsGameplayPaused { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
        IsGameplayPaused = false;
    }

    public void SetGameplayPaused(bool isPaused)
    {
        IsGameplayPaused = isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
