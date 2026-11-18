using UnityEngine;
using System;

public static class GameEvents
{
    public static class Quest
    {
        public static Action<QuestData> DeliveryQuest;
        public static Action<ItemData> PlayerObtainItem;
        public static Action<AvatarData> PlayerSpeakWithNPC;
    }

    public static class Item
    {
        public static Action<ItemData> DeliveryItem;
    }

    public static class Dialog
    {
        public static Action<AvatarData> AvatarUpdateDialogStatusToFinishQuest;
    }
}
