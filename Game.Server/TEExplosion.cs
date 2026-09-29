using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEExplosion>;
[NetworkName("CTEExplosion")]
public class TEExplosion(ReadOnlySpan<char> name) : TEParticleSystem(name)
{
	public static readonly SendTable DT_TEExplosion = new(DT_TEParticleSystem, [
		SendPropFloat(FIELD.OF(nameof(Scale)), 9, 0, 0.0f, 51.2f),
		SendPropInt(FIELD.OF(nameof(Flags)), 8, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Radius)), 16, PropFlags.Unsigned),
		SendPropInt(FIELD.OF(nameof(Magnitude)), 16, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEExplosion);

	public int ModelIndex;
	[NetworkName("m_fScale")]
	public float Scale;
	public int FrameRate;
	[NetworkName("m_nFlags")]
	public int Flags;
	public Vector3 Normal;
	public int ChMaterialType;
	[NetworkName("m_nRadius")]
	public int Radius;
	[NetworkName("m_nMagnitude")]
	public int Magnitude;
}
