using Source.Common;

using System.Numerics;

namespace Game.Shared.HL2;

public struct LadderMove()
{
	public bool ForceLadderMove;
	public bool ForceMount;
	public TimeUnit_t StartTime;
	public TimeUnit_t ArrivalTime;
	public Vector3 GoalPosition;
	public Vector3 StartPosition;
#if CLIENT_DLL || GAME_DLL
	public Handle<FuncLadder> ForceLadder = new();
#else
	public EHANDLE ForceLadder = new();
#endif
	public EHANDLE ReservedSpot = new();
}
