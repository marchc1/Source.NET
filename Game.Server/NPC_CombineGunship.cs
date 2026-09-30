using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<NPC_CombineGunship>;
[NetworkName("CNPC_CombineGunship")]
public partial class NPC_CombineGunship : BaseHelicopter
{
	public static readonly SendTable DT_CombineGunship = new(DT_BaseHelicopter, [
		SendPropVector(NetworkVarFields.HitPos, 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CombineGunship);

	[NetworkName("m_vecHitPos")]
	[NetworkVar] public partial Vector3 HitPos { get; set; }
}
