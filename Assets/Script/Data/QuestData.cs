using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "WitchOnWheels/QuestData/Quest")]
public class QuestData : DeliverableObjectData
{
    [TextArea] public string Description;
    public List<TaskData> Tasks;

}
