global using static Game.Server.EntityListGlobals;

using Game.Server;
using Game.Shared;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public static class EntityListGlobals
{
	public static readonly GlobalEntityList gEntList = new();
	public static BaseEntityList g_pEntityList = gEntList;

	[ConCommand("report_entities", "List all entities")]
	static void report_entities() {
		if (!Util.IsCommandIssuedByServerAdmin())
			return;

		SortedEntityList list = new();
		BaseEntity? ent = gEntList.FirstEnt();
		while (ent != null) {
			list.AddEntityToList(ent);
			ent = gEntList.NextEnt(ent);
		}

		list.ReportEntityList();
	}
}

class SortedEntityList
{
	private readonly List<BaseEntity?> SortedList = new();
	private readonly EntityReportLess Comparer = new();
	private int EmptyCount;

	private sealed class EntityReportLess : IComparer<BaseEntity?>
	{
		public int Compare(BaseEntity? src1, BaseEntity? src2) {
			if (src1 == null && src2 == null)
				return 0;
			if (src1 == null)
				return -1;
			if (src2 == null)
				return 1;

			return src1.GetClassname().CompareTo(src2.GetClassname(), StringComparison.Ordinal);
		}
	}

	public void AddEntityToList(BaseEntity? entity) {
		if (entity == null) {
			EmptyCount++;
			return;
		}

		int index = SortedList.BinarySearch(entity, Comparer);
		if (index < 0)
			index = ~index;

		SortedList.Insert(index, entity);
	}

	public void ReportEntityList() {
		ReadOnlySpan<char> lastClass = default;
		int count = 0;
		int edicts = 0;

		for (int i = 0; i < SortedList.Count; i++) {
			var entity = SortedList[i];
			if (entity == null)
				continue;

			if (entity.Edict() != null)
				edicts++;

			ReadOnlySpan<char> className = entity.GetClassname();

			if (!className.Equals(lastClass, StringComparison.Ordinal)) {
				if (count > 0)
					Msg($"Class: {lastClass} ({count})\n");

				lastClass = className;
				count = 1;
			}
			else {
				count++;
			}
		}

		if (!lastClass.IsEmpty && count > 0)
			Msg($"Class: {lastClass} ({count})\n");

		if (SortedList.Count > 0)
			Msg($"Total {SortedList.Count} entities ({EmptyCount} empty, {edicts} edicts)\n");
	}
}

public class GlobalEntityList : BaseEntityList
{
	public int HighestEnt; // the topmost used array index
	public int NumEnts;
	public int NumEdicts;

	public bool ClearingEntities;
	public readonly List<IEntityListener> EntityListeners = [];

	public BaseEntity? GetBaseEntity(BaseHandle ent) {
		IServerUnknown? unk = (IServerUnknown?)LookupEntity(ent);
		return unk == null ? null : (BaseEntity?)unk.GetBaseEntity();
	}

	public BaseEntity FirstEnt() => NextEnt(null);

	public BaseEntity? NextEnt(BaseEntity? currentEnt) {
		if (currentEnt == null) {
			EntInfo? info = FirstEntInfo();
			if (info == null)
				return null;

			return (BaseEntity?)info.Entity;
		}

		EntInfo? list = GetEntInfoPtr(currentEnt.GetRefEHandle());
		if (list != null)
			list = NextEntInfo(list);

		while (list != null) {
			return (BaseEntity?)list.Entity;
			list = NextEntInfo(list); //??
		}

		return null;
	}

	public void CleanupDeleteList() {
		// todo
	}

	public BaseEntity? FindEntityByName(BaseEntity startEntity, ReadOnlySpan<char> name, BaseEntity? searchingEntity, BaseEntity? activator, BaseEntity? caller, int/*IEntityFindFilter*/? filter) {
		if (name.IsEmpty)
			return null;

		if (name[0] == '!') { // todo

			// if (startEntity == null)
			// 	return FindEntityProcedural(name, searchingEntity, activator, caller);

			return null;
		}

		return null; // TODO
	}
}

public enum NotifySystemEvent
{
	Teleport,
	Destroy
}
public struct NotifyTeleportParams
{
	public Vector3 PrevOrigin;
	public QAngle PrevAngles;
	public bool PhysicsRotate;
}

public struct NotifyDestroyParams;

public ref struct NotifySystemEventParams
{
	public ref readonly NotifyTeleportParams Teleport;
	public ref readonly NotifyDestroyParams Destroy;

	public NotifySystemEventParams(in NotifyTeleportParams parms) => Teleport = ref parms;
	public NotifySystemEventParams(in NotifyDestroyParams parms) => Destroy = ref parms;
}

public interface INotify
{
	void AddEntity(BaseEntity notify, BaseEntity watched);

	// Remove notification for an entity
	void RemoveEntity(BaseEntity notify, BaseEntity watched);

	// Call the named input in each entity who is watching pEvent's status
	void ReportNamedEvent(BaseEntity entity, ReadOnlySpan<char> eventName);

	// System events don't make sense as inputs, so are handled through a generic notify function
	void ReportSystemEvent(BaseEntity entity, NotifySystemEvent eventType, in NotifySystemEventParams parms);

	public void ReportDestroyEvent(BaseEntity entity) {
		NotifyDestroyParams destroy = default;
		ReportSystemEvent(entity, NotifySystemEvent.Destroy, new(in destroy));
	}

	public void ReportTeleportEvent(BaseEntity entity, in Vector3 prevOrigin, in QAngle prevAngles, bool physicsRotate) {
		NotifyTeleportParams teleport = default;
		teleport.PrevOrigin = prevOrigin;
		teleport.PrevAngles = prevAngles;
		teleport.PhysicsRotate = physicsRotate;
		ReportSystemEvent(entity, NotifySystemEvent.Teleport, new(in teleport));
	}

	// Remove this entity from the notify list
	void ClearEntity(BaseEntity notify);
}

public interface IEntityListener
{
	void OnEntityCreated(BaseEntity ent) { }
	void OnEntitySpawned(BaseEntity ent) { }
	void OnEntityDeleted(BaseEntity ent) { }
}
