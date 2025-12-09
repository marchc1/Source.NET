using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEDust>;
public class C_TEDust : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEDust = new(DT_TEParticleSystem, [
		RecvPropFloat(FIELD.OF(nameof(LSize))),
		RecvPropFloat(FIELD.OF(nameof(LSpeed))),
		RecvPropVector(FIELD.OF(nameof(Direction))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEDust", DT_TEDust).WithManualClassID(StaticClassIndices.CTEDust);

	public float LSize;
	public float LSpeed;
	public Vector3 Direction;
}
