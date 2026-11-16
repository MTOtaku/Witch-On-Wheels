using UnityEngine;

[CreateAssetMenu(fileName = "SubTaskData", menuName = "WitchOnWheels/QuestData/SubTask")]
public class SubTaskData : ScriptableObject
{
    public string Name;
    public ObjectiveTypeEnum ObjectiveType;
    public ScriptableObject Objective;
    public int count;

    public T GetObjective<T>() where T : ScriptableObject
    {
        if (Objective is T objective)
            return objective;

        Debug.LogWarning($"Objective não é do tipo {typeof(T).Name}");

        return null;
    }
}
