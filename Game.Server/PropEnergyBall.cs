using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Game.Server.HL2;
namespace Game.Server;
using FIELD = FIELD<PropEnergyBall>;
public class PropEnergyBall : PropCombineBall
{
	public static readonly SendTable DT_PropEnergyBall = new(DT_PropCombineBall, [
		SendPropBool(FIELD.OF(nameof(IsInfiniteLife))),
		SendPropFloat(FIELD.OF(nameof(TimeTillDeath)), 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("PropEnergyBall", DT_PropEnergyBall).WithManualClassID(StaticClassIndices.CPropEnergyBall);

	public bool IsInfiniteLife;
	public float TimeTillDeath;
}
