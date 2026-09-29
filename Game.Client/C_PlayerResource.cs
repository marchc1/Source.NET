global using static Game.Client.PlayerResourceGlobals;
namespace Game.Client;

using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<C_PlayerResource>;

public static class PlayerResourceGlobals
{
	public static C_PlayerResource? g_pPlayerResource;
}
[NetworkName("CPlayerResource")]
public class C_PlayerResource : C_BaseEntity
{
	public static readonly RecvTable DT_PlayerResource = new([
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Ping)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Ping), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Score)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Score), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Deaths)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Deaths), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Connected)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Connected), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Team)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Team), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Alive)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Alive), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Health)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Health), 0))),
		RecvPropArray3(FIELD.OF_ARRAY(nameof(Armor)), RecvPropInt(FIELD.OF_ARRAYINDEX(nameof(Armor)))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_PlayerResource);

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
