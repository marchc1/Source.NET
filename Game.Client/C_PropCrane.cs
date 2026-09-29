using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_PropCrane>;
[NetworkName("CPropCrane")]
public class C_PropCrane : C_BaseAnimating
{
	public static readonly RecvTable DT_PropCrane = new(DT_BaseAnimating, [
		RecvPropEHandle(FIELD.OF(nameof(Player))),
		RecvPropBool(FIELD.OF(nameof(MagnetOn))),
		RecvPropBool(FIELD.OF(nameof(EnterAnimOn))),
		RecvPropBool(FIELD.OF(nameof(ExitAnimOn))),
		RecvPropVector(FIELD.OF(nameof(EyeExitEndpoint))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropCrane);

	[NetworkName("m_hPlayer")]
	public EHANDLE Player = new();
	[NetworkName("m_bMagnetOn")]
	public bool MagnetOn;
	[NetworkName("m_bEnterAnimOn")]
	public bool EnterAnimOn;
	[NetworkName("m_bExitAnimOn")]
	public bool ExitAnimOn;
	[NetworkName("m_vecEyeExitEndpoint")]
	public Vector3 EyeExitEndpoint;
}
