using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    [SerializeField] private DialogData dialogData;
    [SerializeField] private List<BubbleChatController> avatarsChatController = new List<BubbleChatController>();
    [SerializeField] private bool isShowingMessage;

    [SerializeField] private Action finishDialogCallback;
    public void SetupDialog(DialogData newDialog, List<BubbleChatController> avatarChats, Action finishCallback = null)
    {
        avatarsChatController.Clear();
        foreach (var chat in avatarChats)
        {
            avatarsChatController.Add(chat);
        }
        dialogData = newDialog;
        finishDialogCallback = finishCallback;
        StartCoroutine(StartDialog());
    }

    IEnumerator StartDialog()
    {
        var currentDialogIndex = 0;
        while(dialogData.LoadNextSequence(currentDialogIndex) != null)
        {
            var currentDialogData = dialogData.LoadNextSequence(currentDialogIndex);
            currentDialogIndex++;
            HideAllChats();
            BubbleChatController currentChat = null;
            currentChat = avatarsChatController.Find(x => x.Avatar.AvatarName == currentDialogData.Avatar.AvatarName);
            currentChat?.ShowChat(true);
            isShowingMessage = true;
            currentChat?.SetupBubbleDialog(currentDialogData, FinishWriteDialogSentence);
            yield return new WaitUntil(() => !isShowingMessage);
        }
        HideAllChats();
        ObjectPoolManager.ReturnObjectToPool(this.gameObject, PoolType.DialogSystem);
        finishDialogCallback?.Invoke();
    }

    private void FinishWriteDialogSentence()
    {
        isShowingMessage = false;
    }

    private void HideAllChats()
    {
        foreach(var chat in avatarsChatController)
        {
            chat.ClearChat();
            chat.ShowChat(false);
        }
    }
}
