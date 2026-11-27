using UnityEngine;
using UnityEngine.EventSystems;



public class ButtonHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler

/*
Feedback Visual para Hover/Seleção: muda os sprites e escala o botão.
Vive no GenericButton, não interfere com outros códigos.
Para adicionar mais efeitos(cor, som, rotação...), colocar dentrode ApplyInstant() e Update()
*/

//comentário de teste de git
{
    [Header("Mudar Sprite")]
    [Tooltip("O SpriteRenderer que mostra o ícone")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [Tooltip("A sprite sem estar hovering ou selecionada")]
    [SerializeField] private Sprite normalSprite;
    [Tooltip("A sprite durante o hovering")]
    [SerializeField] private Sprite hoverSprite;

    [Header("Escala")]
    [Tooltip("O que é para ser escalonado")]
    [SerializeField] private Transform scaleTarget;
    [SerializeField] private float hoverScaleMultiplier = 1.05f;
    [SerializeField] private float scaleSpeed = 12f;

    private Vector3 baseScale;
    private bool hovered;
    private bool selected;

    private bool IsHighlighted => hovered || selected;

    private void Awake()
    {
        if (scaleTarget == null) scaleTarget = transform;
        baseScale = scaleTarget.localScale;

        // Assegura que se faltar algo, a sprite não sumirá
        if (iconRenderer != null && normalSprite == null) normalSprite = iconRenderer.sprite;
        if (hoverSprite == null) hoverSprite = normalSprite;
    }

    private void OnEnable() => ApplyInstant();

    private void OnDisable()
    {
        // Pointer-exit doesn't fire on a disabled object, so reset manually
        hovered = false;
        selected = false;
        ApplyInstant();
    }

    private void Update()
    {
        // Smooth scale (unscaled time so it also works if the game is paused)
        Vector3 target = IsHighlighted ? baseScale * hoverScaleMultiplier : baseScale;
        scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, target, scaleSpeed * Time.unscaledDeltaTime);
    }

    // Mouse
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; RefreshSprite(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; RefreshSprite(); }

    // EventSystem selection
    public void OnSelect(BaseEventData eventData) => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);

    // Can be called by any keyboard/gamepad navigation script
    public void SetSelected(bool value) { selected = value; RefreshSprite(); }

    private void RefreshSprite()
    {
        if (iconRenderer == null) return;
        iconRenderer.sprite = IsHighlighted ? hoverSprite : normalSprite;
    }

    private void ApplyInstant()
    {
        RefreshSprite();
        if (scaleTarget != null) scaleTarget.localScale = IsHighlighted ? baseScale * hoverScaleMultiplier : baseScale;
    }
}
