using Godot;

[GlobalClass]
public partial class HealthComponent : Area2D
{
	[Export] public int MaxHealth { get; set; } = 10;
	public int CurrentHealth { get; private set; }

	[Signal] public delegate void HealthChangedEventHandler(int newHealth);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		// Importante: Configura el monitoreo para recibir áreas
		Monitoring = true; 
		Monitorable = true;
	}

	public void TakeDamage(int damage)
	{
		if (damage <= 0 || CurrentHealth <= 0) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
		EmitSignal(SignalName.HealthChanged, CurrentHealth);

		if (CurrentHealth <= 0)
		{
			EmitSignal(SignalName.Died);
		}
	}
}
