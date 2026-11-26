using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// Como o trilho lança o Player ao chegar na ponta
public enum RailEndLaunch {
    None,            // sai com a velocidade que já tinha
    PlayerDefault,   // usa a "Força de lançamento" padrão do PlayerController (railEndLaunchForce)
    Custom           // usa a força definida neste trilho
}

// Trilho como uma curva amostrada por distancia:
// dado "d" (unidades percorridas desde o início), devolve a posição e a direção do trilho.
[ExecuteAlways]
[RequireComponent(typeof(EdgeCollider2D))]
public class RailPath : MonoBehaviour {
    [Tooltip("Gruda sozinho quando o Player cai em cima. Desmarque para trilhos no chão: o Player precisa apertar para cima.")]
    public bool autoGrab = true;

    [Tooltip("Lançamento ao chegar na ponta do trilho (em qualquer das pontas, no sentido do movimento). None = sai com a velocidade que tinha. PlayerDefault = usa a força padrão do Player. Custom = usa a força abaixo.")]
    public RailEndLaunch endLaunch = RailEndLaunch.None;

    [Tooltip("Velocidade (u/s) do lançamento deste trilho. Só vale com End Launch = Custom. Nunca reduz a velocidade que o Player já tinha: se ele chegar mais rápido, sai mais rápido.")]
    [Min(0f)] public float customEndLaunchForce = 20f;

    [Tooltip("Opcional: Sprite Shape Controller (shape ABERTO) que desenha este trilho. Quando preenchido, o trilho e o Edge Collider seguem a curva do Sprite Shape (editada com a ferramenta do próprio Sprite Shape) e os pontos amarelos do Rail Path deixam de ser usados. Desligue o 'Update Collider' do Sprite Shape Controller: quem atualiza o collider é o Rail Path.")]
    public SpriteShapeController spriteShape;

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

    private int _splineHash;

    private void OnEnable() => Rebuild();
    private void OnValidate() => Rebuild();

#if UNITY_EDITOR
    // Editando a curva do Sprite Shape na Scene, o trilho e o collider acompanham na hora
    private void Update() {
        if (Application.isPlaying || spriteShape == null) return;
        if (ComputeSplineHash() != _splineHash) Rebuild();
    }
#endif

    // Trocar os pontos por código (ex.: cabo entre duas âncoras, trilho gerado de Tilemap)
    public void SetPoints(List<Vector2> newPoints, bool useSmooth) {
        points = new List<Vector2>(newPoints);
        smooth = useSmooth;
        Rebuild();
    }

    // Se mudar a ESCALA do objeto, use o menu de contexto do componente para recalcular
    [ContextMenu("Rebuild")]
    public void Rebuild() {
        bool useShape = spriteShape != null && spriteShape.spline != null && spriteShape.spline.GetPointCount() >= 2;

        if (!useShape && (points == null || points.Count < 2)) {
            _samples = Array.Empty<Vector2>();
            _cumulative = Array.Empty<float>();
            return;
        }

        // 1) Amostra a curva
        var raw = new List<Vector2>();
        if (useShape) {
            SampleSpriteShape(raw);
            _splineHash = ComputeSplineHash();
        }
        else if (smooth) {
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

    // Amostra a curva Bézier do Sprite Shape (posições e tangentes dos pontos) e converte para o espaço local deste trilho
    private void SampleSpriteShape(List<Vector2> output) {
        Spline spline = spriteShape.spline;
        int count = spline.GetPointCount();
        int segments = spline.isOpenEnded ? count - 1 : count;

        for (int i = 0; i < segments; i++) {
            int j = (i + 1) % count;

            bool linearA = spline.GetTangentMode(i) == ShapeTangentMode.Linear;
            bool linearB = spline.GetTangentMode(j) == ShapeTangentMode.Linear;

            Vector3 p0 = spline.GetPosition(i);
            Vector3 p3 = spline.GetPosition(j);
            Vector3 p1 = p0 + (linearA ? Vector3.zero : spline.GetRightTangent(i));
            Vector3 p2 = p3 + (linearB ? Vector3.zero : spline.GetLeftTangent(j));

            int steps = linearA && linearB ? 1 : samplesPerSegment;
            for (int s = 0; s < steps; s++) {
                output.Add(ShapeToLocal(Bezier(p0, p1, p2, p3, s / (float)steps)));
            }

            if (i == segments - 1) output.Add(ShapeToLocal(p3));
        }
    }

    private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    private Vector2 ShapeToLocal(Vector3 shapePoint) {
        Vector3 world = spriteShape.transform.TransformPoint(shapePoint);
        return transform.InverseTransformPoint(world);
    }

    // Muda quando a curva (ou a posição do objeto) muda
    private int ComputeSplineHash() {
        if (spriteShape == null || spriteShape.spline == null) return 0;

        Spline spline = spriteShape.spline;
        int count = spline.GetPointCount();

        unchecked {
            int hash = count * 31 + (spline.isOpenEnded ? 1 : 0);
            for (int i = 0; i < count; i++) {
                hash = hash * 31 + spline.GetPosition(i).GetHashCode();
                hash = hash * 31 + spline.GetLeftTangent(i).GetHashCode();
                hash = hash * 31 + spline.GetRightTangent(i).GetHashCode();
                hash = hash * 31 + (int)spline.GetTangentMode(i);
            }
            hash = hash * 31 + spriteShape.transform.localToWorldMatrix.GetHashCode();
            hash = hash * 31 + transform.localToWorldMatrix.GetHashCode();
            return hash;
        }
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

    // ------------------------------------------- Ganchos para trilhos especiais (ex.: LoopRailPath) -------------------------------------------
    // O trilho comum não restringe nada: estes valores reproduzem o comportamento padrão.

    // Decide se o Player pode entrar neste trilho e em qual distância.
    // feet = pés, pivot = pivô do Player, velocity = velocidade atual, maxDistance = raio máximo de detecção.
    public virtual bool TryGetEntry(Vector2 feet, Vector2 pivot, Vector2 velocity, float maxDistance, out float distance) {
        distance = GetClosestDistance(feet);
        return true;
    }

    // true = o trilho obriga o sentido e o lado do Player ao entrar (dir: +1 rumo ao fim / -1 rumo ao início; normalSign: lado do trilho)
    public virtual bool GetForcedEntry(out int dir, out float normalSign) {
        dir = 0;
        normalSign = 0f;
        return false;
    }

    // true = o Player não pode inverter o sentido dentro deste trilho
    public virtual bool LockDirection => false;

    // false = o Player não pode trocar de trilho (cima/baixo) de dentro deste trilho
    public virtual bool AllowRailSwitch => true;

    protected virtual void DrawExtraGizmos() { }

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

        DrawExtraGizmos();
    }
}