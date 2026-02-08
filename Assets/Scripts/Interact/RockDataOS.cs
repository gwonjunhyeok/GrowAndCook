using UnityEngine;

[CreateAssetMenu(fileName = "NewRockData", menuName = "World/Rock Data")]
public class RockDataSO : ScriptableObject
{
    [Header("Basic")]
    public string displayName = "Rock";
    public float interactRange = 2f;

    [Header("Durability")]
    public int maxHp = 3;

    [Header("Drops (Optional)")]
    public bool useDrops = false;
}
