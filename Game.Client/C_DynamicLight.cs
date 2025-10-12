using Game.Shared;

using Source;
using Source.Common;
namespace Game.Client;
using FIELD = FIELD<C_DynamicLight>;

public class C_DynamicLight : C_BaseEntity
{
	public static readonly RecvTable DT_DynamicLight = new(DT_BaseEntity, [
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(LightStyle))),
		RecvPropFloat(FIELD.OF(nameof(Radius))),
		RecvPropInt(FIELD.OF(nameof(Exponent))),
		RecvPropFloat(FIELD.OF(nameof(InnerAngle))),
		RecvPropFloat(FIELD.OF(nameof(OuterAngle))),
		RecvPropFloat(FIELD.OF(nameof(SpotRadius))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("DynamicLight", DT_DynamicLight).WithManualClassID(StaticClassIndices.CDynamicLight);

	public int Flags;
	public int LightStyle;
	public float Radius;
	public int Exponent;
	public float InnerAngle;
	public float OuterAngle;
	public float SpotRadius;
}

