// =============================================
// 1. HealthComponent.cs
// =============================================
using Godot;

public partial class HealthComponent : Node
{
    [Export]
    public int MaxHealth = 100;

    public int CurrentHealth { get; private set; }

    [Signal]
    public delegate void HealthDepletedEventHandler();

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0)
            CurrentHealth = 0;

        if (CurrentHealth == 0)
        {
            EmitSignal("HealthDepleted");
        }
    }
}
