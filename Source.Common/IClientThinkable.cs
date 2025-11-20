namespace Source.Common;

public interface IClientThinkable {
	IClientUnknown GetIClientUnknown();
	void ClientThink();
	void Release();
	ClientThinkHandle_t GetThinkHandle();
	void SetThinkHandle(ClientThinkHandle_t hThink);
}
