using System;
using UnityEngine;

[Serializable]
public class ItemStack
{
    public ItemData itemData;
    public int count;

    public ItemStack(ItemData data, int amount = 1)
    {
        itemData = data;
        count = Mathf.Max(0, amount);

        // 장비류는 ItemData에서 maxStack=1로 강제되지만,
        // 혹시 데이터가 잘못되어도 안전하게 한 번 더 보정
        ClampToMaxStack();
    }

    public bool IsEmpty => itemData == null || count <= 0;

    public int MaxStack => itemData != null ? Mathf.Max(1, itemData.maxStack) : 1;

    public bool IsFull => !IsEmpty && count >= MaxStack;

    public int ItemId => itemData != null ? itemData.id : -1;

    public bool CanStackWith(ItemStack other)
    {
        if (other == null) return false;
        if (IsEmpty || other.IsEmpty) return false;

        // id 기준으로 같은 아이템인지 판정
        return ItemId == other.ItemId;
    }

    // 주어진 수량을 이 스택에 추가하고, 실제로 추가된 수량을 반환
    public int Add(int amount)
    {
        if (IsEmpty) return 0;
        if (amount <= 0) return 0;

        int space = MaxStack - count;
        int add = Mathf.Clamp(amount, 0, space);
        count += add;
        return add;
    }

    // 주어진 수량을 이 스택에서 제거하고, 실제로 제거된 수량을 반환
    public int Remove(int amount)
    {
        if (IsEmpty) return 0;
        if (amount <= 0) return 0;

        int remove = Mathf.Clamp(amount, 0, count);
        count -= remove;

        if (count <= 0)
        {
            count = 0;
        }

        return remove;
    }

    public ItemStack Clone(int newCount)
    {
        if (itemData == null) return null;
        return new ItemStack(itemData, newCount);
    }

    public void ClampToMaxStack()
    {
        if (itemData == null)
        {
            count = 0;
            return;
        }

        count = Mathf.Clamp(count, 0, MaxStack);
    }
}
