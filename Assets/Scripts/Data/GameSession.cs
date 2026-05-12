using UnityEngine;

// 역할: 현재 플레이 중인 세이브 슬롯과 분리 저장된 런타임 데이터를 보관한다.
public static class GameSession
{
    public static int CurrentSlotIndex { get; private set; } = -1;
    public static SaveGameData CurrentSaveData { get; private set; }

    public static bool HasActiveSession => CurrentSlotIndex > 0 && CurrentSaveData != null;
    public static bool IsPlayerDirty { get; private set; }
    public static bool IsDataDirty { get; private set; }
    public static bool IsWorldDirty { get; private set; }
    public static bool IsAnyDirty => IsPlayerDirty || IsDataDirty || IsWorldDirty;

    public static SaveGameData StartNewGame(int slotIndex, string sceneName, Vector2 startPosition)
    {
        CurrentSlotIndex = Mathf.Clamp(slotIndex, 1, 3);
        CurrentSaveData = SaveFileService.CreateNew(CurrentSlotIndex, sceneName, startPosition);
        ClearDirtyFlags();
        return CurrentSaveData;
    }

    public static bool LoadGame(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, 1, 3);
        if (!SaveFileService.TryLoad(validSlotIndex, out SaveGameData saveData))
            return false;

        CurrentSlotIndex = validSlotIndex;
        CurrentSaveData = saveData;
        ClearDirtyFlags();
        return true;
    }

    public static void SaveCurrent()
    {
        if (!HasActiveSession)
            return;

        SaveFileService.Save(CurrentSlotIndex, CurrentSaveData);
        ClearDirtyFlags();
    }

    public static void SaveCurrentIfDirty()
    {
        if (!HasActiveSession || !IsAnyDirty)
            return;

        SaveCurrent();
    }

    public static void MarkPlayerDirty()
    {
        if (HasActiveSession)
            IsPlayerDirty = true;
    }

    public static void MarkDataDirty()
    {
        if (HasActiveSession)
            IsDataDirty = true;
    }

    public static void MarkWorldDirty()
    {
        if (HasActiveSession)
            IsWorldDirty = true;
    }

    public static void ClearDirtyFlags()
    {
        IsPlayerDirty = false;
        IsDataDirty = false;
        IsWorldDirty = false;
    }

    public static void Clear()
    {
        CurrentSlotIndex = -1;
        CurrentSaveData = null;
        ClearDirtyFlags();
    }
}
