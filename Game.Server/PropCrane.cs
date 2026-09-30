using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<PropCrane>;
[NetworkName("CPropCrane")]
public partial class PropCrane : BaseAnimating
{
	public static readonly SendTable DT_PropCrane = new(DT_BaseAnimating, [
		SendPropEHandle(PropCrane.NetworkVarFields.Player),
		SendPropBool(NetworkVarFields.MagnetOn),
		SendPropBool(NetworkVarFields.EnterAnimOn),
		SendPropBool(NetworkVarFields.ExitAnimOn),
		SendPropVector(NetworkVarFields.EyeExitEndpoint, 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropCrane);

	[NetworkName("m_hPlayer")]
	[NetworkVar] public partial NetworkHandle<BaseEntity> Player { get; }
	[NetworkName("m_bMagnetOn")]
	[NetworkVar] public partial bool MagnetOn { get; set; }
	[NetworkName("m_bEnterAnimOn")]
	[NetworkVar] public partial bool EnterAnimOn { get; set; }
	[NetworkName("m_bExitAnimOn")]
	[NetworkVar] public partial bool ExitAnimOn { get; set; }
	[NetworkName("m_vecEyeExitEndpoint")]
	[NetworkVar] public partial Vector3 EyeExitEndpoint { get; set; }
}
