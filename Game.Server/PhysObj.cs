using Game.Shared;

using Source.Common;

namespace Game.Server;

public class PhysBox : Breakable
{
	public static readonly SendTable DT_PhysBox = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass("PhysBox", DT_PhysBox).WithManualClassID(StaticClassIndices.CPhysBox);
}
