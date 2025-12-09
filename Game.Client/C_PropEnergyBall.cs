using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Game.Client.HL2;
namespace Game.Client;
using FIELD = FIELD<C_PropEnergyBall>;
public class C_PropEnergyBall : C_PropCombineBall
{
	public static readonly RecvTable DT_PropEnergyBall = new(DT_PropCombineBall, [
		RecvPropBool(FIELD.OF(nameof(IsInfiniteLife))),
		RecvPropFloat(FIELD.OF(nameof(TimeTillDeath))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("PropEnergyBall", DT_PropEnergyBall).WithManualClassID(StaticClassIndices.CPropEnergyBall);

	public bool IsInfiniteLife;
	public float TimeTillDeath;
}
