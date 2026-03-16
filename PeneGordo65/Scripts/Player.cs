using Godot;

public partial class Player : CharacterBody3D
{
	private Node currentWeapon;

	private AnimationPlayer animationPlayer;
	private Node weaponHolder;

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		weaponHolder = GetNode("WeaponHolder");
	}

	public void PickWeapon(PackedScene weaponScene)
	{
		if (currentWeapon != null)
			currentWeapon.QueueFree();

		currentWeapon = weaponScene.Instantiate();
		weaponHolder.AddChild(currentWeapon);

		animationPlayer.Play("idle_with_weapon");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("attack"))
		{
			Attack();
		}
	}

	private void Attack()
	{
		if (currentWeapon != null)
		{
			animationPlayer.Play("attack_weapon");
		}
		else
		{
			animationPlayer.Play("punch");
		}
	}
}
