using Godot;

public partial class WeaponPickup : Area3D
{
	[Export] public PackedScene WeaponScene;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player player)
		{
			player.PickWeapon(WeaponScene);
			QueueFree(); // eliminar arma del suelo
		}
	}
}
