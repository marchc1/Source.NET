using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncSmokeVolume>;

[LinkEntityToClass("func_smokevolume")]
[NetworkName("CFuncSmokeVolume")]
public class FuncSmokeVolume : BaseParticleEntity
{
	public static readonly SendTable DT_FuncSmokeVolume = new(DT_BaseParticleEntity, [
		SendPropInt(FIELD.OF(nameof(Color1)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Color2)), 32, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(MaterialName))),
		SendPropFloat(FIELD.OF(nameof(ParticleDrawWidth)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ParticleSpacingDistance)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(DensityRampSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(RotationSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MovementSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Density)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(SpawnFlags)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncSmokeVolume);

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
