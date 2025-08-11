namespace Source.Common.Entity;

public class IClientEntity;
public interface IClientEntityList
{
	public IClientNetworkable? GetClientNetworkable(int entnum);
	public int GetHighestEntityIndex();
}