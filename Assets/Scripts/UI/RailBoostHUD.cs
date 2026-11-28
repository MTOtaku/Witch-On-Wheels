using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class RailBoostHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [Tooltip("Sprite shown when the boost is NOT available (empty / no energy). Sits behind the fill image.")]
    [SerializeField] private Image emptyImage;
    [Tooltip("Sprite shown when the boost is available (full energy). Fills from bottom to top.")]
    [SerializeField] private Image fillImage;

    [Header("Visibility")]
    [Tooltip("Seconds to wait after landing before the icon fades out.")]
    [SerializeField] private float hideDelay = 2f;
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float fadeOutTime = 0.3f;

    [Header("Ready pulse")]
    [Tooltip("Peak scale of the pulse when the boost is refilled (1.25 = 25% bigger).")]
    [SerializeField] private float pulseScale = 1.25f;
    [Tooltip("Total duration of the pulse in seconds (grow and return).")]
    [SerializeField] private float pulseDuration = 0.25f;

    private CanvasGroup group;
    private float hideTimer;
    private bool wasReady = true;
    private float pulseTimer = -1f;
    private Vector3 baseScale;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        baseScale = transform.localScale;

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImage.fillAmount = 1f;
        }
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null) return;

        UpdateVisibility();
        UpdateFill();
        UpdatePulse();
    }

    private void UpdateVisibility()
    {
        float dt = Time.unscaledDeltaTime;

        if (player.IsGrinding)
            hideTimer = hideDelay;      // on a rail: show, cancel any pending hide
        else if (player.isGrounded)
            hideTimer -= dt;            // landed: count down to hiding
        // In the air after leaving a rail: timer is held, icon stays visible

        float targetAlpha = hideTimer > 0f ? 1f : 0f;
        float fadeTime = targetAlpha > group.alpha ? fadeInTime : fadeOutTime;
        float step = fadeTime > 0f ? dt / fadeTime : 1f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
    }

    private void UpdateFill()
    {
        if (fillImage == null) return;

        float fill;
        if (player.RailBoostReady)
        {
            fill = 1f;
        }
        else
        {
            // One continuous refill spanning boost duration + cooldown
            float total = player.railBoostDuration + player.railBoostCooldown;
            float timeLeft = player.RailBoostActive
                ? player.RailBoostTimeLeft + player.railBoostCooldown
                : player.RailBoostCooldownLeft;

            fill = total > 0f ? 1f - Mathf.Clamp01(timeLeft / total) : 1f;
        }

        fillImage.fillAmount = fill;
    }

    private void UpdatePulse()
    {
        bool ready = player.RailBoostReady;

        if (ready && !wasReady && group.alpha > 0.5f)
            pulseTimer = 0f;
        wasReady = ready;

        if (pulseTimer < 0f) return;

        pulseTimer += Time.unscaledDeltaTime;
        float t = pulseDuration > 0f ? pulseTimer / pulseDuration : 1f;

        if (t >= 1f)
        {
            pulseTimer = -1f;
            transform.localScale = baseScale;
        }
        else
        {
            float k = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
            transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, k);
        }
    }
}
