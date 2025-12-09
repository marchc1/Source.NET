using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESprite>;
public class C_TESprite : C_BaseTempEntity
{
	public static readonly RecvTable DT_TESprite = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(Brightness))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TESprite", DT_TESprite).WithManualClassID(StaticClassIndices.CTESprite);

	public Vector3 Origin;
	public int ModelIndex;
	public float Scale;
	public int Brightness;
}
