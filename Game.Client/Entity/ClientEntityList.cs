using Source.Common.Entity;

namespace Game.Client.Entity;

public class ClientEntityList : IClientEntityList
{
	public IClientNetworkable? GetClientNetworkable(int entnum)
	{
		return null;
	}

	public int GetHighestEntityIndex()
	{
		return 0;
	}
}