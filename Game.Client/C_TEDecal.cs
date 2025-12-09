using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEDecal>;
public class C_TEDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Start))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Hitbox))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEDecal", DT_TEDecal).WithManualClassID(StaticClassIndices.CTEDecal);

	public Vector3 Origin;
	public Vector3 Start;
	public int Entity;
	public int Hitbox;
	public int Index;
}
