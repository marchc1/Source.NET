using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SteamJet>;
[NetworkName("CSteamJet")]
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
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SteamJet);

	[NetworkName("m_SpreadSpeed")]
	public float SpreadSpeed;
	[NetworkName("m_Speed")]
	public new float Speed;
	[NetworkName("m_StartSize")]
	public float StartSize;
	[NetworkName("m_EndSize")]
	public float EndSize;
	[NetworkName("m_Rate")]
	public float Rate;
	[NetworkName("m_JetLength")]
	public float JetLength;
	[NetworkName("m_bEmit")]
	public bool Emit;
	[NetworkName("m_bFaceLeft")]
	public bool FaceLeft;
	[NetworkName("m_nType")]
	public int Type;
	[NetworkName("m_spawnflags")]
	public int Spawnflags;
	[NetworkName("m_flRollSpeed")]
	public float RollSpeed;
}
