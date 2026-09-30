using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<MortarShell>;
[NetworkName("CMortarShell")]
public partial class MortarShell : BaseEntity
{
	public static readonly SendTable DT_MortarShell = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Lifespan, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Radius, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.SurfaceNormal, 0, PropFlags.VarInt | PropFlags.VarInt),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_MortarShell);

	[NetworkName("m_flLifespan")]
	[NetworkVar] public partial float Lifespan { get; set; }
	[NetworkName("m_flRadius")]
	[NetworkVar] public partial float Radius { get; set; }
	[NetworkName("m_vecSurfaceNormal")]
	[NetworkVar] public partial Vector3 SurfaceNormal { get; set; }
}
