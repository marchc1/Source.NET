using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SteamJet>;
[LinkEntityToClass("env_steam")]
[LinkEntityToClass("env_steamjet")]
[NetworkName("CSteamJet")]
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
		SendPropInt(FIELD.OF(nameof(Type)), 2, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Spawnflags)), 8, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(RollSpeed)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SteamJet);

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
