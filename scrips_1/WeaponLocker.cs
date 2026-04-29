using Godot;

public partial class WeaponLocker : Area2D
{
	[Export] public float PencilWeight = 55f;
	[Export] public float ScissorsWeight = 25f;
	[Export] public float RulerWeight = 35f;
	[Export] public float ExtinguisherWeight = 0f;   // ← nueva (puedes cambiarla)
	[Export] public float NothingWeight = 15f;
	[Export] public string OpenAnimationName = "open";

	private Prota_arm playerInRange = null;
	private Label interactionPrompt;
	private AnimationPlayer lockerAnimation;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		interactionPrompt = GetNodeOrNull<Label>("InteractionPrompt");
		lockerAnimation = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

		if (interactionPrompt != null)
			interactionPrompt.Visible = false;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Prota_arm player)
		{
			playerInRange = player;
			if (interactionPrompt != null)
				interactionPrompt.Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body is Prota_arm player && player == playerInRange)
		{
			playerInRange = null;
			if (interactionPrompt != null)
				interactionPrompt.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		if (playerInRange != null && Input.IsActionJustPressed("interact"))
		{
			GiveRandomWeapon();
		}
	}

	private void GiveRandomWeapon()
	{
		float totalWeight = PencilWeight + ScissorsWeight + RulerWeight + ExtinguisherWeight + NothingWeight;
		if (totalWeight <= 0) return;

		var rng = new RandomNumberGenerator();
		rng.Randomize();
		float roll = rng.Randf() * totalWeight;

		float cumulative = 0f;
		WeaponType selected = WeaponType.None;

		cumulative += PencilWeight;
		if (roll < cumulative) selected = WeaponType.Pencil;
		else
		{
			cumulative += ScissorsWeight;
			if (roll < cumulative) selected = WeaponType.Scissors;
			else
			{
				cumulative += RulerWeight;
				if (roll < cumulative) selected = WeaponType.Ruler;
				else
				{
					cumulative += ExtinguisherWeight;
					if (roll < cumulative) selected = WeaponType.Extinguisher;
					// else queda None
				}
			}
		}

		// Animación de apertura de la taquilla
		if (lockerAnimation != null && lockerAnimation.HasAnimation(OpenAnimationName))
		{
			lockerAnimation.Play(OpenAnimationName);
		}

		if (playerInRange != null)
		{
			playerInRange.EquipWeapon(selected);
			GD.Print($"¡Arma obtenida: {selected}!");

			this.Monitoring = false;
			if (interactionPrompt != null)
				interactionPrompt.Visible = false;
		}
	}
}
