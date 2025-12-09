using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;

using FIELD = FIELD<C_TriggerPlayerMovement>;
public class C_TriggerPlayerMovement : C_BaseTrigger
{
	public static readonly RecvTable DT_TriggerPlayerMovement = new(DT_BaseTrigger, []);
	public static readonly new ClientClass ClientClass = new ClientClass("TriggerPlayerMovement", DT_TriggerPlayerMovement).WithManualClassID(StaticClassIndices.CTriggerPlayerMovement);
}
