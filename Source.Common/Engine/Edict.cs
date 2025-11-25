namespace Source.Common.Engine;

[Flags]
public enum EdictFlags
{
	/// <summary>
	/// Game DLL sets this when the entity state changes
	/// </summary>
	Changed = (1 << 0),
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
		return null;
	}

	public IServerUnknown? GetUnknown() {
		return null;
	}

	public void SetEdict(IServerUnknown unk, bool fullEdict) {

	}

	public int AreaNum() => Unk == null ? 0 : Networkable!.AreaNum();
	public ReadOnlySpan<char> GetClassName() => Unk == null ? "" : Networkable!.GetClassName();

	public bool IsFree() => (StateFlags & EdictFlags.Free) != 0;
	public void SetFree() => StateFlags |= EdictFlags.Free;
	public void ClearFree() => StateFlags &= ~EdictFlags.Free;

	public bool HasStateChanged() => (StateFlags & EdictFlags.Changed) != 0;
	public void ClearStateChanged() {
		StateFlags &= ~(EdictFlags.Changed | EdictFlags.FullEdictChanged);
		SetChangeInfoSerialNumber(0);
	}
	public void StateChanged() {
		StateFlags |= (EdictFlags.Changed | EdictFlags.FullEdictChanged);
		SetChangeInfoSerialNumber(0);
	}
	public void StateChanged(ushort offset) {
		// todo
	}

	public void ClearTransmitState() => StateFlags &= ~(EdictFlags.Always | EdictFlags.PVSCheck | EdictFlags.DontSend);

	public void SetChangeInfo(ushort info) { /* TODO */ }
	public void SetChangeInfoSerialNumber(ushort sn) { /* TODO */ }
	public ushort GetChangeInfo() => 0; // TODO
	public ushort GetChangeInfoSerialNumber() => 0; // TODO

	public EdictFlags StateFlags;

	public short NetworkSerialNumber;
	public short EdictIndex;
	public IServerNetworkable? Networkable;
	protected IServerUnknown? Unk;
}

/// <summary>
/// Analog of edict_t
/// </summary>
public class Edict : BaseEdict {
	public ICollideable? GetCollideable() {
		IServerEntity? ent = GetIServerEntity();
		if (ent != null)
			return ent.GetCollideable();
		else
			return null;
	}
	public TimeUnit_t FreeTime;
}
