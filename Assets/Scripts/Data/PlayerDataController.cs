using UnityEngine;
using UnityEngine.SceneManagement;

// 역할: 플레이어 상태를 Player/Data 저장 데이터와 런타임 상태 사이에서 동기화한다.
public sealed class PlayerDataController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int stamina = 100;
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int gold = 0;

    [Header("Refs")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Inven_System inventorySystem;

    public int Hp => hp;
    public int Stamina => stamina;
    public int Level => level;
    public int Experience => experience;
    public int Gold => gold;

    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;
    }

    private void Start()
    {
        if (inventorySystem == null)
            inventorySystem = Inven_System.instance;

        ApplySessionData();
    }

    public void SetHp(int newHp)
    {
        int clampedValue = Mathf.Max(0, newHp);
        if (hp == clampedValue)
            return;

        hp = clampedValue;
        GameSession.MarkPlayerDirty();
    }

    public void SetStamina(int newStamina)
    {
        int clampedValue = Mathf.Max(0, newStamina);
        if (stamina == clampedValue)
            return;

        stamina = clampedValue;
        GameSession.MarkPlayerDirty();
    }

    public void SetLevel(int newLevel)
    {
        int clampedValue = Mathf.Max(1, newLevel);
        if (level == clampedValue)
            return;

        level = clampedValue;
        GameSession.MarkPlayerDirty();
    }

    public void SetExperience(int newExperience)
    {
        int clampedValue = Mathf.Max(0, newExperience);
        if (experience == clampedValue)
            return;

        experience = clampedValue;
        GameSession.MarkPlayerDirty();
    }

    public void SetGold(int newGold)
    {
        int clampedValue = Mathf.Max(0, newGold);
        if (gold == clampedValue)
            return;

        gold = clampedValue;
        GameSession.MarkDataDirty();
    }

    public void ApplySessionData()
    {
        if (!GameSession.HasActiveSession)
        {
            ApplyDefaultData();
            return;
        }

        SaveGameData saveData = GameSession.CurrentSaveData;
        if (saveData == null || saveData.player == null || saveData.data == null)
        {
            ApplyDefaultData();
            return;
        }

        hp = Mathf.Max(0, saveData.player.hp);
        stamina = Mathf.Max(0, saveData.player.stamina);
        level = Mathf.Max(1, saveData.player.level);
        experience = Mathf.Max(0, saveData.player.experience);
        gold = Mathf.Max(0, saveData.data.gold);

        if (playerTransform != null)
        {
            Vector3 loadedPosition = playerTransform.position;
            loadedPosition.x = saveData.player.positionX;
            loadedPosition.y = saveData.player.positionY;
            playerTransform.position = loadedPosition;
        }

        if (inventorySystem != null)
            inventorySystem.LoadInventorySaveData(saveData.data.inventory);
    }

    public void CaptureCurrentStateToSession()
    {
        if (!GameSession.HasActiveSession)
            return;

        // 저장 직전에 드래그 중 상태를 정리해 세이브 불일치를 막는다.
        if (inventorySystem != null)
            inventorySystem.ReturnDragItem();

        SaveGameData saveData = GameSession.CurrentSaveData;
        if (saveData == null)
            return;

        if (saveData.player == null)
            saveData.player = PlayerSaveData.CreateDefault();

        if (saveData.data == null)
            saveData.data = GameDataSaveData.CreateDefault();

        saveData.player.hp = hp;
        saveData.player.stamina = stamina;
        saveData.player.level = level;
        saveData.player.experience = experience;
        saveData.player.sceneName = SceneManager.GetActiveScene().name;

        saveData.data.gold = gold;

        if (playerTransform != null)
        {
            saveData.player.positionX = playerTransform.position.x;
            saveData.player.positionY = playerTransform.position.y;
        }

        if (inventorySystem != null)
        {
            if (saveData.data.inventory == null)
                saveData.data.inventory = new InventorySaveData();

            inventorySystem.WriteInventorySaveData(saveData.data.inventory);
        }
    }

    public void SaveCurrentSession()
    {
        CaptureCurrentStateToSession();
        GameSession.SaveCurrent();
    }

    private void ApplyDefaultData()
    {
        PlayerSaveData defaultPlayerData = PlayerSaveData.CreateDefault();
        GameDataSaveData defaultGameData = GameDataSaveData.CreateDefault();

        hp = defaultPlayerData.hp;
        stamina = defaultPlayerData.stamina;
        level = defaultPlayerData.level;
        experience = defaultPlayerData.experience;
        gold = defaultGameData.gold;

        if (inventorySystem != null)
            inventorySystem.LoadInventorySaveData(defaultGameData.inventory);
    }
}
