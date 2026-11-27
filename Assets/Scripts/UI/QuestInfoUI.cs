using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI questDescription;
    [SerializeField] private TextMeshProUGUI questTask;
    [SerializeField] private Image questReward;


    public void Setup(GameQuest quest)
    {
        questName.text = quest.QuestName;
        questDescription.text = quest.Description;
        questTask.text = FormaterTaskText(quest.QuestTask, quest.CurrentTaskIndex);
        questReward.enabled = quest.IsComplete;
    }

    private string FormaterTaskText(List<Task> tasks, int currentQuestTaskIndex)
    {
        var indexTask= 0;
        var textoFinal = "";
        foreach(var task in tasks)
        {
            if(indexTask <= currentQuestTaskIndex)
            {
                Debug.Log(task.IsComplete);
                textoFinal += task.IsComplete ? "\u25A0 " : "\u2713 ";
                textoFinal += task.Name + "\n";
                foreach(var subTask in task.SubTasks)
                {
                    textoFinal += subTask.IsComplete ? "\t\u25CF " : "\t\u25CB ";
                    textoFinal += subTask.Name + "\n";
                }
            }
            else
            {
                textoFinal += "????????????\n";
            }
            indexTask++;
        }
        return textoFinal;
    }

    public void Reset()
    {
        questName.text = "";
        questDescription.text = "";
        questTask.text = "";
    }
}
