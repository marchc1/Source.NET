using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEHL2MPFireBullets>;
public class TEHL2MPFireBullets : BaseTempEntity
{
	public static readonly SendTable DT_TEHL2MPFireBullets = new([
		SendPropVector(FIELD.OF(nameof(Origin)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(AmmoID)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Seed)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Shots)), 5, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Player)), 6, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(Spread)), 10, 0),
		SendPropInt(FIELD.OF(nameof(DoImpacts)), 1, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(DoTracers)), 1, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(TracerType)), 10, 0),
		SendPropFloat(FIELD.OF(nameof(SpreadY)), 10, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("TEHL2MPFireBullets", DT_TEHL2MPFireBullets).WithManualClassID(StaticClassIndices.CTEHL2MPFireBullets);

	public Vector3 Origin;
	public Vector3 Dir;
	public int AmmoID;
	public int Seed;
	public int Shots;
	public int Player;
	public float Spread;
	public int DoImpacts;
	public int DoTracers;
	public InlineArray256<char> TracerType;
	public float SpreadY;
}
