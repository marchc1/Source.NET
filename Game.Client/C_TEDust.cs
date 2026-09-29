using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEDust>;
[NetworkName("CTEDust")]
public class C_TEDust : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEDust = new(DT_TEParticleSystem, [
		RecvPropFloat(FIELD.OF(nameof(LSize))),
		RecvPropFloat(FIELD.OF(nameof(LSpeed))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TEDust);

	[NetworkName("m_flSize")]
	public float LSize;
	[NetworkName("m_flSpeed")]
	public float LSpeed;
	[NetworkName("m_vecDirection")]
	public Vector3 Direction;
}
