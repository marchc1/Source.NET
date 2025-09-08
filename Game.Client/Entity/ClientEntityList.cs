using Source;
using Source.Common.Entity;
using System;

namespace Game.Client.Entity;

public class ClientEntityList : BaseEntityList, IClientEntityList
{
	private struct EntityCacheInfo
	{
		// Cached off because GetClientNetworkable is called a *lot*
		public IClientNetworkable? Networkable;
		public ushort BaseEntitiesIndex; // Index into m_BaseEntities (or m_BaseEntities.InvalidIndex() if none).
	};

	private EntityCacheInfo[] EntityCacheInfos = new EntityCacheInfo[Constants.NUM_ENT_ENTRIES];
	private int MaxUsedServerIndex = 0;

	public IClientNetworkable? GetClientNetworkable(int Entity)
	{
		return EntityCacheInfos[Entity].Networkable;
	}

	public int GetHighestEntityIndex()
	{
		return MaxUsedServerIndex;
	}

	public IClientUnknown? GetListedEntity(int Entity)
	{
		return (IClientUnknown?)LookupEntityByNetworkIndex(Entity);
	}

	private void RecomputeHighestEntityUsed()
	{
		MaxUsedServerIndex = -1;

		// Walk backward looking for first valid index
		for (int i=Constants.MAX_EDICTS - 1; i >= 0; i--)
		{
			if (GetListedEntity(i) != null)
			{
				MaxUsedServerIndex = i;
				break;
			}
		}
	}
}