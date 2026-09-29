using Game.Shared;

using Source;
using Source.Common;
namespace Game.Client;
using FIELD = FIELD<C_FuncRotating>;

[NetworkName("CFuncRotating")]
public class C_FuncRotating : C_BaseEntity
{
	public static readonly RecvTable DT_FuncRotating = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF_NAMED(nameof(NetworkOrigin), "m_vecOrigin")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[0]", "m_angRotation[0]")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[1]", "m_angRotation[1]")),
		RecvPropFloat(FIELD.OF_NAMED($"{nameof(NetworkAngles)}[2]", "m_angRotation[2]")),
		RecvPropInt(FIELD.OF(nameof(SimulationTime)), 0, RecvProxy_SimulationTime)
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_FuncRotating);
}

