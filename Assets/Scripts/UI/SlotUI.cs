using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{

    [SerializeField] private Image itemRenderer;
    [SerializeField] private GameObject amountItemObject;
    [SerializeField] private TextMeshProUGUI amountItemText;

    public void SetupSlot(ItemSlot itemSlot)
    {
        var sprite = itemSlot.Item != null ?itemSlot.Item.ItemUIImage: null;
        itemRenderer.sprite = sprite;
        amountItemObject.SetActive(itemSlot.Amount > 1);
        amountItemText.text = "" + itemSlot.Amount;
    }
}
