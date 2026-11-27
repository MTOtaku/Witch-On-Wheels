using UnityEngine;

public enum LoopEntrance {
    Start,   // o Player entra pelo INÍCIO do trilho (ponto 0) e anda rumo ao fim
    End,     // o Player entra pelo FIM do trilho e anda rumo ao início
    Both     // as DUAS pontas são entradas: o sentido depende da ponta por onde ele entrou
}

public enum LoopSide {
    Left,    // o Player fica à ESQUERDA do trilho, visto de quem anda do INÍCIO para o FIM (loop anti-horário: por dentro)
    Right    // o Player fica à DIREITA do trilho, visto de quem anda do INÍCIO para o FIM (loop horário: por dentro)
}

// Trilho para LOOPS. Igual ao RailPath, mas:
//  - só se entra pela(s) ponta(s) escolhida(s) (zona de entrada) e no sentido certo;
//  - só se entra pelo lado de DENTRO do loop (por fora, o trilho é ignorado);
//  - ao entrar, o sentido e o lado ficam travados, então o Player não sobe por fora nem fica preso na curva;
//  - se o Player sai do loop no ar (pulo), pode voltar a ele em qualquer ponto por um tempo (reentryWindow).
// Desenhe o trilho do início ao fim na ordem do percurso (entrada -> loop -> saída).
public class LoopRailPath : RailPath {
    [Header("Entrada do loop")]
    [Tooltip("Ponta(s) por onde o Player pode entrar. Start = ponto 0; End = último ponto; Both = as duas (o sentido do percurso segue a ponta usada).")]
    public LoopEntrance entrance = LoopEntrance.Start;

    [Tooltip("Tamanho (u) do trecho, a partir de cada ponta de entrada, onde o Player pode agarrar. Fora dele o trilho é ignorado (ex.: cair em cima do topo do loop não gruda).")]
    [Min(0.1f)] public float entryZoneLength = 3f;

    [Tooltip("Lado do trilho onde o Player fica, visto de quem anda do INÍCIO para o FIM (a ordem em que os pontos foram desenhados). Não muda com o sentido do percurso. " +
             "Em um loop desenhado anti-horário o lado de dentro é a ESQUERDA. Os risquinhos verde-água no Gizmo mostram onde o Player vai ficar.")]
    public LoopSide insideSide = LoopSide.Left;

    [Tooltip("Velocidade horizontal (u/s) máxima, no sentido CONTRÁRIO ao da entrada, que ainda é aceita para agarrar. Acima disso o Player não gruda (está vindo pelo lado errado).")]
    [Min(0f)] public float wrongWayTolerance = 1f;

    [Tooltip("Folga (u) para o Player ser considerado do lado de dentro. Aumente se ele estiver sendo recusado ao tocar o trilho de cima.")]
    [Min(0f)] public float sideTolerance = 0.1f;

    [Header("Reentrada (saiu do loop no ar)")]
    [Tooltip("Tempo (s), depois de sair do loop por pulo (ou tempo de trilho esgotado), em que o Player pode voltar a grudar em QUALQUER ponto dele, pelo lado de dentro e no mesmo sentido. " +
             "Não vale ao sair pela ponta nem descendo com Baixo + Pulo. 0 = desliga: só entra pela zona de entrada.")]
    [Min(0f)] public float reentryWindow = 1f;

    [Header("Dentro do loop")]
    [Tooltip("Impede o Player de inverter o sentido dentro do loop (evita voltar de costas no meio da curva).")]
    public bool lockDirection = true;

    [Tooltip("Permite trocar de trilho (cima/baixo) de dentro do loop.")]
    public bool allowRailSwitch = false;

    // Passo da busca na zona de entrada e na reentrada (u). A reentrada percorre o trilho todo, então usa um passo maior.
    private const float EntryStep = 0.1f;
    private const float ReentryStep = 0.2f;

    // Sinal da normal em relação à tangente bruta (início -> fim): Perp(tangente) * sinal aponta para o lado onde o Player fica.
    // É um lado FÍSICO do trilho, igual nos dois sentidos de percurso.
    private float NormalSign => insideSide == LoopSide.Left ? 1f : -1f;

    public override bool LockDirection => lockDirection;
    public override bool AllowRailSwitch => allowRailSwitch;
    public override float ReentryWindow => reentryWindow;

    private bool AllowsStart => entrance == LoopEntrance.Start || entrance == LoopEntrance.Both;
    private bool AllowsEnd => entrance == LoopEntrance.End || entrance == LoopEntrance.Both;

    // Normal (para "cima" do Player) na distância d
    private Vector2 SurfaceNormal(float d, float normalSign) {
        Vector2 t = GetTangent(d);
        return new Vector2(-t.y, t.x) * normalSign;
    }

    private void GetZone(bool atStart, out float from, out float to) {
        float zone = Mathf.Min(entryZoneLength, Length);
        from = atStart ? 0f : Length - zone;
        to = atStart ? zone : Length;
    }

    public override bool TryGetEntry(Vector2 feet, Vector2 pivot, Vector2 velocity, float maxDistance, RailReentry reentry, out RailEntry entry) {
        entry = default;
        if (!IsValid) return false;

        float bestSqr = float.MaxValue;
        bool found = false;

        // 1) Entrada normal: zona de cada ponta permitida. Procurar só nela evita pegar o trecho
        //    de saída quando as pontas do loop se cruzam. O sentido do percurso segue a ponta usada.
        for (int i = 0; i < 2; i++) {
            bool atStart = i == 0;
            if (atStart ? !AllowsStart : !AllowsEnd) continue;

            GetZone(atStart, out float from, out float to);
            ScanCandidates(from, to, EntryStep, atStart ? 1 : -1, NormalSign, true,
                           feet, pivot, velocity, maxDistance, ref bestSqr, ref found, ref entry);
        }

        // 2) Reentrada: o Player saiu do loop no ar há pouco. Aceita em QUALQUER ponto, mantendo o sentido e o lado de quando saiu.
        if (reentry.active) {
            ScanCandidates(0f, Length, ReentryStep, reentry.dir, reentry.normalSign, false,
                           feet, pivot, velocity, maxDistance, ref bestSqr, ref found, ref entry);
        }

        return found;
    }

    // Procura em [from, to] o ponto mais perto dos pés que passa nos testes de sentido e lado.
    // O teste é feito em CADA ponto candidato: onde as pontas do loop se cruzam, só vale o trecho do lado certo.
    private void ScanCandidates(float from, float to, float step, int travelSign, float normalSign, bool checkDirection,
                                Vector2 feet, Vector2 pivot, Vector2 velocity, float maxDistance,
                                ref float bestSqr, ref bool found, ref RailEntry entry) {
        float maxSqr = maxDistance * maxDistance;
        int steps = Mathf.Max(1, Mathf.CeilToInt((to - from) / step));

        for (int i = 0; i <= steps; i++) {
            float d = Mathf.Lerp(from, to, i / (float)steps);
            Vector2 point = GetPoint(d);

            float sqr = (point - feet).sqrMagnitude;
            if (sqr > maxSqr || sqr >= bestSqr) continue;

            // Sentido (só na entrada normal): vindo contra o sentido da entrada não gruda.
            // Na reentrada o sentido já está fixado pelo que o Player fazia ao sair.
            if (checkDirection) {
                Vector2 travel = GetTangent(d) * travelSign;
                if (Mathf.Abs(travel.x) > 0.2f && velocity.x * Mathf.Sign(travel.x) < -wrongWayTolerance) continue;
            }

            // Lado: o pivô do Player precisa estar do lado de dentro (senão ele "atravessaria" o trilho ao grudar)
            if (Vector2.Dot(pivot - point, SurfaceNormal(d, normalSign)) < -sideTolerance) continue;

            bestSqr = sqr;
            found = true;
            entry = new RailEntry { distance = d, dir = travelSign, normalSign = normalSign };
        }
    }

    // Gizmos: zonas de entrada (verde), saída (vermelho), setas do sentido e risquinhos do lado onde o Player fica
    protected override void DrawExtraGizmos() {
        if (!IsValid) return;

        Color green = new Color(0.2f, 1f, 0.3f);
        Color red = new Color(1f, 0.3f, 0.2f);

        for (int i = 0; i < 2; i++) {
            bool atStart = i == 0;
            bool isEntrance = atStart ? AllowsStart : AllowsEnd;
            float endD = atStart ? 0f : Length;

            if (!isEntrance) {
                // Ponta que não é entrada = saída
                Gizmos.color = red;
                Gizmos.DrawWireSphere(GetPoint(endD), 0.2f);
                continue;
            }

            // Zona de entrada
            GetZone(atStart, out float from, out float to);
            Gizmos.color = green;
            const float step = 0.25f;
            for (float d = from; d < to; d += step) {
                Gizmos.DrawLine(GetPoint(d), GetPoint(Mathf.Min(d + step, to)));
            }
            Gizmos.DrawWireSphere(GetPoint(endD), 0.2f);

            // Seta do sentido na entrada
            Vector2 p = GetPoint(endD);
            Vector2 dir = GetTangent(endD) * (atStart ? 1 : -1);
            Vector2 side = new Vector2(-dir.y, dir.x);
            Gizmos.DrawLine(p, p + dir * 0.8f);
            Gizmos.DrawLine(p + dir * 0.8f, p + dir * 0.55f + side * 0.15f);
            Gizmos.DrawLine(p + dir * 0.8f, p + dir * 0.55f - side * 0.15f);
        }

        // Lado onde o Player fica (risquinhos a cada 1 u)
        Gizmos.color = new Color(0.4f, 1f, 0.8f, 0.7f);
        for (float d = 0f; d <= Length; d += 1f) {
            Vector2 point = GetPoint(d);
            Gizmos.DrawLine(point, point + SurfaceNormal(d, NormalSign) * 0.35f);
        }
    }
}
