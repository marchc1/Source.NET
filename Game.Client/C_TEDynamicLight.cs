using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEDynamicLight>;
public class C_TEDynamicLight : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEDynamicLight = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(R))),
		RecvPropInt(FIELD.OF(nameof(G))),
		RecvPropInt(FIELD.OF(nameof(B))),
		RecvPropInt(FIELD.OF(nameof(Exponent))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
		RecvPropFloat(FIELD.OF(nameof(Time))),
		RecvPropFloat(FIELD.OF(nameof(Decay))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEDynamicLight", DT_TEDynamicLight).WithManualClassID(StaticClassIndices.CTEDynamicLight);

	public Vector3 Origin;
	public int R;
	public int G;
	public int B;
	public int Exponent;
	public float Radius;
	public float Time;
	public float Decay;
}
