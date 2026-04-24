#if CLIENT_DLL || GAME_DLL
using Source.Common;

using System.Numerics;
namespace Game.Shared;
using FIELD = Source.FIELD<BaseToggle>;
public class BaseToggle : BaseEntity
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseToggle = new(DT_BaseEntity, [
#if CLIENT_DLL
		RecvPropVector(FIELD.OF(nameof(FinalDest))),
		RecvPropInt(FIELD.OF(nameof(MovementType))),
		RecvPropFloat(FIELD.OF(nameof(MoveTargetTime)))
#else
		SendPropVector(FIELD.OF(nameof(FinalDest)), 0, PropFlags.NoScale),
		SendPropInt(FIELD.OF(nameof(MovementType)), 4),
		SendPropFloat(FIELD.OF(nameof(MoveTargetTime)), 0, PropFlags.NoScale)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("BaseToggle", null, null, DT_BaseToggle).WithManualClassID(StaticClassIndices.CBaseToggle);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("BaseToggle", DT_BaseToggle).WithManualClassID(StaticClassIndices.CBaseToggle);
#endif
	public Vector3 FinalDest;
	public int MovementType;
	public TimeUnit_t MoveTargetTime;
}
#endif
