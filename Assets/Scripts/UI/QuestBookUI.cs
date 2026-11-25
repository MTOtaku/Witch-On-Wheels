using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestBookUI : MonoBehaviour
{

    [SerializeField] private List<GameQuest> gameQuest;
    void OnEnable()
    {
        GameEvents.UI.UpdateQuestUI += UpdateQuestUI;
    }

    void OnDisable()
    {
        GameEvents.UI.UpdateQuestUI -= UpdateQuestUI;

    }

    private void UpdateQuestUI(List<GameQuest> quests)
    {
        gameQuest.Clear();
        foreach(var quest in quests)
        {
            gameQuest.Add(quest);
        }
    }
}
