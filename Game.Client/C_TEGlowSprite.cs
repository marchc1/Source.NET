using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEGlowSprite>;
public class C_TEGlowSprite : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEGlowSprite = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropFloat(FIELD.OF(nameof(Life))),
		RecvPropInt(FIELD.OF(nameof(Brightness))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEGlowSprite", DT_TEGlowSprite).WithManualClassID(StaticClassIndices.CTEGlowSprite);

	public Vector3 Origin;
	public int ModelIndex;
	public float Scale;
	public float Life;
	public int Brightness;
}
