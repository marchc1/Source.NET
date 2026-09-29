using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client;
using FIELD = FIELD<C_FuncSmokeVolume>;

[NetworkName("CFuncSmokeVolume")]
public class C_FuncSmokeVolume : C_BaseParticleEntity
{
	public static readonly RecvTable DT_FuncSmokeVolume = new(DT_BaseParticleEntity, [
		RecvPropInt(FIELD.OF(nameof(Color1)), 0, RecvProxy_IntToColor32),
		RecvPropInt(FIELD.OF(nameof(Color2)), 0, RecvProxy_IntToColor32),
		RecvPropString(FIELD.OF(nameof(MaterialName))),
		RecvPropFloat(FIELD.OF(nameof(ParticleDrawWidth))),
		RecvPropFloat(FIELD.OF(nameof(ParticleSpacingDistance))),
		RecvPropFloat(FIELD.OF(nameof(DensityRampSpeed))),
		RecvPropFloat(FIELD.OF(nameof(RotationSpeed))),
		RecvPropFloat(FIELD.OF(nameof(MovementSpeed))),
		RecvPropFloat(FIELD.OF(nameof(Density))),
		RecvPropInt(FIELD.OF(nameof(SpawnFlags))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FuncSmokeVolume);

	[NetworkName("m_Color1")]
	public Color Color1;
	[NetworkName("m_Color2")]
	public Color Color2;
	[NetworkName("m_MaterialName")]
	public InlineArray255<char> MaterialName;
	[NetworkName("m_ParticleDrawWidth")]
	public float ParticleDrawWidth;
	[NetworkName("m_ParticleSpacingDistance")]
	public float ParticleSpacingDistance;
	[NetworkName("m_DensityRampSpeed")]
	public float DensityRampSpeed;
	[NetworkName("m_RotationSpeed")]
	public float RotationSpeed;
	[NetworkName("m_MovementSpeed")]
	public float MovementSpeed;
	[NetworkName("m_Density")]
	public float Density;
}

