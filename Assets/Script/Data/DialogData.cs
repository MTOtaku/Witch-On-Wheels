using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialog", menuName = "WitchOnWheels/Dialogo")]
public class DialogData : ScriptableObject
{
    public List<DialogPart> SequenceDialog;

    public DialogPart LoadNextSequence(int countSequence)
    {
        DialogPart dialog = null;
        if(countSequence < SequenceDialog.Count)
        {
            dialog = SequenceDialog[countSequence];
        }
            return dialog;
    }
}