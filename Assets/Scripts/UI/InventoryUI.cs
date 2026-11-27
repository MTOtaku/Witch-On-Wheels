using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject questInfoSide;

    [SerializeField] private List<InventorySlotUI> slotsItem;
    [SerializeField] private List<ItemSlot> itens;
    [SerializeField] private ItemInfoUI itemInfo;

    void OnEnable()
    {
        GameEvents.UI.UpdateAvatarItemUI += UpdateItemUI;
    }

    void OnDisable()
    {
        GameEvents.UI.UpdateAvatarItemUI -= UpdateItemUI;
    }

    public void UpdateItemUI(List<ItemSlot> list)
    {
        itens = list;
        for(int i = 0; i< list.Count; i++)
        {
            slotsItem[i].SetupSlot(list[i], ClickAction);
        }
    }

    public void ClickAction(int index)
    {
        questInfoSide.SetActive(true);
        if(itens[index].Item != null)
        {
            itemInfo.Setup(itens[index]);
        }
    }
}
