using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuestBookUI : MonoBehaviour
{
    [SerializeField] private List<GameQuest> gameQuest;
    [SerializeField] private List<GenericButton> gameQuestButtons;
    [SerializeField] private GenericButton slotUI;
    [SerializeField] private GameObject questMainSlotSide;
    [SerializeField] private GameObject questInfoSide;
    [SerializeField] private QuestInfoUI questInfoUI;

    void OnEnable()
    {
    }

    void OnDisable()
    {
    }

    public void DesactiveAllQuestUI()
    {
        foreach(var button in gameQuestButtons)
        {
            ObjectPoolManager.ReturnObjectToPool(button.gameObject, PoolType.QuestSlotUI);
        }
        gameQuestButtons.Clear();
        ResetQuestDetail();
    }

    public void ResetQuestUI()
    {
        foreach(var button in gameQuestButtons)
        {
            ObjectPoolManager.ReturnObjectToPool(button.gameObject, PoolType.QuestSlotUI);
        }
        gameQuestButtons.Clear();
    }
    public void CreateQuestUI(List<GameQuest> quests)
    {
        gameQuest.Clear();
        gameQuestButtons.Clear();
        foreach(var quest in quests)
        {
            gameQuest.Add(quest);
            var questSlotUI = ObjectPoolManager.SpawnObject<GenericButton>(slotUI,questMainSlotSide.transform.position, questMainSlotSide.transform.rotation, PoolType.QuestSlotUI);
            questSlotUI.SetupText(quest.QuestName);
            gameQuestButtons.Add(questSlotUI);
            questSlotUI.gameObject.transform.SetParent(questMainSlotSide.transform);
            questSlotUI.Initialize(ShowQuestDetail);
        }
    }

    private void ShowQuestDetail(int index, GenericButton button)
    {
        var buttonIndex = button.gameObject.transform.GetSiblingIndex();
        var quest = gameQuest[buttonIndex];
        questInfoSide.SetActive(true);
        questInfoUI.Setup(quest);
    }

    private void ResetQuestDetail()
    {
        questInfoSide.SetActive(false);
        questInfoUI.Reset();
    }

    private void UpdateQuestUI(List<GameQuest> quests)
    {
        //gameQuest.Clear();
        //foreach(var quest in quests)
        //{
            //gameQuest.Add(quest);
            //var questSlotUI = ObjectPoolManager.SpawnObject<GenericButton>(slotUI,questMainSlotSide.transform.position, questMainSlotSide.transform.rotation, PoolType.QuestSlotUI);
            //questSlotUI.SetupText(quest.QuestName);
        //}
    }
}
