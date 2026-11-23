using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [Header("Avatar information")]
    [SerializeField] private NPCStateEnum npcState;
    [SerializeField] private AvatarData avatarData;

    [Header("Avatar Objects")]
    [SerializeField] private SpriteRenderer body;

    [Header("Avatar Dialog Objects")]
    [SerializeField] private DialogController dialogController;
    [SerializeField] private BubbleChatController chatController;

    [Header("Quest State Objects")]
    [SerializeField] private QuestStateController questStatus;
    [Header("Dialogs")]
    [SerializeField] private DialogData defaultNPCDialog;
    [SerializeField] private DialogData hasQuestToGiveDialog;
    [SerializeField] private DialogData waitingToFinishQuestDialog;
    [SerializeField] private DialogData questCompletedDialog;
    [Header("Quest Possui Pré Requisito")]
    [SerializeField] private QuestData preRequisiteQuestToShowQuest;

    private bool canInteract = true;

    void Start()
    {
        body.sprite = avatarData.AvatarSprite;
        chatController.ShowChat(false);
        if(hasQuestToGiveDialog == null || preRequisiteQuestToShowQuest != null)
            UpdateNPCState(NPCStateEnum.Default);        
        else
            UpdateNPCState(NPCStateEnum.HasQuestToGive);
    }

    void OnEnable()
    {
        GameEvents.Dialog.AvatarUpdateDialogStatusToFinishQuest += UpdateDialogReceiveQuest;
    }

    void OnDisable()
    {
        GameEvents.Dialog.AvatarUpdateDialogStatusToFinishQuest -= UpdateDialogReceiveQuest;

    }

    public void Interact(PlayerInteractable playerInteractable)
    {
        if(!canInteract)
            return;
        ShowDialog(playerInteractable.ChatController);
    }

    private void FinishDialog()
    {
        var newStateAfterDialog = npcState;
        if(npcState == NPCStateEnum.HasQuestToGive)
            newStateAfterDialog = NPCStateEnum.WaitingToFinishQuest;

        if(npcState == NPCStateEnum.QuestCompleted)
            newStateAfterDialog = NPCStateEnum.Default;
        
        UpdateNPCState(newStateAfterDialog);

        GameEvents.Quest.PlayerSpeakWithNPC?.Invoke(avatarData);
        SetCanInteract();
    }
    private void SetCanInteract()
    {
        canInteract = !canInteract;
    }
    private void ShowDialog(BubbleChatController playerChat)
    {
        var controller = ObjectPoolManager.SpawnObject<DialogController>(dialogController, transform.position, transform.rotation, PoolType.DialogSystem);
        List<BubbleChatController> allDialogChats = new List<BubbleChatController>(){playerChat, chatController};
        SetCanInteract();
        var dialog = ReturnCorrectDialog();
        controller.SetupDialog(dialog, allDialogChats, FinishDialog);
    }

    private DialogData ReturnCorrectDialog()
    {
        switch(npcState)
        {
            case NPCStateEnum.Default:
                return defaultNPCDialog;
            case NPCStateEnum.HasQuestToGive:
                return hasQuestToGiveDialog;
            case NPCStateEnum.WaitingToFinishQuest:
                return waitingToFinishQuestDialog;
            case NPCStateEnum.QuestCompleted:
                return questCompletedDialog;
        }
        Debug.LogWarning("Nao to retornando nenhum dialogo");
        return null;
    }

    private void UpdateDialogReceiveQuest(AvatarData data)
    {
        if(avatarData != data)
            return;

        UpdateNPCState(NPCStateEnum.QuestCompleted);
    }

    private void UpdateNPCState(NPCStateEnum newState)
    {
        npcState = newState;
        questStatus.UpdateState(npcState);
    }
}
