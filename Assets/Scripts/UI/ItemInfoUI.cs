using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Image itemImage;
    
    private  ItemSlot itemSlot;

    public void Setup(ItemSlot item)
    {
        itemSlot = item;
        FillDataInformation();
    }

    public void FillDataInformation()
    {
        itemName.text = itemSlot.Item.Name;
        itemDescription.text = itemSlot.Item.Description;
        itemImage.sprite = itemSlot.Item.ItemUIImage;
    }
    public void Reset()
    {
        itemName.text = "";
        itemDescription.text = "";
    }
}
