using Source.Common;
using Source;

using Game.Shared;

using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TEDynamicLight>;
[NetworkName("CTEDynamicLight")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEDynamicLight).AsEvent<C_TEDynamicLight>();

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("r")]
	public int R;
	[NetworkName("g")]
	public int G;
	[NetworkName("b")]
	public int B;
	[NetworkName("exponent")]
	public int Exponent;
	[NetworkName("m_fRadius")]
	public float Radius;
	[NetworkName("m_fTime")]
	public float Time;
	[NetworkName("m_fDecay")]
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
