using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Fish>;
[LinkEntityToClass("fish")]
[NetworkName("CFish")]
public class Fish : BaseAnimating
{
	public static readonly SendTable DT_CFish = new([
		SendPropVector(FIELD.OF(nameof(PoolOrigin)), 0, PropFlags.Coord),
		SendPropFloat(FIELD.OF(nameof(Le)), 7, 0, 0.0f, 360.0f),
		SendPropFloat(FIELD.OF(nameof(X)), 7, 0, -255.0f, 255.0f),
		SendPropFloat(FIELD.OF(nameof(Y)), 7, 0, -255.0f, 255.0f),
		SendPropFloat(FIELD.OF(nameof(Z)), 0, PropFlags.Coord | PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(ModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(nameof(WaterLevel)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_CFish);

	[NetworkName("m_poolOrigin")]
	public Vector3 PoolOrigin;
	[NetworkName("m_angle")]
	public float Le;
	[NetworkName("m_x")]
	public float X;
	[NetworkName("m_y")]
	public float Y;
	[NetworkName("m_z")]
	public float Z;
	[NetworkName("m_waterLevel")]
	public new float WaterLevel;
}
