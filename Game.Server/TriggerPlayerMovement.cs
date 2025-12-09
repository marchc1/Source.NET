using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;

using FIELD = FIELD<TriggerPlayerMovement>;
public class TriggerPlayerMovement : BaseTrigger
{
	public static readonly SendTable DT_TriggerPlayerMovement = new(DT_BaseTrigger, []);
	public static readonly new ServerClass ServerClass = new ServerClass("TriggerPlayerMovement", DT_TriggerPlayerMovement).WithManualClassID(StaticClassIndices.CTriggerPlayerMovement);
}
