using Game.Shared;

using Source.Common;

namespace Game.Server;

[LinkEntityToClass("func_physbox")]
[NetworkName("CPhysBox")]
public class PhysBox : Breakable
{
	public static readonly SendTable DT_PhysBox = new(DT_BaseEntity, []);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_PhysBox);
}
