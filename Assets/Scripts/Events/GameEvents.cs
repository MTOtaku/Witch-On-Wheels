using UnityEngine;
using System;

public static class GameEvents
{
    public static class Quest
    {
        public static Action<QuestData> DeliveryQuest;
    }

    public static class Item
    {
        public static Action<ItemData> DeliveryItem;
    }
}
