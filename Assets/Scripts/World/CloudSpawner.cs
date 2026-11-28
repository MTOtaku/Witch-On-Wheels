using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CloudBand
    {
        public string name = "Far";
        [Range(-0.5f, 1f)] public float parallaxX = 0.9f;
        [Range(-0.5f, 1f)] public float parallaxY = 0.85f;

        [Header("Look")]
        public Vector2 widthScaleRange = new Vector2(0.4f, 0.7f);
        public Vector2 heightScaleRange = new Vector2(0.35f, 0.6f);
        public Vector2 alphaRange = new Vector2(0.3f, 0.6f);
        public Color tint = Color.white;
        public int sortingOrder = -60;

        [Header("Motion")]
        public Vector2 driftSpeedRange = new Vector2(0.05f, 0.15f);
        public Vector2 yOffsetRange = new Vector2(-6f, 8f); // relative to camera Y

        [Header("Density")]
        public int maxCount = 8;
        public AnimationCurve density = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [HideInInspector] public Queue<Cloud> pool = new Queue<Cloud>();
        [HideInInspector] public List<Cloud> active = new List<Cloud>();
    }

    [SerializeField] private Sprite cloudSprite;
    [SerializeField] private string sortingLayerName = "Background";
    [SerializeField] private CloudBand[] bands;

    [Header("Altitude range (world Y of camera)")]
    [SerializeField] private float minAltitude = -110f;
    [SerializeField] private float maxAltitude = 125f;

    [Header("Spawning")]
    [Tooltip("How far outside the view new clouds can appear")]
    [SerializeField] private float spawnMargin = 6f;
    [SerializeField] private bool driftRight = true;

    private Camera cam;
    private Transform camT;
    private Vector3 lastCamPos;
    private Vector2 camVel; // smoothed camera velocity (units/sec)

    private void Start()
    {
        cam = Camera.main;
        camT = cam.transform;
        lastCamPos = camT.position;

        foreach (var band in bands)
            for (int i = 0; i < band.maxCount; i++)
                band.pool.Enqueue(CreateCloud(band));

        float alt = Altitude01();
        foreach (var band in bands)
        {
            int target = TargetCount(band, alt);
            for (int i = 0; i < target && band.pool.Count > 0; i++)
                Spawn(band, prewarm: true);
        }
    }

    private void Update()
    {
        Vector3 camPos = camT.position;

        // Estimate camera velocity so we know which side clouds will enter from
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector2 rawVel = (camPos - lastCamPos) / dt;
        camVel = Vector2.Lerp(camVel, rawVel, 1f - Mathf.Exp(-8f * dt));
        lastCamPos = camPos;

        float alt = Altitude01();
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        foreach (var band in bands)
        {
            // Recycle clouds that left the view (sideways or vertically)
            for (int i = band.active.Count - 1; i >= 0; i--)
            {
                Cloud c = band.active[i];
                Bounds b = c.Renderer.bounds;
                float dx = Mathf.Abs(b.center.x - camPos.x);
                float dy = Mathf.Abs(b.center.y - camPos.y);

                bool outX = dx > halfW + b.extents.x + spawnMargin * 2f;
                bool outY = dy > halfH + b.extents.y + spawnMargin * 2f;

                if (outX || outY)
                {
                    c.gameObject.SetActive(false);
                    band.pool.Enqueue(c);
                    band.active.RemoveAt(i);
                }
            }

            int target = TargetCount(band, alt);
            while (band.active.Count < target && band.pool.Count > 0)
                Spawn(band, prewarm: false);
        }
    }

    private float Altitude01() =>
        Mathf.InverseLerp(minAltitude, maxAltitude, camT.position.y);

    private int TargetCount(CloudBand band, float alt01) =>
        Mathf.RoundToInt(band.maxCount * Mathf.Clamp01(band.density.Evaluate(alt01)));

    private Cloud CreateCloud(CloudBand band)
    {
        var go = new GameObject($"Cloud_{band.name}");
        go.transform.SetParent(transform);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cloudSprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = band.sortingOrder;

        var cloud = go.AddComponent<Cloud>();
        go.SetActive(false);
        return cloud;
    }

    private void Spawn(CloudBand band, bool prewarm)
    {
        Cloud cloud = band.pool.Dequeue();
        var go = cloud.gameObject;
        var sr = cloud.Renderer;

        // Randomize look
        sr.flipX = Random.value > 0.5f;
        Color col = band.tint;
        col.a = Random.Range(band.alphaRange.x, band.alphaRange.y);
        sr.color = col;

        Vector3 scale = new Vector3(
            Random.Range(band.widthScaleRange.x, band.widthScaleRange.y),
            Random.Range(band.heightScaleRange.x, band.heightScaleRange.y),
            1f);
        go.transform.localScale = scale;

        Vector3 camPos = camT.position;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float halfWidth = sr.sprite.bounds.extents.x * scale.x;
        float height = sr.sprite.bounds.size.y * scale.y; // pivot is at the bottom

        float drift = (driftRight ? 1f : -1f) * Random.Range(band.driftSpeedRange.x, band.driftSpeedRange.y);

        float x, y;

        if (prewarm)
        {
            x = camPos.x + Random.Range(-halfW, halfW);
            y = camPos.y + Random.Range(band.yOffsetRange.x, band.yOffsetRange.y);
        }
        else
        {
            // Motion of this cloud relative to the camera, in units/sec
            float relX = (band.parallaxX - 1f) * camVel.x + drift;
            float relY = (band.parallaxY - 1f) * camVel.y;

            // Choose the entry edge, weighted by how much cloud "flow" crosses each edge
            float fluxH = Mathf.Abs(relX) * 2f * halfH;
            float fluxV = Mathf.Abs(relY) * 2f * halfW;
            float total = fluxH + fluxV;
            bool horizontalEntry = total < 0.0001f || Random.value * total < fluxH;

            if (horizontalEntry)
            {
                // Moving right relative to camera -> enters from the left, and vice versa
                float side = relX >= 0f ? -1f : 1f;
                x = camPos.x + side * (halfW + halfWidth + Random.Range(0f, spawnMargin));
                y = camPos.y + Random.Range(band.yOffsetRange.x, band.yOffsetRange.y);
            }
            else
            {
                x = camPos.x + Random.Range(-halfW - halfWidth, halfW + halfWidth);
                if (relY >= 0f)
                    // Moving up relative to camera -> enters from below
                    y = camPos.y - halfH - height - Random.Range(0f, spawnMargin);
                else
                    // Moving down relative to camera -> enters from above
                    y = camPos.y + halfH + Random.Range(0f, spawnMargin);
            }
        }

        go.transform.position = new Vector3(x, y, 0f);
        go.SetActive(true);

        cloud.Init(camT, band.parallaxX, band.parallaxY, drift);
        band.active.Add(cloud);
    }
}
