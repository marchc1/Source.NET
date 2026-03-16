using Source;
using Source.Common;

namespace Game.Shared;

public class EntInfo
{
	public IHandleEntity? Entity;
	public int SerialNumber;
	public EntInfo? Prev;
	public EntInfo? Next;

	public void ClearLinks() => Prev = Next = this;
}

public class EntInfoList
{
	public EntInfo? First;
	public EntInfo? Last;

	public void AddToHead(EntInfo element) => LinkAfter(null, element);
	public void AddToTail(EntInfo element) => LinkBefore(null, element);

	public void LinkBefore(EntInfo? before, EntInfo element) {
		Assert(element != null);

		Unlink(element);

		element.Next = before;

		if (before == null) {
			element.Prev = Last;
			Last = element;
		}
		else {
			Assert(IsInList(before));
			element.Prev = before.Prev;
			before.Prev = element;
		}

		if (element.Prev == null)
			First = element;
		else
			element.Prev.Next = element;
	}

	public void LinkAfter(EntInfo? after, EntInfo element) {
		Assert(element != null);

		if (IsInList(element))
			Unlink(element);

		element.Prev = after;
		if (after == null) {
			element.Next = First;
			First = element;
		}
		else {
			Assert(IsInList(after));
			element.Next = after.Next;
			after.Next = element;
		}

		if (element.Next == null)
			Last = element;
		else
			element.Next.Prev = element;
	}

	public void Unlink(EntInfo element) {
		if (IsInList(element)) {
			if (element.Prev != null)
				element.Prev.Next = element.Next;
			else
				First = element.Next;

			if (element.Next != null)
				element.Next.Prev = element.Prev;
			else
				Last = element.Prev;

			element.ClearLinks();
		}
	}

	public bool IsInList(EntInfo element) => element.Prev != element;
}

public class BaseEntityList
{
	const int SERIAL_MASK = 0x7fff;

	public BaseEntityList() {
		((Span<EntInfo>)EntPtrArray).ClearInstantiatedReferences();

		for (int i = 0; i < Constants.NUM_ENT_ENTRIES; i++) {
			EntPtrArray[i].ClearLinks();
			EntPtrArray[i].SerialNumber = Random.Shared.Next() & SERIAL_MASK;
			EntPtrArray[i].Entity = null;
		}

		for (int i = Constants.MAX_EDICTS + 1; i < Constants.NUM_ENT_ENTRIES; i++)
			FreeNonNetworkableList.AddToTail(EntPtrArray[i]);
	}

	public BaseHandle AddNetworkableEntity(IHandleEntity ent, int index, int forcedSerialNum = -1) {
		Assert(index >= 0 && index < Constants.MAX_EDICTS);
		return AddEntityAtSlot(ent, index, forcedSerialNum);
	}

	private BaseHandle AddEntityAtSlot(IHandleEntity ent, int slot, int forcedSerialNum) {
		EntInfo entSlot = EntPtrArray[slot];
		Assert(entSlot.Entity == null);
		entSlot.Entity = ent;

		if (forcedSerialNum != -1) {
			entSlot.SerialNumber = forcedSerialNum;
#if !CLIENT_DLL
			Assert(false);
#endif
		}

		ActiveList.AddToTail(entSlot);

		BaseHandle ret = new(slot, entSlot.SerialNumber);

		ent.SetRefEHandle(ret);

		OnAddEntity(ent, ret);
		return ret;
	}

	public BaseHandle AddNonNetworkableEntity(IHandleEntity ent) {
		EntInfo? slot = FreeNonNetworkableList.First;
		if (slot == null) {
			Warning("BaseEntityList.AddNonNetworkableEntity: no free slots!\n");
			Assert(false, "BaseEntityList.AddNonNetworkableEntity: no free slots!\n");
			return new();
		}

		FreeNonNetworkableList.Unlink(slot);
		int iSlot = GetEntInfoIndex(slot);

		return AddEntityAtSlot(ent, iSlot, -1);
	}

	public void RemoveEntity(BaseHandle handle) => RemoveEntityAtSlot(handle.GetEntryIndex());

	void RemoveEntityAtSlot(int slot) {
		Assert(slot >= 0 && slot < Constants.NUM_ENT_ENTRIES);

		EntInfo info = EntPtrArray[slot];

		if (info.Entity != null) {
			info.Entity.SetRefEHandle(new BaseHandle(Constants.INVALID_EHANDLE_INDEX));

			OnRemoveEntity(info.Entity, new BaseHandle(slot, info.SerialNumber));

			info.Entity = null;
			info.SerialNumber = (info.SerialNumber + 1) & SERIAL_MASK;

			ActiveList.Unlink(info);

			if (slot >= Constants.MAX_EDICTS)
				FreeNonNetworkableList.AddToTail(info);
		}
	}

	public IHandleEntity? LookupEntityByNetworkIndex(int edictIndex) {
		if (edictIndex < 0)
			return null;
		return EntPtrArray[edictIndex].Entity;
	}

	public IHandleEntity? LookupEntity(in BaseHandle handle) {
		if (handle.Index == Constants.INVALID_EHANDLE_INDEX)
			return null;

		EntInfo info = EntPtrArray[handle.GetEntryIndex()];
		if (info.SerialNumber == handle.GetSerialNumber())
			return info.Entity;
		else
			return null;
	}

	public BaseHandle FirstHandle() {
		if (ActiveList.First == null)
			return new BaseHandle(Constants.INVALID_EHANDLE_INDEX);

		int index = GetEntInfoIndex(ActiveList.First);
		return new BaseHandle(index, EntPtrArray[index].SerialNumber);
	}

	public BaseHandle NextHandle(BaseHandle ent) {
		int slot = ent.GetEntryIndex();
		EntInfo? next = EntPtrArray[slot].Next;
		if (next == null)
			return new(Constants.INVALID_EHANDLE_INDEX);

		int index = GetEntInfoIndex(next);
		return new(index, EntPtrArray[index].SerialNumber);
	}

	public static BaseHandle InvalidHandle() => new(Constants.INVALID_EHANDLE_INDEX);

	// These are notifications to the derived class. It can cache info here if it wants.
	protected virtual void OnAddEntity(IHandleEntity? pEnt, BaseHandle handle) { }
	// It is safe to delete the entity here. We won't be accessing the pointer after
	// calling OnRemoveEntity.
	protected virtual void OnRemoveEntity(IHandleEntity? pEnt, BaseHandle handle) { }

	int GetEntInfoIndex(EntInfo entInfo) {
		Assert(entInfo != null);
		Span<EntInfo> span = EntPtrArray;
		for (int i = 0; i < span.Length; i++) {
			if (span[i] == entInfo)
				return i;
		}
		Assert(false, "EntInfo not found in EntPtrArray");
		return -1;
	}

	InlineArrayNumEntEntries<EntInfo> EntPtrArray;
	public EntInfoList ActiveList = new();
	EntInfoList FreeNonNetworkableList = new();

	public EntInfo? FirstEntInfo() => ActiveList.First;
	public EntInfo? NextEntInfo(EntInfo? current) => current?.Next;

	public EntInfo GetEntInfoPtr(BaseHandle ent) {
		int slot = ent.GetEntryIndex();
		return EntPtrArray[slot];
	}

	public EntInfo GetEntInfoPtrByIndex(int index) => EntPtrArray[index];
}
