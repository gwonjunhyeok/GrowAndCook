using UnityEngine;
using UnityEngine.Localization.Settings;

public enum ItemType
{
    Material,
    Consumable,
    Equipment,
    Tool,
    Etc
}

public enum EquipSlotType
{
    None = 0,
    Head = 1,
    Body = 2,
    Leg = 3,
    Feet = 4,
    Bag = 5
}

public enum ToolType
{
    None = 0,
    Pickaxe = 1,
    Axe = 2,
    Knife = 3,
    Hoe = 4
}

[System.Serializable]
public sealed class LocalizedItemText
{
    [Header("이름")]
    public string koreanName;
    public string englishName;

    [Header("설명")]
    [TextArea]
    public string koreanDescription;

    [TextArea]
    public string englishDescription;
}

// 역할: 아이템의 고유 데이터와 언어별 표시 데이터, 장착 가능 정보를 정의한다.
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("고유 ID (저장/로드 및 비교 기준)")]
    public int id = 1;

    [Header("Localized Text")]
    public LocalizedItemText localizedText = new LocalizedItemText();

    [Header("Stack")]
    [Min(1)]
    public int maxStack = 1;

    [Header("UI")]
    public Sprite icon;

    [Header("Type")]
    public ItemType itemType = ItemType.Etc;

    [Header("Equip")]
    [Tooltip("장비 아이템일 때 장착 가능한 슬롯 종류")]
    public EquipSlotType equipSlotType = EquipSlotType.None;

    [Header("Tool")]
    [Tooltip("도구 아이템일 때 사용하는 도구 종류")]
    public ToolType toolType = ToolType.None;

    [Tooltip("도구 아이템일 때 사용하는 도구 레벨")]
    [Min(0)]
    public int toolLevel = 0;

    public string itemName => GetDisplayName();
    public string ItemDescription => GetDescription();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (id < 1)
            id = 1;

        if (maxStack < 1)
            maxStack = 1;

        // 장비와 도구는 스택 불가를 기본값으로 유지한다.
        if (itemType == ItemType.Equipment || itemType == ItemType.Tool)
            maxStack = 1;

        if (itemType != ItemType.Equipment)
            equipSlotType = EquipSlotType.None;

        if (itemType == ItemType.Tool)
        {
            toolLevel = Mathf.Clamp(toolLevel, 1, 4);
        }
        else
        {
            toolType = ToolType.None;
            toolLevel = 0;
        }
    }
#endif

    public string GetDisplayName(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.Korean:
                return GetFallbackText(localizedText.koreanName, localizedText.englishName, "Item");
            default:
                return GetFallbackText(localizedText.englishName, localizedText.koreanName, "Item");
        }
    }

    public string GetDisplayName()
    {
        return GetDisplayName(GetCurrentLanguage());
    }

    public string GetDescription(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.Korean:
                return GetFallbackText(localizedText.koreanDescription, localizedText.englishDescription, string.Empty);
            default:
                return GetFallbackText(localizedText.englishDescription, localizedText.koreanDescription, string.Empty);
        }
    }

    public string GetDescription()
    {
        return GetDescription(GetCurrentLanguage());
    }

    public bool IsEquipable()
    {
        return itemType == ItemType.Equipment;
    }

    public bool IsTool()
    {
        return itemType == ItemType.Tool;
    }

    public bool IsTool(ToolType requiredToolType, int minToolLevel)
    {
        if (!IsTool())
            return false;

        if (toolType != requiredToolType)
            return false;

        return toolLevel >= minToolLevel;
    }

    public bool CanEquipTo(EquipSlotType targetSlotType)
    {
        if (!IsEquipable())
            return false;

        if (equipSlotType == EquipSlotType.None)
            return false;

        return equipSlotType == targetSlotType;
    }

    private static SystemLanguage GetCurrentLanguage()
    {
        if (LocalizationSettings.HasSettings && LocalizationSettings.SelectedLocale != null)
        {
            string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            if (!string.IsNullOrEmpty(localeCode) && localeCode.StartsWith("ko"))
                return SystemLanguage.Korean;

            return SystemLanguage.English;
        }

        return Application.systemLanguage;
    }

    private static string GetFallbackText(string primary, string secondary, string defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(primary))
            return primary;

        if (!string.IsNullOrWhiteSpace(secondary))
            return secondary;

        return defaultValue;
    }
}
