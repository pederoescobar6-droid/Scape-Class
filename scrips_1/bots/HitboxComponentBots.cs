// =============================================
// HitboxComponentBots.cs
// =============================================
using Godot;

public partial class HitboxComponentBots : Area2D
{
	[Export] public int Damage = 20;

	public void DealDamageToOverlaps()
	{
		// ✅ GetOverlappingAreas en lugar de GetOverlappingBodies
		// porque HealthComponent es un Area2D, no un CharacterBody2D
		foreach (var area in GetOverlappingAreas())
		{
			if (area is HealthComponent health)
			{
				health.TakeDamage(Damage);
				GD.Print($"[HitboxComponentBots] Daño aplicado: {Damage}");
			}
		}
	}
}
