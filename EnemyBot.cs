// =============================================
// EnemyBot.cs  (el script principal del bot)
// Adjúntalo al CharacterBody2D raíz de tu escena de bot
// =============================================
using Godot;

public partial class EnemyBot : CharacterBody2D
{
    [Export]
    public float Speed = 150.0f;

    [Export]
    public float DetectionRange = 400.0f;

    [Export]
    public float AttackRange = 50.0f;

    [Export]
    public float AttackCooldownTime = 1.5f;

    private enum BotType { Passive, Active }
    private BotType _currentType;

    private CharacterBody2D _player;
    private AnimatedSprite2D _animatedSprite;
    private HealthComponent _healthComponent;
    private HitboxComponent _hitboxComponent;

    private double _attackCooldownTimer = 0.0;

    public override void _Ready()
    {
        // Generación aleatoria 50/50 (cada bot es independiente)
        _currentType = (GD.Randf() < 0.5f) ? BotType.Active : BotType.Passive;
        GD.Print($"[EnemyBot] Bot generado como {(_currentType == BotType.Active ? "ACTIVO (ataca)" : "PASIVO (huye)")}");

        // Referencia al jugador (debe estar en grupo "player")
        _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
        if (_player == null)
            GD.PrintErr("[EnemyBot] ¡No se encontró jugador en el grupo 'player'!");

        // Componentes y sprite (nombres exactos que debes usar en la escena)
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        _hitboxComponent = GetNodeOrNull<HitboxComponent>("HitboxComponent");

        if (_animatedSprite == null) GD.PrintErr("[EnemyBot] AnimatedSprite2D no encontrado (debe llamarse 'AnimatedSprite2D')");
        if (_healthComponent == null) GD.PrintErr("[EnemyBot] HealthComponent no encontrado");
        if (_hitboxComponent == null && _currentType == BotType.Active)
            GD.PrintErr("[EnemyBot] Bot activo sin HitboxComponent");

        // Conexiones
        if (_healthComponent != null)
            _healthComponent.HealthDepleted += OnHealthDepleted;

        if (_hitboxComponent != null)
            _hitboxComponent.Monitoring = false;

        if (_animatedSprite != null)
        {
            _animatedSprite.AnimationFinished += OnAnimationFinished;
            _animatedSprite.Play("idle");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _healthComponent == null || _animatedSprite == null) return;

        // Si ya está muerto, no procesamos nada más
        if (_healthComponent.CurrentHealth <= 0)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Vector2 directionToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
        float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

        Vector2 velocity = Velocity;
        bool isMoving = false;

        // === IA PASIVA (huye) ===
        if (_currentType == BotType.Passive)
        {
            if (distanceToPlayer < DetectionRange && distanceToPlayer > 0)
            {
                Vector2 fleeDir = -directionToPlayer;
                velocity = fleeDir * Speed;
                isMoving = true;
            }
            else
            {
                velocity = Vector2.Zero;
            }
        }
        // === IA ACTIVA (ataca) ===
        else
        {
            if (distanceToPlayer < DetectionRange && distanceToPlayer > 0)
            {
                if (distanceToPlayer > AttackRange)
                {
                    // Persecución
                    velocity = directionToPlayer * Speed;
                    isMoving = true;
                }
                else
                {
                    // En rango → atacar
                    velocity = Vector2.Zero;
                    AttemptAttack(delta);
                }
            }
            else
            {
                velocity = Vector2.Zero;
            }
        }

        Velocity = velocity;
        MoveAndSlide();

        // === ANIMACIONES (comunes a ambos tipos) ===
        if (isMoving)
        {
            _animatedSprite.Play("walk");
            if (velocity.X != 0)
                _animatedSprite.FlipH = velocity.X < 0;
        }
        else if (_animatedSprite.Animation != "attack" && _animatedSprite.Animation != "death")
        {
            _animatedSprite.Play("idle");
        }
    }

    private void AttemptAttack(double delta)
    {
        _attackCooldownTimer -= delta;

        if (_attackCooldownTimer <= 0.0)
        {
            _attackCooldownTimer = AttackCooldownTime;

            // Animación de ataque (solo el bot activo la usa)
            if (_animatedSprite != null)
                _animatedSprite.Play("attack");

            // Realizar daño (una sola vez por ataque)
            if (_hitboxComponent != null)
            {
                _hitboxComponent.Monitoring = true;
                _hitboxComponent.DealDamageToOverlaps();
            }
        }
    }

    private void OnHealthDepleted()
    {
        if (_animatedSprite != null)
            _animatedSprite.Play("death");

        if (_hitboxComponent != null)
            _hitboxComponent.Monitoring = false;

        // Desactivar colisiones y movimiento → el cadáver se queda en el suelo
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.Disabled = true;

        SetPhysicsProcess(false);
        GD.Print("[EnemyBot] Bot muerto - cadáver en el suelo");
    }

    private void OnAnimationFinished()
    {
        // Al terminar el ataque volvemos a idle
        if (_animatedSprite.Animation == "attack" && _hitboxComponent != null)
        {
            _hitboxComponent.Monitoring = false;
            _animatedSprite.Play("idle");
        }
        // La animación de muerte NO vuelve a idle → se queda en el último frame (cadáver)
    }
}
