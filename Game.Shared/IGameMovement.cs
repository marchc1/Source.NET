#if CLIENT_DLL || GAME_DLL
using System.Numerics;

#if CLIENT_DLL
using Game.Client;
#elif GAME_DLL
using Game.Server;
#endif

using Source.Common.Mathematics;


namespace Game.Shared;

public class MoveData
{
	public bool FirstRunOfFunctions;
	public bool GameCodeMovedPlayer;

	public readonly EntityHandle_t PlayerHandle = new(); // edict index on server, client entity handle on client

	public int ImpulseCommand;  // Impulse command issued.
	public QAngle ViewAngles; // Command view angles (local space)
	public QAngle AbsViewAngles;  // Command view angles (world space)
	public int Buttons;         // Attack buttons.
	public int OldButtons;      // From host_client->oldbuttons;
	public float ForwardMove;
	public float OldForwardMove;
	public float SideMove;
	public float UpMove;

	public float MaxSpeed;
	public float ClientMaxSpeed;

	// Variables from the player edict (sv_player) or entvars on the client.
	// These are copied in here before calling and copied out after calling.
	public Vector3 Velocity;
	public QAngle Angles;
	public QAngle OldAngles;

	// Output only
	public float StepHeight;  // how much you climbed this move
	public Vector3 WishVel;   // This is where you tried 
	public Vector3 JumpVel;   // This is your jump velocity

	// Movement constraints	(radius 0 means no constraint)
	public Vector3 ConstraintCenter;
	public float ConstraintRadius;
	public float ConstraintWidth;
	public float ConstraintSpeedFactor;

	public void SetAbsOrigin(in Vector3 vec) => AbsOrigin = vec;
	public ref readonly Vector3 GetAbsOrigin() => ref AbsOrigin;

	Vector3 AbsOrigin;
}

public interface IGameMovement
{
	void ProcessMovement(BasePlayer player, MoveData pMove);
	void StartTrackPredictionErrors(BasePlayer player);
	void FinishTrackPredictionErrors(BasePlayer player);

	// Allows other parts of the engine to find out the normal and ducked player bbox sizes
	Vector3 GetPlayerMins(bool ducked);
	Vector3 GetPlayerMaxs(bool ducked);
	Vector3 GetPlayerViewOffset(bool ducked);
}
#endif

