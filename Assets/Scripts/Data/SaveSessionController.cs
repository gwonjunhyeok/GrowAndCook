using UnityEngine;

// 역할: 플레이타임 누적과 수동 저장(P), 종료 저장을 한 곳에서 관리한다.
public sealed class SaveSessionController : MonoBehaviour
{
    [SerializeField] private PlayerDataController playerDataController;
    [SerializeField] private KeyCode manualSaveKey = KeyCode.P;

    private void Update()
    {
        if (!GameSession.HasActiveSession)
            return;

        UpdatePlayTime(Time.deltaTime);

        if (Input.GetKeyDown(manualSaveKey))
            SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveNow();
    }

    // 역할: 현재 누적 플레이타임을 Data 저장 데이터에 반영한다.
    private static void UpdatePlayTime(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        SaveGameData saveData = GameSession.CurrentSaveData;
        if (saveData == null)
            return;

        if (saveData.data == null)
            saveData.data = GameDataSaveData.CreateDefault();

        saveData.data.playTimeSeconds += deltaTime;
        GameSession.MarkDataDirty();
    }

    public void SaveNow()
    {
        if (!GameSession.HasActiveSession)
            return;

        if (playerDataController != null)
            playerDataController.CaptureCurrentStateToSession();

        GameSession.SaveCurrentIfDirty();
    }
}
