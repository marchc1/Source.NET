global using static Game.Client.PlayerResourceGlobals;
namespace Game.Client;

using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<C_PlayerResource>;

public static class PlayerResourceGlobals
{
	public static C_PlayerResource? g_pPlayerResource;
}
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
	public static readonly new ClientClass ClientClass = new ClientClass("PlayerResource", null, null, DT_PlayerResource).WithManualClassID(StaticClassIndices.CPlayerResource);

	InlineArrayMaxPlayersPlusOne<int> Ping = new();
	InlineArrayMaxPlayersPlusOne<int> Score = new();
	InlineArrayMaxPlayersPlusOne<int> Deaths = new();
	InlineArrayMaxPlayersPlusOne<bool> Connected = new();
	InlineArrayMaxPlayersPlusOne<int> Team = new();
	InlineArrayMaxPlayersPlusOne<bool> Alive = new();
	new InlineArrayMaxPlayersPlusOne<int> Health = new();
	InlineArrayMaxPlayersPlusOne<int> Armor = new();
}
