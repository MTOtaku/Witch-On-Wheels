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
    public static readonly int Grind      = Animator.StringToHash("Grind");
}

// -------------------------------------------  Base dos estados no chão -------------------------------------------
public abstract class GroundedState : PlayerState {
    protected GroundedState(PlayerController player) : base(player) { }

    public override void Enter() => player.SetGravityMultiplier(1f);

    // Retorna true se trocou de estado
    protected bool CheckAirTransitions() {
        if (player.TryEnterGrind()) return true;

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

        // Rail Grind
        if (player.TryEnterGrind()) return;

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
        
        // Rail Grind
        if (player.TryEnterGrind()) return;
        
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

// ------------------------------------------- Grind -------------------------------------------
public class GrindState : PlayerState {
    // Limite da velocidade usada para encaixar no trilho.
    // Evita deslocamentos excessivos caso o Player esteja longe do ponto.
    private const float MaxSnapVelocity = 60f;

    private RailPath _rail;
    private float _distance;
    private int _dir = 1;          // +1 ou -1 ao longo do trilho
    private float _speed;          // velocidade ao longo do trilho
    private float _height;         // distância do pivô do Player até os pés
    private float _prevInputY;
    private Transform _visual;
    private float _boostTimer;
    private float _boostCooldownTimer;

    public GrindState(PlayerController player) : base(player) { }

    public override void Enter() {
        _rail = player.GrindRail;
        _distance = player.GrindStartDistance;
        _height = player.PivotToFeet;
        _prevInputY = player.MoveInputY;
        _visual = player.RailVisualRoot;

        if (_rail == null || !_rail.IsValid) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        Vector2 tangent = _rail.GetTangent(_distance);

        // Direção: movimento horizontal atual ou input.
        // Se estiver parado, começa para a direita.
        float vx = Mathf.Abs(player.VelocityX) > 0.5f
            ? player.VelocityX
            : (player.HasInput ? player.MoveInput : 0f);

        if (vx != 0f)
            _dir = vx * tangent.x >= 0f ? 1 : -1;
        else
            _dir = tangent.x >= 0f ? 1 : -1;

        // Preserva o embalo que o Player já tinha ao entrar no trilho.
        float along = Vector2.Dot(
            player.rb.linearVelocity,
            tangent * _dir
        );

        _speed = Mathf.Clamp(
            along,
            player.railMinSpeed,
            player.railMaxSpeed
        );

        player.SetGravityMultiplier(0f);
        player.PlayAnimation(PlayerAnim.Grind);
    }

    public override void Tick() {
        // Trilho desativado ou destruído.
        if (_rail == null || !_rail.IsValid) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Pula para fora do trilho, seguindo a direção do sprite.
        if (player.HasJumpBuffer) {
            player.StoreRailReturnSpeed(_speed);
            
            Vector2 railVelocity = TravelVelocity();
            
            player.StateMachine.ChangeState(player.Rise);
            
            Vector2 jumpDirection = _visual != null ? (Vector2)_visual.up : Vector2.up;

            if (jumpDirection.y < 0f)
                jumpDirection *= -1f;

            jumpDirection.Normalize();

            player.rb.linearVelocity = railVelocity + jumpDirection * player.railJumpForce;
            return;
        }

        // Cima / baixo: detecta apenas o momento em que o botão é apertado.
        float y = player.MoveInputY;

        bool upPressed = y > 0.5f && _prevInputY <= 0.5f;
        bool downPressed = y < -0.5f && _prevInputY >= -0.5f;

        _prevInputY = y;

        // Tenta trocar para o trilho acima.
        if (upPressed && TrySwitchRail(+1))
            return;

        // Tenta trocar para o trilho abaixo.
        if (downPressed) {
            if (TrySwitchRail(-1))
                return;

            // Não existe trilho abaixo: solta.
            player.rb.linearVelocity = TravelVelocity();
            player.StateMachine.ChangeState(player.Fall);
            return;
        }
        
        // Ativa o boost caso esteja disponível.
        if (player.RailBoostPressed &&
            _boostTimer <= 0f &&
            _boostCooldownTimer <= 0f) {

            _boostTimer = player.railBoostDuration;
        }

        TiltVisual();
    }

    public override void PhysicsTick() {
    if (_rail == null || !_rail.IsValid) return;

    float dt = Time.fixedDeltaTime;

    // Atualiza o tempo do boost e inicia o cooldown ao terminar.
    if (_boostTimer > 0f) {
        _boostTimer -= dt;

        if (_boostTimer <= 0f) {
            _boostTimer = 0f;
            _boostCooldownTimer = player.railBoostCooldown;
        }
    }
    else if (_boostCooldownTimer > 0f) {
        _boostCooldownTimer -= dt;
    }

    Vector2 tangent = _rail.GetTangent(_distance) * _dir;

    bool boosting = _boostTimer > 0f;

    // Verifica se o jogador está pressionando na direção do movimento.
    bool pushing = player.HasInput &&
                   player.MoveInput * tangent.x > 0f;
    
    float target;
    float rate;

    if (boosting) {
        target = player.railBoostMaxSpeed;
        rate = player.railBoostAcceleration;
    }
    else {
        target = pushing ? player.railMaxSpeed : player.railMinSpeed;

        // Se ainda estiver acima da velocidade normal após o boost, desacelera gradualmente até voltar ao limite normal.
        if (_speed > player.railMaxSpeed)
            rate = player.railBoostEndDeceleration;
        else
            rate = pushing ? player.railAcceleration : player.railDeceleration;
    }

    _speed = Mathf.MoveTowards(_speed, target, rate * dt);

    // A inclinação continua influenciando a velocidade.
    _speed -= tangent.y * player.railSlopeAcceleration * dt;

    // Durante o boost, permite alcançar uma velocidade maior, ao terminar, não corta instantaneamente a velocidade excedente.
    float speedLimit = boosting
        ? player.railBoostMaxSpeed
        : Mathf.Max(player.railMaxSpeed, _speed);

    _speed = Mathf.Clamp(_speed, player.railMinSpeed, speedLimit);

    // Avança pelo trilho.
    _distance += _dir * _speed * dt;

    // Chegou na ponta: sai com a velocidade da tangente.
    if (_distance <= 0f || _distance >= _rail.Length) {
        _distance = Mathf.Clamp(_distance, 0f, _rail.Length);

        player.rb.linearVelocity = TravelVelocity();
        player.StateMachine.ChangeState(player.Fall);
        return;
    }

    // Mantém o jogador encaixado no trilho.
    Vector2 targetPos = _rail.GetPoint(_distance) + Vector2.up * _height;
    Vector2 velocity = (targetPos - player.rb.position) / dt;

    player.rb.linearVelocity = Vector2.ClampMagnitude(velocity, MaxSnapVelocity);
}

    public override void Exit() {
        player.SetGravityMultiplier(1f);
        player.StartRailCooldown();

        // Restaura a rotação original do sprite.
        if (_visual != null)
            _visual.localRotation = Quaternion.identity;
    }

    // Velocidade no sentido do movimento, usada ao soltar do trilho.
    private Vector2 TravelVelocity() {
        return _rail.GetTangent(_distance) * (_dir * _speed);
    }

    // Troca para o trilho de cima (+1) ou de baixo (-1).
    // Mantém o sentido e a velocidade do movimento.
    private bool TrySwitchRail(int vertical) {
        if (!player.FindNeighborRail(
                _rail,
                vertical,
                out RailPath next,
                out float nextDistance))
            return false;

        Vector2 travel = _rail.GetTangent(_distance) * _dir;
        Vector2 nextTangent = next.GetTangent(nextDistance);

        _rail = next;
        _distance = nextDistance;

        // Ajusta a direção para preservar o sentido do movimento.
        _dir = travel.x * nextTangent.x >= 0f ? 1 : -1;

        return true;
    }

    // Inclina somente o sprite. O Rigidbody não gira.
    private void TiltVisual() {
        if (!player.railTiltVisual || _visual == null)
            return;

        Vector2 tangent = _rail.GetTangent(_distance) * _dir;

        float angle = Mathf.Atan2(
            tangent.y * Mathf.Sign(tangent.x),
            Mathf.Abs(tangent.x)
        ) * Mathf.Rad2Deg;

        _visual.localRotation = Quaternion.Slerp(
            _visual.localRotation,
            Quaternion.Euler(0f, 0f, angle),
            player.railTiltSpeed * Time.deltaTime
        );
    }
}
