using Game.Shared;

using Source.Common;

namespace Game.Client.HL2;

using DEFINE = Source.DEFINE<C_BaseHLPlayer>;
using FIELD = Source.FIELD<C_BaseHLPlayer>;

public partial class C_BaseHLPlayer : C_BasePlayer
{
	public static readonly RecvTable DT_HL2_Player = new(DT_BasePlayer, [
		RecvPropDataTable(nameof(HL2Local), C_HL2PlayerLocalData.DT_HL2Local, 0, DataTableRecvProxy_PointerDataTable),
		RecvPropBool(FIELD.OF(nameof(_IsSprinting)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("HL2_Player", null, null, DT_HL2_Player)
															.WithManualClassID(StaticClassIndices.CHL2_Player);

	public static readonly new DataMap PredMap = new(typeof(C_BaseHLPlayer), C_BasePlayer.PredMap, [
		DEFINE.PRED_TYPEDESCRIPTION( nameof(HL2Local), C_HL2PlayerLocalData.PredMap ),
		DEFINE.PRED_FIELD( nameof(_IsSprinting), FieldType.Boolean, FieldTypeDescFlags.InSendTable ),
	]); public override DataMap? GetPredDescMap() => PredMap;

	public C_BaseHLPlayer() {
		AddVar(this, FIELD.OF("Local.PunchAngle"), Local.iv_PunchAngle, LatchFlags.LatchSimulationVar);
		AddVar(this, FIELD.OF("Local.PunchAngleVel"), Local.iv_PunchAngleVel, LatchFlags.LatchSimulationVar);
	}

	public override void OnDataChanged(DataUpdateType updateType) {
		if (updateType == DataUpdateType.Created)
			SetNextClientThink(CLIENT_THINK_ALWAYS);
		base.OnDataChanged(updateType);
	}

	public readonly C_HL2PlayerLocalData HL2Local = new();
	public bool _IsSprinting;

	public bool IsSprinting() => _IsSprinting;
}
