using UnityEngine;

// 역할: 바위 상호작용 오브젝트의 내구도, 파편, 드랍 데이터를 정의하는 SO다.
[CreateAssetMenu(fileName = "NewRockData", menuName = "World/Rock Data")]
public class RockDataSO : ScriptableObject
{
    [Header("Basic")]
    public string displayName = "Rock";
    public float interactRange = 2f;

    [Header("Durability")]
    public int maxHp = 3;

    [Header("Required Tool")]
    public ToolType requiredToolType = ToolType.Pickaxe;
    [Min(1)]
    public int requiredToolLevel = 1;

    [Header("Break Effect")]
    public BreakShardSettings breakShards = new BreakShardSettings();

    [Header("Drops (Optional)")]
    public bool useDrops = false;
    public WeightedDropEntry[] dropEntries =
    {
        new WeightedDropEntry { itemId = 1, weight = 50 },
        new WeightedDropEntry { dropNothing = true, weight = 50 }
    };

    // 역할: 드랍 테이블에서 한 번만 가중치 선택해 결과 아이템 id를 반환한다.
    public bool TryRollDropItemId(out int itemId)
    {
        itemId = 0;

        if (!useDrops || dropEntries == null || dropEntries.Length == 0)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < dropEntries.Length; i++)
        {
            WeightedDropEntry entry = dropEntries[i];
            if (entry == null || entry.weight <= 0)
                continue;

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return false;

        int randomValue = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        for (int i = 0; i < dropEntries.Length; i++)
        {
            WeightedDropEntry entry = dropEntries[i];
            if (entry == null || entry.weight <= 0)
                continue;

            accumulatedWeight += entry.weight;
            if (randomValue >= accumulatedWeight)
                continue;

            if (entry.dropNothing || entry.itemId <= 0)
                return false;

            itemId = entry.itemId;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (requiredToolLevel < 1)
            requiredToolLevel = 1;
    }
#endif
}
