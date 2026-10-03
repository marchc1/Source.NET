using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<MortarShell>;
[LinkEntityToClass("mortarshell")]
[NetworkName("CMortarShell")]
public class MortarShell : BaseEntity
{
	public static readonly SendTable DT_MortarShell = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(Lifespan)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Radius)), 0, PropFlags.NoScale),
		SendPropVector(FIELD.OF(nameof(SurfaceNormal)), 0, PropFlags.VarInt | PropFlags.VarInt),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_MortarShell);

	[NetworkName("m_flLifespan")]
	public float Lifespan;
	[NetworkName("m_flRadius")]
	public float Radius;
	[NetworkName("m_vecSurfaceNormal")]
	public Vector3 SurfaceNormal;
}
