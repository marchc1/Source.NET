#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using C_FuncLadder = Game.Shared.FuncLadder;
#endif

using Source.Common;

using System.Numerics;

namespace Game.Shared;

using FIELD = Source.FIELD<FuncLadder>;
public partial class FuncLadder : SharedBaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_FuncLadder = new(DT_BaseEntity, [
#if CLIENT_DLL
			RecvPropVector(FIELD.OF(nameof(PlayerMountPositionTop))),
			RecvPropVector(FIELD.OF(nameof(PlayerMountPositionBottom))),
			RecvPropVector(FIELD.OF(nameof(LadderDir))),
			RecvPropBool(FIELD.OF(nameof(FakeLadder))),
#else
			SendPropVector(FIELD.OF(nameof(PlayerMountPositionTop)), 0, PropFlags.NoScale),
			SendPropVector(FIELD.OF(nameof(PlayerMountPositionBottom)), 0, PropFlags.NoScale),
			SendPropVector(FIELD.OF(nameof(LadderDir)), 0, PropFlags.NoScale),
			SendPropBool(FIELD.OF(nameof(FakeLadder))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("FuncLadder", null, null, DT_FuncLadder).WithManualClassID(StaticClassIndices.CFuncLadder);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("FuncLadder", DT_FuncLadder).WithManualClassID(StaticClassIndices.CFuncLadder);
#endif

	public Vector3 PlayerMountPositionTop;
	public Vector3 PlayerMountPositionBottom;
	public Vector3 LadderDir;
	public bool FakeLadder;
}
#endif
