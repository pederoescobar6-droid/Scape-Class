using Godot;
using System;
using System.Threading.Tasks;

public partial class Protagonista : CharacterBody2D
{
	[Export] public float Speed = 150.0f;
	[Export] public int Salud = 100;

	private bool _tieneLapiz = false;
	public bool TieneLapiz
	{
		get => _tieneLapiz;
		set
		{
			_tieneLapiz = value;
			ActualizarVisualesEquipo();
		}
	}

	[Export] public float DashInitialSpeed = 700.0f;
	[Export] public float DashFriction = 0.08f;

	private Vector2 _dashVelocity = Vector2.Zero;
	private bool _isDashing = false;
	private bool _canDash = true;

	private bool _isAttacking = false;
	private bool _puedenDañarme = true;
	private bool _estaMuerto = false;

	private AnimatedSprite2D _cuerpoAnim;
	private AnimatedSprite2D _manoAnim;
	private Sprite2D _lapizSprite;
	private Control _interfazMuerte;

	public override void _Ready()
	{
		_cuerpoAnim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_manoAnim = GetNode<AnimatedSprite2D>("ManoAnim");
		_lapizSprite = GetNode<Sprite2D>("ManoAnim/LapizSprite");

		_interfazMuerte = GetParent().GetNodeOrNull<Control>("CanvasLayer/InterfazMuerte");

		_cuerpoAnim.AnimationFinished += OnAnimationFinished;

		// Estado inicial
		_manoAnim.Hide();
		_lapizSprite.Hide();

		// Orden visual
		_cuerpoAnim.ZIndex = 10;
		_manoAnim.ZIndex = 11;
		_lapizSprite.ZIndex = 12;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_estaMuerto)
		{
			if (Input.IsKeyPressed(Key.R))
				GetTree().ReloadCurrentScene();
			return;
		}

		if (_isDashing)
		{
			_dashVelocity = _dashVelocity.Lerp(Vector2.Zero, DashFriction);
			Velocity = _dashVelocity;

			if (_dashVelocity.Length() < 10f)
				_isDashing = false;

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

		Velocity = direction != Vector2.Zero ? direction * Speed : Velocity.MoveToward(Vector2.Zero, Speed);

		MoveAndSlide();
		ActualizarAnimaciones(direction);
	}

	private void ActualizarVisualesEquipo()
	{
		if (_tieneLapiz)
		{
			_manoAnim.Show();
			_lapizSprite.Show();
			_manoAnim.Play("idle");
			GD.Print("¡Lápiz equipado!");
		}
	}

	private void ActualizarAnimaciones(Vector2 direction)
	{
		string sufijo = _tieneLapiz ? "_sin_mano" : "";

		if (direction != Vector2.Zero)
		{
			ReproducirSeguro(_cuerpoAnim, "caminar" + sufijo);

			bool flip = direction.X < 0;
			_cuerpoAnim.FlipH = flip;

			if (_tieneLapiz)
			{
				_manoAnim.Play("caminar");
				_manoAnim.FlipH = flip;
				_lapizSprite.FlipH = flip;

				AjustarPosicionMano(flip);
			}
		}
		else
		{
			ReproducirSeguro(_cuerpoAnim, "idle" + sufijo);

			if (_tieneLapiz)
			{
				_manoAnim.Play("idle");
				bool flip = _cuerpoAnim.FlipH;
				_manoAnim.FlipH = flip;
				_lapizSprite.FlipH = flip;

				AjustarPosicionMano(flip);
			}
		}
	}

	private void AjustarPosicionMano(bool mirandoIzquierda)
	{
		float offset = mirandoIzquierda ? -5f : 5f;
		_manoAnim.Position = new Vector2(offset, 0);
	}

	private void EjecutarDash(Vector2 dir)
	{
		_isDashing = true;
		_canDash = false;
		_dashVelocity = dir.Normalized() * DashInitialSpeed;

		ReproducirSeguro(_cuerpoAnim, "dash");

		// Recuperar dash después de 1 segundo
		GetTree().CreateTimer(1.0).Timeout += () => _canDash = true;
	}

	private void Atacar()
	{
		if (_cuerpoAnim.SpriteFrames.HasAnimation("ataque"))
		{
			_isAttacking = true;
			Velocity = Vector2.Zero;
			_cuerpoAnim.Play("ataque");
		}
	}

	public void RecibirDano(int cantidad)
	{
		if (!_puedenDañarme || _estaMuerto)
			return;

		Salud -= cantidad;

		if (Salud <= 0)
			Morir();
		else
			_ = EfectoDañado(); // Fire-and-forget safely
	}

	private void Morir()
	{
		_estaMuerto = true;
		Velocity = Vector2.Zero;

		if (_interfazMuerte != null)
			_interfazMuerte.Visible = true;

		ReproducirSeguro(_cuerpoAnim, "idle");
		Modulate = new Color(0.3f, 0.3f, 0.3f);
	}

	private async Task EfectoDañado()
	{
		_puedenDañarme = false;
		Modulate = new Color(1, 0, 0);

		await ToSignal(GetTree().CreateTimer(0.5), "timeout");

		if (!_estaMuerto)
			Modulate = new Color(1, 1, 1);

		_puedenDañarme = true;
	}

	private void OnAnimationFinished()
	{
		if (_cuerpoAnim.Animation.ToString().Contains("ataque"))
			_isAttacking = false;
	}

	private void ReproducirSeguro(AnimatedSprite2D animNode, string nombre)
	{
		if (animNode.SpriteFrames.HasAnimation(nombre))
			animNode.Play(nombre);
	}
}
