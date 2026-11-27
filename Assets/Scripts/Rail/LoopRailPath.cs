using UnityEngine;

public enum LoopEntrance {
    Start,   // o Player entra pelo INÍCIO do trilho (ponto 0) e anda rumo ao fim
    End      // o Player entra pelo FIM do trilho e anda rumo ao início
}

public enum LoopSide {
    Left,    // o Player fica à ESQUERDA de quem anda no sentido do percurso (loop anti-horário: por dentro)
    Right    // o Player fica à DIREITA de quem anda no sentido do percurso (loop horário: por dentro)
}

// Trilho para LOOPS. Igual ao RailPath, mas:
//  - só se entra por uma ponta (zona de entrada) e no sentido certo;
//  - só se entra pelo lado de DENTRO do loop (por fora, o trilho é ignorado);
//  - ao entrar, o sentido e o lado ficam travados, então o Player não sobe por fora nem fica preso na curva.
// Desenhe o trilho do início ao fim na ordem do percurso (entrada -> loop -> saída).
public class LoopRailPath : RailPath {
    [Header("Entrada do loop")]
    [Tooltip("Ponta do trilho por onde o Player pode entrar. Start = ponto 0 do trilho; End = último ponto.")]
    public LoopEntrance entrance = LoopEntrance.Start;

    [Tooltip("Tamanho (u) do trecho, a partir da ponta de entrada, onde o Player pode agarrar. Fora dele o trilho é ignorado (ex.: cair em cima do topo do loop não gruda).")]
    [Min(0.1f)] public float entryZoneLength = 3f;

    [Tooltip("Lado do trilho onde o Player fica, visto de quem anda no sentido do percurso. Em um loop desenhado anti-horário o lado de dentro é a ESQUERDA. " +
             "Os risquinhos verdes no Gizmo mostram onde o Player vai ficar.")]
    public LoopSide insideSide = LoopSide.Left;

    [Tooltip("Velocidade horizontal (u/s) máxima, no sentido CONTRÁRIO ao da entrada, que ainda é aceita para agarrar. Acima disso o Player não gruda (está vindo pelo lado errado).")]
    [Min(0f)] public float wrongWayTolerance = 1f;

    [Tooltip("Folga (u) para o Player ser considerado do lado de dentro. Aumente se ele estiver sendo recusado ao tocar o trilho de cima.")]
    [Min(0f)] public float sideTolerance = 0.1f;

    [Header("Dentro do loop")]
    [Tooltip("Impede o Player de inverter o sentido dentro do loop (evita voltar de costas no meio da curva).")]
    public bool lockDirection = true;

    [Tooltip("Permite trocar de trilho (cima/baixo) de dentro do loop.")]
    public bool allowRailSwitch = false;

    // +1 = percurso do início para o fim, -1 = do fim para o início
    private int TravelSign => entrance == LoopEntrance.Start ? 1 : -1;

    // Sinal da normal em relação à tangente bruta (início -> fim): Perp(tangente) * sinal aponta para o lado onde o Player fica
    private float NormalSign => TravelSign * (insideSide == LoopSide.Left ? 1f : -1f);

    public override bool LockDirection => lockDirection;
    public override bool AllowRailSwitch => allowRailSwitch;

    public override bool GetForcedEntry(out int dir, out float normalSign) {
        dir = TravelSign;
        normalSign = NormalSign;
        return true;
    }

    // Normal (para "cima" do Player) na distância d
    private Vector2 SurfaceNormal(float d) {
        Vector2 t = GetTangent(d);
        return new Vector2(-t.y, t.x) * NormalSign;
    }

    private void GetEntryRange(out float from, out float to) {
        float zone = Mathf.Min(entryZoneLength, Length);
        from = entrance == LoopEntrance.Start ? 0f : Length - zone;
        to = entrance == LoopEntrance.Start ? zone : Length;
    }

    public override bool TryGetEntry(Vector2 feet, Vector2 pivot, Vector2 velocity, float maxDistance, out float distance) {
        distance = 0f;
        if (!IsValid) return false;

        // 1) Ponto da ZONA DE ENTRADA mais perto dos pés. Procurar só nela evita pegar o trecho
        //    de saída quando as pontas do loop se cruzam.
        GetEntryRange(out float from, out float to);

        const float step = 0.1f;
        int steps = Mathf.Max(1, Mathf.CeilToInt((to - from) / step));
        float bestSqr = float.MaxValue;

        for (int i = 0; i <= steps; i++) {
            float d = Mathf.Lerp(from, to, i / (float)steps);
            float sqr = (GetPoint(d) - feet).sqrMagnitude;
            if (sqr < bestSqr) {
                bestSqr = sqr;
                distance = d;
            }
        }

        if (bestSqr > maxDistance * maxDistance) return false;

        // 2) Sentido: vindo contra o sentido da entrada não gruda
        Vector2 travel = GetTangent(distance) * TravelSign;
        if (Mathf.Abs(travel.x) > 0.2f && velocity.x * Mathf.Sign(travel.x) < -wrongWayTolerance)
            return false;

        // 3) Lado: o pivô do Player precisa estar do lado de dentro (senão ele "atravessaria" o trilho ao grudar)
        if (Vector2.Dot(pivot - GetPoint(distance), SurfaceNormal(distance)) < -sideTolerance)
            return false;

        return true;
    }

    // Gizmos: zona de entrada (verde), saída (vermelho), seta do sentido e risquinhos do lado onde o Player fica
    protected override void DrawExtraGizmos() {
        if (!IsValid) return;

        GetEntryRange(out float from, out float to);

        Gizmos.color = new Color(0.2f, 1f, 0.3f);
        const float step = 0.25f;
        for (float d = from; d < to; d += step) {
            Gizmos.DrawLine(GetPoint(d), GetPoint(Mathf.Min(d + step, to)));
        }

        // Ponta de entrada (verde) e de saída (vermelha)
        float entranceD = entrance == LoopEntrance.Start ? 0f : Length;
        float exitD = Length - entranceD;
        Gizmos.DrawWireSphere(GetPoint(entranceD), 0.2f);
        Gizmos.color = new Color(1f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(GetPoint(exitD), 0.2f);

        // Seta do sentido na entrada
        Vector2 p = GetPoint(entranceD);
        Vector2 dir = GetTangent(entranceD) * TravelSign;
        Vector2 side = new Vector2(-dir.y, dir.x);
        Gizmos.color = new Color(0.2f, 1f, 0.3f);
        Gizmos.DrawLine(p, p + dir * 0.8f);
        Gizmos.DrawLine(p + dir * 0.8f, p + dir * 0.55f + side * 0.15f);
        Gizmos.DrawLine(p + dir * 0.8f, p + dir * 0.55f - side * 0.15f);

        // Lado onde o Player fica (risquinhos a cada 1 u)
        Gizmos.color = new Color(0.4f, 1f, 0.8f, 0.7f);
        for (float d = 0f; d <= Length; d += 1f) {
            Vector2 point = GetPoint(d);
            Gizmos.DrawLine(point, point + SurfaceNormal(d) * 0.35f);
        }
    }
}
