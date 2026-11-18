using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TaskData", menuName = "WitchOnWheels/QuestData/Task")]

public class TaskData : ScriptableObject
{
    public string Name;
    [TextArea] public string Description;
    public List<SubTaskData> SubTasks;
}
