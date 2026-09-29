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
public class PlayerResource : BaseEntity
{

	public static readonly SendTable DT_PlayerResource = new([
		SendPropArray3(FIELD.OF_ARRAY(nameof(Ping)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Ping), 0), 12, PropFlags.Unsigned ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Score)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Score), 0), 32 ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Deaths)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Deaths), 0), 32 ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Connected)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Connected), 0), 1, PropFlags.Unsigned ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Team)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Team), 0), 16 ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Alive)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Alive), 0), 1, PropFlags.Unsigned ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Health)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Health), 0), 32, PropFlags.VarInt | PropFlags.Unsigned | PropFlags.Normal ) ),
		SendPropArray3(FIELD.OF_ARRAY(nameof(Armor)), SendPropInt(FIELD.OF_ARRAYINDEX(nameof(Health), 0), 32, PropFlags.Unsigned) ),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PlayerResource);

	[NetworkName("m_iPing")]
	InlineArrayMaxPlayersPlusOne<int> Ping = new();
	[NetworkName("m_iScore")]
	InlineArrayMaxPlayersPlusOne<int> Score = new();
	[NetworkName("m_iDeaths")]
	InlineArrayMaxPlayersPlusOne<int> Deaths = new();
	[NetworkName("m_bConnected")]
	InlineArrayMaxPlayersPlusOne<bool> Connected = new();
	[NetworkName("m_iTeam")]
	InlineArrayMaxPlayersPlusOne<int> Team = new();
	[NetworkName("m_bAlive")]
	InlineArrayMaxPlayersPlusOne<bool> Alive = new();
	[NetworkName("m_iHealth")]
	new InlineArrayMaxPlayersPlusOne<int> Health = new();
	[NetworkName("m_iArmor")]
	InlineArrayMaxPlayersPlusOne<int> Armor = new();
}
