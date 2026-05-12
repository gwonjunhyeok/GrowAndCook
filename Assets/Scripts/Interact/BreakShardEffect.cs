using UnityEngine;

// 역할: 파편 SpriteRenderer 묶음을 재사용하며 짧게 흩뿌려지는 파괴 연출을 재생한다.
public sealed class BreakShardEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] shardRenderers;

    private Vector2[] velocities;
    private float[] angularSpeeds;
    private float[] startScales;
    private float lifetime;
    private float velocityDamping;
    private float endScaleMultiplier;
    private float elapsedTime;
    private int activeShardCount;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

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
        if (shardRenderers == null || shardRenderers.Length == 0)
            AutoBind();

        int shardCount = shardRenderers != null ? shardRenderers.Length : 0;
        velocities = new Vector2[shardCount];
        angularSpeeds = new float[shardCount];
        startScales = new float[shardCount];
        SetAllShardsVisible(false);
    }

    private void OnDisable()
    {
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        elapsedTime += deltaTime;
        float normalizedTime = lifetime > 0f ? Mathf.Clamp01(elapsedTime / lifetime) : 1f;
        float dampingFactor = Mathf.Exp(-velocityDamping * deltaTime);
        float scaleMultiplier = Mathf.Lerp(1f, endScaleMultiplier, normalizedTime);
        float alpha = 1f - normalizedTime;

        for (int i = 0; i < activeShardCount; i++)
        {
            SpriteRenderer shard = shardRenderers[i];
            if (shard == null)
                continue;

            Transform shardTransform = shard.transform;
            Vector2 localPosition = shardTransform.localPosition;
            localPosition += velocities[i] * deltaTime;
            shardTransform.localPosition = localPosition;
            shardTransform.Rotate(0f, 0f, angularSpeeds[i] * deltaTime);
            shardTransform.localScale = Vector3.one * (startScales[i] * scaleMultiplier);

            Color color = shard.color;
            color.a = alpha;
            shard.color = color;

            velocities[i] *= dampingFactor;
        }

        if (normalizedTime >= 1f)
            StopEffect();
    }

    public void Play(Sprite sprite, Color color, int sortingLayerId, int sortingOrder, BreakShardSettings settings)
    {
        if (sprite == null || settings == null || shardRenderers == null || shardRenderers.Length == 0)
        {
            StopEffect();
            return;
        }

        lifetime = Mathf.Max(0.01f, settings.lifetime);
        velocityDamping = Mathf.Max(0f, settings.velocityDamping);
        endScaleMultiplier = Mathf.Clamp(settings.endScaleMultiplier, 0f, 1f);
        elapsedTime = 0f;
        activeShardCount = Mathf.Clamp(settings.shardCount, 1, shardRenderers.Length);
        isPlaying = true;

        for (int i = 0; i < shardRenderers.Length; i++)
        {
            SpriteRenderer shard = shardRenderers[i];
            if (shard == null)
                continue;

            bool shouldUse = i < activeShardCount;
            shard.enabled = shouldUse;

            if (!shouldUse)
                continue;

            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;

            direction.Normalize();

            float scale = Random.Range(settings.scaleMin, settings.scaleMax);
            float speed = Random.Range(settings.speedMin, settings.speedMax);
            float angle = Random.Range(0f, 360f);

            shard.sprite = sprite;
            shard.sortingLayerID = sortingLayerId;
            shard.sortingOrder = sortingOrder + settings.sortingOrderOffset;
            shard.color = color;

            Transform shardTransform = shard.transform;
            shardTransform.localPosition = (Vector3)(Random.insideUnitCircle * settings.spawnRadius);
            shardTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            shardTransform.localScale = Vector3.one * scale;

            velocities[i] = direction * speed;
            angularSpeeds[i] = Random.Range(settings.angularSpeedMin, settings.angularSpeedMax);
            startScales[i] = scale;
        }
    }

    private void StopEffect()
    {
        isPlaying = false;
        SetAllShardsVisible(false);
        gameObject.SetActive(false);
    }

    private void SetAllShardsVisible(bool visible)
    {
        if (shardRenderers == null)
            return;

        for (int i = 0; i < shardRenderers.Length; i++)
        {
            if (shardRenderers[i] != null)
                shardRenderers[i].enabled = visible;
        }
    }

    private void AutoBind()
    {
        shardRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }
}
