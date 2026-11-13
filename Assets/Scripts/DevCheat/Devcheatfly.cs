using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class DevCheatFly : MonoBehaviour {
    [Header("Entrada")]
    [Tooltip("Ação que liga/desliga o cheat")]
    public InputActionReference enableCheatAction;

    [Tooltip("Ação de movimento do cheat")]
    public InputActionReference cheatMoveAction;

    [Header("Movimento")]
    [Tooltip("Velocidade do voo livre")]
    [Min(0f)] public float flySpeed = 12f;

    [Header("Componentes")]
    [Tooltip("Se vazio, usa o PlayerController do mesmo objeto.")]
    public PlayerController controller;

    [Header("Status (leitura)")]
    public bool cheatActive;

    private Rigidbody2D _rb;
    private RigidbodyType2D _originalBodyType;
    private Vector2 _input;

    private void Awake() {
        if (!Debug.isDebugBuild) { enabled = false; return; }

        _rb = GetComponent<Rigidbody2D>();
        if (controller == null) controller = GetComponent<PlayerController>();
    }

    private void OnEnable() {
        if (enableCheatAction?.action != null) enableCheatAction.action.Enable();
        if (cheatMoveAction?.action != null) cheatMoveAction.action.Enable();
    }

    private void OnDisable() {
        if (cheatActive) SetCheat(false);

        if (enableCheatAction?.action != null) enableCheatAction.action.Disable();
        if (cheatMoveAction?.action != null) cheatMoveAction.action.Disable();
    }

    private void Update() {
        if (enableCheatAction?.action != null && enableCheatAction.action.WasPressedThisFrame())
            SetCheat(!cheatActive);

        _input = cheatActive && cheatMoveAction?.action != null
            ? cheatMoveAction.action.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private void FixedUpdate() {
        if (!cheatActive) return;
        _rb.linearVelocity = Vector2.ClampMagnitude(_input, 1f) * flySpeed;
    }

    private void SetCheat(bool on) {
        if (on == cheatActive) return;
        cheatActive = on;

        if (on) {
            _originalBodyType = _rb.bodyType;

            if (controller != null) {
                controller.StateMachine.ChangeState(controller.Fall);
                controller.enabled = false;
            }

            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = _originalBodyType;

            if (controller != null) controller.enabled = true;
        }
    }
}