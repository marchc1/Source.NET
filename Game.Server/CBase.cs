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

	public static void ServiceEventQueue() => throw new NotImplementedException();
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

	public EventAction(ReadOnlySpan<char> actionData = default) => throw new NotImplementedException();
}

public class BaseEntityOutput
{
	protected Variant_t Value;
	protected EventAction? ActionList;

	protected BaseEntityOutput() { }

	public float GetMaxDelay() => throw new NotImplementedException();

	public void FireOutput(Variant_t value, BaseEntity? activator, BaseEntity? caller, float delay = 0) => throw new NotImplementedException();

	public void ParseEventAction(ReadOnlySpan<char> eventData) => throw new NotImplementedException();

	public void AddEventAction(EventAction eventAction) => throw new NotImplementedException();

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
	public void FireOutput(BaseEntity? activator, BaseEntity? caller, float delay = 0) => throw new NotImplementedException();
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

	public bool Parse(in SaveRestoreFieldInfo fieldInfo, ReadOnlySpan<char> value) => throw new NotImplementedException();
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

	public EventQueue() => throw new NotImplementedException();

	public void AddEvent(ReadOnlySpan<char> target, ReadOnlySpan<char> targetInput, Variant_t value, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) => throw new NotImplementedException();

	public void AddEvent(BaseEntity? target, ReadOnlySpan<char> targetInput, Variant_t value, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) => throw new NotImplementedException();

	public void AddEvent(BaseEntity? target, ReadOnlySpan<char> action, float fireDelay, BaseEntity? activator, BaseEntity? caller, int outputID = 0) => throw new NotImplementedException();

	void AddEvent(EventQueuePrioritizedEvent newEvent) => throw new NotImplementedException();

	void RemoveEvent(EventQueuePrioritizedEvent pe) => throw new NotImplementedException();

	public void Init() => throw new NotImplementedException();

	public void Clear() => throw new NotImplementedException();

	public void Dump() => throw new NotImplementedException();

	public void ServiceEvents() => throw new NotImplementedException();

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
