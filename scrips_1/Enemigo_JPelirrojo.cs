using Godot;

public partial class Enemigo_JPelirrojo : CharacterBody2D
{
	[Export] public float Speed          { get; set; } = 150.0f;
	[Export] public float AttackDistance { get; set; } = 55.0f;
	[Export] public float DetectionRange { get; set; } = 400.0f;
	// Distancia a la que el enemigo saluda al jugador (debe ser <= DetectionRange)
	[Export] public float GreetRange     { get; set; } = 220.0f;

	// === PERSISTENCIA ===
	[Export] public string UniqueId { get; set; } = "enemigo_sala_1"; // ← ¡CAMBIA ESTE ID EN CADA ENEMIGO!

	private AnimationPlayer _anim;
	private Sprite2D        _sprite;
	private HealthComponent_JPelirrojo _health;
	private HitboxComponent_JPelirrojo _hitbox;
	private CharacterBody2D _player;

	private bool _isAttacking     = false;
	private bool _isDead          = false;
	private bool _isGreeting      = false;

	// Verdadero mientras se reproduce death_closed como animación de transición de fase.
	// El enemigo se queda completamente inmóvil durante este tiempo.
	private bool _isTransitioning = false;

	// Se activa la primera vez que el jugador entra en GreetRange;
	// se reinicia cuando el jugador sale del rango de detección.
	private bool _hasGreetedThisEncounter = false;

	// Fase de baja vida (≤ 50 % HP).
	// Se pone a true AL TERMINAR la animación de transición, no antes.
	private bool _isLowHealth = false;

	// Daño base tomado del Hitbox al inicio; se duplica al activarse la fase baja.
	private int _baseDamage = 1;

	// Sufijo de animación según fase de vida.
	// Afecta a: idle, run, death, attack_1, attack_2.
	// NO afecta a: greet (es independiente de la fase).
	private string AnimSuffix => _isLowHealth ? "_open" : "_closed";

	// Ruta de escena capturada en _Ready, segura para usar en _ExitTree.
	private string _scenePath = "";

	// ====================================================================
	public override void _Ready()
	{
		_scenePath = GetTree().CurrentScene?.SceneFilePath ?? "";

		_anim   = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_health = GetNode<HealthComponent_JPelirrojo>("HealthComponent");
		_hitbox = GetNodeOrNull<HitboxComponent_JPelirrojo>("HitboxComponent");

		if (_hitbox != null)
			_baseDamage = _hitbox.Damage;

		if (_health != null)
		{
			_health.Died          += OnDied;
			_health.HealthChanged += OnHealthChanged;
		}

		_player = GetTree().GetFirstNodeInGroup("Player") as CharacterBody2D;

		// === CARGAR ESTADO GUARDADO (muerte + posición) ===
		if (GameState.Instance != null)
		{
			var state           = GameState.Instance.GetState(UniqueId);
			bool isDeadFromSave = (bool)state["dead"];

			if (isDeadFromSave)
			{
				_isDead  = true;
				Velocity = Vector2.Zero;

				_anim.Stop();
				_anim.Play("death_closed"); // al cargar muerto asumimos fase inicial

				var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (collision != null)
					collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

				if (_health != null)
					_health.SetDeferred("monitoring", false);

				GD.Print($"Enemigo {UniqueId} cargado como MUERTO.");
				return;
			}
			else
			{
				string savedScene = (string)state["scene"];
				Vector2 savedPos  = (Vector2)state["position"];

				if (savedScene == _scenePath && savedPos != Vector2.Zero)
				{
					GlobalPosition = savedPos;
					GD.Print($"Enemigo {UniqueId} restaurado en posición: {GlobalPosition}");
				}
				else if (savedScene != _scenePath && savedScene != "")
				{
					GD.Print($"Enemigo {UniqueId}: escena nueva, ignorando posición guardada de '{savedScene}'");
				}
			}
		}

		GD.Print($"Enemigo {UniqueId} listo.");
	}

	// ====================================================================
	// Al bajar del 50 % HP: bloquea al enemigo y reproduce death_closed
	// como animación de transición. La fase _open y el daño doble se
	// activan AL TERMINAR dicha animación.
	// ====================================================================
	private void OnHealthChanged(int newHealth)
	{
		// Ignorar si ya está en fase baja, en transición o muerto
		if (_isLowHealth || _isTransitioning || _isDead || _health == null) return;

		if (newHealth <= _health.MaxHealth / 2)
		{
			_isTransitioning = true;
			Velocity         = Vector2.Zero;

			// Si la animación no existe, activar la fase directamente sin bloquear
			if (!_anim.HasAnimation("death_closed"))
			{
				GD.PrintErr($"[{UniqueId}] 'death_closed' no encontrada. Activando fase directamente.");
				_isTransitioning = false;
				ActivateLowHealthPhase();
				return;
			}

			_anim.Stop();
			_anim.Play("death_closed");
			float len = (float)_anim.GetAnimation("death_closed").Length;
			GD.Print($"[{UniqueId}] Transición de fase → reproduciendo 'death_closed' ({len:F2}s).");

			GetTree().CreateTimer(len).Timeout += () =>
			{
				// Si murió durante la transición, OnDied ya gestionó la muerte
				if (_isDead) return;
				_isTransitioning = false;
				ActivateLowHealthPhase();
			};
		}
	}

	// Activa la fase baja de vida y duplica el daño base
	private void ActivateLowHealthPhase()
	{
		_isLowHealth = true;
		_baseDamage *= 2;
		GD.Print($"[{UniqueId}] ¡Fase BAJA VIDA activa! Animaciones → _open | Daño base → {_baseDamage}");
	}

	// ====================================================================
	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _player == null || !GodotObject.IsInstanceValid(_player))
			return;

		UpdateFacingDirection();

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// ── Reiniciar saludo cuando el jugador abandona el rango de detección
		if (distance > DetectionRange)
			_hasGreetedThisEncounter = false;

		// ── Bloqueado durante transición de fase, saludo o ataque
		if (_isTransitioning || _isGreeting || _isAttacking)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// ── Saludo: primera vez que entra en GreetRange
		if (!_hasGreetedThisEncounter && distance <= GreetRange && distance > AttackDistance)
			TryPlayGreeting();

		// ── Lógica de movimiento / ataque
		if (distance > DetectionRange)
		{
			Velocity = Vector2.Zero;
			_anim.Play("idle" + AnimSuffix);
		}
		else if (distance > AttackDistance)
		{
			Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
			Velocity = dir * Speed;
			_anim.Play("run" + AnimSuffix);
		}
		else
		{
			Velocity = Vector2.Zero;
			StartAttack();
		}

		MoveAndSlide();
	}

	// ====================================================================
	// Saludo — sin sufijo, es independiente de la fase de vida
	// ====================================================================
	private void TryPlayGreeting()
	{
		_hasGreetedThisEncounter = true; // evitar doble llamada aunque no exista la anim

		if (!_anim.HasAnimation("greet"))
		{
			GD.PrintErr($"[{UniqueId}] Animación 'greet' no encontrada, saltando saludo.");
			return;
		}

		_isGreeting = true;
		_anim.Play("greet");
		float len = (float)_anim.GetAnimation("greet").Length;

		GetTree().CreateTimer(len).Timeout += () =>
		{
			if (!_isDead)
				_isGreeting = false;
		};

		GD.Print($"[{UniqueId}] Saludando al jugador.");
	}

	// ====================================================================
	// Dirección del sprite
	// ====================================================================
	private void UpdateFacingDirection()
	{
		if (_player == null) return;
		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
		if (Mathf.Abs(dir.X) > 0.1f)
			_sprite.FlipH = dir.X < 0;
	}

	// ====================================================================
	// Ataque con selección aleatoria y daño variable
	//   35 % → attack_1_[sufijo]  (daño base × 1.5, redondeado)
	//   65 % → attack_2_[sufijo]  (daño base)
	// _baseDamage ya incluye el ×2 si la fase baja está activa.
	// ====================================================================
	private void StartAttack()
	{
		if (_isAttacking || _isDead) return;
		_isAttacking = true;

		bool useAttack1   = GD.Randf() < 0.35f;
		string attackAnim = (useAttack1 ? "attack_1" : "attack_2") + AnimSuffix;

		int damage = useAttack1
			? Mathf.RoundToInt(_baseDamage * 1.5f)
			: _baseDamage;

		if (_hitbox != null)
		{
			_hitbox.Damage     = damage;
			_hitbox.Monitoring = true;
		}

		_anim.Play(attackAnim);
		GD.Print($"[{UniqueId}] Ataca con '{attackAnim}' — daño: {damage}");

		GetTree().CreateTimer(0.6f).Timeout += () =>
		{
			_isAttacking = false;
			if (_hitbox != null)
				_hitbox.Monitoring = false;
		};
	}

	// ====================================================================
	// Muerte — usa AnimSuffix para elegir death_closed o death_open.
	// Si muere durante la transición de fase, fuerza el sufijo _open
	// (el enemigo ya estaba en proceso de cambio) y cancela la transición.
	// ====================================================================
	private void OnDied()
	{
		if (_isDead) return;
		_isDead          = true;
		_isTransitioning = false; // cancelar transición si estaba en marcha

		// Si murió mientras se transformaba, consideramos que ya era fase _open
		if (!_isLowHealth && _health != null &&
			_health.CurrentHealth <= _health.MaxHealth / 2)
		{
			_isLowHealth = true;
		}

		Velocity = Vector2.Zero;
		_anim.Stop();
		_anim.Play("death" + AnimSuffix); // death_closed o death_open

		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
			collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		if (_health != null)
			_health.SetDeferred("monitoring", false);

		GD.Print($"[{UniqueId}] Ha caído. Animación de muerte: 'death{AnimSuffix}'.");

		if (GameState.Instance != null)
			GameState.Instance.SetState(UniqueId, true, GlobalPosition, "death", 0,
										"", 0, 0, _scenePath);
	}

	// ====================================================================
	// Guardar posición al salir del árbol (solo si está vivo)
	// ====================================================================
	public override void _ExitTree()
	{
		if (GameState.Instance != null && !_isDead)
		{
			GameState.Instance.SetState(UniqueId, false, GlobalPosition,
										"", 0, "", 0, 0, _scenePath);
			GD.Print($"[{UniqueId}] Posición guardada: {GlobalPosition} en '{_scenePath}'.");
		}
		base._ExitTree();
	}
}
