using Godot;
using System;
using System.Threading.Tasks;

public partial class Protagonista : CharacterBody2D
{
	[Export] public float Speed = 150.0f;
	[Export] public int Salud = 100;

	[Export] public float DashInitialSpeed = 700.0f;
	[Export] public float DashFriction = 0.08f;
	private Vector2 _dashVelocity = Vector2.Zero;
	private bool _isDashing = false;
	private bool _canDash = true;

	private bool _isAttacking = false;
	private bool _puedenDañarme = true;

	private AnimatedSprite2D _anim;
	private AudioStreamPlayer2D _audioPasos;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_audioPasos = GetNodeOrNull<AudioStreamPlayer2D>("AudioPasos");
		_anim.AnimationFinished += OnAnimationFinished;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDashing)
		{
			_dashVelocity = _dashVelocity.Lerp(Vector2.Zero, DashFriction);
			Velocity = _dashVelocity;

			if (_dashVelocity.Length() < 10.0f)
			{
				TerminarDash();
			}

			MoveAndSlide();
			return;
		}

		if (_isAttacking)
		{
			MoveAndSlide();
			return;
		}

		Vector2 direction = Input.GetVector("mover_izq", "mover_der", "mover_arriba", "mover_abajo");

		if (Input.IsActionJustPressed("atacar"))
		{
			Atacar();
			return;
		}

		if (Input.IsActionJustPressed("esquivar") && _canDash && direction != Vector2.Zero)
		{
			EjecutarDash(direction);
			return;
		}

		if (direction != Vector2.Zero)
		{
			Velocity = direction * Speed;
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Speed);
		}

		MoveAndSlide();
		ActualizarAnimaciones(direction);
	}

	private void EjecutarDash(Vector2 dir)
	{
		_isDashing = true;
		_canDash = false;
		
		_dashVelocity = dir.Normalized() * DashInitialSpeed;

		if (_anim.SpriteFrames.HasAnimation("dash"))
		{
			if (_audioPasos != null && _audioPasos.Playing) _audioPasos.Stop();
			_anim.Play("dash");
			if (dir.X != 0) _anim.FlipH = dir.X < 0;
		}
		
		GetTree().CreateTimer(1.0f).Connect("timeout", Callable.From(() => _canDash = true));
	}

	private void TerminarDash()
	{
		_isDashing = false;
	}

	private void Atacar()
	{
		if (_anim.SpriteFrames.HasAnimation("ataque"))
		{
			_isAttacking = true;
			Velocity = Vector2.Zero;
			if (_audioPasos != null && _audioPasos.Playing) _audioPasos.Stop();
			_anim.Play("ataque");
		}
	}

	public void RecibirDaño(int cantidad)
	{
		if (!_puedenDañarme) return;
		Salud -= cantidad;
		if (Salud <= 0) GetTree().CallDeferred("reload_current_scene");
		else EfectoDañado();
	}

	private async void EfectoDañado()
	{
		_puedenDañarme = false;
		Modulate = new Color(1, 0, 0); 
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		Modulate = new Color(1, 1, 1); 
		_puedenDañarme = true;
	}

	private void OnAnimationFinished()
	{
		if (_anim.Animation == "ataque")
		{
			_isAttacking = false;
			_anim.Play("idle");
		}
	}

	private void ActualizarAnimaciones(Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			_anim.Play("caminar");
			if (direction.X != 0) _anim.FlipH = direction.X < 0;
			if (_audioPasos != null && !_audioPasos.Playing) _audioPasos.Play();
		}
		else
		{
			_anim.Play("idle");
			if (_audioPasos != null && _audioPasos.Playing) _audioPasos.Stop();
		}
	}
}
