using System.Collections.Generic;
using UnityEngine;

// 역할: 월드 드랍 프리팹을 재사용해 드랍 생성 비용을 줄인다.
public sealed class WorldItemDropPool : MonoBehaviour
{
    private static WorldItemDropPool instance;

    [Header("Pool")]
    [SerializeField] private WorldItemDrop dropPrefab;
    [SerializeField] private int initialPoolSize = 6;
    [SerializeField] private Transform poolRoot;

    private readonly List<WorldItemDrop> pooledDrops = new();

    public static WorldItemDropPool Instance => instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (poolRoot == null)
            poolRoot = transform;

        Prewarm();
    }

    public WorldItemDrop Spawn(Vector3 worldPosition, ItemData itemData, int itemCount)
    {
        if (dropPrefab == null || itemData == null)
            return null;

        WorldItemDrop drop = GetOrCreate();
        if (drop == null)
            return null;

        Transform dropTransform = drop.transform;
        dropTransform.SetParent(poolRoot, false);
        dropTransform.position = worldPosition;
        dropTransform.rotation = Quaternion.identity;

        drop.gameObject.SetActive(true);
        drop.Initialize(itemData, itemCount);
        return drop;
    }

    private void Prewarm()
    {
        int count = Mathf.Max(0, initialPoolSize);
        for (int i = 0; i < count; i++)
            CreateDrop();
    }

    private WorldItemDrop GetOrCreate()
    {
        for (int i = 0; i < pooledDrops.Count; i++)
        {
            WorldItemDrop drop = pooledDrops[i];
            if (drop != null && !drop.gameObject.activeSelf)
                return drop;
        }

        return CreateDrop();
    }

    private WorldItemDrop CreateDrop()
    {
        if (dropPrefab == null)
            return null;

        WorldItemDrop created = Instantiate(dropPrefab, poolRoot != null ? poolRoot : transform);
        created.gameObject.SetActive(false);
        pooledDrops.Add(created);
        return created;
    }
}
