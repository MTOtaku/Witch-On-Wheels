using System.Collections.Generic;
using UnityEngine;
public class PlayerInventoryManager : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> playerItems; 
    private void OnEnable()
    {
        GameEvents.Item.DeliveryItem += ReceiveItem;
    }

    private void OnDisable()
    {
        GameEvents.Item.DeliveryItem -= ReceiveItem;

    }

    private void ReceiveItem(ItemData data)
    {
        var item = playerItems.Find(x => x.Item.Name == data.Name);
        if(item != null)
        {
            Debug.Log("Item Existente");
            item.Amount++;
        }
        else
        {
            ItemSlot newItem = new ItemSlot();
            newItem.ObtainNewItem(data);
            playerItems.Add(newItem);
        }
        GameEvents.Quest.PlayerObtainItem?.Invoke(data);
    }
}

[System.Serializable]
public class ItemSlot
{
    public ItemData Item;
    public int Amount;

    public void ObtainNewItem(ItemData data, int amount = 1)
    {
        Item = data;
        Amount = amount;
    }
}
