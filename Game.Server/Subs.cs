global using static Game.Server.SubsGlobals;

using Game.Shared;

using Source;

namespace Game.Server;

public static class SubsGlobals
{
	public static void FireTargets(ReadOnlySpan<char> targetName, BaseEntity? activator, BaseEntity? caller, UseType useType, float value) {
		BaseEntity? target = null;
		if (targetName.IsEmpty)
			return;

		DevMsg(2, $"Firing: ({targetName})\n");

		for (; ; ) {
			BaseEntity? searchingEntity = activator;
			target = gEntList.FindEntityByName(target, targetName, searchingEntity, activator, caller);
			if (target == null)
				break;

			if (!target.IsMarkedForDeletion()) {
				DevMsg(2, $"[{gpGlobals.TickCount % 1000:D3}] Found: {target.GetDebugName()}, firing ({targetName})\n");
				target.Use(activator, caller, useType, value);
			}
		}
	}
}
