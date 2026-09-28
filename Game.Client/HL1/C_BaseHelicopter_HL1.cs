using Source.Common;
using Game.Shared;

namespace Game.Client.HL1;

public class C_BaseHelicopter_HL1 : C_AI_BaseNPC
{
	public static readonly RecvTable DT_BaseHelicopter_HL1 = new(DT_AI_BaseNPC, []);
	public static readonly new ClientClass ClientClass = new ClientClass("BaseHelicopter_HL1", DT_BaseHelicopter_HL1).WithManualClassID(StaticClassIndices.CBaseHelicopter_HL1);
}
