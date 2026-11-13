using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [SerializeField] private AvatarData avatarData;
    [SerializeField] private DialogData defaultNPCDialog;
    [SerializeField] private DialogController dialogController;
    [SerializeField] private BubbleChatController chatController;
    private bool canInteract = true;

    void Start()
    {
        chatController.ShowChat(false);
    }
    public void Interact(PlayerInteractable playerInteractable)
    {
        if(!canInteract)
            return;
        ShowDefaultDialog(playerInteractable.ChatController);
    }

    private void SetCanInteract()
    {
        canInteract = !canInteract;
    }
    private void ShowDefaultDialog(BubbleChatController playerChat)
    {
        var controller = ObjectPoolManager.SpawnObject<DialogController>(dialogController, transform.position, transform.rotation, PoolType.DialogSystem);
        List<BubbleChatController> allDialogChats = new List<BubbleChatController>(){playerChat, chatController};
        SetCanInteract();
        controller.SetupDialog(defaultNPCDialog, allDialogChats, SetCanInteract);
    }
}
