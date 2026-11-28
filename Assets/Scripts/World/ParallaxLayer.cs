using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorX = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactorY = 0.5f;

    private Transform cam;
    private Vector3 lastCamPos;

    private void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactorX,
                                          delta.y * parallaxFactorY,
                                          0f);
        lastCamPos = cam.position;
    }
}
