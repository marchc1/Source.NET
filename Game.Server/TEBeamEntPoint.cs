using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEBeamEntPoint>;
[NetworkName("CTEBeamEntPoint")]
public class TEBeamEntPoint(ReadOnlySpan<char> name) : BaseBeam(name)
{
	public static readonly SendTable DT_TEBeamEntPoint = new(DT_BaseBeam, [
		SendPropInt(FIELD.OF(nameof(StartEntity)), 24, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(EndEntity)), 24, PropFlags.Unsigned),
		SendPropVector(FIELD.OF(nameof(StartPoint)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(EndPoint)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEBeamEntPoint);

	[NetworkName("m_nStartEntity")]
	public int StartEntity;
	[NetworkName("m_nEndEntity")]
	public int EndEntity;
	[NetworkName("m_vecStartPoint")]
	public Vector3 StartPoint;
	[NetworkName("m_vecEndPoint")]
	public Vector3 EndPoint;
}
