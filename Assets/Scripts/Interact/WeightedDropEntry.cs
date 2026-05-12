using UnityEngine;

// 역할: 드랍 테이블의 단일 항목을 표현한다.
[System.Serializable]
public sealed class WeightedDropEntry
{
    [Header("드랍 결과")]
    public bool dropNothing = false;
    [Min(1)]
    public int itemId = 1;

    [Header("가중치")]
    [Min(0)]
    public int weight = 1;
}
