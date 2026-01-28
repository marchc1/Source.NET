using Game.Shared;

using Source;
using Source.Common;

using FIELD = Source.FIELD<Game.Client.C_Team>;

namespace Game.Client;

public class C_Team : C_BaseEntity
{
	public static readonly RecvTable DT_Team = new([
		RecvPropInt( FIELD.OF(nameof(TeamNum))),
		RecvPropInt( FIELD.OF(nameof(Score))),
		RecvPropInt( FIELD.OF(nameof(RoundsWon)) ),
		RecvPropString( FIELD.OF(nameof(Teamname))),

		RecvPropInt( "player_array_element", 0, RecvProxy_PlayerList ),
		RecvPropArray2(RecvProxyArrayLength_PlayerArray, Constants.MAX_PLAYERS, "player_array")
	]);

	private static void RecvProxyArrayLength_PlayerArray(object instance, int objectID, int currentArrayLength) {
		C_Team team = (C_Team)instance;

		if (team.Players.Count != currentArrayLength)
			team.Players.SetSize(currentArrayLength);
	}

	private static void RecvProxy_PlayerList(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		C_Team team = (C_Team)instance;
		team.Players[data.Element] = data.Value.Int;
	}

	public static readonly new ClientClass ClientClass = new ClientClass("Team", null, null, DT_Team).WithManualClassID(StaticClassIndices.CTeam);

	public readonly List<int> Players = [];
	public InlineArray32<char> Teamname;
	public int Score;
	public int RoundsWon;

	public int Deaths;
	public int Ping;
	public int Packetloss;
	public new int TeamNum;
}
