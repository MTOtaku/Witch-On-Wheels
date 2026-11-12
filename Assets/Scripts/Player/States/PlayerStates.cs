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
    public static readonly int RailSwitch = Animator.StringToHash("RailSwitch");   // ainda sem animação: usa a de pulo (Rise)
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
// Rail Grinding:
//   - Anda por DISTÂNCIA no RailPath, com a gravidade em 0. Só este estado mexe no Rigidbody2D.
//   - Velocidade FIXA (railCruiseSpeed). Apertar para o lado oposto freia, vira e acelera de novo.
//   - Boost (botão): impulso para o lado em que o Player aponta; tempo e cooldown ficam no PlayerController.
//   - Pulo: sai mantendo a velocidade (embalo). Baixo + Pulo: desce do trilho.
//   - O tempo no trilho é limitado (com upgrades). Acabou o tempo -> solta o Player.
public class GrindState : PlayerState {
    // Limite da velocidade usada para encaixar no trilho.
    // Evita deslocamentos excessivos caso o Player esteja longe do ponto.
    private const float MaxSnapVelocity = 60f;

    // Abaixo disso o trecho do trilho é considerado vertical (o lado esquerda/direita não decide o sentido)
    private const float VerticalTangentThreshold = 0.05f;

    private RailPath _rail;
    private float _distance;
    private float _speed;          // velocidade COM SINAL ao longo do trilho (+ = rumo ao fim, - = rumo ao início)
    private int _side = 1;         // lado do mundo para onde o Player aponta: +1 direita, -1 esquerda
    private float _height;         // distância do pivô do Player até os pés
    private float _prevInputY;
    private Transform _visual;

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

        // Lado: input atual, senão o movimento horizontal. Parado: o lado para onde o trilho segue.
        if (player.HasInput)                       _side = player.MoveInput > 0f ? 1 : -1;
        else if (Mathf.Abs(player.VelocityX) > 0.5f) _side = player.VelocityX > 0f ? 1 : -1;
        else                                       _side = tangent.x >= 0f ? 1 : -1;

        float alongSign = SideToAlongSign(tangent);

        // Velocidade ao entrar: a guardada do pulo (volta com a mesma velocidade) ou o embalo atual
        float magnitude;
        if (player.TryConsumeRailReturnSpeed(out float stored)) {
            magnitude = stored;
        }
        else {
            magnitude = Mathf.Max(0f, Vector2.Dot(player.rb.linearVelocity, tangent * alongSign));
        }

        magnitude = Mathf.Min(magnitude, Mathf.Max(player.railBoostMaxSpeed, player.RailCruiseSpeed));
        _speed = alongSign * magnitude;

        player.ClearRailMomentum();   // a partir daqui quem controla é o trilho
        player.SetRailGrinding(true);
        player.SetGravityMultiplier(0f);
        player.PlayAnimation(PlayerAnim.Grind);
    }

    public override void Tick() {
        // Trilho desativado ou destruído.
        if (_rail == null || !_rail.IsValid) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        // Acabou o tempo de trilho: solta o Player mantendo a velocidade.
        if (!player.DrainRailTime(Time.deltaTime)) {
            Release(player.Fall);
            return;
        }

        // Pulo. Com Baixo segurado, desce do trilho em vez de pular.
        if (player.HasJumpBuffer) {
            if (player.MoveInputY < -player.railDropInputThreshold) {
                player.ConsumeJump();
                player.IgnoreRailTemporarily(_rail, player.railDropIgnoreTime);
                Release(player.Fall, dropDown: true);
            }
            else {
                Release(player.Rise, jump: true);
            }
            return;
        }

        // Cima / baixo: detecta apenas o momento em que o botão é apertado.
        float y = player.MoveInputY;

        bool upPressed = y > 0.5f && _prevInputY <= 0.5f;
        bool downPressed = y < -0.5f && _prevInputY >= -0.5f;

        _prevInputY = y;

        if (upPressed && TrySwitchRail(+1)) return;
        if (downPressed && player.railDownSwitchesRail && TrySwitchRail(-1)) return;

        // Boost: só liga se não estiver ativo nem em cooldown.
        if (player.RailBoostPressed) player.TryStartRailBoost();

        TiltVisual();
    }

    public override void PhysicsTick() {
        if (_rail == null || !_rail.IsValid) return;

        float dt = Time.fixedDeltaTime;

        // O Player aponta para o último lado apertado e continua nele ao soltar o input.
        if (player.HasInput) _side = player.MoveInput > 0f ? 1 : -1;

        Vector2 tangent = _rail.GetTangent(_distance);
        float alongSign = SideToAlongSign(tangent);

        // Velocidade alvo: fixa, ou a do boost enquanto ele durar. O sinal vem do lado apontado.
        bool boosting = player.RailBoostActive;
        float targetMagnitude = boosting
            ? Mathf.Max(player.railBoostMaxSpeed, player.RailCruiseSpeed)
            : player.RailCruiseSpeed;
        float target = alongSign * targetMagnitude;

        // Aceleração / frenagem
        float rate;
        if (_speed * target < 0f)                        rate = player.railTurnDeceleration;      // indo para o lado oposto: freia até parar
        else if (boosting)                               rate = player.railBoostAcceleration;
        else if (Mathf.Abs(_speed) > targetMagnitude)    rate = player.railBoostEndDeceleration;  // sobra do boost
        else                                             rate = player.railAcceleration;

        _speed = Mathf.MoveTowards(_speed, target, rate * dt);

        // Avança pelo trilho (velocidade constante em curvas e retas).
        _distance += _speed * dt;

        // Chegou na ponta: sai com a velocidade da tangente.
        if (_distance <= 0f || _distance >= _rail.Length) {
            _distance = Mathf.Clamp(_distance, 0f, _rail.Length);
            Release(player.Fall, endOfRail: true);
            return;
        }

        // Mantém o jogador encaixado no trilho.
        Vector2 targetPos = _rail.GetPoint(_distance) + Vector2.up * _height;
        Vector2 velocity = (targetPos - player.rb.position) / dt;

        player.rb.linearVelocity = Vector2.ClampMagnitude(velocity, MaxSnapVelocity);
    }

    public override void Exit() {
        player.SetGravityMultiplier(1f);
        player.SetRailGrinding(false);
        player.StartRailCooldown();

        // Restaura a rotação original do sprite.
        if (_visual != null)
            _visual.localRotation = Quaternion.identity;
    }

    // Sentido ao longo do trilho (+1 / -1) que leva o Player ao lado do mundo que ele aponta.
    private float SideToAlongSign(Vector2 tangent) {
        if (Mathf.Abs(tangent.x) > VerticalTangentThreshold)
            return _side * Mathf.Sign(tangent.x);

        return Mathf.Sign(_speed);   // trecho vertical: mantém o sentido atual
    }

    // Velocidade no sentido do movimento, usada ao soltar do trilho.
    private Vector2 TravelVelocity() => _rail.GetTangent(_distance) * _speed;

    // Sai do trilho mantendo a velocidade. Muda de estado ANTES de definir a velocidade
    // (o RiseState.Enter define a velocidade vertical do pulo comum).
    private void Release(PlayerState next, bool jump = false, bool dropDown = false, bool endOfRail = false) {
        Vector2 travel = TravelVelocity();
        float travelSpeed = Mathf.Abs(_speed);

        // Ponta do trilho: aplica a força de lançamento deste trilho (nunca reduz a velocidade atual)
        if (endOfRail && travelSpeed > 0.01f) {
            float launch = player.GetRailEndLaunchSpeed(_rail);
            if (launch > travelSpeed) {
                travel = travel.normalized * launch;
                travelSpeed = launch;
            }
        }

        player.StateMachine.ChangeState(next);

        if (jump)          player.rb.linearVelocity = new Vector2(travel.x, Mathf.Max(travel.y, 0f) + player.railJumpForce);
        else if (dropDown) player.rb.linearVelocity = new Vector2(travel.x, Mathf.Min(travel.y, 0f));
        else               player.rb.linearVelocity = travel;

        player.BeginRailMomentum(travel.x, travelSpeed);
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

        Vector2 travel = _rail.GetTangent(_distance) * _speed;
        Vector2 nextTangent = next.GetTangent(nextDistance);

        RailPath previous = _rail;
        _rail = next;
        _distance = nextDistance;

        // Ajusta o sinal para preservar o sentido do movimento.
        _speed = Mathf.Abs(_speed) * (travel.x * nextTangent.x >= 0f ? 1f : -1f);

        // Avisa a troca (animação + evento). Aqui é o único ponto onde a troca acontece.
        player.NotifyRailSwitched(previous, next, vertical);

        return true;
    }

    // Inclina somente o sprite. O Rigidbody não gira.
    private void TiltVisual() {
        if (!player.railTiltVisual || _visual == null)
            return;

        Vector2 tangent = _rail.GetTangent(_distance);
        Vector2 t = tangent * SideToAlongSign(tangent);

        float angle = Mathf.Atan2(
            t.y * Mathf.Sign(t.x),
            Mathf.Abs(t.x)
        ) * Mathf.Rad2Deg;

        _visual.localRotation = Quaternion.Slerp(
            _visual.localRotation,
            Quaternion.Euler(0f, 0f, angle),
            player.railTiltSpeed * Time.deltaTime
        );
    }
}
