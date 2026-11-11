using UnityEngine;

public class PlayerInteractable : MonoBehaviour
{
    [SerializeField] private BubbleChatController chatController;
    public BubbleChatController ChatController => chatController;
    private AvatarData data;
    public AvatarData Data => data;
    private IInteractable interactObject;
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactableObject)){
            interactObject = interactableObject;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactableObject)){
            if(interactObject == interactableObject)
            {
                interactObject = null;
            }
        }
    }

    public bool TryExecutInteraction(AvatarData playerData)
    {
        data = playerData;
        interactObject?.Interact(this);
        return interactObject != null;
    }
}
