using Godot;

public partial class Pasillo_atac1 : Area2D
{
	[Export]
	public string NextScenePath = "res://Escenas/clases/clase_attack.tscn";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		Monitoring = true;
		CollisionLayer = 0;           // No ocupa capa de colisión física
		CollisionMask = 1;            // Detecta todo lo que esté en la layer 1 (cambia si tu jugador usa otra)

		GD.Print("=== PASILLO LISTO ===");
		GD.Print("Monitoring: ", Monitoring);
		GD.Print("CollisionMask: ", CollisionMask);
	}

	private void OnBodyEntered(Node2D body)
	{
		GD.Print("=== BODY ENTERED DETECTADO ===");
		GD.Print($"Nombre: {body.Name}");
		GD.Print($"Tipo: {body.GetType()}");
		GD.Print($"Parent: {body.GetParent()?.Name ?? "null"}");
		GD.Print($"¿Está en grupo Player? {body.IsInGroup("Player")}");

		// Detectamos jugador de varias formas posibles
		bool esJugador = 
			body.IsInGroup("Player") ||
			body.GetParent()?.IsInGroup("Player") == true ||
			body.Name.ToString().Contains("Prota") ||
			body is Protagonista;   // si tienes la clase Protagonista

		if (esJugador)
		{
			GD.Print("¡JUGADOR DETECTADO! Cambiando escena...");

			if (!string.IsNullOrEmpty(NextScenePath))
			{
				GetTree().ChangeSceneToFile(NextScenePath);
			}
			else
			{
				GD.PrintErr("NextScenePath está vacío");
			}
		}
		else
		{
			GD.Print("Body detectado pero NO es el jugador");
		}
	}
}
