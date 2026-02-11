using UnityEngine;

public enum ItemType
{
    Material,   // 재료(예: 나무, 돌, 광석)
    Consumable, // 소모품(예: 음식)
    Tool,       // 도구/장비류(스택 불가)
    Bag,        // 가방(스택 불가)
    Etc         // 기타(임시 분류)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("고유 ID (저장/로드 및 비교 기준). 1 이상 권장")]
    public int id = 1;

    [Tooltip("표시용 이름(언어 변경 가능). 비교 기준으로 쓰지 않기")]
    public string itemName;

    [Header("Stack")]
    [Min(1)]
    public int maxStack = 1;

    [Header("UI")]
    public Sprite icon;

    [Header("Type")]
    public ItemType itemType = ItemType.Etc;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (id < 1) id = 1;
        if (maxStack < 1) maxStack = 1;

        // 장비류(도구/가방)는 스택 금지
        if (itemType == ItemType.Tool || itemType == ItemType.Bag)
        {
            maxStack = 1;
        }
    }
#endif
}
