using Godot;
using System;

public partial class Enemigo : CharacterBody2D
{
	[Export] public float Velocidad = 60.0f;
	[Export] public int PuntosDeDaño = 10;
	[Export] public float MargenParada = 5.0f;
	
	private Node2D _jugador;
	private bool _jugadorEnRangoVisio = false;
	private Timer _timerDaño;
	private bool _jugadorEnZonaDaño = false;

	public override void _Ready()
	{
		_jugador = GetTree().CurrentScene.FindChild("Protagonista") as Node2D;
		_timerDaño = GetNode<Timer>("Timer");

		_timerDaño.Timeout += AplicarDañoSecuencial;

		Area2D zonaDaño = GetNodeOrNull<Area2D>("ZonaDaño");
		if (zonaDaño != null)
		{
			zonaDaño.BodyEntered += (body) => {
				if (body is Protagonista) {
					_jugadorEnZonaDaño = true;
					
					if (_timerDaño.IsStopped())
					{
						AplicarDañoSecuencial();
						_timerDaño.Start();
					}
				}
			};

			zonaDaño.BodyExited += (body) => {
				if (body is Protagonista) {
					_jugadorEnZonaDaño = false;
				}
			};
		}

		Area2D areaVision = GetNodeOrNull<Area2D>("AreaVision");
		if (areaVision != null)
		{
			areaVision.BodyEntered += (body) => { if (body == _jugador) _jugadorEnRangoVisio = true; };
			areaVision.BodyExited += (body) => { if (body == _jugador) _jugadorEnRangoVisio = false; };
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_jugadorEnRangoVisio && _jugador != null)
		{
			float distancia = GlobalPosition.DistanceTo(_jugador.GlobalPosition);
			if (distancia > MargenParada)
			{
				Vector2 direccion = (_jugador.GlobalPosition - GlobalPosition).Normalized();
				Velocity = direccion * Velocidad;
			}
			else
			{
				Velocity = Vector2.Zero;
			}
			MoveAndSlide();
		}
		else
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
		}
	}

	private void AplicarDañoSecuencial()
	{
		if (_jugadorEnZonaDaño && _jugador is Protagonista protagonista)
		{
			protagonista.RecibirDaño(PuntosDeDaño);
		}
		else
		{
			_timerDaño.Stop();
		}
	}
}
