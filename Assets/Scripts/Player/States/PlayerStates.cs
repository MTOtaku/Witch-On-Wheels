using UnityEngine;

// Nomes dos estados no Animator Controller (precisam ser idênticos)
public static class PlayerAnim {
    public static readonly int Idle = Animator.StringToHash("Idle");
    public static readonly int Run = Animator.StringToHash("Run");
    public static readonly int TurnOver = Animator.StringToHash("TurnOver");
    public static readonly int Rise = Animator.StringToHash("Rise");
    public static readonly int Fall = Animator.StringToHash("Fall");
    public static readonly int WallSlide = Animator.StringToHash("WallSlide");
    public static readonly int LedgeGrab = Animator.StringToHash("LedgeGrab");
    public static readonly int LedgeClimb = Animator.StringToHash("LedgeClimb");
}

// ---------- Base dos estados no chão ----------
public abstract class GroundedState : PlayerState {
    protected GroundedState(PlayerController player) : base(player) { }

    public override void Enter() => player.SetGravityMultiplier(1f);

    protected bool CheckAirTransitions() {
        if (player.WantsToJump) {
            player.StateMachine.ChangeState(player.Rise);
            return true;
        }

        if (!player.isGrounded && !player.HasCoyoteTime) {
            player.StateMachine.ChangeState(player.Fall);
            return true;
        }

        return false;
    }
}

public class IdleState : GroundedState {
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.Idle);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;

        if (player.HasInput) {
            player.StateMachine.ChangeState(player.Run);
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(0f);
}

public class RunState : GroundedState {
    private const float StopSpeed = 0.05f;

    public RunState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.Run);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;

        if (player.IsReversing(player.turnOverMinSpeed)) {
            player.StateMachine.ChangeState(player.TurnOver);
        }
        else if (!player.HasInput && player.Speed <= StopSpeed) {
            player.StateMachine.ChangeState(player.Idle);
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput);
}

// Virada brusca / derrapagem
public class TurnOverState : GroundedState {
    public TurnOverState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.TurnOver);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;

        float vx = player.VelocityX;
        float input = player.MoveInput;

        bool stopped = Mathf.Abs(vx) <= 0.05f;
        bool sameDirection = (vx > 0f && input >= 0f) || (vx < 0f && input <= 0f);

        if (!player.HasInput || stopped || sameDirection) {
            bool keepMoving = player.HasInput || player.Speed > 0.05f;
            player.StateMachine.ChangeState(keepMoving ? (PlayerState)player.Run : player.Idle);
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput, braking: true);
}

// ---------- No ar ----------
public class RiseState : PlayerState {
    public RiseState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.ConsumeJump();
        player.SetVelocityY(player.jumpForce);
        player.SetGravityMultiplier(1f);
        player.PlayAnimation(PlayerAnim.Rise);
    }

    public override void Tick() {
        // Ledge Grab: agarra a borda se encontrar uma quina
        if (player.CheckLedge(out Vector2 corner, out int ledgeDir)) {
            player.GrabLedge(corner, ledgeDir);
            player.StateMachine.ChangeState(player.LedgeGrab);
            return;
        }

        // Wall Jump no ar ao tocar a parede (alternando o lado)
        if (player.HasJumpBuffer && player.IsTouchingWall && player.CanWallJump(player.WallDirection)) {
            player.WallJump(player.WallDirection);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }

        if (player.VelocityY <= 0f) {
            player.StateMachine.ChangeState(player.Fall);
        }
    }

    public override void PhysicsTick() {
        player.MoveHorizontal(player.MoveInput);

        // Pulo variável: soltou o botão durante a subida
        if (player.ConsumeJumpRelease() && player.VelocityY > 0f) {
            player.SetVelocityY(player.VelocityY * player.jumpCutMultiplier);
        }

        // Subindo sem segurar o botão: gravidade de queda
        player.SetGravityMultiplier(player.JumpHeld ? 1f : player.fallMultiplier);
    }
}

public class FallState : PlayerState {
    public FallState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.SetGravityMultiplier(player.fallMultiplier);
        player.PlayAnimation(PlayerAnim.Fall);
    }

    public override void Tick() {
        if (player.isGrounded) {
            // Pulo com buffer: apertou um instante antes de aterrissar
            if (player.WantsToJump) {
                player.StateMachine.ChangeState(player.Rise);
                return;
            }

            bool moving = player.HasInput || player.Speed > 0.05f;
            player.StateMachine.ChangeState(moving ? (PlayerState)player.Run : player.Idle);
            return;
        }

        // Ledge Grab
        if (player.CheckLedge(out Vector2 corner, out int ledgeDir)) {
            player.GrabLedge(corner, ledgeDir);
            player.StateMachine.ChangeState(player.LedgeGrab);
            return;
        }

        // Wall Jump enquanto cai
        if (player.HasJumpBuffer && player.IsTouchingWall && player.CanWallJump(player.WallDirection)) {
            player.WallJump(player.WallDirection);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }

        // Se encostar na parede descendo, entra em deslize (WallSlide)
        if (player.IsTouchingWall && (player.MoveInput != 0f || player.VelocityY < 0f)) {
            player.StateMachine.ChangeState(player.WallSlide);
            return;
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput);
}

// ---------- Parede (Wall Slide) ----------
public class WallSlideState : PlayerState {
    public WallSlideState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.SetGravityMultiplier(1f);
        player.PlayAnimation(PlayerAnim.WallSlide);
    }

    public override void Tick() {
        if (player.isGrounded) {
            player.StateMachine.ChangeState(player.HasInput ? (PlayerState)player.Run : player.Idle);
            return;
        }

        // Ledge Grab tem prioridade
        if (player.CheckLedge(out Vector2 corner, out int ledgeDir)) {
            player.GrabLedge(corner, ledgeDir);
            player.StateMachine.ChangeState(player.LedgeGrab);
            return;
        }

        // Wall Jump: pula para longe da parede se for do lado alternado
        if (player.HasJumpBuffer && player.CanWallJump(player.WallDirection)) {
            player.WallJump(player.WallDirection);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }

        // Desencostou da parede
        if (!player.IsTouchingWall) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Se o jogador pressionar para a direção oposta da parede
        if ((player.WallDirection > 0 && player.MoveInput < -0.2f) ||
            (player.WallDirection < 0 && player.MoveInput > 0.2f)) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }
    }

    public override void PhysicsTick() {
        // Limita a velocidade de queda ao deslizar pela parede
        float vy = Mathf.Max(player.rb.linearVelocity.y, -player.wallSlideSpeed);
        player.rb.linearVelocity = new Vector2(0f, vy);
    }
}

// ---------- Borda / Quina (Ledge Grab & Climb) ----------
public class LedgeGrabState : PlayerState {
    public LedgeGrabState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.PlayAnimation(PlayerAnim.LedgeGrab);
    }

    public override void Tick() {
        int dir = player.LedgeDir;
        float inputX = player.MoveInput;

        // Subir na plataforma usando o botão de andar para o lado da borda
        bool walkTowardsLedge = (dir > 0 && inputX > 0.1f) || (dir < 0 && inputX < -0.1f);
        if (walkTowardsLedge) {
            player.StateMachine.ChangeState(player.LedgeClimb);
            return;
        }

        // Soltar da borda: apertou para baixo ou na direção contrária
        bool dropDown = player.MoveInputY < -0.5f || (dir > 0 && inputX < -0.3f) || (dir < 0 && inputX > 0.3f);
        if (dropDown) {
            player.StartLedgeCooldown();
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Pular da quina (Wall Jump afastando-se da parede)
        if (player.HasJumpBuffer && player.CanWallJump(dir)) {
            player.StartLedgeCooldown();
            player.WallJump(dir);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }
    }

    public override void PhysicsTick() {
        player.rb.linearVelocity = Vector2.zero;
    }

    public override void Exit() {
        player.SetGravityMultiplier(1f);
    }
}

public class LedgeClimbState : PlayerState {
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private float _timer;

    public LedgeClimbState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.SetGravityMultiplier(0f);
        player.rb.linearVelocity = Vector2.zero;
        player.PlayAnimation(PlayerAnim.LedgeClimb);

        _startPos = player.transform.position;
        _targetPos = new Vector2(
            player.LedgeCorner.x + player.LedgeDir * player.ledgeClimbOffset.x,
            player.LedgeCorner.y + player.ledgeClimbOffset.y
        );
        _timer = 0f;
    }

    public override void PhysicsTick() {
        _timer += Time.fixedDeltaTime;
        float duration = Mathf.Max(0.05f, player.ledgeClimbDuration);
        float t = Mathf.Clamp01(_timer / duration);

        player.rb.MovePosition(Vector2.Lerp(_startPos, _targetPos, t));

        if (t >= 1f) {
            player.transform.position = _targetPos;
            player.rb.linearVelocity = Vector2.zero;
            player.StartLedgeCooldown();

            bool keepMoving = player.HasInput;
            player.StateMachine.ChangeState(keepMoving ? (PlayerState)player.Run : player.Idle);
        }
    }

    public override void Exit() {
        player.SetGravityMultiplier(1f);
    }
}
