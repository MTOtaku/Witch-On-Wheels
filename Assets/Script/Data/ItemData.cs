using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "WitchOnWheels/ItemData")]
public class ItemData : DeliverableObjectData
{
    [TextArea] public string Description;
    public Sprite ItemUIImage;
    public Sprite ItemBodyImage;
}
