using UnityEngine;

// Nomes dos estados no Animator Controller precisam ser idênticos
public static class PlayerAnim {
    public static readonly int Idle       = Animator.StringToHash("Idle");
    public static readonly int Run        = Animator.StringToHash("Run");
    public static readonly int TurnOver   = Animator.StringToHash("TurnOver");
    public static readonly int Rise       = Animator.StringToHash("Rise");
    public static readonly int Fall       = Animator.StringToHash("Fall");
    public static readonly int WallGrab   = Animator.StringToHash("WallGrab");
    public static readonly int WallSlide  = Animator.StringToHash("WallSlide");
    public static readonly int LedgeGrab  = Animator.StringToHash("LedgeGrab");
    public static readonly int LedgeClimb = Animator.StringToHash("LedgeClimb");
}

// -------------------------------------------  Base dos estados no chão -------------------------------------------
public abstract class GroundedState : PlayerState {
    protected GroundedState(PlayerController player) : base(player) { }

    public override void Enter() => player.SetGravityMultiplier(1f);

// Retorna true se trocou de estado
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

// ------------------------------------------- Idle ------------------------------------------- //
public class IdleState : GroundedState {
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.Idle);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;
        if (player.HasInput) player.StateMachine.ChangeState(player.Run);
    }

    public override void PhysicsTick() => player.MoveHorizontal(0f);
}

// ------------------------------------------- Run ------------------------------------------- //
public class RunState : GroundedState {
    private const float StopSpeed = 0.05f;

    public RunState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.Run);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;

        if (player.IsReversing(player.turnOverMinSpeed))
            player.StateMachine.ChangeState(player.TurnOver);
        else if (!player.HasInput && player.Speed <= StopSpeed)
            player.StateMachine.ChangeState(player.Idle);
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput);
}

// ------------------------------------------- TurnOver -------------------------------------------
public class TurnOverState : GroundedState {
    public TurnOverState(PlayerController player) : base(player) { }

    public override void Enter() {
        base.Enter();
        player.PlayAnimation(PlayerAnim.TurnOver);
    }

    public override void Tick() {
        if (CheckAirTransitions()) return;

        float vx    = player.VelocityX;
        float input = player.MoveInput;

        bool stopped      = Mathf.Abs(vx) <= 0.05f;
        bool sameDirection = (vx > 0f && input >= 0f) || (vx < 0f && input <= 0f);

        if (!player.HasInput || stopped || sameDirection) {
            bool keepMoving = player.HasInput || player.Speed > 0.05f;
            player.StateMachine.ChangeState(keepMoving ? (PlayerState)player.Run : player.Idle);
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput, braking: true);
}

// ------------------------------------------- Rise -------------------------------------------
public class RiseState : PlayerState {
    public RiseState(PlayerController player) : base(player) { }

    public override void Enter() {
        // Quando vem de um Wall Jump, a velocidade já foi definida por DoWallJump.
        // Não sobrescrevemos — apenas limpamos a flag e ajustamos a gravidade.
        if (player.IsFromWallJump) {
            player.ClearWallJumpFlag();
        } else {
            player.ConsumeJump();
            player.SetVelocityY(player.jumpForce);
        }
        player.SetGravityMultiplier(1f);
        player.PlayAnimation(PlayerAnim.Rise);
    }

    public override void Tick() {
        // Ledge Grab tem prioridade
        if (player.CheckLedge(out Vector2 corner, out int ledgeDir)) {
            player.GrabLedge(corner, ledgeDir);
            player.StateMachine.ChangeState(player.LedgeGrab);
            return;
        }

        // Encostar na parede subindo → Wall Grab ou Wall Slide
        if (!player.isGrounded && player.IsTouchingWall) {
            if (player.CanWallGrab(player.wallDirection)) {
                player.StateMachine.ChangeState(player.WallGrab);
                return;
            }
            if (player.CanWallSlide(player.wallDirection)) {
                player.StateMachine.ChangeState(player.WallSlide);
                return;
            }
        }

        if (player.VelocityY <= 0f)
            player.StateMachine.ChangeState(player.Fall);
    }

    public override void PhysicsTick() {
        player.MoveHorizontal(player.MoveInput);

        // Pulo variável: soltou o botão durante a subida
        if (player.ConsumeJumpRelease() && player.VelocityY > 0f)
            player.SetVelocityY(player.VelocityY * player.jumpCutMultiplier);

        player.SetGravityMultiplier(player.JumpHeld ? 1f : player.fallMultiplier);
    }
}

// ------------------------------------------- Fall -------------------------------------------
public class FallState : PlayerState {
    public FallState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.SetGravityMultiplier(player.fallMultiplier);
        player.PlayAnimation(PlayerAnim.Fall);
    }

    public override void Tick() {
        // Pousou
        if (player.isGrounded) {
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

        // Encostou na parede caindo → Wall Grab ou Wall Slide
        if (player.IsTouchingWall) {
            if (player.CanWallGrab(player.wallDirection)) {
                player.StateMachine.ChangeState(player.WallGrab);
                return;
            }
            if (player.CanWallSlide(player.wallDirection)) {
                player.StateMachine.ChangeState(player.WallSlide);
                return;
            }
        }
    }

    public override void PhysicsTick() => player.MoveHorizontal(player.MoveInput);
}

// ------------------------------------------- WallGrab -------------------------------------------

//   1- Encostar na parede entra em WallGrab.
//   2- WallGrab trava o jogador por <wallGrabDuration> com gravity 0.
//   3- Se pular durante o WallGrab ativa DoWallJump
//   4- Se o tempo acabar entra em WallSlide
//   5- WallSlide: pode pular ou soltar.
//   6- Após o Wall Jump, o controle horizontal fica travado por <wallJumpControlLockDuration>.
// Passado esse tempo, o jogador tem controle total novamente podendo virar de direção.

public class WallGrabState : PlayerState {
    private int _wallDir;        // lado da parede ao entrar no estado

    public WallGrabState(PlayerController player) : base(player) { }

    public override void Enter() {
        _wallDir = player.wallDirection;

        // Se a parede for diferente da anterior, reseta o tempo de grab
        if (_wallDir != player.LastGrabbedWallSide) {
            player.ResetWallGrabTimer(_wallDir);
        }

        player.rb.linearVelocity = Vector2.zero;
        player.SetGravityMultiplier(0f);
        player.PlayAnimation(PlayerAnim.WallGrab);
    }

    public override void Tick() {
        // Pousou no chão enquanto travado
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

        // --- Wall Jump ---
        // O salto é sempre para o lado OPOSTO à parede, ignorando o input.
        if (player.HasJumpBuffer && player.CanWallJump(_wallDir)) {
            player.DoWallJump(_wallDir);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }

        // Desencostou da parede (ex.: quina) → cai normalmente
        if (!player.IsTouchingWall) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Regra de input: o jogador precisa segurar em direção à parede para ficar agarrado.
        // Durante o lockout de wall jump (player foi arremessado para esta parede pelo sistema),
        // não exigimos o input para evitar soltar imediatamente ao chegar na parede oposta.
        if (!player.IsWallJumpLocked) {
            bool holdingTowards = (_wallDir > 0 && player.MoveInput > 0.1f) ||
                                  (_wallDir < 0 && player.MoveInput < -0.1f);
            if (!holdingTowards) {
                player.StateMachine.ChangeState(player.Fall);
                return;
            }
        }

        // Decrementa o tempo de grab da parede atual
        player.CurrentWallGrabTimer -= Time.deltaTime;
        if (player.CurrentWallGrabTimer <= 0f) {
            player.CurrentWallGrabTimer = 0f;
            player.StateMachine.ChangeState(player.WallSlide);
            return;
        }
    }

    public override void PhysicsTick() {
        // Mantém o jogador colado na parede (velocidade zero)
        player.rb.linearVelocity = Vector2.zero;
    }

    public override void Exit() {
        player.SetGravityMultiplier(1f);
    }
}

// ------------------------------------------- WallSlide -------------------------------------------

// Desliza lento pela parede após o Wall Grab expirar.
public class WallSlideState : PlayerState {
    private int _wallDir;

    public WallSlideState(PlayerController player) : base(player) { }

    public override void Enter() {
        _wallDir = player.wallDirection;
        // Ao entrar em slide, consome o tempo de grab desta parede para não permitir re-grab nela
        player.DepleteWallGrabTimer(_wallDir);
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

        // Wall Jump: lança para o lado oposto, independente do input
        if (player.HasJumpBuffer && player.CanWallJump(_wallDir)) {
            player.DoWallJump(_wallDir);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }

        // Desencostou da parede
        if (!player.IsTouchingWall) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // No WallSlide também exige segurar em direção à parede para continuar
        bool holdingTowards = (_wallDir > 0 && player.MoveInput > 0.1f) ||
                              (_wallDir < 0 && player.MoveInput < -0.1f);
        if (!holdingTowards) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }
    }

    public override void PhysicsTick() {
        // Limita a velocidade de queda ao deslizar
        float vy = Mathf.Max(player.rb.linearVelocity.y, -player.wallSlideSpeed);
        player.rb.linearVelocity = new Vector2(0f, vy);
    }
}

// ------------------------------------------- LedgeGrab ------------------------------------------- 
public class LedgeGrabState : PlayerState {
    public LedgeGrabState(PlayerController player) : base(player) { }

    public override void Enter() => player.PlayAnimation(PlayerAnim.LedgeGrab);

    public override void Tick() {
        int   dir    = player.LedgeDir;
        float inputX = player.MoveInput;

        // Subir: andar para o lado da borda
        bool walkTowardsLedge = (dir > 0 && inputX > 0.1f) || (dir < 0 && inputX < -0.1f);
        if (walkTowardsLedge) {
            player.StateMachine.ChangeState(player.LedgeClimb);
            return;
        }

        // Soltar: para baixo ou direção contrária
        bool dropDown = player.MoveInputY < -0.5f ||
                        (dir > 0 && inputX < -0.3f) ||
                        (dir < 0 && inputX >  0.3f);
        if (dropDown) {
            player.StartLedgeCooldown();
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Pular da quina (Wall Jump)
        if (player.HasJumpBuffer && player.CanWallJump(dir)) {
            player.StartLedgeCooldown();
            player.DoWallJump(dir);
            player.StateMachine.ChangeState(player.Rise);
            return;
        }
    }

    public override void PhysicsTick() => player.rb.linearVelocity = Vector2.zero;

    public override void Exit() => player.SetGravityMultiplier(1f);
}

// ------------------------------------------- LedgeClimb ------------------------------------------- //
public class LedgeClimbState : PlayerState {
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private float   _timer;

    public LedgeClimbState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.SetGravityMultiplier(0f);
        player.rb.linearVelocity = Vector2.zero;
        player.PlayAnimation(PlayerAnim.LedgeClimb);

        _startPos  = player.transform.position;
        _targetPos = new Vector2(
            player.LedgeCorner.x + player.LedgeDir * player.ledgeClimbOffset.x,
            player.LedgeCorner.y + player.ledgeClimbOffset.y);
        _timer = 0f;
    }

    public override void PhysicsTick() {
        _timer += Time.fixedDeltaTime;
        float duration = Mathf.Max(0.05f, player.ledgeClimbDuration);
        float t        = Mathf.Clamp01(_timer / duration);

        player.rb.MovePosition(Vector2.Lerp(_startPos, _targetPos, t));

        if (t >= 1f) {
            player.transform.position    = _targetPos;
            player.rb.linearVelocity     = Vector2.zero;
            player.StartLedgeCooldown();

            player.StateMachine.ChangeState(player.HasInput ? (PlayerState)player.Run : player.Idle);
        }
    }

    public override void Exit() => player.SetGravityMultiplier(1f);
}
