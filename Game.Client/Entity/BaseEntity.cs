using Game.Client;
using Source.Common.Mathematics;
using Source.Common.Networking.DataTable;
using System.Runtime.InteropServices;
using System.Xml;

namespace Source.Common.Entity;

//-----------------------------------------------------------------------------
// Purpose: Base client side entity object
//-----------------------------------------------------------------------------
public class BaseEntity : IClientEntity
{
	public static BaseEntityList EntityList;
	private BaseHandle RefEHandle;
	// private static ClientClass<BaseEntity> Class = new("C_BaseEntity");
	private int index;
	private long CreationTick;
	private GCHandle GChandle;

	~BaseEntity()
	{
		Term();
	}

	public void Init(int entityNum, int serialNum)
	{
		index = entityNum;

		HLClient.EntityList.AddNetworkableEntity(GetIClientUnknown(), entityNum, serialNum);

		CreationTick = HLClient.GlobalVars.TickCount;
	}

	public virtual void ClientThink()
	{
	}

	public void SetRefEHandle(BaseHandle handle)
	{
		RefEHandle = handle;
	}

	public BaseHandle GetRefEHandle()
	{
		return RefEHandle;
	}

	public virtual void Release()
	{
	}

	public virtual void Term()
	{
		if (RefEHandle.Index != BaseHandle.INVALID_EHANDLE_INDEX)
		{
			HLClient.EntityList.RemoveEntity(RefEHandle);
			RefEHandle.Term();
		}
	}

	public virtual IClientUnknown? GetIClientUnknown()
	{
		return this;
	}

	public virtual IntPtr GetDataTableBasePtr()
    {
        if (!GChandle.IsAllocated)
            GChandle = GCHandle.Alloc(this, GCHandleType.Pinned);

		// RaphaelIT7: Holy FUCK, why is C# such a piece of shit, I want "this" as a pointer/IntPtr though it cannot be casted in any way :sob:
        return GChandle.AddrOfPinnedObject();
    }


	/*public virtual ClientClass GetClientClass()
	{
		// RaphaelIT7: I should really change the names xD
		return Class.Class;
	}*/

	public virtual void SetDestroyedOnRecreateEntities()
	{
	}
	public virtual void OnPreDataChanged(DataUpdateType updateType)
	{
	}
	public virtual void OnDataChanged(DataUpdateType updateType)
	{
	}

	public virtual void PreDataUpdate(DataUpdateType updateType)
	{
	}
	public virtual void PostDataUpdate(DataUpdateType updateType)
	{
	}
}