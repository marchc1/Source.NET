using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEHL2MPFireBullets>;
public class C_TEHL2MPFireBullets : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEHL2MPFireBullets = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Dir))),
		RecvPropInt(FIELD.OF(nameof(AmmoID))),
		RecvPropInt(FIELD.OF(nameof(Seed))),
		RecvPropInt(FIELD.OF(nameof(Shots))),
		RecvPropInt(FIELD.OF(nameof(Player))),
		RecvPropFloat(FIELD.OF(nameof(Spread))),
		RecvPropInt(FIELD.OF(nameof(DoImpacts))),
		RecvPropInt(FIELD.OF(nameof(DoTracers))),
		RecvPropString(FIELD.OF(nameof(TracerType))),
		RecvPropFloat(FIELD.OF(nameof(SpreadY))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEHL2MPFireBullets", DT_TEHL2MPFireBullets).WithManualClassID(StaticClassIndices.CTEHL2MPFireBullets);

	public Vector3 Origin;
	public Vector3 Dir;
	public int AmmoID;
	public int Seed;
	public int Shots;
	public int Player;
	public float Spread;
	public int DoImpacts;
	public int DoTracers;
	public InlineArray512<char> TracerType;
	public float SpreadY;
}
