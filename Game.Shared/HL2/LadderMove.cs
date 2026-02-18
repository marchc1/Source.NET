using System.Numerics;

namespace Game.Shared.HL2;

public struct LadderMove()
{
	public bool ForceLadderMove;
	public bool ForceMount;
	public float StartTime;
	public float ArrivalTime;
	public Vector3 GoalPosition;
	public Vector3 StartPosition;
	public EHANDLE ForceLadder = new();
	public EHANDLE ReservedSpot = new();
}
