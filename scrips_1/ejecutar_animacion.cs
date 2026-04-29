using Godot;

public partial class Ejecutar_animacion : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 50f;

	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		// 1. Buscamos el nodo por su nombre en la escena
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		// 2. Ejecutamos la animación por su nombre (el que sale en la lista de abajo)
		// Casi siempre la primera animación se llama "default"
		_animatedSprite.Play("idle"); 
	}
}
