global using static Game.Server.CBaseGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public enum ITBD
{
	Paralyze,
	NerveGas,
	PoisonRecover,
	Radiation,
	DrownRecover,
	Acid,
	SlowBurn,
	SlowFreeze,
	TimeBasedDamageCount
}

public static class CBaseGlobals
{
	public const int EVENT_FIRE_ALWAYS = -1;

	public static readonly EventsSaveDataOps g_EventsSaveDataOps = new();
	public static readonly ISaveRestoreOps eventFuncs = g_EventsSaveDataOps;

	public static readonly EventQueue g_EventQueue = new();

	static short EVENTQUEUE_SAVE_RESTORE_VERSION = 1;

	// public static readonly EventQueue_SaveRestoreBlockHandler g_EventQueue_SaveRestoreBlockHandler = new();

	// public static readonly VariantSaveDataOps g_VariantSaveDataOps = new();
	// public static readonly ISaveRestoreOps variantFuncs = g_VariantSaveDataOps;

	[ConCommand("dumpeventqueue", "Dump the contents of the Entity I/O event queue to the console.")]
	static void CC_DumpEventQueue() => throw new NotImplementedException();

	public static void ServiceEventQueue() => g_EventQueue.ServiceEvents();
}

public class EventAction
{
	public string? Target;
	public string? TargetInput;
	public string? Parameter;
	public float Delay;
	public int TimesToFire;
	public int IDStamp;
	public static int s_NextIDStamp;
	public EventAction? Next;

	public EventAction(ReadOnlySpan<char> actionData = default) {
		Next = null;
		IDStamp = ++s_NextIDStamp;

		Delay = 0;
		Target = null;
		Parameter = null;
		TargetInput = null;
		TimesToFire = EVENT_FIRE_ALWAYS;

		if (actionData.IsEmpty)
			return;

		nexttoken(out ReadOnlySpan<char> token, actionData, ',', out ReadOnlySpan<char> psz);
		if (!token.IsEmpty)
			Target = new(token);

		nexttoken(out token, psz, ',', out psz);
		if (!token.IsEmpty)
			TargetInput = new(token);
		else
			TargetInput = "Use";

		nexttoken(out token, psz, ',', out psz);
		if (!token.IsEmpty)
			Parameter = new(token);

		nexttoken(out token, psz, ',', out psz);
		if (!token.IsEmpty)
			Delay = strtof(token, out _);

		nexttoken(out token, psz, ',', out _);
		if (!token.IsEmpty) {
			TimesToFire = atoi(token);
			if (TimesToFire == 0)
				TimesToFire = EVENT_FIRE_ALWAYS;
		}
	}
}

public class BaseEntityOutput
{
	protected Variant_t Value;
	protected EventAction? ActionList;

	protected BaseEntityOutput() { }

	public float GetMaxDelay() {
		float maxDelay = 0;
		EventAction? ev = ActionList;

		while (ev != null) {
			if (ev.Delay > maxDelay)
				maxDelay = ev.Delay;
			ev = ev.Next;
		}

		return maxDelay;
	}

	public void FireOutput(Variant_t value, BaseEntity? activator, BaseEntity? caller, float delay = 0) {
		EventAction? ev = ActionList;
		EventAction? prev = null;

		while (ev != null) {
			if (ev.Parameter == null)
				g_EventQueue.AddEvent(ev.Target, ev.TargetInput, value, ev.Delay + delay, activator, caller, ev.IDStamp);
			else {
				Variant_t valueOverride = new();
				valueOverride.SetString(ev.Parameter);
				g_EventQueue.AddEvent(ev.Target, ev.TargetInput, valueOverride, ev.Delay, activator, caller, ev.IDStamp);
			}

			if (ev.Delay != 0)
				DevMsg(2, $"({gpGlobals.CurTime:F2}) output: ({(caller != null ? caller.GetClassname() : "NULL")},{(caller != null ? caller.GetEntityName() : "NULL")}) -> ({ev.Target},{ev.TargetInput},{ev.Delay:F1})({ev.Parameter})\n");
			else
				DevMsg(2, $"({gpGlobals.CurTime:F2}) output: ({(caller != null ? caller.GetClassname() : "NULL")},{(caller != null ? caller.GetEntityName() : "NULL")}) -> ({ev.Target},{ev.TargetInput})({ev.Parameter})\n");

			// if (caller != null && (caller.DebugOverlays & OVERLAY_MESSAGE_BIT) != 0)
			// 	caller.DrawOutputOverlay(ev);

			bool remove = false;
			if (ev.TimesToFire != EVENT_FIRE_ALWAYS) {
				ev.TimesToFire--;
				if (ev.TimesToFire == 0) {
					DevMsg(2, $"Removing from action list: ({(caller != null ? caller.GetClassname() : "NULL")},{(caller != null ? caller.GetEntityName() : "NULL")}) -> ({ev.Target},{ev.TargetInput})\n");
					remove = true;
				}
			}

			if (!remove) {
				prev = ev;
				ev = ev.Next;
			}
			else {
				if (prev != null)
					prev.Next = ev.Next;
				else
					ActionList = ev.Next;

				ev = ev.Next;
			}
		}
	}

	public void ParseEventAction(ReadOnlySpan<char> eventData) => AddEventAction(new EventAction(eventData));

	public void AddEventAction(EventAction eventAction) {
		eventAction.Next = ActionList;
		ActionList = eventAction;
	}

	public int Save(ISave save) => throw new NotImplementedException();

	public int Restore(IRestore restore, int elementCount) => throw new NotImplementedException();

	public int NumberOfElements() => throw new NotImplementedException();

	public void DeleteAllElements() => throw new NotImplementedException();

	public FieldType ValueFieldType() => throw new NotImplementedException();
}

public class EntityOutputTemplate<T> : BaseEntityOutput
{
	public void Init(T value) => throw new NotImplementedException();

	public void Set(T value, BaseEntity? activator, BaseEntity? caller) => throw new NotImplementedException();

	public T Get() => throw new NotImplementedException();
}

public class OutputVector : BaseEntityOutput
{
	public void Init(in Vector3 value) => throw new NotImplementedException();

	public void Set(in Vector3 value, BaseEntity? activator, BaseEntity? caller) => throw new NotImplementedException();

	public void Get(out Vector3 vec) => throw new NotImplementedException();
}

public class OutputPositionVector : BaseEntityOutput
{
	public void Init(in Vector3 value) => throw new NotImplementedException();

	public void Set(in Vector3 value, BaseEntity? activator, BaseEntity? caller) => throw new NotImplementedException();

	public void Get(out Vector3 vec) => throw new NotImplementedException();
}

public class OutputEvent : BaseEntityOutput
{
	public void FireOutput(BaseEntity? activator, BaseEntity? caller, float delay = 0) {
		Variant_t val = new();
		val.Set(FieldType.Void, null!, null!);
		FireOutput(val, activator, caller, delay);
	}
}

public class OutputVariant : EntityOutputTemplate<Variant_t>;
public class OutputInt : EntityOutputTemplate<int>;
public class OutputFloat : EntityOutputTemplate<float>;
public class OutputString : EntityOutputTemplate<string?>;
public class OutputEHANDLE : EntityOutputTemplate<EHANDLE>;
public class OutputColor32 : EntityOutputTemplate<Color>;

public class EventsSaveDataOps : ISaveRestoreOps
{
	public void Save(in SaveRestoreFieldInfo fieldInfo, ISave save) => throw new NotImplementedException();

	public void Restore(in SaveRestoreFieldInfo fieldInfo, IRestore restore) => throw new NotImplementedException();

	public bool IsEmpty(in SaveRestoreFieldInfo fieldInfo) => throw new NotImplementedException();

	public void MakeEmpty(in SaveRestoreFieldInfo fieldInfo) => throw new NotImplementedException();

	public bool Parse(in SaveRestoreFieldInfo fieldInfo, ReadOnlySpan<char> value) {
		BaseEntityOutput ev = fieldInfo.Field.GetValue<BaseEntityOutput>(fieldInfo.Owner);
		ev.ParseEventAction(value);
		return true;
	}
}

public class MultiInputVar
{
	public class InputItem
	{
		public Variant_t Value;
		public int OutputID;
		public InputItem? Next;
	}

	public InputItem? InputList;
	public int UpdatedThisFrame;

	public void AddValue(Variant_t newVal, int outputID) => throw new NotImplementedException();
}

public class EventQueuePrioritizedEvent
{
	public TimeUnit_t FireTime;
	public string? Target;
	public string? TargetInput;
	public EHANDLE Activator;
	public EHANDLE Caller;
	public int OutputID;
	public EHANDLE EntTarget;
	public Variant_t VariantValue;
	public EventQueuePrioritizedEvent? Next;
	public EventQueuePrioritizedEvent? Prev;
}

// [LinkEntityToClass("event_queue_saveload_proxy")]
// public class EventQueueSaveLoadProxy : LogicalEntity
// {
// 	public int Save(ISave save) => throw new NotImplementedException();
//
// 	public int Restore(IRestore restore) => throw new NotImplementedException();
// }

// public class EventQueue_SaveRestoreBlockHandler : DefSaveRestoreBlockHandler
// {
// 	bool DoLoad;
//
// 	public ReadOnlySpan<char> GetBlockName() => throw new NotImplementedException();
//
// 	public void Save(ISave save) => throw new NotImplementedException();
//
// 	public void WriteSaveHeaders(ISave save) => throw new NotImplementedException();
//
// 	public void ReadRestoreHeaders(IRestore restore) => throw new NotImplementedException();
//
// 	public void Restore(IRestore restore, bool createPlayers) => throw new NotImplementedException();
// }
//
// public static ISaveRestoreBlockHandler GetEventQueueSaveRestoreBlockHandler() => throw new NotImplementedException();

public class EventQueue
{
	readonly EventQueuePrioritizedEvent Events = new();
	int ListCount;

	public EventQueue() {
		Events.FireTime = -float.MaxValue;
		Events.Next = null;

		Init();
	}

	public void AddEvent(ReadOnlySpan<char> target, ReadOnlySpan<char> targetInput, Variant_t value, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) {
		EventQueuePrioritizedEvent newEvent = new();
		newEvent.FireTime = gpGlobals.CurTime + fireDelay;
		newEvent.Target = new(target);
		newEvent.EntTarget.Set(null);
		newEvent.TargetInput = new(targetInput);
		newEvent.Activator.Set(activator);
		newEvent.Caller.Set(caller);
		newEvent.VariantValue = value;
		newEvent.OutputID = outputID;

		AddEvent(newEvent);
	}

	public void AddEvent(BaseEntity? target, ReadOnlySpan<char> targetInput, Variant_t value, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) {
		EventQueuePrioritizedEvent newEvent = new();
		newEvent.FireTime = gpGlobals.CurTime + fireDelay;
		newEvent.Target = null;
		newEvent.EntTarget.Set(target);
		newEvent.TargetInput = new(targetInput);
		newEvent.Activator.Set(activator);
		newEvent.Caller.Set(caller);
		newEvent.VariantValue = value;
		newEvent.OutputID = outputID;

		AddEvent(newEvent);
	}

	public void AddEvent(BaseEntity? target, ReadOnlySpan<char> action, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) {
		Variant_t value = new();
		value.Set(FieldType.Void, null!, null!);
		AddEvent(target, action, value, fireDelay, activator, caller, outputID);
	}

	void AddEvent(EventQueuePrioritizedEvent newEvent) {
		EventQueuePrioritizedEvent pe;
		for (pe = Events; pe.Next != null; pe = pe.Next)
			if (pe.Next.FireTime > newEvent.FireTime)
				break;

		Assert(pe != null);

		newEvent.Next = pe.Next;
		newEvent.Prev = pe;
		pe.Next = newEvent;
		if (newEvent.Next != null)
			newEvent.Next.Prev = newEvent;
	}

	void RemoveEvent(EventQueuePrioritizedEvent pe) {
		Assert(pe.Prev != null);
		pe.Prev!.Next = pe.Next;
		if (pe.Next != null)
			pe.Next.Prev = pe.Prev;
	}

	public void Init() => Clear();

	public void Clear() => Events.Next = null;

	public void Dump() => throw new NotImplementedException();

	public void ServiceEvents() {
		// if (!BaseEntity.Debug_ShouldStep())
		// 	return;

		EventQueuePrioritizedEvent? pe = Events.Next;

		while (pe != null && pe.FireTime <= gpGlobals.CurTime) {
			bool targetFound = false;

			if (pe.Target != null) {
				BaseEntity? searchingEntity = pe.Caller.Get();
				BaseEntity? target = null;
				while (true) {
					target = gEntList.FindEntityByName(target, pe.Target, searchingEntity, pe.Activator.Get(), pe.Caller.Get());
					if (target == null)
						break;

					target.AcceptInput(pe.TargetInput, pe.Activator.Get(), pe.Caller.Get(), pe.VariantValue, pe.OutputID);
					targetFound = true;
				}
			}

			if (pe.EntTarget.Get() != null) {
				pe.EntTarget.Get()!.AcceptInput(pe.TargetInput, pe.Activator.Get(), pe.Caller.Get(), pe.VariantValue, pe.OutputID);
				targetFound = true;
			}

			if (!targetFound) {
				if (pe.Target != null) {
					BaseEntity? target = null;
					while (true) {
						target = gEntList.FindEntityByClassname(target, pe.Target);
						if (target == null)
							break;

						target.AcceptInput(pe.TargetInput, pe.Activator.Get(), pe.Caller.Get(), pe.VariantValue, pe.OutputID);
						targetFound = true;
					}
				}
			}

			if (!targetFound) {
				ReadOnlySpan<char> pClass = "", pName = "";

				BaseEntity? caller = pe.Caller.Get();
				if (caller != null) {
					pClass = caller.GetClassname();
					pName = caller.GetEntityName();
				}

				DevMsg(2, $"unhandled input: ({pe.TargetInput}) -> ({pe.Target}), from ({pClass},{pName}); target entity not found\n");
			}

			RemoveEvent(pe);

			// if (BaseEntity.Debug_IsPaused()) {
			// 	if (!BaseEntity.Debug_Step())
			// 		break;
			// }

			pe = Events.Next;
		}
	}

	public void CancelEvents(BaseEntity? caller) => throw new NotImplementedException();

	public void CancelEventOn(BaseEntity? target, ReadOnlySpan<char> inputName) => throw new NotImplementedException();

	public bool HasEventPending(BaseEntity? target, ReadOnlySpan<char> inputName) => throw new NotImplementedException();

	public void ValidateQueue() => throw new NotImplementedException();

	public int Save(ISave save) => throw new NotImplementedException();

	public int Restore(IRestore restore) => throw new NotImplementedException();
}

public struct VariantSaveVector
{
	public Vector3 VecSave;
}

public struct VariantSaveVMatrix
{
	public Matrix4x4 MatSave;
}

public struct VariantSaveVMatrix3x4
{
	public Matrix3x4 MatSave;
}

// public class VariantSaveDataOps : DefSaveRestoreOps
// {
// 	public override void Save(in SaveRestoreFieldInfo fieldInfo, ISave save) => throw new NotImplementedException();
//
// 	public override void Restore(in SaveRestoreFieldInfo fieldInfo, IRestore restore) => throw new NotImplementedException();
//
// 	public override bool IsEmpty(in SaveRestoreFieldInfo fieldInfo) => throw new NotImplementedException();
//
// 	public override void MakeEmpty(in SaveRestoreFieldInfo fieldInfo) => throw new NotImplementedException();
// }
