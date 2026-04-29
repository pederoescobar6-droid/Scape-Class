using Godot;

public partial class pantalla_final : Area2D
{
// Cambia esto a la ruta real de tu escena:
// por ejemplo: "res://Escenas/Nivel2.tscn"
[Export]
public string NextScenePath = "res://Escenas/clases/pantalla_final.tscn";

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

if (body.Name.ToString() == "Protagonista" ||
body.Name.ToString().Contains("Protagonista") ||
body is Protagonista)
{
GD.Print("Cobarde");

// Cambiar de escena
if (!string.IsNullOrEmpty(NextScenePath))
{
GetTree().ChangeSceneToFile(NextScenePath); // Godot 4 C#[web:5][web:8]
}
else
{
GD.PrintErr("NextScenePath no está configurado");

}

QueueFree();
}
else
{
GD.Print(" No es Protagonista");
}
}
}
