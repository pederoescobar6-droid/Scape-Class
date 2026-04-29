using Godot;

public partial class Enemigo : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 150.0f;
	[Export] public float AttackDistance { get; set; } = 55.0f;
	[Export] public float DetectionRange { get; set; } = 400.0f;
	[Export] public string SpecialAnim1 { get; set; } = "idle_special1";
	[Export] public string SpecialAnim2 { get; set; } = "idle_special2";
	[Export] public bool StaticDuringSpecial2 { get; set; } = false;

	// === PERSISTENCIA ===
	[Export] public string UniqueId { get; set; } = "enemigo_sala_1"; // ← ¡CAMBIA ESTE ID EN CADA ENEMIGO!

	private AnimationPlayer _anim;
	private Sprite2D _sprite;
	private HealthComponent _health;
	private CharacterBody2D _player;
	private bool _isAttacking = false;
	private bool _isDead = false;
	private bool _isInSpecialAnimation = false;
	private Timer _specialTimer;

	// ← NUEVO: ruta de escena capturada en _Ready, segura para usar en _ExitTree
	private string _scenePath = "";

	public override void _Ready()
	{
		// Capturar la escena ANTES de cualquier otra cosa
		_scenePath = GetTree().CurrentScene?.SceneFilePath ?? "";

		_anim   = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_health = GetNode<HealthComponent>("HealthComponent");
		if (_health != null)
			_health.Died += OnDied;

		_player = GetTree().GetFirstNodeInGroup("Player") as CharacterBody2D;

		// === CARGAR ESTADO GUARDADO (muerte + posición) ===
		if (GameState.Instance != null)
		{
			var state         = GameState.Instance.GetState(UniqueId);
			bool isDeadFromSave = (bool)state["dead"];

			if (isDeadFromSave)
			{
				_isDead              = true;
				_isInSpecialAnimation = false;
				Velocity             = Vector2.Zero;

				_anim.Stop();
				_anim.Play("death");

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
				// ← FIX PRINCIPAL: solo restaurar posición si venimos de la misma escena
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

		SetupSpecialAnimationCycle();
		GD.Print("Enemigo listo - Ciclo de animaciones especiales iniciado");
	}

	// ====================== ANIMACIONES ESPECIALES ======================
	private void SetupSpecialAnimationCycle()
	{
		_specialTimer = new Timer();
		AddChild(_specialTimer);
		_specialTimer.Timeout   += OnSpecialTimerTimeout;
		_specialTimer.WaitTime  = 5.0f;
		_specialTimer.OneShot   = true;
		_specialTimer.Start();
	}

	private void OnSpecialTimerTimeout()
	{
		if (_isDead || _anim == null) return;
		GD.Print("→ Iniciando SpecialAnim1 (idle_special1)");
		PlaySpecialAnimation1();
	}

	private void PlaySpecialAnimation1()
	{
		if (!_anim.HasAnimation(SpecialAnim1))
		{
			GD.PrintErr($"Error: Animación {SpecialAnim1} no existe");
			ScheduleNextCycle(10.0f);
			return;
		}
		_isInSpecialAnimation = true;
		_anim.Play(SpecialAnim1);
		float animLength = (float)_anim.GetAnimation(SpecialAnim1).Length;

		GetTree().CreateTimer(animLength + 3.0f).Timeout += () =>
		{
			if (_isDead) return;
			_isInSpecialAnimation = false;
			GD.Print("→ SpecialAnim1 terminada. Iniciando SpecialAnim2");
			PlaySpecialAnimation2();
		};
	}

	private void PlaySpecialAnimation2()
	{
		if (!_anim.HasAnimation(SpecialAnim2))
		{
			GD.PrintErr($"Error: Animación {SpecialAnim2} no existe");
			ScheduleNextCycle(10.0f);
			return;
		}
		_isInSpecialAnimation = true;
		_anim.Play(SpecialAnim2);
		float animLength = (float)_anim.GetAnimation(SpecialAnim2).Length;

		GetTree().CreateTimer(animLength).Timeout += () =>
		{
			if (_isDead) return;
			_isInSpecialAnimation = false;
			GD.Print("→ SpecialAnim2 terminada. Esperando próximo ciclo...");
			ScheduleNextCycle(10.0f);
		};
	}

	private void ScheduleNextCycle(float waitTime)
	{
		if (_isDead || _specialTimer == null) return;
		_specialTimer.WaitTime = waitTime;
		_specialTimer.OneShot  = true;
		_specialTimer.Start();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _player == null || !GodotObject.IsInstanceValid(_player))
			return;

		UpdateFacingDirection();

		if (_isInSpecialAnimation)
		{
			if (SpecialAnim1 == _anim.CurrentAnimation ||
				(StaticDuringSpecial2 && SpecialAnim2 == _anim.CurrentAnimation))
				Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// === LÓGICA NORMAL DE PERSECUCIÓN ===
		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
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

	private void UpdateFacingDirection()
	{
		if (_player == null) return;
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		if (Mathf.Abs(direction.X) > 0.1f)
			_sprite.FlipH = direction.X < 0;
	}

	private void StartAttack()
	{
		if (_isAttacking || _isDead || _isInSpecialAnimation) return;
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

	private void OnDied()
	{
		if (_isDead) return;
		_isDead              = true;
		_isInSpecialAnimation = false;
		Velocity             = Vector2.Zero;

		if (_specialTimer != null)
			_specialTimer.Stop();

		_anim.Stop();
		_anim.Play("death");

		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
			collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		if (_health != null)
			_health.SetDeferred("monitoring", false);

		GD.Print("El enemigo ha caído.");

		// ← FIX: guardamos también la escena al morir
		if (GameState.Instance != null)
			GameState.Instance.SetState(UniqueId, true, GlobalPosition, "death", 0,
										"", 0, 0, _scenePath);

		GD.Print($"Estado guardado → {UniqueId} muerto en '{_scenePath}'");
	}

	// === GUARDAR POSICIÓN MIENTRAS ESTÁ VIVO ===
	public override void _ExitTree()
	{
		if (GameState.Instance != null && !_isDead)
		{
			// ← FIX: usamos _scenePath (capturado en _Ready) en lugar de leer CurrentScene aquí
			GameState.Instance.SetState(UniqueId, false, GlobalPosition,
										"", 0, "", 0, 0, _scenePath);
			GD.Print($"📍 Posición de {UniqueId} guardada: {GlobalPosition} en '{_scenePath}'");
		}
		base._ExitTree();
	}
}
