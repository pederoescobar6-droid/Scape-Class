using Godot;
using System;

public partial class Prota_arm : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 250.0f;
	[Export] public float AttackSpeed { get; set; } = 1.0f;
	[Export] public float SpecialDashSpeed { get; set; } = 250.0f;
	[Export] public float SpecialAnimSpeed { get; set; } = 0.35f;
	[Export] public float SpecialDuration { get; set; } = 0.7f;

	[ExportGroup("Durabilidad de Armas")]
	[Export] public int PencilDurability = 10;
	[Export] public int ScissorsDurability = 8;
	[Export] public int RulerDurability = 12;
	[Export] public int ExtinguisherDurability = 6;

	[ExportGroup("Daño por Arma (Inspector)")]
	[Export] public int PunchDamage = 8;
	[Export] public int PencilDamage = 12;
	[Export] public int ScissorsDamage = 22;
	[Export] public int RulerDamage = 15;
	[Export] public int ExtinguisherDamage = 35;

	// === PERSISTENCIA DEL JUGADOR ===
	[Export] public string UniqueId { get; set; } = "player";

	private WeaponType currentWeapon = WeaponType.None;
	private string currentAttackAnimation = "attack";
	private int currentDurability = 0;
	private int currentDamage = 0;

	private AnimationPlayer _animationPlayer;
	private Sprite2D _sprite;
	private HealthComponent _health;
	private Label _gameOverLabel;
	private bool _isDead = false;
	private bool _isAttacking = false;
	private bool _isSpecial = false;
	private float _specialCooldownTimer = 0f;
	private const float SpecialCooldownDuration = 10f;
	private Vector2 _specialDirection = Vector2.Zero;

	// ← NUEVO: ruta de escena capturada en _Ready, segura para usar en _ExitTree
	private string _scenePath = "";

	public override void _Ready()
	{
		// Capturar la escena ANTES de cualquier otra cosa
		_scenePath = GetTree().CurrentScene?.SceneFilePath ?? "";

		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_health = GetNode<HealthComponent>("HealthComponent");
		_health.Died += OnDied;
		AddToGroup("Player");

		_gameOverLabel = GetNodeOrNull<Label>("CanvasLayer/GameOverLabel");
		if (_gameOverLabel != null)
			_gameOverLabel.Visible = false;

		// === CARGAR TODO EL ESTADO DEL JUGADOR ===
		if (GameState.Instance != null)
		{
			var state = GameState.Instance.GetState(UniqueId);

			// ← FIX PRINCIPAL: solo restaurar posición si venimos de la misma escena
			string savedScene = (string)state["scene"];
			Vector2 savedPos  = (Vector2)state["position"];

			if (savedScene == _scenePath && savedPos.Length() > 0.1f)
			{
				GlobalPosition = savedPos;
				GD.Print($"✅ Posición restaurada en '{_scenePath}': {savedPos}");
			}
			else if (savedScene != _scenePath && savedScene != "")
			{
				GD.Print($"🔄 Escena nueva ('{_scenePath}'), ignorando posición guardada de '{savedScene}'");
			}

			string savedWeapon = (string)state["weapon"];
			if (!string.IsNullOrEmpty(savedWeapon))
			{
				currentWeapon          = Enum.Parse<WeaponType>(savedWeapon);
				currentDurability      = (int)state["durability"];
				currentDamage          = (int)state["damage"];
				currentAttackAnimation = GetAttackAnimForWeapon(currentWeapon);
				GD.Print($"✅ Jugador cargado - Arma: {currentWeapon} | Dur: {currentDurability} | Daño: {currentDamage}");
			}
		}

		currentDamage = currentDamage > 0 ? currentDamage : PunchDamage;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_specialCooldownTimer > 0f)
			_specialCooldownTimer -= (float)delta;

		if (_isDead || _isAttacking || _isSpecial)
		{
			if (_isSpecial)
			{
				Velocity = _specialDirection * SpecialDashSpeed;
				MoveAndSlide();
			}
			return;
		}

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = input * Speed;
		MoveAndSlide();

		if (input.Length() > 0)
		{
			_animationPlayer.Play("run");
			if (input.X != 0)
				_sprite.FlipH = input.X < 0;
		}
		else
		{
			_animationPlayer.Play("idle");
		}

		if (Input.IsActionJustPressed("ui_accept")) StartAttack();
		if (Input.IsActionJustPressed("special") && _specialCooldownTimer <= 0f)
			StartSpecial();
	}

	// ====================== MÉTODOS ORIGINALES (CORREGIDOS) ======================
	private void StartAttack()
	{
		_isAttacking = true;
		string animToPlay = currentAttackAnimation;
		bool consumeDurability = currentWeapon != WeaponType.None && currentDurability > 0;
		if (consumeDurability)
		{
			currentDurability--;
			if (currentDurability <= 0)
			{
				currentWeapon          = WeaponType.None;
				currentAttackAnimation = "attack";
				currentDamage          = PunchDamage;
				GD.Print("¡El arma se ha roto!");
			}
		}
		_animationPlayer.Play(animToPlay, customSpeed: AttackSpeed);

		var hitbox = GetNodeOrNull<HitboxComponent>("HitboxComponent");
		if (hitbox != null)
		{
			hitbox.Damage     = currentDamage;
			hitbox.Monitoring = true;
		}
		GetTree().CreateTimer(0.4f / AttackSpeed).Timeout += OnAttackFinished;
	}

	private void OnAttackFinished()
	{
		_isAttacking = false;
		var hitbox = GetNodeOrNull<HitboxComponent>("HitboxComponent");
		if (hitbox != null) hitbox.Monitoring = false;
	}

	public void EquipWeapon(WeaponType newWeapon)
	{
		currentWeapon          = newWeapon;
		currentAttackAnimation = GetAttackAnimForWeapon(newWeapon);
		currentDurability      = GetMaxDurabilityForWeapon(newWeapon);
		currentDamage          = GetDamageForWeapon(newWeapon);
		GD.Print($"¡Arma equipada: {newWeapon}! Daño: {currentDamage} | Durabilidad: {currentDurability}");
	}

	private string GetAttackAnimForWeapon(WeaponType weapon)
	{
		switch (weapon)
		{
			case WeaponType.Pencil:       return "attack_pencil";
			case WeaponType.Scissors:     return "attack_scissors";
			case WeaponType.Ruler:        return "attack_ruler";
			case WeaponType.Extinguisher: return "attack_extinguisher";
			default:                      return "attack";
		}
	}

	private int GetMaxDurabilityForWeapon(WeaponType weapon)
	{
		switch (weapon)
		{
			case WeaponType.Pencil:       return PencilDurability;
			case WeaponType.Scissors:     return ScissorsDurability;
			case WeaponType.Ruler:        return RulerDurability;
			case WeaponType.Extinguisher: return ExtinguisherDurability;
			default:                      return 0;
		}
	}

	private int GetDamageForWeapon(WeaponType weapon)
	{
		switch (weapon)
		{
			case WeaponType.Pencil:       return PencilDamage;
			case WeaponType.Scissors:     return ScissorsDamage;
			case WeaponType.Ruler:        return RulerDamage;
			case WeaponType.Extinguisher: return ExtinguisherDamage;
			default:                      return PunchDamage;
		}
	}

	private void StartSpecial()
	{
		_specialCooldownTimer = SpecialCooldownDuration;
		_isSpecial            = true;
		_isAttacking          = true;
		_specialDirection     = _sprite.FlipH ? Vector2.Left : Vector2.Right;

		if (_animationPlayer.HasAnimation("special"))
			_animationPlayer.Play("special", customSpeed: SpecialAnimSpeed);
		else
		{
			GD.PrintErr("¡Error! La animación 'special' no existe.");
			_isSpecial   = false;
			_isAttacking = false;
			return;
		}
		float realDuration = SpecialDuration / SpecialAnimSpeed;
		GetTree().CreateTimer(realDuration).Timeout += OnSpecialFinished;
	}

	private void OnSpecialFinished()
	{
		_isSpecial   = false;
		_isAttacking = false;
		Velocity     = Vector2.Zero;
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (input.Length() > 0)
			_animationPlayer.Play("run");
		else
			_animationPlayer.Play("idle");
	}

	private void OnDied()
	{
		if (_isDead) return;
		_isDead  = true;
		Velocity = Vector2.Zero;

		if (_animationPlayer.HasAnimation("death"))
			_animationPlayer.Play("death");
		else
			_animationPlayer.Play("idle");

		if (_gameOverLabel != null)
		{
			_gameOverLabel.Text    = "卐卐 Te jodes 卐卐";
			_gameOverLabel.Visible = true;
		}
		_animationPlayer.AnimationFinished += OnDeathAnimationFinished;
	}

	private void OnDeathAnimationFinished(StringName animName)
	{
		if (animName == "death")
		{
			_animationPlayer.AnimationFinished -= OnDeathAnimationFinished;
			CambiarEscenaConPausa();
		}
	}

	private async void CambiarEscenaConPausa()
	{
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		GetTree().ChangeSceneToFile("res://Escenas/clases/pantalla_inic.tscn");
	}

	// ====================== GUARDAR ESTADO AL SALIR DE LA ESCENA ======================
	public override void _ExitTree()
	{
		if (GameState.Instance != null && !_isDead)
		{
			// ← FIX: pasamos _scenePath (capturado en _Ready) para saber a qué escena pertenece
			GameState.Instance.SetState(
				UniqueId,
				false,
				GlobalPosition,
				"",
				0,
				currentWeapon.ToString(),
				currentDurability,
				currentDamage,
				_scenePath          // ← escena de origen
			);
			GD.Print($"📍 Jugador guardado - Escena: {_scenePath} | Pos: {GlobalPosition} | Arma: {currentWeapon}");
		}
		base._ExitTree();
	}
}
