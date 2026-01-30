global using static Game.Server.PhysConstraintEvents;
using Source.Common.Physics;

namespace Game.Server;

public class PhysConstraintEvents : IPhysicsConstraintEvent
{
	public static readonly PhysConstraintEvents constraintevents = new();
	public static readonly IPhysicsConstraintEvent g_pConstraintEvents = constraintevents;
	public void ConstraintBroken(IPhysicsConstraint constraint) {
		throw new NotImplementedException();
	}
}
