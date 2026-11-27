using System;

public abstract class PlayerState {
    protected readonly PlayerController player;

    protected PlayerState(PlayerController player) {
        this.player = player;
    }

    public virtual void Enter() { }        
    public virtual void Exit() { }         
    public virtual void Tick() { }         
    public virtual void PhysicsTick() { }  
}

public class PlayerStateMachine {
    public PlayerState Current { get; private set; }
    public PlayerState Previous { get; private set; }

    // (Estado anterior, novo estado). Útil para VFX, sons e câmera.
    public event Action<PlayerState, PlayerState> StateChanged;

    public void Initialize(PlayerState startState) {
        if (startState == null || Current != null) return;

        Current = startState;
        Current.Enter();

        StateChanged?.Invoke(null, Current);
    }

    public void ChangeState(PlayerState next) {
        if (next == null || next == Current) return;

        PlayerState previous = Current;

        previous?.Exit();

        Previous = previous;
        Current = next;

        Current.Enter();

        StateChanged?.Invoke(previous, next);
    }

    public void Tick() {
        Current?.Tick();
    }

    public void PhysicsTick() {
        Current?.PhysicsTick();
    }
}