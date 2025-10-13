using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_FuncSmokeVolume>;

public class C_FuncSmokeVolume : C_BaseParticleEntity
{
	public static readonly RecvTable DT_FuncSmokeVolume = new(DT_BaseParticleEntity, [
		RecvPropInt(FIELD.OF(nameof(Color1))),
		RecvPropInt(FIELD.OF(nameof(Color2))),
		RecvPropString(FIELD.OF(nameof(MaterialName))),
		RecvPropFloat(FIELD.OF(nameof(ParticleDrawWidth))),
		RecvPropFloat(FIELD.OF(nameof(ParticleSpacingDistance))),
		RecvPropFloat(FIELD.OF(nameof(DensityRampSpeed))),
		RecvPropFloat(FIELD.OF(nameof(RotationSpeed))),
		RecvPropFloat(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(SpawnFlags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("FuncSmokeVolume", DT_FuncSmokeVolume).WithManualClassID(StaticClassIndices.CFuncSmokeVolume);

	public Color Color1;
	public Color Color2;
	public InlineArray255<char> MaterialName;
	public float ParticleDrawWidth;
	public float ParticleSpacingDistance;
	public float DensityRampSpeed;
	public float RotationSpeed;
	public float MovementSpeed;
	public float Density;
}

