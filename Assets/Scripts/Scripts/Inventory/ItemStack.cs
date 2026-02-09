[System.Serializable]
public class ItemStack
{
    public ItemData itemData;
    public int count;

    public ItemStack(ItemData data, int amount = 1)
    {
        itemData = data;
        count = amount;
    }

    public bool IsFull => count >= itemData.maxStack;
}
