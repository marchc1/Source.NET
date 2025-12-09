using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SteamJet>;
public class C_SteamJet : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SteamJet = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpreadSpeed))),
		RecvPropFloat(FIELD.OF(nameof(Speed))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(Rate))),
		RecvPropFloat(FIELD.OF(nameof(JetLength))),
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropBool(FIELD.OF(nameof(FaceLeft))),
		RecvPropInt(FIELD.OF(nameof(Type))),
		RecvPropInt(FIELD.OF(nameof(Spawnflags))),
		RecvPropFloat(FIELD.OF(nameof(RollSpeed))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("SteamJet", DT_SteamJet).WithManualClassID(StaticClassIndices.CSteamJet);

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
