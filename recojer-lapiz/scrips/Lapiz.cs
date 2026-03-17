using Godot;

public partial class Lapiz : Area2D
{
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		Monitoring = true;
	}

	private void OnBodyEntered(Node2D body)
	{
		GD.Print("=== DEBUG ===");
		GD.Print("body.Name = '" + body.Name + "'");
		GD.Print("body.GetType() = " + body.GetType());
		
		// PRUEBA TODAS estas opciones:
		if (body.Name.ToString() == "Protagonista" || 
			body.Name.ToString().Contains("Protagonista") ||
			body is Protagonista)
		{
			GD.Print("✅ LÁPIZ RECOGIDO!");
			QueueFree();
		}
		else
		{
			GD.Print("❌ No es Protagonista");
		}
	}
}
