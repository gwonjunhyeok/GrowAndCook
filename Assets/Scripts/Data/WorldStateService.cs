using System.Collections.Generic;
using UnityEngine;

// 역할: 현재 지역의 월드 상태를 메모리에서 관리하고 씬 오브젝트에 복원한다.
public sealed class WorldStateService : MonoBehaviour
{
    [SerializeField] private string currentRegionId = "Main_Map";

    private readonly Dictionary<string, WorldObjectIdentity> worldObjectRegistry = new();
    private readonly List<WorldObjectIdentity> applyBuffer = new();
    private WorldRegionSaveData currentRegionData;

    public static WorldStateService Instance { get; private set; }

    public string CurrentRegionId => currentRegionId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeCurrentRegion();
        RebuildRegistry();
        ApplyCurrentRegionState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 역할: 씬에 활성화된 월드 오브젝트를 등록 테이블로 다시 구성한다.
    public void RebuildRegistry()
    {
        worldObjectRegistry.Clear();

        IReadOnlyList<WorldObjectIdentity> identities = WorldObjectIdentity.RegisteredIdentities;
        for (int i = 0; i < identities.Count; i++)
        {
            RegisterWorldObject(identities[i]);
        }
    }

    // 역할: 월드 상태가 저장 대상인 오브젝트를 ID 기반으로 등록한다.
    public void RegisterWorldObject(WorldObjectIdentity identity)
    {
        if (identity == null)
            return;

        string objectId = identity.ObjectId;
        if (string.IsNullOrWhiteSpace(objectId))
        {
            Debug.LogWarning($"{identity.name}: WorldObjectIdentity.objectId가 비어 있습니다.");
            return;
        }

        worldObjectRegistry[objectId] = identity;

        if (currentRegionData != null)
            ApplyStateToObject(identity);
    }

    // 역할: 씬에서 사라진 오브젝트를 등록 테이블에서 제거한다.
    public void UnregisterWorldObject(WorldObjectIdentity identity)
    {
        if (identity == null || string.IsNullOrWhiteSpace(identity.ObjectId))
            return;

        if (worldObjectRegistry.TryGetValue(identity.ObjectId, out WorldObjectIdentity registeredIdentity)
            && registeredIdentity == identity)
        {
            worldObjectRegistry.Remove(identity.ObjectId);
        }
    }

    // 역할: 부숴지거나 소진된 오브젝트를 현재 지역 상태에 기록한다.
    public void MarkObjectDepleted(WorldObjectIdentity identity)
    {
        if (identity == null)
            return;

        MarkObjectDepleted(identity.ObjectId);
    }

    // 역할: 저장용 ID 기준으로 소진 상태를 기록한다.
    public void MarkObjectDepleted(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return;

        if (!EnsureCurrentRegionData())
            return;

        if (!currentRegionData.TryAddDepletedObject(objectId))
            return;

        GameSession.CurrentSaveData.world.currentRegionId = currentRegionId;
        GameSession.MarkWorldDirty();
    }

    // 역할: 현재 지역에 이미 소진된 오브젝트인지 조회한다.
    public bool IsObjectDepleted(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId) || currentRegionData == null)
            return false;

        return currentRegionData.ContainsDepletedObject(objectId);
    }

    // 역할: 현재 지역 데이터를 보장하고 첫 진입이면 기본 region을 만든다.
    public void InitializeCurrentRegion()
    {
        if (!EnsureCurrentRegionData())
            return;

        GameSession.CurrentSaveData.world.currentRegionId = currentRegionId;
    }

    // 역할: 현재 지역에 저장된 상태를 씬 오브젝트들에 반영한다.
    public void ApplyCurrentRegionState()
    {
        if (currentRegionData == null)
            return;

        applyBuffer.Clear();

        foreach (KeyValuePair<string, WorldObjectIdentity> pair in worldObjectRegistry)
            applyBuffer.Add(pair.Value);

        for (int i = 0; i < applyBuffer.Count; i++)
            ApplyStateToObject(applyBuffer[i]);
    }

    private void ApplyStateToObject(WorldObjectIdentity identity)
    {
        if (identity == null)
            return;

        if (!IsObjectDepleted(identity.ObjectId))
            return;

        identity.gameObject.SetActive(false);
    }

    private bool EnsureCurrentRegionData()
    {
        if (!GameSession.HasActiveSession)
            return false;

        SaveGameData saveData = GameSession.CurrentSaveData;
        if (saveData == null)
            return false;

        if (saveData.world == null)
            saveData.world = WorldSaveData.CreateDefault();

        bool regionCreated;
        currentRegionData = saveData.world.GetOrCreateRegion(currentRegionId, out regionCreated);
        if (currentRegionData == null)
            return false;

        if (regionCreated)
            GameSession.MarkWorldDirty();

        return true;
    }
}
