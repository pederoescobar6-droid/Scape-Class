using Godot;

public partial class Lapiz : Area2D
{
	[Export]
	public string NextScenePath = "res://Escenas/clases/clase_attack.tscn";

	[Export]
	public string CharacterId = "Jugador";

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		Monitoring = true;
	}

	private void OnBodyEntered(Node2D body)
	{
		GD.Print("=== DEBUG BodyEntered ===");
		GD.Print($"Cuerpo detectado: {body.Name} | Tipo: {body.GetType().Name}");

		if (body.Name != CharacterId)
		{
			GD.Print($"→ Ignorado (no es el protagonista, se esperaba '{CharacterId}')");
			return;
		}

		GD.Print("=== LÁPIZ RECOGIDO POR EL PROTAGONISTA! ===");

		if (string.IsNullOrEmpty(NextScenePath))
		{
			GD.PrintErr("NextScenePath no está configurado");
			return;
		}

		Error err = GetTree().ChangeSceneToFile(NextScenePath);
		if (err != Error.Ok)
		{
			GD.PrintErr($"❌ ERROR al cambiar de escena: {err}");
			GD.PrintErr($"   Ruta utilizada: {NextScenePath}");
			GD.PrintErr("   → Revisa que el archivo exista y la ruta sea correcta");
		}
		else
		{
			GD.Print("✅ Cambio de escena iniciado correctamente");
		}

		QueueFree();
	}  // ← cierra OnBodyEntered
}      // ← cierra la clase
