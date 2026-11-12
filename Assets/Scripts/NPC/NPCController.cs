using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [SerializeField] private AvatarData avatarData;
    [SerializeField] private DialogData defaultNPCDialog;
    [SerializeField] private DialogController dialogController;
    [SerializeField] private BubbleChatController chatController;

    void Start()
    {
        chatController.ShowChat(false);
    }
    public void Interact(PlayerInteractable playerInteractable)
    {
        ShowDefaultDialog(playerInteractable.ChatController);
    }

    private void ShowDefaultDialog(BubbleChatController playerChat)
    {
        var controller = ObjectPoolManager.SpawnObject<DialogController>(dialogController, transform.position, transform.rotation, PoolType.DialogSystem);
        List<BubbleChatController> allDialogChats = new List<BubbleChatController>(){playerChat, chatController};
        controller.SetupDialog(defaultNPCDialog, allDialogChats);
    }
}
