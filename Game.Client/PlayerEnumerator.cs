using Source.Common;

using System.Numerics;

namespace Game.Client;

public struct PlayerEnumerator(float radius, Vector3 origin) : IPartitionEnumerator
{
	public float RadiusSquared = radius * radius;
	public Vector3 Origin = origin;
	public List<C_BaseEntity> Objects = [];

	public readonly int GetObjectCount() => Objects.Count;

	public readonly C_BaseEntity? GetObject(int index) {
		if (index < 0 || index >= GetObjectCount())
			return null;

		return Objects[index];
	}

	public IterationRetval EnumElement(IHandleEntity? handleEntity) {
		C_BaseEntity? ent = cl_entitylist.GetBaseEntityFromHandle(handleEntity!.GetRefEHandle());
		if (ent == null)
			return IterationRetval.Continue;

		if (!ent.IsPlayer())
			return IterationRetval.Continue;

		Vector3 deltaPos = ent.GetAbsOrigin() - Origin;
		if (deltaPos.LengthSquared() > RadiusSquared)
			return IterationRetval.Continue;

		Objects.Add(ent);

		return IterationRetval.Continue;
	}
}
