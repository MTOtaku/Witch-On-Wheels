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
    public static readonly int RailSwitch = Animator.StringToHash("RailSwitch");
    public static readonly int Interact   = Animator.StringToHash("Interact");     // ainda sem animação: mantém a atual   // ainda sem animação: usa a de pulo (Rise)
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
//   - O sentido (_dir) é guardado ao longo do TRILHO e só muda quando o jogador vira. Por isso loops
//     e curvas de 360° funcionam: o sentido não depende de a tangente apontar para a esquerda ou direita.
//   - O Player fica sempre do mesmo lado do trilho (normal). Em loop, isso o leva de cabeça para baixo por dentro.
//   - Boost (botão): impulso no sentido em que o Player anda; tempo e cooldown ficam no PlayerController.
//   - Pulo: sai mantendo a velocidade (embalo). Baixo + Pulo: desce do trilho.
//   - O tempo no trilho é limitado (com upgrades). Acabou o tempo -> solta o Player.
public class GrindState : PlayerState {
    // Limite da velocidade usada para encaixar no trilho.
    // Evita deslocamentos excessivos caso o Player esteja longe do ponto.
    private const float MaxSnapVelocity = 60f;

    // Abaixo disso o trecho do trilho é considerado vertical (esquerda/direita não diz o sentido)
    private const float VerticalTangentThreshold = 0.05f;

    // Pulo: se a normal apontar mais para cima que isso, pula reto para cima; senão pula para longe do trilho
    private const float UprightNormalY = 0.5f;

    private RailPath _rail;
    private float _distance;
    private float _speed;          // velocidade COM SINAL ao longo da tangente (+ = rumo ao fim do trilho)
    private int _dir = 1;          // sentido DESEJADO ao longo do trilho (+1 rumo ao fim, -1 rumo ao início)
    private float _normalSign = 1f; // lado do trilho onde o Player fica (+1 / -1), constante durante o grind
    private int _lastInputSide;    // último lado do input (-1, 0, +1): o sentido só é reavaliado quando ele muda
    private float _height;         // distância do pivô do Player até os pés
    private float _prevInputY;
    private Transform _visual;
    private Vector3 _visualRestPosition;   // posição local do sprite fora do trilho (restaurada ao sair)

    public GrindState(PlayerController player) : base(player) { }

    public override void Enter() {
        _rail = player.GrindRail;
        _distance = player.GrindStartDistance;
        _height = player.PivotToFeet;
        _prevInputY = player.MoveInputY;
        _visual = player.RailVisualRoot;
        if (_visual != null) _visualRestPosition = _visual.localPosition;

        if (_rail == null || !_rail.IsValid) {
            player.StateMachine.ChangeState(player.Fall);
            return;
        }

        Vector2 tangent = _rail.GetTangent(_distance);

        // Lado do trilho em que o Player está (normalmente em cima)
        Vector2 toPlayer = player.rb.position - _rail.GetPoint(_distance);
        float side = Vector2.Dot(toPlayer, Perp(tangent));
        _normalSign = Mathf.Abs(side) > 0.01f ? Mathf.Sign(side) : (Perp(tangent).y >= 0f ? 1f : -1f);

        // Lado do mundo: input atual, senão o movimento horizontal. Parado: o lado para onde o trilho segue.
        int worldSide;
        if (player.HasInput)                         worldSide = player.MoveInput > 0f ? 1 : -1;
        else if (Mathf.Abs(player.VelocityX) > 0.5f) worldSide = player.VelocityX > 0f ? 1 : -1;
        else                                         worldSide = tangent.x >= 0f ? 1 : -1;

        _dir = SideToDir(tangent, worldSide);
        _lastInputSide = player.HasInput ? (player.MoveInput > 0f ? 1 : -1) : 0;

        // Velocidade ao entrar: a guardada do pulo (volta com a mesma velocidade) ou o embalo atual
        float magnitude;
        if (player.TryConsumeRailReturnSpeed(out float stored))
            magnitude = stored;
        else
            magnitude = Mathf.Max(0f, Vector2.Dot(player.rb.linearVelocity, tangent * _dir));

        magnitude = Mathf.Min(magnitude, Mathf.Max(player.railBoostMaxSpeed, player.RailCruiseSpeed));
        _speed = _dir * magnitude;

        UpdateFlip();

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

        UpdateDirection();
        UpdateFlip();

        // Boost: só liga se não estiver ativo nem em cooldown.
        if (player.RailBoostPressed) player.TryStartRailBoost();

        TiltVisual();
    }

    public override void PhysicsTick() {
        if (_rail == null || !_rail.IsValid) return;

        float dt = Time.fixedDeltaTime;

        // Velocidade alvo: fixa, ou a do boost enquanto ele durar. O sinal vem do sentido (_dir).
        bool boosting = player.RailBoostActive;
        float targetMagnitude = boosting
            ? Mathf.Max(player.railBoostMaxSpeed, player.RailCruiseSpeed)
            : player.RailCruiseSpeed;
        float target = _dir * targetMagnitude;

        // Aceleração / frenagem
        float rate;
        if (_speed * target < 0f)                        rate = player.railTurnDeceleration;      // indo contra o sentido desejado: freia até parar
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

        // Mantém o jogador encaixado no trilho, do lado da normal (em loop, por dentro e de cabeça para baixo).
        Vector2 tangent = _rail.GetTangent(_distance);
        Vector2 targetPos = _rail.GetPoint(_distance) + Normal(tangent) * _height;
        Vector2 velocity = (targetPos - player.rb.position) / dt;

        player.rb.linearVelocity = Vector2.ClampMagnitude(velocity, MaxSnapVelocity);
    }

    public override void Exit() {
        player.SetGravityMultiplier(1f);
        player.SetRailGrinding(false);
        player.StartRailCooldown();

        // Restaura a rotação e a posição originais do sprite.
        if (_visual != null) {
            _visual.localRotation = Quaternion.identity;
            _visual.localPosition = _visualRestPosition;
        }

        // Ao sair, o Player olha para o lado do mundo em que estava indo
        if (_rail != null && _rail.IsValid) {
            Vector2 heading = _rail.GetTangent(_distance) * _dir;
            if (Mathf.Abs(heading.x) > VerticalTangentThreshold)
                player.SetFacing(heading.x > 0f ? 1 : -1);
        }
    }

    // Espelha o sprite (escala X) para olhar no sentido em que anda no trilho.
    // Com o giro ligado, o lado é "sentido x lado do trilho": não muda ao dar a volta num loop.
    private void UpdateFlip() {
        if (_rail == null || !_rail.IsValid) return;

        float flip;
        if (player.railTiltVisual) flip = _dir * _normalSign;
        else                       flip = (_rail.GetTangent(_distance) * _dir).x >= 0f ? 1f : -1f;

        player.SetVisualFlip(flip);
    }

    private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

    // Normal do trilho do lado em que o Player fica (para "cima" do Player)
    private Vector2 Normal(Vector2 tangent) => Perp(tangent) * _normalSign;

    // Converte o lado do mundo (esquerda/direita) em sentido ao longo do trilho
    private int SideToDir(Vector2 tangent, int worldSide) {
        if (Mathf.Abs(tangent.x) > VerticalTangentThreshold)
            return worldSide * tangent.x > 0f ? 1 : -1;

        return Vector2.Dot(player.rb.linearVelocity, tangent) >= 0f ? 1 : -1;   // trecho vertical
    }

    // Vira o sentido só quando o jogador APERTA um lado novo e esse lado é o oposto ao que ele está indo.
    // Segurar o mesmo lado (ou soltar o input) nunca muda o sentido: é isso que evita o vai e volta nos loops.
    private void UpdateDirection() {
        int inputSide = player.HasInput ? (player.MoveInput > 0f ? 1 : -1) : 0;
        if (inputSide == _lastInputSide) return;

        if (inputSide == 0) {
            _lastInputSide = 0;
            return;
        }

        Vector2 tangent = _rail.GetTangent(_distance);
        if (Mathf.Abs(tangent.x) <= VerticalTangentThreshold) return;   // trecho vertical: espera o lado ficar claro

        int facing = tangent.x * _dir > 0f ? 1 : -1;   // lado do mundo para onde o Player anda agora
        if (inputSide != facing) _dir = -_dir;         // oposto: vira (o motor freia e acelera para o outro lado)

        _lastInputSide = inputSide;
    }

    // Velocidade no sentido do movimento, usada ao soltar do trilho.
    private Vector2 TravelVelocity() => _rail.GetTangent(_distance) * _speed;

    // Sai do trilho mantendo a velocidade. Muda de estado ANTES de definir a velocidade
    // (o RiseState.Enter define a velocidade vertical do pulo comum).
    private void Release(PlayerState next, bool jump = false, bool dropDown = false, bool endOfRail = false) {
        Vector2 tangent = _rail.GetTangent(_distance);
        Vector2 normal = Normal(tangent);
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

        if (jump) {
            if (normal.y >= UprightNormalY)
                player.rb.linearVelocity = new Vector2(travel.x, Mathf.Max(travel.y, 0f) + player.railJumpForce);
            else
                player.rb.linearVelocity = travel + normal * player.railJumpForce;   // em loop: pula para longe do trilho
        }
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

        Vector2 oldTangent = _rail.GetTangent(_distance);
        Vector2 oldNormal = Normal(oldTangent);
        Vector2 heading = oldTangent * _dir;
        Vector2 nextTangent = next.GetTangent(nextDistance);

        RailPath previous = _rail;
        _rail = next;
        _distance = nextDistance;

        // Mantém o mesmo sentido de movimento e o mesmo lado do trilho
        int newDir = Vector2.Dot(heading, nextTangent) >= 0f ? 1 : -1;
        _speed *= newDir * _dir;
        _dir = newDir;
        _normalSign = Vector2.Dot(Perp(nextTangent), oldNormal) >= 0f ? 1f : -1f;

        // Avisa a troca (animação + evento). Aqui é o único ponto onde a troca acontece.
        player.NotifyRailSwitched(previous, next, vertical);

        return true;
    }

    // Gira somente o sprite para ficar "em pé" na normal do trilho (0° a 360°). O Rigidbody não gira.
    // O sprite gira em volta do PIVÔ DO PLAYER (a posição dele também é girada), assim os pés
    // continuam no trilho mesmo de cabeça para baixo, enquanto o collider fica em pé.
    private void TiltVisual() {
        if (!player.railTiltVisual || _visual == null)
            return;

        Vector2 normal = Normal(_rail.GetTangent(_distance));
        float angle = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;

        _visual.localRotation = Quaternion.Slerp(
            _visual.localRotation,
            Quaternion.Euler(0f, 0f, angle),
            player.railTiltSpeed * Time.deltaTime
        );

        // Gira também o deslocamento do sprite em relação ao pivô do Player
        _visual.localPosition = _visual.localRotation * _visualRestPosition;
    }
}

// ------------------------------------------- Interact -------------------------------------------
// Jogador interagindo (diálogo, NPC, objeto). O movimento fica desabilitado:
// só freia até parar. Quem entra e sai deste estado é o PlayerController
// (TryBeginInteraction / EndInteraction), chamado pelo sistema de diálogo.
public class InteractState : PlayerState {
    public InteractState(PlayerController player) : base(player) { }

    public override void Enter() {
        player.ConsumeJump();           // um pulo apertado antes não dispara ao sair
        player.ClearRailMomentum();
        player.SetGravityMultiplier(1f);
        player.PlayAnimation(PlayerAnim.Interact);
    }

    // Sem lógica no Tick: nenhuma transição acontece sozinha, só o EndInteraction sai daqui.

    // Freia até parar e deixa a gravidade agir (se estiver no ar, cai)
    public override void PhysicsTick() => player.MoveHorizontal(0f, braking: true);

    public override void Exit() {
        player.ConsumeJump();           // botões apertados durante o diálogo não viram pulo
    }
}
