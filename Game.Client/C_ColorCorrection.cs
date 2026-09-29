using Game.Shared;

using Source;
using Source.Common;
using System.Net;

using System.Security.Cryptography.X509Certificates;
using Source.Common.MaterialSystem;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ColorCorrection>;

[NetworkName("CColorCorrection")]
public class C_ColorCorrection : C_BaseEntity
{
	public static readonly RecvTable DT_ColorCorrection = new([
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropFloat(FIELD.OF(nameof(MinFalloff))),
		RecvPropFloat(FIELD.OF(nameof(MaxFalloff))),
		RecvPropFloat(FIELD.OF(nameof(CurWeight))),
		RecvPropFloat(FIELD.OF(nameof(MaxWeight))),
		RecvPropFloat(FIELD.OF(nameof(FadeInDuration))),
		RecvPropFloat(FIELD.OF(nameof(FadeOutDuration))),
		RecvPropString(FIELD.OF(nameof(NetLookupFilename))),
		RecvPropBool(FIELD.OF(nameof(Enabled))),
		RecvPropBool(FIELD.OF(nameof(ClientSide))),
		RecvPropBool(FIELD.OF(nameof(Exclusive))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_ColorCorrection);


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

