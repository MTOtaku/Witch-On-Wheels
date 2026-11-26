using UnityEngine;
using System;
using System.Collections.Generic;

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

    public static class UI
    {
        public static Action<List<ItemSlot>> UpdateAvatarItemUI;

        public static Action<TypeBook, List<GameQuest>> SetBookVisibility;
        public static Action<List<GameQuest>> UpdateQuestUI;
    }
}
