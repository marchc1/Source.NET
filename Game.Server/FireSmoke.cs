using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FireSmoke>;
[LinkEntityToClass("_firesmoke")]
[NetworkName("CFireSmoke")]
public class FireSmoke : BaseEntity
{
	public static readonly SendTable DT_FireSmoke = new(DT_BaseEntity, [
		SendPropFloat(FIELD.OF(nameof(StartScale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(Scale)), 0, PropFlags.NoScale),
		SendPropFloat(FIELD.OF(nameof(ScaleTime)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(FlameModelIndex)), 14, 0),
		SendPropInt(FIELD.OF(nameof(FlameFromAboveModelIndex)), 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FireSmoke);

	[NetworkName("m_flStartScale")]
	public float StartScale;
	[NetworkName("m_flScale")]
	public float Scale;
	[NetworkName("m_flScaleTime")]
	public float ScaleTime;
	[NetworkName("m_nFlags")]
	public int Flags;
	[NetworkName("m_nFlameModelIndex")]
	public int FlameModelIndex;
	[NetworkName("m_nFlameFromAboveModelIndex")]
	public int FlameFromAboveModelIndex;
}
