using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;
using System;

namespace Game.Server;


using FIELD = FIELD<FuncSmokeVolume>;

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
		SendPropFloat(FIELD.OF(nameof(Density)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(SpawnFlags)), 8, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("FuncSmokeVolume", DT_FuncSmokeVolume).WithManualClassID(StaticClassIndices.CFuncSmokeVolume);

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
