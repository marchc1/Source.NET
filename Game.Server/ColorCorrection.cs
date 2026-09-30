using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<ColorCorrection>;

[NetworkName("CColorCorrection")]
public partial class ColorCorrection : BaseEntity
{
	public static readonly SendTable DT_ColorCorrection = new([
		SendPropVector(BaseEntity.NetworkVarFields.Origin, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MinFalloff, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MaxFalloff, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.CurWeight, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.MaxWeight, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeInDuration, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.FadeOutDuration, 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(NetLookupFilename))),
		SendPropBool(NetworkVarFields.Enabled),
		SendPropBool(NetworkVarFields.ClientSide),
		SendPropBool(NetworkVarFields.Exclusive),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ColorCorrection);

	[NetworkName("m_MinFalloff")]
	[NetworkVar] public partial float MinFalloff { get; set; }
	[NetworkName("m_MaxFalloff")]
	[NetworkVar] public partial float MaxFalloff { get; set; }
	[NetworkName("m_flCurWeight")]
	[NetworkVar] public partial float CurWeight { get; set; }
	[NetworkName("m_flMaxWeight")]
	[NetworkVar] public partial float MaxWeight { get; set; }
	[NetworkName("m_flFadeInDuration")]
	[NetworkVar] public partial float FadeInDuration { get; set; }
	[NetworkName("m_flFadeOutDuration")]
	[NetworkVar] public partial float FadeOutDuration { get; set; }
	[NetworkName("m_netlookupFilename")]
	public InlineArrayMaxPath<char> NetLookupFilename;
	[NetworkName("m_bEnabled")]
	[NetworkVar] public partial bool Enabled { get; set; }
	[NetworkName("m_bClientSide")]
	[NetworkVar] public partial bool ClientSide { get; set; }
	[NetworkName("m_bExclusive")]
	[NetworkVar] public partial bool Exclusive { get; set; }
}
