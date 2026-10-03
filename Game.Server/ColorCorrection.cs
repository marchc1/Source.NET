using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<ColorCorrection>;

[LinkEntityToClass("color_correction")]
[NetworkName("CColorCorrection")]
public class ColorCorrection : BaseEntity
{
	public static readonly SendTable DT_ColorCorrection = new([
		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MinFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxFalloff)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(CurWeight)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(MaxWeight)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeInDuration)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(FadeOutDuration)), 0, PropFlags.NoScale),
		SendPropString(FIELD.OF(nameof(NetLookupFilename))),
		SendPropBool(FIELD.OF(nameof(Enabled))),
		SendPropBool(FIELD.OF(nameof(ClientSide))),
		SendPropBool(FIELD.OF(nameof(Exclusive))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_ColorCorrection);

	[NetworkName("m_MinFalloff")]
	public float MinFalloff;
	[NetworkName("m_MaxFalloff")]
	public float MaxFalloff;
	[NetworkName("m_flCurWeight")]
	public float CurWeight;
	[NetworkName("m_flMaxWeight")]
	public float MaxWeight;
	[NetworkName("m_flFadeInDuration")]
	public float FadeInDuration;
	[NetworkName("m_flFadeOutDuration")]
	public float FadeOutDuration;
	[NetworkName("m_netlookupFilename")]
	public InlineArrayMaxPath<char> NetLookupFilename;
	[NetworkName("m_bEnabled")]
	public bool Enabled;
	[NetworkName("m_bClientSide")]
	public bool ClientSide;
	[NetworkName("m_bExclusive")]
	public bool Exclusive;
}
