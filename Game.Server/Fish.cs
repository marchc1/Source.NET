using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Fish>;
[NetworkName("CFish")]
public partial class Fish : BaseAnimating
{
	public static readonly SendTable DT_CFish = new([
		SendPropVector(NetworkVarFields.PoolOrigin, 0, PropFlags.Coord),
		SendPropFloat(NetworkVarFields.Le, 7, 0, 0.0f, 360.0f),
		SendPropFloat(NetworkVarFields.X, 7, 0, -255.0f, 255.0f),
		SendPropFloat(NetworkVarFields.Y, 7, 0, -255.0f, 255.0f),
		SendPropFloat(NetworkVarFields.Z, 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropInt(BaseEntity.NetworkVarFields.ModelIndex, 14, 0),
		SendPropInt(BaseEntity.NetworkVarFields.LifeState, 3, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.WaterLevel, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CFish);

	[NetworkName("m_poolOrigin")]
	[NetworkVar] public partial Vector3 PoolOrigin { get; set; }
	[NetworkName("m_angle")]
	[NetworkVar] public partial float Le { get; set; }
	[NetworkName("m_x")]
	[NetworkVar] public partial float X { get; set; }
	[NetworkName("m_y")]
	[NetworkVar] public partial float Y { get; set; }
	[NetworkName("m_z")]
	[NetworkVar] public partial float Z { get; set; }
	[NetworkName("m_waterLevel")]
	[NetworkVar] public new partial float WaterLevel { get; set; }
}
