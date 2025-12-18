using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SmokeStack>;

public struct SmokeStackLightInfo{
	public Vector3 Pos;
	public Vector3 Color;
	public float Intensity;
}

public class SmokeStack : BaseParticleEntity
{
	public static readonly SendTable DT_SmokeStack = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpreadSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Speed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Rate)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(JetLength)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropFloat(FIELD.OF(nameof(BaseSpread)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(RollSpeed)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("DirLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("DirLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Pos"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF("AmbientLight.Color"), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF("AmbientLight.Intensity"), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(Wind)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Twist)), 0, PropFlags.NoScale),
		SendPropIntWithMinusOneFlag(FIELD.OF(nameof(MaterialModel)), 16),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SmokeStack", DT_SmokeStack).WithManualClassID(StaticClassIndices.CSmokeStack);

	public float SpreadSpeed;
	public new float Speed;
	public float StartSize;
	public float EndSize;
	public float Rate;
	public float JetLength;
	public bool Emit;
	public float BaseSpread;
	public float RollSpeed;

	public SmokeStackLightInfo AmbientLight;
	public SmokeStackLightInfo DirLight;

	public Vector3 Wind;
	public float Twist;
	public int MaterialModel;
}
