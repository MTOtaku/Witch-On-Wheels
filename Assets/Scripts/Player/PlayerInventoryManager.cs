using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class PlayerInventoryManager : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> playerItems;
    private const int MAX_INVENTORY_ITEMS = 20;

    void Start()
    {
        InitializeInventory();
    }

    private void OnEnable()
    {
        GameEvents.Item.DeliveryItem += ReceiveItem;
    }

    private void OnDisable()
    {
        GameEvents.Item.DeliveryItem -= ReceiveItem;

    }

    private void InitializeInventory()
    {
        for(int i = 0; i < MAX_INVENTORY_ITEMS; i++)
        {
            var freeSlot = new ItemSlot();
            playerItems.Add(freeSlot);
        }
        GameEvents.UI.UpdateAvatarItemUI?.Invoke(playerItems);
    }

    private void ReceiveItem(ItemData data)
    {
        var item = playerItems.Find(x => x.Item?.Name == data.Name);
        if(item != null)
        {
            item.Amount++;
        }
        else
        {
            ItemSlot newItem = new ItemSlot();
            newItem.ObtainNewItem(data);
            var freeSlotIndex = playerItems.FindIndex(x => x.Item == null);
            if(freeSlotIndex >= 0)
            {
                playerItems[freeSlotIndex] = newItem;
            }
            else
            {
                Debug.LogWarning("Inventório lotado impossível adicionar outro item");
            }
        }
        GameEvents.Quest.PlayerObtainItem?.Invoke(data);
        GameEvents.UI.UpdateAvatarItemUI?.Invoke(playerItems);
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
