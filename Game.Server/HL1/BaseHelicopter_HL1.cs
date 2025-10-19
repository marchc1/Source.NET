using Source.Common;
using Game.Shared;

namespace Game.Server.HL1;

public class BaseHelicopter_HL1 : SharedBaseEntity
{
	public static readonly SendTable DT_BaseHelicopter_HL1 = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("BaseHelicopter_HL1", DT_BaseHelicopter_HL1).WithManualClassID(StaticClassIndices.CBaseHelicopter_HL1);
}
