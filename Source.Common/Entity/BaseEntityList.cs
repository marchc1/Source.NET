using Source;
using Source.Common.Entity;
using static Source.Common.Engine.IStaticPropMgrEngine;
using static Source.Dbg;

namespace Source.Common.Entity;

public class EntInfo
{
	public IHandleEntity? Entity;
	public int SerialNumber;
	public EntInfo? Prev;
	public EntInfo? Next;

	public void ClearLinks()
	{
		Prev = Next = null;
	}
};


public class BaseEntityList
{
	public BaseEntityList()
	{
		activeList = new();
		freeNonNetworkableList = new();
		EntPtrArray = new EntInfo[Constants.NUM_ENT_ENTRIES];
		for (int i=0; i<Constants.NUM_ENT_ENTRIES; ++i)
		{
			EntPtrArray[i] = new EntInfo();

			EntPtrArray[i].ClearLinks();
			EntPtrArray[i].SerialNumber = (Random.Shared.Next() & SERIAL_MASK); // generate random starting serial number
			EntPtrArray[i].Entity = null;
		}

		// make a free list of the non-networkable entities
		// Initially, all the slots are free.
		for (int i=Constants.MAX_EDICTS+1; i<Constants.NUM_ENT_ENTRIES; ++i)
		{
			EntInfo pList = EntPtrArray[i];
			freeNonNetworkableList.AddToTail(pList);
		}
	}

	~BaseEntityList()
	{
		EntInfo? pList = activeList.Head();
		while (pList != null)
		{
			EntInfo? pNext = pList.Next;
			RemoveEntityAtSlot(GetEntInfoIndex(ref pList));
			pList = pNext;
		}
	}
	
	// Add and remove entities. iForcedSerialNum should only be used on the client. The server
	// gets to dictate what the networkable serial numbers are on the client so it can send
	// ehandles over and they work.
	public BaseHandle AddNetworkableEntity(IHandleEntity Entity, int index, int ForcedSerialNum = -1)
	{
		return AddEntityAtSlot(Entity, index, ForcedSerialNum);
	}

	public BaseHandle AddNonNetworkableEntity(IHandleEntity Entity)
	{
		// Find a slot for it.
		EntInfo? pSlot = freeNonNetworkableList.Head();
		if (pSlot == null)
		{
			Warning("CBaseEntityList::AddNonNetworkableEntity: no free slots!\n");
			AssertMsg(false, "CBaseEntityList::AddNonNetworkableEntity: no free slots!\n");
			return new BaseHandle();
		}

		// Move from the free list into the allocated list.
		freeNonNetworkableList.Unlink(pSlot);
		int SlotIndex = GetEntInfoIndex(ref pSlot);
	
		return AddEntityAtSlot(Entity, SlotIndex, -1);
	}

	public void RemoveEntity(BaseHandle handle)
	{
		RemoveEntityAtSlot(handle.GetEntryIndex());
	}

	// Get an ehandle from a networkable entity's index (note: if there is no entity in that slot,
	// then the ehandle will be invalid and produce NULL).
	public BaseHandle GetNetworkableHandle(int iEntity)
	{
		if (EntPtrArray[iEntity].Entity != null)
			return new BaseHandle(iEntity, EntPtrArray[iEntity].SerialNumber);
		else
			return new BaseHandle();
	}

	// ehandles use this in their Get() function to produce a pointer to the entity.
	public IHandleEntity? LookupEntity(BaseHandle handle)
	{
		if (handle.Index == BaseHandle.INVALID_EHANDLE_INDEX)
			return null;

		EntInfo? pInfo = EntPtrArray[handle.GetEntryIndex()];
		if (pInfo.SerialNumber == handle.GetSerialNumber())
			return pInfo.Entity;
		else
			return null;
	}

	public IHandleEntity? LookupEntityByNetworkIndex(int edictIndex)
	{
		// (Legacy support).
		if (edictIndex < 0)
			return null;

		return EntPtrArray[edictIndex].Entity;
	}

	// Use these to iterate over all the entities.
	public BaseHandle FirstHandle()
	{
		EntInfo info = activeList.Head();
		if ( info == null )
			return new BaseHandle();

		int index = GetEntInfoIndex(ref info);
		return new BaseHandle(index, EntPtrArray[index].SerialNumber);
	}

	public BaseHandle NextHandle(BaseHandle Entity)
	{
		int iSlot = Entity.GetEntryIndex();
		EntInfo? pNext = EntPtrArray[iSlot].Next;
		if (pNext == null)
			return new BaseHandle();

		int index = GetEntInfoIndex(ref pNext);
		return new BaseHandle(index, EntPtrArray[index].SerialNumber);
	}

	public static BaseHandle InvalidHandle()
	{
		return new BaseHandle();
	}

	public EntInfo? FirstEntInfo()
	{
		return activeList.Head();
	}

	public EntInfo? NextEntInfo(EntInfo pInfo)
	{
		return pInfo.Next;
	}

	public EntInfo? GetEntInfoPtr(BaseHandle Entity)
	{
		int iSlot = Entity.GetEntryIndex();
		return EntPtrArray[iSlot];
	}

	public EntInfo? GetEntInfoPtrByIndex(int index)
	{
		return EntPtrArray[index];
	}

	// These are notifications to the derived class. It can cache info here if it wants.
	protected virtual void OnAddEntity(IHandleEntity pEnt, BaseHandle handle)
	{
	}
	
	// It is safe to delete the entity here. We won't be accessing the pointer after
	// calling OnRemoveEntity.
	protected virtual void OnRemoveEntity(IHandleEntity pEnt, BaseHandle handle)
	{
	}

	private BaseHandle AddEntityAtSlot(IHandleEntity pEnt, int Slot, int ForcedSerialNum)
	{
		// Init the CSerialEntity.
		EntInfo pSlot = EntPtrArray[Slot];
		pSlot.Entity = pEnt;

		// Force the serial number (client-only)?
		if (ForcedSerialNum != -1)
		{
			pSlot.SerialNumber = ForcedSerialNum;
		}
	
		// Update our list of active entities.
		activeList.AddToTail(pSlot);
		BaseHandle retVal = new BaseHandle(Slot, pSlot.SerialNumber);

		// Tell the entity to store its handle.
		pEnt.SetRefEHandle(retVal);

		// Notify any derived class.
		OnAddEntity(pEnt, retVal);
		return retVal;
	}

	private const int SERIAL_MASK = 0x7fff;
	private void RemoveEntityAtSlot(int Slot)
	{
		EntInfo? pInfo = EntPtrArray[Slot];

		if (pInfo.Entity != null)
		{
			pInfo.Entity.SetRefEHandle(new BaseHandle(BaseHandle.INVALID_EHANDLE_INDEX));

			// Notify the derived class that we're about to remove this entity.
			OnRemoveEntity(pInfo.Entity, new BaseHandle(Slot, pInfo.SerialNumber));

			// Increment the serial # so ehandles go invalid.
			pInfo.Entity = null;
			pInfo.SerialNumber = (pInfo.SerialNumber + 1) & SERIAL_MASK;

			activeList.Unlink(pInfo);

			// Add the slot back to the free list if it's a non-networkable entity.
			if (Slot >= Constants.MAX_EDICTS)
			{
				freeNonNetworkableList.AddToTail( pInfo );
			}
		}
	}

	
	private class EntInfoList
	{
		public EntInfoList()
		{
			pHead = null;
			pTail = null;
		}

		public EntInfo? Head() { return pHead; }
		public EntInfo? Tail() { return pTail; }
		public void AddToHead(EntInfo pElement) { LinkAfter( null, pElement ); }
		public void AddToTail(EntInfo pElement) { LinkBefore( null, pElement ); }

		public void LinkBefore(EntInfo? pBefore, EntInfo pElement)
		{
			// Unlink it if it's in the list at the moment
			Unlink(pElement);
	
			// The element *after* our newly linked one is the one we linked before.
			pElement.Next = pBefore;
			if (pBefore == null)
			{
				// In this case, we're linking to the end of the list, so reset the tail
				pElement.Prev = pTail;
				pTail = pElement;
			} else {
				// Here, we're not linking to the end. Set the prev pointer to point to
				// the element we're linking.
				pElement.Prev = pBefore.Prev;
				pBefore.Prev = pElement;
			}
	
			// Reset the head if we linked to the head of the list
			if (pElement.Prev == null)
			{
				pHead = pElement;
			} else {
				pElement.Prev.Next = pElement;
			}
		}

		public void LinkAfter(EntInfo? pAfter, EntInfo pElement)
		{
			// Unlink it if it's in the list at the moment
			if (IsInList(pElement))
				Unlink(pElement);
	
			// The element *before* our newly linked one is the one we linked after
			pElement.Prev = pAfter;
			if (pAfter == null)
			{
				// In this case, we're linking to the head of the list, reset the head
				pElement.Next = pHead;
				pHead = pElement;
			} else {
				// Here, we're not linking to the end. Set the next pointer to point to
				// the element we're linking.
				pElement.Next = pAfter.Next;
				pAfter.Next = pElement;
			}
	
			// Reset the tail if we linked to the tail of the list
			if (pElement.Next == null )
			{
				pTail = pElement;
			} else {
				pElement.Next.Prev = pElement;
			}
		}

		public void Unlink(EntInfo pElement)
		{
			if (IsInList(pElement))
			{
				// If we're the first guy, reset the head
				// otherwise, make our previous node's next pointer = our next
				if ( pElement.Prev != null )
				{
					pElement.Prev.Next = pElement.Next;
				} else {
					pHead = pElement.Next;
				}
		
				// If we're the last guy, reset the tail
				// otherwise, make our next node's prev pointer = our prev
				if ( pElement.Next != null )
				{
					pElement.Next.Prev = pElement.Prev;
				} else {
					pTail = pElement.Prev;
				}
		
				// This marks this node as not in the list, 
				// but not in the free list either
				pElement.ClearLinks();
			}
		}

		public bool IsInList(EntInfo pElement)
		{
			return pElement.Prev != pElement;
		}
	
		private EntInfo? pHead;
		private EntInfo? pTail;
	};

	private unsafe int GetEntInfoIndex(ref EntInfo pEntInfo)
	{
		fixed (EntInfo* entInfoPtr = &pEntInfo)
		fixed (EntInfo* basePtr = EntPtrArray)
		{
			int index = (int)(entInfoPtr - basePtr);
			return index;
		}
	}

	// The first MAX_EDICTS entities are networkable. The rest are client-only or server-only.
	private EntInfo[] EntPtrArray;
	private EntInfoList activeList;
	private EntInfoList freeNonNetworkableList;
};