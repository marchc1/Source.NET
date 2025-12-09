using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESpriteSpray>;
public class C_TESpriteSpray : C_BaseTempEntity
{
	public static readonly RecvTable DT_TESpriteSpray = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Noise))),
		RecvPropInt(FIELD.OF(nameof(Speed))),
		RecvPropInt(FIELD.OF(nameof(Count))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TESpriteSpray", DT_TESpriteSpray).WithManualClassID(StaticClassIndices.CTESpriteSpray);

	public Vector3 Origin;
	public Vector3 Direction;
	public int ModelIndex;
	public float Noise;
	public int Speed;
	public int Count;
}
