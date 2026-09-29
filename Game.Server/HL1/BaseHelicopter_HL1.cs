using Source.Common;
using Game.Shared;

namespace Game.Server.HL1;

[NetworkName("CBaseHelicopter_HL1")]
public class BaseHelicopter_HL1 : AI_BaseNPC
{
	public static readonly SendTable DT_BaseHelicopter_HL1 = new(DT_AI_BaseNPC, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseHelicopter_HL1);
}
