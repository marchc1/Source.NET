using System.Runtime.CompilerServices;

namespace Source.Common.Engine;

[Flags]
public enum EdictFlags
{
	/// <summary>
	/// Game DLL sets this when the entity state changes
	/// </summary>
	EdictChanged = (1 << 0),
	/// <summary>
	/// this edict if free for reuse
	/// </summary>
	Free = (1 << 1),
	/// <summary>
	/// this is a full server entity
	/// </summary>
	Full = (1 << 2),
	/// <summary>
	/// call ShouldTransmit() each time, this is a fake flag
	/// </summary>
	FullCheck = (0 << 0),
	/// <summary>
	/// always transmit this entity
	/// </summary>
	Always = (1 << 3),
	/// <summary>
	/// don't transmit this entity
	/// </summary>
	DontSend = (1 << 4),
	/// <summary>
	/// always transmit entity, but cull against PVS
	/// </summary>
	PVSCheck = (1 << 5),
	PendingDormantCheck = (1 << 6),
	DirtyPVSInformation = (1 << 7),
	FullEdictChanged = (1 << 8)
}

[InlineArray(BaseEdict.MAX_CHANGE_OFFSETS)] public struct InlineArrayMaxChangeOffsets<T> { T? first; }
[InlineArray(BaseEdict.MAX_EDICT_CHANGE_INFOS)] public struct InlineArrayMaxEdictChangeInfos<T> { T first; }

public static class EdictGlobals {
	public static SharedEdictChangeInfo? g_SharedChangeInfo;
}

public class EdictChangeInfo {
	public InlineArrayMaxChangeOffsets<IFieldAccessor> ChangedFields;
	public ushort NumChangeFields;
}

public class SharedEdictChangeInfo {
	public ushort SerialNumber;
	public InlineArrayMaxEdictChangeInfos<EdictChangeInfo> ChangeInfos;
	public ushort NumChangeInfos;

	public SharedEdictChangeInfo(){
		SerialNumber = 1;
		for (int i = 0; i < BaseEdict.MAX_EDICT_CHANGE_INFOS; i++)
			ChangeInfos[i] = new();
	}
}

public class BaseEdict
{
	public const int MAX_CHANGE_OFFSETS = 19;
	public const int MAX_EDICT_CHANGE_INFOS = 100;

	public IServerEntity? GetIServerEntity() {
		if ((StateFlags & EdictFlags.Full) != 0)
			return (IServerEntity?)Unk;
		else
			return null;
	}
	public IServerNetworkable? GetNetworkable() {
		return Networkable;
	}

	public IServerUnknown? GetUnknown() {
		return Unk;
	}

	public void SetEdict(IServerUnknown? unk, bool fullEdict) {
		Unk = unk;
		if (unk != null && fullEdict)
			StateFlags = EdictFlags.Full;
		else
			StateFlags = 0;
	}

	public int AreaNum() => Unk == null ? 0 : Networkable!.AreaNum();
	public ReadOnlySpan<char> GetClassName() => Unk == null ? "" : Networkable!.GetClassName();

	public bool IsFree() => (StateFlags & EdictFlags.Free) != 0;
	public void SetFree() => StateFlags |= EdictFlags.Free;
	public void ClearFree() => StateFlags &= ~EdictFlags.Free;

	public bool HasStateChanged() => (StateFlags & EdictFlags.EdictChanged) != 0;
	public void ClearStateChanged() {
		StateFlags &= ~(EdictFlags.EdictChanged | EdictFlags.FullEdictChanged);
		SetChangeInfoSerialNumber(0);
	}
	public void StateChanged() {
		StateFlags |= (EdictFlags.EdictChanged | EdictFlags.FullEdictChanged);
		SetChangeInfoSerialNumber(0);
	}
	public void StateChanged(IFieldAccessor field) {
		if ((StateFlags & EdictFlags.FullEdictChanged) != 0)
			return;

		StateFlags |= EdictFlags.EdictChanged;

		if (g_SharedChangeInfo == null)
			return;

		IChangeInfoAccessor? accessor = GetChangeAccessor?.Invoke(this);

		if (accessor == null)
			return;

		if (accessor.GetChangeInfoSerialNumber() == g_SharedChangeInfo.SerialNumber) {
			// Ok, I still own this one.
			EdictChangeInfo p = g_SharedChangeInfo.ChangeInfos[accessor.GetChangeInfo()];

			// Now add this offset to our list of changed variables.		
			for (ushort i = 0; i < p.NumChangeFields; i++)
				if (p.ChangedFields[i] == field)
					return;

			if (p.NumChangeFields == MAX_CHANGE_OFFSETS) {
				// Invalidate our change info.
				accessor.SetChangeInfoSerialNumber(0);
				StateFlags |= EdictFlags.FullEdictChanged; // So we don't get in here again.
			}
			else 
				p.ChangedFields[p.NumChangeFields++] = field;
		}
		else {
			if (g_SharedChangeInfo.NumChangeInfos == MAX_EDICT_CHANGE_INFOS) {
				// Shucks.. have to mark the edict as fully changed because we don't have room to remember this change.
				accessor.SetChangeInfoSerialNumber(0);
				StateFlags |= EdictFlags.FullEdictChanged; // So we don't get in here again.
			}
			else {
				// Get a new CEdictChangeInfo and fill it out.
				accessor.SetChangeInfo(g_SharedChangeInfo.NumChangeInfos);
				g_SharedChangeInfo.NumChangeInfos++;

				accessor.SetChangeInfoSerialNumber(g_SharedChangeInfo.SerialNumber);

				EdictChangeInfo p = g_SharedChangeInfo.ChangeInfos[accessor.GetChangeInfo()];
				p.ChangedFields[0] = field;
				p.NumChangeFields = 1;
			}
		}
	}

	public static Func<BaseEdict, IChangeInfoAccessor>? GetChangeAccessor;

	public void ClearTransmitState() => StateFlags &= ~(EdictFlags.Always | EdictFlags.PVSCheck | EdictFlags.DontSend);

	public void SetChangeInfo(ushort info) { /* TODO */ }
	public void SetChangeInfoSerialNumber(ushort sn) { /* TODO */ }
	public ushort GetChangeInfo() => 0; // TODO
	public ushort GetChangeInfoSerialNumber() => 0; // TODO

	public EdictFlags StateFlags;

	public short NetworkSerialNumber;
	public int EdictIndex;
	public IServerNetworkable? Networkable;
	protected IServerUnknown? Unk;
	// Use this to clear any fields on the edict
	public virtual void InitializeEntityDLLFields() { }
}

/// <summary>
/// Analog of edict_t
/// </summary>
public class Edict : BaseEdict
{
	public ICollideable? GetCollideable() {
		IServerEntity? ent = GetIServerEntity();
		if (ent != null)
			return ent.GetCollideable();
		else
			return null;
	}

	public override void InitializeEntityDLLFields() {
		base.InitializeEntityDLLFields();
		FreeTime = default;
	}

	public TimeUnit_t FreeTime;
}

public class IChangeInfoAccessor
{
	public void SetChangeInfo(ushort info) => ChangeInfo = info;
	public void SetChangeInfoSerialNumber(ushort sn) => ChangeInfoSerialNumber = sn;

	public ushort GetChangeInfo() => ChangeInfo;
	public ushort GetChangeInfoSerialNumber() => ChangeInfoSerialNumber;

	private ushort ChangeInfo;
	private ushort ChangeInfoSerialNumber;
}
