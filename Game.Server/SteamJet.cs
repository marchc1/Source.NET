using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<SteamJet>;
[NetworkName("CSteamJet")]
public partial class SteamJet : BaseParticleEntity
{
	public static readonly SendTable DT_SteamJet = new(DT_BaseParticleEntity, [
		SendPropFloat(NetworkVarFields.SpreadSpeed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Speed, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.StartSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.EndSize, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Rate, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.JetLength, 0, PropFlags.NoScale),
		SendPropBool(NetworkVarFields.Emit),
		SendPropBool(NetworkVarFields.FaceLeft),
		SendPropInt(NetworkVarFields.Type, 2, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Spawnflags, 8, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.RollSpeed, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_SteamJet);

	[NetworkName("m_SpreadSpeed")]
	[NetworkVar] public partial float SpreadSpeed { get; set; }
	[NetworkName("m_Speed")]
	[NetworkVar] public new partial float Speed { get; set; }
	[NetworkName("m_StartSize")]
	[NetworkVar] public partial float StartSize { get; set; }
	[NetworkName("m_EndSize")]
	[NetworkVar] public partial float EndSize { get; set; }
	[NetworkName("m_Rate")]
	[NetworkVar] public partial float Rate { get; set; }
	[NetworkName("m_JetLength")]
	[NetworkVar] public partial float JetLength { get; set; }
	[NetworkName("m_bEmit")]
	[NetworkVar] public partial bool Emit { get; set; }
	[NetworkName("m_bFaceLeft")]
	[NetworkVar] public partial bool FaceLeft { get; set; }
	[NetworkName("m_nType")]
	[NetworkVar] public partial int Type { get; set; }
	[NetworkName("m_spawnflags")]
	[NetworkVar] public partial int Spawnflags { get; set; }
	[NetworkName("m_flRollSpeed")]
	[NetworkVar] public partial float RollSpeed { get; set; }
}
