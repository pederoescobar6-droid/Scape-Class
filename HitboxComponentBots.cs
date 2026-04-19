// =============================================
// HitboxComponentBots.cs
// =============================================
using Godot;

public partial class HitboxComponent : Area2D
{
    [Export]
    public int Damage = 20;

    /// <summary>
    /// Llama a este método cuando el bot activo realiza un ataque (se llama una sola vez por ataque).
    /// Evita daño continuo aunque el jugador esté solapado.
    /// </summary>
    public void DealDamageToOverlaps()
    {
        var overlappingBodies = GetOverlappingBodies();
        foreach (var body in overlappingBodies)
        {
            if (body.IsInGroup("player"))
            {
                var healthComponent = body.GetNodeOrNull<HealthComponent>("HealthComponent");
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(Damage);
                }
            }
        }
    }
}
