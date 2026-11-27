using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{

    [SerializeField] private Image itemRenderer;
    [SerializeField] private GameObject amountItemObject;
    [SerializeField] private TextMeshProUGUI amountItemText;
    private ItemSlot item;
    private Action<int> action;
    public void SetupSlot(ItemSlot itemSlot, Action<int> callback)
    {
        action = callback;
        item = itemSlot;
        var sprite = itemSlot.Item != null ?itemSlot.Item.ItemUIImage: null;
        itemRenderer.sprite = sprite;
        amountItemObject.SetActive(itemSlot.Amount > 1);
        amountItemText.text = "" + itemSlot.Amount;
    }

    public void Click()
    {
        action?.Invoke(gameObject.transform.GetSiblingIndex());
    }
}
