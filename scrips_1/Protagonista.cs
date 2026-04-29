using Godot;

public partial class Protagonista : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 50f;

	private AnimatedSprite2D _animatedSprite;
	private Label _gameOverLabel;

	public bool IsDead { get; private set; } = false;

	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Intentamos encontrar el Label de varias formas
		_gameOverLabel = GetNodeOrNull<Label>("CanvasLayer/Label");

		if (_gameOverLabel == null)
			_gameOverLabel = GetNodeOrNull<Label>("CanvasLayer/GameOverLabel");

		if (_gameOverLabel == null)
			_gameOverLabel = GetNodeOrNull<Label>("%Label");

		if (_gameOverLabel != null)
		{
			_gameOverLabel.Visible = false;
			GD.Print("✓ Label encontrado correctamente");
		}
		else
		{
			GD.PrintErr("✗ No se encontró el Label. Revisa la estructura de la escena.");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = inputDir * Speed;
		MoveAndSlide();

		ActualizarAnimacion(inputDir);
	}

	private void ActualizarAnimacion(Vector2 direccion)
	{
		if (IsDead) return;

		if (direccion != Vector2.Zero)
		{
			_animatedSprite.Play("walk");
			if (direccion.X != 0)
				_animatedSprite.FlipH = direccion.X < 0;
		}
		else
		{
			_animatedSprite.Play("idle");
		}
	}

	public void Morir()
	{
		if (IsDead) return;

		IsDead = true;
		Velocity = Vector2.Zero;

		GD.Print("Personaje ha muerto - Reproduciendo animación death");

		_animatedSprite.Play("death");
		_animatedSprite.AnimationFinished += OnDeathAnimationFinished;
	}

	private void OnDeathAnimationFinished()
	{
		if (_animatedSprite.Animation != "death") return;

		_animatedSprite.AnimationFinished -= OnDeathAnimationFinished;

		GD.Print("Animación de muerte terminada");

		if (_gameOverLabel != null)
		{
			_gameOverLabel.Text = "GAME OVER";
			_gameOverLabel.Visible = true;
			GD.Print("✓ Label 'GAME OVER' mostrado");
		}
		else
		{
			GD.PrintErr("✗ No se pudo mostrar el Label porque no se encontró");
		}

		CambiarEscenaConPausa();
	}

	private async void CambiarEscenaConPausa()
	{
		GD.Print("Esperando 1.5 segundos antes de cambiar de escena...");
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
		GD.Print("Cambiando a escena GameOver.tscn");
		GetTree().ChangeSceneToFile("res://GameOver.tscn");
	}
}
