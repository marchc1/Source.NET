using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Game.Server.HL2;
namespace Game.Server;
using FIELD = FIELD<PropEnergyBall>;
[NetworkName("CPropEnergyBall")]
public partial class PropEnergyBall : PropCombineBall
{
	public static readonly SendTable DT_PropEnergyBall = new(DT_PropCombineBall, [
		SendPropBool(NetworkVarFields.IsInfiniteLife),
		SendPropFloat(NetworkVarFields.TimeTillDeath, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PropEnergyBall);

	[NetworkName("m_bIsInfiniteLife")]
	[NetworkVar] public partial bool IsInfiniteLife { get; set; }
	[NetworkName("m_fTimeTillDeath")]
	[NetworkVar] public partial float TimeTillDeath { get; set; }
}
