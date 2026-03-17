using Godot;

public partial class Protagonista : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 300f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = inputDir * Speed;
		MoveAndSlide();
	}
}
