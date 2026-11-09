using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMovement : MonoBehaviour {
    [Header("Configurações de Velocidade")]
    [Tooltip("Velocidade máxima que o jogador pode atingir (em unidades por segundo).")]
    public float maxSpeed = 10f;

    [Header("Aceleração e Desaceleração")]
    [Tooltip("Tempo em segundos necessário para ir do repouso (0) até a velocidade máxima.")]
    [Min(0.01f)]
    public float accelerationTime = 1.0f;

    [Tooltip("Tempo em segundos necessário para desacelerar da velocidade máxima até parar completamente após soltar os controles.")]
    [Min(0.01f)]
    public float decelerationTime = 0.5f;

    [Header("Pulo")]
    [Tooltip("Velocidade vertical inicial aplicada ao pular.")]
    public float jumpForce = 12f;

    [Tooltip("Multiplicador da velocidade vertical ao soltar o botão durante a subida (0 a 1). Quanto menor, mais baixo é o pulo mínimo.")]
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;

    [Tooltip("Multiplicador de gravidade durante a descida (queda). Valores entre 1.5 e 2.5 eliminam a sensação de pulo flutuante.")]
    public float fallMultiplier = 2f;

    [Tooltip("Tempo de tolerância para pular logo após sair de uma plataforma (Coyote Time).")]
    public float coyoteTime = 0.15f;

    [Tooltip("Tempo de tolerância para registrar o comando de pulo instantes antes de tocar no chão (Jump Buffer).")]
    public float jumpBufferTime = 0.15f;

    [Tooltip("Ponto utilizado para verificar se o jogador está no chão.")]
    public Transform groundCheck;

    [Tooltip("Camadas consideradas como chão.")]
    public LayerMask groundLayer;

    [Tooltip("Raio da verificação de chão.")]
    public float groundCheckRadius = 0.2f;

    [Header("Máquina de Estados e Animação")]
    [Tooltip("Referência ao Animator. Se não for atribuído, busca no GameObject ou nos filhos automaticamente.")]
    public Animator animator;

    [Tooltip("Nome do parâmetro Bool no Animator para a animação de Turn Over (virada brusca/derrapagem).")]
    public string turnOverBoolParam = "IsTurningOver";

    [Tooltip("Nome do parâmetro Trigger no Animator para o disparo da virada (opcional). Deixe vazio se usar apenas Bool.")]
    public string turnOverTriggerParam = "TurnOver";

    [Tooltip("Velocidade mínima necessária para disparar o estado de Turn Over ao tentar inverter o sentido.")]
    [Min(0.1f)]
    public float turnOverMinSpeed = 1.0f;

    [Header("Componentes e Entrada")]
    [Tooltip("Referência ao Rigidbody2D do jogador. Se deixado vazio, será obtido automaticamente no Awake.")]
    public Rigidbody2D rb;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("Ação de movimento do Input System (opcional). Se não for definida, o script usará automaticamente o teclado (WASD / Setas) e analógico do Gamepad.")]
    public InputActionReference moveAction;

    [Tooltip("Ação responsável pelo pulo (opcional). Se não for definida, o script usará automaticamente Espaço no teclado e botão de ação no Gamepad.")]
    public InputActionReference jumpAction;
#endif

    [Header("Status em Tempo Real (Leitura)")]
    [Tooltip("Velocidade horizontal atual.")]
    public float speed;

    [Tooltip("Taxa de aceleração atual calculada em unidades/s² (maxSpeed / accelerationTime).")]
    public float accelerationSpeed;

    [Tooltip("Valor da entrada de movimento no eixo horizontal (-1 a 1).")]
    public float move;

    [Tooltip("Indica se o jogador está executando Turn Over.")]
    public bool isTurningOver;

    [Tooltip("Indica se o jogador está no chão.")]
    public bool isGrounded;

    [Tooltip("Indica se o jogador está em fase de subida do pulo.")]
    public bool isJumping;
    
    // Evento disparado quando o estado de Turn Over muda
    
    public System.Action<bool> OnTurnOverChanged;

    private Vector2 _moveInput;
    private float _defaultGravityScale = 1f;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpHeld;
    private bool _jumpCutRequested;

    private int _turnOverBoolHash;
    private int _turnOverTriggerHash;
    private int _speedHash;
    private int _isMovingHash;

    private bool _hasTurnOverBool;
    private bool _hasTurnOverTrigger;
    private bool _hasSpeedFloat;
    private bool _hasIsMovingBool;
    private bool _animatorInitialized;

    private void Awake() {
        if (rb == null) {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null) {
            rb.freezeRotation = true;
            _defaultGravityScale = rb.gravityScale;
        }

        CacheAnimatorParameters();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable() {
        if (moveAction != null && moveAction.action != null) {
            moveAction.action.Enable();
        }

        if (jumpAction != null && jumpAction.action != null) {
            jumpAction.action.Enable();
        }
    }

    private void OnDisable() {
        if (moveAction != null && moveAction.action != null) {
            moveAction.action.Disable();
        }

        if (jumpAction != null && jumpAction.action != null) {
            jumpAction.action.Disable();
        }
    }
#endif

    private void Update() {
        ReadInput();
        CheckGround();
        UpdateJumpTimers();

        accelerationSpeed = accelerationTime > 0.001f
            ? maxSpeed / accelerationTime
            : float.MaxValue;

        UpdateAnimator();
    }

    private void FixedUpdate() {
        ApplyMovement();
        ApplyJump();
    }

    private void ReadInput() {
        _moveInput = Vector2.zero;

        // 1. Entrada de Movimento Horizontal
        if (moveAction != null && moveAction.action != null) {
            _moveInput.x = moveAction.action.ReadValue<Vector2>().x;
        }
#if ENABLE_INPUT_SYSTEM
        else {
            if (Keyboard.current != null) {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) {
                    _moveInput.x -= 1f;
                }
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) {
                    _moveInput.x += 1f;
                }
            }

            if (Gamepad.current != null) {
                float stickX = Gamepad.current.leftStick.ReadValue().x;
                if (Mathf.Abs(stickX) > 0.1f) {
                    _moveInput.x = stickX;
                }
            }
        }
#else
        else {
            _moveInput.x = Input.GetAxisRaw("Horizontal");
        }
#endif

        move = _moveInput.x;

        // 2. Entrada de Pulo
        bool jumpPressed = false;
        bool jumpReleased = false;

        if (jumpAction != null && jumpAction.action != null) {
            jumpPressed = jumpAction.action.WasPressedThisFrame();
            jumpReleased = jumpAction.action.WasReleasedThisFrame();
            _jumpHeld = jumpAction.action.IsPressed();
        }
#if ENABLE_INPUT_SYSTEM
        else {
            if (Keyboard.current != null) {
                jumpPressed |= Keyboard.current.spaceKey.wasPressedThisFrame;
                jumpReleased |= Keyboard.current.spaceKey.wasReleasedThisFrame;
                _jumpHeld = Keyboard.current.spaceKey.isPressed;
            }

            if (Gamepad.current != null) {
                jumpPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame;
                jumpReleased |= Gamepad.current.buttonSouth.wasReleasedThisFrame;
                _jumpHeld |= Gamepad.current.buttonSouth.isPressed;
            }
        }
#else
        else {
            jumpPressed = Input.GetButtonDown("Jump");
            jumpReleased = Input.GetButtonUp("Jump");
            _jumpHeld = Input.GetButton("Jump");
        }
#endif

        if (jumpPressed) {
            _jumpBufferTimer = jumpBufferTime;
        }

        if (jumpReleased) {
            _jumpCutRequested = true;
        }
    }

    private void CheckGround() {
        if (rb == null) {
            isGrounded = false;
            return;
        }

        bool touchingGround = false;

        if (groundCheck != null) {
            touchingGround = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }
        else {
            // Fallback caso groundCheck não tenha sido configurado no Inspector
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) {
                Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y);
                touchingGround = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer);
            }
        }

        isGrounded = touchingGround && rb.linearVelocity.y <= 0.05f;
    }

    private void UpdateJumpTimers() {
        if (isGrounded) {
            _coyoteTimer = coyoteTime;
        }
        else {
            _coyoteTimer -= Time.deltaTime;
        }

        if (_jumpBufferTimer > 0f) {
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void ApplyMovement() {
        if (rb == null) return;

        float currentX = rb.linearVelocity.x;
        float targetX = _moveInput.x * maxSpeed;
        bool hasInput = Mathf.Abs(_moveInput.x) > 0.001f;

        // Detecta tentativa de mover no sentido oposto com velocidade mínima
        bool isOpposing = (currentX > turnOverMinSpeed && _moveInput.x < -0.1f) ||
                          (currentX < -turnOverMinSpeed && _moveInput.x > 0.1f);

        if (isTurningOver) {
            if (!hasInput || (currentX > 0f && _moveInput.x >= 0f) || (currentX < 0f && _moveInput.x <= 0f) || Mathf.Abs(currentX) <= 0.05f) {
                SetTurnOver(false);
            }
        }
        else if (isOpposing) {
            SetTurnOver(true);
        }

        float rate;
        if (!hasInput) {
            rate = decelerationTime > 0.001f ? (maxSpeed / decelerationTime) : float.MaxValue;
        }
        else if (isTurningOver || (currentX > 0f && targetX < 0f) || (currentX < 0f && targetX > 0f)) {
            rate = decelerationTime > 0.001f ? (maxSpeed / decelerationTime) : float.MaxValue;
        }
        else {
            rate = accelerationTime > 0.001f ? (maxSpeed / accelerationTime) : float.MaxValue;
        }

        float newX = Mathf.MoveTowards(
            currentX,
            targetX,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        speed = Mathf.Abs(newX);
    }

    private void ApplyJump() {
        if (rb == null) return;

        // 1. Inicia o pulo se houver comando no buffer e estiver no chão (ou dentro do Coyote Time)
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f) {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            isJumping = true;
            _jumpCutRequested = false;
        }

        // 2. Pulo Variável (Jump Cut): corta a velocidade vertical se o jogador soltar o botão antes do ápice
        if (_jumpCutRequested) {
            _jumpCutRequested = false;

            if (rb.linearVelocity.y > 0f && isJumping) {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCutMultiplier
                );
            }
        }

        // 3. Ajuste Dinâmico de Gravidade (queda mais ágil e menos flutuante)
        if (rb.linearVelocity.y < 0f) {
            // Caindo: aplica o multiplicador de queda para dar peso e precisão
            rb.gravityScale = _defaultGravityScale * fallMultiplier;
            isJumping = false;
        }
        else if (rb.linearVelocity.y > 0f && !_jumpHeld) {
            // Subindo mas sem segurar o botão de pulo
            rb.gravityScale = _defaultGravityScale * fallMultiplier;
        }
        else {
            // Subindo segurando o botão ou parado no chão: gravidade padrão
            rb.gravityScale = _defaultGravityScale;
        }
    }

    private void SetTurnOver(bool active) {
        if (isTurningOver == active) return;

        isTurningOver = active;

        if (active && animator != null && _hasTurnOverTrigger) {
            animator.SetTrigger(_turnOverTriggerHash);
        }

        OnTurnOverChanged?.Invoke(active);
    }

    private void CacheAnimatorParameters() {
        if (animator == null) {
            animator = GetComponent<Animator>();

            if (animator == null) {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator != null &&
            animator.runtimeAnimatorController != null) {

            _turnOverBoolHash = Animator.StringToHash(turnOverBoolParam);

            _turnOverTriggerHash = !string.IsNullOrEmpty(turnOverTriggerParam)
                ? Animator.StringToHash(turnOverTriggerParam)
                : 0;

            _speedHash = Animator.StringToHash("Speed");
            _isMovingHash = Animator.StringToHash("IsMoving");

            _hasTurnOverBool = HasParameter(animator, _turnOverBoolHash);
            _hasTurnOverTrigger = _turnOverTriggerHash != 0 &&
                                  HasParameter(animator, _turnOverTriggerHash);

            _hasSpeedFloat = HasParameter(animator, _speedHash);
            _hasIsMovingBool = HasParameter(animator, _isMovingHash);

            _animatorInitialized = true;
        }
    }

    private void UpdateAnimator() {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;

        if (!_animatorInitialized) {
            CacheAnimatorParameters();
        }

        if (_hasTurnOverBool) {
            animator.SetBool(_turnOverBoolHash, isTurningOver);
        }

        if (_hasSpeedFloat) {
            animator.SetFloat(_speedHash, speed);
        }

        if (_hasIsMovingBool) {
            animator.SetBool(_isMovingHash, speed > 0.05f);
        }
    }

    private bool HasParameter(Animator anim, int hash) {
        if (anim == null || anim.runtimeAnimatorController == null) {
            return false;
        }

        foreach (var parameter in anim.parameters) {
            if (parameter.nameHash == hash) {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected() {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
