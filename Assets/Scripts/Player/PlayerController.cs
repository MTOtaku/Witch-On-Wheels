using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour {
    [Header("Velocidade")]
    [Tooltip("Velocidade horizontal máxima (unidades por segundo).")]
    public float maxSpeed = 10f;

    [Tooltip("Tempo em segundos para ir do repouso até a velocidade máxima.")]
    [Min(0.01f)] public float accelerationTime = 1f;

    [Tooltip("Tempo em segundos para frear da velocidade máxima até parar.")]
    [Min(0.01f)] public float decelerationTime = 0.5f;

    [Tooltip("Velocidade mínima para disparar o Turn Over ao inverter o sentido.")]
    [Min(0.1f)] public float turnOverMinSpeed = 1f;

    [Header("Pulo")]
    [Tooltip("Velocidade vertical inicial do pulo.")]
    public float jumpForce = 12f;

    [Tooltip("Multiplicador da velocidade vertical ao soltar o botão na subida (0 a 1).")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Tooltip("Multiplicador de gravidade na queda.")]
    public float fallMultiplier = 2f;

    [Tooltip("Tolerância para pular logo depois de sair da plataforma.")]
    public float coyoteTime = 0.15f;

    [Tooltip("Tolerância para registrar o pulo logo antes de tocar o chão.")]
    public float jumpBufferTime = 0.15f;

    [Header("Chão")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("Parede")]
    [Tooltip("Camada considerada parede. Se vazia (Nothing), usará groundLayer.")]
    public LayerMask wallLayer;
    [Tooltip("Distância do raycast lateral para detectar parede.")]
    public float wallCheckDistance = 0.15f;
    [Tooltip("Ativa ou desativa o Wall Grab (travar na parede ao encostar).")]
    public bool wallGrabEnabled = true;
    [Tooltip("Tempo (s) que o jogador fica preso na parede antes de começar a deslizar.")]
    public float wallGrabDuration = 0.6f;
    [Tooltip("Velocidade máxima de descida ao deslizar pela parede após o Wall Grab.")]
    public float wallSlideSpeed = 2.5f;
    [Tooltip("Ativa ou desativa o Wall Jump.")]
    public bool wallJumpEnabled = true;
    [Tooltip("Força vertical do Wall Jump.")]
    public float wallJumpForce = 12f;
    [Tooltip("Força horizontal do Wall Jump (sempre oposta à parede).")]
    public float wallJumpHorizontalForce = 10f;
    [Tooltip("Duração (s) do bloqueio de controle horizontal após o Wall Jump.")]
    public float wallJumpControlLockDuration = 0.4f;
    [Tooltip("Cooldown (s) entre Wall Jumps consecutivos do mesmo lado.")]
    public float wallJumpCooldown = 0.1f;

    [Header("Ledge Grab & Climb")]
    [Tooltip("Distância horizontal para checagem da quina/borda.")]
    public float ledgeCheckDistance = 0.5f;
    [Tooltip("Offset da posição do jogador em relação à quina enquanto pendurado (X afasta da quina, Y desce).")]
    public Vector2 ledgeGrabOffset = new Vector2(0.35f, 0.7f);
    [Tooltip("Offset da posição final em cima da plataforma após subir (X avança para cima da quina, Y altura dos pés).")]
    public Vector2 ledgeClimbOffset = new Vector2(0.5f, 1.0f);
    [Tooltip("Duração do movimento de subida na plataforma.")]
    public float ledgeClimbDuration = 0.2f;

    [Header("Rail Grinding")]
    
    [Tooltip("Ação do Input System utilizada para entrar manualmente em um trilho.")]
    [SerializeField] private InputActionReference railGrindInput;
    
    [Tooltip("Ativa ou desativa o grind em trilhos.")]
    public bool railGrindEnabled = true;

    [Tooltip("Camada dos trilhos (objetos com RailPath).")]
    public LayerMask railLayer;

    [Tooltip("Raio ao redor dos pés para encontrar um trilho. Quanto maior, mais perdoa.")]
    public float railCheckRadius = 0.6f;

    [Tooltip("Ao cair, ignora trilhos que estejam acima dos pés por mais que isso (evita grudar vindo por baixo).")]
    public float railAboveTolerance = 0.3f;

    [Tooltip("Só gruda no ar se a velocidade vertical for menor ou igual a isso (ou seja, caindo).")]
    public float railMaxAttachRiseSpeed = 0.5f;

    [Tooltip("Valor do input para cima (MoveInputY) para subir no trilho a partir do chão.")]
    [Range(0.1f, 1f)] public float railHopInput = 0.5f;

    [Tooltip("Tempo (s) sem poder grudar de novo depois de sair de um trilho.")]
    public float railReattachCooldown = 0.25f;

    [Tooltip("Alcance (unidades) para trocar de trilho para cima ou para baixo durante o grind.")]
    public float railSwitchRange = 3f;

    [Header("Rail Grinding - Velocidade")]
    [Tooltip("Velocidade mínima no trilho (unidades por segundo). O Player nunca para.")]
    public float railMinSpeed = 8f;

    [Tooltip("Velocidade máxima no trilho.")]
    public float railMaxSpeed = 18f;

    [Tooltip("Aceleração (u/s²) ao segurar na direção do movimento.")]
    public float railAcceleration = 8f;

    [Tooltip("Atrito (u/s²): volta devagar à velocidade mínima ao soltar o input. Valor alto cancela o ganho da descida.")]
    public float railDeceleration = 1f;

    [Tooltip("Efeito da inclinação (u/s²): descida acelera, subida freia.")]
    public float railSlopeAcceleration = 20f;
    
    [Header("Rail Grinding - Boost")]
    [Tooltip("Ação do Input System utilizada para ativar o boost.")]
    public InputActionReference railBoostAction;

    [Tooltip("Velocidade máxima que pode ser alcançada durante o boost.")]
    [Min(0f)] public float railBoostMaxSpeed = 20f;

    [Tooltip("Aceleração aplicada enquanto o boost estiver ativo (unidades/s²).")]
    [Min(0f)] public float railBoostAcceleration = 20f;

    [Tooltip("Duração do boost em segundos.")]
    [Min(0.1f)] public float railBoostDuration = 1.5f;

    [Tooltip("Tempo de espera após o término do boost para poder utilizá-lo novamente.")]
    [Min(0f)] public float railBoostCooldown = 2f;

    [Tooltip("Desaceleração aplicada ao terminar o boost, até retornar à velocidade normal.")]
    [Min(0f)] public float railBoostEndDeceleration = 10f;
    
    [Header("Rail Grinding - Visual")]
    [Tooltip("Inclina o sprite seguindo o trilho.")]
    public bool railTiltVisual = true;

    [Tooltip("Objeto que inclina. Se vazio, usa o objeto do Animator (o sprite filho).")]
    public Transform railVisual;

    [Tooltip("Rapidez com que o sprite acompanha a inclinação.")]
    public float railTiltSpeed = 12f;

    [Header("Lançamento do Rail Grinding")]
    [Tooltip("Força do pulo ao sair do trilho. O impulso segue a direção perpendicular ao sprite, apontando para cima.")]
    [Min(0f)] public float railJumpForce = 12f;

    [Tooltip("Velocidade do lançamento ao chegar ao final do trilho. Segue a inclinação e o sentido do movimento.")]
    [Min(0f)] public float railEndLaunchForce = 18f;
    
    [Header("Componentes e Entrada")]
    public Rigidbody2D rb;
    public Animator animator;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    [Header("Status (leitura)")]
    public bool isGrounded;
    public int wallDirection; // -1 = esquerda, 1 = direita, 0 = nenhuma
    public float currentSpeed;
    public Vector2 currentVelocity;

    [SerializeField] private string currentState;

    // ------------------------------------------- State Machine -------------------------------------------
    public PlayerStateMachine StateMachine { get; private set; }
    public IdleState Idle       { get; private set; }
    public RunState Run         { get; private set; }
    public TurnOverState TurnOver { get; private set; }
    public RiseState Rise       { get; private set; }
    public FallState Fall       { get; private set; }
    public WallGrabState WallGrab { get; private set; }
    public WallSlideState WallSlide { get; private set; }
    public LedgeGrabState LedgeGrab { get; private set; }
    public LedgeClimbState LedgeClimb { get; private set; }
    public GrindState Grind     { get; private set; }

    // ------------------------------------------- Leitura de entrada -------------------------------------------
    public float MoveInput  { get; private set; }
    public float MoveInputY { get; private set; }
    public bool  JumpHeld   { get; private set; }
    public bool  HasInput   => Mathf.Abs(MoveInput) > 0.1f;

    public float VelocityX => rb.linearVelocity.x;
    public float VelocityY => rb.linearVelocity.y;
    public float Speed     => Mathf.Abs(rb.linearVelocity.x);

    public bool WantsToJump   => _jumpBufferTimer > 0f && _coyoteTimer > 0f;
    public bool HasJumpBuffer => _jumpBufferTimer > 0f;
    public bool HasCoyoteTime => _coyoteTimer > 0f;
    public bool IsTouchingWall => wallDirection != 0;
    public bool RailBoostPressed => railBoostAction?.action != null && railBoostAction.action.WasPressedThisFrame();

    // ------------------------------------------- Wall Jump & Wall Grab Tracking -------------------------------------------
    public bool  IsWallJumpLocked    => _wallJumpControlTimer > 0f;
    public int   WallJumpLockedDir   => _wallJumpDir;      // lado DA PAREDE do último jump
    public bool  WallJumpOnCooldown  => _wallJumpCooldownTimer > 0f;
    public int   LastWallJumpSide    => _lastWallJumpSide; // -1 / 1 / 0
    // Flag que informa ao RiseState que a velocidade JÁ foi definida pelo DoWallJump
    public bool  IsFromWallJump      { get; private set; }

    public int   LastGrabbedWallSide => _lastGrabbedWallSide;
    public float CurrentWallGrabTimer { get; set; }

    public void ResetWallGrabTimer(int wallDir) {
        _lastGrabbedWallSide = wallDir;
        CurrentWallGrabTimer = wallGrabDuration;
    }

    public void DepleteWallGrabTimer(int wallDir) {
        _lastGrabbedWallSide = wallDir;
        CurrentWallGrabTimer = 0f;
    }

    // ------------------------------------------- Ledge -------------------------------------------
    public Vector2 LedgeCorner  { get; private set; }
    public int     LedgeDir     { get; private set; }
    public bool    CanGrabLedge => _ledgeGrabCooldown <= 0f;

    // ------------------------------------------- Rail Grinding -------------------------------------------
    public RailPath GrindRail { get; private set; }
    public float GrindStartDistance { get; private set; }

    public Vector2 FeetPosition => new Vector2(_col.bounds.center.x, _col.bounds.min.y);
    public float PivotToFeet => rb.position.y - _col.bounds.min.y;
    public Transform RailVisualRoot => railVisual != null ? railVisual : (animator != null ? animator.transform : null);

    public LayerMask EffectiveWallLayer => wallLayer.value != 0 ? wallLayer : groundLayer;

    // ------------------------------------------- Privados -------------------------------------------
    private Collider2D _col;
    private float _defaultGravity = 1f;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool  _jumpReleased;

    // ------------------------------------------- Wall Jump & Wall Grab -------------------------------------------
    private float _wallJumpControlTimer;   // bloqueio do controle horizontal
    private int   _wallJumpDir;            // lado da parede de onde pulou (para manter impulso)
    private int   _lastWallJumpSide;       // -1/1 — impede dois jumps seguidos do mesmo lado
    private float _wallJumpCooldownTimer;
    private int   _lastGrabbedWallSide;    // -1/1 — lado da última parede agarrada

    // ------------------------------------------- Ledge -------------------------------------------
    private float _ledgeGrabCooldown;

    // ------------------------------------------- Rail Grinding -------------------------------------------
    private const float SwitchHorizontalTolerance = 1.5f;
    private readonly Collider2D[] _railHits = new Collider2D[8];
    private float _railCooldownUntil;
    private float _railStoredReturnSpeed;
    private bool _hasRailStoredReturnSpeed;

    private void Awake() {
        if (rb       == null) rb       = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        _col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        _defaultGravity   = rb.gravityScale;

        StateMachine = new PlayerStateMachine();
        Idle       = new IdleState(this);
        Run        = new RunState(this);
        TurnOver   = new TurnOverState(this);
        Rise       = new RiseState(this);
        Fall       = new FallState(this);
        WallGrab   = new WallGrabState(this);
        WallSlide  = new WallSlideState(this);
        LedgeGrab  = new LedgeGrabState(this);
        LedgeClimb = new LedgeClimbState(this);
        Grind      = new GrindState(this);

        StateMachine.StateChanged += (_, next) =>
            currentState = next != null ? next.GetType().Name : string.Empty;
    }

    private void OnEnable() {
        if (moveAction?.action != null) moveAction.action.Enable();
        if (jumpAction?.action != null) jumpAction.action.Enable();
        if (railBoostAction?.action != null) railBoostAction.action.Enable();
    }

    private void OnDisable() {
        if (moveAction?.action != null) moveAction.action.Disable();
        if (jumpAction?.action != null) jumpAction.action.Disable();
        if (railBoostAction?.action != null) railBoostAction.action.Disable();
    }

    private void Start() => StateMachine.Initialize(Idle);

    private void Update() {
        ReadInput();
        CheckGround();
        CheckWall();
        UpdateTimers();
        StateMachine.Tick();
    }

    private void FixedUpdate() {
        StateMachine.PhysicsTick();

        if (rb != null) {
            currentSpeed = rb.linearVelocity.magnitude;
            currentVelocity = rb.linearVelocity;
        }
    }

    // ------------------------------------------- Ler Input -------------------------------------------
    private void ReadInput() {
        if (moveAction?.action != null) {
            Vector2 move = moveAction.action.ReadValue<Vector2>();
            MoveInput  = move.x;
            MoveInputY = move.y;
        }

        if (jumpAction?.action != null) {
            if (jumpAction.action.WasPressedThisFrame())  _jumpBufferTimer = jumpBufferTime;
            if (jumpAction.action.WasReleasedThisFrame()) _jumpReleased    = true;
            JumpHeld = jumpAction.action.IsPressed();
        }
    }

    // -------------------------------------------  Chão   -------------------------------------------
      
    
    private void CheckGround() {
        Vector2 origin;
        if (groundCheck != null)
            origin = groundCheck.position;
        else if (_col != null)
            origin = new Vector2(_col.bounds.center.x, _col.bounds.min.y);
        else { isGrounded = false; return; }

        bool touching = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer) != null;
        isGrounded = touching && rb.linearVelocity.y <= 0.05f;
    }

    // ------------------------------------------- Parede -------------------------------------------
    private void CheckWall() {
        if (_col == null) { wallDirection = 0; return; }

        Bounds  bounds    = _col.bounds;
        Vector2 center    = bounds.center;
        float   rayLength = bounds.extents.x + wallCheckDistance;
        LayerMask mask    = EffectiveWallLayer;

        Vector2 originMid  = center;
        Vector2 originHigh = new Vector2(center.x, bounds.max.y - 0.2f);

        bool hitLeft  = Physics2D.Raycast(originMid,  Vector2.left,  rayLength, mask) ||
                        Physics2D.Raycast(originHigh, Vector2.left,  rayLength, mask);
        bool hitRight = Physics2D.Raycast(originMid,  Vector2.right, rayLength, mask) ||
                        Physics2D.Raycast(originHigh, Vector2.right, rayLength, mask);

        if      (hitLeft  && hitRight) wallDirection = MoveInput < -0.1f ? -1 : MoveInput > 0.1f ? 1 : 0;
        else if (hitLeft)              wallDirection = -1;
        else if (hitRight)             wallDirection =  1;
        else                           wallDirection =  0;
    }

    // ------------------------------------------- Timers -------------------------------------------
    private void UpdateTimers() {
        if (isGrounded) {
            _coyoteTimer         = coyoteTime;
            _lastWallJumpSide    = 0; // reseta restrição de lado ao tocar o chão
            _lastGrabbedWallSide = 0; // reseta parede do grab ao tocar o chão
            CurrentWallGrabTimer = wallGrabDuration;
        }
        else {
            _coyoteTimer -= Time.deltaTime;
        }

        if (_jumpBufferTimer       > 0f) _jumpBufferTimer       -= Time.deltaTime;
        if (_wallJumpControlTimer  > 0f) _wallJumpControlTimer  -= Time.deltaTime;
        if (_wallJumpCooldownTimer > 0f) _wallJumpCooldownTimer -= Time.deltaTime;
        if (_ledgeGrabCooldown     > 0f) _ledgeGrabCooldown     -= Time.deltaTime;
    }

    
    // -------------------------------------------  API do Wall Grab / Wall Jump -------------------------------------------

    
    // Retorna true se o jogador pode se agarrar à parede naquela direção.
    public bool CanWallGrab(int wallDir) {
        if (!wallGrabEnabled) return false;
        if (wallDir == 0)     return false;
        if (isGrounded)       return false;

        // Se for a mesma parede e o tempo de grab nela já esgotou, não reseta nem agarra novamente
        if (wallDir == _lastGrabbedWallSide && CurrentWallGrabTimer <= 0f) {
            return false;
        }

        // Durante o lockout de wall jump, impede re-agarrar a MESMA parede de onde saltou.
        // Isso evita entrar imediatamente de volta no WallGrab antes de se afastar da parede.
        if (IsWallJumpLocked && wallDir == WallJumpLockedDir) return false;

        // Fora do lockout: o jogador precisa estar segurando em direção à parede.
        // Durante o lockout (indo para a parede oposta), o grab da parede oposta é permitido.
        if (!IsWallJumpLocked) {
            bool holdingTowards = (wallDir > 0 && MoveInput > 0.1f) ||
                                  (wallDir < 0 && MoveInput < -0.1f);
            if (!holdingTowards) return false;
        }

        return true;
    }

    // Retorna true se o jogador pode deslizar pela parede (WallSlide).
    public bool CanWallSlide(int wallDir) {
        if (wallDir == 0) return false;
        if (isGrounded)   return false;
        if (IsWallJumpLocked && wallDir == WallJumpLockedDir) return false;

        bool holdingTowards = (wallDir > 0 && MoveInput > 0.1f) ||
                              (wallDir < 0 && MoveInput < -0.1f);
        return holdingTowards;
    }
    
    // Retorna true se o jogador pode fazer Wall Jump nessa direção de parede.
    public bool CanWallJump(int wallDir) {
        if (!wallJumpEnabled)              return false;
        if (wallDir == 0)                  return false;
        if (isGrounded)                    return false;
        if (WallJumpOnCooldown)            return false;
        // Alterna lados: não pode pular duas vezes do mesmo lado consecutivo
        return wallDir != _lastWallJumpSide;
    }
    
    // Executa o Wall Jump: sempre lança o jogador para o lado OPOSTO à parede, independente do input atual.
    public void DoWallJump(int wallDir) {
        _lastWallJumpSide      = wallDir;
        _wallJumpDir           = wallDir;
        _wallJumpControlTimer  = wallJumpControlLockDuration;
        _wallJumpCooldownTimer = wallJumpCooldown;
        IsFromWallJump         = true;   // sinaliza ao RiseState que NÃO deve resetar a velocidade
        ConsumeJump();

        // A direção do salto é SEMPRE oposta à parede
        float jumpDirX = -wallDir;
        rb.linearVelocity = new Vector2(jumpDirX * wallJumpHorizontalForce, wallJumpForce);
    }

    /// <summary>Chamado por RiseState.Enter para consumir a flag após usar.</summary>
    public void ClearWallJumpFlag() => IsFromWallJump = false;
    
    //------------------------------------------- API do Ledge Grab -------------------------------------------
    public bool CheckLedge(out Vector2 cornerPos, out int ledgeDir) {
        cornerPos = Vector2.zero;
        ledgeDir  = 0;

        if (_col == null || !CanGrabLedge || isGrounded) return false;

        Bounds bounds = _col.bounds;
        LayerMask mask = EffectiveWallLayer;

        int[] testDirs;
        if      (wallDirection != 0)  testDirs = new[] { wallDirection };
        else if (MoveInput > 0.1f)    testDirs = new[] {  1, -1 };
        else if (MoveInput < -0.1f)   testDirs = new[] { -1,  1 };
        else                          testDirs = new[] {  1, -1 };

        foreach (int dir in testDirs) {
            Vector2 dirVec = new Vector2(dir, 0f);

            // 1. Raio no peito/ombro — deve acertar a parede
            Vector2 chestOrigin = new Vector2(bounds.center.x, bounds.center.y + bounds.extents.y * 0.3f);
            float   forwardDist = bounds.extents.x + ledgeCheckDistance;
            RaycastHit2D wallHit = Physics2D.Raycast(chestOrigin, dirVec, forwardDist, mask);
            if (!wallHit) continue;

            // 2. Raio acima da cabeça — deve ser espaço livre
            Vector2 headOrigin = new Vector2(bounds.center.x, bounds.max.y + 0.15f);
            if (Physics2D.Raycast(headOrigin, dirVec, forwardDist, mask)) continue;

            // 3. Raio vertical para achar a quina
            Vector2 downOrigin = new Vector2(wallHit.point.x + dir * 0.05f, bounds.max.y + 0.3f);
            RaycastHit2D downHit = Physics2D.Raycast(downOrigin, Vector2.down, bounds.size.y, mask);
            if (!downHit) continue;

            float ledgeY = downHit.point.y;
            if (ledgeY >= bounds.center.y && ledgeY <= bounds.max.y + 0.3f) {
                cornerPos = new Vector2(wallHit.point.x, ledgeY);
                ledgeDir  = dir;
                return true;
            }
        }
        return false;
    }

    public void GrabLedge(Vector2 corner, int dir) {
        LedgeCorner = corner;
        LedgeDir    = dir;
        rb.linearVelocity = Vector2.zero;
        SetGravityMultiplier(0f);
        transform.position = new Vector3(
            corner.x - dir * ledgeGrabOffset.x,
            corner.y - ledgeGrabOffset.y,
            transform.position.z);
    }

    public void StartLedgeCooldown() => _ledgeGrabCooldown = 0.3f;

    // ------------------------------------------- API do Rail Grinding -------------------------------------------

    // Tenta entrar no grind. Retorna true se trocou de estado (quem chamou deve dar return).
    public bool TryEnterGrind() {
        if (!railGrindEnabled || _col == null || Time.time < _railCooldownUntil)
            return false;
        
        bool manualRequest = railGrindInput != null &&
                             railGrindInput.action.WasPressedThisFrame();
        
        bool automaticRequest = !isGrounded &&
                                VelocityY <= railMaxAttachRiseSpeed;

        if (!manualRequest && !automaticRequest)
            return false;

        if (!FindRail(manualRequest, out RailPath rail, out float distance))
            return false;

        GrindRail = rail;
        GrindStartDistance = distance;
        StateMachine.ChangeState(Grind);

        return true;
    }

    // Procura o trilho mais próximo dos pés
    private bool FindRail(bool manualRequest, out RailPath best, out float bestDistance) {
        best = null;
        bestDistance = 0f;

        Vector2 feet = FeetPosition;
        int count = OverlapRails(feet, railCheckRadius);
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++) {
            if (!_railHits[i].TryGetComponent(out RailPath rail)) continue;
            if (!rail.isActiveAndEnabled || !rail.IsValid) continue;
            
            if (!manualRequest && !rail.autoGrab) continue;

            float distance = rail.GetClosestDistance(feet);
            Vector2 point = rail.GetPoint(distance);
            
            if (!manualRequest && point.y > feet.y + railAboveTolerance) continue;

            float sqr = (point - feet).sqrMagnitude;

            if (sqr < bestSqr) {
                bestSqr = sqr;
                best = rail;
                bestDistance = distance;
            }
        }

        return best != null;
    }

    // Trilho vizinho para trocar durante o grind. vertical: +1 = acima, -1 = abaixo.
    public bool FindNeighborRail(RailPath current, int vertical, out RailPath best, out float bestDistance) {
        best = null;
        bestDistance = 0f;
        if (_col == null) return false;

        Vector2 feet = FeetPosition;
        int count = OverlapRails(feet, railSwitchRange);
        float bestDy = float.MaxValue;

        for (int i = 0; i < count; i++) {
            if (!_railHits[i].TryGetComponent(out RailPath rail) || rail == current) continue;
            if (!rail.isActiveAndEnabled || !rail.IsValid) continue;

            float distance = rail.GetClosestDistance(feet);
            Vector2 point = rail.GetPoint(distance);

            float dy = (point.y - feet.y) * vertical;   // positivo = está na direção pedida
            if (dy < 0.1f || Mathf.Abs(point.x - feet.x) > SwitchHorizontalTolerance) continue;

            if (dy < bestDy) {
                bestDy = dy;
                best = rail;
                bestDistance = distance;
            }
        }
        return best != null;
    }

    private int OverlapRails(Vector2 center, float radius) {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(railLayer);
        filter.useTriggers = true;   // o collider do trilho é trigger
        return Physics2D.OverlapCircle(center, radius, filter, _railHits);
    }

    public void StartRailCooldown() => _railCooldownUntil = Time.time + railReattachCooldown;
    
    // Guarda a velocidade para recuperar quando o jogador retornar ao trilho
    public void StoreRailReturnSpeed(float speed){
        _railStoredReturnSpeed = speed;
        _hasRailStoredReturnSpeed = true;
    }
    
    // Recupera a velocidade armazenada uma unica vez
    public bool TryConsumeRailReturnSpeed(out float speed){
        speed =  _railStoredReturnSpeed;

        if (!_hasRailStoredReturnSpeed) {
            return false;
        }
        
        _hasRailStoredReturnSpeed = false;
        return true;
    }
    
    
    //  ------------------------------------------- Ferramentas de movimento usadas pelos states -------------------------------------------
    public void ConsumeJump() {
        _jumpBufferTimer = 0f;
        _coyoteTimer     = 0f;
        _jumpReleased    = false;
    }

    public bool ConsumeJumpRelease() {
        bool released = _jumpReleased;
        _jumpReleased = false;
        return released;
    }

    public bool IsReversing(float minSpeed) {
        return (VelocityX >  minSpeed && MoveInput < -0.1f) ||
               (VelocityX < -minSpeed && MoveInput >  0.1f);
    }

    // Move o personagem horizontalmente
    // Durante o lock do Wall Jump, o impulso da parede é preservado sem ser cancelado pelo input
    // Após o lock, o jogador retoma controle total
    public void MoveHorizontal(float input, bool braking = false) {
        if (_wallJumpControlTimer > 0f) {
            return;
        } // Mantém o impulso horizontal gerado pelo Wall Jump, não deixa o input cancelar imediatamente

        float currentX = rb.linearVelocity.x;
        float targetX  = input * maxSpeed;

        bool noInput   = Mathf.Abs(input) < 0.001f;
        bool reversing = currentX * targetX < 0f;

        float time = (braking || noInput || reversing) ? decelerationTime : accelerationTime;
        float rate = maxSpeed / time;

        float newX = Mathf.MoveTowards(currentX, targetX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    public void SetVelocityY(float y)                   => rb.linearVelocity = new Vector2(rb.linearVelocity.x, y);
    public void SetGravityMultiplier(float multiplier)  => rb.gravityScale = _defaultGravity * multiplier;

    public void PlayAnimation(int stateHash, float fadeSeconds = 0.05f) {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (!animator.HasState(0, stateHash)) return;
        animator.CrossFadeInFixedTime(stateHash, fadeSeconds);
    }
    
    // ------------------------------------------- Gizmos -------------------------------------------
    private void OnDrawGizmosSelected() {
        if (groundCheck != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Collider2D col = _col != null ? _col : GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;

        Gizmos.color = Color.blue;
        float wallDist = b.extents.x + wallCheckDistance;
        Gizmos.DrawLine(b.center, b.center + Vector3.left  * wallDist);
        Gizmos.DrawLine(b.center, b.center + Vector3.right * wallDist);

        Gizmos.color = Color.yellow;
        Vector3 chest       = new Vector3(b.center.x, b.center.y + b.extents.y * 0.3f, 0f);
        float   forwardDist = b.extents.x + ledgeCheckDistance;
        Gizmos.DrawLine(chest, chest + Vector3.left  * forwardDist);
        Gizmos.DrawLine(chest, chest + Vector3.right * forwardDist);

        Vector3 head = new Vector3(b.center.x, b.max.y + 0.15f, 0f);
        Gizmos.DrawLine(head, head + Vector3.left  * forwardDist);
        Gizmos.DrawLine(head, head + Vector3.right * forwardDist);
        
        //Rail Check Radius
        Vector3 railCheckPosition = new Vector3(
            b.center.x,
            b.min.y,
            transform.position.z
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(railCheckPosition, railCheckRadius);
        
        Gizmos.DrawSphere(railCheckPosition, 0.04f);
    }
}
