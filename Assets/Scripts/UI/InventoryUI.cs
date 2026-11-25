using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{

    [SerializeField] private List<InventorySlotUI> slotsItem;

    void OnEnable()
    {
        GameEvents.UI.UpdateAvatarItemUI += UpdateItemUI;
    }

    void OnDisable()
    {
        GameEvents.UI.UpdateAvatarItemUI -= UpdateItemUI;
    }

    private void UpdateItemUI(List<ItemSlot> list)
    {
        for(int i = 0; i< list.Count; i++)
        {
            slotsItem[i].SetupSlot(list[i]);
        }
    }
}
