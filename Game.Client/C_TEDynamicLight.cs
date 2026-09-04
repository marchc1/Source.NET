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
	public static readonly new ClientClass ClientClass = new ClientClass("TEDynamicLight", DT_TEDynamicLight).AsEvent<C_TEDynamicLight>().WithManualClassID(StaticClassIndices.CTEDynamicLight);

	public Vector3 Origin;
	public int R;
	public int G;
	public int B;
	public int Exponent;
	public float Radius;
	public float Time;
	public float Decay;

	public override void PostDataUpdate(DataUpdateType updateType) {
		BroadcastRecipientFilter filter = new();
		TE_DynamicLight(filter, 0.0f, in Origin, R, G, B, Exponent, Radius, Time, Decay, (int)LightIndex.TEDynamic);
	}
}

public static partial class TempEnts
{
	public static void TE_DynamicLight(IRecipientFilter filter, float delay, in Vector3 org, int r, int g, int b, int exponent, float radius, float time, float decay, int lightIndex = (int)LightIndex.TEDynamic) {
		DLight? dl = effects.AllocDlight(lightIndex);
		if (dl == null)
			return;

		dl.Origin = org;
		dl.Radius = radius;
		dl.Color.R = (byte)r;
		dl.Color.G = (byte)g;
		dl.Color.B = (byte)b;
		dl.Color.Exponent = (sbyte)exponent;
		dl.Die = (float)gpGlobals.CurTime + time;
		dl.Decay = decay;
	}
}
