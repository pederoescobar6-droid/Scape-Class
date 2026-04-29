using Godot;

[GlobalClass]
public partial class HitboxComponent_JPelirrojo : Area2D
{
	[Export] public int Damage { get; set; } = 1; // El enemigo sobreescribe este valor en cada ataque

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		Monitoring   = false; // Solo activo durante el frame del ataque
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is HealthComponent_JPelirrojo health)
			health.TakeDamage(Damage);
	}
}
