using Source;
using Source.Common;

namespace Game.Shared;

public class EntInfo {
	public IHandleEntity? Entity;
	public int SerialNumber;
	public EntInfo? Prev;
	public EntInfo? Next;
}

public class EntInfoList : LinkedList<EntInfo>;

public class BaseEntityList
{
	public BaseEntityList() {
		((Span<EntInfo>)EntPtrArray).ClearInstantiatedReferences();
	}
	public BaseHandle AddNetworkableEntity(IHandleEntity ent, int index, int forcedSerialNum = -1) {
		return AddEntityAtSlot(ent, index, forcedSerialNum);
	}

	private BaseHandle AddEntityAtSlot(IHandleEntity ent, int slot, int forcedSerialNum) {
		EntInfo entSlot = EntPtrArray[slot];
		Assert(entSlot.Entity == null);
		entSlot.Entity = ent;

		if(forcedSerialNum != -1) 
			entSlot.SerialNumber = forcedSerialNum;

		ActiveList.AddLast(entSlot);
		BaseHandle ret = new(slot, entSlot.SerialNumber);

		ent.SetRefEHandle(ret);
		OnAddEntity(ent, ret);

		return ret;
	}

	public BaseHandle AddNonNetworkableEntity(IHandleEntity pEnt) {
		throw new NotImplementedException();
	}
	public void RemoveHandle(BaseHandle handle) {
		throw new NotImplementedException();
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

	// These are notifications to the derived class. It can cache info here if it wants.
	protected virtual void OnAddEntity(IHandleEntity? pEnt, BaseHandle handle) { }
	// It is safe to delete the entity here. We won't be accessing the pointer after
	// calling OnRemoveEntity.
	protected virtual void OnRemoveEntity(IHandleEntity? pEnt, BaseHandle handle) { }

	InlineArrayNumEntEntries<EntInfo> EntPtrArray;
	EntInfoList ActiveList = [];
	EntInfoList FreeNonNetworkableList = [];
}
