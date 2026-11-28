using UnityEngine;

public class Cloud : MonoBehaviour
{
    public SpriteRenderer Renderer { get; private set; }

    private Transform cam;
    private Vector3 lastCamPos;
    private float parallaxX, parallaxY, driftSpeed;

    private void Awake() => Renderer = GetComponent<SpriteRenderer>();

    public void Init(Transform camTransform, float px, float py, float drift)
    {
        cam = camTransform;
        parallaxX = px;
        parallaxY = py;
        driftSpeed = drift;
        lastCamPos = cam.position; // reset so reused clouds don't jump
    }

    private void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(
            delta.x * parallaxX + driftSpeed * Time.deltaTime,
            delta.y * parallaxY,
            0f);
        lastCamPos = cam.position;
    }
}