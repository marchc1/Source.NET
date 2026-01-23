using Source.Common.Mathematics;

namespace Source.Common;

public class PlayerState
{
	public bool DeadFlag;
	public QAngle ViewingAngle;

	// We can't use CLIENT_DLL here in Common... w/e

	public string? NetName;
	public int FixAngle;
	public QAngle AngleChange;
	public bool HLTV;
	public bool Replay;
	public int Frags;
	public int Deaths;
}
