namespace Source.Common.Entity;

public interface IClientEntityList
{
	public IClientNetworkable? GetClientNetworkable(int entnum);
	public int GetHighestEntityIndex();
}