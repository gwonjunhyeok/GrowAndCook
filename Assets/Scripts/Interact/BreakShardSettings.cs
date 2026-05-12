using UnityEngine;

// 역할: 파괴 파편 연출에 필요한 수치만 묶어 두는 직렬화 데이터다.
[System.Serializable]
public sealed class BreakShardSettings
{
    [Header("기본 사용 여부")]
    public bool useBreakShards = true;

    [Header("조각 개수")]
    [Range(1, 20)]
    public int shardCount = 20;

    [Header("수명")]
    [Min(0.05f)]
    public float lifetime = 0.45f;

    [Header("초기 퍼짐")]
    [Min(0f)]
    public float spawnRadius = 0.08f;
    [Min(0f)]
    public float speedMin = 1.4f;
    [Min(0f)]
    public float speedMax = 2.8f;

    [Header("감쇠")]
    [Min(0f)]
    public float velocityDamping = 6f;

    [Header("크기")]
    [Min(0.01f)]
    public float scaleMin = 0.12f;
    [Min(0.01f)]
    public float scaleMax = 0.2f;
    [Range(0f, 1f)]
    public float endScaleMultiplier = 0.6f;

    [Header("회전")]
    public float angularSpeedMin = -360f;
    public float angularSpeedMax = 360f;

    [Header("정렬")]
    public int sortingOrderOffset = 1;
}
