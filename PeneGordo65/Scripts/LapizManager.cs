using Godot;

public partial class Lapiz : Area2D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Protagonista jugador)
		{
			jugador.TieneLapiz = true;
			QueueFree();
		}
	}
}
