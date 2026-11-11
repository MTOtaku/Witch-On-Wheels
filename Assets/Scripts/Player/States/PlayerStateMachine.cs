using System;

public abstract class PlayerState {
    protected readonly PlayerController player;

    protected PlayerState(PlayerController player) {
        this.player = player;
    }

    public virtual void Enter() { }        // ao entrar no estado
    public virtual void Exit() { }         // ao sair do estado
    public virtual void Tick() { }         // Update: decide as transições
    public virtual void PhysicsTick() { }  // FixedUpdate: mexe no Rigidbody2D
}

public class PlayerStateMachine {
    public PlayerState Current { get; private set; }
    public PlayerState Previous { get; private set; }

    // (estado anterior, novo estado). Ouça isto para VFX, som, câmera...
    public event Action<PlayerState, PlayerState> StateChanged;

    public void Initialize(PlayerState startState) {
        Current = startState;
        Current.Enter();
        StateChanged?.Invoke(null, Current);
    }

    // Dentro de um Tick, dê "return" logo depois de chamar isto
    public void ChangeState(PlayerState next) {
        if (next == null || next == Current) return;

        PlayerState previous = Current;
        previous?.Exit();

        Previous = previous;
        Current = next;
        Current.Enter();

        StateChanged?.Invoke(previous, next);
    }

    public void Tick() => Current?.Tick();
    public void PhysicsTick() => Current?.PhysicsTick();
}