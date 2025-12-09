using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEPlayerDecal>;
public class C_TEPlayerDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEPlayerDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Player))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEPlayerDecal", DT_TEPlayerDecal).WithManualClassID(StaticClassIndices.CTEPlayerDecal);

	public Vector3 Origin;
	public int Entity;
	public int Player;
}
