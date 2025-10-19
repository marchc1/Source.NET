using Source.Common;
using Source;

using Game.Shared;
using System.Numerics;
using Source.Common.MaterialSystem;

namespace Game.Server;


using FIELD = FIELD<ColorCorrection>;

public class ColorCorrection : BaseEntity
{
	public static readonly SendTable DT_ColorCorrection = new([
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
	public static readonly new ServerClass ServerClass = new ServerClass("ColorCorrection", DT_ColorCorrection).WithManualClassID(StaticClassIndices.CColorCorrection);

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
