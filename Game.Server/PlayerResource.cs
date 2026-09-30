global using static Game.Server.PlayerResourceGlobals;

namespace Game.Server;

using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<PlayerResource>;

public static class PlayerResourceGlobals{
	public static PlayerResource? g_pPlayerResource;
}

[LinkEntityToClass("player_manager")]
[NetworkName("CPlayerResource")]
public partial class PlayerResource : BaseEntity
{

	public static readonly SendTable DT_PlayerResource = new([
		SendPropArray3(PlayerResource.NetworkVarFields.Ping, SendPropInt(PlayerResource.NetworkVarFields.Ping.AtIndex(0)!, 12, PropFlags.Unsigned ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Score, SendPropInt(PlayerResource.NetworkVarFields.Score.AtIndex(0)!, 32 ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Deaths, SendPropInt(PlayerResource.NetworkVarFields.Deaths.AtIndex(0)!, 32 ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Connected, SendPropInt(PlayerResource.NetworkVarFields.Connected.AtIndex(0)!, 1, PropFlags.Unsigned ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Team, SendPropInt(PlayerResource.NetworkVarFields.Team.AtIndex(0)!, 16 ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Alive, SendPropInt(PlayerResource.NetworkVarFields.Alive.AtIndex(0)!, 1, PropFlags.Unsigned ) ),
		SendPropArray3(PlayerResource.NetworkVarFields.Health, SendPropInt(PlayerResource.NetworkVarFields.Health.AtIndex(0)!, 32, PropFlags.VarInt | PropFlags.Unsigned | PropFlags.Normal ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Armor)), SendPropInt(PlayerResource.NetworkVarFields.Health.AtIndex(0)!, 32, PropFlags.Unsigned) ),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PlayerResource);

	[NetworkName("m_iPing")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<int>, int> Ping { get; }
	[NetworkName("m_iScore")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<int>, int> Score { get; }
	[NetworkName("m_iDeaths")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<int>, int> Deaths { get; }
	[NetworkName("m_bConnected")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<bool>, bool> Connected { get; }
	[NetworkName("m_iTeam")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<int>, int> Team { get; }
	[NetworkName("m_bAlive")]
	[NetworkVar] private partial NetworkArray<InlineArrayMaxPlayersPlusOne<bool>, bool> Alive { get; }
	[NetworkName("m_iHealth")]
	[NetworkVar] private new partial NetworkArray<InlineArrayMaxPlayersPlusOne<int>, int> Health { get; }
	[NetworkName("m_iArmor")]
	InlineArrayMaxPlayersPlusOne<int> Armor = new();
}
