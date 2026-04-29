using Godot;

public partial class Idle : CharacterBody2D
{
	private AnimationPlayer _animationPlayer;

	public override void _Ready()
	{
		// Asume que este CharacterBody2D tiene un hijo AnimationPlayer
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		// Reproduce la animación "idle"
		_animationPlayer.Play("idle");
	}
}
