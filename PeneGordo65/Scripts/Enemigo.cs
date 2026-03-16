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

		// IMPORTANTE: Asegúrate de que el Timer NO sea "One Shot" en el Inspector
		_timerDaño.Timeout += AplicarDañoSecuencial;

		Area2D zonaDaño = GetNodeOrNull<Area2D>("ZonaDaño");
		if (zonaDaño != null)
		{
			zonaDaño.BodyEntered += (body) => {
				if (body is Protagonista) {
					_jugadorEnZonaDaño = true;
					
					// EXPLICACIÓN:
					// Si el timer está parado, significa que es la primera vez que entramos
					// o que ya pasó el cooldown. Lo activamos.
					if (_timerDaño.IsStopped())
					{
						AplicarDañoSecuencial(); // Primer golpe
						_timerDaño.Start();      // Inicia el cooldown para el siguiente
					}
				}
			};

			zonaDaño.BodyExited += (body) => {
				if (body is Protagonista) {
					_jugadorEnZonaDaño = false;
					// NO paramos el Timer aquí. Dejamos que siga corriendo 
					// hasta que termine su tiempo. Así, si entras y sales rápido,
					// el Timer sigue contando el cooldown.
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
		// Si el jugador está dentro cuando el Timer termina, hace daño
		if (_jugadorEnZonaDaño && _jugador is Protagonista protagonista)
		{
			protagonista.RecibirDano(10);
		}
		else
		{
			// Si el jugador ya NO está dentro cuando el Timer termina, 
			// paramos el Timer para que no gaste recursos.
			_timerDaño.Stop();
		}
	}
}
