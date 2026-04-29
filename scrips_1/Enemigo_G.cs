using Godot;

public partial class Enemigo_G : CharacterBody2D
{
	// ─── Exportados ────────────────────────────────────────────────────────────
	[Export] public float  Speed          { get; set; } = 150.0f;
	[Export] public float  AttackDistance { get; set; } = 55.0f;
	[Export] public float  DetectionRange { get; set; } = 400.0f;
	[Export] public string SpecialAnim1   { get; set; } = "idle_special1";

	/// <summary>
	/// ID único de este enemigo. Debe ser distinto para cada instancia en el editor.
	/// Ejemplo: "enemigo_g_sala1_01"
	/// </summary>
	[Export] public string UniqueId { get; set; } = "";

	// ─── Nodos ─────────────────────────────────────────────────────────────────
	private AnimationPlayer    _anim;
	private Sprite2D           _sprite;
	private HealthComponent    _health;
	private CharacterBody2D    _player;

	// ─── Estado interno ────────────────────────────────────────────────────────
	private bool _isAttacking          = false;
	private bool _isDead               = false;
	private bool _hasPlayedSpecialIdle = false;

	// ─── Escena actual ─────────────────────────────────────────────────────────
	private string CurrentScene => GetTree().CurrentScene?.SceneFilePath ?? "";

	// ═══════════════════════════════════════════════════════════════════════════
	public override void _Ready()
	{
		_anim   = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_health = GetNode<HealthComponent>("HealthComponent");

		if (_health != null)
			_health.Died += OnDied;

		_player = GetTree().GetFirstNodeInGroup("Player") as CharacterBody2D;

		// ── Validación de UniqueId ──────────────────────────────────────────────
		if (string.IsNullOrEmpty(UniqueId))
		{
			GD.PrintErr($"[Enemigo_G] '{Name}' no tiene UniqueId asignado. El estado NO se guardará.");
		}
		else
		{
			RestoreFromGameState();
			if (_isDead) return; // Si ya estaba muerto, _Ready termina aquí
		}

		// ── Idle especial solo la primera vez ──────────────────────────────────
		if (_anim.HasAnimation(SpecialAnim1))
		{
			_anim.Play(SpecialAnim1);
			_hasPlayedSpecialIdle = true;
			GD.Print($"[Enemigo_G] Reproduciendo {SpecialAnim1} al inicio.");
		}
		else
		{
			GD.PrintErr($"[Enemigo_G] Animación '{SpecialAnim1}' no encontrada.");
		}
	}

	// ═══════════════════════════════════════════════════════════════════════════
	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _player == null || !GodotObject.IsInstanceValid(_player))
			return;

		UpdateFacingDirection();

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// Esperar a que termine el idle especial inicial
		if (_hasPlayedSpecialIdle && _anim.CurrentAnimation == SpecialAnim1)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// ── Lógica normal de persecución ───────────────────────────────────────
		if (distance > DetectionRange || _isAttacking)
		{
			Velocity = Vector2.Zero;
			if (!_isAttacking)
				_anim.Play("idle");
		}
		else
		{
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();

			if (distance > AttackDistance)
			{
				Velocity = direction * Speed;
				_anim.Play("run");
			}
			else
			{
				Velocity = Vector2.Zero;
				StartAttack();
			}
		}

		MoveAndSlide();
	}

	// ═══════════════════════════════════════════════════════════════════════════
	/// <summary>
	/// Se llama automáticamente cuando el nodo abandona el árbol de escena.
	/// Guarda posición y animación actuales si el enemigo sigue vivo.
	/// </summary>
	public override void _ExitTree()
	{
		if (string.IsNullOrEmpty(UniqueId) || _isDead) return;

		SaveToGameState();
		GD.Print($"[Enemigo_G] '{UniqueId}' estado guardado al salir de escena.");
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Helpers privados
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Lee el GameState y aplica el estado guardado.
	/// Si el enemigo estaba muerto, lo elimina del árbol.
	/// </summary>
	private void RestoreFromGameState()
	{
		var state = GameState.Instance?.GetState(UniqueId);
		if (state == null) return;

		// Solo restaurar si corresponde a esta misma escena
		string savedScene = state["scene"].AsString();
		if (!string.IsNullOrEmpty(savedScene) && savedScene != CurrentScene)
			return;

		bool wasDead = state["dead"].AsBool();

		if (wasDead)
		{
			_isDead = true;
			GD.Print($"[Enemigo_G] '{UniqueId}' estaba muerto. Eliminando del árbol.");
			QueueFree();
			return;
		}

		// Restaurar posición
		Vector2 savedPos = state["position"].AsVector2();
		if (savedPos != Vector2.Zero)
			GlobalPosition = savedPos;

		// Restaurar animación (si existe)
		string savedAnim = state["animation"].AsString();
		if (!string.IsNullOrEmpty(savedAnim) && _anim.HasAnimation(savedAnim))
		{
			int savedFrame = state["frame"].AsInt32();
			_anim.Play(savedAnim);
			_anim.Seek(0, true); // seek por frame requiere mapear tiempo; usa 0 como seguro
			GD.Print($"[Enemigo_G] '{UniqueId}' restaurado en posición {savedPos}, anim '{savedAnim}'.");
		}
	}

	/// <summary>
	/// Persiste el estado actual del enemigo en GameState.
	/// </summary>
	private void SaveToGameState()
	{
		if (GameState.Instance == null) return;

		string animName = _anim.CurrentAnimation ?? "";
		int    frame    = 0; // AnimationPlayer no expone frame directamente

		GameState.Instance.SetState(
			uniqueId:      UniqueId,
			isDead:        _isDead,
			position:      GlobalPosition,
			animationName: animName,
			frame:         frame,
			scene:         CurrentScene
		);
	}

	// ─── Dirección ─────────────────────────────────────────────────────────────
	private void UpdateFacingDirection()
	{
		if (_player == null) return;
		Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
		if (Mathf.Abs(dir.X) > 0.1f)
			_sprite.FlipH = dir.X < 0;
	}

	// ─── Ataque ────────────────────────────────────────────────────────────────
	private void StartAttack()
	{
		if (_isAttacking || _isDead) return;

		_isAttacking = true;
		_anim.Play("attack");

		var hitbox = GetNodeOrNull<HitboxComponent>("HitboxComponent");
		if (hitbox != null) hitbox.Monitoring = true;

		GetTree().CreateTimer(0.6f).Timeout += () =>
		{
			_isAttacking = false;
			if (hitbox != null) hitbox.Monitoring = false;
		};
	}

	// ─── Muerte ────────────────────────────────────────────────────────────────
	private void OnDied()
	{
		if (_isDead) return;

		_isDead   = true;
		Velocity  = Vector2.Zero;

		_anim.Stop();
		_anim.Play("death");

		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
			collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		if (_health != null)
			_health.SetDeferred("monitoring", false);

		// ── Guardar estado de muerte en GameState ──────────────────────────────
		if (!string.IsNullOrEmpty(UniqueId))
		{
			SaveToGameState();
			GD.Print($"[Enemigo_G] '{UniqueId}' muerte guardada en GameState.");
		}

		GD.Print("[Enemigo_G] El enemigo ha caído.");
	}
}
