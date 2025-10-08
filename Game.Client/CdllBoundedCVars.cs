using Source.Common.Commands;
using Source.Common.Networking;

namespace Game.Client;

public class BoundedCvar_Rate()
	: ConVar_ServerBounded("rate", NetChannel.DEFAULT_RATE.ToString(), FCvar.Archive | FCvar.UserInfo, "Max bytes/sec the host can receive data")
{
	public override float GetFloat() {
		return 0;
	}
}

public class BoundedCvar_CmdRate()
	: ConVar_ServerBounded("cl_cmdrate", "30", FCvar.Archive | FCvar.UserInfo, "Max number of command packets sent to server per second", Protocol.MIN_CMD_RATE, Protocol.MAX_CMD_RATE)
{
	public override float GetFloat() {
		return 0;
	}
}

public class BoundedCvar_UpdateRate()
	: ConVar_ServerBounded("cl_updaterate", NetChannel.DEFAULT_RATE.ToString(), FCvar.Archive | FCvar.UserInfo | FCvar.NotConnected, "Number of packets per second of updates you are requesting from the server")
{
	public override float GetFloat() {
		return 0;
	}
}

internal static class CdllBoundedCVars
{
	public static readonly BoundedCvar_Rate rate = new();
	public static readonly BoundedCvar_CmdRate cl_cmdrate = new();
	public static readonly BoundedCvar_UpdateRate cl_updaterate = new();

	public static double GetClientInterpAmount() {
		return 0.1; // MASSIVE TODO: REQUIRES ConVar_ServerBounded and a lot of other annoying things!!!!
		// For now, just hardcoding it at 0.1
	}
}
