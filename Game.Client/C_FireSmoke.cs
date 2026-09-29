using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_FireSmoke>;
[NetworkName("CFireSmoke")]
public class C_FireSmoke : C_BaseEntity
{
	public static readonly RecvTable DT_FireSmoke = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(StartScale))),
		// todo: RecvProxy_Scale
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		// todo: RecvProxy_ScaleTime
		RecvPropFloat(FIELD.OF(nameof(ScaleTime))),
		RecvPropInt(FIELD.OF(nameof(Flags))),
		RecvPropInt(FIELD.OF(nameof(FlameModelIndex))),
		RecvPropInt(FIELD.OF(nameof(FlameFromAboveModelIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FireSmoke);

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
