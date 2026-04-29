using Godot;

[GlobalClass]
public partial class HealthComponent_JPelirrojo : Area2D
{
	[Export] public int MaxHealth { get; set; } = 10;
	public int CurrentHealth { get; private set; }

	[Signal] public delegate void HealthChangedEventHandler(int newHealth);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		Monitoring    = true;
		Monitorable   = true;
		AreaEntered   += OnAreaEntered;
	}

	private void OnAreaEntered(Area2D area)
	{
		// Golpe del propio enemigo (HitboxComponent con sufijo)
		if (area is HitboxComponent_JPelirrojo enemyHitbox)
			TakeDamage(enemyHitbox.Damage);

		// Golpe del protagonista (HitboxComponent sin sufijo)
		else if (area is HitboxComponent playerHitbox)
			TakeDamage(playerHitbox.Damage);
	}

	public void TakeDamage(int damage)
	{
		if (damage <= 0 || CurrentHealth <= 0) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
		EmitSignal(SignalName.HealthChanged, CurrentHealth);

		if (CurrentHealth <= 0)
			EmitSignal(SignalName.Died);
	}
}
