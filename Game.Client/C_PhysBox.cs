using Game.Shared;

using Source.Common;


namespace Game.Client;
public class C_PhysBox : C_BaseEntity
{
	public static readonly RecvTable DT_PhysBox = new(DT_BaseEntity, []);
	public static readonly new ClientClass ClientClass = new ClientClass("PhysBox", DT_PhysBox).WithManualClassID(StaticClassIndices.CPhysBox);
}
