using System;
using System.Collections.Generic;
using UnityEngine;

// Trilho como uma curva amostrada por distancia:
// dado "d" (unidades percorridas desde o início), devolve a posição e a direção do trilho.
[ExecuteAlways]
[RequireComponent(typeof(EdgeCollider2D))]
public class RailPath : MonoBehaviour {
    [Tooltip("Gruda sozinho quando o Player cai em cima. Desmarque para trilhos no chão: o Player precisa apertar para cima.")]
    public bool autoGrab = true;

    [Tooltip("Suaviza a linha entre os pontos (Catmull-Rom). Desmarque para seguir os pontos em linha reta.")]
    public bool smooth = true;

    [Tooltip("Resolução da curva suavizada: amostras entre cada par de pontos.")]
    [Range(4, 32)] public int samplesPerSegment = 12;

    // Pontos de controle em espaço LOCAL. Editados pela Scene View, sem digitar valores.
    [HideInInspector] public List<Vector2> points = new List<Vector2> {
        new Vector2(-3f, 0f), new Vector2(0f, 1f), new Vector2(3f, 0f)
    };

    // Avisa quem depende do trilho (ex.: visual do cabo)
    public event Action Rebuilt;

    private Vector2[] _samples = Array.Empty<Vector2>();   // pontos da curva (espaço local)
    private float[] _cumulative = Array.Empty<float>();    // comprimento acumulado (mundo) em cada amostra

    public float Length => _cumulative.Length > 0 ? _cumulative[_cumulative.Length - 1] : 0f;
    public bool IsValid => _samples.Length >= 2 && Length > 0.001f;
    public IReadOnlyList<Vector2> LocalSamples => _samples;

    private void OnEnable() => Rebuild();
    private void OnValidate() => Rebuild();

    // Trocar os pontos por código (ex.: cabo entre duas âncoras, trilho gerado de Tilemap)
    public void SetPoints(List<Vector2> newPoints, bool useSmooth) {
        points = new List<Vector2>(newPoints);
        smooth = useSmooth;
        Rebuild();
    }

    // Se mudar a ESCALA do objeto, use o menu de contexto do componente para recalcular
    [ContextMenu("Rebuild")]
    public void Rebuild() {
        if (points == null || points.Count < 2) {
            _samples = Array.Empty<Vector2>();
            _cumulative = Array.Empty<float>();
            return;
        }

        // 1) Amostra a curva
        var raw = new List<Vector2>();
        if (smooth) {
            for (int i = 0; i < points.Count - 1; i++) {
                for (int s = 0; s < samplesPerSegment; s++) {
                    raw.Add(CatmullRom(i, s / (float)samplesPerSegment));
                }
            }
            raw.Add(points[points.Count - 1]);
        }
        else {
            raw.AddRange(points);
        }

        // 2) Remove pontos repetidos (segmento de tamanho zero quebra a tangente e a distância)
        var clean = new List<Vector2>(raw.Count);
        foreach (Vector2 p in raw) {
            if (clean.Count == 0 || (p - clean[clean.Count - 1]).sqrMagnitude > 1e-6f) {
                clean.Add(p);
            }
        }

        if (clean.Count < 2) {
            _samples = Array.Empty<Vector2>();
            _cumulative = Array.Empty<float>();
            return;
        }

        // 3) Tabela de comprimento acumulado (é ela que dá velocidade constante em curvas e retas)
        _samples = clean.ToArray();
        _cumulative = new float[_samples.Length];
        for (int i = 1; i < _samples.Length; i++) {
            float segment = transform.TransformVector(_samples[i] - _samples[i - 1]).magnitude;
            _cumulative[i] = _cumulative[i - 1] + segment;
        }

        // 4) Collider para detectar o trilho (trigger: o Player atravessa)
        var edge = GetComponent<EdgeCollider2D>();
        if (edge != null) {
            edge.isTrigger = true;
            edge.points = _samples;
        }

        Rebuilt?.Invoke();
    }

    // Catmull-Rom uniforme entre points[i] e points[i + 1]
    private Vector2 CatmullRom(int i, float t) {
        Vector2 p0 = points[Mathf.Max(i - 1, 0)];
        Vector2 p1 = points[i];
        Vector2 p2 = points[i + 1];
        Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // ------------------------------------------- API usada pelo Player -------------------------------------------

    // Posição no MUNDO a uma distância d do início do trilho
    public Vector2 GetPoint(float d) {
        if (!IsValid) return transform.position;

        GetSegment(d, out int i, out float f);
        return transform.TransformPoint(Vector2.Lerp(_samples[i], _samples[i + 1], f));
    }

    // Direção unitária (mundo) do trilho na distância d, no sentido início -> fim
    public Vector2 GetTangent(float d) {
        if (!IsValid) return Vector2.right;

        GetSegment(d, out int i, out _);
        return ((Vector2)transform.TransformVector(_samples[i + 1] - _samples[i])).normalized;
    }

    // Distância (0..Length) do ponto do trilho mais próximo de uma posição no mundo
    public float GetClosestDistance(Vector2 worldPos) {
        if (!IsValid) return 0f;

        Vector2 local = transform.InverseTransformPoint(worldPos);
        float best = float.MaxValue;
        float bestDistance = 0f;

        for (int i = 0; i < _samples.Length - 1; i++) {
            Vector2 a = _samples[i];
            Vector2 ab = _samples[i + 1] - a;

            float f = Mathf.Clamp01(Vector2.Dot(local - a, ab) / ab.sqrMagnitude);
            float sqr = (a + ab * f - local).sqrMagnitude;

            if (sqr < best) {
                best = sqr;
                bestDistance = _cumulative[i] + (_cumulative[i + 1] - _cumulative[i]) * f;
            }
        }
        return bestDistance;
    }

    // Acha o segmento que contém a distância d
    private void GetSegment(float d, out int index, out float fraction) {
        d = Mathf.Clamp(d, 0f, Length);

        int i = Array.BinarySearch(_cumulative, d);
        if (i < 0) i = ~i - 1;
        i = Mathf.Clamp(i, 0, _samples.Length - 2);

        float segmentLength = _cumulative[i + 1] - _cumulative[i];
        index = i;
        fraction = segmentLength > 1e-5f ? Mathf.Clamp01((d - _cumulative[i]) / segmentLength) : 0f;
    }

    // Mostra o trilho na Scene (e na Game com o botão Gizmos ligado)
    private void OnDrawGizmos() {
        if (!IsValid) return;

        Gizmos.color = autoGrab ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.7f, 0.2f);
        for (int i = 0; i < _samples.Length - 1; i++) {
            Gizmos.DrawLine(transform.TransformPoint(_samples[i]), transform.TransformPoint(_samples[i + 1]));
        }
    }
}