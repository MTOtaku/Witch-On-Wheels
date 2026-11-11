using UnityEngine;

[System.Serializable]
public class DialogPart
{
    [Tooltip("Antes de mostrar a próxima sequencia preciso limpar o dialogo?")]
    public bool IsNeedClearDialog;

    [Tooltip("Velocidade para aparecer cada caractere da sentença do dialogo")]
    [Range(0.05f, 1f)]
    public float CharacterDisplayDelay;

    [Tooltip("Sentença a ser mostrada")]
    public string Sentence;

    public AvatarData Avatar;
}