using System;
using System.IO;
using UnityEngine;

// 역할: 슬롯 폴더와 Player/Data/World JSON 파일의 생성, 로드, 저장을 처리한다.
public static class SaveFileService
{
    private const int MinSlotIndex = 1;
    private const int MaxSlotIndex = 3;

    private const string SaveRootFolderName = "SaveSlots";
    private const string PlayerFileName = "Player.json";
    private const string DataFileName = "Data.json";
    private const string WorldFileName = "World.json";

    public static string GetSlotDirectoryPath(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        return Path.Combine(Application.persistentDataPath, SaveRootFolderName, $"Slot_{validSlotIndex}");
    }

    public static string GetPlayerPath(int slotIndex)
    {
        return Path.Combine(GetSlotDirectoryPath(slotIndex), PlayerFileName);
    }

    public static string GetDataPath(int slotIndex)
    {
        return Path.Combine(GetSlotDirectoryPath(slotIndex), DataFileName);
    }

    public static string GetWorldPath(int slotIndex)
    {
        return Path.Combine(GetSlotDirectoryPath(slotIndex), WorldFileName);
    }

    public static string GetLegacyPath(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        return Path.Combine(Application.persistentDataPath, $"SaveSlot_{validSlotIndex}.json");
    }

    public static bool Exists(int slotIndex)
    {
        return HasSplitFiles(slotIndex) || File.Exists(GetLegacyPath(slotIndex));
    }

    public static void DeleteSlot(int slotIndex)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);

        string slotDirectoryPath = GetSlotDirectoryPath(validSlotIndex);
        if (Directory.Exists(slotDirectoryPath))
            Directory.Delete(slotDirectoryPath, true);

        string legacyPath = GetLegacyPath(validSlotIndex);
        if (File.Exists(legacyPath))
            File.Delete(legacyPath);
    }

    public static SaveGameData CreateNew(int slotIndex, string sceneName, Vector2 startPosition)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);

        SaveGameData saveData = new SaveGameData();
        EnsureValidData(saveData, validSlotIndex);

        saveData.player.sceneName = sceneName;
        saveData.player.positionX = startPosition.x;
        saveData.player.positionY = startPosition.y;

        saveData.data.meta.slotIndex = validSlotIndex;
        saveData.data.meta.sceneName = sceneName;
        Save(validSlotIndex, saveData);
        return saveData;
    }

    public static bool TryLoad(int slotIndex, out SaveGameData saveData)
    {
        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        saveData = null;

        if (TryLoadSplitFiles(validSlotIndex, out saveData))
        {
            EnsureValidData(saveData, validSlotIndex);
            return true;
        }

        if (!TryLoadLegacy(validSlotIndex, out LegacySaveGameData legacyData))
            return false;

        saveData = ConvertLegacy(validSlotIndex, legacyData);
        Save(validSlotIndex, saveData);
        return true;
    }

    public static void Save(int slotIndex, SaveGameData saveData)
    {
        if (saveData == null)
            return;

        int validSlotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);
        EnsureValidData(saveData, validSlotIndex);
        UpdateMeta(saveData, validSlotIndex);

        string slotDirectoryPath = GetSlotDirectoryPath(validSlotIndex);
        Directory.CreateDirectory(slotDirectoryPath);

        File.WriteAllText(GetPlayerPath(validSlotIndex), JsonUtility.ToJson(saveData.player, true));
        File.WriteAllText(GetDataPath(validSlotIndex), JsonUtility.ToJson(saveData.data, true));
        File.WriteAllText(GetWorldPath(validSlotIndex), JsonUtility.ToJson(saveData.world, true));
    }

    private static bool HasSplitFiles(int slotIndex)
    {
        return File.Exists(GetPlayerPath(slotIndex))
            && File.Exists(GetDataPath(slotIndex));
    }

    private static bool TryLoadSplitFiles(int slotIndex, out SaveGameData saveData)
    {
        saveData = null;

        string playerPath = GetPlayerPath(slotIndex);
        string dataPath = GetDataPath(slotIndex);
        string worldPath = GetWorldPath(slotIndex);

        if (!File.Exists(playerPath) || !File.Exists(dataPath))
            return false;

        PlayerSaveData player = TryReadJson<PlayerSaveData>(playerPath);
        GameDataSaveData data = TryReadJson<GameDataSaveData>(dataPath);
        WorldSaveData world = File.Exists(worldPath)
            ? TryReadJson<WorldSaveData>(worldPath)
            : WorldSaveData.CreateDefault();

        if (player == null || data == null)
            return false;

        saveData = new SaveGameData
        {
            version = 2,
            player = player,
            data = data,
            world = world ?? WorldSaveData.CreateDefault()
        };

        return true;
    }

    private static bool TryLoadLegacy(int slotIndex, out LegacySaveGameData saveData)
    {
        saveData = null;

        string legacyPath = GetLegacyPath(slotIndex);
        if (!File.Exists(legacyPath))
            return false;

        saveData = TryReadJson<LegacySaveGameData>(legacyPath);
        return saveData != null;
    }

    private static T TryReadJson<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<T>(json);
    }

    private static SaveGameData ConvertLegacy(int slotIndex, LegacySaveGameData legacyData)
    {
        SaveGameData converted = new SaveGameData
        {
            version = 2,
            player = legacyData != null && legacyData.player != null
                ? legacyData.player
                : PlayerSaveData.CreateDefault(),
            data = GameDataSaveData.CreateDefault(),
            world = WorldSaveData.CreateDefault()
        };

        if (legacyData != null)
        {
            converted.data.meta = legacyData.meta ?? new SaveSlotMeta();
            converted.data.inventory = legacyData.inventory ?? new InventorySaveData();
            converted.data.wear = legacyData.wear ?? CharacterWearSaveData.CreateDefault();
            converted.data.gold = Mathf.Max(converted.data.meta.gold, 0);
        }

        converted.data.meta.slotIndex = slotIndex;
        converted.data.meta.level = converted.player.level;
        converted.data.meta.sceneName = converted.player.sceneName;
        EnsureValidData(converted, slotIndex);
        return converted;
    }

    private static void EnsureValidData(SaveGameData saveData, int slotIndex)
    {
        if (saveData.player == null)
            saveData.player = PlayerSaveData.CreateDefault();

        if (saveData.data == null)
            saveData.data = GameDataSaveData.CreateDefault();

        if (saveData.world == null)
            saveData.world = WorldSaveData.CreateDefault();

        if (saveData.world.regions == null)
            saveData.world.regions = new();

        if (saveData.data.meta == null)
            saveData.data.meta = new SaveSlotMeta();

        if (saveData.data.inventory == null)
            saveData.data.inventory = new InventorySaveData();

        if (saveData.data.inventory.mainSlots == null)
            saveData.data.inventory.mainSlots = new();

        if (saveData.data.inventory.subSlots == null)
            saveData.data.inventory.subSlots = new();

        if (saveData.data.inventory.equipmentSlots == null)
            saveData.data.inventory.equipmentSlots = new();

        if (saveData.data.wear == null)
            saveData.data.wear = CharacterWearSaveData.CreateDefault();

        saveData.player.hp = Mathf.Max(0, saveData.player.hp);
        saveData.player.stamina = Mathf.Max(0, saveData.player.stamina);
        saveData.player.level = Mathf.Max(1, saveData.player.level);
        saveData.player.experience = Mathf.Max(0, saveData.player.experience);

        saveData.data.gold = Mathf.Max(0, saveData.data.gold);
        saveData.data.playTimeSeconds = Mathf.Max(0f, saveData.data.playTimeSeconds);
        if (string.IsNullOrWhiteSpace(saveData.data.seasonId))
            saveData.data.seasonId = "Spring";

        saveData.data.meta.slotIndex = Mathf.Clamp(slotIndex, MinSlotIndex, MaxSlotIndex);

        for (int i = 0; i < saveData.world.regions.Count; i++)
        {
            WorldRegionSaveData region = saveData.world.regions[i];
            if (region == null)
            {
                saveData.world.regions[i] = new WorldRegionSaveData();
                region = saveData.world.regions[i];
            }

            region.EnsureLists();
        }
    }

    private static void UpdateMeta(SaveGameData saveData, int slotIndex)
    {
        saveData.data.meta.slotIndex = slotIndex;
        saveData.data.meta.level = saveData.player.level;
        saveData.data.meta.gold = saveData.data.gold;
        saveData.data.meta.sceneName = saveData.player.sceneName;
        saveData.data.meta.lastSavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
