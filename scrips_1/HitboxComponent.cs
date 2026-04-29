using Godot;

[GlobalClass]
public partial class HitboxComponent : Area2D
{
	[Export] public int Damage { get; set; } = 1;   // Este valor lo cambia el jugador según el arma

	public override void _Ready()
	{
		// Conectar la señal que ya usabas
		AreaEntered += OnAreaEntered;

		// El hitbox siempre empieza desactivado (solo se activa al atacar)
		Monitoring = false;
	}

	private void OnAreaEntered(Area2D area)
	{
		// Si el área que entra es un HealthComponent (como tienes montado ahora)
		if (area is HealthComponent health)
		{
			health.TakeDamage(Damage);
		}
	}
}
