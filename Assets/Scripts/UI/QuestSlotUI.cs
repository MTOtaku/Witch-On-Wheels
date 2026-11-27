using UnityEngine;

public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private GameQuest slotQuest;
    public void SetupQuestSlot(GameQuest quest)
    {
        slotQuest = quest; 
    }
}
