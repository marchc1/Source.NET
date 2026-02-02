global using static Game.Server.TeamGlobals;
using Source.Common;
using Source;
using Game.Shared;

namespace Game.Server;
using FIELD = Source.FIELD<Team>;

public static class TeamGlobals {
	public static readonly List<Team> g_Teams = [];
	public static int GetNumberOfTeams() => g_Teams.Count;
	public static Team? GetGlobalTeam(int index){
		if (index < 0 || index >= GetNumberOfTeams())
			return null;

		return g_Teams[index];
	}
}

public class Team : BaseEntity
{
	public static readonly SendTable DT_Team = new([
		SendPropInt(FIELD.OF(nameof(TeamNum)), 5),
		SendPropInt(FIELD.OF(nameof(Score)), 0),
		SendPropInt(FIELD.OF(nameof(RoundsWon)), 8),
		SendPropString(FIELD.OF(nameof(Teamname))),

		SendPropInt("player_array_element", 10, PropFlags.Unsigned, SendProxy_PlayerList, 4),
		SendPropArray2(SendProxyArrayLength_PlayerArray, Constants.MAX_PLAYERS, "player_array")
	]);

	private static int SendProxyArrayLength_PlayerArray(object instance, int objectID) => ((Team)instance).Players.Count;
	private static void SendProxy_PlayerList(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
	}
	public static readonly new ServerClass ServerClass = new ServerClass("Team", DT_Team).WithManualClassID(StaticClassIndices.CTeam);

	public readonly List<BasePlayer> Players = [];
	public InlineArray32<char> Teamname;
	public int Score;
	public int RoundsWon;
	public int Deaths;
	public int LastSpawn;
	public new int TeamNum;
}
