namespace Game.Server;

using Game.Shared;

using Source.Common;

using FIELD = Source.FIELD<PlayerResource>;

[LinkEntityToClass("player_manager")]
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
	public static readonly new ServerClass ServerClass = new ServerClass("PlayerResource", DT_PlayerResource).WithManualClassID(StaticClassIndices.CPlayerResource);

	InlineArrayMaxPlayersPlusOne<int> Ping = new();
	InlineArrayMaxPlayersPlusOne<int> Score = new();
	InlineArrayMaxPlayersPlusOne<int> Deaths = new();
	InlineArrayMaxPlayersPlusOne<bool> Connected = new();
	InlineArrayMaxPlayersPlusOne<int> Team = new();
	InlineArrayMaxPlayersPlusOne<bool> Alive = new();
	new InlineArrayMaxPlayersPlusOne<int> Health = new();
	InlineArrayMaxPlayersPlusOne<int> Armor = new();
}
