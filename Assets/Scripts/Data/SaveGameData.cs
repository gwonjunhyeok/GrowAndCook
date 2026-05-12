using System;
using System.Collections.Generic;

// 역할: 현재 세이브 슬롯의 런타임 저장 데이터를 Player/Data/World 단위로 묶는다.
[Serializable]
public sealed class SaveGameData
{
    public int version = 2;
    public PlayerSaveData player = PlayerSaveData.CreateDefault();
    public GameDataSaveData data = GameDataSaveData.CreateDefault();
    public WorldSaveData world = WorldSaveData.CreateDefault();
}

// 역할: 로드 UI와 세이브 식별에 필요한 슬롯 요약 정보를 보관한다.
[Serializable]
public sealed class SaveSlotMeta
{
    public int slotIndex = 1;
    public string lastSavedAt = string.Empty;
    public int level = 1;
    public int gold = 0;
    public string sceneName = string.Empty;
}

// 역할: 플레이어 상태와 위치처럼 캐릭터 자체에 속한 데이터를 저장한다.
[Serializable]
public sealed class PlayerSaveData
{
    public int hp = 100;
    public int stamina = 100;
    public int level = 1;
    public int experience = 0;

    public string sceneName = string.Empty;
    public float positionX = 0f;
    public float positionY = 0f;

    public static PlayerSaveData CreateDefault()
    {
        return new PlayerSaveData
        {
            hp = 100,
            stamina = 100,
            level = 1,
            experience = 0,
            sceneName = string.Empty,
            positionX = 0f,
            positionY = 0f
        };
    }
}

// 역할: 인벤토리, 시즌, 플레이타임처럼 게임 진행 데이터 전반을 저장한다.
[Serializable]
public sealed class GameDataSaveData
{
    public SaveSlotMeta meta = new SaveSlotMeta();
    public InventorySaveData inventory = new InventorySaveData();
    public CharacterWearSaveData wear = CharacterWearSaveData.CreateDefault();

    public int gold = 0;
    public string seasonId = "Spring";
    public float playTimeSeconds = 0f;

    public static GameDataSaveData CreateDefault()
    {
        return new GameDataSaveData
        {
            meta = new SaveSlotMeta(),
            inventory = new InventorySaveData(),
            wear = CharacterWearSaveData.CreateDefault(),
            gold = 0,
            seasonId = "Spring",
            playTimeSeconds = 0f
        };
    }
}

// 역할: 월드 저장 파일 placeholder로 사용되며 실제 월드 상태 저장은 추후 확장한다.
[Serializable]
public sealed class WorldSaveData
{
    public string currentRegionId = string.Empty;
    public List<WorldRegionSaveData> regions = new();

    public static WorldSaveData CreateDefault()
    {
        return new WorldSaveData
        {
            currentRegionId = string.Empty,
            regions = new List<WorldRegionSaveData>()
        };
    }

    // 역할: 지역 ID에 해당하는 월드 데이터를 찾고, 없으면 새로 만든다.
    public WorldRegionSaveData GetOrCreateRegion(string regionId, out bool created)
    {
        created = false;

        if (string.IsNullOrWhiteSpace(regionId))
            return null;

        if (regions == null)
            regions = new List<WorldRegionSaveData>();

        for (int i = 0; i < regions.Count; i++)
        {
            WorldRegionSaveData region = regions[i];
            if (region == null || region.regionId != regionId)
                continue;

            region.EnsureLists();
            return region;
        }

        WorldRegionSaveData newRegion = new WorldRegionSaveData
        {
            regionId = regionId
        };
        newRegion.EnsureLists();
        regions.Add(newRegion);
        created = true;
        return newRegion;
    }
}

// 역할: 지역 하나의 월드 변화 데이터를 시스템별로 보관한다.
[Serializable]
public sealed class WorldRegionSaveData
{
    public string regionId = string.Empty;
    public List<string> depletedObjectIds = new();
    public List<FarmTileSaveData> farmTiles = new();
    public List<PlacedObjectSaveData> placedObjects = new();

    public void EnsureLists()
    {
        if (depletedObjectIds == null)
            depletedObjectIds = new List<string>();

        if (farmTiles == null)
            farmTiles = new List<FarmTileSaveData>();

        if (placedObjects == null)
            placedObjects = new List<PlacedObjectSaveData>();
    }

    // 역할: 해당 오브젝트가 이미 소진 상태로 기록됐는지 확인한다.
    public bool ContainsDepletedObject(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId) || depletedObjectIds == null)
            return false;

        for (int i = 0; i < depletedObjectIds.Count; i++)
        {
            if (depletedObjectIds[i] == objectId)
                return true;
        }

        return false;
    }

    // 역할: 부숴지거나 소진된 오브젝트 ID를 중복 없이 기록한다.
    public bool TryAddDepletedObject(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return false;

        EnsureLists();
        if (ContainsDepletedObject(objectId))
            return false;

        depletedObjectIds.Add(objectId);
        return true;
    }
}

// 역할: 농사 셀 저장 구조를 위한 기본 골격이다.
[Serializable]
public sealed class FarmTileSaveData
{
    public int cellX;
    public int cellY;
}

// 역할: 플레이어 설치물 저장 구조를 위한 기본 골격이다.
[Serializable]
public sealed class PlacedObjectSaveData
{
    public string objectDataId = string.Empty;
    public float positionX;
    public float positionY;
}

// 역할: 메인/서브 인벤토리와 장비 슬롯 저장 데이터를 묶는다.
[Serializable]
public sealed class InventorySaveData
{
    public List<SlotItemSaveData> mainSlots = new();
    public List<SlotItemSaveData> subSlots = new();
    public List<EquipmentItemSaveData> equipmentSlots = new();
}

// 역할: 인벤토리 슬롯 하나의 아이템/수량 상태를 저장한다.
[Serializable]
public sealed class SlotItemSaveData
{
    public int slotIndex;
    public int itemId;
    public int count;

    public SlotItemSaveData()
    {
    }

    public SlotItemSaveData(int slotIndex, int itemId, int count)
    {
        this.slotIndex = slotIndex;
        this.itemId = itemId;
        this.count = count;
    }
}

// 역할: 장비 슬롯 하나의 장착 상태를 저장한다.
[Serializable]
public sealed class EquipmentItemSaveData
{
    public EquipSlotType slotType = EquipSlotType.None;
    public int itemId = -1;

    public EquipmentItemSaveData()
    {
    }

    public EquipmentItemSaveData(EquipSlotType slotType, int itemId)
    {
        this.slotType = slotType;
        this.itemId = itemId;
    }
}

// 역할: 추후 확장될 외형 저장의 placeholder다.
[Serializable]
public sealed class CharacterWearSaveData
{
    public bool hasCustomization = false;

    public static CharacterWearSaveData CreateDefault()
    {
        return new CharacterWearSaveData
        {
            hasCustomization = false
        };
    }
}

// 역할: 기존 단일 JSON 세이브를 새 구조로 옮길 때만 사용하는 호환 데이터다.
[Serializable]
public sealed class LegacySaveGameData
{
    public int version = 1;
    public SaveSlotMeta meta = new SaveSlotMeta();
    public PlayerSaveData player = PlayerSaveData.CreateDefault();
    public InventorySaveData inventory = new InventorySaveData();
    public CharacterWearSaveData wear = CharacterWearSaveData.CreateDefault();
}
