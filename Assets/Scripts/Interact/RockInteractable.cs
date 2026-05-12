using UnityEngine;

// 역할: 바위 상호작용과 내구도 감소를 처리하고, 파괴 시 파편 연출을 요청한다.
public class RockInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private RockDataSO data;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private WorldObjectIdentity worldObjectIdentity;

    private int hp;

    public float InteractRange => data != null ? data.interactRange : 2f;

#if UNITY_EDITOR
    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            AutoBind();
    }
#endif

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: RockDataSO가 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        if (targetSpriteRenderer == null)
            AutoBind();

        hp = Mathf.Max(1, data.maxHp);
    }

    public void Interact(Transform interactor)
    {
        if (!CanBreakWithSelectedTool())
            return;

        Debug.Log($"Hit {data.displayName}: {name} (HP {hp}/{data.maxHp})");

        Shake shake = GetComponent<Shake>();
        if (shake != null)
            shake.Play();

        hp--;

        if (hp <= 0)
        {
            Debug.Log($"Break {data.displayName}: {name}");
            MarkWorldState();
            PlayBreakShards();
            SpawnDropIfNeeded();
            Destroy(gameObject);
        }
    }

    // 역할: 현재 선택한 핫바 아이템이 바위 채굴 조건을 만족하는지 검사한다.
    private bool CanBreakWithSelectedTool()
    {
        Inven_System inventorySystem = Inven_System.instance;
        if (inventorySystem == null)
        {
            Debug.LogWarning($"{name}: Inven_System.instance가 없어 채굴 조건을 확인할 수 없습니다.");
            return false;
        }

        if (!inventorySystem.TryGetSelectedHotbarItem(out ItemData selectedItem))
        {
            Debug.Log($"{name}: 현재 선택된 핫바 아이템이 없어 채굴하지 않습니다.");
            return false;
        }

        if (selectedItem.IsTool(data.requiredToolType, data.requiredToolLevel))
            return true;

        Debug.Log($"{name}: {selectedItem.GetDisplayName()}으로는 채굴할 수 없습니다. 필요 도구={data.requiredToolType}, 최소 레벨={data.requiredToolLevel}");
        return false;
    }

    // 역할: 돌이 부숴졌다는 사실을 현재 지역 월드 상태에 기록한다.
    private void MarkWorldState()
    {
        if (worldObjectIdentity == null)
            AutoBind();

        if (worldObjectIdentity == null)
        {
            Debug.LogWarning($"{name}: WorldObjectIdentity가 없어 월드 상태를 기록하지 못했습니다.");
            return;
        }

        WorldStateService worldStateService = WorldStateService.Instance;
        if (worldStateService == null)
        {
            Debug.LogWarning($"{name}: WorldStateService가 없어 월드 상태를 기록하지 못했습니다.");
            return;
        }

        worldStateService.MarkObjectDepleted(worldObjectIdentity);
    }

    private void PlayBreakShards()
    {
        if (data == null || data.breakShards == null || !data.breakShards.useBreakShards)
            return;

        if (targetSpriteRenderer == null || targetSpriteRenderer.sprite == null)
            return;

        BreakShardEffectPool pool = BreakShardEffectPool.Instance;
        if (pool == null)
        {
            Debug.LogWarning($"{name}: BreakShardEffectPool을 찾지 못했습니다.");
            return;
        }

        pool.Play(
            targetSpriteRenderer.bounds.center,
            targetSpriteRenderer.sprite,
            targetSpriteRenderer.color,
            targetSpriteRenderer.sortingLayerID,
            targetSpriteRenderer.sortingOrder,
            data.breakShards);
    }

    // 역할: 현재 돌의 드랍 설정이 실제로 동작하는지 단계별로 로그를 남긴다.
    private void SpawnDropIfNeeded()
    {
        if (data == null)
        {
            Debug.LogWarning($"{name}: 드랍 실패 - RockDataSO가 없습니다.");
            return;
        }

        Debug.Log($"{name}: 드랍 체크 시작, useDrops={data.useDrops}");

        if (!data.useDrops)
        {
            Debug.Log($"{name}: 드랍 미사용 상태입니다.");
            return;
        }

        if (!data.TryRollDropItemId(out int itemId))
        {
            Debug.Log($"{name}: 드랍 결과 없음");
            return;
        }

        Debug.Log($"{name}: 드랍 itemId={itemId} 선택");

        Inven_System inventorySystem = Inven_System.instance;
        if (inventorySystem == null)
        {
            Debug.LogWarning($"{name}: 드랍 실패 - Inven_System.instance가 없습니다.");
            return;
        }

        ItemData itemData = inventorySystem.FindItemById(itemId);
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: 드랍 실패 - itemId={itemId}에 해당하는 ItemData를 찾지 못했습니다.");
            return;
        }

        Debug.Log($"{name}: ItemData 조회 성공 - id={itemData.id}, name={itemData.GetDisplayName()}");

        WorldItemDropPool dropPool = WorldItemDropPool.Instance;
        if (dropPool == null)
        {
            Debug.LogWarning($"{name}: 드랍 실패 - WorldItemDropPool을 찾지 못했습니다.");
            return;
        }

        Vector3 dropPosition = targetSpriteRenderer != null
            ? targetSpriteRenderer.bounds.center
            : transform.position;

        WorldItemDrop spawnedDrop = dropPool.Spawn(dropPosition, itemData, 1);
        if (spawnedDrop == null)
        {
            Debug.LogWarning($"{name}: 드랍 실패 - WorldItemDropPool.Spawn이 null을 반환했습니다.");
            return;
        }

        Debug.Log($"{name}: 드랍 스폰 성공 - {itemData.GetDisplayName()}");
    }

    private void AutoBind()
    {
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (worldObjectIdentity == null)
            worldObjectIdentity = GetComponent<WorldObjectIdentity>();
    }
}
