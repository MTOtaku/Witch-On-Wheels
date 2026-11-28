using Unity.Cinemachine;
using UnityEngine;

public class RailCameraZoom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private PlayerController player;

    [Header("Zoom out amounts (added to the original lens size)")]
    [SerializeField] private float grindZoomOut = 2f;
    [SerializeField] private float boostZoomOut = 4f;

    [Header("Zoom speed (seconds to reach target, lower = faster)")]
    [SerializeField] private float zoomOutTime = 0.4f;
    [SerializeField] private float zoomInTime = 0.6f;

    [Header("Behaviour")]
    [Tooltip("Seconds the player must be grounded before zooming back in (avoids flicker).")]
    [SerializeField] private float groundedConfirmTime = 0.05f;

    private float originalSize;
    private float sizeVelocity;
    private bool zoomSession;     // from grind start until landing
    private float groundedTimer;

    private void Start()
    {
        if (cmCamera == null) cmCamera = GetComponent<CinemachineCamera>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        originalSize = cmCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (player == null) return;

        if (player.IsGrinding)
            zoomSession = true;

        // Landing: grounded and not on a rail, held for a short moment
        if (zoomSession)
        {
            bool landed = player.isGrounded && !player.IsGrinding;
            groundedTimer = landed ? groundedTimer + Time.deltaTime : 0f;

            if (groundedTimer >= groundedConfirmTime)
            {
                zoomSession = false;
                groundedTimer = 0f;
            }
        }

        float target = originalSize;
        if (zoomSession)
            target += player.RailBoostActive ? boostZoomOut : grindZoomOut;

        var lens = cmCamera.Lens;
        float smoothTime = target > lens.OrthographicSize ? zoomOutTime : zoomInTime;
        float newSize = Mathf.SmoothDamp(lens.OrthographicSize, target, ref sizeVelocity, smoothTime);

        if (Mathf.Abs(newSize - target) < 0.01f)
        {
            newSize = target;
            sizeVelocity = 0f;
        }

        lens.OrthographicSize = newSize;
        cmCamera.Lens = lens;
    }
}
