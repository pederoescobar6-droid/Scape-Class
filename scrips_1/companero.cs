using Godot;

public partial class Companero : CharacterBody2D
{
	[Export] public NodePath ProtagonistaPath; // Asigna la referencia al Protagonista desde el Editor
	private Protagonista _protagonista;
	private AnimatedSprite2D _animatedSprite;

	private bool _animacionAlternativa = false; // Controla si se activa la animación con E

	public override void _Ready()
	{
		// Obtenemos el nodo Protagonista
		if (ProtagonistaPath != null)
			_protagonista = GetNode<Protagonista>(ProtagonistaPath);
		else
			GD.PrintErr("ProtagonistaPath no asignado en Companero");

		// Obtenemos el AnimatedSprite2D
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_protagonista == null)
			return;

		// Copiamos la posición y rotación del protagonista
		GlobalPosition = _protagonista.GlobalPosition;
		Rotation = _protagonista.Rotation;

		// Si se presiona E, alternamos animación
		if (Input.IsActionJustPressed("e"))
		{
			_animacionAlternativa = !_animacionAlternativa;
			CambiarAnimacion();
		}
	}

	private void CambiarAnimacion()
	{
		if (_animatedSprite == null) return;

		if (_animacionAlternativa)
			_animatedSprite.Play("accion"); // Reemplaza por tu animación “de E”
		else
			_animatedSprite.Play("default"); // Vuelve al estado normal
	}
}
	
