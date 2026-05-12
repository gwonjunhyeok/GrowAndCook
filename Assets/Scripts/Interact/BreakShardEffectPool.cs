using System.Collections.Generic;
using UnityEngine;

// 역할: 파편 이펙트 프리팹을 재사용하며 파괴 연출 요청을 공용으로 처리한다.
public sealed class BreakShardEffectPool : MonoBehaviour
{
    private static BreakShardEffectPool instance;

    [Header("Pool")]
    [SerializeField] private BreakShardEffect effectPrefab;
    [SerializeField] private int initialPoolSize = 4;
    [SerializeField] private Transform poolRoot;

    private readonly List<BreakShardEffect> pooledEffects = new();

    public static BreakShardEffectPool Instance
    {
        get
        {
            if (instance == null)
                instance = Object.FindFirstObjectByType<BreakShardEffectPool>();

            return instance;
        }
    }

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

    public void Play(Vector3 worldPosition, Sprite sprite, Color color, int sortingLayerId, int sortingOrder, BreakShardSettings settings)
    {
        if (effectPrefab == null || sprite == null || settings == null || !settings.useBreakShards)
            return;

        BreakShardEffect effect = GetOrCreate();
        if (effect == null)
            return;

        Transform effectTransform = effect.transform;
        effectTransform.SetParent(poolRoot, false);
        effectTransform.position = worldPosition;
        effectTransform.rotation = Quaternion.identity;
        effectTransform.localScale = Vector3.one;

        effect.gameObject.SetActive(true);
        effect.Play(sprite, color, sortingLayerId, sortingOrder, settings);
    }

    private void Prewarm()
    {
        int count = Mathf.Max(0, initialPoolSize);
        for (int i = 0; i < count; i++)
            CreateEffect();
    }

    private BreakShardEffect GetOrCreate()
    {
        for (int i = 0; i < pooledEffects.Count; i++)
        {
            BreakShardEffect effect = pooledEffects[i];
            if (effect != null && !effect.gameObject.activeSelf)
                return effect;
        }

        return CreateEffect();
    }

    private BreakShardEffect CreateEffect()
    {
        if (effectPrefab == null)
            return null;

        BreakShardEffect created = Instantiate(effectPrefab, poolRoot != null ? poolRoot : transform);
        created.gameObject.SetActive(false);
        pooledEffects.Add(created);
        return created;
    }
}
