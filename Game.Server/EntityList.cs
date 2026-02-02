global using static Game.Server.EntityListGlobals;

using Game.Server;
using Game.Shared;

using Source.Common;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Server;

public static class EntityListGlobals
{
	public static readonly GlobalEntityList gEntList = new();
	public static BaseEntityList g_pEntityList = gEntList;
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
