using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncSmokeVolume>;

[NetworkName("CFuncSmokeVolume")]
public partial class FuncSmokeVolume : BaseParticleEntity
{
	public static readonly SendTable DT_FuncSmokeVolume = new(DT_BaseParticleEntity, [
		SendPropInt(NetworkVarFields.Color1, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Color2, 32, PropFlags.Unsigned),
		SendPropString(FIELD.OF(nameof(MaterialName))),
		SendPropFloat(NetworkVarFields.ParticleDrawWidth, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ParticleSpacingDistance, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.DensityRampSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.RotationSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MovementSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Density, 0, PropFlags.NoScale),
		SendPropInt(BaseEntity.NetworkVarFields.SpawnFlags, 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncSmokeVolume);

	[NetworkName("m_Color1")]
	[NetworkVar] public partial Color Color1 { get; set; }
	[NetworkName("m_Color2")]
	[NetworkVar] public partial Color Color2 { get; set; }
	[NetworkName("m_MaterialName")]
	public InlineArray255<char> MaterialName;
	[NetworkName("m_ParticleDrawWidth")]
	[NetworkVar] public partial float ParticleDrawWidth { get; set; }
	[NetworkName("m_ParticleSpacingDistance")]
	[NetworkVar] public partial float ParticleSpacingDistance { get; set; }
	[NetworkName("m_DensityRampSpeed")]
	[NetworkVar] public partial float DensityRampSpeed { get; set; }
	[NetworkName("m_RotationSpeed")]
	[NetworkVar] public partial float RotationSpeed { get; set; }
	[NetworkName("m_MovementSpeed")]
	[NetworkVar] public partial float MovementSpeed { get; set; }
	[NetworkName("m_Density")]
	[NetworkVar] public partial float Density { get; set; }
}
