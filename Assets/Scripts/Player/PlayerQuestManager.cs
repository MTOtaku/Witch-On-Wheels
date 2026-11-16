using UnityEngine;

public class PlayerQuestManager : MonoBehaviour
{


    private void OnEnable()
    {
        GameEvents.Quest.DeliveryQuest += ReceiveQuest;
    }

    private void OnDisable()
    {
        GameEvents.Quest.DeliveryQuest -= ReceiveQuest;
    }

    private void ReceiveQuest(QuestData data)
    {
        Debug.Log("Player recebeu a quest: " + data.Name);
    }
}
