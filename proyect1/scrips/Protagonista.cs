using Godot;

public partial class Protagonista : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 50f;

	// Referencia al nodo de animación
	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		// Obtenemos el nodo. Asegúrate de que las comillas sean rectas " "
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		Velocity = inputDir * Speed;
		MoveAndSlide();

		// Lógica de animación
		ActualizarAnimacion(inputDir);
	}

	private void ActualizarAnimacion(Vector2 direccion)
	{
		if (direccion != Vector2.Zero)
		{
			// Cambia "walk" por el nombre que le hayas puesto a tu animación de caminar
			_animatedSprite.Play("walk");

			if (direccion.X != 0)
			{
				_animatedSprite.FlipH = direccion.X < 0;
			}
		}
		else
		{
			// Cambia "default" o "idle" según tus animaciones
			_animatedSprite.Play("default"); 
		}
	}
}
