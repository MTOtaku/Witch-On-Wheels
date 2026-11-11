using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BubbleChatController : MonoBehaviour
{
    [SerializeField] private AvatarData avatar;
    public AvatarData Avatar => avatar;
    [SerializeField] private GameObject body;
    [SerializeField] private TextMeshPro bubbleDialogue;
    private Action dialogFinishCallBack;

    public void ShowChat(bool isShow)
    {
        body.SetActive(isShow);
    }

    public void ClearChat()
    {
        bubbleDialogue.text = "";
    }
    public void SetupBubbleDialog(DialogPart dialog, Action callback)
    {
        dialogFinishCallBack = callback;
        StartCoroutine(WriteDialog(dialog));
    }

    IEnumerator WriteDialog(DialogPart dialog)
    {
        int count = 0;

        if (dialog.IsNeedClearDialog)
        {
            yield return new WaitForSeconds(0.5f);
            bubbleDialogue.text = "";
        }

        while (count < dialog.Sentence.Length)
        {
            bubbleDialogue.text += dialog.Sentence[count];
            yield return new WaitForSeconds(dialog.CharacterDisplayDelay);
            count++;
        }
        dialogFinishCallBack?.Invoke();
    }
}
