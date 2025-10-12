
using Source.Common;

namespace Game.Server;

public class BaseTempEntity
{
	public static readonly SendTable DT_BaseTempEntity = new([]);
	public static readonly ServerClass ServerClass = new ServerClass("BaseTempEntity", DT_BaseTempEntity).WithManualClassID(Shared.StaticClassIndices.CBaseTempEntity);
}
