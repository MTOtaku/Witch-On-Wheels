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

    [Header("Direção e Eixos")]
    [Tooltip("Marque para movimentação nos eixos X e Y (Top-Down). Desmarque para controlar apenas o eixo X em jogos de visão lateral/plataforma com gravidade.")]
    public bool moveVertical = false;

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
#endif

    [Header("Status em Tempo Real (Leitura)")]
    [Tooltip("Velocidade linear atual do jogador.")]
    public float speed;

    [Tooltip("Taxa de aceleração atual calculada em unidades/s² (maxSpeed / accelerationTime).")]
    public float accelerationSpeed;

    [Tooltip("Valor da entrada de movimento no eixo horizontal (-1 a 1).")]
    public float move;

    [Tooltip("Indica se o jogador está executando a animação/estado de Turn Over.")]
    public bool isTurningOver;

    
    // Evento disparado quando o estado de Turn Over muda.
    
    public System.Action<bool> OnTurnOverChanged;

    private Vector2 _moveInput;
    private int _turnOverBoolHash;
    private int _turnOverTriggerHash;
    private int _speedHash;
    private int _isMovingHash;

    private bool _hasTurnOverBool;
    private bool _hasTurnOverTrigger;
    private bool _hasSpeedFloat;
    private bool _hasIsMovingBool;
    private bool _animatorInitialized;

    private void Awake(){
        if (rb == null) {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.freezeRotation = true;

        CacheAnimatorParameters();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable(){
        if (moveAction != null && moveAction.action != null) {
            moveAction.action.Enable();
        }
    }

    private void OnDisable(){
        if (moveAction != null && moveAction.action != null) {
            moveAction.action.Disable();
        }
    }
#endif

    private void Update(){
        ReadInput();
        accelerationSpeed = accelerationTime > 0.001f ? (maxSpeed / accelerationTime) : float.MaxValue;
        UpdateAnimator();
    }

    private void FixedUpdate(){
        ApplyMovement();
    }

    private void ReadInput(){
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // 1. Tenta ler via InputActionReference, se atribuído no Inspector
        if (moveAction != null && moveAction.action != null) {
            input = moveAction.action.ReadValue<Vector2>();
        }
        else {
            // 2. Leitura direta de teclado via New Input System
            if (Keyboard.current != null) {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    input.x += 1f;

                if (moveVertical) {
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                        input.y += 1f;
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                        input.y -= 1f;
                }
            }

            // 3. Leitura direta de Gamepad via New Input System
            if (Gamepad.current != null) {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) {
                    input = stick;
                }
            }
        }
#else
        input.x = Input.GetAxisRaw("Horizontal");
        if (moveVertical){
            input.y = Input.GetAxisRaw("Vertical");
        }
#endif

        if (!moveVertical) {
            input.y = 0f;
        }

        // Normaliza para não se mover mais rápido na diagonal
        _moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        move = _moveInput.x;
    }

    private void ApplyMovement(){
        if (rb == null) return;

        if (moveVertical) {
            // Movimentação bidimensional (Top-Down)
            Vector2 currentVelocity = rb.linearVelocity;
            Vector2 targetVelocity = _moveInput * maxSpeed;
            bool hasInput = _moveInput.sqrMagnitude > 0.001f;

            bool isOpposing = currentVelocity.magnitude > turnOverMinSpeed &&
                              hasInput &&
                              Vector2.Dot(currentVelocity.normalized, _moveInput.normalized) < -0.5f;

            if (isTurningOver) {
                if (!hasInput || Vector2.Dot(currentVelocity, _moveInput) >= 0f || currentVelocity.magnitude <= 0.05f) {
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
            else if (isTurningOver || Vector2.Dot(currentVelocity, targetVelocity) < 0f) {
                // Inversão brusca: desacelera para frear antes de reverter o sentido
                rate = decelerationTime > 0.001f ? (maxSpeed / decelerationTime) : float.MaxValue;
            }
            else {
                rate = accelerationTime > 0.001f ? (maxSpeed / accelerationTime) : float.MaxValue;
            }

            rb.linearVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            speed = rb.linearVelocity.magnitude;
        }
        else {
            // Movimentação unidimensional no eixo X (visão lateral / plataforma)
            float currentX = rb.linearVelocity.x;
            float targetX = _moveInput.x * maxSpeed;
            bool hasInput = Mathf.Abs(_moveInput.x) > 0.001f;

            // Detecta tentativa de mover no sentido oposto com velocidade mínima
            bool isOpposing = (currentX > turnOverMinSpeed && _moveInput.x < -0.1f) ||
                              (currentX < -turnOverMinSpeed && _moveInput.x > 0.1f);

            if (isTurningOver) {
                // Conclui o Turn Over quando a velocidade freia próxima de zero ou quando o comando muda
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
                // Inversão brusca: desacelera antes de acelerar no novo sentido
                rate = decelerationTime > 0.001f ? (maxSpeed / decelerationTime) : float.MaxValue;
            }
            else {
                rate = accelerationTime > 0.001f ? (maxSpeed / accelerationTime) : float.MaxValue;
            }

            float newX = Mathf.MoveTowards(currentX, targetX, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
            speed = Mathf.Abs(newX);
        }
    }

    private void SetTurnOver(bool active){
        if (isTurningOver == active) return;

        isTurningOver = active;

        if (active && animator != null && _hasTurnOverTrigger) {
            animator.SetTrigger(_turnOverTriggerHash);
        }

        OnTurnOverChanged?.Invoke(active);
    }

    private void CacheAnimatorParameters(){
        if (animator == null) {
            animator = GetComponent<Animator>();
            if (animator == null) {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (animator != null && animator.runtimeAnimatorController != null) {
            _turnOverBoolHash = Animator.StringToHash(turnOverBoolParam);
            _turnOverTriggerHash = !string.IsNullOrEmpty(turnOverTriggerParam) ? Animator.StringToHash(turnOverTriggerParam) : 0;
            _speedHash = Animator.StringToHash("Speed");
            _isMovingHash = Animator.StringToHash("IsMoving");

            _hasTurnOverBool = HasParameter(animator, _turnOverBoolHash);
            _hasTurnOverTrigger = _turnOverTriggerHash != 0 && HasParameter(animator, _turnOverTriggerHash);
            _hasSpeedFloat = HasParameter(animator, _speedHash);
            _hasIsMovingBool = HasParameter(animator, _isMovingHash);
            _animatorInitialized = true;
        }
    }

    private void UpdateAnimator(){
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

    private bool HasParameter(Animator anim, int hash){
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var p in anim.parameters) {
            if (p.nameHash == hash) return true;
        }
        return false;
    }
}
