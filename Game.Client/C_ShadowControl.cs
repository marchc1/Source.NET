using Game.Client;
using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<C_ShadowControl>;

public class C_ShadowControl : C_BaseEntity
{
	public static readonly RecvTable DT_ShadowControl = new([
		RecvPropVector(FIELD.OF(nameof(ShadowDirection))),
		RecvPropInt(FIELD.OF(nameof(ShadowColor))),
		RecvPropFloat(FIELD.OF(nameof(ShadowMaxDist))),
		RecvPropBool(FIELD.OF(nameof(DisableShadows))),
		RecvPropBool(FIELD.OF(nameof(EnableLocalLightShadows))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("ShadowControl", DT_ShadowControl).WithManualClassID(StaticClassIndices.CShadowControl);

	public Vector3 ShadowDirection;
	public Color ShadowColor;
	public float ShadowMaxDist;
	public bool DisableShadows;
	public bool EnableLocalLightShadows;
}
