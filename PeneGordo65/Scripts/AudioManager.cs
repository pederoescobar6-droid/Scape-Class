using Godot;
using System;

public partial class AudioManager : Node
{
	private AudioStreamPlayer _musicaFondo;

	public override void _Ready()
	{
		// Buscamos el nodo hijo por su nombre exacto
		_musicaFondo = GetNodeOrNull<AudioStreamPlayer>("MusicaFondo");

		if (_musicaFondo != null)
		{
			_musicaFondo.Play();
			GD.Print("AudioManager: Música iniciada correctamente.");
		}
		else
		{
		}
	}
}
