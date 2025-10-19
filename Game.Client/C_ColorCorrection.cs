using Game.Shared;

using Source;
using Source.Common;
using System.Net;

using System.Security.Cryptography.X509Certificates;
using Source.Common.MaterialSystem;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_ColorCorrection>;

public class C_ColorCorrection : C_BaseEntity
{
	public static readonly RecvTable DT_ColorCorrection = new([
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
	public static readonly new ClientClass ClientClass = new ClientClass("ColorCorrection", DT_ColorCorrection).WithManualClassID(StaticClassIndices.CColorCorrection);


	public float MinFalloff;
	public float MaxFalloff;
	public float CurWeight;
	public float MaxWeight;
	public float FadeInDuration;
	public float FadeOutDuration;
	public InlineArrayMaxPath<char> NetLookupFilename;
	public bool Enabled;
	public bool ClientSide;
	public bool Exclusive;
}

