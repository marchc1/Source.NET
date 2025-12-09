using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SteamJet>;
public class SteamJet : BaseParticleEntity
{
	public static readonly SendTable DT_SteamJet = new(DT_BaseParticleEntity, [
		SendPropFloat(FIELD.OF(nameof(SpreadSpeed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Speed)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(StartSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(EndSize)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Rate)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(JetLength)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(Emit))),
		SendPropBool(FIELD.OF(nameof(FaceLeft))),
		SendPropInt(FIELD.OF(nameof(Type)), 32, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Spawnflags)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(RollSpeed)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("SteamJet", DT_SteamJet).WithManualClassID(StaticClassIndices.CSteamJet);

	public float SpreadSpeed;
	public new float Speed;
	public float StartSize;
	public float EndSize;
	public float Rate;
	public float JetLength;
	public bool Emit;
	public bool FaceLeft;
	public int Type;
	public int Spawnflags;
	public float RollSpeed;
}
