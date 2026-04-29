using Godot;

public partial class EnemyBot : CharacterBody2D
{
	[Export] public float Speed = 150.0f;
	[Export] public float DetectionRange = 400.0f;
	[Export] public float AttackRange = 50.0f;
	[Export] public float AttackCooldownTime = 1.5f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float ActiveChance = 0.5f;

	private enum BotType { Passive, Active }
	private BotType _currentType;

	private CharacterBody2D _player;
	private Sprite2D _sprite;
	private AnimationPlayer _animationPlayer;
	private HealthComponent _healthComponent;       // ✅ misma clase que el jugador
	private HitboxComponentBots _hitboxComponent;

	private double _attackCooldownTimer = 0.0;

	public override void _Ready()
	{
		_currentType = (GD.Randf() < ActiveChance) ? BotType.Active : BotType.Passive;
		GD.Print($"[EnemyBot] Bot generado como {(_currentType == BotType.Active ? "ACTIVO" : "PASIVO")} (ActiveChance={ActiveChance:P0})");

		_player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
		if (_player == null)
			GD.PrintErr("[EnemyBot] ¡No se encontró jugador en el grupo 'player'!");

		_sprite          = GetNodeOrNull<Sprite2D>("Sprite2D");
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		_healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent"); // ✅
		_hitboxComponent = GetNodeOrNull<HitboxComponentBots>("HitboxComponent");

		if (_sprite == null)          GD.PrintErr("[EnemyBot] Sprite2D no encontrado");
		if (_animationPlayer == null) GD.PrintErr("[EnemyBot] AnimationPlayer no encontrado");
		if (_healthComponent == null) GD.PrintErr("[EnemyBot] HealthComponent no encontrado");
		if (_hitboxComponent == null && _currentType == BotType.Active)
			GD.PrintErr("[EnemyBot] Bot activo sin HitboxComponent");

		if (_healthComponent != null)
			_healthComponent.Died += OnDied; // ✅ señal Died en vez de HealthDepleted

		if (_hitboxComponent != null)
			_hitboxComponent.Monitoring = false;

		if (_animationPlayer != null)
		{
			_animationPlayer.AnimationFinished += OnAnimationFinished;
			_animationPlayer.Play("idle");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null || _healthComponent == null || _animationPlayer == null) return;

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

		if (_currentType == BotType.Passive)
		{
			if (distanceToPlayer < DetectionRange && distanceToPlayer > 0)
			{
				velocity = -directionToPlayer * Speed;
				isMoving = true;
			}
			else
			{
				velocity = Vector2.Zero;
			}
		}
		else
		{
			if (distanceToPlayer < DetectionRange && distanceToPlayer > 0)
			{
				if (distanceToPlayer > AttackRange)
				{
					velocity = directionToPlayer * Speed;
					isMoving = true;
				}
				else
				{
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

		if (isMoving)
		{
			_animationPlayer.Play("walk");
			if (_sprite != null && velocity.X != 0)
				_sprite.FlipH = velocity.X < 0;
		}
		else if (_animationPlayer.CurrentAnimation != "attack" &&
				 _animationPlayer.CurrentAnimation != "death")
		{
			_animationPlayer.Play("idle");
		}
	}

	private void AttemptAttack(double delta)
	{
		_attackCooldownTimer -= delta;

		if (_attackCooldownTimer <= 0.0)
		{
			_attackCooldownTimer = AttackCooldownTime;
			_animationPlayer?.Play("attack");

			if (_hitboxComponent != null)
			{
				_hitboxComponent.Monitoring = true;
				_hitboxComponent.DealDamageToOverlaps();
			}
		}
	}

	private void OnDied() // ✅ renombrado para coincidir con la señal Died
	{
		_animationPlayer?.Play("death");

		if (_hitboxComponent != null)
			_hitboxComponent.Monitoring = false;

		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
			collision.Disabled = true;

		SetPhysicsProcess(false);
		GD.Print("[EnemyBot] Bot muerto");
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "attack" && _hitboxComponent != null)
		{
			_hitboxComponent.Monitoring = false;
			_animationPlayer.Play("idle");
		}
	}
}
