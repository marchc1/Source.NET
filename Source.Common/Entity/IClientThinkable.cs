namespace Source.Common.Entity;

public interface IClientThinkable
{
	public IClientUnknown? GetIClientUnknown();
	public void ClientThink();
	// public object GetThinkHandle();
	// public void SetThinkHandle(object value);
	public void Release();
}