using System.Collections.Generic;
using UnityEngine;

public class QuestStateController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer rendererState;
    [SerializeField] private List<Sprite> states;
    public void UpdateState(NPCStateEnum state)
    {
        switch(state)
        {
            case NPCStateEnum.Default:
            rendererState.gameObject.SetActive(false);
            rendererState.sprite = null;
            break;
            case NPCStateEnum.HasQuestToGive:
                rendererState.gameObject.SetActive(true);
                rendererState.sprite = states[0];
            break;
            case NPCStateEnum.WaitingToFinishQuest:
                rendererState.gameObject.SetActive(true);
                rendererState.sprite = states[1];
            break;
            case NPCStateEnum.QuestCompleted:
                rendererState.gameObject.SetActive(true);
                rendererState.sprite = states[2];
            break;
        }
    }
}
