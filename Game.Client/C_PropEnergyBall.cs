using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Game.Client.HL2;
namespace Game.Client;
using FIELD = FIELD<C_PropEnergyBall>;
[LinkEntityToClass("prop_energy_ball")]
[NetworkName("CPropEnergyBall")]
public class C_PropEnergyBall : C_PropCombineBall
{
	public static readonly RecvTable DT_PropEnergyBall = new(DT_PropCombineBall, [
		RecvPropBool(FIELD.OF(nameof(IsInfiniteLife))),
		RecvPropFloat(FIELD.OF(nameof(TimeTillDeath))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_PropEnergyBall);

	[NetworkName("m_bIsInfiniteLife")]
	public bool IsInfiniteLife;
	[NetworkName("m_fTimeTillDeath")]
	public float TimeTillDeath;
}
