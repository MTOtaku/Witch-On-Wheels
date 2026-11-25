using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerQuestManager : MonoBehaviour
{
    [SerializeField] private List<GameQuest> playerQuests; 

    private void OnEnable()
    {
        GameEvents.Quest.DeliveryQuest += ReceiveQuest;
        GameEvents.Quest.PlayerObtainItem += PlayerObtainedItem;
        GameEvents.Quest.PlayerSpeakWithNPC += PlayerSpeakedWithNPC;
    }

    private void OnDisable()
    {
        GameEvents.Quest.DeliveryQuest -= ReceiveQuest;
        GameEvents.Quest.PlayerObtainItem -= PlayerObtainedItem;
        GameEvents.Quest.PlayerSpeakWithNPC -= PlayerSpeakedWithNPC;
    }

    private void ReceiveQuest(QuestData data)
    {
        Debug.Log("Recebi uma quest");
        GameQuest newQuest = new GameQuest();
        newQuest.Initialize(data);
        playerQuests.Add(newQuest);
        GameEvents.UI.UpdateQuestUI?.Invoke(playerQuests);
    }

    private void PlayerObtainedItem(ItemData data)
    {
        foreach(var quest in playerQuests)
        {
            if (!quest.IsComplete)
            {
                quest.CheckQuest(data);
            }
        }
        GameEvents.UI.UpdateQuestUI?.Invoke(playerQuests);

    }

    private void PlayerSpeakedWithNPC(AvatarData data)
    {
        foreach(var quest in playerQuests)
        {
            if (!quest.IsComplete)
            {
                quest.CheckQuest(data);
            }
        }
                GameEvents.UI.UpdateQuestUI?.Invoke(playerQuests);
    }
}

[System.Serializable]
public class GameQuest
{
    public string QuestName;
    public List<Task> QuestTask = new List<Task>();
    public int CurrentTaskIndex;
    public bool IsComplete;

    public void Initialize(QuestData data)
    {
        QuestName = data.Name;
        CurrentTaskIndex = 0;
        foreach (var taskData in data.Tasks)
        {
            Task task = new Task();
            task.Initialize(taskData);
            QuestTask.Add(task);
        }
        IsComplete = false;
    }

    public void CheckQuest(ScriptableObject objective)
    {
        var currentTask = QuestTask[CurrentTaskIndex];
        var wasComplete = currentTask.CheckTask(objective);
        Debug.Log("Testando na task corrente: " + currentTask.Name);
        if (wasComplete)
        {
            CurrentTaskIndex++;
            if(CurrentTaskIndex < QuestTask.Count)
            {
                var newTask = QuestTask[CurrentTaskIndex];
                newTask.CheckQuestDeliveryItemToAvatar(); 
            }
        }

        if(CurrentTaskIndex == QuestTask.Count)
        {
            IsComplete = true;
        }
    }
}

[System.Serializable]
public class Task
{
    public string Name;
    public string Description;
    public List<SubTask> SubTasks = new List<SubTask>();
    public bool IsComplete;

    public void Initialize(TaskData data)
    {        
        foreach (var subTaskData in data.SubTasks)
        {
            SubTask subTask = new SubTask();
            subTask.Initialize(subTaskData);
            SubTasks.Add(subTask);
        }
        Name = data.Name;
        Description = data.Description;
    }

    public bool CheckTask(ScriptableObject objective)
    {
        bool isAllCompleted = true;

        foreach(var subtask in SubTasks)
        {
            if (!subtask.IsComplete)
            {
                var wasComplete = subtask.CheckObjective(objective);
                if (!wasComplete)
                {
                    isAllCompleted = false;
                }
            }
        }
        
        IsComplete = isAllCompleted;
        return IsComplete;
    }

    public void CheckQuestDeliveryItemToAvatar()
    {
        foreach(var subtask in SubTasks)
        {
            subtask.CheckAvatarUpdateDialog();
        }
    }
}

[System.Serializable]
public class SubTask
{
    public string Name;
    public ObjectiveTypeEnum ObjectiveType;
    public ScriptableObject Objective;
    public int count;
    public bool IsComplete;

    public void Initialize(SubTaskData data)
    {
        Name = data.name;
        ObjectiveType = data.ObjectiveType;
        Objective = data.Objective;
        count = data.count;
    }

    public bool CheckObjective(ScriptableObject objective)
    {
        if(Objective == objective)
        {
            IsComplete = true;
        }
        return IsComplete;
    }

    public void CheckAvatarUpdateDialog()
    {
        if(ObjectiveType == ObjectiveTypeEnum.Avatar)
        {
            GameEvents.Dialog.AvatarUpdateDialogStatusToFinishQuest?.Invoke((AvatarData) Objective);
        }
    }
}
