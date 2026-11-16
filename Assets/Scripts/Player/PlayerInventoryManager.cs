using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
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
        Debug.Log("Player recebeu o item: " + data.Name);

    }
}
