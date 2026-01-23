using Game.Shared;

using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<HL2_Player>;

public class HL2_Player : BasePlayer {
	public static readonly SendTable DT_HL2_Player = new(DT_BasePlayer, [
		SendPropDataTable(nameof(HL2Local), HL2PlayerLocalData.DT_HL2Local, SendProxy_SendLocalDataTable),
		SendPropBool(FIELD.OF(nameof(_IsSprinting)))
	]);
	public static new readonly ServerClass ServerClass = new ServerClass("HL2_Player", DT_HL2_Player)
															.WithManualClassID(StaticClassIndices.CHL2_Player);

	public readonly HL2PlayerLocalData HL2Local = new();
	public bool _IsSprinting;
	public bool IsSprinting() => _IsSprinting;
}
