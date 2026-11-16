using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "WitchOnWheels/QuestData/Quest")]
public class QuestData : DeliverableObjectData
{
    public List<TaskData> Tasks;
}
